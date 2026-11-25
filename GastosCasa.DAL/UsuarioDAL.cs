using GastosCasa.BE;
using Microsoft.Data.Sqlite;

namespace GastosCasa.DAL;

public class UsuarioDAL
{
    private readonly Conexion _conexion;

    public UsuarioDAL(Conexion conexion) => _conexion = conexion;

    public Usuario? ObtenerPorNombre(string nombre)
    {
        using var cn = _conexion.Abrir();
        using var cmd = cn.CreateCommand();
        // Siempre parámetros (@nombre), nunca concatenar strings: evita SQL Injection.
        cmd.CommandText = "SELECT Id, Nombre, PasswordHash FROM Usuario WHERE Nombre = @nombre";
        cmd.Parameters.AddWithValue("@nombre", nombre);
        using var dr = cmd.ExecuteReader();
        return dr.Read() ? Mapear(dr) : null;
    }

    public Usuario? ObtenerPorId(int id)
    {
        using var cn = _conexion.Abrir();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT Id, Nombre, PasswordHash FROM Usuario WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        using var dr = cmd.ExecuteReader();
        return dr.Read() ? Mapear(dr) : null;
    }

    public List<Usuario> Listar()
    {
        var lista = new List<Usuario>();
        using var cn = _conexion.Abrir();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT Id, Nombre, PasswordHash FROM Usuario ORDER BY Nombre";
        using var dr = cmd.ExecuteReader();
        while (dr.Read()) lista.Add(Mapear(dr));
        return lista;
    }

    public int Contar()
    {
        using var cn = _conexion.Abrir();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Usuario";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public int Insertar(Usuario u)
    {
        using var cn = _conexion.Abrir();
        using var cmd = cn.CreateCommand();
        // RETURNING Id devuelve el Id autogenerado en la misma consulta.
        cmd.CommandText = "INSERT INTO Usuario (Nombre, PasswordHash) VALUES (@nombre, @hash) RETURNING Id";
        cmd.Parameters.AddWithValue("@nombre", u.Nombre);
        cmd.Parameters.AddWithValue("@hash", u.PasswordHash);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static Usuario Mapear(SqliteDataReader dr) => new()
    {
        Id = dr.GetInt32(0),
        Nombre = dr.GetString(1),
        PasswordHash = dr.GetString(2)
    };
}
