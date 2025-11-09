#!/bin/bash

# Test Login per tutti gli utenti InsightLearn
# Verifica autenticazione e ruoli

API_URL="http://localhost:31081"
CLOUDFLARE_URL="https://wasm.insightlearn.cloud"

echo "========================================"
echo "   TEST LOGIN INSIGHTLEARN PLATFORM    "
echo "========================================"
echo ""

# Test 1: Admin Login
echo "1️⃣  Testing ADMIN login..."
cat > /tmp/admin-login.json << 'EOF'
{
  "Email": "admin@insightlearn.cloud",
  "Password": "Admin@InsightLearn2025!",
  "RememberMe": true
}
EOF

ADMIN_RESULT=$(curl -X POST $API_URL/api/auth/login \
  -H "Content-Type: application/json" \
  -d @/tmp/admin-login.json \
  -s | jq '{IsSuccess, Email: .User.Email, Roles: .User.Roles}')

echo "$ADMIN_RESULT"
echo ""

# Test 2: Teacher Login
echo "2️⃣  Testing TEACHER login..."
cat > /tmp/teacher-login.json << 'EOF'
{
  "Email": "teacher@insightlearn.cloud",
  "Password": "Teacher@123!",
  "RememberMe": true
}
EOF

TEACHER_RESULT=$(curl -X POST $API_URL/api/auth/login \
  -H "Content-Type: application/json" \
  -d @/tmp/teacher-login.json \
  -s | jq '{IsSuccess, Email: .User.Email, Roles: .User.Roles, IsInstructor: .User.IsInstructor}')

echo "$TEACHER_RESULT"
echo ""

# Test 3: Student Login
echo "3️⃣  Testing STUDENT login..."
cat > /tmp/student-login.json << 'EOF'
{
  "Email": "student@insightlearn.cloud",
  "Password": "Student@123!",
  "RememberMe": true
}
EOF

STUDENT_RESULT=$(curl -X POST $API_URL/api/auth/login \
  -H "Content-Type: application/json" \
  -d @/tmp/student-login.json \
  -s | jq '{IsSuccess, Email: .User.Email, Roles: .User.Roles, IsInstructor: .User.IsInstructor}')

echo "$STUDENT_RESULT"
echo ""

# Summary
echo "========================================"
echo "             SUMMARY                    "
echo "========================================"
echo ""
echo "✅ Admin:   $(echo $ADMIN_RESULT | jq -r '.IsSuccess')"
echo "✅ Teacher: $(echo $TEACHER_RESULT | jq -r '.IsSuccess')"
echo "✅ Student: $(echo $STUDENT_RESULT | jq -r '.IsSuccess')"
echo ""
echo "All login tests completed!"
echo ""

# Cleanup
rm -f /tmp/admin-login.json /tmp/teacher-login.json /tmp/student-login.json
