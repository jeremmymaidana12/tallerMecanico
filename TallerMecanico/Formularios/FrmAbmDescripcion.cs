using System.ComponentModel;
using System.Data;
using TallerMecanico.Datos;
using TallerMecanico.UI;

namespace TallerMecanico.Formularios;

/// <summary>
/// ABM reutilizable para tablas que solo tienen "id" y "descripcion"
/// (marcas, categorias, tipos de empleado...). Para sumar otra tabla igual
/// alcanza con agregar un metodo de fabrica como Marcas() o Categorias().
/// </summary>
[DesignerCategory("Code")]
public class FrmAbmDescripcion : FrmAbmBase
{
    private readonly string _tabla;

    /// <summary>Descripcion que no se puede renombrar ni borrar (ej.: el rol Administrador).</summary>
    private string? _protegido;
    private readonly TextBox txtDescripcion = new();

    public FrmAbmDescripcion(string tabla, string titulo, string subtitulo,
                             string singular, string plural, bool femenino, int largoMaximo)
    {
        _tabla = tabla;
        Titulo = titulo;
        Subtitulo = subtitulo;
        NombreEntidad = singular;
        NombreEntidadPlural = plural;
        TextoAlta = (femenino ? "Nueva " : "Nuevo ") + singular;
        TextoEdicion = "Editar " + singular;

        txtDescripcion.MaxLength = largoMaximo;
        AgregarCampo("Descripción", txtDescripcion, obligatorio: true, ayuda: "No puede repetirse.");
    }

    public static FrmAbmDescripcion Marcas() => new("marcas", "Marcas",
        "Marcas de vehículos que atiende el taller", "marca", "marcas", true, 100);

    public static FrmAbmDescripcion Categorias() => new("categorias", "Categorías",
        "Agrupan los artículos y servicios del taller", "categoría", "categorías", true, 120);

    public static FrmAbmDescripcion TiposEmpleado() => new("tipos_empleados", "Roles",
        "Roles del personal. El rol Administrador ve la sección Administración.", "rol", "roles", false, 100)
    {
        _protegido = Servicios.Sesion.RolAdministrador
    };

    private bool EsProtegido(object id) => _protegido is not null && string.Equals(
        Convert.ToString(Db.Escalar($"SELECT descripcion FROM {_tabla} WHERE id = @id", Db.P("@id", id))),
        _protegido, StringComparison.OrdinalIgnoreCase);

    protected override DataTable ObtenerDatos() => Db.ConsultarTabla(
        $"SELECT id, descripcion AS `Descripción` FROM {_tabla} ORDER BY descripcion");

    protected override void LimpiarCampos() => txtDescripcion.Clear();

    protected override void CargarRegistro(object id) =>
        txtDescripcion.Text = Convert.ToString(Db.Escalar(
            $"SELECT descripcion FROM {_tabla} WHERE id = @id", Db.P("@id", id)))
            ?? throw new InvalidOperationException("El registro ya no existe. Actualizá la pantalla.");

    protected override bool ValidarCampos()
    {
        var texto = txtDescripcion.Text.Trim();
        if (texto.Length == 0)
            return MarcarError(txtDescripcion, "Ingresá la descripción.");
        if (IdActual is not null && EsProtegido(IdActual) && !string.Equals(texto, _protegido, StringComparison.OrdinalIgnoreCase))
            return MarcarError(txtDescripcion, $"«{_protegido}» es del sistema y no se puede renombrar.");

        var repetidos = Db.Escalar(
            $"SELECT COUNT(*) FROM {_tabla} WHERE UPPER(TRIM(descripcion)) = UPPER(@d) AND (@id IS NULL OR id <> @id)",
            Db.P("@d", texto), Db.P("@id", IdActual));
        if (Convert.ToInt32(repetidos) > 0)
            return MarcarError(txtDescripcion, $"Ya existe «{texto}».");
        return true;
    }

    protected override object? Guardar(object? id)
    {
        var texto = txtDescripcion.Text.Trim();
        if (id is null)
            return (int)Db.Insertar($"INSERT INTO {_tabla} (descripcion) VALUES (@d)", Db.P("@d", texto));

        Db.Ejecutar($"UPDATE {_tabla} SET descripcion = @d WHERE id = @id", Db.P("@d", texto), Db.P("@id", id));
        return id;
    }

    protected override void Eliminar(object id)
    {
        if (EsProtegido(id))
            throw new InvalidOperationException($"«{_protegido}» es del sistema y no se puede eliminar.");
        Db.Ejecutar($"DELETE FROM {_tabla} WHERE id = @id", Db.P("@id", id));
    }
}
