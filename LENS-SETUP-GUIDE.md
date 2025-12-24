# Guida Configurazione Lens per Cluster K3s InsightLearn

**Data**: 2025-12-05
**Server**: insightlearn-k3s (192.168.1.114)
**Cluster**: K3s v1.33.5+k3s1

## ✅ Prerequisiti

- [x] Cluster K3s operativo
- [x] Certificati verificati e validi
- [x] Porta 6443 accessibile
- [x] Kubeconfig preparato per accesso remoto

---

## 📥 1. Download Lens

**Lens Desktop** - Kubernetes IDE (gratuito)
- **URL**: https://k8slens.dev/
- **Versioni disponibili**: Windows, macOS, Linux

**Installazione**:
```bash
# macOS
brew install --cask lens

# Windows
winget install Lens.Lens

# Linux (Ubuntu/Debian)
wget https://api.k8slens.dev/binaries/Lens-latest.amd64.deb
sudo dpkg -i Lens-latest.amd64.deb
```

---

## 🔑 2. Ottieni il Kubeconfig

Il file kubeconfig è già stato preparato per Lens con l'IP corretto del server.

**Percorso file**: `/home/mpasqui/k3s-lens-config.yaml`

**Copia il file sul tuo computer locale**:

```bash
# Da un altro computer sulla stessa rete, esegui:
scp mpasqui@192.168.1.114:~/k3s-lens-config.yaml ~/Downloads/

# Oppure copia manualmente il contenuto del file
cat ~/k3s-lens-config.yaml
```

**Contenuto del kubeconfig**:
- **Server**: `https://192.168.1.114:6443`
- **Certificati**: Embedded (certificate-authority-data, client-certificate-data, client-key-data)
- **Contesto**: `default`
- **Utente**: `system:admin` (full admin access)

---

## 🔧 3. Configura Lens

### Passo 1: Apri Lens
Avvia Lens Desktop sul tuo computer.

### Passo 2: Aggiungi Cluster
1. Click su **"+"** (Add Cluster) in alto a sinistra
2. Seleziona **"Add cluster from kubeconfig"**

### Passo 3: Importa Kubeconfig
**Opzione A - Incolla contenuto**:
1. Copia il contenuto di `k3s-lens-config.yaml`
2. Incollalo nel campo di testo in Lens
3. Click su **"Add cluster(s)"**

**Opzione B - Seleziona file**:
1. Click su **"Browse"**
2. Seleziona il file `k3s-lens-config.yaml` scaricato
3. Click su **"Add cluster(s)"**

### Passo 4: Verifica Connessione
Se tutto è configurato correttamente, Lens mostrerà:
- ✅ **Nome cluster**: `default`
- ✅ **Server**: `192.168.1.114:6443`
- ✅ **Stato**: 🟢 Connected
- ✅ **Node**: `insightlearn-k3s`
- ✅ **Kubernetes Version**: `v1.33.5+k3s1`

---

## 🔍 4. Verifica Cluster in Lens

Dopo la connessione, dovresti vedere:

### Namespaces
- `default`
- `kube-system`
- `kube-public`
- `kube-node-lease`
- `insightlearn` ← **Namespace principale applicazione**

### Workloads (namespace: insightlearn)
- **Grafana**: Dashboard monitoring
- **Prometheus**: Metrics collection
- **Redis**: Cache
- **MongoDB**: Video storage
- **SQL Server**: Database relazionale
- **Ollama**: AI LLM server
- **API**: Backend InsightLearn
- **Web**: Frontend Blazor WASM

### Nodes
- `insightlearn-k3s` (192.168.1.114) - **Ready**
  - OS: Rocky Linux 10.1
  - Kernel: 6.12.0-124.16.1
  - Container Runtime: containerd 2.1.4

---

## 🛠️ 5. Troubleshooting

### Errore: "Unable to connect to the server"

**Causa**: Firewall blocca porta 6443

**Soluzione**:
```bash
# Sul server K3s, verifica firewall
sudo firewall-cmd --list-all

# Se porta 6443 non è aperta, aggiungi regola
sudo firewall-cmd --permanent --add-port=6443/tcp
sudo firewall-cmd --reload
```

---

### Errore: "x509: certificate signed by unknown authority"

**Causa**: Lens non riconosce il certificato K3s self-signed

**Soluzione**: Il kubeconfig preparato include già `certificate-authority-data` embedded, quindi questo errore NON dovrebbe apparire. Se appare:

```bash
# Verifica che il kubeconfig contenga certificate-authority-data
grep -A 2 "certificate-authority-data:" ~/k3s-lens-config.yaml

# Dovrebbe mostrare una stringa base64 lunga
```

---

### Errore: "Unauthorized"

**Causa**: Credenziali client certificate scadute o invalide

**Soluzione**: Rigenera il kubeconfig
```bash
# Sul server K3s
kubectl config view --raw > ~/k3s-lens-config-new.yaml
sed -i 's|https://127.0.0.1:6443|https://192.168.1.114:6443|g' ~/k3s-lens-config-new.yaml

# Reimporta in Lens il nuovo file
```

---

### Lens non mostra i pod

**Causa**: Permessi RBAC insufficienti

**Verifica**:
```bash
# Sul server K3s, verifica ruoli utente
kubectl auth can-i --list

# Dovrebbe mostrare "*" per tutte le risorse (full admin)
```

---

## 📊 6. Features di Lens per InsightLearn

### Monitoring
- **CPU/Memory Usage** per pod
- **Network Traffic** in real-time
- **Pod Logs** con filtering e search
- **Events** del cluster

### Management
- **Restart Pods**: Click destro → Restart
- **Delete Pods**: Click destro → Delete
- **Port Forwarding**: Accesso diretto ai pod (es. Redis, MongoDB)
- **Shell Access**: Exec bash/sh nei container

### Debugging
- **Describe Resources**: YAML completo di deployment, services, etc.
- **Logs Streaming**: Tail logs in real-time
- **Events Timeline**: Cronologia eventi del cluster
- **Resource Metrics**: CPU, RAM, Storage usage

---

## 🔐 7. Informazioni Certificati

**CA Certificate**:
- **Issuer**: CN=k3s-server-ca@1763030719
- **Subject**: O=k3s, CN=k3s
- **Validità**: 10 anni (expires 2035-11-11)

**Client Certificate**:
- **Subject**: O=system:masters, CN=system:admin
- **Validità**: 1 anno (expires 2026-11-13)

**Nota**: Se il client certificate scade, rigenerarlo con:
```bash
sudo k3s kubectl config view --raw > ~/k3s-lens-config.yaml
```

---

## 📋 Checklist Finale

Prima di chiudere, verifica:

- [ ] Lens mostra lo stato del cluster come "Connected" (🟢)
- [ ] Vedi almeno 12 pod nel cluster
- [ ] I pod in namespace `insightlearn` sono tutti "Running"
- [ ] Puoi aprire i logs di un pod (es. Grafana)
- [ ] Puoi eseguire port-forward (es. Grafana → localhost:3000)

---

## 📞 Supporto

**Documentazione K3s**: https://docs.k3s.io/
**Documentazione Lens**: https://docs.k8slens.dev/

**Cluster Info**:
- **Endpoint API**: https://192.168.1.114:6443
- **Node Name**: insightlearn-k3s
- **Kubernetes Version**: v1.33.5+k3s1
- **Container Runtime**: containerd 2.1.4-k3s1

**InsightLearn Application**:
- **API**: http://localhost:31081 (NodePort)
- **Frontend**: https://www.insightlearn.cloud
- **Grafana**: http://localhost:3000 (port-forward)
- **Prometheus**: http://localhost:9091 (NodePort)

---

**✅ Setup Completato!** 🎉

Ora puoi gestire il cluster K3s InsightLearn direttamente da Lens con una GUI professionale.
