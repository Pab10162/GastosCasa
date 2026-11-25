using GastosCasa.BE;
using GastosCasa.BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastosCasa.API.Controllers;

// Datos "de catálogo" que el frontend necesita para armar los combos.
[ApiController]
[Authorize]
[Route("api")]
public class CatalogosController : ControllerBase
{
    private readonly CategoriaBLL _categorias;
    private readonly UsuarioBLL _usuarios;

    public CatalogosController(CategoriaBLL categorias, UsuarioBLL usuarios)
    {
        _categorias = categorias;
        _usuarios = usuarios;
    }

    [HttpGet("categorias")]
    public List<Categoria> Categorias() => _categorias.Listar();

    [HttpGet("usuarios")]
    public List<UsuarioDto> Usuarios() =>
        _usuarios.Listar().Select(u => new UsuarioDto(u.Id, u.Nombre)).ToList();
}
