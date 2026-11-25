namespace GastosCasa.BLL;

// Error "esperado": el usuario hizo algo que las reglas no permiten
// (monto negativo, categoría inexistente...). La API lo convierte en un 400
// con el mensaje, en vez de un 500 genérico.
public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje) { }
}

// El usuario quiso tocar algo que no es suyo -> la API devuelve 403.
public class SinPermisoException : Exception
{
    public SinPermisoException(string mensaje) : base(mensaje) { }
}

// Se pidió algo que no existe (por ejemplo, un Id borrado) -> la API devuelve 404.
public class NoEncontradoException : Exception
{
    public NoEncontradoException(string mensaje) : base(mensaje) { }
}
