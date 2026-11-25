using Microsoft.Data.Sqlite;

namespace GastosCasa.DAL;

// Única clase que sabe "dónde" está la base. Si mañana pasás a SQL Server,
// cambiás esto y los SQL de las clases DAL; la BLL y la API no se enteran.
public class Conexion
{
    private readonly string _cadena;

    // Ruta completa del archivo .db (la usa InicializadorBD para crear la carpeta).
    public string RutaArchivo { get; }

    public Conexion(string rutaArchivo)
    {
        RutaArchivo = rutaArchivo;
        _cadena = new SqliteConnectionStringBuilder
        {
            DataSource = rutaArchivo,
            ForeignKeys = true,           // SQLite no valida FKs si no se lo pedís
            DefaultTimeout = 10           // si otro está escribiendo, espera hasta 10 s
        }.ToString();
    }

    // Cada operación abre su propia conexión y la cierra con "using".
    // Así varias personas a la vez no se pisan: cada request tiene la suya.
    public SqliteConnection Abrir()
    {
        var cn = new SqliteConnection(_cadena);
        cn.Open();
        return cn;
    }
}
