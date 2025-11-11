#!/usr/bin/env bash
# Fix Nginx reverse proxy for Katana API under /api → 127.0.0.1:5055/api
# Safe-by-default: backs up current site file, injects/repairs location /api/ block
# and reloads nginx after config test passes.
#
# Requirements on remote: bash, awk, sed, sudo privileges, nginx installed.
# This script connects over SSH; you'll be prompted for the password (or use SSH key).

set -euo pipefail

SERVER_USER=${SERVER_USER:-"huseyinadm"}
SERVER_HOST=${SERVER_HOST:-"31.186.24.44"}
SERVER="${SERVER_USER}@${SERVER_HOST}"
DOMAIN=${DOMAIN:-"bfmmrp.com"}
API_PORT=${API_PORT:-"5055"}
DRY_RUN=${DRY_RUN:-0}

usage() {
  cat <<EOF
Usage: $(basename "$0") [--server user@host] [--domain bfmmrp.com] [--api-port 5055] [--dry-run]

Environment variables (optional):
  SERVER_USER, SERVER_HOST, DOMAIN, API_PORT, DRY_RUN

Examples:
  $(basename "$0")
  $(basename "$0") --server ${SERVER} --domain ${DOMAIN} --api-port ${API_PORT}
  DOMAIN=bfmmrp.com API_PORT=5055 $(basename "$0")
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --server)
      SERVER="$2"; shift 2;;
    --domain)
      DOMAIN="$2"; shift 2;;
    --api-port)
      API_PORT="$2"; shift 2;;
    --dry-run)
      DRY_RUN=1; shift;;
    -h|--help)
      usage; exit 0;;
    *) echo "Unknown arg: $1"; usage; exit 1;;
  esac
done

printf "\n=== Fixing Nginx API proxy on %s (domain=%s, api-port=%s) ===\n" "$SERVER" "$DOMAIN" "$API_PORT"

REMOTE_SCRIPT='bash -euo pipefail <<\"EOS\"
DOMAIN="'"$DOMAIN"'"
API_PORT="'"$API_PORT"'"

# Find site file path for this domain
CANDIDATES=(/etc/nginx/sites-available/${DOMAIN}.conf /etc/nginx/sites-available/${DOMAIN} \
            /etc/nginx/sites-enabled/${DOMAIN}.conf /etc/nginx/sites-enabled/${DOMAIN})
CONF_FILE=""
for f in "${CANDIDATES[@]}"; do
  if [[ -f "$f" ]]; then CONF_FILE="$f"; break; fi
done

if [[ -z "$CONF_FILE" ]]; then
  # Fallback: grep server_name across known dirs
  CONF_FILE=$(grep -RIl "server_name[[:space:]]\+${DOMAIN};" /etc/nginx/sites-available /etc/nginx/sites-enabled 2>/dev/null | head -n1 || true)
fi

if [[ -z "$CONF_FILE" ]]; then
  echo "Could not locate nginx site file for $DOMAIN" >&2
  exit 2
fi

echo "Using site file: $CONF_FILE"
BACKUP="$CONF_FILE.bak-$(date +%F_%H%M%S)"
sudo cp -a "$CONF_FILE" "$BACKUP"
echo "Backup created: $BACKUP"

# Prepare temp output
TMP_OUT=$(mktemp)
trap "rm -f '$TMP_OUT'" EXIT

# AWK filter: ensure within the server block that has this server_name,
# there exists location /api/ with desired content. If exists, replace; otherwise insert before server closing brace.
awk -v domain="$DOMAIN" -v port="$API_PORT" '
  BEGIN {
    in_server=0; brace=0; has_name=0; in_api=0; replaced_api=0;
  }
  function print_api_block() {
    print "    location /api/ {";
    #!/usr/bin/env bash
    # Fix Nginx reverse proxy for Katana API under /api → 127.0.0.1:5055/api (LOCAL MODE)
    # - Backs up current site file
    # - Injects or replaces a proper `location /api/ { ... }` block inside the server_name $DOMAIN site
    # - Runs nginx -t and reloads on success

    set -Eeuo pipefail

    # Defaults (can be overridden via env)
    DOMAIN=${DOMAIN:-"bfmmrp.com"}
    API_PORT=${API_PORT:-"5055"}

    if [[ ${EUID:-$(id -u)} -ne 0 ]]; then
      SUDO="sudo"
    else
      SUDO=""
    fi

    echo ""
    echo "=== Fixing Nginx API proxy locally (domain=${DOMAIN}, api-port=${API_PORT}) ==="

    # Try to locate the nginx site file for the domain
    declare -a CANDIDATES=(
      "/etc/nginx/sites-available/${DOMAIN}.conf"
      "/etc/nginx/sites-available/${DOMAIN}"
      "/etc/nginx/sites-enabled/${DOMAIN}.conf"
      "/etc/nginx/sites-enabled/${DOMAIN}"
    )

    CONF_FILE=""
    for f in "${CANDIDATES[@]}"; do
      if [[ -f "$f" ]]; then CONF_FILE="$f"; break; fi
    done

    if [[ -z "$CONF_FILE" ]]; then
      # Fallback: grep for server_name lines
      CONF_FILE=$(grep -RIl "server_name[[:space:]]\+${DOMAIN}[[:space:]]*;" /etc/nginx/sites-available /etc/nginx/sites-enabled 2>/dev/null | head -n1 || true)
    fi

    if [[ -z "$CONF_FILE" ]]; then
      echo "[ERROR] Nginx site file for domain '${DOMAIN}' not found." >&2
      exit 2
    fi

    echo "Using site file: $CONF_FILE"

    # Backup
    BACKUP="$CONF_FILE.bak-$(date +%F_%H%M%S)"
    $SUDO cp -a "$CONF_FILE" "$BACKUP"
    echo "Backup created: $BACKUP"

    # Temp output
    TMP_OUT=$(mktemp)
    trap 'rm -f "$TMP_OUT"' EXIT

    # Build new config via AWK transformation
    awk -v domain="$DOMAIN" -v port="$API_PORT" '
      function print_api_block() {
        print "        location /api/ {";
        print "            proxy_pass http://127.0.0.1:" port "/api/;";
        print "            proxy_http_version 1.1;";
        print "            proxy_set_header Host $host;";
        print "            proxy_set_header X-Real-IP $remote_addr;";
        print "            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;";
        print "            proxy_set_header X-Forwarded-Proto $scheme;";
        print "        }";
      }
      {
        line=$0;

        # If currently skipping original /api/ block, consume until its braces close
        if (skipping_api) {
          opens = gsub(/\{/, "{", line);
          closes = gsub(/\}/, "}", line);
          api_depth += opens - closes;
          if (api_depth <= 0) skipping_api=0;
          next;
        }

        # Detect entering a server block
        if (!in_server && line ~ /^\s*server\s*\{/) {
          in_server=1; brace=1; has_name=0; replaced_api=0;
          print line; next;
        }

        if (in_server) {
          # Track server_name match for this domain
          if (line ~ ("server_name[[:space:]]+.*\\b" domain "\\b[[:space:]]*;")) {
            has_name=1;
          }

          # If this is the target server and we hit location /api/ {, replace it
          if (has_name && line ~ /^\s*location[[:space:]]+\/api\/[[:space:]]*\{/) {
            replaced_api=1;
            print_api_block();
            skipping_api=1; api_depth=1; # we already saw the opening '{'
            next;
          }

          # Compute brace movement to know if server is closing
          opens = gsub(/\{/, "{", line);
          closes = gsub(/\}/, "}", line);
          next_brace = brace + opens - closes;

          # If we are about to close this server block and no /api/ seen, inject it
          if (has_name && !replaced_api && next_brace == 0) {
            print_api_block();
          }

          print line;
          brace = next_brace;
          if (brace == 0) { in_server=0; has_name=0; }
          next;
        }

        # Outside any server block
        print line;
      }
    ' "$CONF_FILE" > "$TMP_OUT"

    echo "--- Preview of changes (first 200 lines of diff if any) ---"
    if command -v diff >/dev/null 2>&1; then
      diff -u "$CONF_FILE" "$TMP_OUT" | sed -n '1,200p' || true
    else
      echo "diff not available"
    fi

    # Write back
    $SUDO tee "$CONF_FILE" >/dev/null < "$TMP_OUT"

    # Test & reload
    $SUDO nginx -t
    $SUDO systemctl reload nginx

    echo "== Local backend checks =="
    printf "health: ";  curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:${API_PORT}/api/health || true
    printf "products.luca: "; curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:${API_PORT}/api/Products/luca || true
    printf "failed-records: "; curl -s -o /dev/null -w "%{http_code}\n" "http://127.0.0.1:${API_PORT}/api/adminpanel/failed-records?page=1&pageSize=1" || true

    echo "== Domain checks (HEAD) =="
    curl -Is "https://${DOMAIN}/api/health" | head -n1 || true
    curl -Is "https://${DOMAIN}/api/Products/luca" | head -n1 || true
    curl -Is "https://${DOMAIN}/api/adminpanel/failed-records" | head -n1 || true
    echo "Done."
