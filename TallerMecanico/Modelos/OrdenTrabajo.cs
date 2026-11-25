namespace TallerMecanico.Modelos;

/// <summary>Representa una fila de ordenes_trabajo con sus dos detalles (servicios y articulos).</summary>
public sealed class OrdenTrabajo : EntidadBase
{
    public int IdEmpleado { get; set; }
    public int IdVehiculo { get; set; }
    public int IdEmpresa { get; set; }
    public DateTime FechaRecepcion { get; set; } = DateTime.Now;
    public EstadoOrdenTrabajo Estado { get; set; } = EstadoOrdenTrabajo.Recibido;
    public string? Diagnostico { get; set; }
    public DateTime? FechaDiagnostico { get; set; }
    public DateTime? FechaFinalizacion { get; set; }

    public List<OrdenTrabajoServicio> Servicios { get; set; } = [];
    public List<OrdenTrabajoArticulo> Articulos { get; set; } = [];

    public decimal TotalServicios => Servicios.Sum(s => s.Precio);
    public decimal TotalArticulos => Articulos.Sum(a => a.Subtotal);
    public decimal Total => TotalServicios + TotalArticulos;

    /// <summary>Completa las fechas segun el estado: diagnostico y finalizacion se registran una sola vez.</summary>
    public void ActualizarFechas()
    {
        bool diagnosticada = Estado >= EstadoOrdenTrabajo.Diagnosticado && Estado != EstadoOrdenTrabajo.Cancelado
                             || !string.IsNullOrWhiteSpace(Diagnostico);
        if (diagnosticada) FechaDiagnostico ??= DateTime.Now;

        bool terminada = Estado is EstadoOrdenTrabajo.Finalizado or EstadoOrdenTrabajo.Entregado;
        if (terminada) FechaFinalizacion ??= DateTime.Now;
        else if (Estado != EstadoOrdenTrabajo.Cancelado) FechaFinalizacion = null;
    }
}

/// <summary>Fila de ordenes_trabajo_servicios.</summary>
public sealed class OrdenTrabajoServicio : EntidadBase
{
    public long IdOrden { get; set; }
    public int IdServicio { get; set; }
    public string? Observacion { get; set; }
    public decimal Precio { get; set; }

    /// <summary>Descripcion del servicio (viene de servicios, no se guarda).</summary>
    public string Servicio { get; set; } = "";
}

/// <summary>Fila de ordenes_trabajo_articulos.</summary>
public sealed class OrdenTrabajoArticulo : EntidadBase
{
    public long IdOrden { get; set; }
    public int IdArticulo { get; set; }
    public int Cantidad { get; set; } = 1;
    public decimal Precio { get; set; }

    /// <summary>Descripcion del articulo (viene de articulos, no se guarda).</summary>
    public string Articulo { get; set; } = "";

    public decimal Subtotal => Math.Round(Cantidad * Precio, 2, MidpointRounding.AwayFromZero);
}
