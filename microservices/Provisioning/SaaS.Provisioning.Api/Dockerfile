# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all files
COPY . .

# Restore and build
RUN dotnet restore "EsempiDotNet.sln"
RUN dotnet build "microservices/Provisioning/SaaS.Provisioning.Api/SaaS.Provisioning.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "microservices/Provisioning/SaaS.Provisioning.Api/SaaS.Provisioning.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SaaS.Provisioning.Api.dll"]
