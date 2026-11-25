namespace GastosCasa.BE;

// BE = Business Entities. Son clases "tontas": solo datos, sin lógica ni acceso a BD.
// Todas las capas las conocen, por eso viven en un proyecto aparte.

public enum TipoMovimiento
{
    Gasto = 1,
    Ingreso = 2
}

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    // Nunca guardamos la contraseña: solo un hash (ver BLL/Seguridad.cs).
    public string PasswordHash { get; set; } = "";
}

public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public TipoMovimiento Tipo { get; set; }
}

public class Movimiento
{
    public int Id { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public decimal Monto { get; set; }          // decimal, nunca double, para plata
    public DateOnly Fecha { get; set; }
    public string Descripcion { get; set; } = "";
    public int CategoriaId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime CreadoEn { get; set; }

    // Datos "de lectura" que trae el JOIN, para no hacer otra consulta.
    public string CategoriaNombre { get; set; } = "";
    public string UsuarioNombre { get; set; } = "";
}

// Criterios de búsqueda. Los null significan "no filtrar por esto".
public class FiltroMovimientos
{
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public int? CategoriaId { get; set; }
    public int? UsuarioId { get; set; }
    public TipoMovimiento? Tipo { get; set; }
}

public class TotalPorGrupo
{
    public string Nombre { get; set; } = "";
    public TipoMovimiento Tipo { get; set; }
    public decimal Total { get; set; }
}

public class Resumen
{
    public decimal TotalIngresos { get; set; }
    public decimal TotalGastos { get; set; }
    public decimal Balance => TotalIngresos - TotalGastos;
    public List<TotalPorGrupo> PorCategoria { get; set; } = new();
    public List<TotalPorGrupo> PorUsuario { get; set; } = new();
}
