#!/usr/bin/env bash
# ==========================================================================
#  ERPSystem — تحديث التطبيق فقط (بدون إعادة إعداد الخادم)
#  يسحب آخر شيفرة، يعيد البناء والنشر، ويعيد تشغيل الخدمات.
#     sudo bash deploy/deploy-app.sh
# ==========================================================================
set -Eeuo pipefail
c_g="\033[1;32m"; c_r="\033[1;31m"; c_0="\033[0m"
log(){ echo -e "${c_g}[+]${c_0} $*"; }
err(){ echo -e "${c_r}[x]${c_0} $*" >&2; }
trap 'err "فشل عند السطر $LINENO."' ERR

[[ $EUID -eq 0 ]] || { err "شغّل بصلاحية root: sudo bash $0"; exit 1; }
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1090
source "${SCRIPT_DIR}/.env"
: "${DOMAIN:?}"; : "${REPO_BRANCH:=main}"; : "${SERVICE_USER:=erpapi}"
: "${BUILD_WEB_CLIENT:=yes}"
APP_ROOT="/opt/erpsystem"; SRC_DIR="${APP_ROOT}/src"; API_DIR="${APP_ROOT}/api"
WEB_ROOT="/var/www/${DOMAIN}"

log "سحب آخر شيفرة (${REPO_BRANCH})"
git -C "$SRC_DIR" fetch --all --prune
git -C "$SRC_DIR" reset --hard "origin/${REPO_BRANCH}"

log "نشر الـ API"
dotnet publish "${SRC_DIR}/ERPSystem.Api/ERPSystem.Api.csproj" -c Release -o "$API_DIR" /p:UseAppHost=false
chown -R "$SERVICE_USER":"$SERVICE_USER" "$API_DIR"

if [[ "$BUILD_WEB_CLIENT" == "yes" ]]; then
  log "بناء web-client"
  # Root shells often lack nvm/npm on PATH — load common Node installs.
  if ! command -v npm >/dev/null 2>&1; then
    export NVM_DIR="${NVM_DIR:-/home/ubuntu/.nvm}"
    # shellcheck disable=SC1091
    [[ -s "$NVM_DIR/nvm.sh" ]] && source "$NVM_DIR/nvm.sh"
  fi
  if ! command -v npm >/dev/null 2>&1; then
    for npm_bin in /home/ubuntu/.nvm/versions/node/*/bin; do
      [[ -x "$npm_bin/npm" ]] || continue
      export PATH="$npm_bin:$PATH"
      break
    done
  fi
  command -v npm >/dev/null 2>&1 || { err "npm غير موجود على PATH — ثبّت Node أو عطّل BUILD_WEB_CLIENT"; exit 1; }
  pushd "${SRC_DIR}/web-client" >/dev/null
  export VITE_API_BASE_URL="https://${DOMAIN}"
  npm ci
  npm run build
  rsync -a --delete dist/ "$WEB_ROOT/"
  chown -R www-data:www-data "$WEB_ROOT"
  popd >/dev/null
fi

log "إعادة تشغيل الخدمات"
systemctl restart erpsystem-api
systemctl reload nginx
sleep 3
systemctl is-active --quiet erpsystem-api && log "الـ API يعمل ✅" || err "الـ API متوقف — journalctl -u erpsystem-api -n 50"
log "تم التحديث."
