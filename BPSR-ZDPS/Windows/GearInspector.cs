using BPSR_ZDPS.DataTypes;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Zproto;

namespace BPSR_ZDPS.Windows
{
    public static class GearInspector
    {
        public const string LAYER = "GearInspectorLayer";
        public static string TITLE_ID = "###GearInspectorWindow";
        public static string TITLE = "Gear Inspector";

        public static Entity? LoadedEntity { get; set; }
        static List<Zproto.EquipNine> LoadedEquipList = new();
        static Dictionary<int, EquipAttrData> Attributes = new();

        public static bool IsOpened = false;

        static int RunOnceDelayed = 0;

        static bool ShowBasicAttributes = false;
        static bool ShowAdvancedAttributes = true;
        static bool ShowUnknownAttributes = false;

        static Dictionary<string, int> GearSlots = new()
        {
            { "Weapon", 200 },
            { "Head", 201 },
            { "Chest", 202 },
            { "Hands", 203 },
            { "Boots", 204 },
            { "Earring", 205 },
            { "Necklace", 206 },
            { "Ring", 207 },
            { "Bracelet (L)", 208 },
            { "Bracelet (R)", 209 },
            { "Charm", 210 },
        };

        public static void Open()
        {
            RunOnceDelayed = 0;
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            IsOpened = true;
            ImGui.PopID();
        }

        public static void LoadEntity(Entity entity)
        {
            Attributes.Clear();
            Attributes = new();
            LoadedEntity = entity;
        }

        public static void Draw(MainWindow mainWindow)
        {
            if (LoadedEntity == null)
            {
                return;
            }

            if (!IsOpened)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(640, 600), ImGuiCond.FirstUseEver);

            ImGui.SetNextWindowSizeConstraints(new Vector2(400, 150), new Vector2(ImGui.GETFLTMAX()));

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            string entityName = "";
            if (!string.IsNullOrEmpty(LoadedEntity.Name))
            {
                entityName = $"{LoadedEntity.Name} [{LoadedEntity.UID}]";
            }
            else
            {
                entityName = $"[{LoadedEntity.UID}]";
            }

            if (ImGui.Begin($"{TITLE} - {entityName}{TITLE_ID}", ref IsOpened, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking))
            {
                if (RunOnceDelayed == 0)
                {
                    RunOnceDelayed++;
                }
                else if (RunOnceDelayed == 1)
                {
                    RunOnceDelayed++;
                    Utils.SetCurrentWindowIcon();
                    Utils.BringWindowToFront();
                }

                var attrEquipData = LoadedEntity.GetAttrKV("AttrEquipData");
                if (attrEquipData != null)
                {
                    if (attrEquipData is Newtonsoft.Json.Linq.JArray)
                    {
                        attrEquipData = ((Newtonsoft.Json.Linq.JArray)attrEquipData).ToObject<List<Zproto.EquipNine>>();
                    }

                    if (attrEquipData is List<Zproto.EquipNine>)
                    {
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextUnformatted("Show Basic Attributes: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##ShowBasicAttributes", ref ShowBasicAttributes);

                        ImGui.SameLine();

                        ImGui.AlignTextToFramePadding();
                        ImGui.TextUnformatted("Show Advanced Attributes: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##ShowAdvancedAttributes", ref ShowAdvancedAttributes);

                        ImGui.SameLine();

                        ImGui.AlignTextToFramePadding();
                        ImGui.TextUnformatted("Show Unknown Attributes: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##ShowUnknownAttributes", ref ShowUnknownAttributes);

                        var equipInfo = (List<Zproto.EquipNine>)attrEquipData;

                        if (Attributes.Count == 0 || CheckIfGearChanged(equipInfo))
                        {
                            LoadedEquipList = equipInfo;
                            RebuildGearData(equipInfo);
                        }

                        foreach (var entry in Attributes)
                        {
                            ImGui.BeginChild($"GearSlot_{entry.Key}", ImGuiChildFlags.Borders | ImGuiChildFlags.AutoResizeY);

                            ImGui.TextUnformatted($"Slot ({entry.Key}): {entry.Value.SlotName}");

                            if (entry.Value.Item == null)
                            {
                                if (entry.Value.Equip?.Id > 0)
                                {
                                    ImGui.TextUnformatted($"Item ({entry.Value.Equip.Id}): <UNKNOWN>");
                                }
                                else
                                {
                                    ImGui.TextUnformatted($"Item: <UNKNOWN>");
                                }
                                ImGui.EndChild();
                                continue;
                            }

                            Vector4 qualityColor = Colors.White;
                            int itemQuality = entry.Value.Item.Quality;
                            if (itemQuality == 1)
                            {
                                qualityColor = Colors.Green;
                            }
                            else if (itemQuality == 2)
                            {
                                qualityColor = Colors.Blue;
                            }
                            else if (itemQuality == 3)
                            {
                                qualityColor = Colors.Purple;
                            }
                            else if (itemQuality == 4)
                            {
                                qualityColor = Colors.Gold;
                            }
                            else if (itemQuality == 5)
                            {
                                qualityColor = Colors.Red;
                            }

                            if (itemQuality < 4)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, Colors.OrangeRed);
                                ImGui.TextUnformatted("Note: Items below Mythic and Legendary Quality may have wrong Advanced Attributes");
                                ImGui.PopStyleColor();
                            }
                            if (LoadedEntity.SubProfessionId == 0 && itemQuality == 5)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, Colors.OrangeRed);
                                ImGui.TextUnformatted("Note: Unknown player spec, showing first spec attribute list");
                                ImGui.PopStyleColor();
                            }
                            ImGui.PushStyleColor(ImGuiCol.Text, qualityColor);
                            ImGui.TextUnformatted($"Item ({entry.Value.Equip.Id}): {entry.Value.Item.Name}");
                            ImGui.PopStyleColor();
                            ImGui.SameLine();
                            ImGui.TextUnformatted($"GS: {entry.Value.Equip.EquipGs}");

                            if (entry.Value.BreakThroughs.Any())
                            {
                                ImGui.AlignTextToFramePadding();
                                ImGui.TextUnformatted("Break Through:");
                                ImGui.SameLine();

                                string previewText = "";
                                if (entry.Value.SelectedBreakThroughLevel == 0)
                                {
                                    previewText = $"0 [GS: {entry.Value.Equip.EquipGs}]";
                                }
                                else if (entry.Value.SelectedBreakThroughLevel > 0)
                                {
                                    previewText = $"{entry.Value.SelectedBreakThroughLevel} [GS: {entry.Value.BreakThroughs[entry.Value.SelectedBreakThroughLevel].EquipGs}]";
                                }
                                if (ImGui.BeginCombo($"##GearBreakThrough_{entry.Key}", previewText))
                                {
                                    if (ImGui.Selectable($"0 [GS: {entry.Value.Equip.EquipGs}]", entry.Value.SelectedBreakThroughLevel == 0, ImGuiSelectableFlags.SpanAllColumns))
                                    {
                                        entry.Value.SelectedBreakThroughLevel = 0;
                                    }

                                    foreach (var breakthrough in entry.Value.BreakThroughs)
                                    {
                                        bool isSelected = entry.Value.SelectedBreakThroughLevel == breakthrough.Key;

                                        if (ImGui.Selectable($"{breakthrough.Key} [GS: {breakthrough.Value.EquipGs}]", isSelected, ImGuiSelectableFlags.SpanAllColumns))
                                        {
                                            entry.Value.SelectedBreakThroughLevel = breakthrough.Key;
                                        }
                                    }
                                    ImGui.EndCombo();
                                }
                            }

                            if (ShowBasicAttributes)
                            {
                                ImGui.TextUnformatted("Basic Attributes");
                                ImGui.Indent();
                                var basicAttrList = entry.Value.SelectedBreakThroughLevel > 0 ? entry.Value.BreakThroughs[entry.Value.SelectedBreakThroughLevel].BasicAttrList : entry.Value.BasicAttrList;

                                foreach (var basicAttr in basicAttrList)
                                {
                                    ImGui.TextUnformatted($"{basicAttr.name}");
                                    ImGui.SameLine();
                                    float minValue = basicAttr.min;
                                    float maxValue = basicAttr.max;
                                    string formatSymbol = "";
                                    if (basicAttr.numFormat == 1)
                                    {
                                        minValue = MathF.Round(minValue / 100.0f, 0);
                                        maxValue = MathF.Round(maxValue / 100.0f, 0);
                                        formatSymbol = "%";
                                    }

                                    ImGui.BeginDisabled();
                                    ImGui.TextUnformatted($"({minValue}{formatSymbol} - {maxValue}{formatSymbol})");
                                    ImGui.EndDisabled();
                                }
                                ImGui.Unindent();
                            }
                            
                            if (ShowAdvancedAttributes)
                            {
                                ImGui.TextUnformatted("Advanced Attributes");
                                ImGui.Indent();
                                if (ShowUnknownAttributes)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Text, Colors.Purple_Transparent);
                                    ImGui.TextUnformatted("Rare Attribute:"); // (Enchant) Purple Line
                                    ImGui.PopStyleColor();
                                    ImGui.SameLine();
                                    ImGui.TextDisabled("<UNKNOWN>");
                                }

                                var advancedAttrList = entry.Value.SelectedBreakThroughLevel > 0 ? entry.Value.BreakThroughs[entry.Value.SelectedBreakThroughLevel].AdvancedAttrList : entry.Value.AdvancedAttrList;

                                foreach (var advancedAttr in advancedAttrList)
                                {
                                    ImGui.TextUnformatted($"{advancedAttr.name}");
                                    ImGui.SameLine();
                                    float minValue = advancedAttr.min;
                                    float maxValue = advancedAttr.max;
                                    string formatSymbol = "";
                                    if (advancedAttr.numFormat == 1)
                                    {
                                        minValue = MathF.Round(minValue / 100.0f, 0);
                                        maxValue = MathF.Round(maxValue / 100.0f, 0);
                                        formatSymbol = "%";
                                    }

                                    ImGui.BeginDisabled();
                                    ImGui.TextUnformatted($"({minValue}{formatSymbol} - {maxValue}{formatSymbol})");
                                    ImGui.EndDisabled();
                                }

                                if (ShowUnknownAttributes)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Text, Colors.Yellow_Transparent);
                                    ImGui.TextUnformatted("Reforged Attribute:"); // (Recast) Reforge Line
                                    ImGui.PopStyleColor();
                                    ImGui.SameLine();
                                    ImGui.TextDisabled("<UNKNOWN>");
                                }
                                
                                ImGui.Unindent();
                            }

                            ImGui.EndChild();
                        }
                    }
                    else
                    {
                        ImGui.TextUnformatted("No Gear Data Format Invalid");
                    }
                }
                else
                {
                    ImGui.TextUnformatted("No Gear Data To Load");
                }

                ImGui.End();

                ImGui.PopID();
            }
        }

        public static bool CheckIfGearChanged(List<EquipNine>? equipInfo)
        {
            if (equipInfo == null)
            {
                return true;
            }

            if (LoadedEquipList.Count != equipInfo.Count)
            {
                return true;
            }
            for (int i = 0; i < LoadedEquipList.Count; i++)
            {
                if (LoadedEquipList[i].Slot != equipInfo[i].Slot)
                {
                    return true;
                }
                if (LoadedEquipList[i].EquipID != equipInfo[i].EquipID)
                {
                    return true;
                }
            }

            return false;
        }

        public static void RebuildGearData(List<Zproto.EquipNine> equipInfo)
        {
            if (equipInfo.Count < 11)
            {
                System.Diagnostics.Debug.WriteLine($"RebuildGearData equipInfos.Count == {equipInfo.Count}");
                //return;
            }

            foreach (var gearSlot in GearSlots)
            {
                Attributes[gearSlot.Value] = new();
                Attributes[gearSlot.Value].SlotName = gearSlot.Key;
                Attributes[gearSlot.Value].SlotId = gearSlot.Value;

                var equip = equipInfo.Where(x => x.Slot == gearSlot.Value).FirstOrDefault();
                if (equip == null)
                {
                    continue;
                }

                if (HelperMethods.DataTables.Equips.Data.TryGetValue(equip.EquipID.ToString(), out var equipData))
                {
                    if (HelperMethods.DataTables.Items.Data.TryGetValue(equip.EquipID.ToString(), out var itemData))
                    {
                        
                        ResolveAttrsForEquip(equipData, Attributes[gearSlot.Value]);
                        Attributes[gearSlot.Value].Equip = equipData;
                        Attributes[gearSlot.Value].Item = itemData;

                        var breakthroughs = HelperMethods.DataTables.EquipBreakThroughs.Data.Where(x => x.Value.EquipId == equip.EquipID);
                        if (breakthroughs != null)
                        {
                            foreach (var breakthrough in breakthroughs)
                            {
                                var tempEquipData = new Equip()
                                {
                                    Id = breakthrough.Value.EquipId,
                                    EquipPart = equipData.EquipPart,
                                    EquipProfession = equipData.EquipProfession,
                                    EquipGs = breakthrough.Value.EquipGs,
                                    BasicAttrLibId = breakthrough.Value.BasicAttrLibId,
                                    AdvancedAttrLibId = breakthrough.Value.AdvancedAttrLibId
                                };

                                var newBreakthrough = new BreakThroughData()
                                {
                                    Id = breakthrough.Value.Id,
                                    EquipGs = breakthrough.Value.EquipGs,
                                    BreakThroughTime = breakthrough.Value.BreakThroughTime
                                };

                                ResolveAttrsForEquip(tempEquipData, newBreakthrough);

                                Attributes[gearSlot.Value].BreakThroughs.Add(breakthrough.Value.BreakThroughTime, newBreakthrough);
                            }
                        }
                    }
                }
                else
                {
                    var unknownEquipData = new Equip()
                    {
                        Id = equip.EquipID,
                        EquipPart = equip.Slot
                    };
                    Attributes[gearSlot.Value].Equip = unknownEquipData;
                }
            }
        }

        public static void ResolveAttrsForEquip(Equip equipData, AttrData equipAttrData)
        {
            Dictionary<int, EquipAttrLib> basicAttrs = BuildAttrListFromLibIds(equipData.BasicAttrLibId, equipData.EquipPart);

            equipAttrData.BasicAttrs = basicAttrs;
            equipAttrData.BasicAttrList = BuildTypeAttrList(basicAttrs);

            Dictionary<int, EquipAttrLib> advancedAttrs = BuildAttrListFromLibIds(equipData.AdvancedAttrLibId, equipData.EquipPart);

            equipAttrData.AdvancedAttrs = advancedAttrs;
            equipAttrData.AdvancedAttrList = BuildTypeAttrList(advancedAttrs);
        }

        public static Dictionary<int, EquipAttrLib> BuildAttrListFromLibIds(List<int> libIds, int equipPart)
        {
            Dictionary<int, EquipAttrLib> attrs = new();
            int libVersion = 1;
            if (libIds.Count > 0)
            {
                libVersion = libIds[0];
            }
            int idx = 0;
            foreach (var attr in libIds)
            {
                if (idx == 0)
                {
                    idx++;
                    continue;
                }
                if (libVersion == 1)
                {
                    var matched = HelperMethods.DataTables.EquipAttrLibs.Data.Where(x => x.Value.AttrLibId == attr && x.Value.AllowPart.Contains(equipPart)).FirstOrDefault();
                    if (matched.Value != null && matched.Value != default)
                    {
                        attrs.Add(attr, matched.Value);
                    }
                }
                else if (libVersion == 2)
                {
                    var matched = HelperMethods.DataTables.EquipAttrSchoolLibs.Data.Where(x => {
                        if (x.Value.AttrLibId == attr && x.Value.AllowPart.Contains(equipPart))
                        {
                            if (LoadedEntity.SubProfessionId > 0)
                            {
                                if (x.Value.TalentSchoolId.Contains(Professions.GetTalentIdFromSubProfessionId(LoadedEntity.SubProfessionId)))
                                {
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            return true;
                        }
                        return false;
                        }).FirstOrDefault();
                    if (matched.Value != null && matched.Value != default)
                    {
                        // We're converting down to the more basic type since we don't care about the 2 extra fields the School version has
                        attrs.Add(attr, matched.Value.ToEquipAttrLib());
                    }
                }
                idx++;
            }

            return attrs;
        }

        public static List<(string name, int min, int max, int numFormat)> BuildTypeAttrList(Dictionary<int, EquipAttrLib> typeAttrs)
        {
            List<(string name, int min, int max, int numFormat)> attrs = new();
            foreach (var attr in typeAttrs)
            {
                int effectIdx = 0;
                foreach (var attrEffect in attr.Value.AttrEffect)
                {
                    int attrType = 1;
                    if(attrEffect.Count > 0)
                    {
                        attrType = attrEffect[0];
                    }

                    if (attrType == 1)
                    {
                        foreach (var fightAttr in HelperMethods.DataTables.FightAttrs.Data)
                        {
                            if (fightAttr.Value.AttrAdd == attrEffect[1])
                            {
                                // fightAttr.Value.AttrNumType 0 = Raw Number, 1 = Percent (Divide value by 100)
                                int valueMin = attr.Value.AttrEffectConfig[effectIdx][0];
                                int valueMax = attr.Value.AttrEffectConfig[effectIdx][1];
                                attrs.Add((fightAttr.Value.OfficialName, valueMin, valueMax, fightAttr.Value.AttrNumType));
                            }
                        }
                    }
                    else if (attrType == 3)
                    {
                        if (HelperMethods.DataTables.Buffs.Data.TryGetValue(attrEffect[1].ToString(), out var buff))
                        {
                            int valueMin = attr.Value.AttrEffectConfig[effectIdx][0];
                            int valueMax = attr.Value.AttrEffectConfig[effectIdx][1];

                            bool usedTipsDescription = false;
                            if (buff.TipsDescription > 0)
                            {
                                if (HelperMethods.DataTables.AttrDescriptions.Data.TryGetValue(buff.TipsDescription.ToString(), out var attrDescription))
                                {
                                    string desc = attrDescription.DescriptionDecisionResolve(new List<int>() { valueMin, valueMax }, out var formats);
                                    
                                    if (formats.Count > 0)
                                    {
                                        attrs.Add((desc, valueMin, valueMax, formats.FirstOrDefault()));
                                        usedTipsDescription = true;
                                    }
                                }
                            }

                            if (!usedTipsDescription)
                            {
                                attrs.Add((buff.Desc, valueMin, valueMax, 0));
                            }
                        }
                    }
                    effectIdx++;
                }
            }
            return attrs;
        }
    }

    public class AttrData
    {
        public Dictionary<int, EquipAttrLib> BasicAttrs = new();
        public List<(string name, int min, int max, int numFormat)> BasicAttrList = new();
        public Dictionary<int, EquipAttrLib> AdvancedAttrs = new();
        public List<(string name, int min, int max, int numFormat)> AdvancedAttrList = new();
    }

    public class EquipAttrData : AttrData
    {
        public string SlotName { get; set; }
        public int SlotId { get; set; }

        public Dictionary<int, BreakThroughData> BreakThroughs = new();
        public int SelectedBreakThroughLevel = 0;

        public DataTypes.Equip? Equip;
        public DataTypes.Item? Item;
    }

    public class BreakThroughData : AttrData
    {
        public int Id = 0;
        public int EquipGs = 0;
        public int BreakThroughTime = 0;
    }
}
