# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble AS base
USER $APP_UID
WORKDIR /app

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

RUN apt update \
    && apt install -y python3 \
    && dotnet workload install wasm-tools

COPY ["apps/GameHub.Web.UI/GameHub.Web.UI.csproj", "apps/GameHub.Web.UI/"]
COPY ["src/GameHub.Contracts/GameHub.Contracts.csproj", "src/GameHub.Contracts/"]
COPY ["src/GameHub.Abstractions/GameHub.Abstractions.csproj", "src/GameHub.Abstractions/"]
RUN dotnet restore "./apps/GameHub.Web.UI/GameHub.Web.UI.csproj"
COPY . .
WORKDIR "/src/apps/GameHub.Web.UI"
RUN dotnet build "./GameHub.Web.UI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GameHub.Web.UI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM nginx:latest
COPY --from=publish /app/publish/wwwroot/ /usr/share/nginx/html/
COPY ["apps/GameHub.Web.UI/nginx.conf","/etc/nginx/nginx.conf"]
