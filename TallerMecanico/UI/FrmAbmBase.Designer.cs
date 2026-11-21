namespace TallerMecanico.UI
{
    partial class FrmAbmBase
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            pnlHeader = new Panel();
            lblSubtitulo = new Label();
            lblTitulo = new Label();
            pnlToolbar = new Panel();
            flpAcciones = new FlowLayoutPanel();
            btnNuevo = new Button();
            btnEditar = new Button();
            btnEliminar = new Button();
            btnRefrescar = new Button();
            pnlBuscar = new Panel();
            txtBuscar = new TextBox();
            pnlSeparadorToolbar = new Panel();
            pnlFooter = new Panel();
            lblEstado = new Label();
            pnlEdicion = new Panel();
            pnlCampos = new FlowLayoutPanel();
            lblMensajeEdicion = new Label();
            pnlAccionesEdicion = new Panel();
            btnCancelar = new Button();
            btnGuardar = new Button();
            lblEdicionTitulo = new Label();
            pnlSeparadorEdicion = new Panel();
            pnlGrid = new Panel();
            dgvDatos = new DataGridView();
            errorProvider = new ErrorProvider(components);
            tipAyuda = new ToolTip(components);
            tmrBuscar = new System.Windows.Forms.Timer(components);
            tmrEstado = new System.Windows.Forms.Timer(components);
            pnlHeader.SuspendLayout();
            pnlToolbar.SuspendLayout();
            flpAcciones.SuspendLayout();
            pnlBuscar.SuspendLayout();
            pnlFooter.SuspendLayout();
            pnlEdicion.SuspendLayout();
            pnlAccionesEdicion.SuspendLayout();
            pnlGrid.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDatos).BeginInit();
            ((System.ComponentModel.ISupportInitialize)errorProvider).BeginInit();
            SuspendLayout();
            //
            // pnlHeader
            //
            pnlHeader.Controls.Add(lblSubtitulo);
            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(1180, 78);
            pnlHeader.TabIndex = 0;
            //
            // lblTitulo
            //
            lblTitulo.AutoSize = true;
            lblTitulo.Location = new Point(22, 12);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new Size(100, 32);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "Titulo";
            //
            // lblSubtitulo
            //
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Location = new Point(25, 48);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Size = new Size(100, 17);
            lblSubtitulo.TabIndex = 1;
            lblSubtitulo.Text = "Subtitulo";
            //
            // pnlToolbar
            //
            pnlToolbar.Controls.Add(flpAcciones);
            pnlToolbar.Controls.Add(pnlBuscar);
            pnlToolbar.Dock = DockStyle.Top;
            pnlToolbar.Location = new Point(0, 78);
            pnlToolbar.Name = "pnlToolbar";
            pnlToolbar.Padding = new Padding(24, 10, 24, 10);
            pnlToolbar.Size = new Size(1180, 58);
            pnlToolbar.TabIndex = 1;
            //
            // flpAcciones
            //
            flpAcciones.AutoSize = true;
            flpAcciones.Controls.Add(btnNuevo);
            flpAcciones.Controls.Add(btnEditar);
            flpAcciones.Controls.Add(btnEliminar);
            flpAcciones.Controls.Add(btnRefrescar);
            flpAcciones.Dock = DockStyle.Left;
            flpAcciones.Location = new Point(24, 10);
            flpAcciones.Name = "flpAcciones";
            flpAcciones.Size = new Size(480, 38);
            flpAcciones.TabIndex = 0;
            flpAcciones.WrapContents = false;
            //
            // btnNuevo
            //
            btnNuevo.Margin = new Padding(0, 0, 8, 0);
            btnNuevo.Name = "btnNuevo";
            btnNuevo.Size = new Size(110, 38);
            btnNuevo.TabIndex = 0;
            btnNuevo.Text = "＋  Nuevo";
            //
            // btnEditar
            //
            btnEditar.Margin = new Padding(0, 0, 8, 0);
            btnEditar.Name = "btnEditar";
            btnEditar.Size = new Size(110, 38);
            btnEditar.TabIndex = 1;
            btnEditar.Text = "✎  Editar";
            //
            // btnEliminar
            //
            btnEliminar.Margin = new Padding(0, 0, 8, 0);
            btnEliminar.Name = "btnEliminar";
            btnEliminar.Size = new Size(110, 38);
            btnEliminar.TabIndex = 2;
            btnEliminar.Text = "✕  Eliminar";
            //
            // btnRefrescar
            //
            btnRefrescar.Margin = new Padding(0);
            btnRefrescar.Name = "btnRefrescar";
            btnRefrescar.Size = new Size(118, 38);
            btnRefrescar.TabIndex = 3;
            btnRefrescar.Text = "⟳  Actualizar";
            //
            // pnlBuscar
            //
            pnlBuscar.Controls.Add(txtBuscar);
            pnlBuscar.Dock = DockStyle.Right;
            pnlBuscar.Location = new Point(836, 10);
            pnlBuscar.Name = "pnlBuscar";
            pnlBuscar.Padding = new Padding(0, 6, 0, 0);
            pnlBuscar.Size = new Size(320, 38);
            pnlBuscar.TabIndex = 1;
            //
            // txtBuscar
            //
            txtBuscar.Dock = DockStyle.Top;
            txtBuscar.Location = new Point(0, 6);
            txtBuscar.Name = "txtBuscar";
            txtBuscar.PlaceholderText = "🔍  Buscar...  (Ctrl+F)";
            txtBuscar.Size = new Size(320, 25);
            txtBuscar.TabIndex = 0;
            //
            // pnlSeparadorToolbar
            //
            pnlSeparadorToolbar.Dock = DockStyle.Top;
            pnlSeparadorToolbar.Location = new Point(0, 136);
            pnlSeparadorToolbar.Name = "pnlSeparadorToolbar";
            pnlSeparadorToolbar.Size = new Size(1180, 1);
            pnlSeparadorToolbar.TabIndex = 2;
            //
            // pnlFooter
            //
            pnlFooter.Controls.Add(lblEstado);
            pnlFooter.Dock = DockStyle.Bottom;
            pnlFooter.Location = new Point(0, 668);
            pnlFooter.Name = "pnlFooter";
            pnlFooter.Padding = new Padding(24, 0, 24, 0);
            pnlFooter.Size = new Size(1180, 32);
            pnlFooter.TabIndex = 3;
            //
            // lblEstado
            //
            lblEstado.Dock = DockStyle.Fill;
            lblEstado.Location = new Point(24, 0);
            lblEstado.Name = "lblEstado";
            lblEstado.Size = new Size(1132, 32);
            lblEstado.TabIndex = 0;
            lblEstado.Text = "Listo";
            lblEstado.TextAlign = ContentAlignment.MiddleLeft;
            //
            // pnlEdicion
            //
            pnlEdicion.Controls.Add(pnlCampos);
            pnlEdicion.Controls.Add(lblMensajeEdicion);
            pnlEdicion.Controls.Add(pnlAccionesEdicion);
            pnlEdicion.Controls.Add(lblEdicionTitulo);
            pnlEdicion.Dock = DockStyle.Right;
            pnlEdicion.Location = new Point(800, 137);
            pnlEdicion.Name = "pnlEdicion";
            pnlEdicion.Padding = new Padding(22, 16, 18, 16);
            pnlEdicion.Size = new Size(380, 531);
            pnlEdicion.TabIndex = 4;
            //
            // lblEdicionTitulo
            //
            lblEdicionTitulo.Dock = DockStyle.Top;
            lblEdicionTitulo.Location = new Point(22, 16);
            lblEdicionTitulo.Name = "lblEdicionTitulo";
            lblEdicionTitulo.Size = new Size(340, 40);
            lblEdicionTitulo.TabIndex = 0;
            lblEdicionTitulo.Text = "Nuevo registro";
            //
            // pnlCampos
            //
            pnlCampos.AutoScroll = true;
            pnlCampos.Dock = DockStyle.Fill;
            pnlCampos.FlowDirection = FlowDirection.TopDown;
            pnlCampos.Location = new Point(22, 56);
            pnlCampos.Name = "pnlCampos";
            pnlCampos.Size = new Size(340, 361);
            pnlCampos.TabIndex = 1;
            pnlCampos.WrapContents = false;
            //
            // lblMensajeEdicion
            //
            lblMensajeEdicion.Dock = DockStyle.Bottom;
            lblMensajeEdicion.Location = new Point(22, 417);
            lblMensajeEdicion.Name = "lblMensajeEdicion";
            lblMensajeEdicion.Padding = new Padding(10, 0, 10, 0);
            lblMensajeEdicion.Size = new Size(340, 44);
            lblMensajeEdicion.TabIndex = 2;
            lblMensajeEdicion.TextAlign = ContentAlignment.MiddleLeft;
            lblMensajeEdicion.Visible = false;
            //
            // pnlAccionesEdicion
            //
            pnlAccionesEdicion.Controls.Add(btnCancelar);
            pnlAccionesEdicion.Controls.Add(btnGuardar);
            pnlAccionesEdicion.Dock = DockStyle.Bottom;
            pnlAccionesEdicion.Location = new Point(22, 461);
            pnlAccionesEdicion.Name = "pnlAccionesEdicion";
            pnlAccionesEdicion.Size = new Size(340, 54);
            pnlAccionesEdicion.TabIndex = 3;
            //
            // btnGuardar
            //
            btnGuardar.Location = new Point(0, 12);
            btnGuardar.Name = "btnGuardar";
            btnGuardar.Size = new Size(170, 40);
            btnGuardar.TabIndex = 0;
            btnGuardar.Text = "✓  Guardar  (Ctrl+S)";
            //
            // btnCancelar
            //
            btnCancelar.Location = new Point(180, 12);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(130, 40);
            btnCancelar.TabIndex = 1;
            btnCancelar.Text = "Cancelar  (Esc)";
            //
            // pnlSeparadorEdicion
            //
            pnlSeparadorEdicion.Dock = DockStyle.Right;
            pnlSeparadorEdicion.Location = new Point(799, 137);
            pnlSeparadorEdicion.Name = "pnlSeparadorEdicion";
            pnlSeparadorEdicion.Size = new Size(1, 531);
            pnlSeparadorEdicion.TabIndex = 5;
            //
            // pnlGrid
            //
            pnlGrid.Controls.Add(dgvDatos);
            pnlGrid.Dock = DockStyle.Fill;
            pnlGrid.Location = new Point(0, 137);
            pnlGrid.Name = "pnlGrid";
            pnlGrid.Padding = new Padding(24, 16, 24, 8);
            pnlGrid.Size = new Size(799, 531);
            pnlGrid.TabIndex = 6;
            //
            // dgvDatos
            //
            dgvDatos.Dock = DockStyle.Fill;
            dgvDatos.Location = new Point(24, 16);
            dgvDatos.Name = "dgvDatos";
            dgvDatos.Size = new Size(751, 507);
            dgvDatos.TabIndex = 0;
            //
            // errorProvider
            //
            errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;
            errorProvider.ContainerControl = this;
            //
            // tmrBuscar
            //
            tmrBuscar.Interval = 250;
            //
            // tmrEstado
            //
            tmrEstado.Interval = 4000;
            //
            // FrmAbmBase
            //
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1180, 700);
            Controls.Add(pnlGrid);
            Controls.Add(pnlSeparadorEdicion);
            Controls.Add(pnlEdicion);
            Controls.Add(pnlFooter);
            Controls.Add(pnlSeparadorToolbar);
            Controls.Add(pnlToolbar);
            Controls.Add(pnlHeader);
            MinimumSize = new Size(900, 560);
            Name = "FrmAbmBase";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ABM";
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            flpAcciones.ResumeLayout(false);
            pnlBuscar.ResumeLayout(false);
            pnlBuscar.PerformLayout();
            pnlToolbar.ResumeLayout(false);
            pnlToolbar.PerformLayout();
            pnlFooter.ResumeLayout(false);
            pnlAccionesEdicion.ResumeLayout(false);
            pnlEdicion.ResumeLayout(false);
            pnlGrid.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvDatos).EndInit();
            ((System.ComponentModel.ISupportInitialize)errorProvider).EndInit();
            ResumeLayout(false);
        }

        #endregion

        // Los controles son "protected" para que los formularios hijos
        // puedan usarlos y para que el disenador permita la herencia visual.
        protected Panel pnlHeader;
        protected Label lblTitulo;
        protected Label lblSubtitulo;
        protected Panel pnlToolbar;
        protected FlowLayoutPanel flpAcciones;
        protected Button btnNuevo;
        protected Button btnEditar;
        protected Button btnEliminar;
        protected Button btnRefrescar;
        protected Panel pnlBuscar;
        protected TextBox txtBuscar;
        protected Panel pnlSeparadorToolbar;
        protected Panel pnlFooter;
        protected Label lblEstado;
        protected Panel pnlEdicion;
        protected Label lblEdicionTitulo;
        protected FlowLayoutPanel pnlCampos;
        protected Label lblMensajeEdicion;
        protected Panel pnlAccionesEdicion;
        protected Button btnGuardar;
        protected Button btnCancelar;
        protected Panel pnlSeparadorEdicion;
        protected Panel pnlGrid;
        protected DataGridView dgvDatos;
        protected ErrorProvider errorProvider;
        protected ToolTip tipAyuda;
        private System.Windows.Forms.Timer tmrBuscar;
        private System.Windows.Forms.Timer tmrEstado;
    }
}
