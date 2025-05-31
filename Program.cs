using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Marimon.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Marimon.Services;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using DinkToPdf.Contracts;
using DinkToPdf;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Reflection;
using Marimon.Helpers; // Necesario para SameSiteMode
using Stripe;
using Stripe.Checkout;
using Marimon.Models;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http.Features;  // <-- Aquí está el using para StripeSettings

var builder = WebApplication.CreateBuilder(args);
// Configuración de PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("PostgreSQLConnection")
    ?? throw new InvalidOperationException("Connection string 'PostgreSQLConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

//builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>(); ELIMINAR*/

//  Configuración de Identity CON soporte para roles
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true; //requiere confirmación
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configuración de cookies para Identity
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Configuración de autenticación de Google
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.CallbackPath = "/signin-google";
    });

// Configuración del servicio de email
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailSender, EmailSenderWithAttachments>();
// Registrar la implementación extendida como IEmailSenderWithAttachments
builder.Services.AddTransient<IEmailSenderWithAttachments, EmailSenderWithAttachments>();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();


// Agregar estas líneas
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1); // Tiempo de vida del carrito
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Necesario para que no lo bloquee la política de cookies
});

// Configura DinkToPdf converter con el DLL cargado  según el sistema operativo
var context = new CustomAssemblyLoadContext();
string libFileName;

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    libFileName = "libwkhtmltox.dll";
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    libFileName = "libwkhtmltox.so";
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
{
    libFileName = "libwkhtmltox.dylib";
}
else
{
    throw new PlatformNotSupportedException("Sistema operativo no compatible");
}

// Primero intenta cargar desde la ubicación de la aplicación
string localPath = Path.Combine(AppContext.BaseDirectory, libFileName);
string systemPath = "";

if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    // En Linux, también busca en ubicaciones del sistema
    systemPath = "/usr/local/lib/libwkhtmltox.so";
    if (!System.IO.File.Exists(localPath) && System.IO.File.Exists(systemPath))
    {
        localPath = systemPath;
    }
}

context.LoadUnmanagedLibrary(localPath);

// Registra el converter como singleton (para inyectar en controladores si quieres)
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
// Protección de datos persistente en carpeta del contenedor
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("Marimon");

// Agregar cliente HTTP configurado para Google AI Studio
builder.Services.AddHttpClient("GoogleAI", client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// Configurar límites de carga de archivos
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
});



var app = builder.Build();
//asignación roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Gerente_Operacion", "Personal_Servicio", "Personal_Ventas", "Cliente" }; // Agrega aquí los roles que necesites

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}
// Configuración del pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication(); // Ya tenías esta línea
app.UseAuthorization(); // Ya tenías esta línea
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.Run();