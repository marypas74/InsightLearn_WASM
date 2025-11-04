#!/usr/bin/env python3
"""
Simple reverse proxy for InsightLearn
Routes traffic from port 80 to minikube NodePort 31081
"""

from http.server import HTTPServer, BaseHTTPRequestHandler
import urllib.request
import urllib.error
import sys

BACKEND_HOST = "192.168.58.2"
BACKEND_PORT = 31081

class ProxyHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        self.proxy_request()

    def do_POST(self):
        self.proxy_request()

    def do_PUT(self):
        self.proxy_request()

    def do_DELETE(self):
        self.proxy_request()

    def do_HEAD(self):
        self.proxy_request()

    def do_OPTIONS(self):
        self.proxy_request()

    def proxy_request(self):
        try:
            # Build backend URL
            backend_url = f"http://{BACKEND_HOST}:{BACKEND_PORT}{self.path}"

            # Prepare headers
            headers = {}
            for header, value in self.headers.items():
                if header.lower() not in ['host', 'connection']:
                    headers[header] = value

            # Get request body for POST/PUT
            content_length = self.headers.get('Content-Length')
            body = None
            if content_length:
                body = self.rfile.read(int(content_length))

            # Make request to backend
            req = urllib.request.Request(
                backend_url,
                data=body,
                headers=headers,
                method=self.command
            )

            with urllib.request.urlopen(req, timeout=30) as response:
                # Send response status
                self.send_response(response.status)

                # Send response headers
                for header, value in response.headers.items():
                    if header.lower() not in ['connection', 'transfer-encoding']:
                        self.send_header(header, value)
                self.end_headers()

                # Send response body
                self.wfile.write(response.read())

        except urllib.error.HTTPError as e:
            self.send_response(e.code)
            self.end_headers()
            self.wfile.write(e.read())

        except urllib.error.URLError as e:
            print(f"Backend connection failed: {e}", file=sys.stderr)
            self.send_response(502)
            self.send_header('Content-Type', 'text/plain')
            self.end_headers()
            self.wfile.write(b"502 Bad Gateway - Backend unavailable\n")

        except Exception as e:
            print(f"Proxy error: {e}", file=sys.stderr)
            self.send_response(500)
            self.send_header('Content-Type', 'text/plain')
            self.end_headers()
            self.wfile.write(b"500 Internal Server Error\n")

    def log_message(self, format, *args):
        """Custom logging"""
        print(f"{self.address_string()} - [{self.log_date_time_string()}] {format % args}")

if __name__ == '__main__':
    PORT = 80

    print(f"╔═══════════════════════════════════════════════════════════╗")
    print(f"║  InsightLearn Reverse Proxy                              ║")
    print(f"╚═══════════════════════════════════════════════════════════╝")
    print(f"")
    print(f"Listening on:  0.0.0.0:{PORT}")
    print(f"Backend:       {BACKEND_HOST}:{BACKEND_PORT}")
    print(f"")
    print(f"Site accessible at:")
    print(f"  http://wasm.insightlearn.cloud")
    print(f"  http://localhost")
    print(f"")
    print(f"Press Ctrl+C to stop")
    print(f"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")
    print()

    try:
        server = HTTPServer(('0.0.0.0', PORT), ProxyHandler)
        server.serve_forever()
    except PermissionError:
        print("\n❌ ERROR: Port 80 requires root privileges")
        print("Run with: sudo python3 reverse-proxy.py")
        sys.exit(1)
    except KeyboardInterrupt:
        print("\n\n✓ Proxy stopped")
        sys.exit(0)
