#!/usr/bin/env bash
set -euo pipefail
# Restore isolated Phase 3 E2E database from a verified pre-Phase-3 backup.
# Usage: sudo bash restore-test-db.sh /opt/erpsystem/backups/phase3-verification/erp_pro_pre_phase3_YYYYMMDD.dump

BACKUP_FILE="${1:-}"
if [[ -z "$BACKUP_FILE" || ! -f "$BACKUP_FILE" ]]; then
  echo "Usage: $0 <verified_backup.dump>"
  exit 1
fi

TEST_DB="erp_pro_phase3_e2e"
echo "Verifying backup TOC..."
sudo -u postgres pg_restore --list "$BACKUP_FILE" >/dev/null
echo "Dropping and recreating $TEST_DB..."
sudo -u postgres dropdb --if-exists "$TEST_DB"
sudo -u postgres createdb -O erp_app "$TEST_DB"
echo "Restoring backup into $TEST_DB..."
sudo -u postgres pg_restore -d "$TEST_DB" --no-owner --role=erp_app "$BACKUP_FILE"
echo "Applying Phase 3 migration on test DB only..."
cd /opt/erpsystem/src
sudo -u erp_app dotnet ef database update --project ERPSystem.Infrastructure/ERPSystem.Infrastructure.csproj \
  --connection "Host=localhost;Port=5432;Database=${TEST_DB};Username=erp_app;Password=${ERP_DB_PASSWORD:-}"
echo "RESTORE_OK=1 DATABASE=$TEST_DB"
