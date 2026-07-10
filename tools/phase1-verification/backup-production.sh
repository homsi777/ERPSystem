#!/usr/bin/env bash
set -euo pipefail
BACKUP_DIR="/opt/erpsystem/backups/phase1-verification"
mkdir -p "$BACKUP_DIR"
chown postgres:postgres "$BACKUP_DIR"
chmod 750 "$BACKUP_DIR"
TS="$(date -u +%Y%m%dT%H%M%SZ)"
FILE="${BACKUP_DIR}/erp_pro_phase1_gate_${TS}.dump"
sudo -u postgres pg_dump -d erp_pro -F c -f "$FILE"
ls -lh "$FILE"
echo "BACKUP_FILE=$FILE"
echo "BACKUP_SIZE_BYTES=$(stat -c%s "$FILE")"
echo "PG_VERSION=$(sudo -u postgres psql -tAc 'SELECT version();' | head -1)"
