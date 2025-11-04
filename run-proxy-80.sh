#!/bin/bash
# Run kubectl port-forward on port 80
export KUBECONFIG=/home/mpasqui/.kube/config
export PATH=/home/mpasqui/.local/bin:/usr/local/bin:/usr/bin:/bin

echo "Starting kubectl port-forward on port 80..."
/home/mpasqui/.local/bin/kubectl port-forward -n insightlearn --address 0.0.0.0 service/api-service 80:80
