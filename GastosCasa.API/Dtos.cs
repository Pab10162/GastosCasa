using GastosCasa.BE;

namespace GastosCasa.API;

// DTO = Data Transfer Object: lo que viaja por HTTP.
// No exponemos las entidades BE directamente: por ejemplo, Usuario tiene
// PasswordHash, y eso jamás debe salir del servidor.

public record LoginRequest(string Nombre, string Password);
public record RegistroRequest(string Nombre, string Password, string CodigoCasa);
public record UsuarioDto(int Id, string Nombre);

// El cliente NO manda UsuarioId: el dueño se saca de la cookie de sesión.
public record MovimientoRequest(TipoMovimiento Tipo, decimal Monto, DateOnly Fecha, string? Descripcion, int CategoriaId);
