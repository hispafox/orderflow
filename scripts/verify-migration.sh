#!/bin/bash
# Verificación post-migration — llamar después de cada deploy que incluya migrations

SERVICE_URL="${1:-https://orderflow-orders.azurewebsites.net}"
MAX_RETRIES=5

echo "=== POST-MIGRATION VERIFICATION ==="
echo "Service: $SERVICE_URL"
echo "Time: $(date -u)"

for i in $(seq 1 $MAX_RETRIES); do
    echo ""
    echo "Attempt $i/$MAX_RETRIES..."

    RESPONSE=$(curl -s "$SERVICE_URL/health")
    STATUS=$(echo "$RESPONSE" | jq -r '.status // "unknown"')
    DB_STATUS=$(echo "$RESPONSE" | jq -r '.entries["orders-db-migrations"].status // "unknown"')

    echo "Health: $STATUS | DB Migrations: $DB_STATUS"

    if [ "$STATUS" = "Healthy" ] && [ "$DB_STATUS" = "Healthy" ]; then
        echo "Verification PASSED"
        exit 0
    fi

    echo "Not ready yet, waiting 10 seconds..."
    sleep 10
done

echo "Verification FAILED after $MAX_RETRIES attempts"
echo "Consider rollback: az webapp deployment slot swap --slot production --target-slot staging"
exit 1
