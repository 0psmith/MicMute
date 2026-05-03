using System;
using System.Windows.Forms;

namespace MicMute
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
            {
                MessageBox.Show(e.Exception.Message, "MicMute", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                Exception exception = e.ExceptionObject as Exception;
                MessageBox.Show(exception == null ? "An unexpected error occurred." : exception.Message, "MicMute", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            using (MicMuteApplicationContext context = new MicMuteApplicationContext())
            {
                Application.Run(context);
            }
        }
    }
}
