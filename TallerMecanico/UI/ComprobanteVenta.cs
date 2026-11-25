using System.Drawing.Printing;
using TallerMecanico.Modelos;

namespace TallerMecanico.UI;

/// <summary>Ver e imprimir una venta guardada.</summary>
public static class ComprobanteVenta
{
    /// <param name="datos">Renglones del encabezado (cliente, vendedor, fecha...).</param>
    public static void Ver(IWin32Window dueno, VentaCabecera v, IReadOnlyList<string> datos)
    {
        using var ventana = new Form
        {
            Text = $"Venta #{v.Id}", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, BackColor = Tema.Superficie,
            ClientSize = new Size(Lienzo.Px(620), Lienzo.Px(440)), Padding = new Padding(Lienzo.Px(18))
        };
        var grilla = new DataGridView { Dock = DockStyle.Fill };
        Tema.Grilla(grilla);
        foreach (var titulo in new[] { "Artículo", "Cant.", "Precio", "IVA", "Subtotal" })
            grilla.Columns.Add(titulo, titulo);
        grilla.Columns[0].FillWeight = 190;
        for (int i = 1; i < 5; i++) grilla.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        foreach (var d in v.Detalles)
            grilla.Rows.Add(d.Articulo, Lienzo.Numero(d.Cantidad), Lienzo.Gs(d.Precio), Lienzo.Gs(d.Iva), Lienzo.Gs(d.Subtotal));

        ventana.Controls.Add(grilla);
        ventana.Controls.Add(new Label
        {
            Dock = DockStyle.Top, Height = Lienzo.Px(26 + 20 * datos.Count), Font = Lienzo.Fuente(13),
            Text = string.Join("\n", datos), ForeColor = Tema.Texto
        });
        ventana.Controls.Add(new Label
        {
            Dock = DockStyle.Bottom, Height = Lienzo.Px(64), Font = Lienzo.Fuente(14, true), ForeColor = Tema.Texto,
            TextAlign = ContentAlignment.MiddleRight,
            Text = $"Gravado {Lienzo.Gs(v.Grav)}   IVA {Lienzo.Gs(v.Iva)}   Exento {Lienzo.Gs(v.Exc)}\nTotal {Lienzo.Gs(v.Total)}"
        });
        ventana.ShowDialog(dueno);
    }

    public static void Imprimir(IWin32Window dueno, VentaCabecera v, IReadOnlyList<string> datos)
    {
        using var documento = new PrintDocument { DocumentName = $"Venta {v.Id}" };
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

            void Fila(Font f, string a, string cantidad, string precio, string total)
            {
                g.DrawString(a, f, Brushes.Black, new RectangleF(izq, y, der - izq - 300, alto));
                g.DrawString(cantidad, f, Brushes.Black, der - 220, y, derecha);
                g.DrawString(precio, f, Brushes.Black, der - 110, y, derecha);
                g.DrawString(total, f, Brushes.Black, der, y, derecha);
                y += alto;
            }

            g.DrawString($"Venta #{v.Id}", titulo, Brushes.Black, izq, y);
            y += titulo.GetHeight(g) * 1.4f;
            if (siguiente == 0)
            {
                foreach (var dato in datos) { g.DrawString(dato, normal, Brushes.Black, izq, y); y += alto; }
                y += alto / 2;
            }
            Fila(negrita, "Artículo", "Cant.", "Precio", "Subtotal");
            while (siguiente < v.Detalles.Count)
            {
                if (y > e.MarginBounds.Bottom - alto * 6) { e.HasMorePages = true; return; }
                var d = v.Detalles[siguiente++];
                Fila(normal, d.Articulo, Lienzo.Numero(d.Cantidad), Lienzo.Gs(d.Precio), Lienzo.Gs(d.Subtotal));
            }
            y += alto / 2;
            Fila(normal, "", "", "Gravado", Lienzo.Gs(v.Grav));
            Fila(normal, "", "", "IVA (10%)", Lienzo.Gs(v.Iva));
            Fila(normal, "", "", "Exento", Lienzo.Gs(v.Exc));
            Fila(negrita, "Comprobante interno, sin valor fiscal", "", "TOTAL", Lienzo.Gs(v.Total));
            e.HasMorePages = false;
        };
        using var vista = new PrintPreviewDialog
        {
            Document = documento, Width = Lienzo.Px(800), Height = Lienzo.Px(650), StartPosition = FormStartPosition.CenterParent
        };
        vista.ShowDialog(dueno);
    }
}
