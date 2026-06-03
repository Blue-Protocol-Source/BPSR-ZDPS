using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class DbmTable
    {
        public Dictionary<string, Dbm> Data = new();
    }

    public class Dbm
    {
        public int Id { get; set; }
        public int CountCDTime { get; set; }
        public string Content { get; set; }
    }
}
