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
        static int counter = 0;
        static List<Task> task_list = new List<Task>();
        static bool run = true;

        static void Main(string[] args)
        {
            // Install CA certificate
            InstallCert();

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
            } else if (!File.Exists(certPath))
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
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(onDesiredPropertiesUpdate, ioTHubModuleClient);

            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error when initializing module: {0}", exception);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error when initializing module: {0}", ex.Message);
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
            int counterValue = Interlocked.Increment(ref counter);

            var userContextValues = userContext as Tuple<DeviceClient, Modbus.Slaves.ModuleHandle>;
            if (userContextValues == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " +
                    "expected values");
            }
            DeviceClient ioTHubModuleClient = userContextValues.Item1;
            Modbus.Slaves.ModuleHandle moduleHandle = userContextValues.Item2;

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
        static async Task onDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            DeviceClient ioTHubModuleClient = userContext as DeviceClient;

            try
            {
                // stop all activities while updating configuration
#if IOT_EDGE
                await ioTHubModuleClient.SetInputMessageHandlerAsync(
                "input1",
                DummyCallBack,
                null);
#endif

                run = false;
                await Task.WhenAll(task_list);
                task_list.Clear();
                run = true;

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
            Modbus.Slaves.ModuleHandle moduleHandle;
            string jsonStr;
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
                // get config from local file
                jsonStr = File.ReadAllText(@"iot-edge-modbus.json");
            }

            config = JsonConvert.DeserializeObject<ModuleConfig>(jsonStr);

            moduleHandle = new Modbus.Slaves.ModuleHandle();

            await CompleteHandleFromConfiguration(moduleHandle, config);

            if (moduleHandle != null)
            {
                var userContext = new Tuple<DeviceClient, Modbus.Slaves.ModuleHandle>(ioTHubModuleClient, moduleHandle);
#if IOT_EDGE
                // Register callback to be called when a message is received by the module
                await ioTHubModuleClient.SetInputMessageHandlerAsync(
                "input1",
                PipeMessage,
                userContext);
#else
                task_list.Add(Receive(userContext));
#endif
                task_list.Add(Start(userContext));
            }
        }

        /// <summary>
        /// Complete module handle from configuration
        /// </summary>
        /// <param name="moduleHandle"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        static async Task CompleteHandleFromConfiguration(Modbus.Slaves.ModuleHandle moduleHandle, ModuleConfig config)
        {
            foreach (var config_pair in config.SlaveConfigs)
            {
                ModbusSlaveConfig slaveConfig = config_pair.Value;
                switch (slaveConfig.GetConnectionType())
                {
                    case ModbusSlaveConfig.ConnectionType.ModbusTCP:
                        {
                            ModbusSlaveSession slave = new ModbusTCPSlaveSession(slaveConfig);
                            await slave.InitSession();
                            moduleHandle.ModbusSessionList.Add(slave);
                            break;
                        }
                    case ModbusSlaveConfig.ConnectionType.ModbusRTU:
                        {
                            break;
                        }
                    case ModbusSlaveConfig.ConnectionType.ModbusASCII:
                        {
                            break;
                        }
                    case ModbusSlaveConfig.ConnectionType.Unknown:
                        {
                            break;
                        }
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
            var userContextValues = userContext as Tuple<DeviceClient, Modbus.Slaves.ModuleHandle>;
            if (userContextValues == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " +
                    "expected values");
            }

            DeviceClient ioTHubModuleClient = userContextValues.Item1;
            Modbus.Slaves.ModuleHandle moduleHandle = userContextValues.Item2;

            while (run)
            {
                List<ModbusOutMessage> result = new List<ModbusOutMessage>();
                foreach (ModbusSlaveSession s in moduleHandle.ModbusSessionList)
                {
                    await s.ProcessOperations(result);
                }
                await Task.Delay(1500);
                if (result.Count > 0)
                {
                    Message message = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(result)));
                    message.Properties.Add("content-type", "application/edge-modbus-json");
#if IOT_EDGE
                    await ioTHubModuleClient.SendEventAsync("modbusOutput", message);
#else
                    await ioTHubModuleClient.SendEventAsync(message);
#endif
                }
            }
        }

        /// <summary>
        /// Receive C2D message(running without iot edge)
        /// </summary>
        /// <param name="userContext"></param>
        /// <returns></returns>
        static async Task Receive(object userContext)
        {
            var userContextValues = userContext as Tuple<DeviceClient, Modbus.Slaves.ModuleHandle>;
            if (userContextValues == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " +
                    "expected values");
            }
            DeviceClient ioTHubModuleClient = userContextValues.Item1;
            while (run)
            {
                Message message = await ioTHubModuleClient.ReceiveAsync();
                if (message != null)
                {
                    await PipeMessage(message, userContext);
                    await ioTHubModuleClient.CompleteAsync(message);
                }
            }
        }
    }
}