using System.Globalization;
using GastosCasa.BE;
using Microsoft.Data.Sqlite;

namespace GastosCasa.DAL;

public class MovimientoDAL
{
    private const string FormatoFecha = "yyyy-MM-dd";
    private readonly Conexion _conexion;

    public MovimientoDAL(Conexion conexion) => _conexion = conexion;

    // ---------- Conversión de plata ----------
    // En la BD guardamos centavos (entero). En C# usamos decimal.
    private static long ACentavos(decimal monto) => (long)Math.Round(monto * 100m, MidpointRounding.AwayFromZero);
    private static decimal DeCentavos(long centavos) => centavos / 100m;

    private const string SelectBase = @"
        SELECT m.Id, m.Tipo, m.MontoCentavos, m.Fecha, m.Descripcion,
               m.CategoriaId, c.Nombre, m.UsuarioId, u.Nombre, m.CreadoEn
        FROM Movimiento m
        JOIN Categoria c ON c.Id = m.CategoriaId
        JOIN Usuario   u ON u.Id = m.UsuarioId";

    public int Insertar(Movimiento m)
    {
        using var cn = _conexion.Abrir();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Movimiento (Tipo, MontoCentavos, Fecha, Descripcion, CategoriaId, UsuarioId, CreadoEn)
            VALUES (@tipo, @monto, @fecha, @desc, @cat, @usr, @creado)
            RETURNING Id";
        CargarParametros(cmd, m);
        cmd.Parameters.AddWithValue("@usr", m.UsuarioId);
        cmd.Parameters.AddWithValue("@creado", m.CreadoEn.ToString("o", CultureInfo.InvariantCulture));
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public bool Actualizar(Movimiento m)
    {
        using var cn = _conexion.Abrir();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Movimiento
            SET Tipo = @tipo, MontoCentavos = @monto, Fecha = @fecha,
                Descripcion = @desc, CategoriaId = @cat
            WHERE Id = @id";
        CargarParametros(cmd, m);
        cmd.Parameters.AddWithValue("@id", m.Id);
        return cmd.ExecuteNonQuery() == 1;
    }

    public bool Eliminar(int id)
    {
        using var cn = _conexion.Abrir();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "DELETE FROM Movimiento WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() == 1;
    }

    public Movimiento? ObtenerPorId(int id)
    {
        using var cn = _conexion.Abrir();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = SelectBase + " WHERE m.Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        using var dr = cmd.ExecuteReader();
        return dr.Read() ? Mapear(dr) : null;
    }

    public List<Movimiento> Listar(FiltroMovimientos filtro)
    {
        var lista = new List<Movimiento>();
        using var cn = _conexion.Abrir();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = SelectBase + ArmarWhere(cmd, filtro) + " ORDER BY m.Fecha DESC, m.Id DESC";
        using var dr = cmd.ExecuteReader();
        while (dr.Read()) lista.Add(Mapear(dr));
        return lista;
    }

    // Los totales los calcula la BD con GROUP BY: es mucho más eficiente
    // que traer todos los movimientos y sumarlos en C#.
    public Resumen ObtenerResumen(FiltroMovimientos filtro)
    {
        var resumen = new Resumen();
        using var cn = _conexion.Abrir();

        resumen.PorCategoria = Agrupar(cn, filtro, "c.Nombre");
        resumen.PorUsuario = Agrupar(cn, filtro, "u.Nombre");

        foreach (var g in resumen.PorCategoria)
        {
            if (g.Tipo == TipoMovimiento.Ingreso) resumen.TotalIngresos += g.Total;
            else resumen.TotalGastos += g.Total;
        }
        return resumen;
    }

    private List<TotalPorGrupo> Agrupar(SqliteConnection cn, FiltroMovimientos filtro, string columna)
    {
        var lista = new List<TotalPorGrupo>();
        using var cmd = cn.CreateCommand();
        // "columna" no viene del usuario (la elegimos nosotros arriba), por eso es seguro concatenarla.
        cmd.CommandText = $@"
            SELECT {columna}, m.Tipo, SUM(m.MontoCentavos) AS Total
            FROM Movimiento m
            JOIN Categoria c ON c.Id = m.CategoriaId
            JOIN Usuario   u ON u.Id = m.UsuarioId
            {ArmarWhere(cmd, filtro)}
            GROUP BY {columna}, m.Tipo
            ORDER BY Total DESC";
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            lista.Add(new TotalPorGrupo
            {
                Nombre = dr.GetString(0),
                Tipo = (TipoMovimiento)dr.GetInt32(1),
                Total = DeCentavos(dr.GetInt64(2))
            });
        }
        return lista;
    }

    // Arma el WHERE según qué filtros vinieron cargados.
    private static string ArmarWhere(SqliteCommand cmd, FiltroMovimientos f)
    {
        var condiciones = new List<string>();

        if (f.Desde is not null)
        {
            condiciones.Add("m.Fecha >= @desde");
            cmd.Parameters.AddWithValue("@desde", f.Desde.Value.ToString(FormatoFecha, CultureInfo.InvariantCulture));
        }
        if (f.Hasta is not null)
        {
            condiciones.Add("m.Fecha <= @hasta");
            cmd.Parameters.AddWithValue("@hasta", f.Hasta.Value.ToString(FormatoFecha, CultureInfo.InvariantCulture));
        }
        if (f.CategoriaId is not null)
        {
            condiciones.Add("m.CategoriaId = @fcat");
            cmd.Parameters.AddWithValue("@fcat", f.CategoriaId.Value);
        }
        if (f.UsuarioId is not null)
        {
            condiciones.Add("m.UsuarioId = @fusr");
            cmd.Parameters.AddWithValue("@fusr", f.UsuarioId.Value);
        }
        if (f.Tipo is not null)
        {
            condiciones.Add("m.Tipo = @ftipo");
            cmd.Parameters.AddWithValue("@ftipo", (int)f.Tipo.Value);
        }

        return condiciones.Count == 0 ? "" : " WHERE " + string.Join(" AND ", condiciones);
    }

    private static void CargarParametros(SqliteCommand cmd, Movimiento m)
    {
        cmd.Parameters.AddWithValue("@tipo", (int)m.Tipo);
        cmd.Parameters.AddWithValue("@monto", ACentavos(m.Monto));
        cmd.Parameters.AddWithValue("@fecha", m.Fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("@desc", m.Descripcion);
        cmd.Parameters.AddWithValue("@cat", m.CategoriaId);
    }

    private static Movimiento Mapear(SqliteDataReader dr) => new()
    {
        Id = dr.GetInt32(0),
        Tipo = (TipoMovimiento)dr.GetInt32(1),
        Monto = DeCentavos(dr.GetInt64(2)),
        Fecha = DateOnly.ParseExact(dr.GetString(3), FormatoFecha, CultureInfo.InvariantCulture),
        Descripcion = dr.GetString(4),
        CategoriaId = dr.GetInt32(5),
        CategoriaNombre = dr.GetString(6),
        UsuarioId = dr.GetInt32(7),
        UsuarioNombre = dr.GetString(8),
        CreadoEn = DateTime.Parse(dr.GetString(9), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
    };
}
