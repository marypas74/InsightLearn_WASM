# k6 Stress Testing Docker Image for InsightLearn
#
# This image includes k6 with all necessary extensions and tools
# for comprehensive stress testing of the InsightLearn platform

FROM grafana/k6:latest

USER root

# Install additional utilities
RUN apk add --no-cache \
    curl \
    jq \
    bash \
    ca-certificates

# Create directory for test scripts and results
RUN mkdir -p /tests /results
WORKDIR /tests

# Copy test scripts
COPY config.js /tests/
COPY smoke-test.js /tests/
COPY load-test.js /tests/
COPY stress-test.js /tests/
COPY spike-test.js /tests/
COPY soak-test.js /tests/

# Make sure files are readable
RUN chmod -R 755 /tests && chmod -R 777 /results

# Set default command
CMD ["run", "--out", "json=/results/results.json", "load-test.js"]
