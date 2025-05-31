# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore
    
# Copy everything else and build
COPY . ./
RUN dotnet publish "Marimon.csproj" -c Release -o /app/out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install only basic dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    libgdiplus \
    libc6-dev \
    libx11-dev \
    fontconfig \
    libxext6 \
    libxrender1 \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Create directory for data protection keys
RUN mkdir -p /app/keys

# Copy app files 
COPY --from=build-env /app/out .

# Set environment variables
ENV APP_NET_CORE Marimon.dll 
ENV LD_LIBRARY_PATH=/app:/app/nativelibs/linux-x64:/usr/local/lib:/usr/lib

# Start the application
CMD ASPNETCORE_URLS=http://*:$PORT dotnet $APP_NET_CORE