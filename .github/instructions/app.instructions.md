---
description: How to develop app code guidelines
applyTo: CasesApp.ApiService/*.cs
---

# Styles used in the app
- vertical slice architecture
- CQRS
- DDD

## Vertical slice
Folder structure:
**Features** - endpoints are there and everything that this endpoints need to work, unless it shares some code with another feature
**Infrastructure** - code like EfCore configuration, Repositories, Logging etc
**Domain**, **Services** - shared code

## CQRS
**Queries** doesn't mutate state, endpoints for queries should have everything in one place - like GetSomething/Endpoint.cs in that file you use DbContext directly, don't use layers (service, domain, repositories). Records for dto (response and body), validation in one file.

**Commands** use layers: domain, service, repositories. If some layer is used only by one feature put everything under that feature folder like
```
Features/SomeFeature
  Endpoint.cs
  Domain/
    SomeEntity.cs
  Services/
    SomeService.cs
```
if layer is shared then put it under 
```
CasesApp.ApiService/
  Domain/
  Infrastructure/
  Services/
```

Commands can return responses - ID of created entity. 

## DDD
Never create entities without behavior. Use value objects.