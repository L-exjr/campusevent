FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/EventManagement.Api/EventManagement.Api.csproj backend/EventManagement.Api/
RUN dotnet restore backend/EventManagement.Api/EventManagement.Api.csproj

COPY backend/EventManagement.Api/ backend/EventManagement.Api/
COPY contracts/ contracts/
RUN dotnet publish backend/EventManagement.Api/EventManagement.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    Database__ApplyMigrations=true
EXPOSE 8080
USER $APP_UID

# Railway injects PORT. The fallback keeps the image runnable outside Railway.
ENTRYPOINT ["sh", "-c", "exec dotnet EventManagement.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
