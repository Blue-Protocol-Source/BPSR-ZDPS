using System.Text.RegularExpressions;

namespace BPSR_ZDPS.DataTypes
{
    public class AttrDescriptionTable
    {
        public Dictionary<string, AttrDescription> Data = [];
    }

    public class AttrDescription
    {
        public string Id { get; set; }
        public string Description { get; set; }

        public string DescriptionDecisionResolve(List<int> Decisions, out List<int> DecisionFormats)
        {
            DecisionFormats = new();

            if (string.IsNullOrEmpty(Description))
            {
                return "";
            }

            var matches = Regex.Matches(Description, @"\{\*Decision\.(\w+)\((\d+)\)\*\}");
            string result = Description;

            int idx = 0;
            foreach (Match match in matches)
            {
                string format = match.Groups[1].Value;
                int entryIndex = int.Parse(match.Groups[2].Value);

                int decisionIndex = entryIndex - 1;

                if (decisionIndex < 0 || decisionIndex >= Decisions.Count)
                {
                    continue;
                }

                int value = Decisions[decisionIndex];

                string replacement = "";

                if (format == "unmarkpercent")
                {
                    DecisionFormats.Add(1);
                    replacement = $"{Math.Round(value / 100.0f, 0)}%";
                }
                else if (format == "unmarknormal")
                {
                    DecisionFormats.Add(0);
                    replacement = value.ToString();
                }

                result = result.Remove(match.Index, match.Length).Insert(match.Index, replacement);

                idx++;
            }

            return result;
        }
    }
}
