# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /src

RUN mkdir -p /root/.nuget/NuGet
COPY ./config/NuGetPackageSource.Config /root/.nuget/NuGet/NuGet.Config

#RUN mkdir -p /app/EntityDataAssemblies
#COPY ./EntityDataAssemblies/ /app/EntityDataAssemblies

COPY ./src/ /src

RUN dotnet restore ./WindowsService/

RUN dotnet build ./WindowsService/


#   publish
RUN dotnet publish ./WindowsService/ -o /publish --configuration Release
RUN ls /publish

# Publish Stage
#FROM microsoft/dotnet:2.1-runtime
FROM mcr.microsoft.com/dotnet/aspnet:6.0
ARG git_branch
WORKDIR /app
COPY --from=build-env /publish .

# install System.Drawing native dependencies
# RUN apt-get update \
#     && apt-get install -y --allow-unauthenticated \
#         libc6-dev \
#         libgdiplus \
#         libx11-dev \
#         libproj-dev \
#         libgdal-dev \
#		  libfreetype6-dev \
#         libfontconfig1 \
#         ttf-mscorefonts-installer \
#      && rm -rf /var/lib/apt/lists/*

# RUN echo "deb http://ftp.us.debian.org/debian jessie main contrib" >> /etc/apt/sources.list
RUN mv /var/lib/dpkg/info /var/lib/dpkg/info_old && mkdir /var/lib/dpkg/info
RUN echo "deb http://ftp.us.debian.org/debian stable main contrib" >> /etc/apt/sources.list
RUN apt-get update \
    && apt-get install -y --no-install-recommends libc6-dev libgdiplus libx11-dev libproj-dev libgdal-dev libfreetype6 libfontconfig1 ttf-mscorefonts-installer \
    && rm -rf /var/lib/apt/lists/*
    
ENV ASPNETCORE_ENVIRONMENT=$git_branch

# RUN ln -s /usr/lib64/libproj.so.0 /usr/lib/libproj.so
# RUN ln -s /app/runtimes/linux-x64/nativecd/libgdal.so.20 /usr/lib/libgdal.so

# ENV LD_LIBRARY_PATH=/lib:/usr/lib:/usr/local/lib:/app/runtimes/linux-x64/native
# ENV LD_DEBUG=/lib:/usr/lib:/usr/local/lib:/app/runtimes/linux-x64/native
# ENV LD_DEBUG_PATH=/lib:/usr/lib:/usr/local/lib:/app/runtimes/linux-x64/native

ENTRYPOINT ["dotnet", "WindowsService.dll"]

