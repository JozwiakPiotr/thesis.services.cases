FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CasesApp.ApiService/CasesApp.ApiService.csproj CasesApp.ApiService/
COPY CasesApp.ServiceDefaults/CasesApp.ServiceDefaults.csproj CasesApp.ServiceDefaults/
RUN dotnet restore CasesApp.ApiService/CasesApp.ApiService.csproj

COPY . .
RUN dotnet publish CasesApp.ApiService/CasesApp.ApiService.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CasesApp.ApiService.dll"]
