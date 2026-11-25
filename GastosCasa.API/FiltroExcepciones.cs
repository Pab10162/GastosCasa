using GastosCasa.BLL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GastosCasa.API;

// Atrapa las excepciones de negocio en un solo lugar y las convierte en
// respuestas HTTP con un mensaje legible. Así los controllers no necesitan try/catch.
public class FiltroExcepciones : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ReglaNegocioException e:
                context.Result = new BadRequestObjectResult(new { error = e.Message });
                context.ExceptionHandled = true;
                break;
            case NoEncontradoException e:
                context.Result = new NotFoundObjectResult(new { error = e.Message });
                context.ExceptionHandled = true;
                break;
            case SinPermisoException e:
                context.Result = new ObjectResult(new { error = e.Message }) { StatusCode = 403 };
                context.ExceptionHandled = true;
                break;
        }
        // Cualquier otra excepción sigue de largo -> 500 (es un bug, no un error del usuario).
    }
}
