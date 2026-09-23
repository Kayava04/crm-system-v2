# Build:  docker build -t crm-api backend
# Run:    see docker-compose.yml (profile "app")

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Host/CrmSystem.Host.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# Log files and uploaded files (profile photos) live here; the application does not run as root.
# Mount a volume on /app/uploads to keep the photos when the container is replaced.
RUN mkdir -p /app/Logs /app/uploads && chown -R $APP_UID /app/Logs /app/uploads
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CrmSystem.Host.dll"]
