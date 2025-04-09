
# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0-nanoserver-1809 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["InventoryAPI/InventoryAPI.csproj", "InventoryAPI/"]
RUN dotnet restore "./InventoryAPI/InventoryAPI.csproj"
COPY . .
WORKDIR "/src/InventoryAPI"
RUN dotnet build "./InventoryAPI.csproj" -c %BUILD_CONFIGURATION% -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./InventoryAPI.csproj" -c %BUILD_CONFIGURATION% -o /app/publish /p:UseAppHost=true

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InventoryAPI.dll"]