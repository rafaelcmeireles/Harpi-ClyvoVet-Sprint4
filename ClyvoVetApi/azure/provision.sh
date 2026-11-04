#!/bin/bash
# =============================================
# Script de Provisionamento - Azure CLI
# ClyvoVet API - Challenge FIAP 2026
# =============================================
# Pre-requisito: ter o Azure CLI instalado e logado (az login)
# Uso: bash provision.sh
# =============================================

# ---------- Variaveis ----------
RESOURCE_GROUP="rg-clyvovet"
LOCATION="brazilsouth"
VM_NAME="vm-clyvovet"
VM_IMAGE="Ubuntu2204"
VM_SIZE="Standard_B2s_v2"
ADMIN_USER="rafaelcmeireles"

echo "=========================================="
echo "  ClyvoVet - Provisionamento Azure"
echo "=========================================="

# ---------- 1. Criar Resource Group ----------
echo ""
echo "[1/5] Criando Resource Group: $RESOURCE_GROUP..."
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# ---------- 2. Criar a VM Linux ----------
echo ""
echo "[2/5] Criando VM Linux: $VM_NAME (Ubuntu 22.04 LTS)..."
az vm create \
  --resource-group $RESOURCE_GROUP \
  --name $VM_NAME \
  --image $VM_IMAGE \
  --size $VM_SIZE \
  --admin-username $ADMIN_USER \
  --generate-ssh-keys \
  --public-ip-sku Standard \
  --output table

# ---------- 3. Abrir porta 8080 (API) e 1521 (Oracle) ----------
echo ""
echo "[3/5] Abrindo porta 8080 (API) e 1521 (Oracle)..."
az vm open-port \
  --resource-group $RESOURCE_GROUP \
  --name $VM_NAME \
  --port 8080 \
  --priority 1010

az vm open-port \
  --resource-group $RESOURCE_GROUP \
  --name $VM_NAME \
  --port 1521 \
  --priority 1020

# ---------- 4. Instalar Docker e Docker Compose na VM ----------
echo ""
echo "[4/5] Instalando Docker e Docker Compose na VM..."
az vm run-command invoke \
  --resource-group $RESOURCE_GROUP \
  --name $VM_NAME \
  --command-id RunShellScript \
  --scripts "sudo apt-get update -y && sudo apt-get install -y ca-certificates curl gnupg && sudo install -m 0755 -d /etc/apt/keyrings && curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg && sudo chmod a+r /etc/apt/keyrings/docker.gpg && echo 'deb [arch=amd64 signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu jammy stable' | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null && sudo apt-get update -y && sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin && sudo usermod -aG docker $ADMIN_USER && docker --version && docker compose version"

# ---------- 5. Obter IP publico da VM ----------
echo ""
echo "[5/5] Obtendo IP publico da VM..."
VM_IP=$(az vm show \
  --resource-group $RESOURCE_GROUP \
  --name $VM_NAME \
  --show-details \
  --query publicIps \
  --output tsv)

echo ""
echo "=========================================="
echo "  Provisionamento concluido!"
echo "=========================================="
echo "  VM:       $VM_NAME"
echo "  IP:       $VM_IP"
echo "  Usuario:  $ADMIN_USER"
echo "  SSH:      ssh $ADMIN_USER@$VM_IP"
echo "  API:      http://$VM_IP:8080"
echo "=========================================="
echo ""
echo "Proximo passo: execute 'bash deploy.sh $VM_IP'"
