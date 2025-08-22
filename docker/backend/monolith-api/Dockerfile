FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/backend/XpertSphere.sln", "./"]
COPY ["src/backend/XpertSphere.MonolithApi/XpertSphere.MonolithApi.csproj", "src/backend/XpertSphere.MonolithApi/"]
COPY ["src/backend/XpertSphere.MonolithApi.Tests/XpertSphere.MonolithApi.Tests.csproj", "src/backend/XpertSphere.MonolithApi.Tests/"]

RUN dotnet restore "src/backend/XpertSphere.MonolithApi/XpertSphere.MonolithApi.csproj"
RUN dotnet restore "src/backend/XpertSphere.MonolithApi.Tests/XpertSphere.MonolithApi.Tests.csproj"

COPY . .

WORKDIR "/src/src/backend/XpertSphere.MonolithApi"
RUN dotnet build "XpertSphere.MonolithApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Run tests
WORKDIR "/src/src/backend/XpertSphere.MonolithApi.Tests"
RUN dotnet test "XpertSphere.MonolithApi.Tests.csproj" -c $BUILD_CONFIGURATION --no-restore --verbosity normal

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/src/backend/XpertSphere.MonolithApi"
RUN dotnet publish "XpertSphere.MonolithApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

RUN groupadd -g 1001 appgroup && \
    useradd -u 1001 -g appgroup appuser
RUN chown -R appuser:appgroup /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "XpertSphere.MonolithApi.dll"]
