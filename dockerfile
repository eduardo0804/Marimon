# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore
    
# Copiar el resto del código y compilar
COPY . ./
RUN dotnet publish "marimon.csproj" -c Release -o /app/out

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Crear carpeta para las claves de protección de datos
RUN mkdir -p /app/keys

# Copiar los archivos publicados desde la etapa de build
COPY --from=build-env /app/out .

ENV APP_NET_CORE marimon.dll 

CMD ASPNETCORE_URLS=http://*:$PORT dotnet $APP_NET_CORE