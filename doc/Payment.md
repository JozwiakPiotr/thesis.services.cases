# Algorytm obliczania odsetek

# API

## Inicjalizacja konta

`POST /api/accounts` tworzy konto finansowe dla sprawy oraz zapisuje kapitał
pierwotny, naliczone odsetki i koszty dodatkowe z dnia przekazania.

## Szczegółowe saldo

`GET /api/accounts/{caseId}/balance` zwraca rozbicie salda na kapitał,
odsetki, koszty pozostałe oraz łączną kwotę do spłaty.

```mermaid
flowchart TD
    Start([Start: Codzienne zadanie w tle / Cron]) --> Fetch[Pobierz aktywne konta finansowe z PaymentsService]
    Fetch --> Loop{Dla każdego konta}

    Loop --> CheckType{Jaki typ odsetek?}
    CheckType -->|NONE / Brak| Skip[Pomiń konto] --> Next
    CheckType -->|STATUTORY / CONTRACTUAL| CalcDays[Wylicz liczbę dni od ostatniego naliczenia]

    CalcDays --> Formula["Oblicz przyrost odsetek dziennych:\nDelta = Pozostały Kapitał * (Stawka Roczna / 100) * (Liczba Dni / 365)"]

    Formula --> UpdateDB["Aktualizacja bazy danych:\n- interestAccrued += Delta\n- totalRemaining += Delta\n- interestLastCalculatedAt = Now"]

    UpdateDB --> EmitEvent[Wyślij komunikat BalanceUpdatedEvent na szynę RabbitMQ]
    EmitEvent --> Next{Czy są kolejne konta?}

    Skip --> Next
    Next -->|Tak| Loop
    Next -->|Nie| End([Koniec cyklu obliczeniowego])
```

# Entities
```mermaid
erDiagram
    FinancialAccount {
        uuid id PK "Surrogate key (UUIDv7)"
        uuid caseId UK "1:1 relation to DebtCase (CasesService)"
        string currency "ISO 4217 (e.g. PLN)"
        decimal interestRate "Annual interest rate percentage (e.g. 11.25)"
        string interestType "STATUTORY, CONTRACTUAL, NONE"
        datetime interestLastCalculatedAt "Timestamp of last automated interest job"
        datetime createdAt "Creation timestamp"
    }

    AccountBalance {
        uuid id PK
        uuid accountId FK "1:1 relation to FinancialAccount"
        decimal principalOriginal "Original debt principal amount"
        decimal principalPaid "Total paid towards principal"
        decimal principalRemaining "Calculated: Original - Paid"
        decimal interestAccrued "Total accrued interest"
        decimal interestPaid "Total paid towards interest"
        decimal interestRemaining "Calculated: Accrued - Paid"
        decimal feesAccrued "Additional processing or legal fees"
        decimal feesPaid "Total paid towards fees"
        decimal feesRemaining "Calculated: Accrued - Paid"
        decimal totalRemaining "Grand total remaining balance"
        datetime updatedAt "Timestamp of last balance recalculation"
    }

    Transaction {
        uuid id PK
        uuid accountId FK "Belongs to FinancialAccount"
        string externalTransactionId UK "Bank transfer / Payment gateway ID"
        string type "PAYMENT, INTEREST_ACCRUAL, FEE_ADDITION, CORRECTION"
        decimal totalAmount "Total transaction amount"
        datetime transactionDate "Date transaction occurred"
        string source "BANK_STATEMENT, MANUAL_ENTRY, SYSTEM_AUTOMATION"
        datetime createdAt "System entry timestamp"
    }

    LedgerEntry {
        uuid id PK
        uuid transactionId FK "Belongs to Transaction"
        string bucket "PRINCIPAL, INTEREST, FEES"
        decimal amount "Allocated amount to this bucket"
        string entryType "DEBIT, CREDIT"
    }

    FinancialAccount ||--|| AccountBalance : "maintains current"
    FinancialAccount ||--o{ Transaction : "records history"
    Transaction ||--|{ LedgerEntry : "deconstructs into"
```