using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.DataTypes.Modules;
using BPSR_ZDPS.Managers;
using Hexa.NET.ImGui;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Frozen;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using ZLinq;
using Zproto;

namespace BPSR_ZDPS
{
    public class ModuleSolver
    {
        private static bool IsOpen = false;
        private static PlayerModDataSave PlayerModData = new PlayerModDataSave();
        private static FrozenDictionary<int, ModStatInfo> ModStatInfos;
        private static FrozenDictionary<int, ModuleType> ModTypeMapping;
        private static string ModuleImgBasePath;
        private static int NumTotalModules = 0;
        private static int NumAttackModules = 0;
        private static int NumSupportModules = 0;
        private static int NumGuardModules = 0;
        private static string ModSavePath => Path.Combine(Utils.DATA_DIR_NAME, "ModulesSaveData.json");

        private static SolverConfig SolverConfig = new SolverConfig();
        private static ModStatInfo PendingStatToAdd = null;
        private static List<ModComboResult> BestModResults = [];
        private static bool ShouldBlockMainUI = false;
        private static bool IsCalculating = false;
        private static Task ModuleCalcTask;
        private static string CurrentPresetString = "";
        static int RunOnceDelayed = 0;

        public static List<long> FilteredModules = [];

        public static void Init()
        {
            ModuleImgBasePath = Path.Combine(Utils.DATA_DIR_NAME, "Images", "Modules");
            var effectStatTypes = HelperMethods.DataTables.ModEffects.Data.DistinctBy(x => x.Value.EffectID).Select(x => new ModStatInfo()
            {
                Name = x.Value.EffectName,
                Icon = x.Value.EffectConfigIcon,
                StatId = x.Value.EffectID,
                IconRef = ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, $"{x.Value.EffectConfigIcon.Split('/').Last()}.png")) ??
                    ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "missing.png")),
            });

            ModStatInfos = effectStatTypes.ToFrozenDictionary(x => x.StatId, y => y);
            ModTypeMapping = HelperMethods.DataTables.Modules.Data.ToFrozenDictionary(x => x.Value.Id, y => (ModuleType)y.Value.SimilarId);

            PendingStatToAdd = effectStatTypes.FirstOrDefault();

            LoadSavedModData(ModSavePath);
        }

        public static void Open()
        {
            RunOnceDelayed = 0;
            IsOpen = true;
        }

        public static void SetPlayerInv(CharSerialize data)
        {
            lock (PlayerModData)
            {
                PlayerModData = new PlayerModDataSave()
                {
                    ModulesPackage = data.ItemPackage?.Packages[5] ?? null,
                    Mod = data?.Mod ?? null
                };

                SaveModData(ModSavePath);
                ModuleInvUpdated();
            }
        }

        private static void ModuleInvUpdated()
        {
            if (PlayerModData.ModulesPackage != null)
            {
                NumTotalModules = PlayerModData.ModulesPackage.Items.Count();
                NumAttackModules = PlayerModData.ModulesPackage.Items.Count(x => IsModuleOfType(x.Value.ConfigId, ModuleType.Attack));
                NumSupportModules = PlayerModData.ModulesPackage.Items.Count(x => IsModuleOfType(x.Value.ConfigId, ModuleType.Support));
                NumGuardModules = PlayerModData.ModulesPackage.Items.Count(x => IsModuleOfType(x.Value.ConfigId, ModuleType.Guard));
            }
        }

        public static void Draw()
        {
            if (!IsOpen) return;

            var windowSize = new Vector2(800, 500);
            float leftWidth = 320;
            ImGui.SetNextWindowSize(windowSize, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(1270, 700), new Vector2(float.PositiveInfinity, float.PositiveInfinity));
            if (ImGui.Begin("Module Optimizer", ref IsOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking))
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

                var shouldBlock = CheckAndDrawNoModulesBanner();
                if (ModuleCalcTask?.Status == TaskStatus.Running)
                {
                    DrawBanner("Calculating best module combos!\nThis could take a while.", 0xFF005DD9, "Thinking.png", true);
                }

                ImGui.BeginDisabled(ShouldBlockMainUI || shouldBlock);

                if (ImGui.BeginTabBar("MainTabBar", ImGuiTabBarFlags.None))
                {
                    if (ImGui.BeginTabItem("Optimizer"))
                    {
                        DrawSolverTab(ImGui.GetContentRegionAvail(), leftWidth);
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Module Inventory"))
                    {
                        DrawModuleInv();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Settings"))
                    {
                        //if (ImGui.CollapsingHeader("Presets"))
                        {
                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("Preset Share Code: ");
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(400);
                            if (ImGui.InputText("##PresetCode", ref CurrentPresetString, 1000, ImGuiInputTextFlags.AutoSelectAll))
                            {

                            }
                            ImGui.SameLine();
                            if (ImGui.Button("Apply"))
                            {
                                SolverConfig.FromString(CurrentPresetString);
                            }
                            ImGui.SameLine();
                            if (ImGui.Button("Copy"))
                            {
                                ImGui.SetClipboardText(CurrentPresetString);
                            }
                        }

                        string[] solverNames = ["Legacy", "Fallback", "Normal"];
                        int selectedSolver = (int)Settings.Instance.WindowSettings.ModuleWindow.SolverMode;

                        ImGui.TextUnformatted("Solver Mode:");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(300);
                        ImGui.Combo("##SolverMode", ref selectedSolver, solverNames, 3);
                        Settings.Instance.WindowSettings.ModuleWindow.SolverMode = (SolverModes)selectedSolver;

                        ImGui.Spacing();
                        ImGui.SeparatorText("Debug Info");
                        ImGui.TextUnformatted($"IsHardwareAccelerated: {Vector.IsHardwareAccelerated}");
                        ImGui.TextUnformatted($"Avx2: {Avx2.IsSupported}");
                        ImGui.TextUnformatted($"Size: {Vector<byte>.Count}");

                        ImGui.Text("Settings will go here.");
                        var itsEvieFrFr = ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "Missing.png"));
                        if (itsEvieFrFr.HasValue)
                        {
                            ImGui.Image(itsEvieFrFr.Value, new Vector2(200, 200));
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                ImGui.EndDisabled();
            }

            ImGui.End();
        }

        private static async Task DrawSolverTab(Vector2 windowSize, float leftWidth)
        {
            var contentRegion = ImGui.GetContentRegionAvail();
            ImGui.SetCursorPosY(58);

            var clipStart = ImGui.GetCursorScreenPos();
            ImGui.PushClipRect(clipStart, clipStart + new Vector2(leftWidth, 20), true);
            ImGui.SeparatorText($"Config");
            ImGui.PopClipRect();
            ImGui.SetCursorPosY(85);

            var configChanged = false;
            ImGui.BeginChild("LeftSection", new Vector2(leftWidth, contentRegion.Y - 55), ImGuiChildFlags.Borders);
            ImGui.SeparatorText("Stat Priority");
            ImGui.Spacing();

            int idToRemove = -1;
            for (int i = 0; i < SolverConfig.StatPrioritys.Count; i++)
            {
                var result = DrawStatFilter(i);
                if (result.Item1)
                {
                    idToRemove = i;
                    configChanged = true;
                }

                if (result.Item2)
                {
                    configChanged = true;
                }
            }

            if (idToRemove != -1)
            {
                SolverConfig.StatPrioritys.RemoveAt(idToRemove);
            }

            ImGui.EndChild();

            var pos = ImGui.GetCursorPos();
            ImGui.SetNextItemWidth(leftWidth - 55);
            if (ImGui.BeginCombo("##Stat", $"       {PendingStatToAdd.Name}"))
            {
                foreach (var item in ModStatInfos.AsValueEnumerable().Where(x => !SolverConfig.StatPrioritys.Any(y => y.Id == x.Key)))
                {
                    //ImGui.BeginGroup();
                    ImGui.Image(item.Value.IconRef.Value, new Vector2(22, 22));
                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding();
                    if (ImGui.Selectable($"{item.Value.Name}##StatToSelect", item.Key == PendingStatToAdd.StatId))
                    {
                        PendingStatToAdd = item.Value;
                    }
                    //ImGui.EndGroup();
                }
                ImGui.EndCombo();
            }

            ImGui.SetCursorPos(pos + new Vector2(2, 2));
            ImGui.Image(PendingStatToAdd.IconRef.Value, new Vector2(22, 22));

            ImGui.SetCursorPos(pos + new Vector2(leftWidth - 50, 0));
            var isAlreadyAdded = SolverConfig.StatPrioritys.Any(x => x.Id == PendingStatToAdd.StatId);
            ImGui.BeginDisabled(isAlreadyAdded && SolverConfig.StatPrioritys.Count <= 8);
            if (ImGui.Button("Add", new Vector2(50, 0)))
            {
                SolverConfig.StatPrioritys.Add(new StatPrio()
                {
                    Id = PendingStatToAdd.StatId,
                    MinLevel = 0
                });

                configChanged = true;
            }

            if (isAlreadyAdded)
            {
                ImGui.SetItemTooltip("This stat is already added, please select another one.");
            }
            ImGui.EndDisabled();

            if (configChanged)
            {
                CurrentPresetString = SolverConfig.SaveToString();
            }

            ImGui.SetCursorPosX(leftWidth + 8);
            ImGui.SetCursorPosY(58);

            ImGui.SeparatorText($"Results");
            ImGui.SetCursorPosX(leftWidth + 8);
            ImGui.SetCursorPosY(85);

            ImGui.BeginChild("RightSection", new Vector2(windowSize.X - leftWidth - 5, contentRegion.Y - 55), ImGuiChildFlags.Borders);
            ImGui.Spacing();

            if (BestModResults.Count > 0)
            {
                lock (PlayerModData)
                {
                    lock (BestModResults)
                    {
                        lock (FilteredModules)
                        {
                            bool[] resultsOpenStates = new bool[BestModResults.Count];
                            resultsOpenStates[0] = true;
                            for (int i = 0; i < BestModResults.Count; i++)
                            {
                                ModComboResult modsResult = BestModResults[i];
                                if (ImGui.CollapsingHeader($"Result: {i + 1} (Score: {modsResult.Score})", (resultsOpenStates[i] ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None)))
                                {
                                    var perLine = 3;
                                    var statPos = ImGui.GetCursorPos();
                                    for (int i1 = 0; i1 < modsResult.Stats.Length; i1++)
                                    {
                                        PowerCore stat = modsResult.Stats[i1];
                                        ImGui.SetCursorPos(statPos + new Vector2(i1 * 100, 0));
                                        DrawModuleStat(stat.Id, stat.Value);
                                    }

                                    var mods = modsResult.ModuleSet.Mods;
                                    for (int i1 = 0; i1 < mods.Length; i1++)
                                    {
                                        var modId = FilteredModules[mods[i1]];
                                        var modItem = PlayerModData.ModulesPackage.Items[modId];
                                        DrawModule(modId, modItem);
                                        if ((i1 % 2) == 0)
                                        {
                                            ImGui.SameLine();
                                            ImGui.Dummy(new Vector2(20, 0));
                                            ImGui.SameLine();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            ImGui.EndChild();
            ImGui.SetCursorPosX(leftWidth + 8);
            if (ImGui.Button("Calculate", new Vector2(contentRegion.X - leftWidth, 0)))
            {
                ModuleCalcTask = Task.Factory.StartNew(() =>
                {
                    ShouldBlockMainUI = true;
                    CalculateBestModules();
                    ShouldBlockMainUI = false;
                });
            }
        }

        private static (bool, bool) DrawStatFilter(int i)
        {
            var pos = ImGui.GetCursorPos();
            var statInfo = SolverConfig.StatPrioritys[i];
            bool shouldRemove = false;
            bool wasChanged = false;

            ImGui.SetCursorPos(pos + new Vector2(0, 2));
            ImGui.PushFont(HelperMethods.Fonts["FASIcons"], 13.0f);
            ImGui.BeginDisabled(i == 0);
            if (ImGui.Button($"{FASIcons.AngleUp}##{i}", new Vector2(32, 16)))
            {
                var toMoveTo = i - 1;
                var tempStat = SolverConfig.StatPrioritys[toMoveTo];
                SolverConfig.StatPrioritys[toMoveTo] = SolverConfig.StatPrioritys[i];
                SolverConfig.StatPrioritys[i] = tempStat;
                wasChanged = true;
            }
            ImGui.EndDisabled();

            ImGui.BeginDisabled(i == SolverConfig.StatPrioritys.Count() - 1);
            if (ImGui.Button($"{FASIcons.AngleDown}##{i}", new Vector2(32, 16)))
            {
                var toMoveTo = i + 1;
                var tempStat = SolverConfig.StatPrioritys[toMoveTo];
                SolverConfig.StatPrioritys[toMoveTo] = SolverConfig.StatPrioritys[i];
                SolverConfig.StatPrioritys[i] = tempStat;
                wasChanged = true;
            }
            ImGui.EndDisabled();
            ImGui.PopFont();

            var stat = ModStatInfos[SolverConfig.StatPrioritys[i].Id];
            ImGui.SetCursorPos(pos + new Vector2(40, 4));
            ImGui.Image(stat.IconRef.Value, new Vector2(32, 32));

            ImGui.SetCursorPos(pos + new Vector2(80, 9));
            ImGui.PushFont(HelperMethods.Fonts["Segoe-Bold"], ImGui.GetFontSize());
            ImGui.TextUnformatted(stat.Name);
            ImGui.PopFont();

            var availSize = ImGui.GetContentRegionAvail();
            ImGui.SetCursorPos(pos + new Vector2(availSize.X - 70, 5));
            ImGui.SetNextItemWidth(40);
            if (ImGui.InputInt($"##MinLevel{i}", ref SolverConfig.StatPrioritys[i].MinLevel, 0))
            {
                wasChanged = true;
            }
            ImGui.SetItemTooltip("The minimum link value needed for this stat to be considered.\nLeave 0 to use any level.");

            ImGui.SetCursorPos(pos + new Vector2(availSize.X - 25, 0));
            ImGui.PushFont(HelperMethods.Fonts["FASIcons"], 13.0f);
            ImGui.PushStyleColor(ImGuiCol.Button, Colors.DarkRed);
            if (ImGui.Button($"{FASIcons.TrashCan}##{i}", new Vector2(25, 40)))
            {
                shouldRemove = true;
            }
            ImGui.PopStyleColor();
            ImGui.PopFont();

            ImGui.SetCursorPos(pos + new Vector2(0, 40));
            ImGui.Separator();

            return (shouldRemove, wasChanged);
        }

        private static void DrawModuleInv()
        {
            var contentSize = ImGui.GetContentRegionAvail();
            ImGui.BeginChild("##ModuleInv", new Vector2(-1, contentSize.Y - 25));
            var numPerLine = Math.Floor(ImGui.GetContentRegionAvail().X / MOD_DISPLAY_SIZE.X);
            int i = 0;
            foreach (var item in PlayerModData.ModulesPackage?.Items ?? [])
            {
                DrawModule(item.Key, item.Value);
                if ((++i % numPerLine) != 0)
                {
                    ImGui.SameLine();
                }
            }
            ImGui.EndChild();

            ImGui.Separator();
            ImGui.TextUnformatted($"Total: {NumTotalModules} | Attack: {NumAttackModules} | Support: {NumSupportModules} | Guard: {NumGuardModules}");
        }

        static Vector2 MOD_ICON_SIZE = new Vector2(80, 80);
        static Vector2 MOD_DISPLAY_SIZE = new Vector2(410, 105);
        public static void DrawModule(long id, Item item, bool showId = false)
        {
            var modTypeData = HelperMethods.DataTables.Modules.Data[item.ConfigId];
            var modInfo = PlayerModData.Mod.ModInfos[id];
            var startPos = ImGui.GetCursorPos();
            ImGui.PushClipRect(ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + MOD_DISPLAY_SIZE, true);
            ImGui.BeginGroup();
            var qualityBg = GetItemQualityBg(item.Quality);
            var modIcon = GetModuleIcon(item.ConfigId);

            ImGui.PushFont(HelperMethods.Fonts["Segoe-Bold"], ImGui.GetFontSize());
            ImGui.PushStyleVarX(ImGuiStyleVar.SeparatorTextPadding, 80);
            ImGui.SeparatorText(modTypeData.Name);
            ImGui.PopStyleVar();
            ImGui.PopFont();

            var pos = ImGui.GetCursorPos();
            ImGui.Image(qualityBg, MOD_ICON_SIZE);
            ImGui.SetCursorPos(pos);
            ImGui.Image(modIcon, MOD_ICON_SIZE);

            if (showId)
            {
                ImGui.SetCursorPos(pos);
                ImGui.TextUnformatted($"ID: {id}");
                ImGui.SetCursorPos(pos + new Vector2(0, MOD_ICON_SIZE.Y));
            }

            pos = ImGui.GetCursorPos();
            ImGui.SetCursorPos(pos + new Vector2(100, -MOD_ICON_SIZE.Y));
            var numStats = item.ModNewAttr.ModParts.Count();
            for (int i = 0; i < numStats; i++)
            {
                var partId = item.ModNewAttr.ModParts[i];
                var level = modInfo.InitLinkNums[i];
                ImGui.SetCursorPos(pos + new Vector2(90 + (i * 102), -(MOD_ICON_SIZE.Y + 4)));
                DrawModuleStat(partId, level);
            }

            ImGui.EndGroup();
            ImGui.PopClipRect();

            ImGui.SetCursorPos(startPos);
            ImGui.Dummy(MOD_DISPLAY_SIZE);
        }

        private static void DrawModuleStat(int partId, int level)
        {
            Vector2 iconSize = new Vector2(32, 32);
            Vector2 size = new Vector2(100, 100);

            if (ModStatInfos.TryGetValue(partId, out var statInfo))
            {
                var pos = ImGui.GetCursorPos();
                var titleWidth = ImGui.CalcTextSize(statInfo.Name).X;
                var icon = statInfo.IconRef.Value;
                ImGui.SetCursorPos(pos + new Vector2((size.X / 2) - (titleWidth / 2), 0));
                ImGui.PushFont(HelperMethods.Fonts["Segoe-Bold"], 17);
                ImGui.TextUnformatted(statInfo.Name);
                ImGui.SetCursorPos(pos + new Vector2((size.X / 2) - (iconSize.X / 2), 25));
                ImGui.Image(icon, iconSize);
                ImGui.SetCursorPos(pos + new Vector2((size.X / 2) - 10, 60));
                ImGui.TextUnformatted($"+{level}");
                ImGui.PopFont();
            }
        }

        private static bool CheckAndDrawNoModulesBanner()
        {
            if (PlayerModData == null || PlayerModData.ModulesPackage?.Items == null || PlayerModData.Mod == null)
            {
                DrawBanner("Please change line or teleport to load module inventory data.", 0xFFAD5E15, "Looking.png");
                return true;
            }

            return false;
        }

        static float BannerImgPulseTimer = 0f;
        private static void DrawBanner(string msg, uint bgColor, string img = null, bool animate = false)
        {
            float bannerheight = 200;
            var pos = ImGui.GetCursorScreenPos();
            var size = ImGui.GetContentRegionAvail();
            var start = pos + new Vector2(0, (size.Y / 2) - (bannerheight / 2));
            var drawList = ImGui.GetForegroundDrawList();

            drawList.AddRectFilled(
                start,
                start + new Vector2(size.X, bannerheight),
                bgColor
            );

            ImGui.PushFont(HelperMethods.Fonts["Segoe-Bold"], 30.0f);
            var txtSize = ImGui.CalcTextSize(msg);
            var txtStartPos = start + new Vector2(size.X / 2 - txtSize.X / 2, (bannerheight / 2) - (txtSize.Y / 2));
            drawList.AddText(txtStartPos, ImGui.ColorConvertFloat4ToU32(Colors.White), msg);
            ImGui.PopFont();

            var imgRef = ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, img));
            if (imgRef.HasValue)
            {
                var offset = new Vector2(5, 5);
                if (animate)
                {
                    BannerImgPulseTimer += ImGui.GetIO().DeltaTime;
                    Vector2 pulseAmount = new Vector2(5, 5);
                    float pulseSpeed = 1;
                    offset = new Vector2(
                        pulseAmount.X * MathF.Sin(BannerImgPulseTimer * MathF.PI * pulseSpeed),
                        pulseAmount.Y * MathF.Sin(BannerImgPulseTimer * MathF.PI * pulseSpeed)
                    );
                }

                var imgSize = new Vector2(bannerheight, bannerheight) + offset;
                var imgStart = txtStartPos - new Vector2(imgSize.X + 40, (imgSize.Y / 2) - txtSize.Y / 2);
                drawList.AddImage(imgRef.Value, imgStart, imgStart + imgSize);
            }
        }

        private static void LoadSavedModData(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Log.Information("No modules data saved at {Path}", path);
                    return;
                }

                var txt = File.ReadAllText(path);
                var modData = JsonConvert.DeserializeObject<PlayerModDataSave>(txt);
                if (modData != null)
                {
                    PlayerModData = modData;
                    ModuleInvUpdated();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading Mod data from {Path}", path);
            }
        }

        private static void SaveModData(string path)
        {
            try
            {
                var jsonTxt = JsonConvert.SerializeObject(PlayerModData, Formatting.Indented);
                File.WriteAllText(path, jsonTxt);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving Mod Data to {Path}", path);
            }
        }

        private static ImTextureRef GetModuleIcon(int id)
        {
            var icon = id switch
            {
                5500101 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_attack2.png")),
                5500102 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_attack3.png")),
                5500103 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_attack4.png")),
                5500104 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_attack4.png")),

                5500201 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_2.png")),
                5500202 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_3.png")),
                5500203 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_4.png")),
                5500204 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_4.png")),

                5500301 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_protect2.png")),
                5500302 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_protect3.png")),
                5500303 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_protect4.png")),
                5500304 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_protect4.png")),

                _ => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "missing.png"))
            };

            return icon.Value;
        }

        private static ImTextureRef GetItemQualityBg(int quality)
        {
            var icon = ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, $"item_quality_{quality}.png")) ?? ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "missing.png"));

            return icon.Value;
        }

        private static bool IsModuleOfType(int id, ModuleType modType) => ModTypeMapping.TryGetValue(id, out var info) ? info == modType : false;

        private static void CalculateBestModules()
        {
            var modWindowSettings = Settings.Instance.WindowSettings.ModuleWindow;
            var solver = new ModuleOptimizer();
            var results = solver.Solve(SolverConfig, PlayerModData, Settings.Instance.WindowSettings.ModuleWindow.SolverMode);

            FilteredModules = results.FilteredModules;
            BestModResults = results.BestModResults;
        }

        public static long CountCombinations4(int n)
        {
            if (n < 4) return 0;
            return (long)n * (n - 1) * (n - 2) * (n - 3) / 24;
        }
    }

    public enum ModuleType
    {
        Attack = 5500100,
        Support = 5500200,
        Guard = 5500300
    }

    public class ModStatInfo
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public int StatId   { get; set; }
        public ImTextureRef? IconRef { get; set; }
    }

    public class ModTypeInfo
    {
        public string Name { get; set; }
        public string Icon {  set; get; }
    }

    public struct ModuleSet
    {
        public ushort Mod1;
        public ushort Mod2;
        public ushort Mod3;
        public ushort Mod4;

        public ushort[] Mods => [Mod1, Mod2, Mod3, Mod4];
    }

    public struct ModComboResult
    {
        public ModuleSet ModuleSet;
        public int Score;
        public PowerCore[] Stats;
    }

    public struct PowerCore
    {
        public int Id;
        public int Value;
    }

    public class StatPrio
    {
        public int Id;
        public int MinLevel;
    }

    public class Preset
    {
        public string Name;
        public SolverConfig Config;
    }

    public class ModuleWindowSettings
    {
        public List<Preset> Presets = [];
        public SolverModes SolverMode = SolverModes.Normal;
    }

    public enum SolverModes
    {
        Legacy,
        Fallback,
        Normal
    }

    public class SolverResult
    {
        public List<ModComboResult> BestModResults = [];
        public List<long> FilteredModules = [];
    }
}
