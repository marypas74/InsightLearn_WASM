#!/usr/bin/env python3
"""
Restore GUI Web Server
=====================

Web interface for restoring Kubernetes resources from backup.
Provides user-friendly GUI instead of command line.

Features:
- List available backups
- Browse backup contents
- Select resources to restore
- One-click restore with confirmation
- Real-time logs
- Accessible from any browser in intranet

Port: 9102
URL: http://localhost:9102 or http://192.168.1.114:9102

Author: InsightLearn DevOps Team
Version: 1.0.0
"""

import os
import sys
import json
import tarfile
import subprocess
import tempfile
import shutil
from datetime import datetime
from pathlib import Path
from http.server import HTTPServer, BaseHTTPRequestHandler
from urllib.parse import parse_qs, urlparse
import threading

# Configuration
BACKUP_DIR = "/var/backups/k3s-cluster"
PORT = 9102
HOST = "0.0.0.0"

class RestoreHandler(BaseHTTPRequestHandler):
    """HTTP request handler for restore GUI"""

    def log_message(self, format, *args):
        """Custom logging"""
        timestamp = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
        sys.stderr.write(f"[{timestamp}] {format % args}\n")

    def _send_json(self, data, status=200):
        """Send JSON response"""
        self.send_response(status)
        self.send_header('Content-Type', 'application/json')
        self.send_header('Access-Control-Allow-Origin', '*')
        self.end_headers()
        self.wfile.write(json.dumps(data).encode())

    def _send_html(self, html, status=200):
        """Send HTML response"""
        self.send_response(status)
        self.send_header('Content-Type', 'text/html; charset=utf-8')
        self.end_headers()
        self.wfile.write(html.encode())

    def do_GET(self):
        """Handle GET requests"""
        parsed = urlparse(self.path)
        path = parsed.path

        if path == '/' or path == '/index.html':
            self._send_html(self._get_main_page())
        elif path == '/api/backups':
            self._handle_list_backups()
        elif path.startswith('/api/backup/'):
            backup_name = path.split('/')[-1]
            self._handle_backup_contents(backup_name)
        else:
            self.send_error(404, "Not Found")

    def do_POST(self):
        """Handle POST requests"""
        parsed = urlparse(self.path)
        path = parsed.path

        content_length = int(self.headers.get('Content-Length', 0))
        post_data = self.rfile.read(content_length).decode('utf-8')

        try:
            data = json.loads(post_data) if post_data else {}
        except json.JSONDecodeError:
            self._send_json({'error': 'Invalid JSON'}, 400)
            return

        if path == '/api/restore':
            self._handle_restore(data)
        elif path == '/api/restore-full':
            self._handle_restore_full(data)
        else:
            self.send_error(404, "Not Found")

    def _handle_list_backups(self):
        """List available backups"""
        try:
            backups = []
            backup_path = Path(BACKUP_DIR)

            if not backup_path.exists():
                self._send_json({'backups': []})
                return

            for backup_file in sorted(backup_path.glob('*.tar.gz'), key=lambda x: x.stat().st_mtime, reverse=True):
                if backup_file.name == 'latest-backup.tar.gz':
                    continue  # Skip symlink

                stat = backup_file.stat()
                backups.append({
                    'name': backup_file.name,
                    'size': stat.st_size,
                    'size_human': self._human_size(stat.st_size),
                    'modified': datetime.fromtimestamp(stat.st_mtime).strftime('%Y-%m-%d %H:%M:%S'),
                    'timestamp': stat.st_mtime
                })

            self._send_json({'backups': backups})
        except Exception as e:
            self._send_json({'error': str(e)}, 500)

    def _handle_backup_contents(self, backup_name):
        """Get contents of a specific backup"""
        try:
            backup_path = Path(BACKUP_DIR) / backup_name

            if not backup_path.exists():
                self._send_json({'error': 'Backup not found'}, 404)
                return

            # Extract to temp directory
            with tempfile.TemporaryDirectory() as temp_dir:
                with tarfile.open(backup_path, 'r:gz') as tar:
                    tar.extractall(temp_dir)

                # Find resources directory
                resources_dir = None
                for root, dirs, files in os.walk(temp_dir):
                    if 'resources' in dirs:
                        resources_dir = Path(root) / 'resources'
                        break

                if not resources_dir:
                    self._send_json({'error': 'Resources not found in backup'}, 500)
                    return

                # Parse YAML files
                resources = {}
                for yaml_file in resources_dir.glob('*.yaml'):
                    resource_type = yaml_file.stem

                    # Count resources
                    with open(yaml_file, 'r') as f:
                        content = f.read()
                        count = content.count('kind:')

                    # Get resource names
                    names = self._get_resource_names(yaml_file)

                    resources[resource_type] = {
                        'count': count,
                        'names': names
                    }

                self._send_json({
                    'backup': backup_name,
                    'resources': resources
                })

        except Exception as e:
            self._send_json({'error': str(e)}, 500)

    def _handle_restore(self, data):
        """Execute restore operation"""
        try:
            backup_name = data.get('backup')
            resource_type = data.get('resource_type')
            resource_name = data.get('resource_name')
            namespace = data.get('namespace', 'insightlearn')

            if not all([backup_name, resource_type, resource_name]):
                self._send_json({'error': 'Missing required parameters'}, 400)
                return

            backup_path = Path(BACKUP_DIR) / backup_name

            if not backup_path.exists():
                self._send_json({'error': 'Backup not found'}, 404)
                return

            # Execute restore
            with tempfile.TemporaryDirectory() as temp_dir:
                # Extract backup
                with tarfile.open(backup_path, 'r:gz') as tar:
                    tar.extractall(temp_dir)

                # Find resources directory
                resources_dir = None
                for root, dirs, files in os.walk(temp_dir):
                    if 'resources' in dirs:
                        resources_dir = Path(root) / 'resources'
                        break

                if not resources_dir:
                    self._send_json({'error': 'Resources not found in backup'}, 500)
                    return

                yaml_file = resources_dir / f"{resource_type}.yaml"
                if not yaml_file.exists():
                    self._send_json({'error': f'Resource type {resource_type} not found'}, 404)
                    return

                # Apply resource
                cmd = [
                    'kubectl', 'apply', '-f', str(yaml_file),
                    '--namespace', namespace
                ]

                result = subprocess.run(cmd, capture_output=True, text=True, timeout=30)

                if result.returncode == 0:
                    self._send_json({
                        'success': True,
                        'message': f'Resource {resource_name} restored successfully',
                        'output': result.stdout,
                        'stderr': result.stderr
                    })
                else:
                    self._send_json({
                        'success': False,
                        'error': 'Restore failed',
                        'output': result.stdout,
                        'stderr': result.stderr
                    }, 500)

        except subprocess.TimeoutExpired:
            self._send_json({'error': 'Restore operation timed out'}, 500)
        except Exception as e:
            self._send_json({'error': str(e)}, 500)

    def _handle_restore_full(self, data):
        """Execute full cluster restore from backup"""
        try:
            backup_name = data.get('backup')
            namespace = data.get('namespace', 'insightlearn')

            if not backup_name:
                self._send_json({'error': 'Missing backup name'}, 400)
                return

            backup_path = Path(BACKUP_DIR) / backup_name

            if not backup_path.exists():
                self._send_json({'error': f'Backup {backup_name} not found'}, 404)
                return

            # Extract backup to temp directory
            temp_dir = Path(f'/tmp/k3s-restore-full-{os.getpid()}')
            temp_dir.mkdir(parents=True, exist_ok=True)

            try:
                with tarfile.open(backup_path, 'r:gz') as tar:
                    tar.extractall(temp_dir)

                # Find resources directory
                resources_dir = None
                for root, dirs, files in os.walk(temp_dir):
                    if 'resources' in dirs:
                        resources_dir = Path(root) / 'resources'
                        break

                if not resources_dir:
                    self._send_json({'error': 'Resources not found in backup'}, 500)
                    return

                # Apply resources in correct order (important for dependencies!)
                resource_order = [
                    'namespaces',
                    'customresourcedefinitions',
                    'persistentvolumes',
                    'persistentvolumeclaims',
                    'secrets',
                    'configmaps',
                    'serviceaccounts',
                    'roles',
                    'rolebindings',
                    'clusterroles',
                    'clusterrolebindings',
                    'statefulsets',
                    'daemonsets',
                    'deployments',
                    'services',
                    'ingresses',
                    'networkpolicies'
                ]

                output_lines = []
                success_count = 0
                error_count = 0

                for resource_type in resource_order:
                    yaml_file = resources_dir / f"{resource_type}.yaml"
                    if not yaml_file.exists():
                        continue

                    output_lines.append(f"\\n📦 Ripristino {resource_type}...")

                    # Apply resource
                    cmd = ['kubectl', 'apply', '-f', str(yaml_file), '--namespace', namespace]

                    result = subprocess.run(cmd, capture_output=True, text=True, timeout=60)

                    if result.returncode == 0:
                        # Count number of resources applied
                        count = result.stdout.count('configured') + result.stdout.count('created')
                        output_lines.append(f"✅ {count} risorse applicate")
                        success_count += count
                    else:
                        output_lines.append(f"❌ Errore: {result.stderr[:200]}")
                        error_count += 1

                # Cleanup temp directory
                import shutil
                shutil.rmtree(temp_dir, ignore_errors=True)

                self._send_json({
                    'success': True,
                    'message': f'Restore completo: {success_count} risorse ripristinate, {error_count} errori',
                    'output': '\\n'.join(output_lines),
                    'stats': {
                        'success': success_count,
                        'errors': error_count
                    }
                })

            except Exception as e:
                # Cleanup on error
                import shutil
                shutil.rmtree(temp_dir, ignore_errors=True)
                raise e

        except subprocess.TimeoutExpired:
            self._send_json({'error': 'Restore operation timed out'}, 500)
        except Exception as e:
            self._send_json({'error': str(e)}, 500)

    def _get_resource_names(self, yaml_file):
        """Extract resource names from YAML file"""
        try:
            result = subprocess.run(
                ['kubectl', 'get', '-f', str(yaml_file), '--all-namespaces',
                 '-o', 'custom-columns=NAME:.metadata.name', '--no-headers'],
                capture_output=True,
                text=True,
                timeout=10
            )

            if result.returncode == 0:
                return sorted(set(result.stdout.strip().split('\n')))
            return []
        except:
            return []

    def _human_size(self, bytes):
        """Convert bytes to human readable size"""
        for unit in ['B', 'KB', 'MB', 'GB']:
            if bytes < 1024.0:
                return f"{bytes:.1f} {unit}"
            bytes /= 1024.0
        return f"{bytes:.1f} TB"

    def _get_main_page(self):
        """Generate main HTML page"""
        return """<!DOCTYPE html>
<html lang="it">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>InsightLearn - Restore GUI</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 20px;
        }

        .container {
            max-width: 1200px;
            margin: 0 auto;
        }

        .header {
            background: white;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
            margin-bottom: 20px;
        }

        .header h1 {
            color: #333;
            margin-bottom: 10px;
        }

        .header p {
            color: #666;
        }

        .card {
            background: white;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
            margin-bottom: 20px;
        }

        .card h2 {
            color: #333;
            margin-bottom: 20px;
            padding-bottom: 10px;
            border-bottom: 2px solid #667eea;
        }

        .form-group {
            margin-bottom: 20px;
        }

        label {
            display: block;
            margin-bottom: 8px;
            color: #333;
            font-weight: 600;
        }

        select {
            width: 100%;
            padding: 12px;
            border: 2px solid #ddd;
            border-radius: 5px;
            font-size: 16px;
            transition: border-color 0.3s;
        }

        select:focus {
            outline: none;
            border-color: #667eea;
        }

        select:disabled {
            background: #f5f5f5;
            cursor: not-allowed;
        }

        input[type="text"] {
            width: 100%;
            padding: 12px;
            border: 2px solid #ddd;
            border-radius: 5px;
            font-size: 16px;
        }

        input[type="text"]:focus {
            outline: none;
            border-color: #667eea;
        }

        .btn {
            padding: 12px 30px;
            border: none;
            border-radius: 5px;
            font-size: 16px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.3s;
        }

        .btn-primary {
            background: #667eea;
            color: white;
        }

        .btn-primary:hover:not(:disabled) {
            background: #5568d3;
            transform: translateY(-2px);
            box-shadow: 0 4px 8px rgba(0,0,0,0.2);
        }

        .btn-primary:disabled {
            background: #ccc;
            cursor: not-allowed;
        }

        .btn-secondary {
            background: #6c757d;
            color: white;
        }

        .btn-secondary:hover {
            background: #5a6268;
        }

        .btn-warning {
            background: #ff9800;
            color: white;
        }

        .btn-warning:hover:not(:disabled) {
            background: #e68900;
            transform: translateY(-2px);
            box-shadow: 0 4px 8px rgba(0,0,0,0.2);
        }

        .btn-warning:disabled {
            background: #ccc;
            cursor: not-allowed;
        }

        .info-box {
            background: #e7f3ff;
            border-left: 4px solid #667eea;
            padding: 15px;
            margin-bottom: 20px;
            border-radius: 5px;
        }

        .warning-box {
            background: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 15px;
            margin-bottom: 20px;
            border-radius: 5px;
        }

        .success-box {
            background: #d4edda;
            border-left: 4px solid #28a745;
            padding: 15px;
            margin-bottom: 20px;
            border-radius: 5px;
            display: none;
        }

        .error-box {
            background: #f8d7da;
            border-left: 4px solid #dc3545;
            padding: 15px;
            margin-bottom: 20px;
            border-radius: 5px;
            display: none;
        }

        .log-box {
            background: #f8f9fa;
            border: 1px solid #ddd;
            padding: 15px;
            border-radius: 5px;
            max-height: 300px;
            overflow-y: auto;
            font-family: 'Courier New', monospace;
            font-size: 14px;
            white-space: pre-wrap;
            display: none;
        }

        .loading {
            display: none;
            text-align: center;
            padding: 20px;
        }

        .spinner {
            border: 4px solid #f3f3f3;
            border-top: 4px solid #667eea;
            border-radius: 50%;
            width: 40px;
            height: 40px;
            animation: spin 1s linear infinite;
            margin: 0 auto;
        }

        @keyframes spin {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
        }

        .resource-count {
            color: #666;
            font-size: 14px;
            margin-left: 10px;
        }

        .status-indicator {
            display: inline-block;
            width: 10px;
            height: 10px;
            border-radius: 50%;
            margin-right: 8px;
        }

        .status-ok {
            background: #28a745;
        }

        .status-warning {
            background: #ffc107;
        }

        .status-error {
            background: #dc3545;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🔄 InsightLearn Restore GUI</h1>
            <p>Interfaccia grafica per il ripristino dei pod dal backup</p>
        </div>

        <div class="card">
            <h2>1. Seleziona Backup</h2>
            <div class="form-group">
                <label for="backup-select">Backup Disponibili:</label>
                <select id="backup-select">
                    <option value="">Caricamento...</option>
                </select>
            </div>
            <div id="backup-info" class="info-box" style="display:none;"></div>
        </div>

        <div class="card">
            <h2>2. Seleziona Risorsa</h2>
            <div class="form-group">
                <label for="resource-type-select">Tipo Risorsa:</label>
                <select id="resource-type-select" disabled>
                    <option value="">Prima seleziona un backup</option>
                </select>
            </div>
            <div class="form-group">
                <label for="resource-name-select">Nome Risorsa:</label>
                <select id="resource-name-select" disabled>
                    <option value="">Prima seleziona un tipo</option>
                </select>
            </div>
            <div class="form-group">
                <label for="namespace-input">Namespace:</label>
                <input type="text" id="namespace-input" value="insightlearn" placeholder="insightlearn">
            </div>
        </div>

        <div class="card">
            <h2>3. Ripristina</h2>
            <div class="warning-box">
                <strong>⚠️ Attenzione:</strong> Il ripristino sovrascriverà la risorsa esistente con la stessa nome nel namespace selezionato.
            </div>
            <div style="display: flex; gap: 10px; flex-wrap: wrap;">
                <button id="restore-btn" class="btn btn-primary" disabled>
                    🔄 Ripristina Risorsa
                </button>
                <button id="restore-full-btn" class="btn btn-warning" disabled title="Ripristina TUTTE le risorse dal backup">
                    🔥 Restore Completo
                </button>
                <button id="refresh-btn" class="btn btn-secondary">
                    🔃 Aggiorna Lista
                </button>
            </div>
        </div>

        <div class="success-box" id="success-box"></div>
        <div class="error-box" id="error-box"></div>

        <div class="loading" id="loading">
            <div class="spinner"></div>
            <p>Operazione in corso...</p>
        </div>

        <div class="log-box" id="log-box"></div>
    </div>

    <script>
        let backups = [];
        let selectedBackup = null;
        let backupContents = null;

        // Load backups on page load
        document.addEventListener('DOMContentLoaded', () => {
            loadBackups();

            document.getElementById('backup-select').addEventListener('change', onBackupChange);
            document.getElementById('resource-type-select').addEventListener('change', onResourceTypeChange);
            document.getElementById('restore-btn').addEventListener('click', onRestore);
            document.getElementById('restore-full-btn').addEventListener('click', onRestoreFull);
            document.getElementById('refresh-btn').addEventListener('click', () => {
                loadBackups();
                resetForm();
            });
        });

        async function loadBackups() {
            try {
                console.log('Loading backups...');
                const response = await fetch('/api/backups');
                console.log('Response status:', response.status);

                const data = await response.json();
                console.log('Received data:', data);

                backups = data.backups || [];
                console.log('Backups array:', backups);

                const select = document.getElementById('backup-select');
                select.innerHTML = '<option value="">Seleziona un backup...</option>';

                backups.forEach(backup => {
                    const option = document.createElement('option');
                    option.value = backup.name;
                    option.textContent = `${backup.name} - ${backup.size_human} - ${backup.modified}`;
                    select.appendChild(option);
                });

                console.log('Backups loaded successfully!');

            } catch (error) {
                console.error('Error loading backups:', error);
                showError('Errore nel caricamento dei backup: ' + error.message);
            }
        }

        async function onBackupChange(event) {
            const backupName = event.target.value;

            if (!backupName) {
                resetForm();
                return;
            }

            selectedBackup = backups.find(b => b.name === backupName);

            showLoading();

            try {
                const response = await fetch(`/api/backup/${backupName}`);
                const data = await response.json();

                hideLoading();

                if (data.error) {
                    showError(data.error);
                    return;
                }

                backupContents = data.resources;

                // Show backup info
                const infoBox = document.getElementById('backup-info');
                infoBox.style.display = 'block';
                infoBox.innerHTML = `<strong>Backup selezionato:</strong> ${backupName}<br>
                                     <strong>Risorse disponibili:</strong> ${Object.keys(backupContents).length} tipi`;

                // Populate resource types
                const typeSelect = document.getElementById('resource-type-select');
                typeSelect.disabled = false;
                typeSelect.innerHTML = '<option value="">Seleziona tipo risorsa...</option>';

                Object.keys(backupContents).sort().forEach(type => {
                    const count = backupContents[type].count;
                    const option = document.createElement('option');
                    option.value = type;
                    option.textContent = `${type} (${count} risorse)`;
                    typeSelect.appendChild(option);
                });

            } catch (error) {
                hideLoading();
                showError('Errore nel caricamento del contenuto: ' + error.message);
            }
        }

        function onResourceTypeChange(event) {
            const resourceType = event.target.value;

            if (!resourceType) {
                document.getElementById('resource-name-select').disabled = true;
                document.getElementById('restore-btn').disabled = true;
                return;
            }

            const names = backupContents[resourceType].names;

            const nameSelect = document.getElementById('resource-name-select');
            nameSelect.disabled = false;
            nameSelect.innerHTML = '<option value="">Seleziona nome risorsa...</option>';

            names.forEach(name => {
                if (name && name.trim()) {
                    const option = document.createElement('option');
                    option.value = name;
                    option.textContent = name;
                    nameSelect.appendChild(option);
                }
            });

            nameSelect.addEventListener('change', () => {
                document.getElementById('restore-btn').disabled = !nameSelect.value;
            });
        }

        async function onRestore() {
            const backupName = document.getElementById('backup-select').value;
            const resourceType = document.getElementById('resource-type-select').value;
            const resourceName = document.getElementById('resource-name-select').value;
            const namespace = document.getElementById('namespace-input').value || 'insightlearn';

            if (!backupName || !resourceType || !resourceName) {
                showError('Seleziona backup, tipo risorsa e nome risorsa');
                return;
            }

            const confirmed = confirm(
                `Confermi il ripristino di:\\n\\n` +
                `Tipo: ${resourceType}\\n` +
                `Nome: ${resourceName}\\n` +
                `Namespace: ${namespace}\\n\\n` +
                `Questa operazione sovrascriverà la risorsa esistente!`
            );

            if (!confirmed) {
                return;
            }

            showLoading();
            hideMessages();

            try {
                const response = await fetch('/api/restore', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        backup: backupName,
                        resource_type: resourceType,
                        resource_name: resourceName,
                        namespace: namespace
                    })
                });

                const data = await response.json();

                hideLoading();

                if (data.success) {
                    showSuccess(`✅ Risorsa ${resourceName} ripristinata con successo!`);
                    if (data.output) {
                        showLog(data.output + (data.stderr ? '\\n' + data.stderr : ''));
                    }
                } else {
                    showError(`❌ Errore durante il ripristino: ${data.error || 'Unknown error'}`);
                    if (data.stderr) {
                        showLog(data.stderr);
                    }
                }

            } catch (error) {
                hideLoading();
                showError('Errore di connessione: ' + error.message);
            }
        }

        async function onRestoreFull() {
            const backupName = document.getElementById('backup-select').value;
            const namespace = document.getElementById('namespace-input').value || 'insightlearn';

            if (!backupName) {
                showError('Seleziona un backup prima di procedere');
                return;
            }

            const confirmed = confirm(
                `🔥 RESTORE COMPLETO - CONFERMA\\n\\n` +
                `Backup: ${backupName}\\n` +
                `Namespace: ${namespace}\\n\\n` +
                `⚠️ ATTENZIONE! Questa operazione ripristinerà TUTTE le risorse dal backup:\\n` +
                `• Namespaces\\n` +
                `• Secrets e ConfigMaps\\n` +
                `• PersistentVolumes e PVC\\n` +
                `• Deployments e StatefulSets\\n` +
                `• Services e Ingresses\\n\\n` +
                `Le risorse esistenti verranno SOVRASCRITTE!\\n\\n` +
                `Vuoi procedere?`
            );

            if (!confirmed) {
                return;
            }

            showLoading();
            hideMessages();

            try {
                const response = await fetch('/api/restore-full', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        backup: backupName,
                        namespace: namespace
                    })
                });

                const data = await response.json();

                hideLoading();

                if (data.success) {
                    showSuccess(`✅ ${data.message}`);
                    if (data.output) {
                        showLog(data.output);
                    }
                } else {
                    showError(`❌ Errore durante il restore completo: ${data.error || 'Unknown error'}`);
                }

            } catch (error) {
                hideLoading();
                showError('Errore di connessione: ' + error.message);
            }
        }

        function resetForm() {
            document.getElementById('resource-type-select').disabled = true;
            document.getElementById('resource-type-select').innerHTML = '<option value="">Prima seleziona un backup</option>';
            document.getElementById('resource-name-select').disabled = true;
            document.getElementById('resource-name-select').innerHTML = '<option value="">Prima seleziona un tipo</option>';
            document.getElementById('restore-btn').disabled = true;
            document.getElementById('backup-info').style.display = 'none';
            hideMessages();
        }

        function showLoading() {
            document.getElementById('loading').style.display = 'block';
        }

        function hideLoading() {
            document.getElementById('loading').style.display = 'none';
        }

        function showSuccess(message) {
            const box = document.getElementById('success-box');
            box.textContent = message;
            box.style.display = 'block';
        }

        function showError(message) {
            const box = document.getElementById('error-box');
            box.textContent = message;
            box.style.display = 'block';
        }

        function showLog(log) {
            const box = document.getElementById('log-box');
            box.textContent = log;
            box.style.display = 'block';
        }

        function hideMessages() {
            document.getElementById('success-box').style.display = 'none';
            document.getElementById('error-box').style.display = 'none';
            document.getElementById('log-box').style.display = 'none';
        }
    </script>
</body>
</html>"""


def run_server():
    """Run the HTTP server"""
    server = HTTPServer((HOST, PORT), RestoreHandler)
    print(f"""
╔═══════════════════════════════════════════════════════════════════╗
║                                                                   ║
║           🌐 Restore GUI Server Started                           ║
║                                                                   ║
╚═══════════════════════════════════════════════════════════════════╝

  Local URL:    http://localhost:{PORT}
  Network URL:  http://192.168.1.114:{PORT}

  Press Ctrl+C to stop the server

═══════════════════════════════════════════════════════════════════
""")

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\n\n✅ Server stopped gracefully")
        server.shutdown()


if __name__ == '__main__':
    if os.geteuid() != 0:
        print("⚠️  Warning: This script should be run as root (sudo)")
        print("   Some restore operations may fail without root privileges")
        print()

    run_server()
