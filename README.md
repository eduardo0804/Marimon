# Marimon

## Descripción del Proyecto

Marimon es una aplicación web diseñada para gestionar servicios automotrices, incluyendo reservas, ventas de autopartes, generación de comprobantes electrónicos, y más. El proyecto está desarrollado en ASP.NET Core y utiliza PostgreSQL como base de datos principal.

## Características Principales

- **Gestión de Usuarios y Roles**: Soporte para roles como Gerente de Operación, Personal de Servicio, Personal de Ventas y Cliente.
- **Reservas**: Sistema para gestionar reservas de servicios automotrices.
- **Ventas de Autopartes**: Módulo para la venta de autopartes con integración de Stripe para pagos.
- **Generación de Comprobantes**: Generación de comprobantes electrónicos en formato PDF con soporte para códigos QR.
- **Protección de Datos**: Cumplimiento con la Ley N° 29733 de Protección de Datos Personales.
- **Interfaz de Usuario**: Diseño responsivo y moderno utilizando Bootstrap.

## Tecnologías Utilizadas

- **Framework**: ASP.NET Core 9.0
- **Base de Datos**: PostgreSQL
- **Frontend**: Bootstrap, FontAwesome
- **Generación de PDF**: DinkToPdf
- **Autenticación**: ASP.NET Identity con soporte para roles
- **Pagos**: Stripe

## Configuración del Entorno

1. **Requisitos Previos**:
   - .NET SDK 9.0 o superior
   - PostgreSQL
   - Node.js (opcional, para gestionar dependencias de frontend)

2. **Configuración de la Base de Datos**:
   - Actualiza la cadena de conexión en `appsettings.json`:
     ```json
     "ConnectionStrings": {
       "PostgreSQLConnection": "Host=localhost;Database=Marimon;Username=tu_usuario;Password=tu_contraseña"
     }
     ```

3. **Migraciones de la Base de Datos**:
   Ejecuta los siguientes comandos:
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
4. Ejecución del Proyecto:
dotnet run


Contacto
Para más información, puedes contactar a Automotriz Marimon S.A.C:

Dirección: Av. Angamos Este Nro. 1686 a.H. Casa Huertas
Email: automotrizmarimon@gmail.com
Teléfono: +51 995 256 967