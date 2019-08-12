FROM microsoft/dotnet:2.0-runtime-stretch AS base

RUN apt-get update && \
    apt-get install -y --no-install-recommends unzip procps && \
    rm -rf /var/lib/apt/lists/*

RUN useradd -ms /bin/bash moduleuser
USER moduleuser
RUN curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l ~/vsdbg

FROM microsoft/dotnet:2.1-sdk AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Debug -o out

FROM gcc:7 AS build-env-2
WORKDIR /app

# copy .c and .h file
COPY *.c ./
COPY *.h ./

# build
RUN gcc -shared -o libcomWrapper.so -fPIC comWrapper.c

FROM base
WORKDIR /app
COPY --from=build-env /app/out ./
COPY --from=build-env-2 /app/libcomWrapper.so /usr/lib/

ENTRYPOINT ["dotnet", "iotedgeModbus.dll"]