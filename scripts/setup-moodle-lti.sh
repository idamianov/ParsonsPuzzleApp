#!/usr/bin/env bash
#
# setup-moodle-lti.sh
#
# Waits for the Moodle container to be ready, then runs the LTI auto-setup
# PHP script inside it.
#
# Usage:
#   bash scripts/setup-moodle-lti.sh
#
# Environment variables:
#   APP_PORT          - Port the Parsons Puzzle app runs on (default: 5055)
#   MOODLE_CONTAINER  - Name of the Moodle container (default: auto-detected)
#   COMPOSE_FILE      - Docker Compose file (default: docker-compose.moodle.yml)

set -euo pipefail

APP_PORT="${APP_PORT:-5055}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.moodle.yml}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PHP_SCRIPT="$SCRIPT_DIR/moodle-lti-setup.php"

# ─── Find the Moodle container ────────────────────────────────────────────────

if [ -n "${MOODLE_CONTAINER:-}" ]; then
    CONTAINER="$MOODLE_CONTAINER"
else
    CONTAINER=$(docker compose -f "$COMPOSE_FILE" ps -q moodle 2>/dev/null || true)
    if [ -z "$CONTAINER" ]; then
        echo "ERROR: Could not find the Moodle container."
        echo "Make sure you've run: docker compose -f $COMPOSE_FILE up -d"
        exit 1
    fi
fi

echo "Moodle container: $CONTAINER"

# ─── Wait for Moodle to be ready ─────────────────────────────────────────────

echo "Waiting for Moodle to be ready (this may take a few minutes on first start)..."

MAX_WAIT=300  # 5 minutes
ELAPSED=0
INTERVAL=10

while true; do
    # Check if Moodle's config.php exists (installation complete)
    if docker exec "$CONTAINER" test -f /bitnami/moodle/config.php 2>/dev/null; then
        # Also check if the web server responds
        HTTP_CODE=$(docker exec "$CONTAINER" curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/ 2>/dev/null || echo "000")
        if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "303" ] || [ "$HTTP_CODE" = "302" ]; then
            echo "Moodle is ready!"
            break
        fi
    fi

    if [ "$ELAPSED" -ge "$MAX_WAIT" ]; then
        echo "ERROR: Timed out waiting for Moodle to be ready after ${MAX_WAIT}s."
        echo "Check container logs: docker compose -f $COMPOSE_FILE logs moodle"
        exit 1
    fi

    echo "  Still waiting... (${ELAPSED}s elapsed)"
    sleep "$INTERVAL"
    ELAPSED=$((ELAPSED + INTERVAL))
done

# ─── Copy and run the PHP script ─────────────────────────────────────────────

echo ""
echo "Running LTI setup script..."
echo ""

docker cp "$PHP_SCRIPT" "$CONTAINER":/tmp/moodle-lti-setup.php
docker exec -e APP_PORT="$APP_PORT" "$CONTAINER" php /tmp/moodle-lti-setup.php
docker exec "$CONTAINER" rm -f /tmp/moodle-lti-setup.php
