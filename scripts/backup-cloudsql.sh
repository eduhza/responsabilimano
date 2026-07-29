#!/usr/bin/env bash
#
# backup-cloudsql.sh — Exporta o banco Cloud SQL PostgreSQL e faz upload para GCS.
#
# Requisitos:
#   - gcloud CLI autenticado com acesso ao projeto responsabilimano
#   - Bucket GCS já provisionado: gs://responsabilimano-backups/
#     (criar com: gsutil mb -l us-central1 gs://responsabilimano-backups/)
#   - O bucket NÃO deve ser público. Restringir acesso via IAM.
#
# Uso:
#   ./scripts/backup-cloudsql.sh
#
# Agendamento (Cloud Scheduler ou cron):
#   Ver docs/deploy-manual.md → seção "Backup do banco".
#
set -euo pipefail

# ── Configuração ──────────────────────────────────────────────
PROJECT_ID="responsabilimano"
REGION="us-central1"
INSTANCE_NAME="responsabilimano-db"
DATABASE_NAME="responsabilimano"
BUCKET_URL="gs://responsabilimano-backups"
RETENTION_DAYS=30
# ───────────────────────────────────────────────────────────────

TIMESTAMP=$(date +"%Y%m%d-%H%M%S")
EXPORT_FILE="responsabilimano-${TIMESTAMP}.sql"
GCS_URI="${BUCKET_URL}/${EXPORT_FILE}"

echo "=== Backup Cloud SQL ==="
echo "Projeto:   ${PROJECT_ID}"
echo "Instância: ${INSTANCE_NAME}"
echo "Banco:     ${DATABASE_NAME}"
echo "Destino:   ${GCS_URI}"
echo "Timestamp: ${TIMESTAMP}"
echo ""

# 1. Exportar o banco para o GCS via gcloud sql export sql
echo "[1/3] Exportando banco para ${GCS_URI} ..."
gcloud sql export sql "${INSTANCE_NAME}" "${GCS_URI}" \
  --project="${PROJECT_ID}" \
  --database="${DATABASE_NAME}" \
  --quiet

echo "[1/3] Exportação concluída."

# 2. Verificar que o arquivo existe no bucket
echo "[2/3] Verificando arquivo no GCS ..."
if ! gsutil ls "${GCS_URI}" >/dev/null 2>&1; then
  echo "ERRO: arquivo ${GCS_URI} não encontrado após exportação." >&2
  exit 1
fi
echo "[2/3] Arquivo confirmado no GCS."

# 3. Limpeza de backups antigos (opcional — mantém apenas RETENTION_DAYS dias)
echo "[3/3] Removendo backups com mais de ${RETENTION_DAYS} dias ..."
gsutil -m find -a "${BUCKET_URL}" -type f \
  -name "responsabilimano-*.sql" \
  -condition "age > ${RETENTION_DAYS}d" \
  -exec gsutil -m rm {} \; 2>/dev/null || true

# Alternativa: usar lifecycle do bucket para auto-expiração:
#   gsutil lifecycle set lifecycle.json gs://responsabilimano-backups/
# Onde lifecycle.json contém uma regra Delete com Age = 30.
# Ver: https://cloud.google.com/storage/docs/lifecycle

echo ""
echo "=== Backup concluído com sucesso ==="
echo "Arquivo: ${GCS_URI}"
