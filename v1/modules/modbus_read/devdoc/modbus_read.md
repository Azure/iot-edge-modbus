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
## ModbusRead_ParseConfigurationFromJson
```c
void* ModbusRead_ParseConfigurationFromJson(const void* configuration);
```
Creates a new configuration for MODBUS_READ module instance from a JSON string.`configuration` is a pointer to a `const char*` that contains a json object as supplied by `Gateway_CreateFromJson`.
By convention the json object should contain the target modbus server and related read operation settings.

### Expected Arguments

The arguments to this module is a JSON object with the following information:
```json
{
        "serverConnectionString": "<ip address or COM port number of the modbus connection>",
        "interval": "<the interval value in ms between each cell's update>",
        "deviceType": "<string value to describe the type of the modbus device>",
        "macAddress": "<mac address in canonical form>",
        "sqliteEnabled": "<0/1 to specify whether to enable SQLite module command>",
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
The following Gateway config file describes an instance of the "modbus_read" module, available .\modbus_read.dll:
```json
{
    "modules" :
    [ 
        {
            "name" : "modbus_read",
            "loader": {
                "type": "native",
                "entrypoint": {
                    "module path" : "modbus_read.dll"
                }
            },
            "args" : 
            {
              "serverConnectionString": "127.0.0.1",
              "interval": "10000",
              "deviceType": "powerMeter",
              "macAddress": "01:01:01:01:01:01",
              "sqliteEnabled": "0",
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


**SRS_MODBUS_READ_JSON_99_021: [** If `broker` is NULL then `ModbusRead_CreateFromJson` shall fail and return NULL. **]**

**SRS_MODBUS_READ_JSON_99_023: [** If `configuration` is NULL then `ModbusRead_CreateFromJson` shall fail and return NULL. **]**

**SRS_MODBUS_READ_JSON_99_031: [** If `configuration` is not a JSON object, then `ModbusRead_CreateFromJson` shall fail and return NULL. **]**

**SRS_MODBUS_READ_JSON_99_032: [** If the JSON value does not contain "args" array then `ModbusRead_CreateFromJson` shall fail and return NULL. **]**

**SRS_MODBUS_READ_JSON_99_033: [** If the JSON object of `args` array does not contain "operations" array then `ModbusRead_CreateFromJson` shall fail and return NULL. **]**

**SRS_MODBUS_READ_JSON_99_025: [** `ModbusRead_CreateFromJson` shall pass `broker` and the entire config to `ModbusRead_Create`. **]**

**SRS_MODBUS_READ_JSON_99_026: [** If `ModbusRead_Create` succeeds then `ModbusRead_CreateFromJson` shall succeed and return a non-NULL value. **]**

**SRS_MODBUS_READ_JSON_99_027: [** If `ModbusRead_Create` fails then `ModbusRead_CreateFromJson` shall fail and return NULL. **]**

**SRS_MODBUS_READ_JSON_99_034: [** If the `args` object does not contain a value named "serverConnectionString" then `ModbusRead_CreateFromJson` shall fail and return NULL. **]**

**SRS_MODBUS_READ_JSON_99_035: [** If the `args` object does not contain a value named "macAddress" then `ModbusRead_CreateFromJson` shall fail and return NULL. **]**

**SRS_MODBUS_READ_JSON_99_036: [** If the `args` object does not contain a value named "interval" then `ModbusRead_CreateFromJson` shall fail and return NULL. **]**

**SRS_MODBUS_READ_JSON_99_046: [** If the `args` object does not contain a value named "deviceType" then `ModbusRead_CreateFromJson` shall fail and return NULL. **]**

**SRS_MODBUS_READ_JSON_99_041: [** `ModbusRead_CreateFromJson` shall use "serverConnectionString", "macAddress", and "interval" values as the fields for an `MODBUS_READ_CONFIG` structure and add this element to the link list. **]**

**SRS_MODBUS_READ_JSON_99_037: [** If the `operations` object does not contain a value named "unitId" then `ModbusRead_CreateFromJson` shall fail and return NULL. **]**

**SRS_MODBUS_READ_JSON_99_038: [** If the `operations` object does not contain a value named "functionCode" then `ModbusRead_CreateFromJson` shall fail and return NULL. **]**

**SRS_MODBUS_READ_JSON_99_039: [** If the `operations` object does not contain a value named "startingAddress" then `ModbusRead_CreateFromJson` shall fail and return NULL. **]**

**SRS_MODBUS_READ_JSON_99_040: [** If the `operations` object does not contain a value named "length" then `ModbusRead_CreateFromJson` shall fail and return NULL. **]**

**SRS_MODBUS_READ_JSON_99_042: [** `ModbusRead_CreateFromJson` shall use "unitId", "functionCode", "startingAddress" and "length" values as the fields for an `MODBUS_READ_OPERATION` structure and add this element to the link list. **]**

**SRS_MODBUS_READ_JSON_99_043: [** If the `malloc` for `config` fail, `ModbusRead_CreateFromJson` shall fail and return NULL. **]**

**SRS_MODBUS_READ_JSON_99_044: [** If the `malloc` for `operation` fail, `ModbusRead_CreateFromJson` shall fail and return NULL. **]**

**SRS_MODBUS_READ_JSON_99_045: [** `ModbusRead_CreateFromJson` shall walk through each object of the array. **]**


## ModbusRead_Create
```c
MODULE_HANDLE ModbusRead_Create(BROKER_HANDLE broker, const void* configuration);
```
Creates a new `MODBUS_READ` instance. `configuration` is a pointer to a `MODBUS_READ_CONFIG`.

**SRS_MODBUS_READ_99_001: [**If `broker` is NULL then `ModbusRead_Create` shall fail and return NULL.**]**

**SRS_MODBUS_READ_99_002: [**If `configuration` is NULL then `ModbusRead_Create` shall fail and return NULL.**]**

**SRS_MODBUS_READ_99_007: [**If `ModbusRead_Create` encounters any errors while creating the `MODBUSREAD_HANDLE_DATA` then it shall fail and return NULL.**]**

**SRS_MODBUS_READ_99_008: [**Otherwise `ModbusRead_Create` shall return a non-NULL pointer.**]**


## ModbusRead_Receive
```c
void ModbusRead_Receive(MODULE_HANDLE moduleHandle, MESSAGE_HANDLE messageHandle);
```


**SRS_MODBUS_READ_99_009: [**If `moduleHandle` is NULL then `ModbusRead_Receive` shall fail and return.**]**

**SRS_MODBUS_READ_99_010: [**If `messageHandle` is NULL then `ModbusRead_Receive` shall fail and return.**]**

**SRS_MODBUS_READ_99_011: [**If `messageHandle` properties does not contain a "source" property, then `ModbusRead_Receive` shall fail and return.**]**

**SRS_MODBUS_READ_99_012: [**If `messageHandle` properties contains a "deviceKey" property, then `ModbusRead_Receive` shall fail and return.**]**

**SRS_MODBUS_READ_99_013: [**If `messageHandle` properties contains a "source" property that is set to "mapping", then `ModbusRead_Receive` shall fail and return.**]**

**SRS_MODBUS_READ_99_017: [**`ModbusRead_Receive` shall return.**]**

**SRS_MODBUS_READ_99_018: [**If content of `messageHandle` is not a JSON value, then `ModbusRead_Receive` shall fail and return NULL.**]**


## ModbusRead_FreeConfiguration
```c
void ModbusRead_FreeConfiguration(void* configuration);
```
**SRS_MODBUS_READ_99_006: [**`ModbusRead_FreeConfiguration` shall do nothing, cleanup is done in `ModbusRead_Destroy`.**]**


## ModbusRead_Destroy
```c
void ModbusRead_Destroy(MODULE_HANDLE moduleHandle);
```
**SRS_MODBUS_READ_99_014: [**If `moduleHandle` is NULL then `ModbusRead_Destroy` shall return.**]**

**SRS_MODBUS_READ_99_015: [**Otherwise `ModbusRead_Destroy` shall unuse all used resources.**]**


## Module_GetAPIs
```c
extern const MODULE_API* MODULE_STATIC_GETAPI(MODBUSREAD_MODULE)(const MODULE_API_VERSION gateway_api_version);
extern const MODULE_API* Module_GetApi(const MODULE_API_VERSION gateway_api_version)
```
**SRS_MODBUS_READ_99_016: [**`Module_GetApi` shall return a pointer to a `MODULE_API` structure with the required function pointers.**]**
