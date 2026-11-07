# Wdrożenie na Kubernetes

## Wymagania

Potrzebne są:

- .NET SDK zgodny z projektem,
- Aspire CLI,
- `kubectl` z ustawionym kontekstem klastra,
- Helm 4.2 lub nowszy,
- rejestr obrazów dostępny zarówno lokalnie, jak i z węzłów klastra.

Sprawdzenie narzędzi:

```bash
dotnet --info
aspire --version
kubectl config current-context
helm version
```

## Integracja Kubernetes w Aspire

Do AppHost dodano pakiet:

```xml
<PackageReference Include="Aspire.Hosting.Kubernetes"
                  Version="13.6.1-preview.1.26506.6" />
```

W `AppHost.cs` dodano środowisko Kubernetes i konfigurację chartu:

```csharp
var kubernetes = builder.AddKubernetesEnvironment("k8s")
    .WithHelm(helm =>
    {
        helm.WithChartName("cases-app")
            .WithChartVersion("1.0.0")
            .WithChartDescription("Cases and payments services")
            .WithReleaseName("cases-app")
            .WithNamespace("cases");
    });
```

Zasoby aplikacji przypisano do tego środowiska:

```csharp
.WithComputeEnvironment(kubernetes)
```

API mają również:

```csharp
.WithExternalHttpEndpoints()
```

co pozwala publisherowi wygenerować zewnętrzne endpointy dla usług API.

## Budowanie i generowanie artefaktów

Przywrócenie pakietów i sprawdzenie rozwiązania:

```bash
dotnet restore src/CasesApp.slnx
dotnet build src/CasesApp.slnx --no-restore
```

Wygenerowanie chartu Helm:

```bash
rm -rf k8s-artifacts
aspire publish \
  --apphost src/CasesApp.AppHost/CasesApp.AppHost.csproj \
  --output-path k8s-artifacts \
  --non-interactive
```

Wygenerowany katalog zawiera między innymi:

```text
k8s-artifacts/
  Chart.yaml
  values.yaml
  templates/
    apiservice/
    paymentsapi/
    debtorapp/
    keycloak/
    postgres/
```

Sprawdzenie chartu:

```bash
helm lint k8s-artifacts
```

## Konfiguracja przed wdrożeniem

Przed instalacją należy zmienić `k8s-artifacts/values.yaml`:

1. Ustawić obrazy `apiservice`, `paymentsapi` i `debtorapp` wskazujące na
   rejestr dostępny dla klastra.
2. Uzupełnić sekrety PostgreSQL i połączenia do baz.
3. Ustawić sekrety administratora Keycloak.
4. Skonfigurować użytkowników i realm Keycloak dla klastra.

Nie należy commitować haseł ani tokenów do `values.yaml`. Wartości sekretów
można przekazać przez osobny plik ignorowany przez Git lub przez mechanizm
sekretów używany w danym klastrze.

## Instalacja chartu

Sprawdź kontekst klastra:

```bash
kubectl config current-context
```

Zainstaluj albo zaktualizuj release:

```bash
helm upgrade --install cases-app ./k8s-artifacts \
  --namespace cases \
  --create-namespace
```

Sprawdź zasoby:

```bash
kubectl get pods -n cases
kubectl get services -n cases
kubectl get deployments -n cases
```

Logi API i Keycloak:

```bash
kubectl logs -n cases deployment/cases-app-apiservice
kubectl logs -n cases deployment/cases-app-paymentsapi
kubectl logs -n cases deployment/cases-app-keycloak
```

## Aktualizacja

Po zmianie kodu lub konfiguracji ponownie wygeneruj chart i wykonaj:

```bash
aspire publish \
  --apphost src/CasesApp.AppHost/CasesApp.AppHost.csproj \
  --output-path k8s-artifacts \
  --non-interactive

helm upgrade cases-app ./k8s-artifacts \
  --namespace cases
```

## Uwagi

Weryfikacja wykonana w repozytorium:

- `dotnet build src/CasesApp.slnx --no-restore` — poprawna kompilacja,
- `aspire publish ...` — poprawne wygenerowanie chartu,
- `helm lint k8s-artifacts` — chart poprawny.

Nie wykonywano instalacji na rzeczywistym klastrze Kubernetes, ponieważ
wdrożenie wymaga konkretnego kontekstu `kubectl`, rejestru obrazów i sekretów
środowiska docelowego.
