# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish "Marimon.csproj" -c Release -o /app/out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Instalar wkhtmltopdf y dependencias
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    wkhtmltopdf \
    libgdiplus \
    fontconfig \
    libxext6 \
    libxrender1 \
    xfonts-75dpi \
    xfonts-base \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Crear carpetas necesarias
RUN mkdir -p /app/keys

# Copiar archivos publicados
COPY --from=build-env /app/out .

# Copiar Views y wwwroot si existen
COPY --from=build-env /app/Views/ ./Views/
COPY --from=build-env /app/wwwroot/ ./wwwroot/

# Verificar qué archivos se copiaron (para diagnóstico)
RUN echo "=== VERIFICACIÓN DE ARCHIVOS ===" && \
    echo "Views:" && ls -la Views/ 2>/dev/null || echo "No hay Views/" && \
    echo "wwwroot:" && ls -la wwwroot/ 2>/dev/null || echo "No hay wwwroot/" && \
    echo "Configuración:" && ls -la *.json 2>/dev/null || echo "No hay archivos JSON" && \
    echo "Templates de email:" && find . -name "*email*" -type f 2>/dev/null || echo "No hay templates de email"

# Variables de entorno
ENV APP_NET_CORE=Marimon.dll
ENV ASPNETCORE_ENVIRONMENT=Development

# CMD recomendado (JSON array)
CMD ["dotnet", "Marimon.dll"]