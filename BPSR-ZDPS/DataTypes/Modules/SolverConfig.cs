using Serilog;
using System.Collections.Frozen;
using System.Text;

namespace BPSR_ZDPS.DataTypes.Modules
{
    public class SolverConfig
    {
        public static byte[] DefaultLinkLevels = [1, 2, 4, 8, 16, 32];
        public Dictionary<int, bool> QualitiesV2 = new Dictionary<int, bool>() { { 2, false }, { 3, true }, { 4, true } };
        public List<StatPrio> StatPriorities = [];
        public byte[] LinkLevelBonus = DefaultLinkLevels;
        public bool ValueAllStats = true;

        public string SaveToString(bool asBase64 = false)
        {
            var sb = new StringBuilder();
            sb.Append("ZMO:");
            for (int i = 0; i < StatPriorities.Count; i++)
            {
                var stat = $"{StatPriorities[i].Id}-{StatPriorities[i].MinLevel}-{StatPriorities[i].ReqLevel}";
                sb.Append($"{stat}{(StatPriorities.Count - 1 == i ? "" : ",")}");
            }

            sb.Append("|");
            /*for (int i = 0; i < QualitiesV2.Count; i++)
            {
                sb.Append($"{QualitiesV2[i]}{(QualitiesV2.Count - 1 == i ? "" : ",")}");
            }*/

            if (asBase64)
            {
                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
                return System.Convert.ToBase64String(plainTextBytes);
            }

            return sb.ToString();
        }

        public void FromString(string str)
        {
            try
            {
                if (str.StartsWith("ZMO:"))
                {
                    //QualitiesV2.Clear();
                    StatPriorities.Clear();

                    var configParts = str.Substring(4).Split('|');
                    if (configParts.Length > 1)
                    {
                        var statPriorites = configParts[0].Split(",");
                        foreach (var stat in statPriorites)
                        {
                            var statParts = stat.Split("-");
                            var prio = new StatPrio()
                            {
                                Id = int.Parse(statParts[0]),
                                MinLevel = 0, //Math.Clamp(int.Parse(statParts[1]), 0, 20),
                                ReqLevel = Math.Clamp(int.Parse(statParts[2]), 0, 20),
                            };

                            StatPriorities.Add(prio);
                        }
                    }

                    StatPriorities = StatPriorities.Take(12).ToList();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error parsing preset string for config.");
            }
        }

        public bool Verify(FrozenDictionary<int, ModStatInfo> modStats)
        {
            foreach (var stat in StatPriorities)
            {
                if (!modStats.ContainsKey(stat.Id))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
