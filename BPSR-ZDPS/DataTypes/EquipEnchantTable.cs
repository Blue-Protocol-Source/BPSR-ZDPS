using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Zproto.World.Types;

namespace BPSR_ZDPS.DataTypes
{
    public class EquipEnchantTable
    {
        public Dictionary<string, EquipEnchant> Data = new();
    }

    public class EquipEnchant
    {
        public int Id { get; set; }
        public int EnchantId { get; set; }
        public int EnchantType { get; set; }
        public List<int> EnchantItemList { get; set; }
        //public object EnchantConsume { get; set; }
        public List<int> RecommendedGem { get; set; }
    }
}
