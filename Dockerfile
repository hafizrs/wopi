# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /src
# Copy the files and folders from current directory to "app" directory
COPY ./src/ /src

RUN mkdir -p /root/.nuget/NuGet
COPY ./config/NuGetPackageSource.Config /root/.nuget/NuGet/NuGet.Config

RUN dotnet restore ./WebService/

RUN dotnet build ./WebService/
#   publish
RUN dotnet publish ./WebService/ -o /publish --configuration Release
RUN ls /publish

# Publish Stage
#FROM microsoft/dotnet:2.1-runtime
FROM mcr.microsoft.com/dotnet/aspnet:6.0
ARG git_branch
WORKDIR /app


COPY --from=build-env /publish .
ENV port=80

#RUN echo "deb http://ftp.us.debian.org/debian jessie main contrib" >> /etc/apt/sources.list
RUN mv /var/lib/dpkg/info /var/lib/dpkg/info_old && mkdir /var/lib/dpkg/info
RUN echo "deb http://ftp.us.debian.org/debian stable main contrib" >> /etc/apt/sources.list
RUN apt-get update \
    && apt-get install -y --no-install-recommends libc6-dev libgdiplus libx11-dev libproj-dev libgdal-dev libfontconfig1 ttf-mscorefonts-installer \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_ENVIRONMENT=$git_branch
ENV ASPNETCORE_URLS=http://+:$port
RUN ls /app

ENTRYPOINT ["dotnet", "WebService.dll"]

