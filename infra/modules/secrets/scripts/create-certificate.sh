#!/usr/bin/env bash
set -euo pipefail

echo "🔐 Creating self-signed certificate in Key Vault..."
echo "   Key Vault: $KEY_VAULT_NAME"
echo "   Certificate: $CERTIFICATE_NAME"
echo "   Subject: $CERTIFICATE_SUBJECT"
echo "   Validity: $VALIDITY_MONTHS months"

# Check if certificate already exists
EXISTING_CERT=$(az keyvault certificate show \
  --vault-name "$KEY_VAULT_NAME" \
  --name "$CERTIFICATE_NAME" \
  --query "id" \
  --output tsv 2>/dev/null || echo "")

if [ -n "$EXISTING_CERT" ]; then
  echo "✅ Certificate already exists, retrieving thumbprint..."
  THUMBPRINT=$(az keyvault certificate show \
    --vault-name "$KEY_VAULT_NAME" \
    --name "$CERTIFICATE_NAME" \
    --query "x509ThumbprintHex" \
    --output tsv)
else
  echo "📜 Creating new certificate..."

  # Create certificate policy JSON
  POLICY=$(cat <<EOF
{
  "issuerParameters": {
    "name": "Self"
  },
  "keyProperties": {
    "exportable": true,
    "keySize": 2048,
    "keyType": "RSA",
    "reuseKey": false
  },
  "secretProperties": {
    "contentType": "application/x-pkcs12"
  },
  "x509CertificateProperties": {
    "subject": "$CERTIFICATE_SUBJECT",
    "validityInMonths": $VALIDITY_MONTHS,
    "keyUsage": [
      "digitalSignature",
      "keyEncipherment"
    ],
    "extendedKeyUsage": [
      "1.3.6.1.5.5.7.3.2"
    ]
  },
  "lifetimeActions": [
    {
      "action": {
        "actionType": "EmailContacts"
      },
      "trigger": {
        "daysBeforeExpiry": 30
      }
    }
  ]
}
EOF
)

  # Create the certificate
  az keyvault certificate create \
    --vault-name "$KEY_VAULT_NAME" \
    --name "$CERTIFICATE_NAME" \
    --policy "$POLICY"

  # Wait for certificate to be created
  echo "⏳ Waiting for certificate creation to complete..."
  sleep 10

  # Get the thumbprint
  THUMBPRINT=$(az keyvault certificate show \
    --vault-name "$KEY_VAULT_NAME" \
    --name "$CERTIFICATE_NAME" \
    --query "x509ThumbprintHex" \
    --output tsv)

  echo "✅ Certificate created successfully!"
fi

echo "🔑 Certificate Thumbprint: $THUMBPRINT"

# Output the thumbprint for use in other deployments
echo "{\"thumbprint\": \"$THUMBPRINT\"}" > "$AZ_SCRIPTS_OUTPUT_PATH"
