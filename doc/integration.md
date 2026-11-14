# Integracja kubernetes z AD
## Przygotowanie domeny AD
Tworzymy konto usługi i przypisujemy SPN
```bash
$Password = ConvertTo-SecureString "Qwer!234" -AsPlainText -Force
New-AdUser "svc-keycloak" -SamAccountName "svc-keycloak" -AccountPassword $Password -Enabled $true -PasswordNeverExpires $true
Set-ADUser -Identity "svc-keycloak" -PrincipalsAllowedToDelegateToAccount $null

setspn -s HTTP/keycloak.contoso.lab contoso\svc-keycloak
setspn -s HTTP/keycloak contoso\svc-keycloak

# sprawdzenie, czy SPN nie jest przypisany do innego konta
setspn -Q HTTP/keycloak.contoso.lab
setspn -Q HTTP/keycloak

# lub jeśli nie zadziała setspn
Set-ADUser -Identity "svc-keycloak" -Add @{
    "servicePrincipalName" = @("HTTP/keycloak.contoso.lab", "HTTP/keycloak")
}
# sprawdzenie
setspn -l svc-keycloak
```

tworzymy plik keytab i przekazujemy do hosta z keycloak
```bash
ktpass /princ HTTP/keycloak.contoso.lab@CONTOSO.LAB /mapuser svc-keycloak@CONTOSO.LAB /pass "<hasło konta svc-keycloak>" /crypto AES256-SHA1 /ptype KRB5_NT_PRINCIPAL /out keycloak.keytab /mapop set
```

Po wygenerowaniu keytabu nie wolno commitować go do repozytorium. Na hoście
Keycloak plik powinien mieć uprawnienia `600` i być czytelny dla użytkownika
kontenera. Po zmianie hasła konta serwisowego trzeba wygenerować keytab
ponownie.

## Konfiguracja keycloak
`panel admina > lewy pasek > Configure > User federation > Add kerberos`

**UI display name** 'AD SSO' dowolna nazwa

**Kerberos realm** CONTOSO.LAB

**Server principal** 'HTTP/keycloak.contoso.lab@CONTOSO.LAB' dokładnie taki sam SPN, jaki mapowaliśmy poleceniem ktpass na kontrolerze domeny

**Key tab** `/opt/keycloak/conf/keycloak.keytab`

**Debug** Off kwestia logów w konsoli keycloak

**Allow password authentication** Off zależy nam na SSO

**Update first login** Off inaczej uzytkownik przy pierwszym logowaniu będzie musiał uzupełnić dane

**Cache policy** Default

## Konfiguracja SSO 
trzeba skonfigurować Integrated Windows Authentication (IWA)
- dodajemy GPO nazwa Kerberos-SSO-Policy
- edit, `Computer Configuration -> Policies -> Administrative Templates -> Windows Components -> Internet Explorer -> Internet Control Panel -> Security Page` 
- dodajemy `https://keycloak.contoso.lab` do Local Intranet albo Trusted Sites
  i włączamy Integrated Windows Authentication.
- W Edge/Chrome polityka `AuthServerAllowlist` musi zawierać
  `keycloak.contoso.lab` (w starszych konfiguracjach także
  `AuthServerWhitelist`). W Firefox należy ustawić
  `network.negotiate-auth.trusted-uris` na `https://keycloak.contoso.lab`.
- Użytkownik musi mieć bilet dla `HTTP/keycloak.contoso.lab`. Na komputerze
  domenowym można sprawdzić go poleceniem `klist`; po zmianie SPN lub hasła
  warto wykonać `klist purge` i zalogować się ponownie.

Adres używany przez przeglądarkę musi być dokładnie
`https://keycloak.contoso.lab` lub aplikacja musi przekierowywać do tego
hosta. Nie należy używać adresu IP ani `localhost`, ponieważ Kerberos dobiera
SPN na podstawie nazwy hosta.

Jeżeli log Keycloak zawiera `GSSHeader did not find the right tag`, sprawdź
nagłówek żądania:

```text
Authorization: Negotiate <token>
```

Jeżeli token jest poprzedzony `NTLM`, przeglądarka nie używa Kerberos dla tego
hosta. Jeżeli po `Negotiate` nadal pojawia się ten błąd, sprawdź zgodność
SPN, keytabu i hasła konta `svc-keycloak`.