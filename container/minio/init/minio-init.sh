#!/bin/sh
# MinIO init (BusyBox sh-kompatibel)
# - Buckets (private)
# - Least-privilege policy: ListAllMyBuckets, ListBucket, Get/Put, Deny Delete
# - Servicebruger (fra .env eller autogenereret)
# - Logger til stdout + /logs/

set -eu

ts() { date +"%Y%m%d-%H%M%S"; }

LOG_DIR="/logs"
LOG="$LOG_DIR/minio-init-$(ts).log"
mkdir -p "$LOG_DIR"

# ---- tee stdout/stderr til log uden bash process substitution ----
FIFO="/tmp/minio_init_fifo"
rm -f "$FIFO"
mkfifo "$FIFO"
tee -a "$LOG" < "$FIFO" &
TEE_PID=$!
exec > "$FIFO" 2>&1
cleanup() {
  exec > /dev/null 2>&1 || true
  wait "$TEE_PID" 2>/dev/null || true
  rm -f "$FIFO"
}
trap cleanup EXIT

# ---- helpers ----
trim() {
  # trim for/efterstillede mellemrum
  local s="$1"
  while [ "${s# }" != "$s" ]; do s=${s# }; done
  while [ "${s% }" != "$s" ]; do s=${s% }; done
  printf '%s' "$s"
}
require() { eval "v=\${$1:-}"; [ -n "$v" ] || { echo "ERROR: $1 is required"; exit 2; }; }

# ---- env ----
require MINIO_ENDPOINT      # fx http://minio:9000
require MINIO_ROOT_USER
require MINIO_ROOT_PASSWORD

MINIO_BUCKETS="${MINIO_BUCKETS:-originals,derived}"
APP_MEDIA_POLICY="${APP_MEDIA_POLICY:-app-media-policy}"
APP_MEDIA_ACCESS_KEY="${APP_MEDIA_ACCESS_KEY:-}"
APP_MEDIA_SECRET_KEY="${APP_MEDIA_SECRET_KEY:-}"

echo "== MinIO init start: $(date -Is)"
echo "Endpoint : ${MINIO_ENDPOINT}"
echo "Buckets  : ${MINIO_BUCKETS}"
echo "Policy   : ${APP_MEDIA_POLICY}"

# ---- alias ----
mc alias set local "${MINIO_ENDPOINT}" "${MINIO_ROOT_USER}" "${MINIO_ROOT_PASSWORD}"

# ---- buckets (idempotent + private) ----
IFS=,; set -- $MINIO_BUCKETS
BKS="$@"
for b in "$@"; do
  [ -z "${b:-}" ] && continue
  b="$(trim "$b")"
  [ -z "$b" ] && continue
  echo "--> ensure bucket: $b"
  mc mb -p "local/$b" || true
  mc anonymous set none "local/$b" || true
done

# ---- policy (least privilege) ----
LIST_RES=""; OBJ_RES=""
for b in $BKS; do
  b="$(trim "$b")"
  [ -z "$b" ] && continue
  LIST_RES="${LIST_RES}\"arn:aws:s3:::${b}\","
  OBJ_RES="${OBJ_RES}\"arn:aws:s3:::${b}/*\","
done
LIST_RES="${LIST_RES%,}"; OBJ_RES="${OBJ_RES%,}"
[ -z "$LIST_RES" ] && LIST_RES="\"\""  # edge-case: ingen buckets
[ -z "$OBJ_RES" ]  && OBJ_RES="\"\""

cat >/tmp/policy.json <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    { "Sid": "ListAll", "Effect": "Allow",
      "Action": ["s3:ListAllMyBuckets"],
      "Resource": ["*"] },

    { "Sid": "ListBuckets", "Effect": "Allow",
      "Action": ["s3:ListBucket"],
      "Resource": [ ${LIST_RES} ] },

    { "Sid": "RWObjectsNoDelete", "Effect": "Allow",
      "Action": ["s3:GetObject","s3:PutObject"],
      "Resource": [ ${OBJ_RES} ] },

    { "Sid": "DenyDelete", "Effect": "Deny",
      "Action": ["s3:DeleteObject","s3:DeleteObjectVersion"],
      "Resource": [ ${OBJ_RES} ] }
  ]
}
EOF

echo "--> (re)create policy: ${APP_MEDIA_POLICY}"
mc admin policy remove local "${APP_MEDIA_POLICY}" >/dev/null 2>&1 || true
mc admin policy create local "${APP_MEDIA_POLICY}" /tmp/policy.json

# ---- service user (deterministisk: brug .env hvis sat; ellers autogenerér) ----
if [ -z "$APP_MEDIA_ACCESS_KEY" ]; then
  APP_MEDIA_ACCESS_KEY="app-$(head -c 16 /dev/urandom | base64 | tr -dc 'a-z0-9' | head -c 12)"
fi
if [ -z "$APP_MEDIA_SECRET_KEY" ]; then
  APP_MEDIA_SECRET_KEY="$(head -c 64 /dev/urandom | base64 | tr -dc 'A-Za-z0-9._-' | head -c 44)"
fi

echo "--> ensure user: ${APP_MEDIA_ACCESS_KEY}"
mc admin user remove local "${APP_MEDIA_ACCESS_KEY}" >/dev/null 2>&1 || true
mc admin user add    local "${APP_MEDIA_ACCESS_KEY}" "${APP_MEDIA_SECRET_KEY}"
mc admin policy attach local "${APP_MEDIA_POLICY}" --user "${APP_MEDIA_ACCESS_KEY}"

echo
echo "== RESULTAT =="
echo "Buckets:"
for b in $BKS; do
  b="$(trim "$b")"
  [ -n "$b" ] && echo "  - $b (private)"
done
echo "Policy:"
echo "  - ${APP_MEDIA_POLICY} (ListAllMyBuckets, ListBucket, Get/Put; Deny Delete)"
echo "Service user (til API .env):"
echo "  S3_ACCESS_KEY=${APP_MEDIA_ACCESS_KEY}"
echo "  S3_SECRET_KEY=${APP_MEDIA_SECRET_KEY}"
echo
echo "Log gemt i: $LOG"
echo "== MinIO init done: $(date -Is)"
