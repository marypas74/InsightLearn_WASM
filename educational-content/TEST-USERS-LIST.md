# InsightLearn Test Users

**Password Comune**: `Pa$$W0rd`
**Hash ASP.NET Identity**: `AQAAAAIAAYagAAAAEJl0wPWsA/Iny8sPnTLB47r0zVJAwH8s7AnR2KG72PhtyeYfRMLfpRZoA9lj/COZYQ==`

---

## 👨‍🏫 Instructors (10 users)

| # | Email | Nome | Cognome | Role |
|---|-------|------|---------|------|
| 1 | john.smith@instructors.insightlearn.cloud | John | Smith | Instructor |
| 2 | maria.garcia@instructors.insightlearn.cloud | Maria | Garcia | Instructor |
| 3 | david.chen@instructors.insightlearn.cloud | David | Chen | Instructor |
| 4 | sarah.johnson@instructors.insightlearn.cloud | Sarah | Johnson | Instructor |
| 5 | ahmed.hassan@instructors.insightlearn.cloud | Ahmed | Hassan | Instructor |
| 6 | emily.brown@instructors.insightlearn.cloud | Emily | Brown | Instructor |
| 7 | carlos.rodriguez@instructors.insightlearn.cloud | Carlos | Rodriguez | Instructor |
| 8 | yuki.tanaka@instructors.insightlearn.cloud | Yuki | Tanaka | Instructor |
| 9 | sophia.mueller@instructors.insightlearn.cloud | Sophia | Mueller | Instructor |
| 10 | raj.patel@instructors.insightlearn.cloud | Raj | Patel | Instructor |

---

## 👨‍🎓 Students (100 users)

| # | Email | Nome | Cognome | Role |
|---|-------|------|---------|------|
| 1-100 | student1@students.insightlearn.cloud | Student | User1 | Student |
|  | student2@students.insightlearn.cloud | Student | User2 | Student |
|  | student3@students.insightlearn.cloud | Student | User3 | Student |
|  | ... | ... | ... | ... |
|  | student100@students.insightlearn.cloud | Student | User100 | Student |

---

## 🔐 Admin User

| Email | Nome | Cognome | Role |
|-------|------|---------|------|
| admin@insightlearn.cloud | Admin | User | Admin |

---

## 📝 Note

- **Tutti gli utenti** hanno la stessa password: `Pa$$W0rd`
- Le email seguono il pattern:
  - Instructors: `[firstname].[lastname]@instructors.insightlearn.cloud`
  - Students: `student[N]@students.insightlearn.cloud`
  - Admin: `admin@insightlearn.cloud`
- UserType: 1 = Student, 2 = Instructor, 3 = Admin
- Tutti gli utenti hanno `EmailConfirmed = 1` (verificati)
- Gli account sono pronti per il testing

---

## 🧪 Test Scenarios

### Login Tests
```bash
# Instructor Login
Email: john.smith@instructors.insightlearn.cloud
Password: Pa$$W0rd

# Student Login
Email: student1@students.insightlearn.cloud
Password: Pa$$W0rd

# Admin Login
Email: admin@insightlearn.cloud
Password: Pa$$W0rd
```

### Stress Test
- 100 studenti concorrenti
- 10 instructors con corsi multipli
- Enrollment bulk (1000+ enrollment)
- Review generation (5000+ reviews)

---

Generated: 2025-11-20
