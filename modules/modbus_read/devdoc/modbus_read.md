# MODBUS_READ MODULE

## Overview

This module is mainly a Modbus reader which also performs Modbus writeback action on demand. The module periodically read from a Modbus server and publish data to IoT Hub.

### Additional data types
```c
typedef struct MODBUS_READ_OPERATION_TAG
{
	struct MODBUS_READ_OPERATION_TAG * p_next;
	unsigned char unit_id;
	unsigned char function_code;
	unsigned short address;
	unsigned short length;
}MODBUS_READ_OPERATION;

typedef struct MODBUS_READ_CONFIG_TAG
{
	struct MOD_BUS_READ_CONFIG_TAG * p_next;
	struct MODBUS_READ_OPERATION_TAG * p_operation;
	unsigned int interval;
	char server_ip[16];
	char mac_address[18];
	SOCKET_TYPE s;
	int time_check;
}MODBUS_READ_CONFIG;
```

## ModbusRead_Create
```c
MODULE_HANDLE ModbusRead_Create(BROKER_HANDLE broker, const void* configuration);
```
Creates a new MODBUS_READ instance. `configuration` is a pointer to a `MODBUS_READ_CONFIG`.

**SRS_MODBUS_READ_99_001: [**If broker is NULL then ModbusRead_Create shall fail and return NULL.**]**
**SRS_MODBUS_READ_99_002: [**If configuration is NULL then ModbusRead_Create shall fail and return NULL.**]**
**SRS_MODBUS_READ_99_007: [**If ModbusRead_Create encounters any errors while creating the MODBUSREAD_HANDLE_DATA then it shall fail and return NULL.**]**
**SRS_MODBUS_READ_99_008: [**Otherwise ModbusRead_Create shall return a non-NULL pointer.**]**

## ModbusRead_Receive
```c
void ModbusRead_Receive(MODULE_HANDLE moduleHandle, MESSAGE_HANDLE messageHandle);
```

**SRS_MODBUS_READ_99_009: [**If moduleHandle is NULL then ModbusRead_Receive shall fail and return.**]**
**SRS_MODBUS_READ_99_010: [**If messageHandle is NULL then ModbusRead_Receive shall fail and return.**]**
**SRS_MODBUS_READ_99_011: [**If `messageHandle` properties does not contain a "source" property, then ModbusRead_Receive shall fail and return.**]**
**SRS_MODBUS_READ_99_012: [**If `messageHandle` properties contains a "deviceKey" property, then ModbusRead_Receive shall fail and return.**]**
**SRS_MODBUS_READ_99_013: [**If `messageHandle` properties contains a "source" property that is set to "mapping", then ModbusRead_Receive shall fail and return.**]**
**SRS_MODBUS_READ_99_017: [**ModbusRead_Receive shall return.**]**
**SRS_MODBUS_READ_99_018: [**If content of messageHandle is not a JSON value, then `ModbusRead_Receive` shall fail and return NULL.**]**


## ModbusRead_Destroy
```c
void ModbusRead_Destroy(MODULE_HANDLE moduleHandle);
```
**SRS_MODBUS_READ_99_014: [**If moduleHandle is NULL then ModbusRead_Destroy shall return.**]**
**SRS_MODBUS_READ_99_015: [**Otherwise ModbusRead_Destroy shall unuse all used resources.**]**


## Module_GetAPIs
```c
extern const void Module_GetAPIS(MODULE_APIS* apis);
```
**SRS_MODBUS_READ_99_016: [**Module_GetAPIS` shall fill the provided MODULE_APIS structure with the required function pointers.**]**