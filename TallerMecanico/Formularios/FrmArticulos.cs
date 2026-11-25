using System.ComponentModel;
using System.Data;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;
using TallerMecanico.Servicios;
using TallerMecanico.UI;

namespace TallerMecanico.Formularios;

/// <summary>ABM de articulos (repuestos e insumos que se venden o usan en las ordenes).</summary>
[DesignerCategory("Code")]
public class FrmArticulos : FrmAbmBase
{
    private readonly ArticuloService _servicio = new();

    private readonly TextBox txtDescripcion = new() { MaxLength = 150 };
    private readonly ComboBox cboCategoria = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown nudPrecio = new()
    {
        Maximum = 9_999_999_999m,
        DecimalPlaces = 0,
        ThousandsSeparator = true,
        TextAlign = HorizontalAlignment.Right
    };
    private readonly NumericUpDown nudStock = new()
    {
        Minimum = -999_999,
        Maximum = 999_999,
        ThousandsSeparator = true,
        TextAlign = HorizontalAlignment.Right
    };

    public FrmArticulos()
    {
        Titulo = "Artículos";
        Subtitulo = "Repuestos e insumos con su precio y stock";
        NombreEntidad = "artículo";
        NombreEntidadPlural = "artículos";
        TextoAlta = "Nuevo artículo";
        TextoEdicion = "Editar artículo";

        AgregarCampo("Descripción", txtDescripcion, obligatorio: true);
        AgregarCampoConAlta("Categoría", cboCategoria, "Crear una categoría nueva", CrearCategoria);
        AgregarCampo("Precio (Gs.)", nudPrecio, obligatorio: true);
        AgregarCampo("Stock", nudStock, ayuda: "Las ventas lo descuentan automáticamente.");
    }

    protected override DataTable ObtenerDatos() => Db.ConsultarTabla(@"
        SELECT a.id,
               a.descripcion                  AS `Artículo`,
               COALESCE(c.descripcion, '')    AS `Categoría`,
               a.precio                       AS `Precio`,
               a.stock                        AS `Stock`
          FROM articulos a
     LEFT JOIN categorias c ON c.id = a.id_categoria
      ORDER BY a.descripcion");

    protected override void CargarListas() => Combos.Cargar(cboCategoria,
        Db.ConsultarTabla("SELECT id, descripcion FROM categorias ORDER BY descripcion"), Combos.SinEspecificar);

    protected override void LimpiarCampos()
    {
        txtDescripcion.Clear();
        Combos.Seleccionar(cboCategoria, null);
        nudPrecio.Value = 0;
        nudStock.Value = 0;
    }

    protected override void CargarRegistro(object id)
    {
        var a = _servicio.Obtener(Convert.ToInt64(id))
                ?? throw new InvalidOperationException("El artículo ya no existe. Actualizá la pantalla.");
        txtDescripcion.Text = a.Descripcion;
        Combos.Seleccionar(cboCategoria, a.IdCategoria);
        nudPrecio.Value = Math.Clamp(a.Precio, nudPrecio.Minimum, nudPrecio.Maximum);
        nudStock.Value = Math.Clamp(a.Stock, nudStock.Minimum, nudStock.Maximum);
    }

    protected override bool ValidarCampos()
    {
        var texto = txtDescripcion.Text.Trim();
        if (texto.Length == 0) return MarcarError(txtDescripcion, "Ingresá la descripción del artículo.");
        var repetidos = Db.Escalar(
            "SELECT COUNT(*) FROM articulos WHERE UPPER(TRIM(descripcion)) = UPPER(@d) AND (@id IS NULL OR id <> @id)",
            Db.P("@d", texto), Db.P("@id", IdActual));
        if (Convert.ToInt32(repetidos) > 0)
            return MarcarError(txtDescripcion, "Ya existe un artículo con esa descripción.");
        return true;
    }

    protected override object? Guardar(object? id)
    {
        var articulo = new Articulo
        {
            Id = id is null ? 0 : Convert.ToInt64(id),
            Descripcion = txtDescripcion.Text.Trim(),
            IdCategoria = Combos.Valor(cboCategoria),
            Precio = nudPrecio.Value,
            Stock = (int)nudStock.Value
        };
        return (int)_servicio.Procesar(id is null ? Operacion.Crear : Operacion.Editar, articulo);
    }

    protected override void Eliminar(object id) => _servicio.Eliminar(Convert.ToInt64(id));

    protected override void AjustarColumnas(DataGridView grilla)
    {
        if (grilla.Columns["Precio"] is { } precio) precio.DefaultCellStyle.Format = "N0";
        if (grilla.Columns["Stock"] is { } stock) stock.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        if (grilla.Columns["Artículo"] is { } art) art.FillWeight = 200;
    }

    private void CrearCategoria()
    {
        using var frm = FrmAbmDescripcion.Categorias();
        if (frm.CrearEnDialogo(TopLevelControl) is { } nuevo)
        {
            CargarListas();
            Combos.Seleccionar(cboCategoria, nuevo);
        }
    }
}
