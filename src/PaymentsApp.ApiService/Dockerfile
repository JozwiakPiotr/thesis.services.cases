FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PaymentsApp.ApiService/PaymentsApp.ApiService.csproj PaymentsApp.ApiService/
COPY CasesApp.ServiceDefaults/CasesApp.ServiceDefaults.csproj CasesApp.ServiceDefaults/
RUN dotnet restore PaymentsApp.ApiService/PaymentsApp.ApiService.csproj

COPY . .
RUN dotnet publish PaymentsApp.ApiService/PaymentsApp.ApiService.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PaymentsApp.ApiService.dll"]
