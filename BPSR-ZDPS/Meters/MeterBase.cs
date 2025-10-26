using BPSR_ZDPS.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.Meters
{
    public class MeterBase
    {
        public string Name = "";

        public virtual void Draw(MainWindow mainWindow) { }
    }
}
