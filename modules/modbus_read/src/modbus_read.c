// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdlib.h>
#ifdef _CRTDBG_MAP_ALLOC
#include <crtdbg.h>
#endif
#include "azure_c_shared_utility/gballoc.h"

#include "module.h"

#include "azure_c_shared_utility/threadapi.h"
#include "modbus_read.h"
#include "message.h"
#include "azure_c_shared_utility/xlogging.h"
#include "azure_c_shared_utility/lock.h"

typedef struct MODBUSREAD_HANDLE_DATA_TAG
{
    THREAD_HANDLE threadHandle;
    LOCK_HANDLE lockHandle;
    int stopThread;
	BROKER_HANDLE broker;
	MODBUS_READ_CONFIG * config;

}MODBUSREAD_HANDLE_DATA;

#define MODBUS_MESSAGE "modbus read"
#define MODBUS_TCP_OFFSET 7
#define MODBUS_COM_OFFSET 1
#define MAX_TIME_STR 80
#define NUMOFBITS 8
#define MACSTRLEN 17
static char msgText[1024];

static bool isValidMac(char* mac)
{
	//format XX:XX:XX:XX:XX:XX
	bool ret = true;
	int len = strlen(mac);

	if (len != MACSTRLEN)
	{
		LogError("invalid mac length: %d", len);
		ret = false;
	}
	else
	{
		for (int mac_char = 0; mac_char < MACSTRLEN; mac_char++)
		{
			if (((mac_char + 1) % 3 == 0) )
			{
				if (mac[mac_char] != ':')
				{
					ret = false;
					break;
				}
			}
			else
			{
				if (!((mac[mac_char] >= '0' && mac[mac_char] <= '9') || (mac[mac_char] >= 'a' && mac[mac_char] <= 'f') || (mac[mac_char] >= 'A' && mac[mac_char] <= 'F')))
				{
					ret = false;
					break;
				}
			}
		}
	}
	return ret;
}

static bool isValidServer(char* server)
{
	//ipv4 format XXX.XXX.XXX.XXX
	//serial port format COMX
	bool ret = true;

	if (memcmp(server, "COM", 3) == 0)
	{
		if (0 > atoi(server + 3))
		{
			LogError("invalid COM port: %s", server);
			ret = false;
		}
	}
	else
	{
		if (inet_addr(server) == INADDR_NONE)
		{
			LogError("invalid ipv4: %s", server);
			ret = false;
		}
	}

	return ret;
}
static int get_crc(unsigned char * message, int length, unsigned short * out)//refer to J.J. Lee's code
{
	unsigned short crcFull = 0xFFFF;

	if (message == NULL || length <= 0)
		return -1;

	for (int _byte = 0; _byte < length; ++_byte)
	{
		crcFull = (unsigned short)(crcFull ^ message[_byte]);

		for (int _bit = 0; _bit < NUMOFBITS; ++_bit)
		{
			char crcLsb = (char)(crcFull & 0x0001);
			crcFull = (unsigned short)((crcFull >> 1) & 0x7FFF);
			if (crcLsb == 1) 
			{ 
				crcFull = (unsigned short)(crcFull ^ 0xA001); 
			}
		}
	}
	*out = crcFull;
	return 0;
}

static int send_request_com(MODBUS_READ_CONFIG * config, unsigned char * request, int request_len, unsigned char * response)
{
	int write_size;
	int read_size;
#ifdef WIN32
	if (!WriteFile(config->files, request, request_len, &write_size, NULL))//Additional Address+PDU+Error
	{
		LogError("write failed");
		return -1;
	}
	if (!ReadFile(config->files, response, 256, &read_size, NULL))
	{
		LogError("read failed");
		return -1;
	}
#else
	write_size = write(config->files, request, request_len);
	if (write_size != request_len)
	{
		LogError("write failed");
		return -1;
	}

	read_size = read(config->files, response, 255);
	if (read_size <= 0)
	{
		LogError("read failed");
		return -1;
	}
#endif
	if (response[MODBUS_COM_OFFSET] == (request[MODBUS_COM_OFFSET] + 128))
		return response[MODBUS_COM_OFFSET+1];
	return 0;
}

static int send_request_tcp(MODBUS_READ_CONFIG * config, unsigned char * request, int request_len, unsigned char * response)
{
	int recv_size;
	if (send(config->socks, request, request_len, 0) < 0)//MBAP+PDU
	{
		LogError("send failed");
		return -1;
	}
	if ((recv_size = recv(config->socks, response, 256, 0)) == SOCKET_ERROR)
	{
		LogError("recv failed");
		return -1;
	}
	if (response[MODBUS_TCP_OFFSET] == (request[MODBUS_TCP_OFFSET] + 128))
		return response[MODBUS_TCP_OFFSET+1];
	return 0;
}
static void encode_write_PDU(unsigned char * buf, unsigned short address, unsigned short value)
{
	unsigned short * _pU16;
	//encoding PDU
	buf[0] = address / 10000;  //function code
	_pU16 = (unsigned short *)(buf + 1);
	*_pU16 = htons((address % 10000) - 1);         //addr (2 bytes)
	_pU16 = (unsigned short *)(buf + 3);
	if (buf[0] == 5)//single coil
	{
		*_pU16 = htons((value == 1) ? 0XFF00 : 0);
	}
	else if (buf[0] == 6)//single register
	{
		*_pU16 = htons(value);
	}
}
static void encode_read_PDU(unsigned char * buf, MODBUS_READ_OPERATION * operation)
{
	unsigned short * _pU16;
	//encoding PDU
	buf[0] = operation->function_code;  //function code
	_pU16 = (unsigned short *)(buf + 1);
	*_pU16 = htons(operation->address);         //addr (2 bytes)
	_pU16 = (unsigned short *)(buf + 3);
	*_pU16 = htons(operation->length);         //length (2 bytes)
}
static void encode_MBAP(unsigned char * buf, int uid)
{
	unsigned short * _pU16;
	//encoding MBAP
	_pU16 = (unsigned short *)buf;
	*_pU16 = 0; //Transaction ID (2 bytes)
	buf[2] = 0;         //Protocol ID (2 bytes): 0 = MODBUS
	buf[3] = 0;
	_pU16 = (unsigned short *)(buf + 4);
	*_pU16 = htons(6);         //Length (2 bytes)
	buf[6] = uid;      //Unit ID (1 byte)
}
static int encode_write_request_tcp(unsigned char * buf, int * len, unsigned char uid, unsigned short address, unsigned short value)
{
	encode_MBAP(buf, uid);

	encode_write_PDU(buf + MODBUS_TCP_OFFSET, address, value);

	*len = 12;

	return 0;
}
static int encode_read_request_tcp(unsigned char * buf, int * len, MODBUS_READ_OPERATION * operation)
{
	encode_MBAP(buf, operation->unit_id);

	encode_read_PDU(buf + MODBUS_TCP_OFFSET, operation);

	*len = 12;

	return 0;
}
static int encode_write_request_com(unsigned char * buf, int * len, unsigned char uid, unsigned short address, unsigned short value)
{
	unsigned short * _pU16;
	unsigned short crc;
	int ret = 0;

	buf[0] = uid;      //Unit ID (1 byte)

	encode_write_PDU(buf + MODBUS_COM_OFFSET, address, value);

	if (get_crc(buf, 6, &crc) == -1)
		ret = -1;
	else 
	{
		_pU16 = (unsigned short *)(buf + 6);
		*_pU16 = crc;
		*len = 8;
	}

	return ret;
}
static int encode_read_request_com(unsigned char * buf, int * len, MODBUS_READ_OPERATION * operation)
{
	unsigned short * _pU16;
	unsigned short crc;
	int ret = 0;

	//encoding MBAP
	buf[0] = operation->unit_id;      //Unit ID (1 byte)

	encode_read_PDU(buf + MODBUS_COM_OFFSET, operation);

	if (get_crc(buf, 6, &crc) == -1)
		ret = -1;
	else
	{
		_pU16 = (unsigned short *)(buf + 6);
		*_pU16 = crc;
		*len = 8;
	}

	return ret;
}
static int decode_response_PDU(unsigned char * buf, MODBUS_READ_OPERATION* operation, int * offset)
{
	unsigned char byte_count = buf[1];
	unsigned char index = 0;
	unsigned short count;
	int step_size = 0;
	unsigned char start_digit;
	char temp[64];

	if (buf[0] == 1 || buf[0] == 2)//discrete input or coil status 1 bit
	{
		count = (byte_count * 8);
		count = (count > operation->length) ? operation->length : count;
		step_size = 1;
		start_digit = buf[0] - 1;
	}
	else if (buf[0] == 3 || buf[0] == 4)//register 16 bits
	{
		count = byte_count;
		step_size = 2;
		start_digit = (buf[0] == 3) ? 4 : 3;
	}
	else
		return -1;

	while (count > index)
	{
		memset(temp, 0, sizeof(temp));
		if (step_size == 1)
		{
			LogInfo("status %01X%04u: <%01X>\n", start_digit, operation->address + index + 1, (buf[2 + (index / 8)] >> (index % 8)) & 1);

			if (SNPRINTF_S(temp, sizeof(temp), "\"address_%01X%04u\": \"%01X\"",
				start_digit, operation->address + index + 1, (buf[2 + (index / 8)] >> (index % 8)) & 1) < 0)
			{
				LogError("Failed to set message text");
			}
		}
		else
		{
			LogInfo("register %01X%04u: <%02X%02X>\n", start_digit, operation->address + (index / 2) + 1, buf[2 + index], buf[3 + index]);

			if (SNPRINTF_S(temp, sizeof(temp), "\"address_%01X%04u\": \"%05u\"",
				start_digit, operation->address + (index / 2) + 1, buf[2 + index] * (0x100) + buf[3 + index]) < 0)
			{
				LogError("Failed to set message text");
			}
		}
		msgText[*offset] = ',';
		*offset += 1;
		SNPRINTF_S(msgText + *offset, sizeof(msgText) - *offset, "%s", temp);
		*offset += strlen(temp);
		index += step_size;
	}
	return 0;
}
static void decode_response_tcp(unsigned char * buf, MODBUS_READ_OPERATION* operation, int * offset)
{
	decode_response_PDU(buf + MODBUS_TCP_OFFSET, operation, offset);
}
static void decode_response_com(unsigned char * buf, MODBUS_READ_OPERATION* operation, int * offset)
{
	decode_response_PDU(buf + MODBUS_COM_OFFSET, operation, offset);
}
static void modbus_publish(BROKER_HANDLE broker, MODULE_HANDLE * handle, MESSAGE_CONFIG * msgConfig)
{
	msgConfig->size = strlen(msgText);
	MESSAGE_HANDLE modbusMessage = Message_Create(msgConfig);
	if (modbusMessage == NULL)
	{
		LogError("unable to create \"modbus read\" message");
	}
	else
	{
		(void)Broker_Publish(broker, handle, modbusMessage);
		Message_Destroy(modbusMessage);
	}
}
void close_server_tcp(MODBUS_READ_CONFIG * config)
{
	if (config->socks != INVALID_SOCKET)
	{
#ifdef WIN32
		closesocket(config->socks);
#else
		close(config->socks);
#endif
	}
}
void close_server_com(MODBUS_READ_CONFIG * config)
{
	if (config->files != INVALID_FILE)
	{
#ifdef WIN32
		CloseHandle(config->files);
#else
		close(config->files);
#endif
	}
}
void modbus_cleanup(MODBUS_READ_CONFIG * config)
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
		if (modbus_config->close_server_cb)
			modbus_config->close_server_cb(modbus_config);

		MODBUS_READ_CONFIG * temp_config = modbus_config;
		modbus_config = modbus_config->p_next;
		free(temp_config);
	}
#ifdef WIN32
		WSACleanup();
#endif
}
static int get_timestamp(char* timetemp)
{
	/*getting the time*/
	time_t temp = time(NULL);
	if (temp == (time_t)-1)
	{
		LogError("time function failed");
		return -1;
	}
	else
	{
		struct tm* t = localtime(&temp);
		if (t == NULL)
		{
			LogError("localtime failed");
			return -1;
		}
		else
		{
			if (strftime(timetemp, MAX_TIME_STR, "%x %r", t) == 0)
			{
				LogError("unable to strftime");
				return -1;
			}
		}
	}
	return 0;
}
static int process_operation(MODBUS_READ_CONFIG * config, MODBUS_READ_OPERATION * operation)
{
	unsigned char response[256];
	int request_len = 0;
	int offset;

	memset(msgText, 0, sizeof(msgText));
	msgText[0] = '{';


	char timetemp[MAX_TIME_STR] = { 0 };

	if (get_timestamp(timetemp) != 0)
		return -1;

	SNPRINTF_S(msgText + 1, sizeof(msgText) - 1, "\"DataTimestamp\": \"%s\",", timetemp);

	SNPRINTF_S(msgText + strlen(msgText), sizeof(msgText) - strlen(msgText), "\"mac_address\": \"%s\",", config->mac_address);

	SNPRINTF_S(msgText + strlen(msgText), sizeof(msgText) - strlen(msgText), "\"device_type\": \"%s\"", config->device_type);

	offset = strlen(msgText);

	MODBUS_READ_OPERATION * request_operation = operation;
	while (request_operation) 
	{
		int send_ret = -1;
		if (config->send_request_cb)
			send_ret = config->send_request_cb(config, request_operation->read_request, operation->read_request_len, response);
		if (send_ret == -1)
		{
			LogError("send request failed");
			return 1;
		}
		else if (send_ret > 0)
		{
			LogError("Exception occured, error code : %X\n", send_ret);
			return 1;
		}
		else
		{
			if (config->decode_response_cb)
				config->decode_response_cb(response, request_operation, &offset);
		}
		request_operation = request_operation->p_next;
	}
	msgText[offset] = '}';
	msgText[offset+1] = '\0';
	return 0;
}
static MODBUS_READ_CONFIG * get_config_by_mac(const char * mac_address, MODBUS_READ_CONFIG * config)
{
	MODBUS_READ_CONFIG * modbus_config = config;
	while (modbus_config)
	{
		if (strcmp(mac_address, modbus_config->mac_address) == 0)
			return modbus_config;
		modbus_config = modbus_config->p_next;
	}
	return NULL;
}
static SOCKET_TYPE connect_modbus_server_tcp(const char * server_ip)
{
	SOCKET_TYPE s;
	struct sockaddr_in server;

	if ((s = socket(AF_INET, SOCK_STREAM, 0)) == INVALID_SOCKET)
	{
		LogError("Could not create socket");
	}
	else
	{

		server.sin_addr.s_addr = inet_addr(server_ip);
		server.sin_family = AF_INET;
		server.sin_port = htons(502);
		if (connect(s, (struct sockaddr *)&server, sizeof(server)) < 0)
		{
			LogError("connect error");
			s = INVALID_SOCKET;
		}
	}

	return s;
}
static FILE_TYPE connect_modbus_server_com(int port)
{
	FILE_TYPE f;
	char file[32];
	
#ifdef WIN32
	if (port >= 10)
	{
		SNPRINTF_S(file, 32, "\\\\.\\COM%d", port);
	}
	else
	{
		SNPRINTF_S(file, 32, "COM%d", port);
	}
	f = CreateFile(file, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
#else
	SNPRINTF_S(file, 32, "/dev/ttyS%d", port - 1);
	f = open(file, O_RDWR | O_NOCTTY);
#endif

	return f;
}
static int modbusReadThread(void *param)
{
    MODBUSREAD_HANDLE_DATA* handleData = param;
    MESSAGE_CONFIG msgConfig;

	MODBUS_READ_CONFIG * server_config = handleData->config;

#ifdef WIN32
	WSADATA wsa;
	if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
	{
		LogError("Failed. Error Code : %d", WSAGetLastError());
		return INVALID_SOCKET;
	}
#endif

	while (server_config)
	{
		server_config->files = INVALID_FILE;
		server_config->socks = INVALID_SOCKET;
		//connect to server
		if (memcmp(server_config->server_str, "COM", 3) == 0)
		{
			server_config->files = connect_modbus_server_com(atoi(server_config->server_str + 3));
			if (server_config->files == INVALID_FILE)
			{
				LogError("unable to connect to modbus server %s", server_config->server_str);
				return 1;
			}
			server_config->encode_read_cb = (int*)encode_read_request_com;
			server_config->encode_write_cb = (int*)encode_write_request_com;
			server_config->decode_response_cb = (int*)decode_response_com;
			server_config->send_request_cb = (int*)send_request_com;
			server_config->close_server_cb = (void*)close_server_com;

		}
		else
		{
			server_config->socks = connect_modbus_server_tcp(server_config->server_str);

			if (server_config->socks == INVALID_SOCKET)
			{
				LogError("unable to connect to modbus server %s", server_config->server_str);
				return 1;
			}
			server_config->encode_read_cb = (int*)encode_read_request_tcp;
			server_config->encode_write_cb = (int*)encode_write_request_tcp;
			server_config->decode_response_cb = (int*)decode_response_tcp;
			server_config->send_request_cb = (int*)send_request_tcp;
			server_config->close_server_cb = (void*)close_server_tcp;

		}
		MODBUS_READ_OPERATION * request_operation = server_config->p_operation;
		while (request_operation)
		{
			if (server_config->encode_read_cb)
				server_config->encode_read_cb(request_operation->read_request, &(request_operation->read_request_len), request_operation);
			request_operation = request_operation->p_next;
		}
		//check mac
		server_config = server_config->p_next;
	}

    MAP_HANDLE propertiesMap = Map_Create(NULL);
    if(propertiesMap == NULL)
    {
        LogError("unable to create a Map");
    }
    else
    {
        if (Map_AddOrUpdate(propertiesMap, "modbusRead", "from Azure IoT Gateway SDK simple sample!") != MAP_OK )
		{
			LogError("Could not attach modbusRead property to message");
		} 
		else if (Map_AddOrUpdate(propertiesMap, "source", "modbus") != MAP_OK)
        {
            LogError("Could not attach source property to message");
        }
        else
        {
			msgConfig.source = (const unsigned char *)msgText;
			msgConfig.sourceProperties = propertiesMap;


            while (1)
            {
                if (Lock(handleData->lockHandle) == LOCK_OK)
                {
                    if (handleData->stopThread)
                    {
                        (void)Unlock(handleData->lockHandle);
                        break; /*gets out of the thread*/
                    }
                    else
                    {
						server_config = handleData->config;
						while (server_config)
						{
							if ((server_config->time_check) * 1000 >= server_config->read_interval)
							{
								server_config->time_check = 0;
								if (process_operation(server_config, server_config->p_operation) != 0)
								{
									LogError("unable to send request to modbus server %s", server_config->server_str);
								}
								else
								{
									if (Map_AddOrUpdate(propertiesMap, "macAddress", (const char *)server_config->mac_address) != MAP_OK)
									{
										LogError("Could not attach macAddress property to message");
									}
									else
									{
										modbus_publish(handleData->broker, (MODULE_HANDLE *)param, &msgConfig);
									}
								}
							}
							else
							{
								server_config->time_check++;
							}

							server_config = server_config->p_next;
						}
						(void)Unlock(handleData->lockHandle);
                    }
                }
                else
                {
                    /*shall retry*/
                }
                (void)ThreadAPI_Sleep(1000);
            }
        }
    }
    return 0;
}

static MODULE_HANDLE ModbusRead_Create(BROKER_HANDLE broker, const void* configuration)
{
	bool isValidConfig = true;
    MODBUSREAD_HANDLE_DATA* result = NULL;
    if (
        (broker == NULL || configuration == NULL)
        )
    {

		/*Codes_SRS_MODBUS_READ_99_001: [If broker is NULL then ModbusRead_Create shall fail and return NULL.]*/
		/*Codes_SRS_MODBUS_READ_99_002 : [If configuration is NULL then ModbusRead_Create shall fail and return NULL.]*/
		LogError("invalid arg broker=%p configuration=%p", broker, configuration);
    }
    else
    {
		/* validate mac_address, server_str*/

		MODBUS_READ_CONFIG *cur_config = (MODBUS_READ_CONFIG *)configuration;

		while (cur_config)
		{
			if (!isValidMac(cur_config->mac_address))
			{
				LogError("invalid mac: %s", cur_config->mac_address);
				isValidConfig = false;
				break;
			}
			if (!isValidServer(cur_config->server_str))
			{
				LogError("invalid server: %s", cur_config->server_str);
				isValidConfig = false;
				break;
			}
			cur_config = cur_config->p_next;
		}

		if (isValidConfig)
		{
			result = malloc(sizeof(MODBUSREAD_HANDLE_DATA));
			if (result == NULL)
			{
				/*Codes_SRS_MODBUS_READ_99_007: [If ModbusRead_Create encounters any errors while creating the MODBUSREAD_HANDLE_DATA then it shall fail and return NULL.]*/
				LogError("unable to malloc");
			}
			else
			{
				result->lockHandle = Lock_Init();
				if (result->lockHandle == NULL)
				{
					LogError("unable to Lock_Init");
					free(result);
					result = NULL;
				}
				else
				{
					result->stopThread = 0;
					result->broker = broker;
					result->config = (MODBUS_READ_CONFIG *)configuration;
					if (ThreadAPI_Create(&result->threadHandle, modbusReadThread, result) != THREADAPI_OK)
					{
						LogError("failed to spawn a thread");
						(void)Lock_Deinit(result->lockHandle);
						free(result);
						result = NULL;
					}
					else
					{
						/*all is fine apparently*/
					}
					/*Codes_SRS_MODBUS_READ_99_008: [Otherwise ModbusRead_Create shall return a non - NULL pointer.]*/
				}
			}
		}
	}
    return result;
}

static void ModbusRead_Destroy(MODULE_HANDLE module)
{
	/*Codes_SRS_MODBUS_READ_99_014: [If moduleHandle is NULL then ModbusRead_Destroy shall return.]*/
	if (module != NULL)
	{
		/*first stop the thread*/
		MODBUSREAD_HANDLE_DATA* handleData = module;
		int notUsed;
		if (Lock(handleData->lockHandle) != LOCK_OK)
		{
			LogError("not able to Lock, still setting the thread to finish");
			handleData->stopThread = 1;
		}
		else
		{
			handleData->stopThread = 1;
			Unlock(handleData->lockHandle);
		}

		/*Codes_SRS_MODBUS_READ_99_015 : [Otherwise ModbusRead_Destroy shall unuse all used resources.]*/
		if (ThreadAPI_Join(handleData->threadHandle, &notUsed) != THREADAPI_OK)
		{
			LogError("unable to ThreadAPI_Join, still proceeding in _Destroy");
		}

		(void)Lock_Deinit(handleData->lockHandle);
		modbus_cleanup(handleData->config);
		free(handleData);
	}
}

static void ModbusRead_Receive(MODULE_HANDLE moduleHandle, MESSAGE_HANDLE messageHandle)
{
	if (moduleHandle == NULL || messageHandle == NULL)
	{
		/*Codes_SRS_MODBUS_READ_99_009: [If moduleHandle is NULL then ModbusRead_Receive shall fail and return.]*/
		/*Codes_SRS_MODBUS_READ_99_010 : [If messageHandle is NULL then ModbusRead_Receive shall fail and return.]*/
		LogError("Received NULL arguments: module = %p, massage = %p", moduleHandle, messageHandle);
	}
	else
	{
		MODBUSREAD_HANDLE_DATA* handleData = moduleHandle;
		CONSTMAP_HANDLE properties = Message_GetProperties(messageHandle);

		/*Codes_SRS_MODBUS_READ_99_011: [If `messageHandle` properties does not contain a "source" property, then ModbusRead_Receive shall fail and return.]*/
		/*Codes_SRS_MODBUS_READ_99_012 : [If `messageHandle` properties contains a "deviceKey" property, then ModbusRead_Receive shall fail and return.]*/
		/*Codes_SRS_MODBUS_READ_99_013 : [If `messageHandle` properties contains a "source" property that is set to "mapping", then ModbusRead_Receive shall fail and return.]*/
		const char * source = ConstMap_GetValue(properties, "source");
		if (source != NULL)
		{
			if (strcmp(source, "mapping") == 0 && !ConstMap_ContainsKey(properties, "deviceKey"))
			{
				const char * mac_address = ConstMap_GetValue(properties, "macAddress");
				MODBUS_READ_CONFIG * modbus_config = get_config_by_mac(mac_address, handleData->config);
				if (modbus_config != NULL)
				{
					unsigned short address; // function code + address
					unsigned short value;
					unsigned char uid;
					const CONSTBUFFER * content = Message_GetContent(messageHandle); /*by contract, this is never NULL*/
					JSON_Value* json = json_parse_string((const char*)content->buffer);
					if (json == NULL)
					{
						/*Codes_SRS_MODBUS_READ_99_018 : [If the content of messageHandle is not a JSON value, then `ModbusRead_Receive` shall fail and return NULL.]*/
						LogError("unable to json_parse_string");
					}
					else
					{
						JSON_Object * obj = json_value_get_object(json);
						if (obj == NULL)
						{
							LogError("json_value_get_obj failed");
						}
						else
						{
							//expect a JSON format writeback request
							address = atoi(json_object_get_string(obj, "address"));
							value = atoi(json_object_get_string(obj, "value"));
							uid = atoi(json_object_get_string(obj, "uid"));
							LogInfo("WriteBack to address: %hu, value: %hu, uid: %hhu recived\n", address, value, uid);

							unsigned char request[256];
							unsigned char response[256];
							int request_len = 0;

							if (modbus_config->encode_write_cb)
								modbus_config->encode_write_cb(request, &request_len, uid, address, value);

							while (Lock(handleData->lockHandle) != LOCK_OK)
							{
								(void)ThreadAPI_Sleep(100);
							}
							int send_ret = -1;

							if (modbus_config->send_request_cb)
								send_ret = modbus_config->send_request_cb(modbus_config, request, request_len, response);

							(void)Unlock(handleData->lockHandle);
							if (send_ret == -1)
							{
								LogError("unable to send request to modbus server");
							}
							else if (send_ret > 0)
							{
								LogError("Exception occured, error code : %X\n", send_ret);
							}
						}
					}
				}
				else
				{
					LogError("Did not find device mac_address [%s] of current message", mac_address);
				}
			}
		}
		ConstMap_Destroy(properties);
	}
		/*Codes_SRS_MODBUS_READ_99_017 : [ModbusRead_Receive shall return.]*/
}

static const MODULE_APIS ModbusRead_APIS_all =
{
	ModbusRead_Create,
	ModbusRead_Destroy,
	ModbusRead_Receive
};

#ifdef BUILD_MODULE_TYPE_STATIC
MODULE_EXPORT void MODULE_STATIC_GETAPIS(MODBUSREAD_MODULE)(MODULE_APIS* apis)
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
		/* Codes_SRS_MODBUS_READ_99_016: [ Module_GetAPIS shall fill the provided MODULE_APIS structure with the required function pointers. ] */
		(*apis) = ModbusRead_APIS_all;
	}
}
