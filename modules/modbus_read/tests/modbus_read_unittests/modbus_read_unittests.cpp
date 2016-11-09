// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <cstdlib>
#ifdef _CRTDBG_MAP_ALLOC
#include <crtdbg.h>
#endif

#include "testrunnerswitcher.h"
#include "micromock.h"
#include "micromockcharstararenullterminatedstrings.h"
#include "azure_c_shared_utility/lock.h"
#include "azure_c_shared_utility/constmap.h"
#include "azure_c_shared_utility/map.h"
#include "message.h"
#include "azure_c_shared_utility/threadapi.h"
#include "messageproperties.h"

#include "parson.h"

#include "modbus_read.h"

static CONSTBUFFER messageContent;

class RefCountObject
{
private:
	size_t ref_count;

public:
	RefCountObject() : ref_count(1)
	{
	}

	size_t inc_ref()
	{
		return ++ref_count;
	}

	void dec_ref()
	{
		if (--ref_count == 0)
		{
			delete this;
		}
	}
};

#define VALID_MAP_HANDLE	0xDEAF
#define VALID_VALUE			"value"
static MAP_RESULT currentMapResult;

static BROKER_RESULT currentBrokerResult;

static MICROMOCK_MUTEX_HANDLE g_testByTest;
static MICROMOCK_GLOBAL_SEMAPHORE_HANDLE g_dllByDll;

/*these are simple cached variables*/
static pfModule_Create Module_Create = NULL; /*gets assigned in TEST_SUITE_INITIALIZE*/
static pfModule_Destroy Module_Destroy = NULL; /*gets assigned in TEST_SUITE_INITIALIZE*/
static pfModule_Receive Module_Receive = NULL; /*gets assigned in TEST_SUITE_INITIALIZE*/

#define GBALLOC_H

extern "C" int gballoc_init(void);
extern "C" void gballoc_deinit(void);
extern "C" void* gballoc_malloc(size_t size);
extern "C" void* gballoc_calloc(size_t nmemb, size_t size);
extern "C" void* gballoc_realloc(void* ptr, size_t size);
extern "C" void gballoc_free(void* ptr);

extern "C" int mallocAndStrcpy_s(char** destination, const char*source);
extern "C" int unsignedIntToString(char* destination, size_t destinationSize, unsigned int value);
extern "C" int size_tToString(char* destination, size_t destinationSize, size_t value);


namespace BASEIMPLEMENTATION
{
    /*if malloc is defined as gballoc_malloc at this moment, there'd be serious trouble*/
#define Lock(x) (LOCK_OK + gballocState - gballocState) /*compiler warning about constant in if condition*/
#define Unlock(x) (LOCK_OK + gballocState - gballocState)
#define Lock_Init() (LOCK_HANDLE)0x42
#define Lock_Deinit(x) (LOCK_OK + gballocState - gballocState)
#include "gballoc.c"
	
#undef Lock
#undef Unlock
#undef Lock_Init
#undef Lock_Deinit
	
};


typedef struct json_value_t
{
	int fake;
} JSON_Value;

typedef struct json_object_t
{
	int fake;
} JSON_Object;


static MESSAGE_HANDLE validMessageHandle = (MESSAGE_HANDLE)0x032;
static unsigned char buffer[3] = { 1,2,3 };
static CONSTBUFFER validBuffer = { buffer, sizeof(buffer) / sizeof(buffer[0]) };
//CONSTMAP GetValue mocks
static const char* macAddressProperties;
static const char* sourceProperties;
static const char* deviceNameProperties;
static const char* deviceKeyProperties;

TYPED_MOCK_CLASS(CModbusreadMocks, CGlobalMock)
	{
	public:
		//gballoc
		MOCK_STATIC_METHOD_1(, void*, gballoc_malloc, size_t, size)
			void* result2 = BASEIMPLEMENTATION::gballoc_malloc(size);
		MOCK_METHOD_END(void*, result2);

		MOCK_STATIC_METHOD_1(, void, gballoc_free, void*, ptr)
			BASEIMPLEMENTATION::gballoc_free(ptr);
		MOCK_VOID_METHOD_END()

		// Parson Mocks 
		MOCK_STATIC_METHOD_1(, JSON_Value*, json_parse_string, const char *, parseString)
			JSON_Value* value = NULL;
		if (parseString != NULL)
		{
			value = (JSON_Value*)malloc(1);
		}
		MOCK_METHOD_END(JSON_Value*, value);

		MOCK_STATIC_METHOD_1(, JSON_Object*, json_value_get_object, const JSON_Value*, value)
			JSON_Object* object = NULL;
		if (value != NULL)
		{
			object = (JSON_Object*)0x42;
		}
		MOCK_METHOD_END(JSON_Object*, object);

		MOCK_STATIC_METHOD_2(, const char*, json_object_get_string, const JSON_Object*, object, const char*, name)
			const char * result2;
		if (strcmp(name, "address") == 0)
		{
			result2 = "0000";
		}
		else if (strcmp(name, "uid") == 0)
		{
			result2 = "unit";
		}
		else if (strcmp(name, "value") == 0)
		{
			result2 = "value";
		}
		else
		{
			result2 = NULL;
		}
		MOCK_METHOD_END(const char*, result2);

		MOCK_STATIC_METHOD_1(, void, json_value_free, JSON_Value*, value)
			free(value);
		MOCK_VOID_METHOD_END();

		// Broker mocks
		MOCK_STATIC_METHOD_0(, BROKER_HANDLE, Broker_Create)
			BROKER_HANDLE busResult = (BROKER_HANDLE)(new RefCountObject());
		MOCK_METHOD_END(BROKER_HANDLE, busResult)

			MOCK_STATIC_METHOD_1(, void, Broker_Destroy, BROKER_HANDLE, bus)
			((RefCountObject*)bus)->dec_ref();
		MOCK_VOID_METHOD_END()

			MOCK_STATIC_METHOD_3(, BROKER_RESULT, Broker_Publish, BROKER_HANDLE, bus, MODULE_HANDLE, handle, MESSAGE_HANDLE, message)
			BROKER_RESULT brokerResult = currentBrokerResult;
		MOCK_METHOD_END(BROKER_RESULT, brokerResult)

			// ConstMap mocks
			MOCK_STATIC_METHOD_1(, CONSTMAP_HANDLE, ConstMap_Create, MAP_HANDLE, sourceMap)
			CONSTMAP_HANDLE result2;
			result2 = (CONSTMAP_HANDLE)(new RefCountObject());
		MOCK_METHOD_END(CONSTMAP_HANDLE, result2)

			MOCK_STATIC_METHOD_1(, CONSTMAP_HANDLE, ConstMap_Clone, CONSTMAP_HANDLE, handle)
			CONSTMAP_HANDLE result3;
			result3 = handle;
			((RefCountObject*)handle)->inc_ref();
		MOCK_METHOD_END(CONSTMAP_HANDLE, result3)

			MOCK_STATIC_METHOD_1(, MAP_HANDLE, ConstMap_CloneWriteable, CONSTMAP_HANDLE, handle)
			MAP_HANDLE result4;
			result4 = (MAP_HANDLE)(new RefCountObject());
		MOCK_METHOD_END(MAP_HANDLE, result4)

			MOCK_STATIC_METHOD_1(, void, ConstMap_Destroy, CONSTMAP_HANDLE, map)
			((RefCountObject*)map)->dec_ref();
		MOCK_VOID_METHOD_END()

			MOCK_STATIC_METHOD_2(, const char*, ConstMap_GetValue, CONSTMAP_HANDLE, handle, const char*, key)
			const char * result5 = VALID_VALUE;
		if (strcmp(GW_MAC_ADDRESS_PROPERTY, key) == 0)
		{
			result5 = "01:01:01:01:01:01";
		}
		else if (strcmp(GW_SOURCE_PROPERTY, key) == 0)
		{
			result5 = "mapping";
		}
		MOCK_METHOD_END(const char *, result5)

			MOCK_STATIC_METHOD_2(, bool, ConstMap_ContainsKey, CONSTMAP_HANDLE, handle, const char*, key)
			bool result5 = false;
		if (strcmp("deviceKey", key) == 0)
		{
			result5 = true;
		}
		MOCK_METHOD_END(bool, result5)
			
			// CONSTBUFFER mocks.
			MOCK_STATIC_METHOD_2(, CONSTBUFFER_HANDLE, CONSTBUFFER_Create, const unsigned char*, source, size_t, size)
			CONSTBUFFER_HANDLE result1;
			result1 = (CONSTBUFFER_HANDLE)(new RefCountObject());
		MOCK_METHOD_END(CONSTBUFFER_HANDLE, result1)

			MOCK_STATIC_METHOD_1(, CONSTBUFFER_HANDLE, CONSTBUFFER_Clone, CONSTBUFFER_HANDLE, constbufferHandle)
			CONSTBUFFER_HANDLE result2;
			result2 = constbufferHandle;
			((RefCountObject*)constbufferHandle)->inc_ref();
		MOCK_METHOD_END(CONSTBUFFER_HANDLE, result2)

			MOCK_STATIC_METHOD_1(, void, CONSTBUFFER_Destroy, CONSTBUFFER_HANDLE, constbufferHandle)
			((RefCountObject*)constbufferHandle)->dec_ref();
		MOCK_VOID_METHOD_END()


			// Map related

			MOCK_STATIC_METHOD_1(, MAP_HANDLE, Map_Create, MAP_FILTER_CALLBACK, mapFilterFunc)
			MAP_HANDLE result1;
		result1 = (MAP_HANDLE)(new RefCountObject());
		MOCK_METHOD_END(MAP_HANDLE, result1)
			// Map_Clone
			MOCK_STATIC_METHOD_1(, MAP_HANDLE, Map_Clone, MAP_HANDLE, sourceMap)
			((RefCountObject*)sourceMap)->inc_ref();
		MOCK_METHOD_END(MAP_HANDLE, sourceMap)

			// Map_Destroy
			MOCK_STATIC_METHOD_1(, void, Map_Destroy, MAP_HANDLE, ptr)
			((RefCountObject*)ptr)->dec_ref();
		MOCK_VOID_METHOD_END()

			// Map_AddOrUpdate
			MOCK_STATIC_METHOD_3(, MAP_RESULT, Map_AddOrUpdate, MAP_HANDLE, handle, const char*, key, const char*, value)
			MAP_RESULT result7;
			result7 = MAP_OK;
		MOCK_METHOD_END(MAP_RESULT, result7)

			// Message
			MOCK_STATIC_METHOD_1(, MESSAGE_HANDLE, Message_Create, const MESSAGE_CONFIG*, cfg)
			MESSAGE_HANDLE result2 = (MESSAGE_HANDLE)(new RefCountObject());
		MOCK_METHOD_END(MESSAGE_HANDLE, result2)

			MOCK_STATIC_METHOD_1(, MESSAGE_HANDLE, Message_CreateFromBuffer, const MESSAGE_BUFFER_CONFIG*, cfg)
			MESSAGE_HANDLE result1;
			result1 = (MESSAGE_HANDLE)(new RefCountObject());
		MOCK_METHOD_END(MESSAGE_HANDLE, result1)

			MOCK_STATIC_METHOD_1(, MESSAGE_HANDLE, Message_Clone, MESSAGE_HANDLE, message)
			((RefCountObject*)message)->inc_ref();
		MOCK_METHOD_END(MESSAGE_HANDLE, message)

			MOCK_STATIC_METHOD_1(, CONSTMAP_HANDLE, Message_GetProperties, MESSAGE_HANDLE, message)
			CONSTMAP_HANDLE result1;
			result1 = ConstMap_Create((MAP_HANDLE)VALID_MAP_HANDLE);
		MOCK_METHOD_END(CONSTMAP_HANDLE, result1)

			MOCK_STATIC_METHOD_1(, const CONSTBUFFER*, Message_GetContent, MESSAGE_HANDLE, message)
			CONSTBUFFER* result1 = &messageContent;
		MOCK_METHOD_END(const CONSTBUFFER*, result1)

			MOCK_STATIC_METHOD_1(, CONSTBUFFER_HANDLE, Message_GetContentHandle, MESSAGE_HANDLE, message)
			CONSTBUFFER_HANDLE result1;
			result1 = CONSTBUFFER_Create(NULL, 0);
		MOCK_METHOD_END(CONSTBUFFER_HANDLE, result1)

			MOCK_STATIC_METHOD_1(, void, Message_Destroy, MESSAGE_HANDLE, message)
			((RefCountObject*)message)->dec_ref();
		MOCK_VOID_METHOD_END()

			//thread

			MOCK_STATIC_METHOD_3( , THREADAPI_RESULT, ThreadAPI_Create, THREAD_HANDLE*, threadHandle, THREAD_START_FUNC, func, void*, arg)
		THREADAPI_RESULT result8 = THREADAPI_OK;
		MOCK_METHOD_END(THREADAPI_RESULT, result8)

			MOCK_STATIC_METHOD_2( , THREADAPI_RESULT, ThreadAPI_Join, THREAD_HANDLE, threadHandle, int*, res)
		THREADAPI_RESULT result8;
		if (threadHandle == NULL)
			result8 = THREADAPI_ERROR;
		else
			result8 = THREADAPI_OK;
		MOCK_METHOD_END(THREADAPI_RESULT, result8)

			MOCK_STATIC_METHOD_1( , void, ThreadAPI_Sleep, unsigned int, milliseconds);
		MOCK_VOID_METHOD_END()

			//lock
			MOCK_STATIC_METHOD_0(, LOCK_HANDLE, Lock_Init)
		LOCK_HANDLE result9 = (LOCK_HANDLE)0x43;
		MOCK_METHOD_END(LOCK_HANDLE, result9)

			MOCK_STATIC_METHOD_1( , LOCK_RESULT, Lock, LOCK_HANDLE, handle)
		LOCK_RESULT result10;
		if (handle == NULL)
			result10 = LOCK_ERROR;
		else
			result10 = LOCK_OK;
		MOCK_METHOD_END(LOCK_RESULT, result10)

		MOCK_STATIC_METHOD_1( , LOCK_RESULT, Unlock, LOCK_HANDLE, handle)
		LOCK_RESULT result10;
		if (handle == NULL)
			result10 = LOCK_ERROR;
		else
			result10 = LOCK_OK;
		MOCK_METHOD_END(LOCK_RESULT, result10)

		MOCK_STATIC_METHOD_1( , LOCK_RESULT, Lock_Deinit, LOCK_HANDLE, handle)
		LOCK_RESULT result10;
		if (handle == NULL)
			result10 = LOCK_ERROR;
		else
			result10 = LOCK_OK;
		MOCK_METHOD_END(LOCK_RESULT, result10)
	};


DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , void*, gballoc_malloc, size_t, size);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , void, gballoc_free, void*, ptr);

DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , JSON_Value*, json_parse_string, const char *, filename);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , JSON_Object*, json_value_get_object, const JSON_Value*, value);
DECLARE_GLOBAL_MOCK_METHOD_2(CModbusreadMocks, , const char*, json_object_get_string, const JSON_Object*, object, const char*, name);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , void, json_value_free, JSON_Value*, value);

DECLARE_GLOBAL_MOCK_METHOD_0(CModbusreadMocks, , BROKER_HANDLE, Broker_Create);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , void, Broker_Destroy, BROKER_HANDLE, bus);
DECLARE_GLOBAL_MOCK_METHOD_3(CModbusreadMocks, , BROKER_RESULT, Broker_Publish, BROKER_HANDLE, bus, MODULE_HANDLE, handle, MESSAGE_HANDLE, message);

DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , CONSTMAP_HANDLE, ConstMap_Create, MAP_HANDLE, sourceMap);
DECLARE_GLOBAL_MOCK_METHOD_2(CModbusreadMocks, , bool, ConstMap_ContainsKey, CONSTMAP_HANDLE, handle, const char *, key);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , CONSTMAP_HANDLE, ConstMap_Clone, CONSTMAP_HANDLE, handle);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , void, ConstMap_Destroy, CONSTMAP_HANDLE, map);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , MAP_HANDLE, ConstMap_CloneWriteable, CONSTMAP_HANDLE, handle);
DECLARE_GLOBAL_MOCK_METHOD_2(CModbusreadMocks, , const char*, ConstMap_GetValue, CONSTMAP_HANDLE, handle, const char *, key);

DECLARE_GLOBAL_MOCK_METHOD_2(CModbusreadMocks, , CONSTBUFFER_HANDLE, CONSTBUFFER_Create, const unsigned char*, source, size_t, size);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , CONSTBUFFER_HANDLE, CONSTBUFFER_Clone, CONSTBUFFER_HANDLE, constbufferHandle);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , void, CONSTBUFFER_Destroy, CONSTBUFFER_HANDLE, constbufferHandle);

DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , MAP_HANDLE, Map_Create, MAP_FILTER_CALLBACK, mapFilterFunc);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , MAP_HANDLE, Map_Clone, MAP_HANDLE, sourceMap);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , void, Map_Destroy, MAP_HANDLE, ptr);
DECLARE_GLOBAL_MOCK_METHOD_3(CModbusreadMocks, , MAP_RESULT, Map_AddOrUpdate, MAP_HANDLE, handle, const char*, key, const char*, value);

DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , MESSAGE_HANDLE, Message_Create, const MESSAGE_CONFIG*, cfg);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , MESSAGE_HANDLE, Message_CreateFromBuffer, const MESSAGE_BUFFER_CONFIG*, cfg);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , MESSAGE_HANDLE, Message_Clone, MESSAGE_HANDLE, message);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , CONSTMAP_HANDLE, Message_GetProperties, MESSAGE_HANDLE, message);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , const CONSTBUFFER*, Message_GetContent, MESSAGE_HANDLE, message);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , CONSTBUFFER_HANDLE, Message_GetContentHandle, MESSAGE_HANDLE, message);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , void, Message_Destroy, MESSAGE_HANDLE, message);

DECLARE_GLOBAL_MOCK_METHOD_3(CModbusreadMocks, , THREADAPI_RESULT, ThreadAPI_Create, THREAD_HANDLE*, threadHandle, THREAD_START_FUNC, func, void*, arg);
DECLARE_GLOBAL_MOCK_METHOD_2(CModbusreadMocks, , THREADAPI_RESULT, ThreadAPI_Join, THREAD_HANDLE, threadHandle, int*, res);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , void ,ThreadAPI_Sleep, unsigned int, milliseconds);

DECLARE_GLOBAL_MOCK_METHOD_0(CModbusreadMocks, , LOCK_HANDLE, Lock_Init);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , LOCK_RESULT, Lock, LOCK_HANDLE,  handle);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , LOCK_RESULT, Unlock, LOCK_HANDLE,  handle);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , LOCK_RESULT, Lock_Deinit, LOCK_HANDLE,  handle);


BEGIN_TEST_SUITE(modbus_read_unittests)

    TEST_SUITE_INITIALIZE(TestClassInitialize)
    {
        TEST_INITIALIZE_MEMORY_DEBUG(g_dllByDll);
        g_testByTest = MicroMockCreateMutex();
        ASSERT_IS_NOT_NULL(g_testByTest);

		MODULE_APIS apis;
		Module_GetAPIS(&apis);
		Module_Create = apis.Module_Create;
		Module_Destroy = apis.Module_Destroy;
		Module_Receive = apis.Module_Receive;
    }

    TEST_SUITE_CLEANUP(TestClassCleanup)
    {
        MicroMockDestroyMutex(g_testByTest);
        TEST_DEINITIALIZE_MEMORY_DEBUG(g_dllByDll);
    }

    TEST_FUNCTION_INITIALIZE(TestMethodInitialize)
    {
        if (!MicroMockAcquireMutex(g_testByTest))
        {
            ASSERT_FAIL("our mutex is ABANDONED. Failure in test framework");
        }

    }

    TEST_FUNCTION_CLEANUP(TestMethodCleanup)
    {
        if (!MicroMockReleaseMutex(g_testByTest))
        {
            ASSERT_FAIL("failure in test framework at ReleaseMutex");
        }

    }

	//Tests_SRS_MODBUS_READ_99_016: [ `Module_GetAPIS` shall return a non - NULL pointer to a structure of type MODULE_APIS that has all fields non - NULL. ]
	TEST_FUNCTION(ModbusRead_GetAPIs_Success)
	{
		///Arrange
		CModbusreadMocks mocks;

		///Act
		MODULE_APIS apis;
		Module_GetAPIS(&apis);

		///Assert
		ASSERT_IS_NOT_NULL(apis.Module_Create);
		ASSERT_IS_NOT_NULL(apis.Module_Destroy);
		ASSERT_IS_NOT_NULL(apis.Module_Receive);

		///Ablution
	}

	//Tests_SRS_MODBUS_READ_99_001: [ If broker is NULL then ModbusRead_Create shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_Create_Bus_Null)
	{
		///Arrange
		CModbusreadMocks mocks;
		BROKER_HANDLE broker = NULL;
		unsigned char config;

		///Act
		auto n = Module_Create(broker, &config);

		///Assert
		ASSERT_IS_NULL(n);

		///Ablution
	}

	//Tests_SRS_IDMAP_99_002: [ If configuration is NULL then ModbusRead_Create shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_Create_Config_Null)
	{
		///Arrange
		CModbusreadMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		void * config = NULL;

		///Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);

		///Ablution
	}

	//Tests_SRS_MODBUS_READ_99_008: [** Otherwise ModbusRead_Create shall return a non - NULL pointer. ]
	TEST_FUNCTION(ModbusRead_Create_Success)
	{
		///Arrange
		CModbusreadMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		MODBUS_READ_CONFIG_TAG * config = (MODBUS_READ_CONFIG_TAG *)malloc(sizeof(MODBUS_READ_CONFIG_TAG));
		memset(config, 0, sizeof(MODBUS_READ_CONFIG_TAG));
		sprintf(config->mac_address, "01:01:01:01:01:01");
		sprintf(config->server_str, "127.0.0.1");
		sprintf(config->device_type, "AA");

		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, Lock_Init());
		STRICT_EXPECTED_CALL(mocks, ThreadAPI_Create(IGNORED_PTR_ARG, IGNORED_PTR_ARG, IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.IgnoreArgument(2)
			.IgnoreArgument(3);


		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NOT_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup

		Module_Destroy(n);
	}

	//Tests_SRS_MODBUS_READ_99_007: [ If ModbusRead_Create encounters any errors while creating the MODBUSREAD_HANDLE_DATA then it shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_Create_Fail_malloc)
	{
		///Arrange
		CModbusreadMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		MODBUS_READ_CONFIG_TAG * config = (MODBUS_READ_CONFIG_TAG *)malloc(sizeof(MODBUS_READ_CONFIG_TAG));
		memset(config, 0, sizeof(MODBUS_READ_CONFIG_TAG));
		sprintf(config->mac_address, "01:01:01:01:01:01");
		sprintf(config->server_str, "127.0.0.1");
		sprintf(config->device_type, "AA");

		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1)
			.SetFailReturn((void*)NULL);


		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup
	}

	//Tests_SRS_MODBUS_READ_99_007: [ If ModbusRead_Create encounters any errors while creating the MODBUSREAD_HANDLE_DATA then it shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_Create_Fail_lock_init)
	{
		///Arrange
		CModbusreadMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		MODBUS_READ_CONFIG_TAG * config = (MODBUS_READ_CONFIG_TAG *)malloc(sizeof(MODBUS_READ_CONFIG_TAG));
		memset(config, 0, sizeof(MODBUS_READ_CONFIG_TAG));
		sprintf(config->mac_address, "01:01:01:01:01:01");
		sprintf(config->server_str, "127.0.0.1");
		sprintf(config->device_type, "AA");

		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, Lock_Init())
			.SetFailReturn((LOCK_HANDLE)NULL);
		STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);


		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup
	}

	//Tests_SRS_MODBUS_READ_99_007: [ If ModbusRead_Create encounters any errors while creating the MODBUSREAD_HANDLE_DATA then it shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_Create_Fail_thread_create)
	{
		///Arrange
		CModbusreadMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		MODBUS_READ_CONFIG_TAG * config = (MODBUS_READ_CONFIG_TAG *)malloc(sizeof(MODBUS_READ_CONFIG_TAG));
		memset(config, 0, sizeof(MODBUS_READ_CONFIG_TAG));
		sprintf(config->mac_address, "01:01:01:01:01:01");
		sprintf(config->server_str, "127.0.0.1");
		sprintf(config->device_type, "AA");

		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, Lock_Init());
		STRICT_EXPECTED_CALL(mocks, ThreadAPI_Create(IGNORED_PTR_ARG, IGNORED_PTR_ARG, IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.IgnoreArgument(2)
			.IgnoreArgument(3)
			.SetFailReturn(THREADAPI_ERROR);
		STRICT_EXPECTED_CALL(mocks, Lock_Deinit(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);


		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup
	}

	//Tests_SRS_MODBUS_READ_99_014: [ If moduleHandle is NULL then ModbusRead_Destroy shall return. ]
	TEST_FUNCTION(ModbusRead_Destroy_return_null)
	{
		///arrange
		CModbusreadMocks mocks;
		MODULE_HANDLE n = NULL;

		///act
		Module_Destroy(n);

		///assert
		mocks.AssertActualAndExpectedCalls();
		///cleanup
	}

	//Tests_SRS_MODBUS_READ_99_015: [ Otherwise ModbusRead_Destroy shall unuse all used resources. ]
	TEST_FUNCTION(ModbusRead_Destroy_does_everything)
	{
		///arrange
		CModbusreadMocks mocks;
		LOCK_HANDLE fake_lock = (LOCK_HANDLE)0x44;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		MODBUS_READ_CONFIG_TAG * config = (MODBUS_READ_CONFIG_TAG *)malloc(sizeof(MODBUS_READ_CONFIG_TAG));
		memset(config, 0, sizeof(MODBUS_READ_CONFIG_TAG));
		sprintf(config->mac_address, "01:01:01:01:01:01");
		sprintf(config->server_str, "127.0.0.1");
		sprintf(config->device_type, "AA");

		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, Lock_Init());
		STRICT_EXPECTED_CALL(mocks, ThreadAPI_Create(IGNORED_PTR_ARG, IGNORED_PTR_ARG, IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.IgnoreArgument(2)
			.IgnoreArgument(3);


		auto n = Module_Create(broker, config);

		mocks.ResetAllCalls();

		STRICT_EXPECTED_CALL(mocks, Lock(fake_lock))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, Unlock(fake_lock))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, ThreadAPI_Join(IGNORED_PTR_ARG, IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.IgnoreArgument(2)
			.SetReturn(THREADAPI_OK);
		STRICT_EXPECTED_CALL(mocks, Lock_Deinit(fake_lock))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		///act
		Module_Destroy(n);

		///assert
		mocks.AssertActualAndExpectedCalls();
		///cleanup
	}

	//Tests_SRS_MODBUS_READ_99_009: [ If moduleHandle is NULL then ModbusRead_Receive shall fail and return. ]
	TEST_FUNCTION(ModbusRead_Receive_Fail_moduleHandle_null)
	{
		///arrange
		CModbusreadMocks mocks;
		MESSAGE_HANDLE messageHandle = (MESSAGE_HANDLE)0x42;

		///act
		Module_Receive(NULL, messageHandle);

		///assert
		mocks.AssertActualAndExpectedCalls();

		///cleanup
	}

	//Tests_SRS_MODBUS_READ_99_010: [ If messageHandle is NULL then ModbusRead_Receive shall fail and return. ]
	TEST_FUNCTION(ModbusRead_Receive_Fail_messageHandle_null)
	{
		///arrange
		CModbusreadMocks mocks;
		MODULE_HANDLE moduleHandle = (MODULE_HANDLE)0x45;

		///act
		Module_Receive(moduleHandle, NULL);

		///assert
		mocks.AssertActualAndExpectedCalls();

		///cleanup
	}

	//Tests_SRS_MODBUS_READ_99_011: [ If `messageHandle` properties does not contain a "source" property, then ModbusRead_Receive shall fail and return. ]
	TEST_FUNCTION(ModbusRead_Receive_Fail_no_source)
	{
		///arrange
		CModbusreadMocks mocks;
		MESSAGE_HANDLE messageHandle = (MESSAGE_HANDLE)0x42;
		MODULE_HANDLE moduleHandle = (MODULE_HANDLE)0x45;

		STRICT_EXPECTED_CALL(mocks, Message_GetProperties(messageHandle))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, ConstMap_Create(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, ConstMap_GetValue(IGNORED_PTR_ARG, "source"))
			.IgnoreArgument(1)
			.IgnoreArgument(2)
			.SetFailReturn((const char*)NULL);
		STRICT_EXPECTED_CALL(mocks, ConstMap_Destroy(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		///act
		Module_Receive(moduleHandle, messageHandle);

		///assert
		mocks.AssertActualAndExpectedCalls();

		///cleanup
	}

	//Tests_SRS_MODBUS_READ_99_012: [ If `messageHandle` properties contains a "deviceKey" property, then ModbusRead_Receive shall fail and return. ]
	TEST_FUNCTION(ModbusRead_Receive_Fail_with_deviceKey)
	{
		///arrange
		CModbusreadMocks mocks;
		MESSAGE_HANDLE messageHandle = (MESSAGE_HANDLE)0x42;
		MODULE_HANDLE moduleHandle = (MODULE_HANDLE)0x45;
		const char* valid_source = "mapping";

		STRICT_EXPECTED_CALL(mocks, Message_GetProperties(messageHandle))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, ConstMap_Create(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, ConstMap_GetValue(IGNORED_PTR_ARG, "source"))
			.IgnoreArgument(1)
			.IgnoreArgument(2)
			.SetReturn(valid_source);
		STRICT_EXPECTED_CALL(mocks, ConstMap_ContainsKey(IGNORED_PTR_ARG, "deviceKey"))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, ConstMap_Destroy(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		///act
		Module_Receive(moduleHandle, messageHandle);

		///assert
		mocks.AssertActualAndExpectedCalls();

		///cleanup
	}

	//Tests_SRS_MODBUS_READ_99_013: [ If `messageHandle` properties contains a "source" property that is set to "mapping", then ModbusRead_Receive shall fail and return. ]
	TEST_FUNCTION(ModbusRead_Receive_Fail_invalid_source)
	{
		///arrange
		CModbusreadMocks mocks;
		MESSAGE_HANDLE messageHandle = (MESSAGE_HANDLE)0x42;
		MODULE_HANDLE moduleHandle = (MODULE_HANDLE)0x45;
		const char* invalid_source = "not a valid source";

		STRICT_EXPECTED_CALL(mocks, Message_GetProperties(messageHandle))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, ConstMap_Create(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, ConstMap_GetValue(IGNORED_PTR_ARG, "source"))
			.IgnoreArgument(1)
			.IgnoreArgument(2)
			.SetReturn(invalid_source);
		STRICT_EXPECTED_CALL(mocks, ConstMap_Destroy(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		///act
		Module_Receive(moduleHandle, messageHandle);

		///assert
		mocks.AssertActualAndExpectedCalls();

		///cleanup
	}

	//Tests_SRS_MODBUS_READ_99_018: [ If content of messageHandle is not a JSON value, then `ModbusRead_Receive` shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_Receive_Fail_invalid_json)
	{
		///arrange
		CModbusreadMocks mocks;
		MESSAGE_HANDLE messageHandle = (MESSAGE_HANDLE)0x42;
		const char* valid_source = "mapping";


		MODBUS_READ_CONFIG_TAG * config = (MODBUS_READ_CONFIG_TAG *)malloc(sizeof(MODBUS_READ_CONFIG_TAG));
		memset(config, 0, sizeof(MODBUS_READ_CONFIG_TAG));
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		sprintf(config->mac_address, "01:01:01:01:01:01");
		sprintf(config->server_str, "127.0.0.1");
		sprintf(config->device_type, "AA");

		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, Lock_Init());
		STRICT_EXPECTED_CALL(mocks, ThreadAPI_Create(IGNORED_PTR_ARG, IGNORED_PTR_ARG, IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.IgnoreArgument(2)
			.IgnoreArgument(3);


		auto n = Module_Create(broker, config);

		mocks.ResetAllCalls();

		STRICT_EXPECTED_CALL(mocks, Message_GetProperties(messageHandle))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, ConstMap_Create(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, ConstMap_GetValue(IGNORED_PTR_ARG, "source"))
			.IgnoreArgument(1)
			.IgnoreArgument(2)
			.SetReturn(valid_source);
		STRICT_EXPECTED_CALL(mocks, ConstMap_ContainsKey(IGNORED_PTR_ARG, "deviceKey"))
			.IgnoreArgument(1)
			.IgnoreArgument(2)
			.SetReturn(false);
		STRICT_EXPECTED_CALL(mocks, ConstMap_GetValue(IGNORED_PTR_ARG, "macAddress"))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, Message_GetContent(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_parse_string(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetFailReturn((JSON_Value *)NULL);
		STRICT_EXPECTED_CALL(mocks, ConstMap_Destroy(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		///act
		Module_Receive(n, messageHandle);

		///assert
		mocks.AssertActualAndExpectedCalls();

		///cleanup

		Module_Destroy(n);
	}
	//Tests_SRS_MODBUS_READ_99_017: [ ModbusRead_Receive shall return. ]
	TEST_FUNCTION(ModbusRead_Receive_does_everything)
	{
		///arrange
		CModbusreadMocks mocks;
		MESSAGE_HANDLE messageHandle = (MESSAGE_HANDLE)0x42;
		const char* valid_source = "mapping";
		//const char* valid_json = "\"address\":\"400001\",\"value\":\"999\",\"uid\":\"1\"";


		MODBUS_READ_CONFIG_TAG * config = (MODBUS_READ_CONFIG_TAG *)malloc(sizeof(MODBUS_READ_CONFIG_TAG));
		memset(config, 0, sizeof(MODBUS_READ_CONFIG_TAG));
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		JSON_Value* json = (JSON_Value*)&fake;
		JSON_Object * obj = (JSON_Object *)&fake;
		LOCK_HANDLE fake_lock = (LOCK_HANDLE)&fake;
		sprintf(config->mac_address, "01:01:01:01:01:01");
		sprintf(config->server_str, "127.0.0.1");
		sprintf(config->device_type, "AA");

		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, Lock_Init());
		STRICT_EXPECTED_CALL(mocks, ThreadAPI_Create(IGNORED_PTR_ARG, IGNORED_PTR_ARG, IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.IgnoreArgument(2)
			.IgnoreArgument(3);


		auto n = Module_Create(broker, config);

		mocks.ResetAllCalls();

		STRICT_EXPECTED_CALL(mocks, Message_GetProperties(messageHandle))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, ConstMap_Create(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, ConstMap_GetValue(IGNORED_PTR_ARG, "source"))
			.IgnoreArgument(1)
			.IgnoreArgument(2)
			.SetReturn(valid_source);
		STRICT_EXPECTED_CALL(mocks, ConstMap_ContainsKey(IGNORED_PTR_ARG, "deviceKey"))
			.IgnoreArgument(1)
			.IgnoreArgument(2)
			.SetReturn(false);
		STRICT_EXPECTED_CALL(mocks, ConstMap_GetValue(IGNORED_PTR_ARG, "macAddress"))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, Message_GetContent(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_parse_string(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn(json);
		STRICT_EXPECTED_CALL(mocks, json_value_get_object(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn(obj); 
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "address"))
			.IgnoreArgument(1)
			.IgnoreArgument(2)
			.SetReturn("40001");
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "value"))
			.IgnoreArgument(1)
			.IgnoreArgument(2)
			.SetReturn("22");
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "uid"))
			.IgnoreArgument(1)
			.IgnoreArgument(2)
			.SetReturn("1");
		STRICT_EXPECTED_CALL(mocks, Lock(fake_lock))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, Unlock(fake_lock))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, ConstMap_Destroy(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		///act
		Module_Receive(n, messageHandle);

		///assert
		mocks.AssertActualAndExpectedCalls();

		///Cleanup

		Module_Destroy(n);
	}
END_TEST_SUITE(modbus_read_unittests)
