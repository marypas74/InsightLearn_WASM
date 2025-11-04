#!/usr/bin/env python3
from http.server import HTTPServer, BaseHTTPRequestHandler
import urllib.request
import urllib.error

BACKEND_HOST = "192.168.58.2"
BACKEND_PORT = 31081

class ProxyHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        try:
            backend_url = f"http://{BACKEND_HOST}:{BACKEND_PORT}{self.path}"
            req = urllib.request.Request(backend_url)
            with urllib.request.urlopen(req, timeout=5) as response:
                self.send_response(response.status)
                for header, value in response.headers.items():
                    if header.lower() not in ['connection', 'transfer-encoding']:
                        self.send_header(header, value)
                self.end_headers()
                self.wfile.write(response.read())
        except Exception as e:
            self.send_response(502)
            self.end_headers()
            self.wfile.write(f"Error: {e}".encode())
    
    def log_message(self, format, *args):
        pass  # Silenzioso

print("Testing proxy on port 8888...")
server = HTTPServer(('127.0.0.1', 8888), ProxyHandler)
import threading
t = threading.Thread(target=server.serve_forever, daemon=True)
t.start()

import time
time.sleep(1)

# Test
import urllib.request
try:
    response = urllib.request.urlopen('http://127.0.0.1:8888/health', timeout=5)
    print(f"✓ Proxy works! Status: {response.status}")
    print(f"✓ Response: {response.read().decode()[:50]}")
except Exception as e:
    print(f"✗ Proxy failed: {e}")

server.shutdown()
