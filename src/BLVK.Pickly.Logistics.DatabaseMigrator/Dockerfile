# syntax=docker/dockerfile:1
# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release


WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["src/BLVK.Pickly.Logistics.DatabaseMigrator/BLVK.Pickly.Logistics.DatabaseMigrator.csproj", "src/BLVK.Pickly.Logistics.DatabaseMigrator/"]
COPY ["src/BLVK.Pickly.Logistics.Infrastructure/BLVK.Pickly.Logistics.Infrastructure.csproj", "src/BLVK.Pickly.Logistics.Infrastructure/"]
COPY ["src/BLVK.Pickly.Logistics.Domain/BLVK.Pickly.Logistics.Domain.csproj", "src/BLVK.Pickly.Logistics.Domain/"]
COPY ["src/BLVK.Pickly.Logistics.Contracts/BLVK.Pickly.Logistics.Contracts.csproj", "src/BLVK.Pickly.Logistics.Contracts/"]
COPY ["src/BLVK.Pickly.Logistics.Application/BLVK.Pickly.Logistics.Application.csproj", "src/BLVK.Pickly.Logistics.Application/"]
RUN --mount=type=secret,id=GITHUB_USERNAME \
    --mount=type=secret,id=GITHUB_PAT \
    GITHUB_USERNAME=$(cat /run/secrets/GITHUB_USERNAME) \
    GITHUB_PAT=$(cat /run/secrets/GITHUB_PAT) \
    dotnet restore "./src/BLVK.Pickly.Logistics.DatabaseMigrator/BLVK.Pickly.Logistics.DatabaseMigrator.csproj"
COPY . .
WORKDIR "/src/src/BLVK.Pickly.Logistics.DatabaseMigrator"
RUN dotnet build "./BLVK.Pickly.Logistics.DatabaseMigrator.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BLVK.Pickly.Logistics.DatabaseMigrator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BLVK.Pickly.Logistics.DatabaseMigrator.dll"]
