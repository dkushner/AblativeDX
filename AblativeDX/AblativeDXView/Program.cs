using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

using AblativeDX.Simulations;

namespace AblativeDX
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var simulation = new PhysXFluid())
                simulation.Run();
        }
    }
}
