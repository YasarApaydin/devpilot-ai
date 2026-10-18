# syntax=docker/dockerfile:1
# Multi-stage Dockerfile for DevPilot AI WebChatAI (.NET 9 WebApi + Native Embedded Security Engine)

# Stage 1: Build .NET Application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files for optimal layer caching
COPY ["WebChatAI.sln", "./"]
COPY ["WebChatAI.Domain/WebChatAI.Domain.csproj", "WebChatAI.Domain/"]
COPY ["WebChatAI.Application/WebChatAI.Application.csproj", "WebChatAI.Application/"]
COPY ["WebChatAI.Infrastructure/WebChatAI.Infrastructure.csproj", "WebChatAI.Infrastructure/"]
COPY ["WebChatAI.Persistence/WebChatAI.Persistence.csproj", "WebChatAI.Persistence/"]
COPY ["WebChatAI.WebApi/WebChatAI.WebApi.csproj", "WebChatAI.WebApi/"]
COPY ["WebChatAI.Tests/WebChatAI.Tests.csproj", "WebChatAI.Tests/"]

RUN dotnet restore "WebChatAI.sln"

# Copy full source and publish
COPY . .
WORKDIR "/src/WebChatAI.WebApi"
RUN dotnet publish "WebChatAI.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Container
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl and ca-certificates for network diagnostics & SSL probe
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Copy published .NET application
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000

ENTRYPOINT ["dotnet", "WebChatAI.WebApi.dll"]
