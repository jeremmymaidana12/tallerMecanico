using System.ComponentModel;
using System.Data;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;
using TallerMecanico.Servicios;
using TallerMecanico.UI;

namespace TallerMecanico.Formularios;

/// <summary>
/// Control de usuarios (solo administradores): empleados con su correo de ingreso,
/// clave (guardada con hash, ver Servicios/Seguridad.cs), rol y estado.
/// Protege que siempre quede al menos un administrador activo.
/// </summary>
[DesignerCategory("Code")]
public class FrmEmpleados : FrmAbmBase
{
    private readonly EmpleadoService _servicio = new();
    private readonly EmpresaService _empresas = new();
    private readonly SucursalService _sucursales = new();

    private readonly TextBox txtNombre = new() { MaxLength = 150 };
    private readonly TextBox txtCorreo = new() { MaxLength = 120 };
    private readonly TextBox txtClave = new() { MaxLength = 100, UseSystemPasswordChar = true };
    private readonly ComboBox cboTipo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox cboEmpresa = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox cboSucursal = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox chkActivo = new() { Text = "Activo (puede usar el sistema)", Checked = true, Height = 26 };

    public FrmEmpleados()
    {
        Titulo = "Usuarios";
        Subtitulo = "Empleados que pueden ingresar al sistema, con su rol y estado";
        NombreEntidad = "usuario";
        NombreEntidadPlural = "usuarios";
        TextoAlta = "Nuevo usuario";
        TextoEdicion = "Editar usuario";

        AgregarCampo("Nombre completo", txtNombre, obligatorio: true);
        AgregarCampo("Correo (usuario)", txtCorreo, obligatorio: true, ayuda: "Es el usuario para iniciar sesión.");
        AgregarCampo("Clave", txtClave, ayuda: "Al editar, dejala vacía para no cambiarla.");
        AgregarCampoConAlta("Rol", cboTipo, "Crear un rol nuevo", CrearTipo, obligatorio: true,
            ayuda: "Solo el rol Administrador ve la sección Administración.");
        AgregarCampo("Empresa", cboEmpresa, obligatorio: true);
        AgregarCampo("Sucursal", cboSucursal, obligatorio: true);
        AgregarCampo("Estado", chkActivo);

        cboEmpresa.SelectionChangeCommitted += (_, _) => CargarSucursales();
    }

    protected override DataTable ObtenerDatos() => Db.ConsultarTabla(@"
        SELECT em.id,
               em.nombre                         AS `Empleado`,
               em.correo                         AS `Correo`,
               t.descripcion                     AS `Rol`,
               e.nombre                          AS `Empresa`,
               s.nombre                          AS `Sucursal`,
               IF(em.activo = 1, 'Activo', 'Inactivo') AS `Estado`,
               COALESCE(DATE_FORMAT(em.ultimo_acceso, '%d/%m/%Y %H:%i'), 'Nunca') AS `Último acceso`
          FROM empleados em
          JOIN tipos_empleados t ON t.id = em.id_tipo_empleado
          JOIN empresas e        ON e.id = em.id_empresa
          JOIN sucursales s      ON s.id = em.id_sucursal
      ORDER BY em.nombre");

    protected override void CargarListas()
    {
        Combos.Cargar(cboTipo, Db.ConsultarTabla("SELECT id, descripcion FROM tipos_empleados ORDER BY descripcion"));
        Combos.Cargar(cboEmpresa, _empresas.ListarCombo());
        CargarSucursales();
    }

    private void CargarSucursales()
    {
        if (Combos.Valor(cboEmpresa) is int empresa)
            Combos.Cargar(cboSucursal, _sucursales.ListarCombo(empresa));
        else
            cboSucursal.DataSource = null;
    }

    protected override void LimpiarCampos()
    {
        txtNombre.Clear();
        txtCorreo.Clear();
        txtClave.Clear();
        cboTipo.SelectedIndex = -1;
        chkActivo.Checked = true;
        if (cboEmpresa.Items.Count > 0) cboEmpresa.SelectedIndex = 0;
        CargarSucursales();
    }

    protected override void CargarRegistro(object id)
    {
        var e = _servicio.Obtener(Convert.ToInt64(id))
                ?? throw new InvalidOperationException("El empleado ya no existe. Actualizá la pantalla.");
        txtNombre.Text = e.Nombre;
        txtCorreo.Text = e.Correo;
        Combos.Seleccionar(cboTipo, e.IdTipoEmpleado);
        Combos.Seleccionar(cboEmpresa, e.IdEmpresa);
        CargarSucursales();
        Combos.Seleccionar(cboSucursal, e.IdSucursal);
        chkActivo.Checked = e.Activo;
    }

    protected override bool ValidarCampos()
    {
        if (txtNombre.Text.Trim().Length == 0) return MarcarError(txtNombre, "Ingresá el nombre.");
        var correo = txtCorreo.Text.Trim();
        if (correo.Length == 0 || !correo.Contains('@') || !correo.Contains('.'))
            return MarcarError(txtCorreo, "Ingresá un correo válido.");
        var repetidos = Db.Escalar("SELECT COUNT(*) FROM empleados WHERE correo = @c AND (@id IS NULL OR id <> @id)",
            Db.P("@c", correo), Db.P("@id", IdActual));
        if (Convert.ToInt32(repetidos) > 0)
            return MarcarError(txtCorreo, "Ya hay un empleado con ese correo.");

        if (IdActual is null && txtClave.Text.Length == 0)
            return MarcarError(txtClave, "Ingresá una clave para el empleado nuevo.");
        if (txtClave.Text.Length > 0 && txtClave.Text.Length < 6)
            return MarcarError(txtClave, "La clave debe tener al menos 6 caracteres.");

        if (Combos.Valor(cboTipo) is not int rol) return MarcarError(cboTipo, "Seleccioná el rol.");
        if (Combos.Valor(cboEmpresa) is null) return MarcarError(cboEmpresa, "Seleccioná la empresa.");
        if (Combos.Valor(cboSucursal) is null) return MarcarError(cboSucursal, "Seleccioná la sucursal.");

        // Reglas de administradores
        if (IdActual is not null)
        {
            int id = Convert.ToInt32(IdActual);
            bool quedaAdminActivo = rol == AutenticacionService.IdRolAdministrador() && chkActivo.Checked;
            if (id == Sesion.Actual?.Id && !chkActivo.Checked)
                return MarcarError(chkActivo, "No podés desactivar tu propio usuario.");
            if (id == Sesion.Actual?.Id && !quedaAdminActivo)
                return MarcarError(cboTipo, "No podés quitarte el rol de Administrador a vos mismo.");
            if (!quedaAdminActivo && _servicio.ContarAdminsActivos(excluirId: id) == 0)
                return MarcarError(cboTipo, "Tiene que quedar al menos un administrador activo.");
        }
        return true;
    }

    protected override object? Guardar(object? id)
    {
        string hash;
        if (txtClave.Text.Length > 0) hash = Seguridad.HashClave(txtClave.Text);
        else hash = _servicio.Obtener(Convert.ToInt64(id))?.PasswordHash ?? "";

        var empleado = new Empleado
        {
            Id = id is null ? 0 : Convert.ToInt64(id),
            Nombre = txtNombre.Text.Trim(),
            Correo = txtCorreo.Text.Trim(),
            PasswordHash = hash,
            IdTipoEmpleado = Combos.Valor(cboTipo)!.Value,
            IdEmpresa = Combos.Valor(cboEmpresa)!.Value,
            IdSucursal = Combos.Valor(cboSucursal)!.Value,
            Activo = chkActivo.Checked
        };
        return (int)_servicio.Procesar(id is null ? Operacion.Crear : Operacion.Editar, empleado);
    }

    protected override void Eliminar(object id)
    {
        if (Convert.ToInt32(id) == Sesion.Actual?.Id)
            throw new InvalidOperationException("No podés eliminar tu propio usuario.");
        if (_servicio.EsAdminActivo(Convert.ToInt32(id)) && _servicio.ContarAdminsActivos(excluirId: Convert.ToInt32(id)) == 0)
            throw new InvalidOperationException("Es el último administrador activo; no se puede eliminar.");
        _servicio.Eliminar(Convert.ToInt64(id));
    }

    protected override void AjustarColumnas(DataGridView grilla)
    {
        if (grilla.Columns["Empleado"] is { } e) { e.FillWeight = 150; e.DefaultCellStyle.Font = Tema.FuenteNegrita; }
        if (grilla.Columns["Correo"] is { } c) c.FillWeight = 160;
    }

    private void CrearTipo()
    {
        using var frm = FrmAbmDescripcion.TiposEmpleado();
        if (frm.CrearEnDialogo(TopLevelControl) is { } nuevo)
        {
            CargarListas();
            Combos.Seleccionar(cboTipo, nuevo);
        }
    }
}
