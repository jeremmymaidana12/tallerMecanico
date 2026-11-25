using System.ComponentModel;
using System.Data;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;
using TallerMecanico.Servicios;
using TallerMecanico.UI;

namespace TallerMecanico.Formularios;

/// <summary>
/// ABM de empresas (lo usa el administrador). Al dar de alta una empresa
/// se crea automaticamente su sucursal "Casa Central".
/// </summary>
[DesignerCategory("Code")]
public class FrmEmpresas : FrmAbmBase
{
    private readonly EmpresaService _servicio = new();

    private readonly TextBox txtNombre = new() { MaxLength = 150 };
    private readonly TextBox txtRuc = new() { MaxLength = 30 };
    private readonly TextBox txtDireccion = new() { MaxLength = 255 };
    private readonly TextBox txtCorreo = new() { MaxLength = 120 };
    private readonly TextBox txtRubro = new() { MaxLength = 100 };
    private readonly TextBox txtImagen = new() { MaxLength = 500 };

    public FrmEmpresas()
    {
        Titulo = "Empresas";
        Subtitulo = "Empresas que usan el sistema. Cada una nace con su sucursal Casa Central.";
        NombreEntidad = "empresa";
        NombreEntidadPlural = "empresas";
        TextoAlta = "Nueva empresa";
        TextoEdicion = "Editar empresa";

        AgregarCampo("Nombre / Razón social", txtNombre, obligatorio: true);
        AgregarCampo("RUC", txtRuc);
        AgregarCampo("Dirección", txtDireccion, ayuda: "También se usa para la Casa Central.");
        AgregarCampo("Correo", txtCorreo);
        AgregarCampo("Rubro", txtRubro);
        AgregarCampo("URL del logo", txtImagen);
    }

    protected override DataTable ObtenerDatos() => Db.ConsultarTabla(@"
        SELECT e.id,
               e.nombre                    AS `Empresa`,
               COALESCE(e.ruc, '')         AS `RUC`,
               COALESCE(e.rubro, '')       AS `Rubro`,
               COALESCE(e.correo, '')      AS `Correo`,
               (SELECT COUNT(*) FROM sucursales s WHERE s.id_empresa = e.id) AS `Sucursales`
          FROM empresas e
      ORDER BY e.nombre");

    protected override void LimpiarCampos()
    {
        foreach (var t in new[] { txtNombre, txtRuc, txtDireccion, txtCorreo, txtRubro, txtImagen })
            t.Clear();
    }

    protected override void CargarRegistro(object id)
    {
        var e = _servicio.Obtener(Convert.ToInt64(id))
                ?? throw new InvalidOperationException("La empresa ya no existe. Actualizá la pantalla.");
        txtNombre.Text = e.Nombre;
        txtRuc.Text = e.Ruc ?? "";
        txtDireccion.Text = e.Direccion ?? "";
        txtCorreo.Text = e.Correo ?? "";
        txtRubro.Text = e.Rubro ?? "";
        txtImagen.Text = e.ImgUrl ?? "";
    }

    protected override bool ValidarCampos()
    {
        var nombre = txtNombre.Text.Trim();
        if (nombre.Length == 0) return MarcarError(txtNombre, "Ingresá el nombre de la empresa.");
        var correo = txtCorreo.Text.Trim();
        if (correo.Length > 0 && (!correo.Contains('@') || !correo.Contains('.')))
            return MarcarError(txtCorreo, "El correo no parece válido.");

        var ruc = txtRuc.Text.Trim();
        if (ruc.Length > 0)
        {
            var repetidos = Db.Escalar("SELECT COUNT(*) FROM empresas WHERE ruc = @ruc AND (@id IS NULL OR id <> @id)",
                Db.P("@ruc", ruc), Db.P("@id", IdActual));
            if (Convert.ToInt32(repetidos) > 0)
                return MarcarError(txtRuc, "Ya hay una empresa con ese RUC.");
        }
        return true;
    }

    protected override object? Guardar(object? id)
    {
        var empresa = new Empresa
        {
            Id = id is null ? 0 : Convert.ToInt64(id),
            Nombre = txtNombre.Text.Trim(),
            Ruc = Texto.ONulo(txtRuc.Text),
            Direccion = Texto.ONulo(txtDireccion.Text),
            Correo = Texto.ONulo(txtCorreo.Text),
            Rubro = Texto.ONulo(txtRubro.Text),
            ImgUrl = Texto.ONulo(txtImagen.Text)
        };
        return (int)_servicio.Procesar(id is null ? Operacion.Crear : Operacion.Editar, empresa);
    }

    protected override void Eliminar(object id) => _servicio.Eliminar(Convert.ToInt64(id));

    protected override void AjustarColumnas(DataGridView grilla)
    {
        if (grilla.Columns["Empresa"] is { } e) { e.FillWeight = 180; e.DefaultCellStyle.Font = Tema.FuenteNegrita; }
        if (grilla.Columns["Sucursales"] is { } s) s.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
    }
}
