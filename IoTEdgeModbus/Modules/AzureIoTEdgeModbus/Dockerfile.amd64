FROM microsoft/dotnet:2.1-sdk AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM gcc:7 AS build-env-2
WORKDIR /app

# copy .c and .h file
COPY *.c ./
COPY *.h ./

# build
RUN gcc -shared -o libcomWrapper.so -fPIC comWrapper.c

FROM microsoft/dotnet:2.1-runtime
WORKDIR /app
COPY --from=build-env /app/out ./
COPY --from=build-env-2 /app/libcomWrapper.so /usr/lib/

RUN useradd -ms /bin/bash moduleuser
USER moduleuser

ENTRYPOINT ["dotnet", "iotedgeModbus.dll"]