#Copyright (c) Microsoft. All rights reserved.
#Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required(VERSION 2.8.11)

set(modbus_read_sources
    ./src/modbus_read.c
)

set(modbus_read_headers
    ./inc/modbus_read.h
)

include_directories(./inc)
include_directories(${GW_INC})

#this builds the modbus_read dynamic library
add_library(modbus_read MODULE ${modbus_read_sources}  ${modbus_read_headers})
target_link_libraries(modbus_read gateway)

#this builds the modbus_read static library
add_library(modbus_read_static  ${modbus_read_sources} ${modbus_read_headers})
target_compile_definitions(modbus_read_static PRIVATE BUILD_MODULE_TYPE_STATIC)
target_link_libraries(modbus_read_static gateway)

linkSharedUtil(modbus_read)
linkSharedUtil(modbus_read_static)

add_module_to_solution(modbus_read)
add_subdirectory(tests)

if(install_executables)
    install(TARGETS modbus_read LIBRARY DESTINATION lib) 
endif()

