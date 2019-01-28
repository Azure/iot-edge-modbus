FROM microsoft/dotnet:2.1-sdk AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore -r win10-arm

COPY . ./
RUN dotnet publish -c Release -r win10-arm -o out

FROM mcr.microsoft.com/windows/nanoserver:1809_arm
WORKDIR /app
COPY --from=build-env /app/out ./
CMD ["iotedgeModbus.exe"]