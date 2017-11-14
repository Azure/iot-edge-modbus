// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdio.h>

#include "gateway.h"
#include "azure_c_shared_utility/xlogging.h"
#include "azure_c_shared_utility/vector.h"
#include "azure_c_shared_utility/platform.h"


int main(int argc, char** argv)
{
    GATEWAY_HANDLE gateway;
    if (argc != 2)
    {
        printf("usage: modbus_read_sample configFile\n");
        printf("where configFile is the name of the file that contains the Gateway configuration\n");
    }
    else
    {
		if (platform_init() == 0)
		{
			if ((gateway = Gateway_CreateFromJson(argv[1])) == NULL)
			{
				printf("failed to create the gateway from JSON\n");
			}
			else
			{
				printf("gateway successfully created from JSON\n");
				printf("gateway shall run until ENTER is pressed\n");
				(void)getchar();
				Gateway_Destroy(gateway);
			}
			platform_deinit();
		}
		else
		{
			LogError("Failed to initialize the platform.");
		}
    }
    return 0;
}
