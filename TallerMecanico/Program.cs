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

            Application.Run(new FrmPrincipal());
        }
    }
}
