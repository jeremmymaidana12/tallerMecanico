using System.Data;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;
using TallerMecanico.UI;

namespace TallerMecanico.Formularios;

/// <summary>ABM de vehiculos. Hereda todo el diseno y comportamiento de FrmAbmBase.</summary>
public partial class FrmVehiculos : FrmAbmBase
{
    private const int AnhoMinimo = 1950;
    private static readonly string[] ColoresSugeridos =
    [
        "Blanco", "Negro", "Gris", "Plata", "Rojo", "Azul", "Verde", "Amarillo",
        "Bordó", "Beige", "Marrón", "Naranja", "Dorado", "Celeste"
    ];

    private readonly VehiculoDatos _datos = new();

    private readonly TextBox txtChapa = new() { CharacterCasing = CharacterCasing.Upper, MaxLength = 20 };
    private readonly ComboBox cboCliente = new()
    {
        DropDownStyle = ComboBoxStyle.DropDown,
        AutoCompleteMode = AutoCompleteMode.SuggestAppend,
        AutoCompleteSource = AutoCompleteSource.ListItems
    };
    private readonly ComboBox cboMarca = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox txtModelo = new() { MaxLength = 100 };
    private readonly ComboBox cboAnho = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox cboColor = new()
    {
        DropDownStyle = ComboBoxStyle.DropDown,
        MaxLength = 50,
        AutoCompleteMode = AutoCompleteMode.SuggestAppend,
        AutoCompleteSource = AutoCompleteSource.ListItems
    };
    private readonly TextBox txtObservacion = new()
    {
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        MaxLength = 1000,
        AcceptsReturn = true
    };

    public FrmVehiculos()
    {
        InitializeComponent();

        Titulo = "Vehículos";
        Subtitulo = "Registro de los vehículos de los clientes del taller";
        NombreEntidad = "vehículo";
        NombreEntidadPlural = "vehículos";
        TextoAlta = "Nuevo vehículo";
        TextoEdicion = "Editar vehículo";

        AgregarCampo("Chapa", txtChapa, obligatorio: true, ayuda: "Se guarda en mayúsculas. No puede repetirse.");
        AgregarCampo("Cliente (dueño)", cboCliente, obligatorio: true, ayuda: "Escribí para buscar por apellido o documento.");
        AgregarCampo("Marca", cboMarca);
        AgregarCampo("Modelo", txtModelo);
        AgregarCampo("Año", cboAnho);
        AgregarCampo("Color", cboColor);
        AgregarCampo("Observación", txtObservacion, alto: 84);

        cboColor.Items.AddRange(ColoresSugeridos);
        CargarAnhos();
    }

    protected override DataTable ObtenerDatos() => _datos.Listar();

    protected override void CargarListas()
    {
        cboCliente.DisplayMember = "descripcion";
        cboCliente.ValueMember = "id";
        cboCliente.DataSource = _datos.ListarClientes();

        var marcas = _datos.ListarMarcas();
        // La tabla viene con "id" como clave no nula; se libera para la opcion "sin marca".
        marcas.PrimaryKey = [];
        marcas.Columns["id"]!.AllowDBNull = true;
        var sinMarca = marcas.NewRow();
        sinMarca["id"] = DBNull.Value;
        sinMarca["descripcion"] = "— Sin especificar —";
        marcas.Rows.InsertAt(sinMarca, 0);
        cboMarca.DisplayMember = "descripcion";
        cboMarca.ValueMember = "id";
        cboMarca.DataSource = marcas;
    }

    protected override void LimpiarCampos()
    {
        txtChapa.Clear();
        cboCliente.SelectedIndex = -1;
        cboCliente.Text = "";
        if (cboMarca.Items.Count > 0) cboMarca.SelectedIndex = 0;
        txtModelo.Clear();
        cboAnho.SelectedIndex = 0;
        cboColor.Text = "";
        txtObservacion.Clear();
    }

    protected override void CargarRegistro(object id)
    {
        var v = _datos.Obtener(Convert.ToInt32(id))
                ?? throw new InvalidOperationException("El vehículo ya no existe. Actualizá la pantalla.");

        txtChapa.Text = v.Chapa;
        cboCliente.SelectedValue = v.IdCliente;
        if (v.IdMarca is null) cboMarca.SelectedIndex = 0;
        else cboMarca.SelectedValue = v.IdMarca.Value;
        txtModelo.Text = v.Modelo ?? "";
        SeleccionarAnho(v.Anho);
        cboColor.Text = v.Color ?? "";
        txtObservacion.Text = v.Observacion ?? "";
    }

    protected override bool ValidarCampos()
    {
        var chapa = txtChapa.Text.Trim();
        if (chapa.Length == 0)
            return MarcarError(txtChapa, "Ingresá la chapa del vehículo.");
        if (VehiculoDatos.NormalizarChapa(chapa).Length < 3)
            return MarcarError(txtChapa, "La chapa parece incompleta.");

        int? idActual = IdActual is null ? null : Convert.ToInt32(IdActual);
        if (_datos.ExisteChapa(chapa, idActual))
            return MarcarError(txtChapa, $"Ya existe un vehículo con la chapa {chapa.ToUpperInvariant()}.");

        if (cboCliente.Items.Count == 0)
            return MarcarError(cboCliente, "No hay clientes cargados. Primero registrá un cliente.");
        if (ClienteSeleccionado() is null)
            return MarcarError(cboCliente, "Seleccioná un cliente de la lista.");

        return true;
    }

    protected override object? Guardar(object? id)
    {
        var vehiculo = new Vehiculo
        {
            Id = id is null ? 0 : Convert.ToInt32(id),
            Chapa = txtChapa.Text.Trim().ToUpperInvariant(),
            IdCliente = ClienteSeleccionado()!.Value,
            IdMarca = cboMarca.SelectedValue is int marca ? marca : null,
            Modelo = TextoONulo(txtModelo.Text),
            Anho = cboAnho.SelectedItem is int anho ? anho : null,
            Color = TextoONulo(cboColor.Text),
            Observacion = TextoONulo(txtObservacion.Text)
        };

        if (id is null) return _datos.Insertar(vehiculo);

        _datos.Actualizar(vehiculo);
        return id;
    }

    protected override void Eliminar(object id) => _datos.Eliminar(Convert.ToInt32(id));

    protected override string DescribirSeleccion() =>
        Convert.ToString(dgvDatos.CurrentRow?.Cells["Chapa"].Value) ?? "";

    protected override void AjustarColumnas(DataGridView grilla)
    {
        void Peso(string columna, float peso, int minimo)
        {
            if (grilla.Columns[columna] is not { } c) return;
            c.FillWeight = peso;
            c.MinimumWidth = minimo;
        }
        Peso("Chapa", 80, 105);
        Peso("Marca", 85, 100);
        Peso("Modelo", 90, 90);
        Peso("Año", 50, 70);
        Peso("Color", 65, 80);
        Peso("Cliente", 140, 140);
        Peso("Documento", 80, 100);
        Peso("Teléfono", 85, 115);

        if (grilla.Columns["Chapa"] is { } chapa)
            chapa.DefaultCellStyle.Font = Tema.FuenteNegrita;
        if (grilla.Columns["Año"] is { } anho)
            anho.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
    }

    #region Auxiliares

    private void CargarAnhos()
    {
        cboAnho.Items.Add("— Sin especificar —");
        for (int a = DateTime.Today.Year + 1; a >= AnhoMinimo; a--)
            cboAnho.Items.Add(a);
        cboAnho.SelectedIndex = 0;
    }

    private void SeleccionarAnho(int? anho)
    {
        if (anho is null)
        {
            cboAnho.SelectedIndex = 0;
            return;
        }
        if (!cboAnho.Items.Contains(anho.Value))
            cboAnho.Items.Add(anho.Value); // anho fuera del rango sugerido: se respeta igual
        cboAnho.SelectedItem = anho.Value;
    }

    /// <summary>Cliente elegido; tambien acepta que se haya escrito el texto exacto de la lista.</summary>
    private int? ClienteSeleccionado()
    {
        if (cboCliente.SelectedValue is int id && cboCliente.Text == cboCliente.GetItemText(cboCliente.SelectedItem))
            return id;

        var texto = cboCliente.Text.Trim();
        foreach (DataRowView fila in cboCliente.Items)
        {
            if (string.Equals(Convert.ToString(fila["descripcion"]), texto, StringComparison.CurrentCultureIgnoreCase))
                return Convert.ToInt32(fila["id"]);
        }
        return null;
    }

    private static string? TextoONulo(string texto)
    {
        var t = texto.Trim();
        return t.Length == 0 ? null : t;
    }

    #endregion
}
