# Build context: repo root  (DockerfileContext = ..\..\)

# ── Stage 1: Build Angular ────────────────────────────────────────────────────
FROM node:22-alpine AS angular-build
WORKDIR /client

# Install dependencies (cached unless package-lock changes)
COPY Pricer.WebApp/pricer.webapp.client/package*.json ./
RUN npm ci --ignore-scripts

# Build
COPY Pricer.WebApp/pricer.webapp.client/ ./
RUN npm run build
# Output: /client/dist/pricer.webapp.client/browser/

# ── Stage 2: Build & publish .NET backend ─────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-build
WORKDIR /src

# Restore (cached unless .csproj files change)
COPY Pricer/Pricer.csproj                                                       Pricer/
COPY Pricer.DAL/Pricer.DAL.csproj                                               Pricer.DAL/
COPY Pricer.DAL.Security/Pricer.DAL.Security.csproj                             Pricer.DAL.Security/
COPY Pricer.Application/Pricer.Application.csproj                               Pricer.Application/
COPY Pricer.WebApp/Pricer.WebApp.Server/Pricer.WebApp.Server.csproj             Pricer.WebApp/Pricer.WebApp.Server/
# .esproj stub needed so MSBuild can resolve the project reference during restore
COPY Pricer.WebApp/pricer.webapp.client/pricer.webapp.client.esproj             Pricer.WebApp/pricer.webapp.client/

RUN dotnet restore Pricer.WebApp/Pricer.WebApp.Server/Pricer.WebApp.Server.csproj \
    /p:BuildNodeProject=false

# Copy source
COPY Pricer/                        Pricer/
COPY Pricer.DAL/                    Pricer.DAL/
COPY Pricer.DAL.Security/           Pricer.DAL.Security/
COPY Pricer.Application/            Pricer.Application/
COPY Pricer.WebApp/Pricer.WebApp.Server/ Pricer.WebApp/Pricer.WebApp.Server/

# Inject Angular dist into wwwroot so IncludeAngularDist target picks it up
COPY --from=angular-build /client/dist/pricer.webapp.client/browser/ \
     Pricer.WebApp/pricer.webapp.client/dist/pricer.webapp.client/browser/

RUN dotnet publish Pricer.WebApp/Pricer.WebApp.Server/Pricer.WebApp.Server.csproj \
    -c Release \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:BuildNodeProject=false

# ── Stage 3: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

# Run as non-root (app user is built into the base image)
USER $APP_UID

COPY --from=dotnet-build /app/publish .
ENTRYPOINT ["dotnet", "Pricer.WebApp.Server.dll"]
