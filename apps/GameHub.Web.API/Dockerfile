# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8070

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["apps/GameHub.Web.API/GameHub.Web.API.csproj", "apps/GameHub.Web.API/"]
COPY ["src/GameHub.Infrastructure/GameHub.Infrastructure.csproj", "src/GameHub.Infrastructure/"]
COPY ["src/GameHub.Application/GameHub.Application.csproj", "src/GameHub.Application/"]
COPY ["src/GameHub.Contracts/GameHub.Contracts.csproj", "src/GameHub.Contracts/"]
COPY ["src/GameHub.Abstractions/GameHub.Abstractions.csproj", "src/GameHub.Abstractions/"]
COPY ["src/GameHub.Domain/GameHub.Domain.csproj", "src/GameHub.Domain/"]
COPY ["src/GameHub.EventBus.Contracts/GameHub.EventBus.Contracts.csproj", "src/GameHub.EventBus.Contracts/"]
RUN dotnet restore "./apps/GameHub.Web.API/GameHub.Web.API.csproj"
COPY . .
WORKDIR "/src/apps/GameHub.Web.API"
RUN dotnet build "./GameHub.Web.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GameHub.Web.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameHub.Web.API.dll"]