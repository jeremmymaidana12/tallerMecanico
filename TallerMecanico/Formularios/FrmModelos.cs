using System.ComponentModel;
using System.Data;
using TallerMecanico.Datos;
using TallerMecanico.UI;

namespace TallerMecanico.Formularios;

/// <summary>ABM de modelos de vehiculo (cada modelo pertenece a una marca).</summary>
[DesignerCategory("Code")]
public class FrmModelos : FrmAbmBase
{
    private readonly ComboBox cboMarca = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox txtDescripcion = new() { MaxLength = 100 };

    /// <summary>Marca que se propone al abrir el alta (la usa el "+" de Vehiculos).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int? MarcaInicial { get; set; }

    public FrmModelos()
    {
        Titulo = "Modelos";
        Subtitulo = "Modelos de vehículos agrupados por marca";
        NombreEntidad = "modelo";
        NombreEntidadPlural = "modelos";
        TextoAlta = "Nuevo modelo";
        TextoEdicion = "Editar modelo";

        AgregarCampoConAlta("Marca", cboMarca, "Crear una marca nueva", CrearMarca, obligatorio: true);
        AgregarCampo("Modelo", txtDescripcion, obligatorio: true, ayuda: "Ej.: Hilux, Corolla, Gol.");
    }

    protected override DataTable ObtenerDatos() => Db.ConsultarTabla(@"
        SELECT mo.id, ma.descripcion AS `Marca`, mo.descripcion AS `Modelo`
          FROM modelos mo
          JOIN marcas ma ON ma.id = mo.id_marca
      ORDER BY ma.descripcion, mo.descripcion");

    protected override void CargarListas() =>
        Combos.Cargar(cboMarca, Db.ConsultarTabla("SELECT id, descripcion FROM marcas ORDER BY descripcion"));

    protected override void LimpiarCampos()
    {
        if (MarcaInicial is null) cboMarca.SelectedIndex = -1;
        else Combos.Seleccionar(cboMarca, MarcaInicial);
        txtDescripcion.Clear();
    }

    protected override void CargarRegistro(object id)
    {
        var t = Db.ConsultarTabla("SELECT id_marca, descripcion FROM modelos WHERE id = @id", Db.P("@id", id));
        if (t.Rows.Count == 0) throw new InvalidOperationException("El modelo ya no existe. Actualizá la pantalla.");
        Combos.Seleccionar(cboMarca, t.Rows[0]["id_marca"]);
        txtDescripcion.Text = Convert.ToString(t.Rows[0]["descripcion"]);
    }

    protected override bool ValidarCampos()
    {
        if (Combos.Valor(cboMarca) is not int marca)
            return MarcarError(cboMarca, "Seleccioná la marca.");
        var texto = txtDescripcion.Text.Trim();
        if (texto.Length == 0)
            return MarcarError(txtDescripcion, "Ingresá el nombre del modelo.");

        var repetidos = Db.Escalar(@"
            SELECT COUNT(*) FROM modelos
             WHERE id_marca = @m AND UPPER(TRIM(descripcion)) = UPPER(@d) AND (@id IS NULL OR id <> @id)",
            Db.P("@m", marca), Db.P("@d", texto), Db.P("@id", IdActual));
        if (Convert.ToInt32(repetidos) > 0)
            return MarcarError(txtDescripcion, $"Esa marca ya tiene el modelo «{texto}».");
        return true;
    }

    protected override object? Guardar(object? id)
    {
        var marca = Combos.Valor(cboMarca);
        var texto = txtDescripcion.Text.Trim();
        if (id is null)
            return (int)Db.Insertar("INSERT INTO modelos (id_marca, descripcion) VALUES (@m, @d)",
                Db.P("@m", marca), Db.P("@d", texto));

        Db.Ejecutar("UPDATE modelos SET id_marca = @m, descripcion = @d WHERE id = @id",
            Db.P("@m", marca), Db.P("@d", texto), Db.P("@id", id));
        return id;
    }

    protected override void Eliminar(object id) =>
        Db.Ejecutar("DELETE FROM modelos WHERE id = @id", Db.P("@id", id));

    private void CrearMarca()
    {
        using var frm = FrmAbmDescripcion.Marcas();
        if (frm.CrearEnDialogo(TopLevelControl) is { } nuevo)
        {
            CargarListas();
            Combos.Seleccionar(cboMarca, nuevo);
        }
    }
}
