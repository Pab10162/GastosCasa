using GastosCasa.BE;
using GastosCasa.DAL;

namespace GastosCasa.BLL;

public class UsuarioBLL
{
    private readonly UsuarioDAL _dal;
    private readonly string _codigoCasa;
    private readonly int _maxUsuarios;

    // El "código de la casa" y el máximo de usuarios vienen de appsettings.json.
    // Sirve para que solo tu familia pueda crearse una cuenta.
    public UsuarioBLL(UsuarioDAL dal, string codigoCasa, int maxUsuarios)
    {
        _dal = dal;
        _codigoCasa = codigoCasa;
        _maxUsuarios = maxUsuarios;
    }

    public Usuario Registrar(string nombre, string password, string codigoCasa)
    {
        nombre = (nombre ?? "").Trim();

        if (codigoCasa != _codigoCasa)
            throw new ReglaNegocioException("El código de la casa no es correcto.");
        if (nombre.Length < 2 || nombre.Length > 30)
            throw new ReglaNegocioException("El nombre debe tener entre 2 y 30 caracteres.");
        if ((password ?? "").Length < 6)
            throw new ReglaNegocioException("La contraseña debe tener al menos 6 caracteres.");
        if (_dal.Contar() >= _maxUsuarios)
            throw new ReglaNegocioException($"Ya hay {_maxUsuarios} usuarios registrados.");
        if (_dal.ObtenerPorNombre(nombre) is not null)
            throw new ReglaNegocioException("Ya existe un usuario con ese nombre.");

        var usuario = new Usuario { Nombre = nombre, PasswordHash = Seguridad.Hashear(password!) };
        usuario.Id = _dal.Insertar(usuario);
        return usuario;
    }

    // Devuelve null si los datos no coinciden. A propósito no decimos
    // si falló el usuario o la contraseña: eso le daría pistas a un atacante.
    public Usuario? Login(string nombre, string password)
    {
        var usuario = _dal.ObtenerPorNombre((nombre ?? "").Trim());
        if (usuario is null) return null;
        return Seguridad.Verificar(password ?? "", usuario.PasswordHash) ? usuario : null;
    }

    public List<Usuario> Listar() => _dal.Listar();
}
