#!/bin/bash
# Generate Cryptographically Secure JWT Secret
# Usage: ./scripts/generate-jwt-secret.sh

set -e

echo "=========================================="
echo "  JWT Secret Key Generator"
echo "=========================================="
echo ""
echo "Generating cryptographically secure JWT secret (64 characters base64)..."
echo ""

# Generate 64-byte random data and encode as base64, then remove newlines
JWT_SECRET=$(openssl rand -base64 64 | tr -d '\n')

echo "Generated JWT Secret:"
echo "----------------------------------------"
echo "$JWT_SECRET"
echo "----------------------------------------"
echo ""
echo "Secret length: ${#JWT_SECRET} characters (minimum required: 32)"
echo ""
echo "To use this secret:"
echo ""
echo "1. For Kubernetes deployment:"
echo "   Update k8s/01-secrets.yaml:"
echo "   jwt-secret-key: \"$JWT_SECRET\""
echo ""
echo "2. For Docker Compose (.env file):"
echo "   JWT_SECRET_KEY=$JWT_SECRET"
echo ""
echo "3. For local development (appsettings.json):"
echo "   \"Jwt\": { \"Secret\": \"$JWT_SECRET\" }"
echo ""
echo "SECURITY WARNING:"
echo "- NEVER commit secrets to git"
echo "- NEVER share secrets in plain text communication"
echo "- Store secrets in secure secret management systems"
echo "- Rotate secrets periodically (see docs/JWT-SECRET-ROTATION.md)"
echo ""
