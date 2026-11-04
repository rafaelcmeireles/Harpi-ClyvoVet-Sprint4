# 1. DevOps Tools & Cloud Computing — ClyvoVet

Documentação da infraestrutura para o Challenge FIAP 2026 (Clyvo Vet).

# 1.1 Arquitetura do Deploy

```
┌─────────────────────────────────────────────────────────┐
│                    Azure Cloud                          │
│                                                         │
│   ┌───────────────────────────────────────────────┐     │
│   │        VM Linux (Ubuntu 22.04 LTS)            │     │
│   │        Standard_B2s — eastus                  │     │
│   │                                               │     │
│   │   ┌─────────────────────────────────────┐     │     │
│   │   │         Docker Engine               │     │     │
│   │   │                                     │     │     │
│   │   │   ┌───────────┐   ┌─────────────┐   │     │     │
│   │   │   │  API .NET │   │  Oracle XE  │   │     │     │
│   │   │   │  (porta   │─▶│  21-slim    |    |     |     | 
│   │   │   │   8080)   │   │ (porta 1521 │   │     │     │
│   │   │   └───────────┘   └──────┬──────┘   │     │     │
│   │   │                          │          │     │     │
│   │   │                    ┌─────▼─────┐    │     │     │
│   │   │                    │  Volume   │    │     │     │
│   │   │                    │ oracle-   │    │     │     │
│   │   │                    │  data     │    │     │     │
│   │   │                    └───────────┘    │     │     │
│   │   └─────────────────────────────────────┘     │     │
│   └───────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────┘
```

# 1.2 Tecnologias

| Tecnologia | Versão | Finalidade |
|------------|--------|------------|
| Docker | Latest | Containerização da aplicação |
| Docker Compose | v2 | Orquestração dos containers |
| Azure CLI | Latest | Provisionamento da infraestrutura |
| Ubuntu | 22.04 LTS | Sistema operacional da VM |
| Oracle XE | 21-slim | Banco de dados em container |

# 1.3 Arquivos Criados

| Arquivo | Descrição |
|---------|-----------|
| `Dockerfile` | Multi-stage build (SDK → Runtime) da API .NET |
| `.dockerignore` | Exclui arquivos desnecessários do build |
| `docker-compose.yml` | Orquestração: API + Oracle + Volume + Network |
| `azure/provision.sh` | Script Azure CLI para criar a VM |
| `azure/deploy.sh` | Script para deploy dos containers na VM |

---

# 1.4 Passo a Passo

# 1.4.1 Passo 1 - Rodar Localmente (Docker)

```bash
# Na pasta ClyvoVetApi/
docker compose up -d --build

# Aguardar o Oracle inicializar (~2 min na primeira vez)
docker compose logs -f oracle-db

# Acessar o Swagger
# http://localhost:8080
```

# 1.4.1.1 Comandos úteis:

```bash
# Ver status dos containers
docker ps

# Ver logs da API
docker compose logs -f api

# Parar tudo
docker compose down

# Parar e remover volumes (APAGA os dados!)
docker compose down -v
```

# 1.4.2 Passo 2 - Provisionar a VM na Azure

**Pré-requisito:** Azure CLI instalado e logado (`az login`).

```bash
# Executar o script de provisionamento
bash azure/provision.sh
```

O script executa os seguintes passos automaticamente:
1. ✅ Cria o Resource Group `rg-clyvovet`
2. ✅ Cria a VM `vm-clyvovet` (Ubuntu 22.04, Standard_B2s)
3. ✅ Abre as portas 8080 (API) e 1521 (Oracle)
4. ✅ Instala Docker e Docker Compose na VM
5. ✅ Exibe o IP público da VM

# 1.4.3 Passo 3 - Deploy na VM Azure

```bash
# Usar o IP retornado pelo provision.sh
bash azure/deploy.sh <IP_DA_VM>
```

O script executa:
1. ✅ Copia todos os arquivos do projeto via SCP
2. ✅ Faz o build e sobe os containers com `docker compose up -d --build`
3. ✅ Verifica o status dos containers

# 1.4.4 Passo 4 - Acessar a API na Nuvem

```
Swagger: http://<IP_DA_VM>:8080
SSH:     ssh azureuser@<IP_DA_VM>
```

---

# 1.5 Critérios Atendidos

| Critério | Pts | Status | Evidência |
|----------|-----|--------|-----------|
| Provisionamento Azure CLI (VM Linux) | 25 | ✅ | `azure/provision.sh` — cria VM Ubuntu 22.04 |
| Conteinerização Docker (App + Banco) | 25 | ✅ | `Dockerfile` + `docker-compose.yml` (2 serviços) |
| Persistência com volumes | 25 | ✅ | Volume `oracle-data` no `docker-compose.yml` |
| Deploy e execução em nuvem | 25 | ✅ | `azure/deploy.sh` — sobe containers na VM |
| **TOTAL** | **100** | **✅** | |

---

# 1.6 Detalhes Técnicos

# 1.6.1 Dockerfile (Multi-stage Build)
- **Stage 1 (build):** Usa `dotnet/sdk:8.0` para compilar em modo Release
- **Stage 2 (runtime):** Usa `dotnet/aspnet:8.0` (imagem leve, ~220MB)
- Cache de camadas: copia `.csproj` e faz `restore` antes do código

# 1.6.2 docker-compose.yml
- **oracle-db:** Imagem `gvenzl/oracle-xe:21-slim` (Oracle XE gratuito)
  - Healthcheck com `healthcheck.sh` (aguarda o Oracle iniciar)
  - Volume nomeado `oracle-data` para persistência
- **api:** Build do Dockerfile, porta 8080
  - `depends_on` com `condition: service_healthy` (só inicia após o Oracle estar pronto)
  - Connection string via variável de ambiente (override do `appsettings.json`)

# 1.6.3 Volume de Persistência
```yaml
volumes:
  oracle-data:
    name: clyvovet-oracle-data
```
Os dados do Oracle são armazenados em `/opt/oracle/oradata` dentro do container, mapeados para o volume nomeado `clyvovet-oracle-data`. Mesmo que o container seja destruído (`docker compose down`), os dados permanecem. Para apagar os dados, use `docker compose down -v`.

---

# 1.7 Integrantes do Grupo

Ana Clara de Oliveira Nascimento - RM 561957 | Isis Macedo - RM 561497 | Henrique Pereira - RM 565608 | Pedro Mariutti - RM 75999 | Rafael Carvalho Meireles - RM 563413