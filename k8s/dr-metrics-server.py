#!/usr/bin/env python3
"""
Disaster Recovery Metrics HTTP Server for Prometheus

Exposes disaster recovery metrics via HTTP on port 9101
Prometheus can scrape this endpoint directly

Usage:
    python3 dr-metrics-server.py [--port PORT] [--host HOST]

Author: InsightLearn DevOps Team
Version: 1.0.0
"""

import os
import subprocess
import time
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path

# Configuration
DEFAULT_PORT = 9101
DEFAULT_HOST = "0.0.0.0"
METRICS_SCRIPT = Path(__file__).parent / "export-dr-metrics.sh"


class MetricsHandler(BaseHTTPRequestHandler):
    """HTTP handler for Prometheus metrics endpoint"""

    def do_GET(self):
        """Handle GET request for /metrics endpoint"""
        if self.path == "/metrics":
            self.send_metrics()
        elif self.path == "/health":
            self.send_health()
        else:
            self.send_error(404, "Not Found - Use /metrics or /health")

    def send_metrics(self):
        """Generate and send Prometheus metrics"""
        try:
            # Execute metrics export script with OUTPUT_STDOUT=1
            env = os.environ.copy()
            env["OUTPUT_STDOUT"] = "1"
            env["METRICS_FILE"] = "/tmp/dr_metrics_temp.prom"

            result = subprocess.run(
                ["bash", str(METRICS_SCRIPT)],
                capture_output=True,
                text=True,
                timeout=30,
                env=env
            )

            if result.returncode == 0:
                metrics_data = result.stdout

                # Send response
                self.send_response(200)
                self.send_header("Content-type", "text/plain; version=0.0.4")
                self.end_headers()
                self.wfile.write(metrics_data.encode())
            else:
                self.send_error(500, f"Metrics script failed: {result.stderr}")

        except subprocess.TimeoutExpired:
            self.send_error(504, "Metrics script timeout")
        except Exception as e:
            self.send_error(500, f"Error generating metrics: {str(e)}")

    def send_health(self):
        """Health check endpoint"""
        self.send_response(200)
        self.send_header("Content-type", "text/plain")
        self.end_headers()
        self.wfile.write(b"OK\n")

    def log_message(self, format, *args):
        """Log HTTP requests with timestamp"""
        timestamp = time.strftime("%Y-%m-%d %H:%M:%S")
        print(f"[{timestamp}] {self.address_string()} - {format % args}")


def run_server(host=DEFAULT_HOST, port=DEFAULT_PORT):
    """Start HTTP server"""
    server_address = (host, port)
    httpd = HTTPServer(server_address, MetricsHandler)

    print(f"Starting Disaster Recovery Metrics Server on {host}:{port}")
    print(f"Metrics endpoint: http://{host}:{port}/metrics")
    print(f"Health endpoint: http://{host}:{port}/health")
    print(f"Metrics script: {METRICS_SCRIPT}")
    print("Press Ctrl+C to stop\n")

    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        print("\nShutting down server...")
        httpd.shutdown()


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="DR Metrics HTTP Server for Prometheus")
    parser.add_argument("--port", type=int, default=DEFAULT_PORT, help=f"Port to listen on (default: {DEFAULT_PORT})")
    parser.add_argument("--host", type=str, default=DEFAULT_HOST, help=f"Host to bind to (default: {DEFAULT_HOST})")

    args = parser.parse_args()

    # Check if metrics script exists
    if not METRICS_SCRIPT.exists():
        print(f"ERROR: Metrics script not found: {METRICS_SCRIPT}")
        print("Please ensure export-dr-metrics.sh is in the same directory")
        exit(1)

    run_server(host=args.host, port=args.port)
