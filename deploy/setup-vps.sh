#!/usr/bin/env bash
# ==========================================================================
#  ERPSystem — إعداد خادم Ubuntu VPS من الصفر
#  Full one-shot provisioning: .NET API + PostgreSQL (shared) + React web
#  + Nginx + HTTPS (Let's Encrypt) + firewall.
#
#  الاستخدام / Usage (على الخادم كـ root):
#     sudo bash deploy/setup-vps.sh
#
#  السكربت آمن لإعادة التشغيل (idempotent): يمكن تشغيله أكثر من مرة.
# ==========================================================================
set -Eeuo pipefail

# ---------- ألوان ونقاط تسجيل ----------
c_g="\033[1;32m"; c_y="\033[1;33m"; c_r="\033[1;31m"; c_b="\033[1;34m"; c_0="\033[0m"
log()  { echo -e "${c_g}[+]${c_0} $*"; }
warn() { echo -e "${c_y}[!]${c_0} $*"; }
err()  { echo -e "${c_r}[x]${c_0} $*" >&2; }
step() { echo -e "\n${c_b}==== $* ====${c_0}"; }
trap 'err "فشل عند السطر $LINENO. راجع الخطأ أعلاه."' ERR

# ---------- المسارات ----------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="${SCRIPT_DIR}/.env"
APP_ROOT="/opt/erpsystem"
SRC_DIR="${APP_ROOT}/src"
API_DIR="${APP_ROOT}/api"

# ---------- تحققات أولية ----------
[[ $EUID -eq 0 ]] || { err "شغّل السكربت بصلاحية root:  sudo bash $0"; exit 1; }
[[ -f "$ENV_FILE" ]] || { err "لا يوجد ملف الإعدادات. نفّذ:  cp deploy/.env.example deploy/.env  ثم عدّله."; exit 1; }

# shellcheck disable=SC1090
source "$ENV_FILE"

: "${DOMAIN:?DOMAIN مطلوب في .env}"
: "${LETSENCRYPT_EMAIL:?LETSENCRYPT_EMAIL مطلوب}"
: "${DB_NAME:?}"; : "${DB_APP_USER:?}"; : "${DB_APP_PASSWORD:?}"
: "${REPO_URL:?}"; : "${API_PORT:=5218}"; : "${DB_PORT:=5432}"; : "${WEB_LISTEN_PORT:=80}"
: "${SERVICE_USER:=erpapi}"; : "${REPO_BRANCH:=main}"
: "${ENABLE_REMOTE_DB:=yes}"; : "${BUILD_WEB_CLIENT:=yes}"
: "${DB_REMOTE_ALLOWED_CIDRS:=0.0.0.0/0}"; : "${MANAGE_SSL:=yes}"
WEB_ROOT="/var/www/${DOMAIN}"

# توليد سر JWT إن لم يوجد وحفظه في .env
if [[ -z "${JWT_SECRET:-}" ]]; then
  JWT_SECRET="$(openssl rand -base64 48 | tr -d '\n')"
  if grep -q '^JWT_SECRET=' "$ENV_FILE"; then
    sed -i "s|^JWT_SECRET=.*|JWT_SECRET=\"${JWT_SECRET//|/\\|}\"|" "$ENV_FILE"
  else
    echo "JWT_SECRET=\"${JWT_SECRET}\"" >> "$ENV_FILE"
  fi
  log "تم توليد JWT_SECRET وحفظه في .env"
fi

# ==========================================================================
step "1) تحديث النظام وتثبيت الأدوات الأساسية"
# ==========================================================================
export DEBIAN_FRONTEND=noninteractive
apt-get update -y
apt-get install -y ca-certificates curl gnupg lsb-release software-properties-common \
                   git ufw openssl rsync

# ==========================================================================
step "2) تثبيت .NET SDK 9 (لبناء الـ API)"
# ==========================================================================
DOTNET_INSTALL_DIR="/usr/share/dotnet"
have_net9() { command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -q '^9\.'; }

if have_net9; then
  log ".NET 9 موجود مسبقاً: $(dotnet --version)"
else
  # المحاولة 1: apt عبر خلاصة Microsoft
  UB_VER="$(lsb_release -rs)"
  curl -fsSL "https://packages.microsoft.com/config/ubuntu/${UB_VER}/packages-microsoft-prod.deb" -o /tmp/ms-prod.deb 2>/dev/null && dpkg -i /tmp/ms-prod.deb >/dev/null 2>&1 || true
  apt-get update -y >/dev/null 2>&1 || true
  apt-get install -y dotnet-sdk-9.0 >/dev/null 2>&1 || true

  # المحاولة 2 (احتياطي): سكربت Microsoft الرسمي — لا يعتمد على apt
  if ! have_net9; then
    warn "حزمة apt لـ .NET 9 غير متوفرة — التثبيت عبر السكربت الرسمي (side-by-side مع .NET 10)..."
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    bash /tmp/dotnet-install.sh --channel 9.0 --install-dir "$DOTNET_INSTALL_DIR"
    ln -sf "${DOTNET_INSTALL_DIR}/dotnet" /usr/local/bin/dotnet
    export DOTNET_ROOT="$DOTNET_INSTALL_DIR"
    export PATH="$PATH:$DOTNET_INSTALL_DIR"
  fi

  have_net9 && log ".NET 9 مُثبّت: $(dotnet --version)" || { err ".NET 9 لم يُثبّت. راجع الأخطاء أعلاه."; exit 1; }
fi

# ضمان أن الخدمة والـ EF يجدان dotnet بغض النظر عن مصدر التثبيت
export DOTNET_ROOT="${DOTNET_ROOT:-$DOTNET_INSTALL_DIR}"
DOTNET_BIN="$(command -v dotnet)"

# ==========================================================================
step "3) تثبيت Node.js 20 (لبناء web-client)"
# ==========================================================================
if [[ "$BUILD_WEB_CLIENT" == "yes" ]]; then
  # الأدوات (Vite/Workbox/react-router) تتطلب Node 20+ (Node 18 يفشل بـ "crypto is not defined")
  if ! command -v node >/dev/null 2>&1 || [[ "$(node -v | sed 's/v\([0-9]*\).*/\1/')" -lt 20 ]]; then
    warn "تثبيت/ترقية Node.js إلى الإصدار 20..."
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
    apt-get install -y nodejs
  fi
  log "Node: $(node -v)"
fi

# ==========================================================================
step "4) تثبيت PostgreSQL + Nginx + Certbot"
# ==========================================================================
apt-get install -y postgresql postgresql-contrib nginx certbot python3-certbot-nginx
systemctl enable --now postgresql
systemctl enable --now nginx

# ==========================================================================
step "5) تهيئة قاعدة البيانات المشتركة (PostgreSQL)"
# ==========================================================================
PG_HBA="$(sudo -u postgres psql -tAc 'SHOW hba_file;')"
PG_CONF="$(sudo -u postgres psql -tAc 'SHOW config_file;')"
log "config: $PG_CONF"

# إنشاء مستخدم التطبيق وقاعدة البيانات (idempotent)
sudo -u postgres psql -v ON_ERROR_STOP=1 <<SQL
DO \$\$
BEGIN
   IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '${DB_APP_USER}') THEN
      CREATE ROLE ${DB_APP_USER} LOGIN PASSWORD '${DB_APP_PASSWORD}';
   ELSE
      ALTER ROLE ${DB_APP_USER} WITH LOGIN PASSWORD '${DB_APP_PASSWORD}';
   END IF;
END
\$\$;
SQL

if ! sudo -u postgres psql -tAc "SELECT 1 FROM pg_database WHERE datname='${DB_NAME}'" | grep -q 1; then
  sudo -u postgres createdb -O "${DB_APP_USER}" "${DB_NAME}"
  log "أُنشئت قاعدة البيانات ${DB_NAME}"
else
  sudo -u postgres psql -c "ALTER DATABASE ${DB_NAME} OWNER TO ${DB_APP_USER};" >/dev/null
  log "قاعدة البيانات ${DB_NAME} موجودة"
fi
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE ${DB_NAME} TO ${DB_APP_USER};" >/dev/null

# فرض scram-sha-256 لكلمات المرور
if ! grep -q "^password_encryption *= *scram-sha-256" "$PG_CONF"; then
  sed -i "s/^#\?password_encryption.*/password_encryption = scram-sha-256/" "$PG_CONF"
fi
# تفعيل SSL (شهادة snakeoil مثبتة مع Ubuntu)
sed -i "s/^#\?ssl *=.*/ssl = on/" "$PG_CONF"

if [[ "$ENABLE_REMOTE_DB" == "yes" ]]; then
  step "   5-b) فتح قاعدة البيانات لتطبيق سطح المكتب عن بُعد (SSL)"
  sed -i "s/^#\?listen_addresses.*/listen_addresses = '*'/" "$PG_CONF"
  # أضف قواعد الوصول عن بُعد إلى pg_hba.conf (مرة واحدة)
  if ! grep -q "ERPSystem remote desktop" "$PG_HBA"; then
    {
      echo ""
      echo "# ERPSystem remote desktop (managed by setup-vps.sh)"
      for cidr in $DB_REMOTE_ALLOWED_CIDRS; do
        echo "hostssl ${DB_NAME} ${DB_APP_USER} ${cidr} scram-sha-256"
      done
    } >> "$PG_HBA"
    log "أُضيفت قواعد الوصول عن بُعد لـ: ${DB_REMOTE_ALLOWED_CIDRS}"
  fi
  [[ "$DB_REMOTE_ALLOWED_CIDRS" == "0.0.0.0/0" ]] && \
    warn "قاعدة البيانات مفتوحة لكل العناوين (0.0.0.0/0). يُنصح بحصرها بـ IP مكتبك في .env"
fi
systemctl restart postgresql

# ==========================================================================
step "6) جلب الشيفرة وبناء المشروع"
# ==========================================================================
id -u "$SERVICE_USER" >/dev/null 2>&1 || useradd --system --create-home --shell /usr/sbin/nologin "$SERVICE_USER"
mkdir -p "$APP_ROOT"

# ملكية المجلد قد تكون للمستخدم erpapi (بعد نشر سابق) بينما git يعمل كـ root → تفادي خطأ dubious ownership
git config --global --add safe.directory "$SRC_DIR" 2>/dev/null || true

if [[ -d "$SRC_DIR/.git" ]]; then
  log "تحديث المستودع..."
  git -C "$SRC_DIR" fetch --all --prune
  git -C "$SRC_DIR" checkout "$REPO_BRANCH"
  git -C "$SRC_DIR" reset --hard "origin/${REPO_BRANCH}"
else
  log "استنساخ المستودع..."
  git clone --branch "$REPO_BRANCH" "$REPO_URL" "$SRC_DIR"
fi

log "ملاحظة: الترحيلات تُطبَّق تلقائياً عند إقلاع الـ API (Database.MigrateAsync)."

step "   6-b) نشر الـ API (dotnet publish)"
dotnet publish "${SRC_DIR}/ERPSystem.Api/ERPSystem.Api.csproj" \
  -c Release -o "$API_DIR" /p:UseAppHost=false
chown -R "$SERVICE_USER":"$SERVICE_USER" "$APP_ROOT"

if [[ "$BUILD_WEB_CLIENT" == "yes" ]]; then
  step "   6-c) بناء web-client (Vite)"
  pushd "${SRC_DIR}/web-client" >/dev/null
  export VITE_API_BASE_URL="https://${DOMAIN}/api"
  npm ci
  npm run build
  mkdir -p "$WEB_ROOT"
  rsync -a --delete dist/ "$WEB_ROOT/"
  chown -R www-data:www-data "$WEB_ROOT"
  popd >/dev/null
  log "تم نشر الواجهة إلى ${WEB_ROOT}"
fi

# ==========================================================================
step "7) خدمة systemd للـ API"
# ==========================================================================
CONN_STR="Host=127.0.0.1;Port=${DB_PORT};Database=${DB_NAME};Username=${DB_APP_USER};Password=${DB_APP_PASSWORD}"
CORS_ORIGINS="https://${DOMAIN}"
[[ -n "${WWW_DOMAIN:-}" ]] && CORS_ORIGINS="${CORS_ORIGINS},https://${WWW_DOMAIN}"

cat > /etc/systemd/system/erpsystem-api.service <<UNIT
[Unit]
Description=ERPSystem API
After=network.target postgresql.service
Wants=postgresql.service

[Service]
# simple: لا يعتمد على إشارة sd_notify (التطبيق لا يرسلها افتراضياً)
Type=simple
User=${SERVICE_USER}
WorkingDirectory=${API_DIR}
ExecStart=${DOTNET_BIN:-/usr/bin/dotnet} ${API_DIR}/ERPSystem.Api.dll
Restart=always
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier=erpsystem-api
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:${API_PORT}
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=DOTNET_ROOT=${DOTNET_ROOT:-/usr/share/dotnet}
Environment=DOTNET_ROLL_FORWARD=Major
Environment=ConnectionStrings__DefaultConnection=${CONN_STR}
Environment=JWT_SECRET=${JWT_SECRET}
Environment=Cors__AllowedOrigins=${CORS_ORIGINS}

[Install]
WantedBy=multi-user.target
UNIT

systemctl daemon-reload
systemctl enable erpsystem-api
systemctl restart erpsystem-api
sleep 4
if systemctl is-active --quiet erpsystem-api; then
  log "الـ API يعمل على 127.0.0.1:${API_PORT}"
else
  err "فشل تشغيل الـ API. راجع: journalctl -u erpsystem-api -n 50"
fi

# ==========================================================================
step "8) إعداد Nginx (واجهة + بروكسي للـ API)"
# ==========================================================================
# كتلة مواقع مشتركة (نفس المحتوى لـ HTTP و HTTPS)
read -r -d '' NGINX_LOCATIONS <<LOC || true
    root ${WEB_ROOT};
    index index.html;
    client_max_body_size 50M;

    # بروكسي الـ API:  /api/... -> http://127.0.0.1:${API_PORT}/...
    location /api/ {
        proxy_pass http://127.0.0.1:${API_PORT}/;
        proxy_http_version 1.1;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_read_timeout 300s;
    }

    location = /health { proxy_pass http://127.0.0.1:${API_PORT}/health; }

    # توجيه SPA (React Router)
    location / {
        try_files \$uri \$uri/ /index.html;
    }
LOC

CERT_DIR="/etc/letsencrypt/live/${DOMAIN}"
if [[ "$MANAGE_SSL" == "yes" ]]; then
  # HTTP على 80؛ سيضيف certbot كتلة 443 في الخطوة 9
  cat > "/etc/nginx/sites-available/${DOMAIN}" <<NGINX
server {
    listen 80;
    listen [::]:80;
    server_name ${DOMAIN} ${WWW_DOMAIN:-};

${NGINX_LOCATIONS}
}
NGINX
elif [[ -f "${CERT_DIR}/fullchain.pem" && "$WEB_LISTEN_PORT" == "443" ]]; then
  # شهادة محلية موجودة ونريد TLS مباشرة من Nginx على 443
  log "استخدام الشهادة الموجودة: ${CERT_DIR}"
  cat > "/etc/nginx/sites-available/${DOMAIN}" <<NGINX
server {
    listen 80;
    listen [::]:80;
    server_name ${DOMAIN} ${WWW_DOMAIN:-};
    return 301 https://\$host\$request_uri;
}
server {
    listen 443 ssl;
    listen [::]:443 ssl;
    http2 on;
    server_name ${DOMAIN} ${WWW_DOMAIN:-};

    ssl_certificate     ${CERT_DIR}/fullchain.pem;
    ssl_certificate_key ${CERT_DIR}/privkey.pem;

${NGINX_LOCATIONS}
}
NGINX
else
  # طبقة SSL خارجية (Cloudflare/بروكسي/لوحة استضافة) تُنهي TLS وتوجّه إلى منفذ HTTP داخلي
  log "Nginx يستمع على المنفذ ${WEB_LISTEN_PORT} (طبقة SSL خارجية تدير الدومين)."
  cat > "/etc/nginx/sites-available/${DOMAIN}" <<NGINX
server {
    listen ${WEB_LISTEN_PORT};
    listen [::]:${WEB_LISTEN_PORT};
    server_name ${DOMAIN} ${WWW_DOMAIN:-} _;

${NGINX_LOCATIONS}
}
NGINX
fi

ln -sf "/etc/nginx/sites-available/${DOMAIN}" "/etc/nginx/sites-enabled/${DOMAIN}"
[[ -f /etc/nginx/sites-enabled/default ]] && rm -f /etc/nginx/sites-enabled/default
nginx -t && systemctl reload nginx

# ==========================================================================
step "9) شهادة HTTPS (Let's Encrypt)"
# ==========================================================================
if [[ "$MANAGE_SSL" == "no" ]]; then
  log "MANAGE_SSL=no → تخطّي إصدار الشهادة (الشهادة مُدارة مسبقاً)."
else
  CERTBOT_DOMAINS=(-d "$DOMAIN")
  [[ -n "${WWW_DOMAIN:-}" ]] && CERTBOT_DOMAINS+=(-d "$WWW_DOMAIN")
  if certbot --nginx "${CERTBOT_DOMAINS[@]}" --non-interactive --agree-tos -m "$LETSENCRYPT_EMAIL" --redirect; then
    log "تم إصدار شهادة HTTPS وتفعيل التحويل التلقائي"
  else
    warn "تعذّر إصدار الشهادة تلقائياً — تأكد أن الدومين ${DOMAIN} يشير إلى IP الخادم (سجل A) ثم أعد:"
    warn "  certbot --nginx ${CERTBOT_DOMAINS[*]} -m ${LETSENCRYPT_EMAIL} --agree-tos --redirect"
  fi
fi

# ==========================================================================
step "10) جدار الحماية (ufw)"
# ==========================================================================
ufw allow OpenSSH >/dev/null 2>&1 || ufw allow 22/tcp
ufw allow 'Nginx Full' >/dev/null 2>&1 || { ufw allow 80/tcp; ufw allow 443/tcp; }
# فتح منفذ Nginx الخارجي المخصّص (إن كان غير 80/443)
if [[ "$WEB_LISTEN_PORT" != "80" && "$WEB_LISTEN_PORT" != "443" ]]; then
  ufw allow "${WEB_LISTEN_PORT}/tcp"
fi
if [[ "$ENABLE_REMOTE_DB" == "yes" ]]; then
  if [[ "$DB_REMOTE_ALLOWED_CIDRS" == "0.0.0.0/0" ]]; then
    ufw allow "${DB_PORT}/tcp"
  else
    for cidr in $DB_REMOTE_ALLOWED_CIDRS; do
      ufw allow from "$cidr" to any port "$DB_PORT" proto tcp
    done
  fi
fi
ufw --force enable
ufw status verbose || true

# ==========================================================================
step "تم! ملخص النشر"
# ==========================================================================
PUBLIC_IP="$(curl -fsSL https://api.ipify.org 2>/dev/null || echo 'IP-الخادم')"
echo -e "${c_g}"
cat <<SUMMARY
========================================================================
 ✅ اكتمل النشر
------------------------------------------------------------------------
 الواجهة   : https://${DOMAIN}   (Nginx يستمع على المنفذ ${WEB_LISTEN_PORT})
 الـ API   : https://${DOMAIN}/api   (داخلياً 127.0.0.1:${API_PORT})
 الصحة     : https://${DOMAIN}/health
 توجيه الطبقة الخارجية:  alamal-ab.org  →  ${PUBLIC_IP}:${WEB_LISTEN_PORT}

 قاعدة البيانات (لتطبيق سطح المكتب — Connection String):
   Host=${DOMAIN};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_APP_USER};Password=<كلمة المرور>;SSL Mode=Require;Trust Server Certificate=true

 أوامر مفيدة:
   حالة الـ API   : systemctl status erpsystem-api
   سجل الـ API    : journalctl -u erpsystem-api -f
   تحديث التطبيق  : sudo bash deploy/deploy-app.sh
========================================================================
SUMMARY
echo -e "${c_0}"
[[ "$ENABLE_REMOTE_DB" == "yes" && "$DB_REMOTE_ALLOWED_CIDRS" == "0.0.0.0/0" ]] && \
  warn "أمان: قاعدة البيانات مفتوحة للجميع. حدّد DB_REMOTE_ALLOWED_CIDRS بعنوان مكتبك ثم أعد تشغيل السكربت."
