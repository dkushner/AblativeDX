using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AblativeDX.Framework
{
    public class Configuration
    {
        public int WindowWidth
        {
            get;
            set;
        }
        public int WindowHeight
        {
            get;
            set;
        }
        public string WindowTitle
        {
            get;
            set;
        }

        public Configuration()
        {
            WindowTitle = "AblativeDX Simulation";
            WindowWidth = 800;
            WindowHeight = 600;
        }
    }
}
