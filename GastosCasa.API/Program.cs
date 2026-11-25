using GastosCasa.API;
using GastosCasa.BLL;
using GastosCasa.DAL;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ¿Estamos corriendo en Azure App Service? Azure define esta variable de entorno sola.
var enAzure = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") is not null;

// ---------- Escuchar en la red de la casa ----------
// "0.0.0.0" = aceptar conexiones de cualquier dispositivo de la WiFi,
// no solo de esta PC. El puerto se cambia en appsettings.json.
// En Azure no hace falta: el puerto y el HTTPS los maneja Azure.
if (!enAzure)
    builder.WebHost.UseUrls($"http://0.0.0.0:{config.GetValue("Puerto", 5000)}");

// ---------- Inyección de dependencias ----------
// Registramos cada clase una vez y ASP.NET se encarga de crearlas y pasárselas
// a quien las necesite (por el constructor). Por eso los controllers no hacen "new".

// Dónde se guarda la base:
// - En tu PC: junto al .exe, o la ruta que pongas en "ArchivoBD" (puede ser absoluta; en el JSON se escribe "E:\\GastosCasa\\datos\\gastos.db").
// - En Azure: en %HOME%\data (D:\home\data). Esa carpeta NO se borra al volver a publicar;
//   la carpeta de la app (site\wwwroot) sí se reemplaza en cada deploy.
var archivoBD = config.GetValue("ArchivoBD", "gastos.db")!;
var rutaBD = enAzure
    ? Path.Combine(Environment.GetEnvironmentVariable("HOME")!, "data", Path.GetFileName(archivoBD))
    : Path.Combine(AppContext.BaseDirectory, archivoBD);   // si archivoBD es absoluta, Path.Combine la respeta
builder.Services.AddSingleton(new Conexion(rutaBD));
builder.Services.AddSingleton(sp => new InicializadorBD(sp.GetRequiredService<Conexion>(), usarWal: !enAzure));
builder.Services.AddSingleton<UsuarioDAL>();
builder.Services.AddSingleton<CategoriaDAL>();
builder.Services.AddSingleton<MovimientoDAL>();
builder.Services.AddSingleton(sp => new UsuarioBLL(
    sp.GetRequiredService<UsuarioDAL>(),
    config.GetValue<string>("CodigoCasa") ?? throw new InvalidOperationException("Falta CodigoCasa en appsettings.json"),
    config.GetValue("MaxUsuarios", 4)));
builder.Services.AddSingleton<CategoriaBLL>();
builder.Services.AddSingleton<MovimientoBLL>();

builder.Services.AddControllers(o => o.Filters.Add<FiltroExcepciones>())
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// ---------- Login con cookie ----------
// Al loguearse, el servidor le da al navegador una cookie firmada con el Id del usuario.
// En cada request siguiente el navegador la manda sola y sabemos quién es.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "gastos_sesion";
        o.Cookie.HttpOnly = true;                      // JavaScript no la puede leer (protege contra XSS)
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.ExpireTimeSpan = TimeSpan.FromDays(30);      // que no tengan que loguearse todos los días
        o.SlidingExpiration = true;
        // Por defecto redirige a una página de login; para una API queremos 401/403.
        o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
        o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();

// ---------- Freno a ataques de fuerza bruta ----------
// Cuando la app está en internet, cualquiera puede intentar adivinar contraseñas.
// Esto permite como máximo 10 intentos de login/registro por minuto desde cada IP.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;   // 429 = "Too Many Requests"
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

// Crea la BD y las tablas si no existen.
app.Services.GetRequiredService<InicializadorBD>().Ejecutar();

if (enAzure) app.UseHttpsRedirection();   // en internet, siempre HTTPS
app.UseDefaultFiles();   // "/" sirve wwwroot/index.html
app.UseStaticFiles();    // sirve CSS, JS, íconos de wwwroot
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

Console.WriteLine($"Base de datos: {rutaBD}");
if (!enAzure)
    Console.WriteLine("Desde los celulares entrá a http://<IP-de-esta-PC>:" + config.GetValue("Puerto", 5000));
app.Run();

// Necesario para que el proyecto de pruebas pueda levantar la API.
public partial class Program { }
