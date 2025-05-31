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

# Variables de entorno
ENV APP_NET_CORE=Marimon.dll
ENV ASPNETCORE_ENVIRONMENT=Development

# CMD recomendado (JSON array)
CMD ["dotnet", "Marimon.dll"]