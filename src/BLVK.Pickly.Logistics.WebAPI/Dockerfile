# syntax=docker/dockerfile:1
# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release


WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["src/BLVK.Pickly.Logistics.WebAPI/BLVK.Pickly.Logistics.WebAPI.csproj", "src/BLVK.Pickly.Logistics.WebAPI/"]
COPY ["src/BLVK.Pickly.Logistics.Application/BLVK.Pickly.Logistics.Application.csproj", "src/BLVK.Pickly.Logistics.Application/"]
COPY ["src/BLVK.Pickly.Logistics.Contracts/BLVK.Pickly.Logistics.Contracts.csproj", "src/BLVK.Pickly.Logistics.Contracts/"]
COPY ["src/BLVK.Pickly.Logistics.Domain/BLVK.Pickly.Logistics.Domain.csproj", "src/BLVK.Pickly.Logistics.Domain/"]
COPY ["src/BLVK.Pickly.Logistics.Infrastructure/BLVK.Pickly.Logistics.Infrastructure.csproj", "src/BLVK.Pickly.Logistics.Infrastructure/"]
RUN --mount=type=secret,id=GITHUB_USERNAME \
    --mount=type=secret,id=GITHUB_PAT \
    GITHUB_USERNAME=$(cat /run/secrets/GITHUB_USERNAME) \
    GITHUB_PAT=$(cat /run/secrets/GITHUB_PAT) \
    dotnet restore "./src/BLVK.Pickly.Logistics.WebAPI/BLVK.Pickly.Logistics.WebAPI.csproj"
COPY . .
WORKDIR "/src/src/BLVK.Pickly.Logistics.WebAPI"
RUN dotnet build "./BLVK.Pickly.Logistics.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BLVK.Pickly.Logistics.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM grafana/pyroscope-dotnet:0.15.0-glibc AS pyroscope

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
ENV PYROSCOPE_PROFILING_LOG_DIR=/tmp/pyroscope \
    SSL_CERT_FILE=/etc/ssl/certs/ca-certificates.crt
WORKDIR /app
COPY --from=pyroscope /Pyroscope.Profiler.Native.so /dotnet/Pyroscope.Profiler.Native.so
COPY --from=pyroscope /Pyroscope.Linux.ApiWrapper.x64.so /dotnet/Pyroscope.Linux.ApiWrapper.x64.so
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BLVK.Pickly.Logistics.WebAPI.dll"]
