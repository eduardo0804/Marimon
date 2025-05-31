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

# Instalar las dependencias necesarias para wkhtmltopdf en Linux
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libc6-dev \
    libx11-dev \
    libxext-dev \
    libxrender-dev \
    fontconfig \
    libfontconfig1 \
    wget \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Descargar e instalar wkhtmltopdf para Linux
RUN wget https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6-1/wkhtmltox_0.12.6-1.buster_amd64.deb \
    && dpkg -i wkhtmltox_0.12.6-1.buster_amd64.deb || true \
    && apt-get update && apt-get -f install -y \
    && rm wkhtmltox_0.12.6-1.buster_amd64.deb

# Crear carpeta para las claves de protección de datos
RUN mkdir -p /app/keys

# Copiar los archivos publicados desde la etapa de build
COPY --from=build-env /app/out .

ENV APP_NET_CORE Marimon.dll 

CMD ASPNETCORE_URLS=http://*:$PORT dotnet $APP_NET_CORE