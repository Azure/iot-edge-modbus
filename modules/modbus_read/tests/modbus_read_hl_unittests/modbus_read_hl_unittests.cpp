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

#include "parson.h"

#include "modbus_read.h"
#include "modbus_read_hl.h"


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


/*forward declarations*/
static MODULE_HANDLE ModbusRead_Create(BROKER_HANDLE broker, const void* configuration);
/*this destroys (frees) resources of the module parameter*/
static void ModbusRead_Destroy(MODULE_HANDLE moduleHandle);
/*this is the module's callback function - gets called when a message is to be received by the module*/
static void ModbusRead_Receive(MODULE_HANDLE moduleHandle, MESSAGE_HANDLE messageHandle);

static MODULE_APIS ModbusRead_APIs =
{
	ModbusRead_Create,
	ModbusRead_Destroy,
	ModbusRead_Receive
};

typedef struct json_value_t
{
	int fake;
} JSON_Value;

typedef struct json_object_t
{
	int fake;
} JSON_Object;


TYPED_MOCK_CLASS(CModbusreadHlMocks, CGlobalMock)
	{
	public:
		//gballoc
		MOCK_STATIC_METHOD_1(, void*, gballoc_malloc, size_t, size)
			void* result2 = BASEIMPLEMENTATION::gballoc_malloc(size);
		MOCK_METHOD_END(void*, result2);

		MOCK_STATIC_METHOD_1(, void, gballoc_free, void*, ptr)
			BASEIMPLEMENTATION::gballoc_free(ptr);
		MOCK_VOID_METHOD_END()
		// ModbusRead LL mocks
		MOCK_STATIC_METHOD_1(, void, MODULE_STATIC_GETAPIS(MODBUSREAD_MODULE), MODULE_APIS*, apis)
			(*apis) = ModbusRead_APIs;
			MOCK_VOID_METHOD_END();

		MOCK_STATIC_METHOD_2(, MODULE_HANDLE, ModbusRead_Create, BROKER_HANDLE, broker, const void*, configuration)
			MOCK_METHOD_END(MODULE_HANDLE, malloc(1));

		MOCK_STATIC_METHOD_1(, void, ModbusRead_Destroy, MODULE_HANDLE, moduleHandle)
			free(moduleHandle);
		MOCK_VOID_METHOD_END();

		MOCK_STATIC_METHOD_2(, void, ModbusRead_Receive, MODULE_HANDLE, moduleHandle, MESSAGE_HANDLE, messageHandle)
			MOCK_VOID_METHOD_END();

		// Parson Mocks 
		MOCK_STATIC_METHOD_1(, JSON_Value*, json_parse_string, const char *, parseString)
			JSON_Value* value = NULL;
		if (parseString != NULL)
		{
			value = (JSON_Value*)malloc(1);
		}
		MOCK_METHOD_END(JSON_Value*, value);

		MOCK_STATIC_METHOD_2(, JSON_Object *, json_array_get_object, const JSON_Array *, array, size_t, index)
			JSON_Object* object = NULL;
		if (array != NULL)
		{
			object = (JSON_Object*)0x42;
		}
		MOCK_METHOD_END(JSON_Object*, object);

		MOCK_STATIC_METHOD_1(, JSON_Array*, json_value_get_array, const JSON_Value*, value)
			JSON_Array* object = NULL;
		if (value != NULL)
		{
			object = (JSON_Array*)0x43;
		}
		MOCK_METHOD_END(JSON_Array*, object);

		MOCK_STATIC_METHOD_1(, size_t, json_array_get_count, const JSON_Array *, array)
		MOCK_METHOD_END(size_t, (size_t)0);

		MOCK_STATIC_METHOD_2(, JSON_Array *, json_object_get_array, const JSON_Object*, object, const char*, name)
			JSON_Array* array = NULL;
		if (object != NULL)
		{
			array = (JSON_Array*)0x44;
		}
		MOCK_METHOD_END(JSON_Array*, array);

		MOCK_STATIC_METHOD_2(, const char*, json_object_get_string, const JSON_Object*, object, const char*, name)
			const char * result2;
		if (strcmp(name, "macAddress") == 0)
		{
			result2 = "mac";
		}
		else if (strcmp(name, "serverConnectionString") == 0)
		{
			result2 = "server";
		}
		else if (strcmp(name, "interval") == 0)
		{
			result2 = "int";
		}
		else if (strcmp(name, "deviceType") == 0)
		{
			result2 = "type";
		}
		else if (strcmp(name, "unitId") == 0)
		{
			result2 = "unit";
		}
		else if (strcmp(name, "functionCode") == 0)
		{
			result2 = "function";
		}
		else if (strcmp(name, "startingAddress") == 0)
		{
			result2 = "address";
		}
		else if (strcmp(name, "length") == 0)
		{
			result2 = "length";
		}
		else
		{
			result2 = NULL;
		}
		MOCK_METHOD_END(const char*, result2);

		MOCK_STATIC_METHOD_1(, void, json_value_free, JSON_Value*, value)
			free(value);
		MOCK_VOID_METHOD_END();

	};


DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadHlMocks, , void*, gballoc_malloc, size_t, size);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadHlMocks, , void, gballoc_free, void*, ptr);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadHlMocks, , void, MODULE_STATIC_GETAPIS(MODBUSREAD_MODULE), MODULE_APIS*, apis);

DECLARE_GLOBAL_MOCK_METHOD_2(CModbusreadHlMocks, , MODULE_HANDLE, ModbusRead_Create, BROKER_HANDLE, broker, const void*, configuration);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadHlMocks, , void, ModbusRead_Destroy, MODULE_HANDLE, moduleHandle);
DECLARE_GLOBAL_MOCK_METHOD_2(CModbusreadHlMocks, , void, ModbusRead_Receive, MODULE_HANDLE, moduleHandle, MESSAGE_HANDLE, messageHandle);

DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadHlMocks, , JSON_Value*, json_parse_string, const char *, filename);
DECLARE_GLOBAL_MOCK_METHOD_2(CModbusreadHlMocks, , JSON_Object *, json_array_get_object, const JSON_Array *, array, size_t, index);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadHlMocks, , JSON_Array*, json_value_get_array, const JSON_Value*, value);
DECLARE_GLOBAL_MOCK_METHOD_2(CModbusreadHlMocks, , const char*, json_object_get_string, const JSON_Object*, object, const char*, name);
DECLARE_GLOBAL_MOCK_METHOD_2(CModbusreadHlMocks, , JSON_Array *, json_object_get_array, const JSON_Object*, object, const char*, name);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadHlMocks, , size_t, json_array_get_count, const JSON_Array *, array);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadHlMocks, , void, json_value_free, JSON_Value*, value);


BEGIN_TEST_SUITE(modbus_read_hl_unittests)

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

	//Tests_SRS_MODBUS_READ_HL_99_010: [ `Module_GetAPIS` shall return a non - NULL pointer to a structure of type MODULE_APIS that has all fields non - NULL. ]
	TEST_FUNCTION(ModbusRead_HL_GetAPIs_Success)
	{
		///Arrange
		CModbusreadHlMocks mocks;

		///Act
		MODULE_APIS apis;
		Module_GetAPIS(&apis);

		///Assert
		ASSERT_IS_NOT_NULL(apis.Module_Create);
		ASSERT_IS_NOT_NULL(apis.Module_Destroy);
		ASSERT_IS_NOT_NULL(apis.Module_Receive);

		///Ablution
	}

	//Tests_SRS_MODBUS_READ_HL_99_001: [ If broker is NULL then ModbusRead_HL_Create shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_HL_Create_Bus_Null)
	{
		///Arrange
		CModbusreadHlMocks mocks;
		BROKER_HANDLE broker = NULL;
		unsigned char config;

		///Act
		auto n = Module_Create(broker, &config);

		///Assert
		ASSERT_IS_NULL(n);

		///Ablution
	}

	//Tests_SRS_IDMAP_HL_99_003: [ If configuration is NULL then ModbusRead_HL_Create shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_HL_Create_Config_Null)
	{
		///Arrange
		CModbusreadHlMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		void * config = NULL;

		///Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);

		///Ablution
	}

	//Tests_SRS_MODBUS_READ_HL_99_005: [** `ModbusRead_HL_Create` shall pass `broker` and the entire config to `ModbusRead_Create`. ]
	//Tests_SRS_MODBUS_READ_HL_99_006: [** If `ModbusRead_Create` succeeds then `ModbusRead_HL_Create` shall succeed and return a non - NULL value. ]
	//Tests_SRS_MODBUS_READ_HL_99_021 : [** `ModbusRead_HL_Create` shall use "serverConnectionString", "macAddress", and "interval" values as the fields for an MODBUS_READ_CONFIG structure and add this element to the link list. ]
	//Tests_SRS_MODBUS_READ_HL_99_022 : [** `ModbusRead_HL_Create` shall use "unitId", "functionCode", "startingAddress" and "length" values as the fields for an MODBUS_READ_OPERATION structure and add this element to the link list. ]
	TEST_FUNCTION(ModbusRead_HL_Create_Success)
	{
		///Arrange
		CModbusreadHlMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		const char* config = "pretend this is a valid JSON string";

		STRICT_EXPECTED_CALL(mocks, json_parse_string(config));
		STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_value_get_array(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "serverConnectionString"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_object_get_array(IGNORED_PTR_ARG, IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "unitId"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "functionCode"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "startingAddress"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "length"))
			.IgnoreArgument(1);

		EXPECTED_CALL(mocks, MODULE_STATIC_GETAPIS(MODBUSREAD_MODULE)(IGNORED_PTR_ARG));
		STRICT_EXPECTED_CALL(mocks, ModbusRead_Create(broker, IGNORED_PTR_ARG))
			.IgnoreArgument(2);

		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NOT_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup

		Module_Destroy(n);
	}
	//Tests_SRS_MODBUS_READ_HL_99_025: [ ModbusRead_HL_Create shall walk through each object of the array. ]
	TEST_FUNCTION(ModbusRead_HL_Create_Success_with_2x2_element_array)
	{
		///Arrange
		CModbusreadHlMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		const char* config = "pretend this is a valid JSON string";

		STRICT_EXPECTED_CALL(mocks, json_parse_string(config));
		STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_value_get_array(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)2);

		{
			STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
				.IgnoreArgument(1);
			STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
				.IgnoreArgument(1)
				.IgnoreArgument(2);
			STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "serverConnectionString"))
				.IgnoreArgument(1);
			STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
				.IgnoreArgument(1);
			STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
				.IgnoreArgument(1);
			STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
				.IgnoreArgument(1);

			STRICT_EXPECTED_CALL(mocks, json_object_get_array(IGNORED_PTR_ARG, IGNORED_PTR_ARG))
				.IgnoreArgument(1)
				.IgnoreArgument(2);
			STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
				.IgnoreArgument(1)
				.SetReturn((size_t)2);
			{
				STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
					.IgnoreArgument(1);
				STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
					.IgnoreArgument(1)
					.IgnoreArgument(2);
				STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "unitId"))
					.IgnoreArgument(1);
				STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "functionCode"))
					.IgnoreArgument(1);
				STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "startingAddress"))
					.IgnoreArgument(1);
				STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "length"))
					.IgnoreArgument(1);
			}
			{
				STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
					.IgnoreArgument(1);
				STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 1))
					.IgnoreArgument(1)
					.IgnoreArgument(2);
				STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "unitId"))
					.IgnoreArgument(1);
				STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "functionCode"))
					.IgnoreArgument(1);
				STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "startingAddress"))
					.IgnoreArgument(1);
				STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "length"))
					.IgnoreArgument(1);
			}
		}
		{
			STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
				.IgnoreArgument(1);
			STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 1))
				.IgnoreArgument(1)
				.IgnoreArgument(2);
			STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "serverConnectionString"))
				.IgnoreArgument(1);
			STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
				.IgnoreArgument(1);
			STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
				.IgnoreArgument(1);
			STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
				.IgnoreArgument(1);

			STRICT_EXPECTED_CALL(mocks, json_object_get_array(IGNORED_PTR_ARG, IGNORED_PTR_ARG))
				.IgnoreArgument(1)
				.IgnoreArgument(2);
			STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
				.IgnoreArgument(1)
				.SetReturn((size_t)2);
			{
				STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
					.IgnoreArgument(1);
				STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
					.IgnoreArgument(1)
					.IgnoreArgument(2);
				STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "unitId"))
					.IgnoreArgument(1);
				STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "functionCode"))
					.IgnoreArgument(1);
				STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "startingAddress"))
					.IgnoreArgument(1);
				STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "length"))
					.IgnoreArgument(1);
			}
			{
				STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
					.IgnoreArgument(1);
				STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 1))
					.IgnoreArgument(1)
					.IgnoreArgument(2);
				STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "unitId"))
					.IgnoreArgument(1);
				STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "functionCode"))
					.IgnoreArgument(1);
				STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "startingAddress"))
					.IgnoreArgument(1);
				STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "length"))
					.IgnoreArgument(1);
			}
		}

		EXPECTED_CALL(mocks, MODULE_STATIC_GETAPIS(MODBUSREAD_MODULE)(IGNORED_PTR_ARG));
		STRICT_EXPECTED_CALL(mocks, ModbusRead_Create(broker, IGNORED_PTR_ARG))
			.IgnoreArgument(2);

		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NOT_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup

		Module_Destroy(n);
	}

	//Tests_SRS_IDMAP_HL_17_015: [ If the lower layer identity map module create fails, ModbusRead_HL_Create shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_HL_Create_ll_Create_failed_returns_null)
	{

		///Arrange
		CModbusreadHlMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		const char* config = "pretend this is a valid JSON string";

		STRICT_EXPECTED_CALL(mocks, json_parse_string(config));
		STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_value_get_array(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "serverConnectionString"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_object_get_array(IGNORED_PTR_ARG, IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "unitId"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "functionCode"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "startingAddress"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "length"))
			.IgnoreArgument(1);

		EXPECTED_CALL(mocks, MODULE_STATIC_GETAPIS(MODBUSREAD_MODULE)(IGNORED_PTR_ARG));
		STRICT_EXPECTED_CALL(mocks, ModbusRead_Create(broker, IGNORED_PTR_ARG))
			.IgnoreArgument(2)
			.SetFailReturn((MODULE_HANDLE)NULL);
		STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
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

	//Tests_SRS_MODBUS_READ_HL_99_023: [ If the 'malloc' for `config` fail, ModbusRead_HL_Create shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_HL_Create_malloc_config_failed_returns_null)
	{
		///Arrange
		CModbusreadHlMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		const char* config = "pretend this is a valid JSON string";

		STRICT_EXPECTED_CALL(mocks, json_parse_string(config));
		STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_value_get_array(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1)
			.SetFailReturn((void*)NULL);
		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup

		Module_Destroy(n);
	}

	//Tests_SRS_MODBUS_READ_HL_99_024: [ If the 'malloc' for `operation` fail, ModbusRead_HL_Create shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_HL_Create_malloc_operation_failed_returns_null)
	{
		///Arrange
		CModbusreadHlMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		const char* config = "pretend this is a valid JSON string";

		STRICT_EXPECTED_CALL(mocks, json_parse_string(config));
		STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_value_get_array(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "serverConnectionString"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_object_get_array(IGNORED_PTR_ARG, IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1)
			.SetFailReturn((void*)NULL);
		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup

		Module_Destroy(n);
	}

	//Tests_SRS_MODBUS_READ_HL_99_014: [ If the `args` object does not contain a value named "serverConnectionString" then ModbusRead_HL_Create shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_HL_Create_no_server_returns_null)
	{
		///Arrange
		CModbusreadHlMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		const char* config = "pretend this is a valid JSON string";

		STRICT_EXPECTED_CALL(mocks, json_parse_string(config));
		STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_value_get_array(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "serverConnectionString"))
			.IgnoreArgument(1)
			.SetFailReturn((const char*)NULL);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup

		Module_Destroy(n);
	}

	//Tests_SRS_MODBUS_READ_HL_99_015: [ If the `args` object does not contain a value named "macAddress" then ModbusRead_HL_Create shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_HL_Create_no_macaddress_returns_null)
	{
		///Arrange
		CModbusreadHlMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		const char* config = "pretend this is a valid JSON string";

		STRICT_EXPECTED_CALL(mocks, json_parse_string(config));
		STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_value_get_array(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "serverConnectionString"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
			.IgnoreArgument(1)
			.SetFailReturn((const char*)NULL);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup

		Module_Destroy(n);
	}

	//Tests_SRS_MODBUS_READ_HL_99_016: [ If the `args` object does not contain a value named "interval" then ModbusRead_HL_Create shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_HL_Create_no_interval_returns_null)
	{
		///Arrange
		CModbusreadHlMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		const char* config = "pretend this is a valid JSON string";

		STRICT_EXPECTED_CALL(mocks, json_parse_string(config));
		STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_value_get_array(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "serverConnectionString"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
			.IgnoreArgument(1)
			.SetFailReturn((const char*)NULL);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup

		Module_Destroy(n);
	}

	//Tests_SRS_MODBUS_READ_HL_99_026: [ If the `args` object does not contain a value named "deviceType" then ModbusRead_HL_Create shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_HL_Create_no_devicetype_returns_null)
	{
		///Arrange
		CModbusreadHlMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		const char* config = "pretend this is a valid JSON string";

		STRICT_EXPECTED_CALL(mocks, json_parse_string(config));
		STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_value_get_array(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "serverConnectionString"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
			.IgnoreArgument(1)
			.SetFailReturn((const char*)NULL);
		STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup

		Module_Destroy(n);
	}

	//Tests_SRS_MODBUS_READ_HL_99_017 : [** If the `operations` object does not contain a value named "unitId" then ModbusRead_HL_Create shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_HL_Create_no_unitid_returns_null)
	{
		///Arrange
		CModbusreadHlMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		const char* config = "pretend this is a valid JSON string";

		STRICT_EXPECTED_CALL(mocks, json_parse_string(config));
		STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_value_get_array(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "serverConnectionString"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_object_get_array(IGNORED_PTR_ARG, IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "unitId"))
			.IgnoreArgument(1)
			.SetFailReturn((const char*)NULL);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "functionCode"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "startingAddress"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "length"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup

		Module_Destroy(n);
	}

	//Tests_SRS_MODBUS_READ_HL_99_018 : [** If the `operations` object does not contain a value named "functionCode" then ModbusRead_HL_Create shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_HL_Create_no_functioncode_returns_null)
	{
		///Arrange
		CModbusreadHlMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		const char* config = "pretend this is a valid JSON string";

		STRICT_EXPECTED_CALL(mocks, json_parse_string(config));
		STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_value_get_array(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "serverConnectionString"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_object_get_array(IGNORED_PTR_ARG, IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "unitId"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "functionCode"))
			.IgnoreArgument(1)
			.SetFailReturn((const char*)NULL);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "startingAddress"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "length"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup

		Module_Destroy(n);
	}

	//Tests_SRS_MODBUS_READ_HL_99_019 : [** If the `operations` object does not contain a value named "startingAddress" then ModbusRead_HL_Create shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_HL_Create_no_startaddress_returns_null)
	{
		///Arrange
		CModbusreadHlMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		const char* config = "pretend this is a valid JSON string";

		STRICT_EXPECTED_CALL(mocks, json_parse_string(config));
		STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_value_get_array(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "serverConnectionString"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_object_get_array(IGNORED_PTR_ARG, IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "unitId"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "functionCode"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "startingAddress"))
			.IgnoreArgument(1)
			.SetFailReturn((const char*)NULL);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "length"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup

		Module_Destroy(n);
	}

	//Tests_SRS_MODBUS_READ_HL_99_020 : [** If the `operations` object does not contain a value named "length" then ModbusRead_HL_Create shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_HL_Create_no_length_returns_null)
	{
		///Arrange
		CModbusreadHlMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		const char* config = "pretend this is a valid JSON string";

		STRICT_EXPECTED_CALL(mocks, json_parse_string(config));
		STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_value_get_array(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "serverConnectionString"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_object_get_array(IGNORED_PTR_ARG, IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "unitId"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "functionCode"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "startingAddress"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "length"))
			.IgnoreArgument(1)
			.SetFailReturn((const char*)NULL);
		STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup

		Module_Destroy(n);
	}

	//Tests_SRS_MODBUS_READ_HL_99_012: [ If the JSON value does not contain `args` array then `ModbusRead_HL_Create` shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_HL_Create_no_args_array_returns_null)
	{
		///Arrange
		CModbusreadHlMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		const char* config = "pretend this is a valid JSON string";

		STRICT_EXPECTED_CALL(mocks, json_parse_string(config));
		STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_value_get_array(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetFailReturn((JSON_Array*)NULL);

		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup

		Module_Destroy(n);
	}

	//Tests_SRS_MODBUS_READ_HL_99_013: [ If the JSON object of `args` array does not contain `operations` array then `ModbusRead_HL_Create` shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_HL_Create_no_operations_array_returns_null)
	{
		///Arrange
		CModbusreadHlMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		const char* config = "pretend this is a valid JSON string";


		STRICT_EXPECTED_CALL(mocks, json_parse_string(config));
		STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_value_get_array(IGNORED_PTR_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.SetReturn((size_t)1);
		STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
			.IgnoreArgument(1)
			.IgnoreArgument(2);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "serverConnectionString"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
			.IgnoreArgument(1);
		STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
			.IgnoreArgument(1);

		STRICT_EXPECTED_CALL(mocks, json_object_get_array(IGNORED_PTR_ARG, IGNORED_PTR_ARG))
			.IgnoreArgument(1)
			.IgnoreArgument(2)
			.SetFailReturn((JSON_Array*)NULL);

		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup

		Module_Destroy(n);
	}

	//Tests_SRS_MODBUS_READ_HL_99_011: [ If configuration is not a JSON object, then `ModbusRead_HL_Create` shall fail and return NULL. ]
	TEST_FUNCTION(ModbusRead_HL_Create_parse_fails_returns_null)
	{
		///Arrange
		CModbusreadHlMocks mocks;
		unsigned char fake;
		BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
		const char* config = "pretend this is a valid JSON string";

		STRICT_EXPECTED_CALL(mocks, json_parse_string(config))
			.SetFailReturn((JSON_Value*)NULL);

		//Act
		auto n = Module_Create(broker, config);

		///Assert
		ASSERT_IS_NULL(n);
		mocks.AssertActualAndExpectedCalls();

		///Cleanup
	}

	//Tests_SRS_MODBUS_READ_HL_99_009: [ `ModbusRead_HL_Destroy` shall destroy all used resources. ]
	TEST_FUNCTION(ModbusRead_HL_Destroy_does_everything)
	{
		///arrange
		CModbusreadHlMocks mocks;
		const char * validJsonString = "calling it valid makes it so";
		BROKER_HANDLE broker = (BROKER_HANDLE)0x42;
		auto result = Module_Create(broker, validJsonString);
		mocks.ResetAllCalls();

		EXPECTED_CALL(mocks, MODULE_STATIC_GETAPIS(MODBUSREAD_MODULE)(IGNORED_PTR_ARG));
		STRICT_EXPECTED_CALL(mocks, ModbusRead_Destroy(result));

		///act
		Module_Destroy(result);

		///assert
		mocks.AssertActualAndExpectedCalls();
		///cleanup
	}

	//Tests_SRS_MODBUS_READ_HL_99_008: [ `ModbusRead_HL_Receive` shall pass the received parameters to the underlying ModbusRead's `_Receive` function. ]
	TEST_FUNCTION(ModbusRead_HL_Receive_does_everything)
	{
		///arrange
		CModbusreadHlMocks mocks;
		const char * validJsonString = "calling it valid makes it so";
		BROKER_HANDLE broker = (BROKER_HANDLE)0x42;
		MESSAGE_HANDLE messageHandle = (MESSAGE_HANDLE)0x42;
		auto result = Module_Create(broker, validJsonString);
		mocks.ResetAllCalls();

		EXPECTED_CALL(mocks, MODULE_STATIC_GETAPIS(MODBUSREAD_MODULE)(IGNORED_PTR_ARG));
		STRICT_EXPECTED_CALL(mocks, ModbusRead_Receive(result, messageHandle));

		///act
		Module_Receive(result, messageHandle);

		///assert
		mocks.AssertActualAndExpectedCalls();

		///cleanup
		Module_Destroy(result);
	}

END_TEST_SUITE(modbus_read_hl_unittests)
