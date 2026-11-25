using GastosCasa.BE;
using Microsoft.Data.Sqlite;

namespace GastosCasa.DAL;

public class CategoriaDAL
{
    private readonly Conexion _conexion;

    public CategoriaDAL(Conexion conexion) => _conexion = conexion;

    public List<Categoria> Listar()
    {
        var lista = new List<Categoria>();
        using var cn = _conexion.Abrir();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT Id, Nombre, Tipo FROM Categoria ORDER BY Tipo, Nombre";
        using var dr = cmd.ExecuteReader();
        while (dr.Read()) lista.Add(Mapear(dr));
        return lista;
    }

    public Categoria? ObtenerPorId(int id)
    {
        using var cn = _conexion.Abrir();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT Id, Nombre, Tipo FROM Categoria WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        using var dr = cmd.ExecuteReader();
        return dr.Read() ? Mapear(dr) : null;
    }

    private static Categoria Mapear(SqliteDataReader dr) => new()
    {
        Id = dr.GetInt32(0),
        Nombre = dr.GetString(1),
        Tipo = (TipoMovimiento)dr.GetInt32(2)
    };
}
