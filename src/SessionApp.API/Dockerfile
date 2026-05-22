# ===== Build Stage =====
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# copy csproj files first (better caching)
COPY src/SessionApp.Domain/*.csproj src/SessionApp.Domain/
COPY src/SessionApp.Application/*.csproj src/SessionApp.Application/
COPY src/SessionApp.Infrastructure/*.csproj src/SessionApp.Infrastructure/
COPY src/SessionApp.API/*.csproj src/SessionApp.API/

# restore
RUN dotnet restore src/SessionApp.API/SessionApp.API.csproj

# copy everything
COPY . .

# publish
RUN dotnet publish src/SessionApp.API/SessionApp.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ===== Runtime Stage =====
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "SessionApp.API.dll"]