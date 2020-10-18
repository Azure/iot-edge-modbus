FROM mcr.microsoft.com/dotnet/core/sdk:3.1-nanoserver-1809 AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-nanoserver-1809
WORKDIR /app
COPY --from=build-env /app/out ./

ENTRYPOINT ["dotnet", "iotedgeModbus.dll"]