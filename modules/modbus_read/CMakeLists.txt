#Copyright (c) Microsoft. All rights reserved.
#Licensed under the MIT license. See LICENSE file in the project root for full license information.

#this is CMakeLists.txt for the modbus_read module
cmake_minimum_required(VERSION 2.8.11)

set(modbus_read_sources
	./src/modbus_read.c
)

set(modbus_read_headers
	./inc/modbus_read.h
)

set(modbus_read_static_sources
	${modbus_read_sources}
)

set(modbus_read_static_headers
	${modbus_read_headers}
)

set(modbus_read_hl_sources
	./src/modbus_read_hl.c
)

set(modbus_read_hl_headers
	./inc/modbus_read_hl.h
)

set(modbus_read_hl_static_sources
	${modbus_read_hl_sources}
)

set(modbus_read_hl_static_headers
	${modbus_read_hl_headers}
)


include_directories(./inc)
include_directories(${GW_INC})

#this builds the modbus_read dynamic library
add_library(modbus_read MODULE ${modbus_read_sources}  ${modbus_read_headers})
target_link_libraries(modbus_read gateway)

#this builds the modbus_read static library
add_library(modbus_read_static  ${modbus_read_static_sources} ${modbus_read_static_headers})
target_compile_definitions(modbus_read_static PRIVATE BUILD_MODULE_TYPE_STATIC)
target_link_libraries(modbus_read_static gateway)

#this builds the modbus_read dynamic library (by default it uses modbus_read module linked statically)
add_library(modbus_read_hl MODULE ${modbus_read_hl_sources} ${modbus_read_hl_headers})
target_link_libraries(modbus_read_hl modbus_read_static gateway)

#this builds the modbus_read static library (by default it uses modbus_read module linked statically)
add_library(modbus_read_hl_static ${modbus_read_hl_static_sources} ${modbus_read_hl_static_headers})
target_compile_definitions(modbus_read_hl_static PRIVATE BUILD_MODULE_TYPE_STATIC)
target_link_libraries(modbus_read_hl_static modbus_read_static gateway)

linkSharedUtil(modbus_read)
linkSharedUtil(modbus_read_static)
linkSharedUtil(modbus_read_hl)
linkSharedUtil(modbus_read_hl_static)

add_module_to_solution(modbus_read)
add_subdirectory(tests)

if(install_executables)
	install(TARGETS modbus_read LIBRARY DESTINATION lib) 
    install(TARGETS modbus_read_hl LIBRARY DESTINATION lib) 
endif()

