This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments

# Azure IoT Edge Modbus Module Preview #
Using this module, developers can build Azure IoT Edge solutions with Modbus TCP/RTU connectivity. The Modbus module is an [Azure IoT Edge](https://github.com/Azure/iot-edge) module, capable of reading data from Modbus devices and publishing data to the Azure IoT Hub via the Edge framework. Developers can modify the module tailoring to any scenario. Alternatively, the module can also be run in standalone mode for debug purpose, which doesn't require IoT Edge framework.

![](./doc/diagram.png)

There are prebuilt Modbus TCP module container images ready at [microsoft/azureiotedge-modbus-tcp:1.0-preview](https://hub.docker.com/r/microsoft/azureiotedge-modbus-tcp) for you to quickstart the experience of Azure IoT Edge on your target device or simulated device.

Visit http://azure.com/iotdev to learn more about developing applications for Azure IoT.

## Azure IoT Edge Compatibility ##
Current version of the module is targeted for the [Azure IoT Edge (second version in public preview)](https://github.com/Azure/azure-iot-edge).  
If you are using [v1 version of IoT Edge](https://github.com/Azure/iot-edge/tree/master/v1) (previously known as Azure IoT Gateway), please use v1 version of this module, all materials can be found in [v1](https://github.com/Azure/iot-gateway-modbus/tree/master/v1) folder.

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
Modbus module is a .NET Core 2.0 application, which is developed and built based on the guidelines in Azure IoT Edge document.
Please follow [this link](https://docs.microsoft.com/en-us/azure/iot-edge/tutorial-csharp-module) to setup the build environment. 

Basic requirement:
- Docker CE
- .NET Core 2.0 SDK (optional, if you prefer to manually build application on host machine)

## HowTo Build ##
In this section, the Modbus TCP module we be built as an IoT Edge module.

Dockerfiles are located under [Docker](https://github.com/Azure/iot-edge-modbus/tree/master/Docker) folder, you should be able to find one for your platform. There are two Dockerfiles in each platform, choose either one for your build preference.
- "Dockerfile-auto": For multi-stage build, which will build application inside container and generate a target container image. 
- "Dockerfile": This requires you to build applicaiton on host machine and then use this Dockerfile to copy application binary to generate target container image.  

**Note**: [Multi-stage](https://github.com/Azure/iot-edge-modbus#multi-stage-build) build for Linux-arm32 doesn't work at this moment, please do with [single-stage](https://github.com/Azure/iot-edge-modbus#single-stage-build) build.  
**Note**: Please replace \<platform\> in below scripts with the actual platform path you are trying to build.

### Build as an IoT Edge module ###
1. Open "Program.cs" file in [src](https://github.com/Azure/iot-edge-modbus/tree/master/src) folder.
2. Confirm **IOT_EDGE** flag is enabled at first line code.
```csharp
#define IOT_EDGE
```

#### Multi-stage build ###
Run docker build with the following commands.
```sh
$cd iot-edge-modbus/
$docker build -t "modbus:latest" -f Docker/<PlatForm>/Dockerfile-auto .
```

#### Single-stage build ####
Run docker build with the following commands.
```sh
$cd iot-edge-modbus/src/
$dotnet restore
$dotnet build
$dotnet publish -f netcoreapp2.0 -c Release
$cd ../
$docker build --build-arg EXE_DIR=./src/bin/Release/netcoreapp2.0/publish -t "modbus:latest" -f Docker/<PlatForm>/Dockerfile .
```

## Configuration ##
Before running the module, proper configuration is required. Here is a sample configuration for your reference.
```json
{
  "PublishInterval": "2000",
  "SlaveConfigs": {
    "Slave01": {
      "SlaveConnection": "192.168.0.1",
      "HwId": "PowerMeter-0a:01:01:01:01:01",
      "Operations": {
        "Op01": {
          "PollingInterval": "1000",
          "UnitId": "1",
          "StartAddress": "400001",
          "Count": "1",
          "DisplayName": "Voltage"
        },
        "Op02": {
          "PollingInterval": "1000",
          "UnitId": "1",
          "StartAddress": "400002",
          "Count": "1",
          "DisplayName": "Current"
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
          "StartAddress": "40003",
          "Count": "1",
          "DisplayName": "Power"
        },
        "Op02": {
          "PollingInterval": "2000",
          "UnitId": "1",
          "StartAddress": "40004",
          "Count": "1",
          "DisplayName": "Status"
        }
      }
    }
  }
}
```
Meaning of each field:

* "PublishInterval" – Interval between each push to IoT Hub in millisecond
* "SlaveConfigs" – Contains one or more Modbus slaves' configuration. In this sample, we have "Slave01" and "Slave02" two devices:
    * "Slave01", "Slave02" - User defined names for each Modbus slave, cannot have duplicates under "SlaveConfigs".
    * "SlaveConnection" – Ipv4 address or the serial port name of the Modbus slave
    * "HwId" – Unique Id for each Modbus slave (user defined)
    * "BaudRate" – Serial port communication parameter. (valid values: ...9600, 14400,19200...)
    * "DataBits" – Serial port communication parameter. (valid values: 7, 8)
    * "StopBits" – Serial port communication parameter. (valid values: 1, 1.5, 2)
    * "Parity" – Serial port communication parameter. (valid values: ODD, EVEN, NONE)
    * "FlowControl" – Serial port communication parameter. (valid values: only support NONE now)
    * "Operations" – Contains one or more Modbus read requests. In this sample, we have "Op01" and "Op02" two read requests in both Slave01 and Slave02:
        * "Op01", "Op02" - User defined names for each read request, cannot have duplicates under the same "Operations" section.
        * "PollingInterval": Interval between each read request in millisecond
        * "UnitId" – The unit id to be read
        * "StartAddress" – The starting address of Modbus read request, currently supports both 5-digit and 6-digit [format](https://en.wikipedia.org/wiki/Modbus#Coil.2C_discrete_input.2C_input_register.2C_holding_register_numbers_and_addresses)
        * "Count" – Number of registers/bits to be read
        * "DisplayName" – Alternative name for the "StartAddress" register(s)(user defined)

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
Message Payload:
```json
[
  {
    "DisplayName":"Voltage",
    "HwId":"PowerMeter-0a:01:01:01:01:01",
    "Address":"400001",
    "Value":"19775",
    "SourceTimestamp":"2017-11-17 08:43:50"
  },
  {
    "DisplayName":"Current",
    "HwId":"PowerMeter-0a:01:01:01:01:01",
    "Address":"400002",
    "Value":"15650",
    "SourceTimestamp":"2017-11-17 08:43:50"
  }
]
```

#### Route to IoT Hub ####
```json
{
  "rotues": {
    "modbusToIoTHub":"FROM /messages/modules/modbus/outputs/modbusOutput INTO $upstream"
  }
}
```

#### Route to other (filter) modules ####
```json
{
  "rotues": {
    "modbusToFilter":"FROM /messages/modules/modbus/outputs/modbusOutput INTO BrokeredEndpoint(\"/modules/filtermodule/inputs/input1\")"
  }
}
```

### Write to Modbus ###
Modbus module use input endpoint "input1" to receive commands. Currently it supports writing back to a single register/cell in a Modbus slave.  

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
  "rotues": {
    "filterToModbus":"FROM /messages/modules/filtermodule/outputs/output1 INTO BrokeredEndpoint(\"/modules/modbus/inputs/input1\")"
  }
}
```

## HowTo Run ##

### Run as an IoT Edge module ###
Please follow the [link](https://docs.microsoft.com/en-us/azure/iot-edge/tutorial-csharp-module) to deploy the module as an IoT Edge module.

## Debug from Visual Studio 2017 ##
Running debug mode requires IoT device connection string being inserted as a environment variable named **EdgeHubConnectionString**, and a local configuration file "iot-edge-modbus.json" since module twin is not available under debug mode. You can copy "iot-edge-modbus.json" template from project root directory to working directory and modify the content to fit your test case.  

**Note**: running in debug mode means none of the IoT Edge features is available. This mode is only to debug non edge-related functions. 

1. Open "iot-edge-modbus.csproj" with **Visual Studio 2017**.
2. Open "Program.cs" file in [src](https://github.com/Azure/iot-edge-modbus/tree/master/src) folder.
3. Confirm **IOT_EDGE** flag is disabled at first line code.
```csharp
// #define IOT_EDGE
```
4. Press **Ctrl+Shift+B** to build project.
5. Go to **Project** and select **iot-edge-modbus Properties...**
6. Switch to **Debug** tab.
7. Add **Environment variables** with **Name** = "EdgeHubConnectionString", and **Value** = "\<your IoT device connection string\>". The connection string can be found from Azure Portal.
8. Press F5 to run project.
