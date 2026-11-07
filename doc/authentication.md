# Uwierzytelnianie z Keycloak

## Konfiguracja Aspire

Keycloak jest uruchamiany osobno przez Docker Compose, aby jego adres i port
były stałe niezależnie od cyklu życia Aspire.

Uruchom Keycloak z katalogu głównego repozytorium:

```bash
docker compose up -d keycloak
```

Realm `cases` jest importowany z
`src/CasesApp.AppHost/Keycloak/realm.json`, a Keycloak jest dostępny po HTTP
na porcie `8080`.

Wcześniejsza konfiguracja Aspire używała pakietu:

```xml
<PackageReference Include="Aspire.Hosting.Keycloak"
                  Version="13.6.1-preview.1.26506.6" />
```

Plik `src/CasesApp.AppHost/Keycloak/realm.json` definiuje:

- realm `cases`,
- publicznego klienta `api`,
- audience `api`,
- testowego użytkownika `demo` z hasłem `demo`.

## Konfiguracja API

W obu projektach API dodano pakiet:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer"
                  Version="10.0.12" />
```

W `Program.cs` skonfigurowano walidację tokenów:

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"]
            ?? throw new InvalidOperationException(
                "Keycloak:Authority configuration is required.");
        options.Audience = builder.Configuration["Keycloak:Audience"] ?? "api";
        options.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

Middleware musi być dodany przed mapowaniem endpointów:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Konfiguracja API:

```json
{
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/cases",
    "Audience": "api"
  }
}
```

Fallback policy chroni wszystkie endpointy. Endpointy health-check pozostają
anonimowe, ponieważ Aspire musi móc sprawdzać ich stan.

## Uruchomienie lokalne

Uruchom AppHost:

```bash
aspire start --isolated
```

Pobierz token testowego użytkownika:

```bash
curl -X POST \
  http://localhost:8080/realms/cases/protocol/openid-connect/token \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode 'client_id=api' \
  --data-urlencode 'username=demo' \
  --data-urlencode 'password=demo' \
  --data-urlencode 'grant_type=password'
```

Token z odpowiedzi JSON można przekazać do API:

```bash
curl http://localhost:5588/ \
  -H "Authorization: Bearer <ACCESS_TOKEN>"

curl http://localhost:5589/ \
  -H "Authorization: Bearer <ACCESS_TOKEN>"
```

Bez nagłówka `Authorization` endpointy aplikacyjne zwracają `401 Unauthorized`.

## Dodawanie użytkowników

W środowisku lokalnym można dopisać użytkownika do tablicy `users` w
`realm.json` i utworzyć świeży zasób Keycloak. W środowiskach współdzielonych
użytkowników należy tworzyć przez panel administracyjny Keycloak albo Admin
API. Haseł nie należy przechowywać w repozytorium.

## Ważne ograniczenie dla Kubernetes

Lokalny import `realm.json` używa bind mountu i jest wykonywany tylko w
`RunMode`. Publisher Kubernetes nie obsługuje tego bind mountu, dlatego dla
Kubernetes użytkowników i realm należy skonfigurować osobno, na przykład przez
przygotowany obraz Keycloak z importem realm albo mechanizm inicjalizacyjny
uruchamiany po wdrożeniu.
