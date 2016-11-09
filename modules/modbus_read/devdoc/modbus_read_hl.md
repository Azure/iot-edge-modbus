# MODBUS_READ HL MODULE

## References
* [modbus_read](./modbus_read.md)
* [json](http://www.json.org)
* [gateway](../../../../devdoc/gateway_requirements.md)
* [modbus](https://en.wikipedia.org/wiki/Modbus)

IEEE Std 802-2014, section 8.1 (MAC Address canonical form)

## Overview

This module adapts the Modbus module for use with the Gateway HL library. It is passthrough to the existing module,
with a specialized create function to interpret the serialized JSON module arguments.

## ModbusRead_HL_Create
```c
MODULE_HANDLE ModbusRead_HL_Create(BROKER_HANDLE broker, const void* configuration);
```
Creates a new MODBUS_READ MODULE HL instance. `configuration` is a pointer to a `const char*` that contains a json object as supplied by `Gateway_Create_From_JSON`.
By convention in the json object should contain the target modbus server and related read operation settings.

### Expected Arguments

The arguments to this module is a JSON array of the following object:
```json
{
		"serverConnectionString": "<ip address or COM port number of the modbus connection>",
		"interval": "<the interval value in ms between each cell's update>",
		"deviceType": "<string value to describe the type of the modbus device>",
		"macAddress": "<mac address in canonical form>",
		"operations": [
		{
			"unitId": "<station/slave address of modbus device>",
			"functionCode": "<function code of the read request>",
			"startingAddress": "<starting cell address of the read request>",
			"length": "<number of cells of the read request>"
		}
    ]
}	
```
Example:
The following Gateway config file will contain a module called "modbus_read_hl" build from F:\modbus_read_hl.dll
```json
{
    "modules" :
    [ 
        {
            "module name" : "modbus_read_hl",
            "module path" : "modbus_read_hl.dll",
            "args" : 
            {
              "serverConnectionString": "127.0.0.1",
              "interval": "10000",
			  "deviceType": "powerMeter",
              "macAddress": "01:01:01:01:01:01",
              "operations": [
                {
                  "unitId": "1",
                  "functionCode": "3",
                  "startingAddress": "0",
                  "length": "5"
                }
              ]
            }
        }
   ]
}
```



**SRS_MODBUS_READ_HL_99_001: [** If `broker` is NULL then `ModbusRead_HL_Create` shall fail and return NULL. **]**
**SRS_MODBUS_READ_HL_99_003: [** If `configuration` is NULL then `ModbusRead_HL_Create` shall fail and return NULL. **]**
**SRS_MODBUS_READ_HL_99_011: [** If configuration is not a JSON object, then `ModbusRead_HL_Create` shall fail and return NULL. **]**
**SRS_MODBUS_READ_HL_99_012: [** If the JSON value does not contain `args` array then `ModbusRead_HL_Create` shall fail and return NULL. **]**
**SRS_MODBUS_READ_HL_99_013: [** If the JSON object of `args` array does not contain `operations` array then `ModbusRead_HL_Create` shall fail and return NULL. **]**
**SRS_MODBUS_READ_HL_99_005: [** `ModbusRead_HL_Create` shall pass `broker` and the entire config to `ModbusRead_Create`. **]**
**SRS_MODBUS_READ_HL_99_006: [** If `ModbusRead_Create` succeeds then `ModbusRead_HL_Create` shall succeed and return a non-NULL value. **]**
**SRS_MODBUS_READ_HL_99_007: [** If `ModbusRead_Create` fails then `ModbusRead_HL_Create` shall fail and return NULL. **]**
**SRS_MODBUS_READ_HL_99_014: [** If the `args` object does not contain a value named "serverConnectionString" then ModbusRead_HL_Create shall fail and return NULL. **]**
**SRS_MODBUS_READ_HL_99_015: [** If the `args` object does not contain a value named "macAddress" then ModbusRead_HL_Create shall fail and return NULL. **]**
**SRS_MODBUS_READ_HL_99_016: [** If the `args` object does not contain a value named "interval" then ModbusRead_HL_Create shall fail and return NULL. **]**
**SRS_MODBUS_READ_HL_99_026: [** If the `args` object does not contain a value named "deviceType" then ModbusRead_HL_Create shall fail and return NULL. **]**
**SRS_MODBUS_READ_HL_99_021: [** `ModbusRead_HL_Create` shall use "serverConnectionString", "macAddress", and "interval" values as the fields for an MODBUS_READ_CONFIG structure and add this element to the link list. **]**
**SRS_MODBUS_READ_HL_99_017: [** If the `operations` object does not contain a value named "unitId" then ModbusRead_HL_Create shall fail and return NULL. **]**
**SRS_MODBUS_READ_HL_99_018: [** If the `operations` object does not contain a value named "functionCode" then ModbusRead_HL_Create shall fail and return NULL. **]**
**SRS_MODBUS_READ_HL_99_019: [** If the `operations` object does not contain a value named "startingAddress" then ModbusRead_HL_Create shall fail and return NULL. **]**
**SRS_MODBUS_READ_HL_99_020: [** If the `operations` object does not contain a value named "length" then ModbusRead_HL_Create shall fail and return NULL. **]**
**SRS_MODBUS_READ_HL_99_022: [** `ModbusRead_HL_Create` shall use "unitId", "functionCode", "startingAddress" and "length" values as the fields for an MODBUS_READ_OPERATION structure and add this element to the link list. **]**
**SRS_MODBUS_READ_HL_99_023: [** If the 'malloc' for `config` fail, ModbusRead_HL_Create shall fail and return NULL. **]**
**SRS_MODBUS_READ_HL_99_024: [** If the 'malloc' for `operation` fail, ModbusRead_HL_Create shall fail and return NULL. **]**
**SRS_MODBUS_READ_HL_99_025: [** ModbusRead_HL_Create shall walk through each object of the array. **]**

## ModbusRead_HL_Receive
```c
void ModbusRead_HL_Receive(MODULE_HANDLE moduleHandle, MESSAGE_HANDLE messageHandle);
```
**SRS_MODBUS_READ_HL_99_008: [** `ModbusRead_HL_Receive` shall pass the received parameters to the underlying ModbusRead's `_Receive` function. **]** 


## ModbusRead_HL_Destroy
```c
void ModbusRead_HL_Destroy(MODULE_HANDLE moduleHandle);
```
**SRS_MODBUS_READ_HL_99_009: [** `ModbusRead_HL_Destroy` shall destroy all used resources. **]**

## Module_GetAPIs
```c
extern const void Module_GetAPIS(MODULE_APIS* apis);
```
**SRS_MODBUS_READ_HL_99_010: [** `Module_GetAPIS` shall fill the provided MODULE_APIS structure with the required function pointers. **]**