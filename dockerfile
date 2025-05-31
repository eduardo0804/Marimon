# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore
    
# Copiar el resto del código y compilar
COPY . ./
RUN dotnet publish "Marimon.csproj" -c Release -o /app/out

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Instalar dependencias básicas
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libx11-dev \
    libxext-dev \
    libxrender-dev \
    fontconfig \
    libfontconfig1 \
    wget \
    xfonts-75dpi \
    xfonts-base \
    libjpeg62-turbo \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Descargar e instalar wkhtmltopdf
RUN wget https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-2/wkhtmltox_0.12.6.1-2.bullseye_amd64.deb \
    && apt-get update \
    && apt-get install -y ./wkhtmltox_0.12.6.1-2.bullseye_amd64.deb || (apt-get -f install -y && apt-get install -y ./wkhtmltox_0.12.6.1-2.bullseye_amd64.deb) \
    && rm wkhtmltox_0.12.6.1-2.bullseye_amd64.deb

# Crear carpeta para las bibliotecas nativas
RUN mkdir -p /app/nativelibs/linux-x64 \
    && cp /usr/local/lib/libwkhtmltox.so /app/ || true \
    && cp /usr/local/lib/libwkhtmltox.so /app/nativelibs/linux-x64/ || true

# Crear carpeta para las claves de protección de datos
RUN mkdir -p /app/keys

# Copiar los archivos publicados desde la etapa de build
COPY --from=build-env /app/out .

# Establecer variables de entorno para las bibliotecas
ENV APP_NET_CORE Marimon.dll 
ENV LD_LIBRARY_PATH=/app:/usr/local/lib:/usr/lib:/app/nativelibs/linux-x64

# Comando para iniciar la aplicación
CMD ASPNETCORE_URLS=http://*:$PORT dotnet $APP_NET_CORE