#!/bin/bash

# InsightLearn - Jenkins CI/CD Deployment Script
# Professional automated testing infrastructure
# Usage: ./k8s/deploy-jenkins.sh

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Banner
echo -e "${PURPLE}╔═══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${PURPLE}║         InsightLearn Jenkins CI/CD Deployment                 ║${NC}"
echo -e "${PURPLE}║         Professional Testing Infrastructure v1.0.0           ║${NC}"
echo -e "${PURPLE}╚═══════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Functions
log_info() {
    echo -e "${BLUE}ℹ${NC}  $1"
}

log_success() {
    echo -e "${GREEN}✓${NC}  $1"
}

log_warning() {
    echo -e "${YELLOW}⚠${NC}  $1"
}

log_error() {
    echo -e "${RED}✗${NC}  $1"
}

log_step() {
    echo ""
    echo -e "${CYAN}═══ $1 ═══${NC}"
}

# Step 1: Pre-flight checks
log_step "Pre-flight Checks"

if ! command -v kubectl &> /dev/null; then
    log_error "kubectl not found. Please install kubectl first."
    exit 1
fi
log_success "kubectl found"

if ! kubectl cluster-info &> /dev/null; then
    log_error "Cannot connect to Kubernetes cluster"
    exit 1
fi
log_success "Kubernetes cluster accessible"

# Step 2: Create Jenkins namespace
log_step "Creating Jenkins Namespace"

if kubectl get namespace jenkins &> /dev/null; then
    log_warning "Namespace 'jenkins' already exists"
else
    kubectl apply -f k8s/12-jenkins-namespace.yaml
    log_success "Jenkins namespace created"
fi

# Step 3: Setup RBAC
log_step "Configuring RBAC"

kubectl apply -f k8s/13-jenkins-rbac.yaml
log_success "ServiceAccount, ClusterRole, and ClusterRoleBinding created"

# Step 4: Create Persistent Volume Claim
log_step "Creating Persistent Storage"

if kubectl get pvc jenkins-pvc -n jenkins &> /dev/null; then
    log_warning "PVC 'jenkins-pvc' already exists"
else
    kubectl apply -f k8s/14-jenkins-pvc.yaml
    log_success "Persistent Volume Claim created"
fi

# Wait for PVC to be bound
log_info "Waiting for PVC to be bound..."
kubectl wait --for=jsonpath='{.status.phase}'=Bound pvc/jenkins-pvc -n jenkins --timeout=60s
log_success "PVC is bound"

# Step 5: Deploy Jenkins
log_step "Deploying Jenkins"

kubectl apply -f k8s/15-jenkins-deployment.yaml
log_success "Jenkins deployment and services created"

# Step 6: Wait for Jenkins to be ready
log_step "Waiting for Jenkins to be Ready"

log_info "Waiting for Jenkins pod to be created..."
sleep 5

POD_NAME=$(kubectl get pod -l app=jenkins -n jenkins -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || echo "")

if [ -z "$POD_NAME" ]; then
    log_error "Jenkins pod not found. Deployment may have failed."
    log_info "Check deployment status: kubectl get deployment -n jenkins"
    log_info "Check events: kubectl get events -n jenkins --sort-by='.lastTimestamp'"
    exit 1
fi

log_info "Jenkins pod found: $POD_NAME"
log_info "Waiting for Jenkins to be ready (this may take 2-3 minutes)..."

kubectl wait --for=condition=ready pod -l app=jenkins -n jenkins --timeout=300s
log_success "Jenkins is ready!"

# Step 7: Get access information
log_step "Access Information"

NODEPORT=$(kubectl get svc jenkins -n jenkins -o jsonpath='{.spec.ports[0].nodePort}')
MINIKUBE_IP=$(minikube ip 2>/dev/null || echo "192.168.49.2")

echo ""
echo -e "${GREEN}╔═══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║  ✅ Jenkins Deployment Successful!                            ║${NC}"
echo -e "${GREEN}╚═══════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${CYAN}📍 Access URLs:${NC}"
echo -e "   ${BLUE}➜${NC} Jenkins UI:  http://${MINIKUBE_IP}:${NODEPORT}"
echo -e "   ${BLUE}➜${NC} Dashboard:   http://${MINIKUBE_IP}:${NODEPORT}/blue"
echo ""

# Get initial admin password
log_step "Retrieving Initial Admin Password"

log_info "Waiting for initial admin password file..."
sleep 10

ADMIN_PASSWORD=$(kubectl exec -n jenkins deployment/jenkins -- cat /var/jenkins_home/secrets/initialAdminPassword 2>/dev/null || echo "")

if [ -z "$ADMIN_PASSWORD" ]; then
    log_warning "Could not retrieve password automatically"
    echo ""
    echo -e "${YELLOW}To get the password manually, run:${NC}"
    echo -e "${CYAN}kubectl exec -n jenkins deployment/jenkins -- cat /var/jenkins_home/secrets/initialAdminPassword${NC}"
else
    echo ""
    echo -e "${GREEN}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${GREEN}  🔑 Initial Admin Password:${NC}"
    echo -e "${YELLOW}     $ADMIN_PASSWORD${NC}"
    echo -e "${GREEN}═══════════════════════════════════════════════════════════════${NC}"
fi

echo ""
echo -e "${CYAN}📚 Next Steps:${NC}"
echo ""
echo -e "  ${BLUE}1.${NC} Open Jenkins UI at: http://${MINIKUBE_IP}:${NODEPORT}"
echo -e "  ${BLUE}2.${NC} Login with the admin password above"
echo -e "  ${BLUE}3.${NC} Install recommended plugins:"
echo -e "     - Kubernetes Plugin"
echo -e "     - Docker Pipeline"
echo -e "     - GitHub Integration"
echo -e "     - JUnit Plugin"
echo -e "     - HTML Publisher"
echo -e "  ${BLUE}4.${NC} Create a new Pipeline:"
echo -e "     - New Item → Pipeline"
echo -e "     - Name: InsightLearn-CI-CD"
echo -e "     - Pipeline script from SCM"
echo -e "     - Script Path: Jenkinsfile"
echo -e "  ${BLUE}5.${NC} Configure Kubernetes Cloud:"
echo -e "     - Manage Jenkins → Manage Nodes and Clouds → Configure Clouds"
echo -e "     - Add Kubernetes"
echo -e "     - Kubernetes URL: https://kubernetes.default"
echo -e "     - Kubernetes Namespace: jenkins"
echo -e "     - Jenkins URL: http://jenkins:8080"
echo ""
echo -e "${CYAN}📖 Documentation:${NC}"
echo -e "   ${BLUE}➜${NC} Full guide: JENKINS-TESTING-GUIDE.md"
echo -e "   ${BLUE}➜${NC} Pipeline:   Jenkinsfile"
echo ""
echo -e "${CYAN}🔍 Useful Commands:${NC}"
echo -e "   ${BLUE}➜${NC} View logs:     ${CYAN}kubectl logs -n jenkins deployment/jenkins${NC}"
echo -e "   ${BLUE}➜${NC} Get pods:      ${CYAN}kubectl get pods -n jenkins${NC}"
echo -e "   ${BLUE}➜${NC} Port forward:  ${CYAN}kubectl port-forward -n jenkins svc/jenkins 8080:8080${NC}"
echo -e "   ${BLUE}➜${NC} Delete:        ${CYAN}kubectl delete namespace jenkins${NC}"
echo ""
echo -e "${GREEN}Happy Testing! 🚀${NC}"
echo ""
