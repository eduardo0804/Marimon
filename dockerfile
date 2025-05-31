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

# Instalar solo las dependencias mínimas necesarias
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libx11-dev \
    libxext-dev \
    libxrender-dev \
    fontconfig \
    libfontconfig1 \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Crear carpeta para las claves de protección de datos
RUN mkdir -p /app/keys

# Copiar los archivos publicados desde la etapa de build
COPY --from=build-env /app/out .

# Asegurarse de que las bibliotecas nativas tengan permisos de ejecución
RUN chmod +x /app/nativelibs/linux-x64/libwkhtmltox.so || true

# Establecer variables de entorno para las bibliotecas
ENV APP_NET_CORE Marimon.dll 
ENV LD_LIBRARY_PATH=/app:/app/nativelibs/linux-x64:/usr/local/lib:/usr/lib

# Comando para iniciar la aplicación
CMD ASPNETCORE_URLS=http://*:$PORT dotnet $APP_NET_CORE