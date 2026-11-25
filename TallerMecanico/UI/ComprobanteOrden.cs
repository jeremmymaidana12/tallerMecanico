using System.Drawing.Printing;
using TallerMecanico.Modelos;

namespace TallerMecanico.UI;

/// <summary>Ver e imprimir una orden de trabajo guardada (mismo estilo que ComprobanteVenta).</summary>
public static class ComprobanteOrden
{
    private sealed record Renglon(string Tipo, string Descripcion, string Cantidad, decimal Precio, decimal Subtotal);

    private static List<Renglon> Renglones(OrdenTrabajo o) =>
    [
        .. o.Servicios.Select(s => new Renglon("Servicio",
            s.Servicio + (string.IsNullOrWhiteSpace(s.Observacion) ? "" : $" ({s.Observacion.Trim()})"), "1", s.Precio, s.Precio)),
        .. o.Articulos.Select(a => new Renglon("Artículo", a.Articulo, Lienzo.Numero(a.Cantidad), a.Precio, a.Subtotal))
    ];

    /// <param name="datos">Renglones del encabezado (vehiculo, cliente, mecanico...).</param>
    public static void Ver(IWin32Window dueno, OrdenTrabajo o, IReadOnlyList<string> datos)
    {
        using var ventana = new Form
        {
            Text = $"Orden de trabajo #{o.Id}", StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false,
            BackColor = Tema.Superficie, ClientSize = new Size(Lienzo.Px(680), Lienzo.Px(520)), Padding = new Padding(Lienzo.Px(18))
        };
        var grilla = new DataGridView { Dock = DockStyle.Fill };
        Tema.Grilla(grilla);
        foreach (var titulo in new[] { "Tipo", "Descripción", "Cant.", "Precio", "Subtotal" })
            grilla.Columns.Add(titulo, titulo);
        grilla.Columns[0].FillWeight = 60;
        grilla.Columns[1].FillWeight = 200;
        for (int i = 2; i < 5; i++) grilla.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        foreach (var r in Renglones(o))
            grilla.Rows.Add(r.Tipo, r.Descripcion, r.Cantidad, Lienzo.Gs(r.Precio), Lienzo.Gs(r.Subtotal));

        var encabezado = datos.ToList();
        if (!string.IsNullOrWhiteSpace(o.Diagnostico)) encabezado.Add("Diagnóstico: " + o.Diagnostico.Trim());

        ventana.Controls.Add(grilla);
        ventana.Controls.Add(new Label
        {
            Dock = DockStyle.Top, Height = Lienzo.Px(26 + 20 * encabezado.Count), Font = Lienzo.Fuente(13),
            Text = string.Join("\n", encabezado), ForeColor = Tema.Texto, AutoEllipsis = true
        });
        ventana.Controls.Add(new Label
        {
            Dock = DockStyle.Bottom, Height = Lienzo.Px(64), Font = Lienzo.Fuente(14, true), ForeColor = Tema.Texto,
            TextAlign = ContentAlignment.MiddleRight,
            Text = $"Servicios {Lienzo.Gs(o.TotalServicios)}   Artículos {Lienzo.Gs(o.TotalArticulos)}\nTotal {Lienzo.Gs(o.Total)}"
        });
        ventana.ShowDialog(dueno);
    }

    public static void Imprimir(IWin32Window dueno, OrdenTrabajo o, IReadOnlyList<string> datos)
    {
        var renglones = Renglones(o);
        using var documento = new PrintDocument { DocumentName = $"Orden de trabajo {o.Id}" };
        int siguiente = 0;
        documento.BeginPrint += (_, _) => siguiente = 0;
        documento.PrintPage += (_, e) =>
        {
            var g = e.Graphics!;
            using var normal = new Font("Segoe UI", 10);
            using var negrita = new Font("Segoe UI", 10, FontStyle.Bold);
            using var titulo = new Font("Segoe UI", 16, FontStyle.Bold);
            using var derecha = new StringFormat { Alignment = StringAlignment.Far };
            float y = e.MarginBounds.Top, izq = e.MarginBounds.Left, der = e.MarginBounds.Right, alto = normal.GetHeight(g) * 1.35f;

            void Fila(Font f, string tipo, string descripcion, string cantidad, string precio, string total)
            {
                g.DrawString(tipo, f, Brushes.Black, izq, y);
                g.DrawString(descripcion, f, Brushes.Black, new RectangleF(izq + 80, y, der - izq - 380, alto));
                g.DrawString(cantidad, f, Brushes.Black, der - 220, y, derecha);
                g.DrawString(precio, f, Brushes.Black, der - 110, y, derecha);
                g.DrawString(total, f, Brushes.Black, der, y, derecha);
                y += alto;
            }

            g.DrawString($"Orden de trabajo #{o.Id}", titulo, Brushes.Black, izq, y);
            y += titulo.GetHeight(g) * 1.4f;
            if (siguiente == 0)
            {
                foreach (var dato in datos) { g.DrawString(dato, normal, Brushes.Black, izq, y); y += alto; }
                if (!string.IsNullOrWhiteSpace(o.Diagnostico))
                {
                    var caja = new RectangleF(izq, y, der - izq, alto * 4);
                    var texto = "Diagnóstico: " + o.Diagnostico.Trim();
                    var medida = g.MeasureString(texto, normal, (int)caja.Width);
                    g.DrawString(texto, normal, Brushes.Black, caja);
                    y += Math.Min(medida.Height, caja.Height);
                }
                y += alto / 2;
            }
            Fila(negrita, "Tipo", "Descripción", "Cant.", "Precio", "Subtotal");
            while (siguiente < renglones.Count)
            {
                if (y > e.MarginBounds.Bottom - alto * 5) { e.HasMorePages = true; return; }
                var r = renglones[siguiente++];
                Fila(normal, r.Tipo, r.Descripcion, r.Cantidad, Lienzo.Gs(r.Precio), Lienzo.Gs(r.Subtotal));
            }
            y += alto / 2;
            Fila(normal, "", "", "", "Servicios", Lienzo.Gs(o.TotalServicios));
            Fila(normal, "", "", "", "Artículos", Lienzo.Gs(o.TotalArticulos));
            Fila(negrita, "", "Comprobante interno, sin valor fiscal", "", "TOTAL", Lienzo.Gs(o.Total));
            e.HasMorePages = false;
        };
        using var vista = new PrintPreviewDialog
        {
            Document = documento, Width = Lienzo.Px(800), Height = Lienzo.Px(650), StartPosition = FormStartPosition.CenterParent
        };
        vista.ShowDialog(dueno);
    }
}
