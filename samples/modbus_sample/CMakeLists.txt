#Copyright (c) Microsoft. All rights reserved.
#Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required(VERSION 2.8.12)
#this is CMakeLists for modbus sample

set(modbus_sources
    ./src/main.c
)
if(WIN32)
    set(modbus_sources 
        ${modbus_sources}
        ./src/modbus_win.json
    )
    set_source_files_properties(./src/modbus_win.json PROPERTIES HEADER_FILE_ONLY ON)
else()
    set(modbus_sources 
        ${modbus_sources}
        ./src/modbus_lin.json
    )
    set_source_files_properties(./src/modbus_lin.json PROPERTIES HEADER_FILE_ONLY ON)
endif()

set(modbus_headers
)

include_directories(./inc ${IOTHUB_CLIENT_INC_FOLDER})
include_directories(${GW_INC})
include_directories(../../modules/common)

add_executable(modbus_sample ${modbus_headers} ${modules_path_file} ${modbus_sources})

add_dependencies(modbus_sample logger identity_map iothub modbus_read)

target_link_libraries(modbus_sample gateway gateway)
linkSharedUtil(modbus_sample)
install_broker(modbus_sample ${CMAKE_CURRENT_BINARY_DIR}/$(Configuration) )
copy_gateway_dll(modbus_sample ${CMAKE_CURRENT_BINARY_DIR}/$(Configuration) )
copy_iothub_client_dll(modbus_sample ${CMAKE_CURRENT_BINARY_DIR}/$(Configuration) )

add_sample_to_solution(modbus_sample)