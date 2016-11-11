// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef MODBUS_READ_H
#define MODBUS_READ_H

#include "module.h"
#include "modbus_read_common.h"

#ifdef __cplusplus
extern "C"
{
#endif

MODULE_EXPORT const MODULE_API* MODULE_STATIC_GETAPI(MODBUSREAD_MODULE)(const MODULE_API_VERSION gateway_api_version);

#ifdef __cplusplus
}
#endif


#endif /*MODBUS_READ_H*/
