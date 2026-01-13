# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all files
COPY . .

# Restore and build
RUN dotnet restore "SaaS.Solution.sln"
RUN dotnet build "microservices/Orchestrator/SaaS.Orchestrator.Api/SaaS.Orchestrator.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "microservices/Orchestrator/SaaS.Orchestrator.Api/SaaS.Orchestrator.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SaaS.Orchestrator.Api.dll"]
