```bash
openssl genrsa -out rootCA.key 4096;
openssl req -x509 -new -nodes -key rootCA.key -sha256 -days 3650 -out rootCA.crt -subj "/CN=Contoso-Lab-Root-CA/O=Contoso/C=PL";
cat << 'EOF' > san.cnf
[req]
default_bits       = 2048
prompt             = no
default_md         = sha256
req_extensions     = req_ext
distinguished_name = dn

[dn]
CN = *.contoso.lab

[req_ext]
subjectAltName = @alt_names

[alt_names]
DNS.1 = *.contoso.lab
DNS.2 = contoso.lab
EOF

openssl genrsa -out wildcard.key 2048
openssl req -new -key wildcard.key -out wildcard.csr -config san.cnf
openssl x509 -req -in wildcard.csr -CA rootCA.crt -CAkey rootCA.key -CAcreateserial -out wildcard.crt -days 825 -sha256 -extfile san.cnf -extensions req_ext
```