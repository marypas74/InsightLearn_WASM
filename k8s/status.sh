#!/bin/bash
# Script to check InsightLearn deployment status

echo "=== InsightLearn Kubernetes Status ==="
echo ""

# Check if namespace exists
if kubectl get namespace insightlearn &> /dev/null; then
    echo "Namespace: ✓ insightlearn exists"
else
    echo "Namespace: ✗ insightlearn not found"
    exit 1
fi

echo ""
echo "=== Pods ==="
kubectl get pods -n insightlearn -o wide

echo ""
echo "=== Services ==="
kubectl get services -n insightlearn

echo ""
echo "=== Ingress ==="
kubectl get ingress -n insightlearn

echo ""
echo "=== PersistentVolumeClaims ==="
kubectl get pvc -n insightlearn

echo ""
echo "=== HorizontalPodAutoscalers ==="
kubectl get hpa -n insightlearn

echo ""
echo "=== Recent Events ==="
kubectl get events -n insightlearn --sort-by='.lastTimestamp' | tail -20

echo ""
echo "=== Access Information ==="
echo "Minikube IP: $(minikube ip 2>/dev/null || echo 'minikube not running')"
echo "Add to /etc/hosts: $(minikube ip 2>/dev/null || echo '<minikube-ip>') insightlearn.local"
echo "Web URL: http://insightlearn.local"
echo ""
echo "Or use port-forward:"
echo "  kubectl port-forward -n insightlearn service/web-service 8080:80"
echo "  http://localhost:8080"
