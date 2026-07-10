#!/usr/bin/env bash
set -euo pipefail
BACKUP_DIR="/opt/erpsystem/backups/phase3-verification"
mkdir -p "$BACKUP_DIR"
chown postgres:postgres "$BACKUP_DIR"
chmod 750 "$BACKUP_DIR"
TS="$(date -u +%Y%m%dT%H%M%SZ)"
FILE="${BACKUP_DIR}/erp_pro_pre_phase3_${TS}.dump"
sudo -u postgres pg_dump -d erp_pro -F c -f "$FILE"
ls -lh "$FILE"
echo "BACKUP_FILE=$FILE"
echo "BACKUP_SIZE_BYTES=$(stat -c%s "$FILE")"
sudo -u postgres pg_restore --list "$FILE" | head -5
echo "PG_RESTORE_LIST_OK=1"
echo "PG_VERSION=$(sudo -u postgres psql -tAc 'SELECT version();' | head -1)"
GIT_COMMIT="$(git -C /opt/erpsystem/src rev-parse HEAD 2>/dev/null || echo unknown)"
echo "GIT_COMMIT=$GIT_COMMIT"
