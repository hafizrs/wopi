# Build Stage
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /src

RUN mkdir -p /root/.nuget/NuGet
COPY ./config/NuGetPackageSource.Config /root/.nuget/NuGet/NuGet.Config

COPY ./src/Sync/ ./src/Sync/


#COPY ./src/PraxisMonitor.SyncWinService/*.csproj ./PraxisMonitor.SyncWinService/
RUN dotnet restore ./src/Sync/PraxisMonitor.SyncWinService/

#   Copy everything else and build
#COPY ./src/PraxisMonitor.SyncWinService ./PraxisMonitor.SyncWinService/
RUN dotnet build ./src/Sync/PraxisMonitor.SyncWinService/


#   publish
RUN dotnet publish ./src/Sync/PraxisMonitor.SyncWinService/ -o /publish --configuration Release
RUN ls /publish

# Publish Stage
FROM mcr.microsoft.com/dotnet/core/runtime:3.1-bionic
WORKDIR /app
COPY --from=build-env /publish .

RUN apt-get update; apt-get install -y gss-ntlmssp

ENTRYPOINT ["dotnet", "Selise.Ecap.PraxisMonitor.SyncWinService.dll"]
