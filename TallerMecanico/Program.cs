using TallerMecanico.Servicios;
using TallerMecanico.UI;

namespace TallerMecanico
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Cualquier error no controlado se muestra prolijo en vez de cerrar la aplicacion.
            Application.ThreadException += (_, e) =>
                MessageBox.Show(Datos.Db.TraducirError(e.Exception), "Error inesperado",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

            // Login -> sistema. Al "Cerrar sesion" se vuelve al login; al cerrar la ventana, se sale.
            string? ultimoCorreo = null;
            while (true)
            {
                using (var login = new FrmLogin(ultimoCorreo))
                {
                    if (login.ShowDialog() != DialogResult.OK) return;
                }
                ultimoCorreo = Sesion.Actual?.Correo;

                using var principal = new FrmPrincipal();
                Application.Run(principal);
                Sesion.Cerrar();
                if (!principal.CerroSesion) return;
            }
        }
    }
}
