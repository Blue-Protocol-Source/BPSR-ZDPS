using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class ItemTable
    {
        public Dictionary<string, Item> Data = new();
    }

    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Icon2 { get; set; }
        public int Type { get; set; }
        public int GroupId { get; set; }
        public int Quality { get; set; }
        public int SortID { get; set; }
        public string Description { get; set; }
        public string Description2 { get; set; }
        public int SexLimit { get; set; }
        public int Overlap { get; set; }
        public int TimeType { get; set; }
        public string TimeLimit { get; set; }
        public int DropModel { get; set; }
        public int CorrelationId { get; set; }
        public int SpecialDisplayType { get; set; }
        public int SpecialTips { get; set; }
        public int QuickUse { get; set; }
        public bool IsNotice { get; set; }
        public int GearTips { get; set; }
        public List<int> Warehouse { get; set; }
        public int Discard { get; set; }
        public int LuckyTag { get; set; }
    }
}
