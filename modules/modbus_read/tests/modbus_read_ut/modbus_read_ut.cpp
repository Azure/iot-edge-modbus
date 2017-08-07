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
#include "module_access.h"

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

#define VALID_MAP_HANDLE    0xDEAF
#define VALID_VALUE         "value"
static MAP_RESULT currentMapResult;

static BROKER_RESULT currentBrokerResult;

static MICROMOCK_MUTEX_HANDLE g_testByTest;
static MICROMOCK_GLOBAL_SEMAPHORE_HANDLE g_dllByDll;

/*these are simple cached variables*/
static pfModule_ParseConfigurationFromJson Module_ParseConfigurationFromJson = NULL; /*gets assigned in TEST_SUITE_INITIALIZE*/
static pfModule_FreeConfiguration Module_FreeConfiguration = NULL; /*gets assigned in TEST_SUITE_INITIALIZE*/
static pfModule_Create Module_Create = NULL; /*gets assigned in TEST_SUITE_INITIALIZE*/
static pfModule_Destroy Module_Destroy = NULL; /*gets assigned in TEST_SUITE_INITIALIZE*/
static pfModule_Receive Module_Receive = NULL; /*gets assigned in TEST_SUITE_INITIALIZE*/
static pfModule_Start Module_Start = NULL; /*gets assigned in TEST_SUITE_INITIALIZE*/

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

static size_t currentStrdup_call;
static size_t whenShallStrdup_fail;


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

        MOCK_STATIC_METHOD_1(, JSON_Array*, json_value_get_array, const JSON_Value*, value)
            JSON_Array* object = NULL;
        if (value != NULL)
        {
            object = (JSON_Array*)0x43;
        }
        MOCK_METHOD_END(JSON_Array*, object);

        MOCK_STATIC_METHOD_1(, JSON_Object*, json_value_get_object, const JSON_Value*, value)
            JSON_Object* object = NULL;
        if (value != NULL)
        {
            object = (JSON_Object*)0x42;
        }
        MOCK_METHOD_END(JSON_Object*, object);

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
        else if (strcmp(name, "address") == 0)
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
        else if (strcmp(name, "sqliteEnabled") == 0)
        {
            result2 = "1";
        }
        else
        {
            result2 = NULL;
        }
        MOCK_METHOD_END(const char*, result2);

        MOCK_STATIC_METHOD_2(, JSON_Object *, json_array_get_object, const JSON_Array *, array, size_t, index)
            JSON_Object* object = NULL;
        if (array != NULL)
        {
            object = (JSON_Object*)0x42;
        }
        MOCK_METHOD_END(JSON_Object*, object);

        MOCK_STATIC_METHOD_1(, size_t, json_array_get_count, const JSON_Array *, array)
        MOCK_METHOD_END(size_t, (size_t)0);

        MOCK_STATIC_METHOD_1(, void, json_value_free, JSON_Value*, value)
            free(value);
        MOCK_VOID_METHOD_END();

        MOCK_STATIC_METHOD_1(, char*, json_serialize_to_string_pretty, const JSON_Value *, value)
            char* result2 = NULL;
        if (value != NULL)
        {
            result2 = (char*)malloc(4);
            result2[0] = 'A';
            result2[1] = 'B';
            result2[2] = 'C';
            result2[3] = '\0';
        }
        MOCK_METHOD_END(char*, result2);

        MOCK_STATIC_METHOD_3(, JSON_Status, json_object_set_string, JSON_Object *, object, const char *, name, const char *, string)
        MOCK_METHOD_END(JSON_Status, JSONSuccess);

        MOCK_STATIC_METHOD_0(, JSON_Value *, json_value_init_object);
        MOCK_METHOD_END(JSON_Value *, (JSON_Value *)0x47);

        MOCK_STATIC_METHOD_1(, void, json_free_serialized_string, char*, value)
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

            MOCK_STATIC_METHOD_1( , void, ThreadAPI_Sleep, unsigned int, milliseconds)
        MOCK_VOID_METHOD_END();

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

        // crt_abstractions.h
        MOCK_STATIC_METHOD_2(, int, mallocAndStrcpy_s, char**, destination, const char*, source)
        currentStrdup_call++;
        int r;
        if (currentStrdup_call == whenShallStrdup_fail)
        {
            r = 1;
        }
        else
        {
            if (source == NULL)
            {
                *destination = NULL;
                r = 1;
            }
            else
            {
                *destination = (char*)BASEIMPLEMENTATION::gballoc_malloc(strlen(source) + 1);
                strcpy(*destination, source);
                r = 0;
            }
        }
        MOCK_METHOD_END(int, r)
    };


DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , void*, gballoc_malloc, size_t, size);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , void, gballoc_free, void*, ptr);

DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , JSON_Value*, json_parse_string, const char *, filename);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , JSON_Array*, json_value_get_array, const JSON_Value*, value);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , JSON_Object*, json_value_get_object, const JSON_Value*, value);
DECLARE_GLOBAL_MOCK_METHOD_2(CModbusreadMocks, , const char*, json_object_get_string, const JSON_Object*, object, const char*, name);
DECLARE_GLOBAL_MOCK_METHOD_2(CModbusreadMocks, , JSON_Array *, json_object_get_array, const JSON_Object*, object, const char*, name);
DECLARE_GLOBAL_MOCK_METHOD_2(CModbusreadMocks, , JSON_Object *, json_array_get_object, const JSON_Array *, array, size_t, index);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , size_t, json_array_get_count, const JSON_Array *, array);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , void, json_value_free, JSON_Value*, value);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , void, json_free_serialized_string, char*, value);
DECLARE_GLOBAL_MOCK_METHOD_1(CModbusreadMocks, , char *, json_serialize_to_string_pretty, const JSON_Value*, value);
DECLARE_GLOBAL_MOCK_METHOD_3(CModbusreadMocks, , JSON_Status, json_object_set_string, JSON_Object *, object, const char *, name, const char *, string);
DECLARE_GLOBAL_MOCK_METHOD_0(CModbusreadMocks, , JSON_Value *, json_value_init_object);

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

DECLARE_GLOBAL_MOCK_METHOD_2(CModbusreadMocks, , int, mallocAndStrcpy_s, char**, destination, const char*, source);


BEGIN_TEST_SUITE(modbus_read_ut)

    TEST_SUITE_INITIALIZE(TestClassInitialize)
    {
        TEST_INITIALIZE_MEMORY_DEBUG(g_dllByDll);
        g_testByTest = MicroMockCreateMutex();
        ASSERT_IS_NOT_NULL(g_testByTest);

        const MODULE_API* apis = Module_GetApi(MODULE_API_VERSION_1);
        Module_ParseConfigurationFromJson = MODULE_PARSE_CONFIGURATION_FROM_JSON(apis);
        Module_FreeConfiguration = MODULE_FREE_CONFIGURATION(apis);
        Module_Create = MODULE_CREATE(apis);
        Module_Destroy = MODULE_DESTROY(apis);
        Module_Receive = MODULE_RECEIVE(apis);
        Module_Start = MODULE_START(apis);
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
        currentStrdup_call = 0;
        whenShallStrdup_fail = 0;

    }

    TEST_FUNCTION_CLEANUP(TestMethodCleanup)
    {
        if (!MicroMockReleaseMutex(g_testByTest))
        {
            ASSERT_FAIL("failure in test framework at ReleaseMutex");
        }

    }

    //Tests_SRS_MODBUS_READ_99_016: [`Module_GetApi` shall return a pointer to a `MODULE_API` structure with the required function pointers.]
    TEST_FUNCTION(ModbusRead_GetAPIs_Success)
    {
        ///Arrange
        CModbusreadMocks mocks;

        ///Act
        const MODULE_API* apis = Module_GetApi(MODULE_API_VERSION_1);

        ///assert
        ASSERT_IS_NOT_NULL(apis);
        ///Assert
        ASSERT_IS_TRUE(MODULE_PARSE_CONFIGURATION_FROM_JSON(apis) != NULL);
        ASSERT_IS_TRUE(MODULE_FREE_CONFIGURATION(apis) != NULL);
        ASSERT_IS_TRUE(MODULE_CREATE(apis));
        ASSERT_IS_TRUE(MODULE_START(apis));
        ASSERT_IS_TRUE(MODULE_DESTROY(apis));
        ASSERT_IS_TRUE(MODULE_RECEIVE(apis));

        ///Ablution
    }

    //Tests_SRS_MODBUS_READ_JSON_99_023: [ If configuration is NULL then ModbusRead_ParseConfigurationFromJson shall fail and return NULL. ]
    TEST_FUNCTION(ModbusRead_ParseConfigurationFromJson_Config_Null)
    {
        ///Arrange
        CModbusreadMocks mocks;
        unsigned char fake;
        BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
        char* config = NULL;

        ///Act
        auto n = Module_ParseConfigurationFromJson(config);

        ///Assert
        ASSERT_IS_NULL(n);

        ///Ablution
    }

    //Tests_SRS_MODBUS_READ_JSON_99_025: [** `ModbusRead_ParseConfigurationFromJson` shall pass `broker` and the entire config to `ModbusRead_Create`. ]
    //Tests_SRS_MODBUS_READ_JSON_99_026: [** If `ModbusRead_Create` succeeds then `ModbusRead_ParseConfigurationFromJson` shall succeed and return a non - NULL value. ]
    //Tests_SRS_MODBUS_READ_JSON_99_041: [** `ModbusRead_ParseConfigurationFromJson` shall use "serverConnectionString", "macAddress", and "interval" values as the fields for an MODBUS_READ_CONFIG structure and add this element to the link list. ]
    //Tests_SRS_MODBUS_READ_JSON_99_042: [** `ModbusRead_ParseConfigurationFromJson` shall use "unitId", "functionCode", "startingAddress" and "length" values as the fields for an MODBUS_READ_OPERATION structure and add this element to the link list. ]
    TEST_FUNCTION(ModbusRead_ParseConfigurationFromJson_Success)
    {
        ///Arrange
        CModbusreadMocks mocks;
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
            .SetReturn("COM1");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
            .IgnoreArgument(1)
            .SetReturn("00:00:00:00:00:00");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "baudRate"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "stopBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "dataBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "parity"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "flowControl"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "sqliteEnabled"))
            .IgnoreArgument(1);

        STRICT_EXPECTED_CALL(mocks, json_object_get_array(IGNORED_PTR_ARG, "operations"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
            .IgnoreArgument(1)
            .SetReturn((size_t)1);
        STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "unitId"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "functionCode"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "startingAddress"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "length"))
            .IgnoreArgument(1);

        //Act
        auto n = Module_ParseConfigurationFromJson(config);

        ///Assert
        ASSERT_IS_NOT_NULL(n);
        mocks.AssertActualAndExpectedCalls();

        ///Cleanup
        auto handle = Module_Create(broker, n);
        Module_Start(handle);
        Module_Destroy(handle);
    }
    //Tests_SRS_MODBUS_READ_JSON_99_045: [ ModbusRead_ParseConfigurationFromJson shall walk through each object of the array. ]
    TEST_FUNCTION(ModbusRead_ParseConfigurationFromJson_Success_with_2x2_element_array)
    {
        ///Arrange
        CModbusreadMocks mocks;
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
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "serverConnectionString"))
                .IgnoreArgument(1)
                .SetReturn("COM1");
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
                .IgnoreArgument(1)
                .SetReturn("00:00:00:00:00:00");
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "baudRate"))
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "stopBits"))
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "dataBits"))
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "parity"))
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "flowControl"))
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "sqliteEnabled"))
                .IgnoreArgument(1);

            STRICT_EXPECTED_CALL(mocks, json_object_get_array(IGNORED_PTR_ARG, "operations"))
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
                .IgnoreArgument(1)
                .SetReturn((size_t)2);
            {
                STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
                    .IgnoreArgument(1);
                STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
                    .IgnoreArgument(1);
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
                    .IgnoreArgument(1);
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
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "serverConnectionString"))
                .IgnoreArgument(1)
                .SetReturn("COM1");
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
                .IgnoreArgument(1)
                .SetReturn("00:00:00:00:00:00");
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "baudRate"))
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "stopBits"))
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "dataBits"))
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "parity"))
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "flowControl"))
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "sqliteEnabled"))
                .IgnoreArgument(1);

            STRICT_EXPECTED_CALL(mocks, json_object_get_array(IGNORED_PTR_ARG, "operations"))
                .IgnoreArgument(1);
            STRICT_EXPECTED_CALL(mocks, json_array_get_count(IGNORED_PTR_ARG))
                .IgnoreArgument(1)
                .SetReturn((size_t)2);
            {
                STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
                    .IgnoreArgument(1);
                STRICT_EXPECTED_CALL(mocks, json_array_get_object(IGNORED_PTR_ARG, 0))
                    .IgnoreArgument(1);
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
                    .IgnoreArgument(1);
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

        //Act
        auto n = Module_ParseConfigurationFromJson(config);

        ///Assert
        ASSERT_IS_NOT_NULL(n);
        mocks.AssertActualAndExpectedCalls();

        ///Cleanup

        auto handle = Module_Create(broker, n);
        Module_Start(handle);
        Module_Destroy(handle);
    }

    //Tests_SRS_MODBUS_READ_JSON_99_043: [ If the 'malloc' for `config` fail, ModbusRead_ParseConfigurationFromJson shall fail and return NULL. ]
    TEST_FUNCTION(ModbusRead_ParseConfigurationFromJson_malloc_config_failed_returns_null)
    {
        ///Arrange
        CModbusreadMocks mocks;
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
        auto n = Module_ParseConfigurationFromJson(config);

        ///Assert
        ASSERT_IS_NULL(n);
        mocks.AssertActualAndExpectedCalls();

    }

    //Tests_SRS_MODBUS_READ_JSON_99_044: [ If the 'malloc' for `operation` fail, ModbusRead_ParseConfigurationFromJson shall fail and return NULL. ]
    TEST_FUNCTION(ModbusRead_ParseConfigurationFromJson_malloc_operation_failed_returns_null)
    {
        ///Arrange
        CModbusreadMocks mocks;
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
            .SetReturn("COM1");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
            .IgnoreArgument(1)
            .SetReturn("00:00:00:00:00:00");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "baudRate"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "stopBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "dataBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "parity"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "flowControl"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "sqliteEnabled"))
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
        STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);
        //Act
        auto n = Module_ParseConfigurationFromJson(config);

        ///Assert
        ASSERT_IS_NULL(n);
        mocks.AssertActualAndExpectedCalls();

        ///Cleanup
    }

    //Tests_SRS_MODBUS_READ_JSON_99_034: [ If the `args` object does not contain a value named "serverConnectionString" then ModbusRead_ParseConfigurationFromJson shall fail and return NULL. ]
    TEST_FUNCTION(ModbusRead_ParseConfigurationFromJson_no_server_returns_null)
    {
        ///Arrange
        CModbusreadMocks mocks;
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
            .IgnoreArgument(1)
            .SetReturn("00:00:00:00:00:00");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "baudRate"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "stopBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "dataBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "parity"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "flowControl"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "sqliteEnabled"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);

        //Act
        auto n = Module_ParseConfigurationFromJson(config);

        ///Assert
        ASSERT_IS_NULL(n);
        mocks.AssertActualAndExpectedCalls();

        ///Cleanup
    }

    //Tests_SRS_MODBUS_READ_JSON_99_035: [ If the `args` object does not contain a value named "macAddress" then ModbusRead_ParseConfigurationFromJson shall fail and return NULL. ]
    TEST_FUNCTION(ModbusRead_ParseConfigurationFromJson_no_macaddress_returns_null)
    {
        ///Arrange
        CModbusreadMocks mocks;
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
            .SetReturn("COM1");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
            .IgnoreArgument(1)
            .SetFailReturn((const char*)NULL);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "baudRate"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "stopBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "dataBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "parity"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "flowControl"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "sqliteEnabled"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);

        //Act
        auto n = Module_ParseConfigurationFromJson(config);

        ///Assert
        ASSERT_IS_NULL(n);
        mocks.AssertActualAndExpectedCalls();

        ///Cleanup
    }

    //Tests_SRS_MODBUS_READ_JSON_99_036: [ If the `args` object does not contain a value named "interval" then ModbusRead_ParseConfigurationFromJson shall fail and return NULL. ]
    TEST_FUNCTION(ModbusRead_ParseConfigurationFromJson_no_interval_returns_null)
    {
        ///Arrange
        CModbusreadMocks mocks;
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
            .SetReturn("COM1");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
            .IgnoreArgument(1)
            .SetReturn("00:00:00:00:00:00");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "baudRate"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "stopBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "dataBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "parity"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "flowControl"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
            .IgnoreArgument(1)
            .SetFailReturn((const char*)NULL);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "sqliteEnabled"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);

        //Act
        auto n = Module_ParseConfigurationFromJson(config);

        ///Assert
        ASSERT_IS_NULL(n);
        mocks.AssertActualAndExpectedCalls();

        ///Cleanup
    }

    //Tests_SRS_MODBUS_READ_JSON_99_046: [ If the `args` object does not contain a value named "deviceType" then ModbusRead_ParseConfigurationFromJson shall fail and return NULL. ]
    TEST_FUNCTION(ModbusRead_ParseConfigurationFromJson_no_devicetype_returns_null)
    {
        ///Arrange
        CModbusreadMocks mocks;
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
            .SetReturn("COM1");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
            .IgnoreArgument(1)
            .SetReturn("00:00:00:00:00:00");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "baudRate"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "stopBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "dataBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "parity"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "flowControl"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
            .IgnoreArgument(1)
            .SetFailReturn((const char*)NULL);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "sqliteEnabled"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);

        //Act
        auto n = Module_ParseConfigurationFromJson(config);

        ///Assert
        ASSERT_IS_NULL(n);
        mocks.AssertActualAndExpectedCalls();

        ///Cleanup
    }

    //Tests_SRS_MODBUS_READ_JSON_99_046: [ If the `args` object does not contain a value named "sqliteEnable" then ModbusRead_ParseConfigurationFromJson shall fail and return NULL. ]
    TEST_FUNCTION(ModbusRead_ParseConfigurationFromJson_no_sqliteEnabled_returns_null)
    {
        ///Arrange
        CModbusreadMocks mocks;
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
            .SetReturn("COM1");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
            .IgnoreArgument(1)
            .SetReturn("00:00:00:00:00:00");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "baudRate"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "stopBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "dataBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "parity"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "flowControl"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "sqliteEnabled"))
            .IgnoreArgument(1)
            .SetFailReturn((const char*)NULL);
        STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);

        //Act
        auto n = Module_ParseConfigurationFromJson(config);

        ///Assert
        ASSERT_IS_NULL(n);
        mocks.AssertActualAndExpectedCalls();

        ///Cleanup
    }
    //Tests_SRS_MODBUS_READ_JSON_99_037 : [** If the `operations` object does not contain a value named "unitId" then ModbusRead_ParseConfigurationFromJson shall fail and return NULL. ]
    TEST_FUNCTION(ModbusRead_ParseConfigurationFromJson_no_unitid_returns_null)
    {
        ///Arrange
        CModbusreadMocks mocks;
        unsigned char fake;
        BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
        const char* config = "pretend this is a valid JSON string";

        STRICT_EXPECTED_CALL(mocks, json_parse_string(config));

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
            .SetReturn("COM1");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
            .IgnoreArgument(1)
            .SetReturn("00:00:00:00:00:00");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "baudRate"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "stopBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "dataBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "parity"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "flowControl"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "sqliteEnabled"))
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
        STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);

        //Act
        auto n = Module_ParseConfigurationFromJson(config);

        ///Assert
        ASSERT_IS_NULL(n);
        mocks.AssertActualAndExpectedCalls();

        ///Cleanup
    }

    //Tests_SRS_MODBUS_READ_JSON_99_038 : [** If the `operations` object does not contain a value named "functionCode" then ModbusRead_ParseConfigurationFromJson shall fail and return NULL. ]
    TEST_FUNCTION(ModbusRead_ParseConfigurationFromJson_no_functioncode_returns_null)
    {
        ///Arrange
        CModbusreadMocks mocks;
        unsigned char fake;
        BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
        const char* config = "pretend this is a valid JSON string";

        STRICT_EXPECTED_CALL(mocks, json_parse_string(config));

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
            .SetReturn("COM1");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
            .IgnoreArgument(1)
            .SetReturn("00:00:00:00:00:00");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "baudRate"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "stopBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "dataBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "parity"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "flowControl"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "sqliteEnabled"))
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
        STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);

        //Act
        auto n = Module_ParseConfigurationFromJson(config);

        ///Assert
        ASSERT_IS_NULL(n);
        mocks.AssertActualAndExpectedCalls();

        ///Cleanup
    }

    //Tests_SRS_MODBUS_READ_JSON_99_039 : [** If the `operations` object does not contain a value named "startingAddress" then ModbusRead_ParseConfigurationFromJson shall fail and return NULL. ]
    TEST_FUNCTION(ModbusRead_ParseConfigurationFromJson_no_startaddress_returns_null)
    {
        ///Arrange
        CModbusreadMocks mocks;
        unsigned char fake;
        BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
        const char* config = "pretend this is a valid JSON string";

        STRICT_EXPECTED_CALL(mocks, json_parse_string(config));

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
            .SetReturn("COM1");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
            .IgnoreArgument(1)
            .SetReturn("00:00:00:00:00:00");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "baudRate"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "stopBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "dataBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "parity"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "flowControl"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "sqliteEnabled"))
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
        STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);

        //Act
        auto n = Module_ParseConfigurationFromJson(config);

        ///Assert
        ASSERT_IS_NULL(n);
        mocks.AssertActualAndExpectedCalls();

        ///Cleanup
    }

    //Tests_SRS_MODBUS_READ_JSON_99_040 : [** If the `operations` object does not contain a value named "length" then ModbusRead_ParseConfigurationFromJson shall fail and return NULL. ]
    TEST_FUNCTION(ModbusRead_ParseConfigurationFromJson_no_length_returns_null)
    {
        ///Arrange
        CModbusreadMocks mocks;
        unsigned char fake;
        BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
        const char* config = "pretend this is a valid JSON string";

        STRICT_EXPECTED_CALL(mocks, json_parse_string(config));

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
            .SetReturn("COM1");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
            .IgnoreArgument(1)
            .SetReturn("00:00:00:00:00:00");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "baudRate"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "stopBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "dataBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "parity"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "flowControl"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "sqliteEnabled"))
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
        STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);

        //Act
        auto n = Module_ParseConfigurationFromJson(config);

        ///Assert
        ASSERT_IS_NULL(n);
        mocks.AssertActualAndExpectedCalls();

        ///Cleanup
    }

    //Tests_SRS_MODBUS_READ_JSON_99_032: [ If the JSON value does not contain `args` array then `ModbusRead_ParseConfigurationFromJson` shall fail and return NULL. ]
    TEST_FUNCTION(ModbusRead_ParseConfigurationFromJson_no_args_array_returns_null)
    {
        ///Arrange
        CModbusreadMocks mocks;
        unsigned char fake;
        BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
        const char* config = "pretend this is a valid JSON string";

        STRICT_EXPECTED_CALL(mocks, json_parse_string(config));

        STRICT_EXPECTED_CALL(mocks, json_value_get_array(IGNORED_PTR_ARG))
            .IgnoreArgument(1)
            .SetFailReturn((JSON_Array*)NULL);
        STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);

        //Act
        auto n = Module_ParseConfigurationFromJson(config);

        ///Assert
        ASSERT_IS_NULL(n);
        mocks.AssertActualAndExpectedCalls();

        ///Cleanup
    }

    //Tests_SRS_MODBUS_READ_JSON_99_033: [ If the JSON object of `args` array does not contain `operations` array then `ModbusRead_ParseConfigurationFromJson` shall fail and return NULL. ]
    TEST_FUNCTION(ModbusRead_ParseConfigurationFromJson_no_operations_array_returns_null)
    {
        ///Arrange
        CModbusreadMocks mocks;
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
            .SetReturn("COM1");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "macAddress"))
            .IgnoreArgument(1)
            .SetReturn("00:00:00:00:00:00");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "baudRate"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "stopBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "dataBits"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "parity"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "flowControl"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "interval"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "deviceType"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "sqliteEnabled"))
            .IgnoreArgument(1);

        STRICT_EXPECTED_CALL(mocks, json_object_get_array(IGNORED_PTR_ARG, IGNORED_PTR_ARG))
            .IgnoreArgument(1)
            .IgnoreArgument(2)
            .SetFailReturn((JSON_Array*)NULL);
        STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);

        //Act
        auto n = Module_ParseConfigurationFromJson(config);

        ///Assert
        ASSERT_IS_NULL(n);
        mocks.AssertActualAndExpectedCalls();

        ///Cleanup
    }

    //Tests_SRS_MODBUS_READ_JSON_99_031: [ If configuration is not a JSON object, then `ModbusRead_ParseConfigurationFromJson` shall fail and return NULL. ]
    TEST_FUNCTION(ModbusRead_ParseConfigurationFromJson_parse_fails_returns_null)
    {
        ///Arrange
        CModbusreadMocks mocks;
        unsigned char fake;
        BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
        const char* config = "pretend this is a valid JSON string";

        STRICT_EXPECTED_CALL(mocks, json_parse_string(config))
            .SetFailReturn((JSON_Value*)NULL);

        //Act
        auto n = Module_ParseConfigurationFromJson(config);

        ///Assert
        ASSERT_IS_NULL(n);
        mocks.AssertActualAndExpectedCalls();

        ///Cleanup
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

    //Tests_SRS_MODBUS_READ_99_002: [ If configuration is NULL then ModbusRead_Create shall fail and return NULL. ]
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
        LOCK_HANDLE fake_lock = (LOCK_HANDLE)0x44;
        BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
        MODBUS_READ_CONFIG * config = (MODBUS_READ_CONFIG *)malloc(sizeof(MODBUS_READ_CONFIG));
        memset(config, 0, sizeof(MODBUS_READ_CONFIG));
        sprintf(config->mac_address, "01:01:01:01:01:01");
        sprintf(config->server_str, "127.0.0.1");
        sprintf(config->device_type, "AA");

        STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, Lock_Init());
        STRICT_EXPECTED_CALL(mocks, Lock(fake_lock))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, ThreadAPI_Create(IGNORED_PTR_ARG, IGNORED_PTR_ARG, IGNORED_PTR_ARG))
            .IgnoreArgument(1)
            .IgnoreArgument(2)
            .IgnoreArgument(3);
        STRICT_EXPECTED_CALL(mocks, Unlock(IGNORED_PTR_ARG))
            .IgnoreArgument(1);


        //Act
        auto n = Module_Create(broker, config);
        Module_Start(n);

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
        MODBUS_READ_CONFIG * config = (MODBUS_READ_CONFIG *)malloc(sizeof(MODBUS_READ_CONFIG));
        memset(config, 0, sizeof(MODBUS_READ_CONFIG));
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
        MODBUS_READ_CONFIG * config = (MODBUS_READ_CONFIG *)malloc(sizeof(MODBUS_READ_CONFIG));
        memset(config, 0, sizeof(MODBUS_READ_CONFIG));
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
    TEST_FUNCTION(ModbusRead_Start_Fail_thread_create)
    {
        ///Arrange
        CModbusreadMocks mocks;
        unsigned char fake;
        BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
        LOCK_HANDLE fake_lock = (LOCK_HANDLE)0x44;
        MODBUS_READ_CONFIG * config = (MODBUS_READ_CONFIG *)malloc(sizeof(MODBUS_READ_CONFIG));
        memset(config, 0, sizeof(MODBUS_READ_CONFIG));
        sprintf(config->mac_address, "01:01:01:01:01:01");
        sprintf(config->server_str, "127.0.0.1");
        sprintf(config->device_type, "AA");

        STRICT_EXPECTED_CALL(mocks, gballoc_malloc(IGNORED_NUM_ARG))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, Lock_Init());
        STRICT_EXPECTED_CALL(mocks, Lock(fake_lock))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, ThreadAPI_Create(IGNORED_PTR_ARG, IGNORED_PTR_ARG, IGNORED_PTR_ARG))
            .IgnoreArgument(1)
            .IgnoreArgument(2)
            .IgnoreArgument(3)
            .SetFailReturn(THREADAPI_ERROR);
        STRICT_EXPECTED_CALL(mocks, Unlock(IGNORED_PTR_ARG))
            .IgnoreArgument(1);


        //Act
        auto n = Module_Create(broker, config);
        Module_Start(n);

        ///Assert
        mocks.AssertActualAndExpectedCalls();

        ///Cleanup
        Module_Destroy(n);
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
        MODBUS_READ_CONFIG * config = (MODBUS_READ_CONFIG *)malloc(sizeof(MODBUS_READ_CONFIG));
        memset(config, 0, sizeof(MODBUS_READ_CONFIG));
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
        Module_Start(n);

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


        MODBUS_READ_CONFIG * config = (MODBUS_READ_CONFIG *)malloc(sizeof(MODBUS_READ_CONFIG));
        memset(config, 0, sizeof(MODBUS_READ_CONFIG));
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
        STRICT_EXPECTED_CALL(mocks, mallocAndStrcpy_s(IGNORED_PTR_ARG, "01:01:01:01:01:01"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, Message_GetContent(IGNORED_PTR_ARG))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_parse_string(IGNORED_PTR_ARG))
            .IgnoreArgument(1)
            .SetFailReturn((JSON_Value *)NULL);
        STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);
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


        MODBUS_READ_CONFIG * config = (MODBUS_READ_CONFIG *)malloc(sizeof(MODBUS_READ_CONFIG));
        memset(config, 0, sizeof(MODBUS_READ_CONFIG));
        unsigned char fake;
        BROKER_HANDLE broker = (BROKER_HANDLE)&fake;
        JSON_Value* json = (JSON_Value*)malloc(1);
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
        STRICT_EXPECTED_CALL(mocks, mallocAndStrcpy_s(IGNORED_PTR_ARG, "01:01:01:01:01:01"))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, gballoc_free(IGNORED_PTR_ARG))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, Message_GetContent(IGNORED_PTR_ARG))
            .IgnoreArgument(1);
        STRICT_EXPECTED_CALL(mocks, json_parse_string(IGNORED_PTR_ARG))
            .IgnoreArgument(1)
            .SetReturn(json);
        STRICT_EXPECTED_CALL(mocks, json_value_get_object(IGNORED_PTR_ARG))
            .IgnoreArgument(1)
            .SetReturn(obj);
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "functionCode"))
            .IgnoreArgument(1)
            .IgnoreArgument(2)
            .SetReturn("6");
        STRICT_EXPECTED_CALL(mocks, json_object_get_string(IGNORED_PTR_ARG, "startingAddress"))
            .IgnoreArgument(1)
            .IgnoreArgument(2)
            .SetReturn("1");
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
        STRICT_EXPECTED_CALL(mocks, json_value_free(IGNORED_PTR_ARG))
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
END_TEST_SUITE(modbus_read_ut)
