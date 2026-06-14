FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY VulnScan.Web/VulnScan.Web.csproj VulnScan.Web/
RUN dotnet restore VulnScan.Web/VulnScan.Web.csproj

COPY . .
RUN dotnet build VulnScan.Web/VulnScan.Web.csproj -c Release --no-restore

FROM build AS publish
RUN dotnet publish VulnScan.Web/VulnScan.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    nmap \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:5186
ENV ASPNETCORE_ENVIRONMENT=Production
ENV Database:Provider=SqlServer

EXPOSE 5186

VOLUME ["/app/App_Data"]

ENTRYPOINT ["dotnet", "VulnScan.Web.dll"]
