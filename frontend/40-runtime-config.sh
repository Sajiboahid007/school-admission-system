#!/bin/sh
set -e
cat > /usr/share/nginx/html/config.js <<EOF
window.__APP_CONFIG__ = { API_URL: "${API_URL}" };
EOF
echo "Frontend will call backend at: ${API_URL}"
