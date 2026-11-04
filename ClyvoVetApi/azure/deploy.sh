#!/bin/bash
# =============================================
# Script de Deploy — ClyvoVet API
# Challenge FIAP 2026
# =============================================
# Uso: bash deploy.sh <IP_DA_VM>
# Exemplo: bash deploy.sh 20.121.45.67
# =============================================

# ---------- Variáveis ----------
VM_IP=$1
ADMIN_USER="azureuser"
REMOTE_DIR="/home/$ADMIN_USER/clyvovet"

if [ -z "$VM_IP" ]; then
  echo "❌ Erro: informe o IP da VM."
  echo "Uso: bash deploy.sh <IP_DA_VM>"
  exit 1
fi

echo "=========================================="
echo "  ClyvoVet — Deploy na VM Azure"
echo "  IP: $VM_IP"
echo "=========================================="

# ---------- 1. Criar diretório remoto ----------
echo ""
echo "[1/4] Criando diretório remoto..."
ssh $ADMIN_USER@$VM_IP "mkdir -p $REMOTE_DIR"

# ---------- 2. Copiar arquivos para a VM ----------
echo ""
echo "[2/4] Copiando arquivos do projeto para a VM..."
# Execute este script a partir da RAIZ da solucao (pasta com ClyvoVetApi.sln).
# O build usa ClyvoVetApi/Dockerfile com contexto na raiz (4 projetos: Api, Application, Domain, Infrastructure).
scp -r \
  docker-compose.yml \
  .dockerignore \
  ClyvoVetApi \
  ClyvoVetApi.Application \
  ClyvoVetApi.Domain \
  ClyvoVetApi.Infrastructure \
  $ADMIN_USER@$VM_IP:$REMOTE_DIR/

# ---------- 3. Subir os containers ----------
echo ""
echo "[3/4] Construindo e subindo os containers na VM..."
ssh $ADMIN_USER@$VM_IP "cd $REMOTE_DIR && docker compose up -d --build"

# ---------- 4. Verificar status ----------
echo ""
echo "[4/4] Verificando status dos containers..."
ssh $ADMIN_USER@$VM_IP "docker ps"

echo ""
echo "=========================================="
echo "  ✅ Deploy concluído!"
echo "=========================================="
echo "  Swagger: http://$VM_IP:8080"
echo "  Oracle:  $VM_IP:1521"
echo ""
echo "  Comandos úteis:"
echo "    ssh $ADMIN_USER@$VM_IP"
echo "    docker compose logs -f"
echo "    docker compose down"
echo "=========================================="
