# Remotion Renderer Dockerfile
# Multi-stage build for InsightLearn video export service

# Stage 1: Build
FROM node:20-slim AS builder

WORKDIR /app

# Install dependencies for Remotion and Chromium
RUN apt-get update && apt-get install -y \
    chromium \
    fonts-liberation \
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libcups2 \
    libdbus-1-3 \
    libdrm2 \
    libgbm1 \
    libgtk-3-0 \
    libnspr4 \
    libnss3 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    xdg-utils \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

# Copy package files
COPY package*.json ./

# Install all dependencies (including devDependencies for build)
RUN npm ci

# Copy source code
COPY . .

# Build TypeScript
RUN npm run build || echo "Build step - bundling happens at runtime"

# Stage 2: Production
FROM node:20-slim AS production

WORKDIR /app

# Install runtime dependencies for Chromium and FFmpeg
RUN apt-get update && apt-get install -y \
    chromium \
    fonts-liberation \
    fonts-dejavu-core \
    fonts-noto-cjk \
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libcups2 \
    libdbus-1-3 \
    libdrm2 \
    libgbm1 \
    libgtk-3-0 \
    libnspr4 \
    libnss3 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    xdg-utils \
    ffmpeg \
    --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

# Set Chromium path for Puppeteer/Remotion
ENV PUPPETEER_SKIP_CHROMIUM_DOWNLOAD=true
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium
ENV REMOTION_CHROME_EXECUTABLE=/usr/bin/chromium

# Copy package files
COPY package*.json ./

# Install ALL dependencies (Remotion needs devDeps for bundling at runtime)
RUN npm ci

# Copy built application from builder
COPY --from=builder /app/dist ./dist
COPY --from=builder /app/src ./src
COPY --from=builder /app/tsconfig.json ./

# Create non-root user
RUN groupadd -r remotion && useradd -r -g remotion remotion \
    && mkdir -p /tmp/remotion-bundle /tmp/remotion-output \
    && chown -R remotion:remotion /app /tmp/remotion-bundle /tmp/remotion-output

USER remotion

# Environment
ENV NODE_ENV=production
ENV PORT=3000

EXPOSE 3000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:3000/health || exit 1

# Start server
CMD ["node", "--experimental-specifier-resolution=node", "dist/index.js"]
