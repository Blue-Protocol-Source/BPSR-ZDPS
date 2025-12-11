using System.Text;

namespace BPSR_ZDPS.DataTypes.Modules
{
    public class SolverConfig
    {
        public Dictionary<int, bool> QualitiesV2 = new Dictionary<int, bool>() { { 2, false }, { 3, true }, { 4, true } };
        public List<StatPrio> StatPrioritys = [];
        public byte[] LinkLevelBonus = [1,2,3,4,8,12];

        public string SaveToString(bool asBase64 = false)
        {
            var sb = new StringBuilder();
            sb.Append("ZMO:");
            for (int i = 0; i < StatPrioritys.Count; i++)
            {
                var stat = $"{StatPrioritys[i].Id}-{StatPrioritys[i].MinLevel}";
                sb.Append($"{stat}{(StatPrioritys.Count - 1 == i ? "" : ",")}");
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
                    QualitiesV2.Clear();
                    StatPrioritys.Clear();

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
                                MinLevel = int.Parse(statParts[1])
                            };

                            StatPrioritys.Add(prio);
                        }
                    }

                    StatPrioritys = StatPrioritys.Take(8).ToList();
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
