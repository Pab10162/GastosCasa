namespace GastosCasa.DAL;

public class InicializadorBD
{
    private readonly Conexion _conexion;
    private readonly bool _usarWal;

    // usarWal: true en tu PC, false en Azure (ver comentario abajo).
    public InicializadorBD(Conexion conexion, bool usarWal)
    {
        _conexion = conexion;
        _usarWal = usarWal;
    }

    public void Ejecutar()
    {
        // Si la carpeta de la base no existe (por ejemplo D:\home\data en Azure), la creamos.
        var carpeta = Path.GetDirectoryName(_conexion.RutaArchivo);
        if (!string.IsNullOrEmpty(carpeta)) Directory.CreateDirectory(carpeta);

        var ruta = Path.Combine(AppContext.BaseDirectory, "BD", "schema.sql");
        var script = File.ReadAllText(ruta);

        using var cn = _conexion.Abrir();

        // WAL = Write-Ahead Logging: permite que alguien lea mientras otro escribe.
        // PERO WAL necesita memoria compartida y NO funciona bien sobre discos de red.
        // En Azure App Service, D:\home es un disco de red, así que ahí usamos el modo
        // clásico (DELETE). Para 4 personas la diferencia de velocidad no se nota.
        using (var cmd = cn.CreateCommand())
        {
            cmd.CommandText = _usarWal ? "PRAGMA journal_mode = WAL;" : "PRAGMA journal_mode = DELETE;";
            cmd.ExecuteNonQuery();
        }

        using (var cmd = cn.CreateCommand())
        {
            cmd.CommandText = script;
            cmd.ExecuteNonQuery();
        }
    }
}
