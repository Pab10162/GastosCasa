using GastosCasa.BE;
using GastosCasa.DAL;

namespace GastosCasa.BLL;

// Acá viven las REGLAS DE NEGOCIO. La API no valida nada por su cuenta
// y la DAL no decide nada: solo guarda y lee.
public class MovimientoBLL
{
    private const decimal MontoMaximo = 100_000_000m;
    private readonly MovimientoDAL _dal;
    private readonly CategoriaDAL _categoriaDal;

    public MovimientoBLL(MovimientoDAL dal, CategoriaDAL categoriaDal)
    {
        _dal = dal;
        _categoriaDal = categoriaDal;
    }

    public Movimiento Crear(Movimiento m, int usuarioId)
    {
        Validar(m);
        m.UsuarioId = usuarioId;          // el dueño sale del login, no de lo que mande el cliente
        m.CreadoEn = DateTime.UtcNow;
        m.Id = _dal.Insertar(m);
        return _dal.ObtenerPorId(m.Id)!;
    }

    public Movimiento Editar(Movimiento m, int usuarioId)
    {
        var existente = ObtenerPropio(m.Id, usuarioId);
        Validar(m);
        m.UsuarioId = existente.UsuarioId;
        _dal.Actualizar(m);
        return _dal.ObtenerPorId(m.Id)!;
    }

    public void Eliminar(int id, int usuarioId)
    {
        ObtenerPropio(id, usuarioId);
        _dal.Eliminar(id);
    }

    public List<Movimiento> Listar(FiltroMovimientos filtro)
    {
        ValidarRango(filtro);
        return _dal.Listar(filtro);
    }

    public Resumen ObtenerResumen(FiltroMovimientos filtro)
    {
        ValidarRango(filtro);
        return _dal.ObtenerResumen(filtro);
    }

    // Regla: cada uno edita y borra solo lo que cargó. Todos ven todo.
    private Movimiento ObtenerPropio(int id, int usuarioId)
    {
        var m = _dal.ObtenerPorId(id) ?? throw new NoEncontradoException("El movimiento no existe.");
        if (m.UsuarioId != usuarioId)
            throw new SinPermisoException($"Solo {m.UsuarioNombre} puede modificar este movimiento.");
        return m;
    }

    private void Validar(Movimiento m)
    {
        if (!Enum.IsDefined(m.Tipo))
            throw new ReglaNegocioException("El tipo debe ser Gasto o Ingreso.");
        if (m.Monto <= 0)
            throw new ReglaNegocioException("El monto debe ser mayor a cero.");
        if (m.Monto > MontoMaximo)
            throw new ReglaNegocioException("El monto es demasiado grande.");
        if (decimal.Round(m.Monto, 2) != m.Monto)
            throw new ReglaNegocioException("El monto puede tener como máximo 2 decimales.");

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        if (m.Fecha > hoy.AddDays(1))
            throw new ReglaNegocioException("La fecha no puede ser futura.");
        if (m.Fecha < hoy.AddYears(-5))
            throw new ReglaNegocioException("La fecha es demasiado antigua.");

        m.Descripcion = (m.Descripcion ?? "").Trim();
        if (m.Descripcion.Length > 100)
            throw new ReglaNegocioException("La descripción admite hasta 100 caracteres.");

        var categoria = _categoriaDal.ObtenerPorId(m.CategoriaId)
            ?? throw new ReglaNegocioException("La categoría no existe.");
        if (categoria.Tipo != m.Tipo)
            throw new ReglaNegocioException($"'{categoria.Nombre}' no es una categoría de {m.Tipo.ToString().ToLower()}.");
    }

    private static void ValidarRango(FiltroMovimientos f)
    {
        if (f.Desde is not null && f.Hasta is not null && f.Desde > f.Hasta)
            throw new ReglaNegocioException("La fecha 'desde' no puede ser posterior a 'hasta'.");
    }
}
