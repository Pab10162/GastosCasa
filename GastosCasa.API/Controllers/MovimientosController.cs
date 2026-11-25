using GastosCasa.BE;
using GastosCasa.BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastosCasa.API.Controllers;

[ApiController]
[Authorize]                       // todo este controller exige estar logueado
[Route("api/movimientos")]
public class MovimientosController : ControllerBase
{
    private readonly MovimientoBLL _bll;

    public MovimientosController(MovimientoBLL bll) => _bll = bll;

    // GET /api/movimientos?desde=2026-11-01&hasta=2026-11-30&categoriaId=1&usuarioId=2&tipo=Gasto
    [HttpGet]
    public List<Movimiento> Listar([FromQuery] FiltroMovimientos filtro) => _bll.Listar(filtro);

    // GET /api/movimientos/resumen?desde=...&hasta=...
    [HttpGet("resumen")]
    public Resumen Resumen([FromQuery] FiltroMovimientos filtro) => _bll.ObtenerResumen(filtro);

    [HttpPost]
    public ActionResult<Movimiento> Crear(MovimientoRequest req)
    {
        var creado = _bll.Crear(AEntidad(req), User.UsuarioId());
        return StatusCode(201, creado);
    }

    [HttpPut("{id:int}")]
    public Movimiento Editar(int id, MovimientoRequest req)
    {
        var m = AEntidad(req);
        m.Id = id;
        return _bll.Editar(m, User.UsuarioId());
    }

    [HttpDelete("{id:int}")]
    public IActionResult Eliminar(int id)
    {
        _bll.Eliminar(id, User.UsuarioId());
        return NoContent();
    }

    private static Movimiento AEntidad(MovimientoRequest r) => new()
    {
        Tipo = r.Tipo,
        Monto = r.Monto,
        Fecha = r.Fecha,
        Descripcion = r.Descripcion ?? "",
        CategoriaId = r.CategoriaId
    };
}
