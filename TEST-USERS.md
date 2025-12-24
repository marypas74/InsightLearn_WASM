# Utenti di Test - InsightLearn Platform

**Data Creazione**: 2025-11-09
**Database**: InsightLearnDb (SQL Server)
**Status**: ✅ Tutti verificati e funzionanti

---

## 👥 Utenti Disponibili

### 1️⃣ **ADMIN** (Amministratore Sistema)
```
Email:    admin@insightlearn.cloud
Password: Admin@InsightLearn2025!
Ruolo:    Admin
```

**Privilegi**:
- ✅ Accesso completo alla piattaforma
- ✅ Gestione utenti
- ✅ Gestione corsi e categorie
- ✅ Configurazioni sistema
- ✅ Visualizzazione analytics
- ⚠️ Dashboard admin **NON implementata** (solo placeholder)

---

### 2️⃣ **TEACHER** (Insegnante/Istruttore)
```
Email:    teacher@insightlearn.cloud
Password: Teacher@123!
Ruolo:    Instructor
Nome:     Maria Rossi
```

**Privilegi**:
- ✅ Creazione e gestione corsi propri
- ✅ Gestione studenti iscritti ai propri corsi
- ✅ Caricamento materiali didattici
- ✅ Flag `IsInstructor = true`
- ⚠️ Funzionalità instructor **NON implementate**

---

### 3️⃣ **STUDENT** (Studente)
```
Email:    student@insightlearn.cloud
Password: Student@123!
Ruolo:    Student
Nome:     Luca Bianchi
```

**Privilegi**:
- ✅ Visualizzazione catalogo corsi
- ✅ Iscrizione a corsi
- ✅ Accesso materiali didattici
- ✅ Tracciamento progressi
- ✅ Flag `IsInstructor = false`

---

## 🧪 Test Login

### Metodo 1: Script Automatico
```bash
./test-all-logins.sh
```

### Metodo 2: Test Manuale (curl)

**Admin Login**:
```bash
curl -X POST http://localhost:31081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "Email": "admin@insightlearn.cloud",
    "Password": "Admin@InsightLearn2025!",
    "RememberMe": true
  }' | jq
```

**Teacher Login**:
```bash
curl -X POST http://localhost:31081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "Email": "teacher@insightlearn.cloud",
    "Password": "Teacher@123!",
    "RememberMe": true
  }' | jq
```

**Student Login**:
```bash
curl -X POST http://localhost:31081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "Email": "student@insightlearn.cloud",
    "Password": "Student@123!",
    "RememberMe": true
  }' | jq
```

### Metodo 3: Via Cloudflare Tunnel
```bash
curl -X POST https://www.insightlearn.cloud/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "Email": "admin@insightlearn.cloud",
    "Password": "Admin@InsightLearn2025!",
    "RememberMe": true
  }' | jq
```

---

## 📊 Verifica Database

### Controlla tutti gli utenti:
```bash
kubectl exec -n insightlearn statefulset/sqlserver -- \
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa \
  -P 'InsightLearn123@#' -C \
  -Q "USE InsightLearnDb;
      SELECT Email, FirstName + ' ' + LastName as FullName,
             IsInstructor, EmailConfirmed
      FROM Users ORDER BY Email;"
```

### Controlla ruoli assegnati:
```bash
kubectl exec -n insightlearn statefulset/sqlserver -- \
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa \
  -P 'InsightLearn123@#' -C \
  -Q "USE InsightLearnDb;
      SELECT u.Email, r.Name as Role
      FROM Users u
      JOIN UserRoles ur ON u.Id = ur.UserId
      JOIN Roles r ON ur.RoleId = r.Id
      ORDER BY u.Email;"
```

---

## ⚠️ Note Importanti

1. **Dashboard Admin**: Solo placeholder - nessuna funzionalità implementata
2. **Instructor Features**: Endpoint CRUD corsi non implementati
3. **Student Dashboard**: Funzionalità base presente ma limitata
4. **JWT Token**: Scadenza 7 giorni (configurabile in `.env`)
5. **Password Policy**:
   - Minimo 6 caratteri
   - Almeno 1 maiuscola
   - Almeno 1 minuscola
   - Almeno 1 numero
   - Caratteri speciali opzionali

---

## 🔐 Sicurezza

- ❌ **NON utilizzare queste credenziali in produzione**
- ✅ Password con caratteri speciali supportati correttamente
- ✅ JWT tokens firmati con chiave segreta
- ✅ Refresh token implementato (`/api/auth/refresh`)
- ✅ Lockout account dopo 5 tentativi falliti (15 minuti)

---

## 📝 Modifiche Future

Per cambiare password o creare nuovi utenti:

### Via API (Raccomandato):
```bash
curl -X POST http://localhost:31081/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "Email": "new.user@example.com",
    "Password": "NewPassword123!",
    "ConfirmPassword": "NewPassword123!",
    "FirstName": "Nome",
    "LastName": "Cognome",
    "AgreeToTerms": true
  }' | jq
```

### Via Database (Solo sviluppo):
```sql
-- Cambiare ruolo utente
UPDATE UserRoles SET RoleId = (SELECT Id FROM Roles WHERE Name = 'Instructor')
WHERE UserId = (SELECT Id FROM Users WHERE Email = 'user@example.com');

-- Aggiornare flag IsInstructor
UPDATE Users SET IsInstructor = 1
WHERE Email = 'user@example.com';
```

---

**Last Updated**: 2025-11-09 01:20 CET
**API Version**: 1.6.0-dev
**Test Status**: ✅ All 3 users verified
