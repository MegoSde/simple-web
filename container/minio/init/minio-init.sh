#!/bin/sh
# MinIO init + verify + POLICY DUMP (BusyBox sh, no sed)

set -eu

ts() { date -u +"%Y%m%dT%H%M%SZ"; }
require() { eval "v=\${$1:-}"; [ -n "$v" ] || { echo "ERROR: $1 is required"; exit 2; }; }
trim() { s="$1"; while [ "${s# }" != "$s" ]; do s=${s# }; done; while [ "${s% }" != "$s" ]; do s=${s% }; done; printf '%s' "$s"; }
user_exists() { mc admin user info root "$1" >/dev/null 2>&1; } # 0=exists

PASS=0; FAIL=0
pass(){ echo "✅ $*"; PASS=$((PASS+1)); }
fail(){ echo "❌ $*"; FAIL=$((FAIL+1)); }

LOG_DIR="/logs"; mkdir -p "$LOG_DIR"

# --- ENV ---
require MINIO_ENDPOINT
require MINIO_ROOT_USER
require MINIO_ROOT_PASSWORD

MINIO_BUCKETS="${MINIO_BUCKETS:-originals,work,thumbnail}"
MINIO_ANON_READ_BUCKETS="${MINIO_ANON_READ_BUCKETS:-}"
APP_MEDIA_POLICY="${APP_MEDIA_POLICY:-app-media-policy}"
APP_MEDIA_ACCESS_KEY="${APP_MEDIA_ACCESS_KEY:-app-$(head -c 16 /dev/urandom | base64 | tr -dc 'a-z0-9' | head -c 12)}"
APP_MEDIA_SECRET_KEY="${APP_MEDIA_SECRET_KEY:-}"           # give hvis user findes og du vil verify
APP_MEDIA_RESET_SECRET="${APP_MEDIA_RESET_SECRET:-false}"  # "true" => slet & genskab user + ny secret

echo "== MinIO init $(date -Is) =="
echo "Endpoint : $MINIO_ENDPOINT"
echo "Buckets  : $MINIO_BUCKETS"
[ -n "$MINIO_ANON_READ_BUCKETS" ] && echo "Anon READ: $MINIO_ANON_READ_BUCKETS" || echo "Anon READ: (none)"
echo "Policy   : $APP_MEDIA_POLICY"
echo "User     : $APP_MEDIA_ACCESS_KEY (reset=${APP_MEDIA_RESET_SECRET})"
echo

# --- admin alias ---
mc alias set root "$MINIO_ENDPOINT" "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD" >/dev/null

# --- ensure buckets (private) ---
IFS=,; set -- $MINIO_BUCKETS
BKS="$@"
for b in "$@"; do
  [ -n "${b:-}" ] || continue
  b="$(trim "$b")"; [ -z "$b" ] && continue
  echo "--> ensure bucket: $b"
  mc mb -p "root/$b" >/dev/null 2>&1 || true
  mc anonymous set none "root/$b" >/dev/null 2>&1 || true
done

# optional anonymous read
if [ -n "$MINIO_ANON_READ_BUCKETS" ]; then
  IFS=,; for pb in $MINIO_ANON_READ_BUCKETS; do
    pb="$(trim "$pb")"; [ -z "$pb" ] && continue
    echo "--> set anonymous READ on: $pb"
    mc anonymous set download "root/$pb" >/dev/null 2>&1 || true
  done
fi

# --- build policy JSON (STRICT) ---
# Forvent: MINIO_BUCKETS="originals,work,thumbnail" (eller dine egne)
# Byg JSON-arrays korrekt: ["arn:aws:s3:::originals","arn:aws:s3:::work",...]
IFS=,; set -- $MINIO_BUCKETS
LIST_JSON=""
OBJ_JSON=""
for b in "$@"; do
  # trim
  bb="$b"; while [ "${bb# }" != "$bb" ]; do bb=${bb# }; done; while [ "${bb% }" != "$bb" ]; do bb=${bb% }; done
  [ -z "$bb" ] && continue
  [ -n "$LIST_JSON" ] && LIST_JSON="$LIST_JSON, "
  LIST_JSON="$LIST_JSON\"arn:aws:s3:::$bb\""
  [ -n "$OBJ_JSON" ] && OBJ_JSON="$OBJ_JSON, "
  OBJ_JSON="$OBJ_JSON\"arn:aws:s3:::$bb/*\""
done

# Skriv policy med korrekte arrays
cat > /tmp/policy.json <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    { "Sid": "ListAll", "Effect": "Allow",
      "Action": ["s3:ListAllMyBuckets"],
      "Resource": ["*"] },

    { "Sid": "BucketMetaAndList", "Effect": "Allow",
      "Action": ["s3:GetBucketLocation","s3:ListBucket","s3:ListBucketMultipartUploads"],
      "Resource": [ $LIST_JSON ] },

    { "Sid": "ObjectRW", "Effect": "Allow",
      "Action": [
        "s3:GetObject","s3:PutObject",
        "s3:AbortMultipartUpload","s3:ListMultipartUploadParts",
        "s3:GetObjectTagging","s3:PutObjectTagging"
      ],
      "Resource": [ $OBJ_JSON ] },

    { "Sid": "DenyDelete", "Effect": "Deny",
      "Action": ["s3:DeleteObject","s3:DeleteObjectVersion"],
      "Resource": [ $OBJ_JSON ] }
  ]
}
EOF

echo "-- Generated ListBucket Resource: [ $LIST_JSON ]"
echo "-- Generated Object   Resource: [ $OBJ_JSON ]"

# --- replace custom policy & attach ---
echo "--> replace policy: $APP_MEDIA_POLICY"
mc admin policy remove root "$APP_MEDIA_POLICY" >/dev/null 2>&1 || true
mc admin policy create root "$APP_MEDIA_POLICY" /tmp/policy.json >/dev/null

# --- service user create/keep ---
EXISTS=1; user_exists "$APP_MEDIA_ACCESS_KEY" || EXISTS=0
if [ "$EXISTS" -eq 1 ] && [ "$APP_MEDIA_RESET_SECRET" = "true" ]; then
  echo "--> reset user (remove & recreate)"
  mc admin user remove root "$APP_MEDIA_ACCESS_KEY" >/dev/null 2>&1 || true
  EXISTS=0
fi
if [ "$EXISTS" -eq 0 ]; then
  [ -n "$APP_MEDIA_SECRET_KEY" ] || APP_MEDIA_SECRET_KEY="$(head -c 64 /dev/urandom | base64 | tr -dc 'A-Za-z0-9._-' | head -c 44)"
  mc admin user add root "$APP_MEDIA_ACCESS_KEY" "$APP_MEDIA_SECRET_KEY"
  pass "user created"
else
  echo "    keeping existing secret"
fi
mc admin policy attach root "$APP_MEDIA_POLICY" --user "$APP_MEDIA_ACCESS_KEY" >/dev/null 2>&1 || true
mc admin user info root "$APP_MEDIA_ACCESS_KEY" || true

# === NEW: POLICY DUMP (for comparison) ===
echo
echo "== POLICY DUMP (for compare) =="

# Forventede ARN-ressourcer (fra MINIO_BUCKETS)
echo "-- Expected bucket ARNs (ListBucket scope) --"
for b in $BKS; do b="$(trim "$b")"; [ -n "$b" ] && echo "  arn:aws:s3:::$b"; done
echo "-- Expected object ARNs (Get/Put scope) --"
for b in $BKS; do b="$(trim "$b")"; [ -n "$b" ] && echo "  arn:aws:s3:::$b/*"; done
echo

# Readwrite policy JSON
READWRITE_JSON="$LOG_DIR/policy-readwrite.json"
CUSTOM_JSON="$LOG_DIR/policy-$APP_MEDIA_POLICY.json"

echo "-- readwrite (built-in) --"
if mc admin policy info root readwrite > "$READWRITE_JSON" 2>/dev/null; then
  echo "Saved to: $READWRITE_JSON"
  echo "----- BEGIN readwrite -----"
  cat "$READWRITE_JSON"
  echo "----- END readwrite -----"
else
  echo "(could not fetch built-in 'readwrite' policy)"
fi
echo

# Custom policy JSON
echo "-- $APP_MEDIA_POLICY (custom) --"
if mc admin policy info root "$APP_MEDIA_POLICY" > "$CUSTOM_JSON" 2>/dev/null; then
  echo "Saved to: $CUSTOM_JSON"
  echo "----- BEGIN $APP_MEDIA_POLICY -----"
  cat "$CUSTOM_JSON"
  echo "----- END $APP_MEDIA_POLICY -----"
else
  echo "(could not fetch custom policy '$APP_MEDIA_POLICY')"
fi
echo

# --- OPTIONAL: verify (kun hvis vi kender secret) ---
if [ -z "$APP_MEDIA_SECRET_KEY" ] && [ "$EXISTS" -eq 1 ]; then
  echo "NOTE: existing user but secret not provided; skipping verify."
  echo "Service user for app (.env):"
  echo "  Storage__AccessKey=$APP_MEDIA_ACCESS_KEY"
  echo "  Storage__SecretKey=<KEEP_EXISTING_SECRET>"
  exit 0
fi

echo "== VERIFY (custom) =="
mc alias set app "$MINIO_ENDPOINT" "$APP_MEDIA_ACCESS_KEY" "$APP_MEDIA_SECRET_KEY" >/dev/null
if mc ls app >/dev/null 2>&1; then pass "ListAllMyBuckets OK"; else fail "ListAllMyBuckets FAIL"; fi

IFS=,; set -- $MINIO_BUCKETS
for b in "$@"; do
  [ -n "${b:-}" ] || continue
  b="$(trim "$b")"; [ -z "$b" ] && continue
  key="_health/custom-$(ts)-$$.txt"
  body="custom-test $(ts) bucket=$b"
  if mc ls "app/$b" >/dev/null 2>&1; then pass "ListBucket $b OK"; else fail "ListBucket $b FAIL"; fi
  if printf "%s" "$body" | mc pipe "app/$b/$key" >/dev/null 2>&1; then pass "PutObject $b OK"; else fail "PutObject $b FAIL"; fi
  got="$(mc cat "app/$b/$key" 2>/dev/null || true)"
  if [ "$got" = "$body" ]; then pass "GetObject $b OK"; else fail "GetObject $b FAIL"; fi
done

echo
echo "== RESULT =="
echo "PASS: $PASS"
echo "FAIL: $FAIL"
echo
echo "Service user for app (.env):"
echo "  Storage__AccessKey=$APP_MEDIA_ACCESS_KEY"
echo "  Storage__SecretKey=$APP_MEDIA_SECRET_KEY"
[ "$FAIL" -eq 0 ] || exit 1
