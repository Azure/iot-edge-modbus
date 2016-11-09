// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef MODBUS_READ_COMMON_H
#define MODBUS_READ_COMMON_H

#include "parson.h"

#ifdef WIN32

#define _WINSOCK_DEPRECATED_NO_WARNINGS
#include <winsock2.h>
#pragma comment(lib,"ws2_32.lib") //Winsock Library
#define SOCKET_TYPE SOCKET
#define FILE_TYPE HANDLE
#define INVALID_FILE INVALID_HANDLE_VALUE
#define SNPRINTF_S sprintf_s

#else

#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <unistd.h>
#define SOCKET_TYPE int
#define FILE_TYPE int
#define SOCKET_ERROR (-1)
#define INVALID_SOCKET (~0)
#define INVALID_FILE -1
#define SNPRINTF_S snprintf
#endif

/*the below data structures are used by all versions of ModbusRead (static/dynamic vanilla/hl)*/
typedef struct MODBUS_READ_OPERATION_TAG
{
	struct MODBUS_READ_OPERATION_TAG * p_next;
	unsigned char unit_id;
	unsigned char function_code;
	unsigned short address;
	unsigned short length;
	unsigned char read_request[256];
	int read_request_len;
}MODBUS_READ_OPERATION;

typedef struct MODBUS_READ_CONFIG_TAG
{
	struct MODBUS_READ_CONFIG_TAG * p_next;
	MODBUS_READ_OPERATION * p_operation;
	size_t read_interval;
	char server_str[16];
	char mac_address[18];
	char device_type[64];
	SOCKET_TYPE socks;
	FILE_TYPE files;
	size_t time_check;
	int(*encode_read_cb)(void*, void*, void*);
	int(*encode_write_cb)(void*, void*, unsigned char, unsigned short, unsigned short);
	int(*decode_response_cb)(void*, void*, void*);
	int(*send_request_cb)(struct MODBUS_READ_CONFIG_TAG *, unsigned char*, int, unsigned char*);
	void(*close_server_cb)(struct MODBUS_READ_CONFIG_TAG *);
}MODBUS_READ_CONFIG; /*this needs to be passed to the Module_Create function*/

#endif /*MODBUS_READ_COMMON_H*/
