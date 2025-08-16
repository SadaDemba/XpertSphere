FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/backend/XpertSphere.sln", "./"]
COPY ["src/backend/XpertSphere.MonolithApi/XpertSphere.MonolithApi.csproj", "src/backend/XpertSphere.MonolithApi/"]

RUN dotnet restore "src/backend/XpertSphere.MonolithApi/XpertSphere.MonolithApi.csproj"

COPY . .

WORKDIR "/src/src/backend/XpertSphere.MonolithApi"
RUN dotnet build "XpertSphere.MonolithApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "XpertSphere.MonolithApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

RUN apk --no-cache add curl

COPY --from=publish /app/publish .

RUN addgroup -g 1001 -S appgroup && \
    adduser -S appuser -u 1001 -G appgroup
RUN chown -R appuser:appgroup /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "XpertSphere.MonolithApi.dll"]
