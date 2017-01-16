# Prepare your development environment

This document describes how to prepare your development environment to use the *Microsoft Azure IoT Gateway Modbus Module*. It requires a pre-setup environment for the *Microsoft Azure IoT Gateway SDK*.

- [Setting up an environment for Microsoft Azure IoT Gateway SDK](https://github.com/Azure/azure-iot-gateway-sdk/blob/master/doc/devbox_setup.md)

# Compile the Modbus module

This section shows you how to integrate the Modbus module with the Azure IoT Gateway SDK and compile it.

1. Copy the modules\\**modbus_read** folder from the Azure IoT Gateway Modbus and paste it into the **modules** directory of the Azure IoT Gateway SDK.
2. Copy the samples\\**modbus_sample** folder from the Azure IoT Gateway Modbus and paste it into the **samples** directory of the Azure IoT Gateway SDK.
3. Edit the **CMakeLists.txt** in the **modules** directory. Add this line `add_subdirectory(modbus_read)` to the end of the file.
4. Edit the **CMakeLists.txt** in the **samples** directory. Add this line `add_subdirectory(modbus_sample)` to the end of the file.
5. [Compile the Azure IoT Gateway SDK](https://github.com/Azure/azure-iot-gateway-sdk/blob/master/samples/hello_world/README.md#how-to-build-the-sample) for your machine.
