# 🏠 Gastos de casa

App web para que las 4 personas de la casa carguen gastos e ingresos desde el celular o la PC.
Todo queda en **una sola base de datos** que corre en tu PC.

```
Celulares / PCs (navegador)  ──WiFi──▶  API ASP.NET Core (tu PC)  ──▶  gastos.db (SQLite)
```

## Cómo correrla

**Necesitás:** .NET 8 SDK (o Visual Studio 2022 con "Desarrollo web y ASP.NET").

1. Abrí `GastosCasa.sln` en Visual Studio, poné **GastosCasa.API** como proyecto de inicio y dale ▶.
   Por consola es: `dotnet run --project GastosCasa.API`.
   La primera vez se descarga el paquete `Microsoft.Data.Sqlite` de NuGet.
2. Antes de usarla, abrí `GastosCasa.API/appsettings.json` y **cambiá `CodigoCasa`**. Es la clave que
   piden para crear una cuenta, así que pasásela solo a tu familia.
3. En tu PC entrá a http://localhost:5000, andá a **Crear cuenta** y registrate.

### Entrar desde los celulares

1. Averiguá la IP de tu PC: en una consola escribí `ipconfig` y buscá "Dirección IPv4" (algo como `192.168.0.15`).
2. En el celular, conectado a la **misma WiFi**, entrá a `http://192.168.0.15:5000`.
3. La primera vez, Windows te pregunta si permitís la conexión en el firewall: aceptá para **redes privadas**.
   Si no te preguntó y no anda, buscá "Firewall de Windows Defender → Permitir una aplicación" o abrí el puerto 5000.
4. Para tenerla como app, en Chrome del celular usá el menú ⋮ y después **Agregar a pantalla principal**.

> La PC tiene que estar prendida con la app corriendo. Para usarla fuera de casa, mirá "Próximos pasos".

## Publicarla en Azure (para usarla sin tu PC prendida)

La app detecta sola si corre en Azure (por la variable `WEBSITE_SITE_NAME`) y en ese caso:
- guarda la base en `D:\home\data\gastos.db`, una carpeta que **no se borra al volver a publicar**;
- usa SQLite en modo `DELETE` en vez de `WAL`, porque `D:\home` es un disco de red y WAL no anda bien ahí;
- fuerza HTTPS.

Además, en cualquier entorno, el login y el registro aceptan como máximo 10 intentos por minuto
desde cada IP, y las contraseñas nuevas tienen que tener al menos 6 caracteres.

Pasos:
1. Creá una cuenta en https://azure.microsoft.com (pide tarjeta para verificar; el plan **F1** es gratis).
2. En Visual Studio: clic derecho en **GastosCasa.API** → **Publicar** → **Azure** → **Azure App Service (Windows)**
   → iniciá sesión → **Crear nuevo**. Elegí un nombre (va a quedar `https://<nombre>.azurewebsites.net`),
   región **Brazil South** (la más cercana) y en el plan de hosting tamaño **Free (F1)**.
3. Dale a **Publicar**. Cuando termina, se abre el navegador con la app.
4. **Cambiá el código de la casa en Azure**, no en el archivo: en el portal de Azure abrí tu App Service →
   **Configuración → Variables de entorno** → agregá `CodigoCasa` con un valor difícil de adivinar → Aplicar.
   Lo que pongas ahí tiene prioridad sobre `appsettings.json`.
5. En **Configuración → Configuración general**, activá **Solo HTTPS**.
6. Pasale a tu familia el link `https://<nombre>.azurewebsites.net` y el código. Anda con datos móviles, desde cualquier lado.

**Backup en Azure:** en tu App Service → **Herramientas avanzadas (Kudu)** → **Ir** → **Debug console → CMD**,
entrá a `data` y descargá `gastos.db`.

**Para actualizar la app:** cambiá el código y volvé a darle **Publicar**. Los datos no se pierden.

> Con el plan F1 la app "se duerme" si nadie la usa un rato, así que la primera carga del día tarda unos segundos.

## Cómo está armada (en capas)

| Proyecto | Responsabilidad | Conoce a |
|---|---|---|
| **GastosCasa.BE** | Entidades: `Usuario`, `Categoria`, `Movimiento`, `Resumen`... Solo datos. | nadie |
| **GastosCasa.DAL** | SQL contra SQLite (ADO.NET). `BD/schema.sql` crea las tablas. | BE |
| **GastosCasa.BLL** | Reglas de negocio: validaciones, permisos, hash de contraseñas. | BE, DAL |
| **GastosCasa.API** | Controllers HTTP, login con cookie, y el frontend en `wwwroot/`. | BE, BLL |

**Un ejemplo de punta a punta: cargar un gasto**
`app.js` hace `POST /api/movimientos` → `MovimientosController.Crear` → `MovimientoBLL.Crear` (valida
monto, fecha y categoría, y asigna el usuario de la cookie) → `MovimientoDAL.Insertar` (hace el INSERT) → SQLite.

### Decisiones de diseño que conviene entender

- **La plata se guarda en centavos (`INTEGER`)**. Con `double`, `0.1 + 0.2` da `0.30000000000000004`.
  Con centavos las sumas son exactas. En C# se usa `decimal`.
- **El dueño de cada movimiento sale de la cookie**, no de lo que manda el navegador. Si no fuera así,
  cualquiera podría cargar gastos a nombre de otro.
- **Cada uno edita y borra solo lo suyo**, pero todos ven todo. Esa regla está en la BLL
  (`ObtenerPropio`), no en el frontend. El frontend solo esconde los botones.
- **Varias personas a la vez**: cada request abre su propia conexión, y SQLite está en modo WAL, que
  permite leer mientras otro escribe. Lo probé con 200 cargas simultáneas de 4 usuarios y no se perdió ninguna.
- **Se actualiza sola**: el frontend vuelve a pedir los datos cada 15 segundos y cuando volvés a la pestaña.
- **Contraseñas**: se guardan como hash PBKDF2 con sal (`BLL/Seguridad.cs`), nunca en texto plano.
- **Errores de negocio**: la BLL lanza `ReglaNegocioException` (400), `SinPermisoException` (403) o
  `NoEncontradoException` (404). `FiltroExcepciones` las traduce a HTTP en un solo lugar.
- **SQL Injection**: todas las consultas usan parámetros (`@nombre`).

## Endpoints

| Método | Ruta | Qué hace |
|---|---|---|
| POST | `/api/auth/registro` | `{nombre, password, codigoCasa}` crea la cuenta y deja la sesión iniciada |
| POST | `/api/auth/login` | `{nombre, password}` |
| POST | `/api/auth/logout` | |
| GET | `/api/auth/yo` | Usuario logueado (401 si no hay sesión) |
| GET | `/api/categorias` | |
| GET | `/api/usuarios` | Para el filtro por persona |
| GET | `/api/movimientos?desde=&hasta=&usuarioId=&categoriaId=&tipo=` | Lista con filtros opcionales |
| GET | `/api/movimientos/resumen?desde=&hasta=` | Totales por categoría y por persona |
| POST | `/api/movimientos` | `{tipo: "Gasto"/"Ingreso", monto, fecha: "2026-11-23", descripcion, categoriaId}` |
| PUT | `/api/movimientos/{id}` | Mismo cuerpo. Solo el dueño puede |
| DELETE | `/api/movimientos/{id}` | Solo el dueño puede |

## Datos útiles

- La base queda en `GastosCasa.API/bin/Debug/net8.0/gastos.db`. **Para hacer backup copiá ese archivo**
  con la app cerrada. Podés abrirlo con [DB Browser for SQLite](https://sqlitebrowser.org/) para ver las tablas.
- Para empezar de cero, borrá `gastos.db` y se vuelve a crear vacía al arrancar.
- Para agregar o renombrar categorías, editá el `INSERT` al final de `BD/schema.sql` (o hacelo directo en la base).

## Próximos pasos (ejercicios para vos)

1. **Swagger**: agregá el paquete `Swashbuckle.AspNetCore` y en `Program.cs` sumá
   `builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen();` y después
   `app.UseSwagger(); app.UseSwaggerUI();`. Así podés probar la API desde `/swagger`.
2. **Exportar a CSV**: un endpoint `GET /api/movimientos/csv` que devuelva `File(...)`, y un botón en el frontend.
3. **Presupuesto mensual por categoría**: una tabla nueva y un aviso cuando se pasa.
4. **Tests**: un proyecto xUnit que pruebe `MovimientoBLL` (monto negativo, categoría de otro tipo, editar algo ajeno).
5. **Usarla desde afuera de casa**: instalá Tailscale en la PC y en los celulares (opción C), o publicala
   en la nube (opción B). En la nube, activá HTTPS antes que nada.
6. **Pasar a SQL Server**: solo cambian `Conexion.cs`, el script y los SQL de la DAL. La BLL y la API quedan igual.
   Si eso se cumple, las capas están bien separadas.
