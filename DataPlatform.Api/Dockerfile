# --------------------------
# BASE RUNTIME IMAGE
# --------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# expose HTTP + HTTPS
EXPOSE 8080
EXPOSE 8081

# --------------------------
# BUILD IMAGE
# --------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# copy solution and restore
COPY ["DataPlatform.Api/DataPlatform.Api.csproj", "DataPlatform.Api/"]
COPY ["DataPlatform.Api/DataPlatform.Api.csproj", "DataPlatform.Api/"]

RUN dotnet restore "DataPlatform.Api/DataPlatform.Api.csproj"

# copy everything
COPY . .

WORKDIR "/src/DataPlatform.Api"
RUN dotnet build "./DataPlatform.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# --------------------------
# PUBLISH IMAGE
# --------------------------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DataPlatform.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# --------------------------
# FINAL IMAGE
# --------------------------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "DataPlatform.Api.dll"]
