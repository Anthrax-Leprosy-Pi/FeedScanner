using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FeedScanning {
    static class Program {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            if (Program.IsApplicationAlreadyRunning()) {
                //Allow just one instance
                return;
            }
            IsTask = args.Contains("-task");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormFeedScanner());
        }

        public static bool IsTask { get; private set; }

        private static bool IsApplicationAlreadyRunning() {
            return Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1;
        }
    }
}
