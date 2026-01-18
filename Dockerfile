# Dockerfile for InsightLearn API
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install FFmpeg for audio extraction (required by WhisperTranscriptionService)
RUN apt-get update && \
    apt-get install -y --no-install-recommends ffmpeg && \
    rm -rf /var/lib/apt/lists/*

# Add non-root user
RUN groupadd -r appuser && useradd -r -g appuser -u 1001 appuser

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG VERSION=1.0.0
ARG GIT_COMMIT=unknown
ARG BUILD_NUMBER=0
ARG BUILD_DATE=unknown

WORKDIR /src

# Copy solution and project files
COPY ["InsightLearn.WASM.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/InsightLearn.Application/InsightLearn.Application.csproj", "src/InsightLearn.Application/"]
COPY ["src/InsightLearn.Core/InsightLearn.Core.csproj", "src/InsightLearn.Core/"]
COPY ["src/InsightLearn.Infrastructure/InsightLearn.Infrastructure.csproj", "src/InsightLearn.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/InsightLearn.Application/InsightLearn.Application.csproj"

# Copy all source code
COPY . .

# Publish the application
WORKDIR "/src/src/InsightLearn.Application"
RUN dotnet publish "InsightLearn.Application.csproj" \
    -c Release \
    -o /app/publish \
    /p:Version=${VERSION} \
    /p:SourceRevisionId=${GIT_COMMIT} \
    /p:BuildNumber=${BUILD_NUMBER}

# Verify runtimeconfig was created
RUN ls -la /app/publish/ && \
    ls -la /app/publish/*.runtimeconfig.json || echo "Warning: runtimeconfig not found"

FROM base AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Copy runtimeconfig if missing
COPY InsightLearn.Application.runtimeconfig.json /app/InsightLearn.Application.runtimeconfig.json

# Create necessary directories with correct permissions
RUN mkdir -p /app/wwwroot /app/logs /app/temp /app/sessions && \
    chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "InsightLearn.Application.dll"]
