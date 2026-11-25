using System.Security.Claims;
using GastosCasa.BE;
using GastosCasa.BLL;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GastosCasa.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UsuarioBLL _bll;

    public AuthController(UsuarioBLL bll) => _bll = bll;

    [EnableRateLimiting("login")]
    [HttpPost("registro")]
    public async Task<ActionResult<UsuarioDto>> Registro(RegistroRequest req)
    {
        var usuario = _bll.Registrar(req.Nombre, req.Password, req.CodigoCasa);
        await IniciarSesion(usuario);
        return new UsuarioDto(usuario.Id, usuario.Nombre);
    }

    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<ActionResult<UsuarioDto>> Login(LoginRequest req)
    {
        var usuario = _bll.Login(req.Nombre, req.Password);
        if (usuario is null) return Unauthorized(new { error = "Usuario o contraseña incorrectos." });
        await IniciarSesion(usuario);
        return new UsuarioDto(usuario.Id, usuario.Nombre);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return NoContent();
    }

    // El frontend lo llama al abrir la página para saber si ya hay sesión.
    [Authorize]
    [HttpGet("yo")]
    public UsuarioDto Yo() => new(User.UsuarioId(), User.Identity!.Name!);

    private Task IniciarSesion(Usuario u)
    {
        // "Claims" = datos que van firmados dentro de la cookie.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new Claim(ClaimTypes.Name, u.Nombre)
        };
        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return HttpContext.SignInAsync(new ClaimsPrincipal(identidad),
            new AuthenticationProperties { IsPersistent = true });
    }
}

public static class ClaimsExtensions
{
    public static int UsuarioId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
