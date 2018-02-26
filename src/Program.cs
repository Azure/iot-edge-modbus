#define IOT_EDGE

namespace Modbus.Containers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;
    using Modbus.Slaves;

    class Program
    {
        const string ModbusSlaves = "SlaveConfigs";
        const int DefaultPushInterval = 5000;
        static int m_counter = 0;
        static List<Task> m_task_list = new List<Task>();
        static bool m_run = true;
        static ModbusPushInterval m_interval = null;
        static object message_lock = new object();
        static List<ModbusOutMessage> result = new List<ModbusOutMessage>();

        static void Main(string[] args)
        {
#if IOT_EDGE
            // Install CA certificate
            InstallCert();
#endif

            // Initialize Edge Module
            InitEdgeModule().Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Add certificate in local cert store for use by client for secure connection to IoT Edge runtime
        /// </summary>
        static void InstallCert()
        {
            // Suppress cert validation on Windows for now
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            string certPath = Environment.GetEnvironmentVariable("EdgeModuleCACertificateFile");
            if (string.IsNullOrWhiteSpace(certPath))
            {
                // We cannot proceed further without a proper cert file
                Console.WriteLine("Missing path to certificate collection file.");
                throw new InvalidOperationException("Missing path to certificate file.");
            }
            else if (!File.Exists(certPath))
            {
                // We cannot proceed further without a proper cert file
                Console.WriteLine("Missing certificate collection file.");
                throw new InvalidOperationException("Missing certificate file.");
            }
            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(new X509Certificate2(X509Certificate2.CreateFromCertFile(certPath)));
            Console.WriteLine("Added Cert: " + certPath);
            store.Close();
        }

        /// <summary>
        /// Initializes the Azure IoT Client for the Edge Module
        /// </summary>
        static async Task InitEdgeModule()
        {
            try
            {
                // Open a connection to the Edge runtime using MQTT transport and
                // the connection string provided as an environment variable
                string connectionString = Environment.GetEnvironmentVariable("EdgeHubConnectionString");

                MqttTransportSettings mqttSettings = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
                // Suppress cert validation on Windows for now
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    mqttSettings.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                }
                ITransportSettings[] settings = { mqttSettings };

                DeviceClient ioTHubModuleClient = DeviceClient.CreateFromConnectionString(connectionString, settings);
                await ioTHubModuleClient.OpenAsync();
                Console.WriteLine("IoT Hub module client initialized.");

                // Read config from Twin and Start
                Twin moduleTwin = await ioTHubModuleClient.GetTwinAsync();
                await UpdateStartFromTwin(moduleTwin.Properties.Desired, ioTHubModuleClient);

                // Attach callback for Twin desired properties updates
                await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, ioTHubModuleClient);

            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error when initializing module: {0}", exception);
                }
            }

        }

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>
        static async Task<MessageResponse> PipeMessage(Message message, object userContext)
        {
            Console.WriteLine("Modbus Writer - Received command");
            int counterValue = Interlocked.Increment(ref m_counter);

            var userContextValues = userContext as Tuple<DeviceClient, Slaves.ModuleHandle>;
            if (userContextValues == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " +
                    "expected values");
            }
            DeviceClient ioTHubModuleClient = userContextValues.Item1;
            Slaves.ModuleHandle moduleHandle = userContextValues.Item2;

            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);
            Console.WriteLine($"Received message: {counterValue}, Body: [{messageString}]");

            message.Properties.TryGetValue("command-type", out string cmdType);
            if (cmdType == "ModbusWrite")
            {
                // Get message body, containing the write target and value
                var messageBody = JsonConvert.DeserializeObject<ModbusInMessage>(messageString);

                if (messageBody != null)
                {
                    Console.WriteLine($"Write device {messageBody.HwId}, " +
                        $"address: {messageBody.Address}, value: {messageBody.Value}");

                    ModbusSlaveSession target = moduleHandle.GetSlaveSession(messageBody.HwId);
                    if (target == null)
                    {
                        Console.WriteLine($"target \"{messageBody.HwId}\" not found!");
                    }
                    else
                    {
                        await target.WriteCB(messageBody.UId, messageBody.Address, messageBody.Value);
                    }
                }
            }
            return MessageResponse.Completed;
        }

        /// <summary> 
        /// Callback to handle Twin desired properties updates 
        /// </summary> 
        static async Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            DeviceClient ioTHubModuleClient = userContext as DeviceClient;

            try
            {
#if IOT_EDGE
                // stop all activities while updating configuration
                await ioTHubModuleClient.SetInputMessageHandlerAsync(
                "input1",
                DummyCallBack,
                null);
#endif

                m_run = false;
                await Task.WhenAll(m_task_list);
                m_task_list.Clear();
                m_run = true;

                await UpdateStartFromTwin(desiredProperties, ioTHubModuleClient);
            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error when receiving desired property: {0}", exception);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error when receiving desired property: {0}", ex.Message);
            }
        }

        /// <summary>
        /// A dummy callback does nothing
        /// </summary>
        /// <param name="message"></param>
        /// <param name="userContext"></param>
        /// <returns></returns>
        static async Task<MessageResponse> DummyCallBack(Message message, object userContext)
        {
            await Task.Delay(TimeSpan.FromSeconds(0));
            return MessageResponse.Abandoned;
        }

        /// <summary>
        /// Update Start from module Twin. 
        /// </summary>
        static async Task UpdateStartFromTwin(TwinCollection desiredProperties, DeviceClient ioTHubModuleClient)
        {
            ModuleConfig config;
            Slaves.ModuleHandle moduleHandle;
            string jsonStr = null;
            string serializedStr;

            serializedStr = JsonConvert.SerializeObject(desiredProperties);
            Console.WriteLine("Desired property change:");
            Console.WriteLine(serializedStr);

            if (desiredProperties.Contains(ModbusSlaves))
            {
                // get config from Twin
                jsonStr = serializedStr;
            }
            else
            {
                Console.WriteLine("No configuration found in desired properties, look in local...");
                if (File.Exists(@"iot-edge-modbus.json"))
                {
                    try
                    {
                        // get config from local file
                        jsonStr = File.ReadAllText(@"iot-edge-modbus.json");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Load configuration error: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("No configuration found in local file.");
                }
            }

            if (!string.IsNullOrEmpty(jsonStr))
            {
                Console.WriteLine("Attempt to load configuration: " + jsonStr);
                config = JsonConvert.DeserializeObject<ModuleConfig>(jsonStr);
                m_interval = JsonConvert.DeserializeObject<ModbusPushInterval>(jsonStr);

                if (m_interval == null)
                {
                    m_interval = new ModbusPushInterval(DefaultPushInterval);
                }
                
                moduleHandle = await Slaves.ModuleHandle.CreateHandleFromConfiguration(config, UpdateMessage);

                if (moduleHandle != null)
                {
                    var userContext = new Tuple<DeviceClient, Slaves.ModuleHandle>(ioTHubModuleClient, moduleHandle);
#if IOT_EDGE
                    // Register callback to be called when a message is received by the module
                    await ioTHubModuleClient.SetInputMessageHandlerAsync(
                    "input1",
                    PipeMessage,
                    userContext);
#else
                    m_task_list.Add(Receive(userContext));
#endif
                    m_task_list.Add(Start(userContext));
                }
            }
        }

        /// <summary>
        /// Iterate through each Modbus session to poll data 
        /// </summary>
        /// <param name="userContext"></param>
        /// <returns></returns>
        static async Task Start(object userContext)
        {
            var userContextValues = userContext as Tuple<DeviceClient, Slaves.ModuleHandle>;
            if (userContextValues == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " +
                    "expected values");
            }

            DeviceClient ioTHubModuleClient = userContextValues.Item1;
            Slaves.ModuleHandle moduleHandle = userContextValues.Item2;

            foreach (ModbusSlaveSession s in moduleHandle.ModbusSessionList)
            {
                s.ProcessOperations();
            }

            while (m_run)
            {
                Message message = null;
                lock (message_lock)
                {
                    if (result.Count > 0)
                    {
                        message = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(result)));
                        message.Properties.Add("content-type", "application/edge-modbus-json");
                    }
                    result.Clear();
                }
                if (message != null)
                {
#if IOT_EDGE
                    await ioTHubModuleClient.SendEventAsync("modbusOutput", message);
#else
                    await ioTHubModuleClient.SendEventAsync(message);
#endif
                }
                if (!m_run)
                {
                    break;
                }
                await Task.Delay(m_interval.PublishInterval);
            }
            moduleHandle.Release();
        }

        /// <summary>
        /// Receive C2D message(running without iot edge)
        /// </summary>
        /// <param name="userContext"></param>
        /// <returns></returns>
        static async Task Receive(object userContext)
        {
            var userContextValues = userContext as Tuple<DeviceClient, Slaves.ModuleHandle>;
            if (userContextValues == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " +
                    "expected values");
            }
            DeviceClient ioTHubModuleClient = userContextValues.Item1;
            var timeout = TimeSpan.FromSeconds(3);
            while (m_run)
            {
                try
                {
                    Message message = await ioTHubModuleClient.ReceiveAsync(timeout);
                    if (message != null)
                    {
                        await PipeMessage(message, userContext);
                        await ioTHubModuleClient.CompleteAsync(message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error when receiving: {0}", ex.Message);
                }
            }
        }

        static void UpdateMessage(List<ModbusOutMessage> messages)
        {
            lock(message_lock)
            {
                result.AddRange(messages);
            }
        }
    }
}