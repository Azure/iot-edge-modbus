This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments

# Beta Azure IoT Gateway Modbus Module
Using this module, developers can build Azure IoT Gateway solutions with Modbus connectivity. The Modbus module is a [Azure IoT Gateway SDK](https://github.com/Azure/azure-iot-gateway-sdk) module, capable of reading data from Modbus devices and publish the data to the Azure IoT Hub via a message broker. Developers can modify the module tailoring to any scenario.

Visit http://azure.com/iotdev to learn more about developing applications for Azure IoT.

## Azure IoT Gateway SDK compatibility
Current version of the module is targeted for the Azure IoT Gateway SDK 2016-11-18 version.
Use the following script to download the compatible version Azure IoT Gateway SDK.
```
git clone -b "2016-11-18" --recursive https://github.com/Azure/azure-iot-gateway-sdk.git
```

## Operating system compatibility
Refer to [Azure IoT Gateway SDK](https://github.com/Azure/azure-iot-gateway-sdk#operating-system-compatibility)

## Hardware compatibility
Refer to [Azure IoT Gateway SDK](https://github.com/Azure/azure-iot-gateway-sdk#hardware-compatibility)

## Directory structure

### /doc
This folder contains step by step instructions for building and running the sample:

General documentation

- [Dev box setup](./doc/devbox_setup.md) contains instructions for configure your machine to build the Azure IoT Gateway Modbus module.

Samples

- [modbus sample](./doc/sample_modbus.md) contains step by step instructions for building and running the modbus sample.


### /samples
This folder contains a sample for the Azure IoT Gateway Modbus module. Step by step instructions for building and running the sample can be found at [sample_modbus.md](./doc/sample_modbus.md).

### /modules
This folder contains the Modbus module that could be included with the Azure IoT Gateway SDK. Details on the implementation of the module can be found in the [devdoc](./modules/modbus_read/devdoc) folder. 