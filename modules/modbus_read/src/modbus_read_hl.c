// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdlib.h>
#ifdef _CRTDBG_MAP_ALLOC
#include <crtdbg.h>
#endif
#include "azure_c_shared_utility/gballoc.h"

#include "module.h"
#include "azure_c_shared_utility/xlogging.h"
#include <stdio.h>
#include "modbus_read.h"
#include "modbus_read_hl.h"

static void modbus_hl_cleanup(MODBUS_READ_CONFIG * config)
{
	MODBUS_READ_CONFIG * modbus_config = config;
	while (modbus_config)
	{
		MODBUS_READ_OPERATION * modbus_operation = modbus_config->p_operation;
		while (modbus_operation)
		{
			MODBUS_READ_OPERATION * temp_operation = modbus_operation;
			modbus_operation = modbus_operation->p_next;
			free(temp_operation);
		}

		MODBUS_READ_CONFIG * temp_config = modbus_config;
		modbus_config = modbus_config->p_next;
		free(temp_config);
	}
}
static bool addOneOperation(MODBUS_READ_OPERATION * operation, JSON_Object * operation_obj)
{
	bool result = true;
	const char* unit_id = json_object_get_string(operation_obj, "unitId");
	const char* function = json_object_get_string(operation_obj, "functionCode");
	const char* address = json_object_get_string(operation_obj, "startingAddress");
	const char* length = json_object_get_string(operation_obj, "length");

	if (unit_id == NULL)
	{
		/*Codes_SRS_MODBUSREAD_HL_99_017: [ If the `operations` object does not contain a value named "unitId" then ModbusRead_HL_Create shall fail and return NULL. ]*/
		LogError("Did not find expected %s configuration", "unitId");
		result = false;
	}
	else if (function == NULL)
	{
		/*Codes_SRS_MODBUSREAD_HL_99_018: [ If the `operations` object does not contain a value named "functionCode" then ModbusRead_HL_Create shall fail and return NULL. ]*/
		LogError("Did not find expected %s configuration", "functionCode");
		result = false;
	}
	else if (address == NULL)
	{
		/*Codes_SRS_MODBUSREAD_HL_99_019: [ If the `operations` object does not contain a value named "startingAddress" then ModbusRead_HL_Create shall fail and return NULL. ]*/
		LogError("Did not find expected %s configuration", "startingAddress");
		result = false;
	}
	else if (length == NULL)
	{
		/*Codes_SRS_MODBUSREAD_HL_99_020: [ If the `operations` object does not contain a value named "length" then ModbusRead_HL_Create shall fail and return NULL. ]*/
		LogError("Did not find expected %s configuration", "length");
		result = false;
	}

	if (!result)
	{
		free(operation);
		return result;
	}

	/*Codes_SRS_MODBUS_READ_HL_99_022: [ `ModbusRead_HL_Create` shall use "unitId", "functionCode", "startingAddress" and "length" values as the fields for an MODBUS_READ_OPERATION structure and add this element to the link list. ]*/
	operation->unit_id = atoi(unit_id);
	operation->function_code = atoi(function);
	operation->address = atoi(address);
	operation->length = atoi(length);

	return result;
}
static bool addOneServer(MODBUS_READ_CONFIG * config, JSON_Object * arg_obj)
{
	bool result = true;
	const char* server_str = json_object_get_string(arg_obj, "serverConnectionString");
	const char* mac_address = json_object_get_string(arg_obj, "macAddress");
	const char* interval = json_object_get_string(arg_obj, "interval");
	const char* device_type = json_object_get_string(arg_obj, "deviceType");
	if (server_str == NULL)
	{
		/*Codes_SRS_MODBUSREAD_HL_99_014: [ If the `args` object does not contain a value named "serverConnectionString" then ModbusRead_HL_Create shall fail and return NULL. ]*/
		LogError("Did not find expected %s configuration", "serverConnectionString");
		result = false;
	}
	else if (mac_address == NULL)
	{
		/*Codes_SRS_MODBUSREAD_HL_99_015: [ If the `args` object does not contain a value named "macAddress" then ModbusRead_HL_Create shall fail and return NULL. ]*/
		LogError("Did not find expected %s configuration", "macAddress");
		result = false;
	}
	else if (interval == NULL)
	{
		/*Codes_SRS_MODBUSREAD_HL_99_016: [ If the `args` object does not contain a value named "interval" then ModbusRead_HL_Create shall fail and return NULL. ]*/
		LogError("Did not find expected %s configuration", "interval");
		result = false;
	}
	else if (device_type == NULL)
	{
		/*Codes_SRS_MODBUSREAD_HL_99_026: [ If the `args` object does not contain a value named "deviceType" then ModbusRead_HL_Create shall fail and return NULL. ]*/
		LogError("Did not find expected %s configuration", "deviceType");
		result = false;
	}

	if (!result)
	{
		free(config);
		return result;
	}

	/*Codes_SRS_MODBUS_READ_HL_99_021: [ `ModbusRead_HL_Create` shall use "serverConnectionString", "macAddress", and "interval" values as the fields for an MODBUS_READ_CONFIG structure and add this element to the link list. ]*/
	memcpy(config->server_str, server_str, strlen(server_str));
	config->server_str[strlen(server_str)] = '\0';
	memcpy(config->mac_address, mac_address, strlen(mac_address));
	config->mac_address[strlen(mac_address)] = '\0';
	memcpy(config->device_type, device_type, strlen(device_type));
	config->device_type[strlen(device_type)] = '\0';

	config->read_interval = atoi(interval);

	return result;
}
static MODULE_HANDLE ModbusRead_HL_Create(BROKER_HANDLE broker, const void* configuration)
{

	MODULE_HANDLE result = NULL;
	/*Codes_SRS_MODBUSREAD_HL_99_001: [ If broker is NULL then ModbusRead_HL_Create shall fail and return NULL. ]*/
	/*Codes_SRS_MODBUSREAD_HL_99_003: [ If configuration is NULL then ModbusRead_HL_Create shall fail and return NULL. ]*/
	if (
		(broker == NULL) ||
		(configuration == NULL)
		)
	{
		LogError("NULL parameter detected broker=%p configuration=%p", broker, configuration);
	}
	else
	{
		/*Codes_SRS_MODBUSREAD_HL_99_011: [ If configuration is not a JSON object, then ModbusRead_HL_Create shall fail and return NULL. ]*/
		JSON_Value* json = json_parse_string((const char*)configuration);
		if (json == NULL)
		{
			LogError("unable to json_parse_string");
		}
		else
		{
			/*Codes_SRS_MODBUSREAD_HL_99_012: [ If the JSON value does not contain `args` array then ModbusRead_HL_Create shall fail and return NULL. ]*/
			JSON_Array * arg_array = json_value_get_array(json);
			if (arg_array == NULL)
			{
				LogError("json_value_get_array failed arg");
			}
			else
			{
				int arg_i;
				int arg_count = json_array_get_count(arg_array);
				bool parse_fail = false;
				MODBUS_READ_CONFIG * prev_config = NULL;

				/*Codes_SRS_MODBUS_READ_HL_99_025: [** ModbusRead_HL_Create shall walk through each object of the array. **]*/
				for (arg_i = 0; arg_i < arg_count; arg_i++)
				{
					MODBUS_READ_CONFIG * config = malloc(sizeof(MODBUS_READ_CONFIG));
					if (config == NULL)
					{
						/*Codes_SRS_MODBUS_READ_HL_99_023: [ If the 'malloc' for `config` fail, ModbusRead_HL_Create shall fail and return NULL. ]*/
						parse_fail = true;
						break;
					}
					else
					{
						memset(config, 0, sizeof(MODBUS_READ_CONFIG));// to set socket s to 0
						config->p_next = prev_config;

						JSON_Object* arg_obj = json_array_get_object(arg_array, arg_i);

						if (!addOneServer(config, arg_obj))
						{
							parse_fail = true;
							break;
						}
						else
						{
							/*Codes_SRS_MODBUSREAD_HL_99_013: [ If the JSON object of `args` array does not contain `operations` array then ModbusRead_HL_Create shall fail and return NULL. ]*/
							JSON_Array * operation_array = json_object_get_array(arg_obj, "operations");
							if (operation_array == NULL)
							{
								LogError("unable to json_object_get_array operation");
								parse_fail = true;
								break;
							}
							else
							{
								int operation_i;
								int operation_count = json_array_get_count(operation_array);
								MODBUS_READ_OPERATION * prev_operation = NULL;
								/*Codes_SRS_MODBUS_READ_HL_99_025: [** ModbusRead_HL_Create shall walk through each object of the array. **]*/
								for (operation_i = 0; operation_i < operation_count; operation_i++)
								{
									MODBUS_READ_OPERATION * operation = malloc(sizeof(MODBUS_READ_OPERATION));
									/*Codes_SRS_MODBUS_READ_HL_99_024: [ If the 'malloc' for `operation` fail, ModbusRead_HL_Create shall fail and return NULL. ]*/
									if (operation == NULL)
									{
										parse_fail = true;
										break;
									}
									else
									{
										operation->p_next = prev_operation;

										JSON_Object* operation_obj = json_array_get_object(operation_array, operation_i);

										//add one operation
										if (!addOneOperation(operation, operation_obj))
										{
											parse_fail = true;
											break;
										}
										prev_operation = operation;
									}
								}
								config->p_operation = prev_operation;
								if (parse_fail)
									break;
							}
						}
						prev_config = config;
					}
				}
				if (!parse_fail)
				{
					/*Codes_SRS_MODBUSREAD_HL_99_005: [ ModbusRead_HL_Create shall pass broker and the entire config to ModbusRead_Create. ]*/
					MODULE_APIS apis;
					MODULE_STATIC_GETAPIS(MODBUSREAD_MODULE)(&apis);
					result = apis.Module_Create(broker, prev_config);

					if (result == NULL)
					{
						/*Codes_SRS_MODBUSREAD_HL_99_007: [ If ModbusRead_Create fails then ModbusRead_HL_Create shall fail and return NULL. ]*/
						/*return result "as is" - that is - NULL*/
						LogError("unable to Module_Create MODBUSREAD static");
					}
					else
					{
						/*Codes_SRS_MODBUSREAD_HL_99_006: [ If ModbusRead_Create succeeds then ModbusRead_HL_Create shall succeed and return a non-NULL value. ]*/
						/*return result "as is" - that is - not NULL*/
					}
				}
				if (result == NULL)
					modbus_hl_cleanup(prev_config);
			}
			json_value_free(json);
		}
	}
	return result;
}

static void ModbusRead_HL_Destroy(MODULE_HANDLE module)
{
	/*Codes_SRS_MODBUS_READ_HL_99_009: [ ModbusRead_HL_Destroy shall destroy all used resources. ]*/ /*in this case "all" is "none"*/
	MODULE_APIS apis;
	MODULE_STATIC_GETAPIS(MODBUSREAD_MODULE)(&apis);
	apis.Module_Destroy(module);
}

static void ModbusRead_HL_Receive(MODULE_HANDLE moduleHandle, MESSAGE_HANDLE messageHandle)
{
	/*Codes_SRS_MODBUS_READ_HL_99_008: [ ModbusRead_HL_Receive shall pass the received parameters to the underlying ModbusRead's _Receive function. ]*/
	MODULE_APIS apis;
	MODULE_STATIC_GETAPIS(MODBUSREAD_MODULE)(&apis);
	apis.Module_Receive(moduleHandle, messageHandle);
}

/*
*	Required for all modules:  the public API and the designated implementation functions.
*/
static const MODULE_APIS ModbusRead_HL_APIS_all =
{
	ModbusRead_HL_Create,
	ModbusRead_HL_Destroy,
	ModbusRead_HL_Receive
};

#ifdef BUILD_MODULE_TYPE_STATIC
MODULE_EXPORT void MODULE_STATIC_GETAPIS(MODBUSREAD_MODULE_HL)(MODULE_APIS* apis)
#else
MODULE_EXPORT void Module_GetAPIS(MODULE_APIS* apis)
#endif
{
	if (!apis)
	{
		LogError("NULL passed to Module_GetAPIS");
	}
	else
	{
		/* Codes_SRS_MODBUS_READ_HL_99_010: [ Module_GetAPIS shall fill the provided MODULE_APIS structure with the required function pointers. ] */
		(*apis) = ModbusRead_HL_APIS_all;
	}
}
