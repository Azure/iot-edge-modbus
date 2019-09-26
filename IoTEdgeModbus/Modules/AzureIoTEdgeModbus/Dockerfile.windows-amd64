FROM microsoft/dotnet:2.1-sdk AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -r win10-x64 -o out

FROM mcr.microsoft.com/windows/nanoserver:1809
WORKDIR /app
COPY --from=build-env /app/out ./
CMD ["iotedgeModbus.exe"]