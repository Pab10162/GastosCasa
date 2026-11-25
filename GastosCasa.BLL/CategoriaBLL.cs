using GastosCasa.BE;
using GastosCasa.DAL;

namespace GastosCasa.BLL;

public class CategoriaBLL
{
    private readonly CategoriaDAL _dal;

    public CategoriaBLL(CategoriaDAL dal) => _dal = dal;

    public List<Categoria> Listar() => _dal.Listar();
}
