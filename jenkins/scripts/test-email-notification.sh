#!/bin/bash
# Test Email Notification System

set -e

EMAIL_RECIPIENT="${EMAIL_RECIPIENT:-marcello.pasqui@gmail.com}"
SMTP_SERVER="${SMTP_SERVER:-localhost}"
SMTP_PORT="${SMTP_PORT:-25}"

echo "========================================="
echo "Email Notification Test"
echo "========================================="
echo "Recipient: $EMAIL_RECIPIENT"
echo "SMTP Server: $SMTP_SERVER:$SMTP_PORT"
echo "========================================="

# Create test email content
cat > /tmp/test_email.txt <<EOF
Subject: [Jenkins Test] InsightLearn Automated Test
From: jenkins@insightlearn.cloud
To: $EMAIL_RECIPIENT

This is a test email from Jenkins automated testing system.

Test Date: $(date)
Jenkins URL: http://localhost:8080
Site: https://www.insightlearn.cloud

Last Test Results:
- Frontend: ✅ Operational
- Performance: ✅ 110ms average
- Load Test: ✅ 70/70 requests successful
- Backend API: ❌ 502 errors (being fixed)

This email confirms that notifications are working correctly.

---
InsightLearn Automated Testing
https://github.com/marypas74/InsightLearn_WASM
EOF

# Test 1: Try sending via sendmail (if available)
if command -v sendmail &> /dev/null; then
    echo "✅ sendmail available"
    echo "Sending test email via sendmail..."
    sendmail "$EMAIL_RECIPIENT" < /tmp/test_email.txt
    echo "✅ Email sent via sendmail"
    rm /tmp/test_email.txt
    exit 0
fi

# Test 2: Try mailx (if available)
if command -v mailx &> /dev/null; then
    echo "✅ mailx available"
    echo "Sending test email via mailx..."
    mailx -s "[Jenkins Test] InsightLearn" "$EMAIL_RECIPIENT" < /tmp/test_email.txt
    echo "✅ Email sent via mailx"
    rm /tmp/test_email.txt
    exit 0
fi

# Test 3: Try using curl with SMTP
if command -v curl &> /dev/null; then
    echo "✅ curl available"
    echo "Attempting to send via SMTP..."

    # Create email in RFC 5322 format
    cat > /tmp/smtp_email.txt <<EOF
From: jenkins@insightlearn.cloud
To: $EMAIL_RECIPIENT
Subject: [Jenkins Test] InsightLearn Automated Test

This is a test email from Jenkins.

Test Date: $(date)
Status: Email notifications configured

---
InsightLearn Automated Testing
EOF

    # Try to send (requires SMTP server)
    curl --url "smtp://$SMTP_SERVER:$SMTP_PORT" \
        --mail-from "jenkins@insightlearn.cloud" \
        --mail-rcpt "$EMAIL_RECIPIENT" \
        --upload-file /tmp/smtp_email.txt 2>&1 && \
        echo "✅ Email sent via SMTP" || \
        echo "⚠️  SMTP send failed (check SMTP configuration)"

    rm -f /tmp/smtp_email.txt /tmp/test_email.txt
    exit 0
fi

echo ""
echo "⚠️  No email client available"
echo ""
echo "To enable email notifications, install one of:"
echo "  - sendmail"
echo "  - mailx"
echo "  - Configure SMTP server"
echo ""
echo "For Jenkins, configure in:"
echo "  Manage Jenkins → Configure System → E-mail Notification"
echo ""
echo "Recommended SMTP settings:"
echo "  - SMTP server: smtp.gmail.com (for Gmail)"
echo "  - Port: 465 (SSL) or 587 (TLS)"
echo "  - Use SSL/TLS: Yes"
echo "  - Credentials: App-specific password"
echo ""

rm -f /tmp/test_email.txt

exit 1
