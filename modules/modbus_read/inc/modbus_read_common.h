// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef MODBUS_READ_COMMON_H
#define MODBUS_READ_COMMON_H

#include "parson.h"
#define SOCKET_CLOSED (0)

#ifdef WIN32

#define _WINSOCK_DEPRECATED_NO_WARNINGS
#include <winsock2.h>
#pragma comment(lib,"ws2_32.lib") //Winsock Library
#define SOCKET_TYPE SOCKET
#define FILE_TYPE HANDLE
#define INVALID_FILE INVALID_HANDLE_VALUE
#define SNPRINTF_S sprintf_s

#else

#include <sys/termios.h>
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

typedef struct MODBUS_READ_CONFIG_TAG MODBUS_READ_CONFIG;
typedef struct MODBUS_READ_OPERATION_TAG MODBUS_READ_OPERATION;

typedef int(*encode_read_cb_type)(void*, void*, void*);
typedef int(*encode_write_cb_type)(void*, void*, unsigned char, unsigned char, unsigned short, unsigned short);
typedef int(*decode_response_cb_type)(void*, void*);
typedef int(*send_request_cb_type)(MODBUS_READ_CONFIG *, unsigned char*, int, unsigned char*);
typedef void(*close_server_cb_type)(MODBUS_READ_CONFIG *);

//baud
#define CONFIG_BAUD_9600 9600
//stop bits
#define CONFIG_STOP_ONE 0
#define CONFIG_STOP_ONE5 1
#define CONFIG_STOP_TWO 2
//data bits
#define CONFIG_DATA_8 8
//parity
#define CONFIG_PARITY_NO 0
#define CONFIG_PARITY_ODD 1
#define CONFIG_PARITY_EVEN 2
#define CONFIG_PARITY_MARK 3
#define CONFIG_PARITY_SPACE 4
//flow control
#define CONFIG_FLOW_CONTROL_NONE 0
#define CONFIG_FLOW_CONTROL_XONOFF 1
#define CONFIG_FLOW_CONTROL_RTSCTS 2
#define CONFIG_FLOW_CONTROL_DSRDTR 3

struct MODBUS_READ_OPERATION_TAG
{
    MODBUS_READ_OPERATION * p_next;
    unsigned char unit_id;
    unsigned char function_code;
    unsigned short address;
    unsigned short length;
    unsigned char read_request[256];
    int read_request_len;
};

struct MODBUS_READ_CONFIG_TAG
{
    MODBUS_READ_CONFIG * p_next;
    MODBUS_READ_OPERATION * p_operation;
    size_t read_interval;
    char server_str[16];
    char mac_address[18];
    char device_type[64];
	int sqlite_enabled;
    SOCKET_TYPE socks;
    FILE_TYPE files;
    size_t time_check;
	unsigned int baud_rate;
	unsigned char stop_bits;
	unsigned char data_bits;
	unsigned char parity;
	unsigned char flow_control;
    encode_read_cb_type encode_read_cb;
    encode_write_cb_type encode_write_cb;
    decode_response_cb_type decode_response_cb;
    send_request_cb_type send_request_cb;
    close_server_cb_type close_server_cb;
}; /*this needs to be passed to the Module_Create function*/

#endif /*MODBUS_READ_COMMON_H*/
