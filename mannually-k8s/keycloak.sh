kubectl create secret generic keycloak-secrets \
    -n cases \
    --from-file=keycloak.keytab=./keycloak.data/keycloak.keytab \
    --from-literal=admin-username=admin \
    --from-literal=admin-password=admin \
    --from-file=Contoso-Lab-Root-CA.crt=./keycloak.data/Contoso-Lab-Root-CA.crt    

kubectl create secret tls keycloak-tls \
    -n cases \
    --cert=keycloak.data/tls.crt \
    --key=keycloak.data/tls.key