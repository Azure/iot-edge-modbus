This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments

# Azure IoT Edge Modbus Module GA #
Using this module, developers can build Azure IoT Edge solutions with Modbus TCP/RTU(RTU is currently not available in Windows environment, please use Linux host + Linux container to play with RTU mode) connectivity. The Modbus module is an [Azure IoT Edge](https://github.com/Azure/iot-edge) module, capable of reading data from Modbus devices and publishing data to the Azure IoT Hub via the Edge framework. Developers can modify the module tailoring to any scenario.

![](./doc/diagram.png)

There are prebuilt Modbus TCP module container images ready at [here](https://hub.docker.com/r/microsoft/azureiotedge-modbus-tcp) for you to quickstart the experience of Azure IoT Edge on your target device or simulated device.

Visit http://azure.com/iotdev to learn more about developing applications for Azure IoT.

## Azure IoT Edge Compatibility ##
Current version of the module is targeted for the [Azure IoT Edge GA](https://azure.microsoft.com/en-us/blog/azure-iot-edge-generally-available-for-enterprise-grade-scaled-deployments/).  
If you are using [v1 version of IoT Edge](https://github.com/Azure/iot-edge/tree/master/v1) (previously known as Azure IoT Gateway), please use v1 version of this module, all materials can be found in [v1](https://github.com/Azure/iot-edge-modbus/tree/master/v1) folder.

Find more information about Azure IoT Edge at [here](https://docs.microsoft.com/en-us/azure/iot-edge/how-iot-edge-works).

## Target Device Setup ##

### Platform Compatibility ###
Azure IoT Edge is designed to be used with a broad range of operating system platforms. Modbus module has been tested on the following platforms:

- Windows 10 Enterprise (version 1709) x64
- Windows 10 IoT Core (version 1709) x64
- Linux x64
- Linux arm32v7

### Device Setup ###
- [Windows 10 Desktop](https://docs.microsoft.com/en-us/azure/iot-edge/quickstart)
- [Windows 10 IoT Core](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-core)
- [Linux](https://docs.microsoft.com/en-us/azure/iot-edge/quickstart-linux)


## Build Environment Setup ##
Modbus module is a .NET Core 2.1 application, which is developed and built based on the guidelines in Azure IoT Edge document.
Please follow [this link](https://docs.microsoft.com/en-us/azure/iot-edge/tutorial-csharp-module) to setup the build environment. 

Basic requirement:
- Docker CE
- .NET Core 2.1 SDK

## HowTo Build ##
In this section, the Modbus module we be built as an IoT Edge module.

Open the project in VS Code, and open VS Code command palette, type and run the command Edge: Build IoT Edge solution.
Select the deployment.template.json file for your solution from the command palette.  
***Note: Be sure to check [configuration section](https://github.com/Azure/iot-edge-modbus#configuration) to properly set each fields before deploying the module.*** 

In Azure IoT Hub Devices explorer, right-click an IoT Edge device ID, then select Create deployment for IoT Edge device. 
Open the config folder of your solution, then select the deployment.json file. Click Select Edge Deployment Manifest. 
Then you can see the deployment is successfully created with a deployment ID in VS Code integrated terminal.
You can check your container status in the VS Code Docker explorer or by run the docker ps command in the terminal.

## Configuration ##
Before running the module, proper configuration is required. Here is a sample configuration for your reference.
```json
{
  "PublishInterval": "2000",
  "Version":"2",
  "SlaveConfigs": {
    "Slave01": {
      "SlaveConnection": "192.168.0.1",
      "HwId": "PowerMeter-0a:01:01:01:01:01",
      "RetryCount": "10",
      "RetryInterval": "50",
      "Operations": {
        "Op01": {
          "PollingInterval": "1000",
          "UnitId": "1",
          "StartAddress": "400001",
          "Count": "2",
          "DisplayName": "Voltage",
          "CorrelationId": "MessageType1"
        },
        "Op02": {
          "PollingInterval": "1000",
          "UnitId": "1",
          "StartAddress": "400002",
          "Count": "2",
          "DisplayName": "Current",
          "CorrelationId": "MessageType1"
        }
      }
    },
    "Slave02": {
      "SlaveConnection": "ttyS0",
      "HwId": "PowerMeter-0a:01:01:01:01:02",
      "BaudRate": "9600",
      "DataBits": "8",
      "StopBits": "1",
      "Parity": "ODD",
      "FlowControl": "NONE",
      "Operations": {
        "Op01": {
          "PollingInterval": "2000",
          "UnitId": "1",
          "StartAddress": "40001",
          "Count": "1",
          "DisplayName": "Power"
        },
        "Op02": {
          "PollingInterval": "2000",
          "UnitId": "1",
          "StartAddress": "40003",
          "Count": "1",
          "DisplayName": "Status"
        }
      }
    }
  }
}
```
Meaning of each field:

* "PublishInterval" - Interval between each push to IoT Hub in millisecond
* "Version" - switch between the GA (Generally Available) and the latest Message Payload format. (valid value for GA: "1", all other values will switch to latest format) 
* "SlaveConfigs" - Contains one or more Modbus slaves' configuration. In this sample, we have "Slave01" and "Slave02" two devices:
    * "Slave01", "Slave02" - User defined names for each Modbus slave, cannot have duplicates under "SlaveConfigs".
    * "SlaveConnection" - Ipv4 address or the serial port name of the Modbus slave.
    * "RetryCount" - Max retry attempt for reading data, default to 10
    * "RetryInterval" - Retry interval between each retry attempt, default to 50 milliseconds
    * "HwId" - Unique Id for each Modbus slave (user defined)
    * "BaudRate" - Serial port communication parameter. (valid values: ...9600, 14400,19200...)
    * "DataBits" - Serial port communication parameter. (valid values: 7, 8)
    * "StopBits" - Serial port communication parameter. (valid values: 1, 1.5, 2)
    * "Parity" - Serial port communication parameter. (valid values: ODD, EVEN, NONE)
    * "FlowControl" - Serial port communication parameter. (valid values: ONLY support NONE now)
    * "Operations" - Contains one or more Modbus read requests. In this sample, we have "Op01" and "Op02" two read requests in both Slave01 and Slave02:
        * "Op01", "Op02" - User defined names for each read request, cannot have duplicates under the same "Operations" section.
        * "PollingInterval": Interval between each read request in millisecond
        * "UnitId" - The unit id to be read
        * "StartAddress" - The starting address of Modbus read request, currently supports both 5-digit and 6-digit [format](https://en.wikipedia.org/wiki/Modbus#Coil.2C_discrete_input.2C_input_register.2C_holding_register_numbers_and_addresses)
        * "Count" - Number of registers/bits to be read
        * "DisplayName" - Alternative name for the "StartAddress" register(s)(user defined)
        * "CorrelationId" - The Operations with same id with be grouped together in their output message

For more about Modbus, please refer to the [Wiki](https://en.wikipedia.org/wiki/Modbus) link.

## Module Endpoints and Routing ##
There are two endpoints defined in Modbus TCP module:  
- "modbusOutput": This is a output endpoint for telemetries. All read operations defined in configuration will be composed as telemetry messages output to this endpoint.
- "input1": This is an input endpoint for write commands.

Input/Output message format and Routing rules are introduced below.

### Read from Modbus ###

#### Telemetry Message ####
Message Properties: 
```json
"content-type": "application/edge-modbus-json"
```
Latest Message Payload:
```json
[
    {
      "PublishTimestamp": "2018-04-17 12:28:53",
      "Content": [
        {
          "HwId": "PowerMeter-0a:01:01:01:01:02",
          "Data": [
            {
              "CorrelationId": "MessageType1",
              "SourceTimestamp": "2018-04-17 12:28:48",
              "Values": [
                {
                  "DisplayName": "Op02",
                  "Address": "40003",
                  "Value": "2785"
                },
                {
                  "DisplayName": "Op02",
                  "Address": "40004",
                  "Value": "18529"
                },
                {
                  "DisplayName": "Op01",
                  "Address": "40001",
                  "Value": "1840"
                },
                {
                  "DisplayName": "Op01",
                  "Address": "40002",
                  "Value": "31497"
                }
              ]
            },
            {
              "CorrelationId": "MessageType1",
              "SourceTimestamp": "2018-04-17 12:28:50",
              "Values": [
                {
                  "DisplayName": "Op02",
                  "Address": "40003",
                  "Value": "21578"
                },
                {
                  "DisplayName": "Op02",
                  "Address": "40004",
                  "Value": "26979"
                },
                {
                  "DisplayName": "Op01",
                  "Address": "40001",
                  "Value": "13210"
                },
                {
                  "DisplayName": "Op01",
                  "Address": "40002",
                  "Value": "13549"
                }
              ]
            }
          ]
        }
      ]
    }
  ]
```
GA (generally available) Message Payload:
```json

```

#### Route to IoT Hub ####
```json
{
  "routes": {
    "modbusToIoTHub":"FROM /messages/modules/modbus/outputs/modbusOutput INTO $upstream"
  }
}
```

#### Route to other (filter) modules ####
```json
{
  "routes": {
    "modbusToFilter":"FROM /messages/modules/modbus/outputs/modbusOutput INTO BrokeredEndpoint(\"/modules/filtermodule/inputs/input1\")"
  }
}
```

### Write to Modbus ###
Modbus module use input endpoint "input1" to receive commands. Currently it supports writing back to a single register/cell in a Modbus slave.  
***Note: Currently IoT Edge only supports send messages into one module from another module, direct C2D messages doesn't work.*** 

#### Command Message ####
The content of command must be the following message format.  

Message Properties: 
```json
"command-type": "ModbusWrite"
```

Message Payload:
```json
{
  "HwId":"PowerMeter-0a:01:01:01:01:01",
  "UId":"1",
  "Address":"40001",
  "Value":"15"
}
```

#### Route from other (filter) modules ####
The command should have a property "command-type" with value "ModbusWrite". Also, routing must be enabled by specifying rule like below.
```json
{
  "routes": {
    "filterToModbus":"FROM /messages/modules/filtermodule/outputs/output1 INTO BrokeredEndpoint(\"/modules/modbus/inputs/input1\")"
  }
}
```

## HowTo Run ##

### Run as an IoT Edge module ###
Please follow the [link](https://docs.microsoft.com/en-us/azure/iot-edge/tutorial-csharp-module) to deploy the module as an IoT Edge module.

#### Configure Modbus RTU ####
This is for Modbus RTU only, Modbus TCP could skip this section.

In the **Container Create Option section**, enter the following for device mapping.
```json
{
  "HostConfig": {
    "Devices": [
      {
        "PathOnHost": "<device name on host machine>",
        "PathInContainer": "<device name in container>",
        "CgroupPermissions": "rwm"
      }
    ]
  }
}
```
