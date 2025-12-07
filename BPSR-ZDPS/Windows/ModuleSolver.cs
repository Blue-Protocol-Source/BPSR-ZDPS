using Hexa.NET.ImGui;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Numerics;
using ZLinq;
using Zproto;

namespace BPSR_ZDPS
{
    public class ModuleSolver
    {
        private static bool IsOpen = true;
        private static PlayerModDataSave PlayerModData = new PlayerModDataSave();
        private static FrozenDictionary<int, ModStatInfo> ModStatInfos;
        private static FrozenDictionary<int, ModuleType> ModTypeMapping;
        private static string ModuleImgBasePath;
        private static int NumTotalModules = 0;
        private static int NumAttackModules = 0;
        private static int NumSupportModules = 0;
        private static int NumGuardModules = 0;
        private static string ModSavePath => Path.Combine(Utils.DATA_DIR_NAME, "ModulesSaveData.json");

        private static List<ModStatInfo> CurrentStatPriority = [];
        private static ModStatInfo PendingStatToAdd = null;
        private static List<ModComboResult> BestModResults = [];
        private static bool ShouldBlockMainUI = false;
        private static bool IsCalculating = false;
        private static Task ModuleCalcTask;
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

            CurrentStatPriority = [
                effectStatTypes.FirstOrDefault(x => x.StatId == 2104),
                effectStatTypes.FirstOrDefault(x => x.StatId == 2404),
                effectStatTypes.FirstOrDefault(x => x.StatId == 1114),
                effectStatTypes.FirstOrDefault(x => x.StatId == 1112),
                effectStatTypes.FirstOrDefault(x => x.StatId == 1409),
                effectStatTypes.FirstOrDefault(x => x.StatId == 1113)
            ];

            LoadSavedModData(ModSavePath);
        }

        public static void Open()
        {
            RunOnceDelayed = 0;
            IsOpen = true;
        }

        public static void SetPlayerInv(CharSerialize data)
        {
            PlayerModData = new PlayerModDataSave()
            {
                ModulesPackage = data.ItemPackage?.Packages[5] ?? null,
                Mod = data?.Mod ?? null
            };

            SaveModData(ModSavePath);
            ModuleInvUpdated();
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
            if (ImGui.Begin("Module Solver", ref IsOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking))
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
                    if (ImGui.BeginTabItem("Solver"))
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

            ImGui.BeginChild("LeftSection", new Vector2(leftWidth, contentRegion.Y - 55), ImGuiChildFlags.Borders);
            ImGui.SeparatorText("Stat Priority");
            ImGui.Spacing();

            int idToRemove = -1;
            for (int i = 0; i < CurrentStatPriority.Count; i++)
            {
                if (DrawStatFilter(i))
                {
                    idToRemove = i;
                }
            }

            if (idToRemove != -1)
            {
                CurrentStatPriority.RemoveAt(idToRemove);
            }

            ImGui.EndChild();

            var pos = ImGui.GetCursorPos();
            ImGui.SetNextItemWidth(leftWidth - 55);
            if (ImGui.BeginCombo("##Stat", $"       {PendingStatToAdd.Name}"))
            {
                foreach (var item in ModStatInfos.AsValueEnumerable().Where(x => !CurrentStatPriority.Any(y => y.StatId == x.Key)))
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
            var isAlreadyAdded = CurrentStatPriority.Contains(PendingStatToAdd);
            ImGui.BeginDisabled(isAlreadyAdded);
            if (ImGui.Button("Add", new Vector2(50, 0)))
            {
                CurrentStatPriority.Add(PendingStatToAdd);
            }

            if (isAlreadyAdded)
            {
                ImGui.SetItemTooltip("This stat is already added, please select another one.");
            }
            ImGui.EndDisabled();

            ImGui.SetCursorPosX(leftWidth + 8);
            ImGui.SetCursorPosY(58);

            ImGui.SeparatorText($"Results");
            ImGui.SetCursorPosX(leftWidth + 8);
            ImGui.SetCursorPosY(85);

            ImGui.BeginChild("RightSection", new Vector2(windowSize.X - leftWidth - 5, contentRegion.Y - 55), ImGuiChildFlags.Borders);
            ImGui.Spacing();

            if (BestModResults.Count > 0)
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

        private static bool DrawStatFilter(int i)
        {
            var pos = ImGui.GetCursorPos();
            var statInfo = CurrentStatPriority[i];
            bool shouldRemove = false;

            ImGui.SetCursorPos(pos + new Vector2(0, 2));
            ImGui.PushFont(HelperMethods.Fonts["FASIcons"], 13.0f);
            ImGui.BeginDisabled(i == 0);
            if (ImGui.Button($"{FASIcons.AngleUp}##{i}", new Vector2(32, 16)))
            {
                var toMoveTo = i - 1;
                var tempStat = CurrentStatPriority[toMoveTo];
                CurrentStatPriority[toMoveTo] = CurrentStatPriority[i];
                CurrentStatPriority[i] = tempStat;
            }
            ImGui.EndDisabled();

            ImGui.BeginDisabled(i == CurrentStatPriority.Count() - 1);
            if (ImGui.Button($"{FASIcons.AngleDown}##{i}", new Vector2(32, 16)))
            {
                var toMoveTo = i + 1;
                var tempStat = CurrentStatPriority[toMoveTo];
                CurrentStatPriority[toMoveTo] = CurrentStatPriority[i];
                CurrentStatPriority[i] = tempStat;
            }
            ImGui.EndDisabled();
            ImGui.PopFont();

            ImGui.SetCursorPos(pos + new Vector2(40, 4));
            ImGui.Image(statInfo.IconRef.Value, new Vector2(32, 32));

            ImGui.SetCursorPos(pos + new Vector2(80, 9));
            ImGui.PushFont(HelperMethods.Fonts["Segoe-Bold"], ImGui.GetFontSize());
            ImGui.TextUnformatted(statInfo.Name);
            ImGui.PopFont();

            var availSize = ImGui.GetContentRegionAvail();
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

            return shouldRemove;
        }

        private static void DrawIconCombo()
        {
            Vector2 size = new Vector2(140, 28);

            ImGui.PushStyleColor(ImGuiCol.FrameBg, 0xFF202020);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6, 4));

            // This behaves like an input box / frame
            if (ImGui.BeginChild("StatSelector_preview", size))
            {
                // Make the preview clickable: open popup on click
                if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    ImGui.OpenPopup("StatSelector");
                }

                // Draw icon + text for selected item
                ImGui.Image(PendingStatToAdd.IconRef.Value, new Vector2(20, 20));
                ImGui.SameLine();
                ImGui.Text(PendingStatToAdd.Name);
            }
            ImGui.EndChild();

            ImGui.PopStyleColor();
            ImGui.PopStyleVar();

            // ----- DROPDOWN CONTENT -----
            if (ImGui.BeginPopup("StatSelector"))
            {
                for (int i = 0; i < ModStatInfos.Values.Length; i++)
                {
                    ImGui.PushID(i);

                    bool isSelected = false;
                    ImGui.Selectable("##sel", isSelected);
                        //selectedIndex = i;

                    // Draw the row manually
                    Vector2 pos = ImGui.GetItemRectMin();
                    ImGui.SetCursorScreenPos(pos + new Vector2(4, 2));
                    ImGui.Image(ModStatInfos.Values[i].IconRef.Value, new Vector2(20, 20));
                    ImGui.SameLine();
                    ImGui.Text(ModStatInfos.Values[i].Name);

                    ImGui.PopID();
                }

                ImGui.EndPopup();
            }
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
                ImGui.SetCursorPos(pos + new Vector2(100 + (i * 100), -(MOD_ICON_SIZE.Y + 4)));
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
                ImGui.PushFont(HelperMethods.Fonts["Segoe-Bold"], ImGui.GetFontSize());
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

                5500201 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_2.png")),
                5500202 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_3.png")),
                5500203 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_4.png")),

                5500301 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_protect2.png")),
                5500302 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_protect3.png")),
                5500303 => ImageHelper.LoadTexture(Path.Combine(ModuleImgBasePath, "item_mod_device_protect4.png")),

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
            lock (BestModResults)
            {
                BestModResults.Clear();
            }

            var filtered = FilterModulesWithStats();

            // Create short indices
            // Create base scores for modules
            // Score combos based on the stat priorities and breakpoints reached

            // Build module scores
            ushort[] baseModuleScores = new ushort[filtered.Count];
            for (int i = 0; i < filtered.Count; i++)
            {
                var score = CalcModuleScore(filtered[i]);
                baseModuleScores[i] = score;
            }

            var numCombos = CountCombinations4(filtered.Count());
            var sw = Stopwatch.StartNew();
            var numMods = filtered.Count;
            var bestMods = new List<ModComboResult>();
            Parallel.For(0, numMods - 3, i =>
            {
                ModComboResult[] topBest = new ModComboResult[10];

                for (int j = i + 1; j < numMods - 2; j++)
                {
                    for (int k = j + 1; k < numMods - 1; k++)
                    {
                        for (int l = k + 1; l < numMods; l++)
                        {
                            var combo = new ModuleSet()
                            {
                                Mod1 = (ushort)i,
                                Mod2 = (ushort)j,
                                Mod3 = (ushort)k,
                                Mod4 = (ushort)l
                            };

                            var mod1Cores = GetModPowerCores(filtered[i]);
                            var mod2Cores = GetModPowerCores(filtered[j]);
                            var mod3Cores = GetModPowerCores(filtered[k]);
                            var mod4Cores = GetModPowerCores(filtered[l]);

                            var allCores = new List<PowerCore>(10);
                            allCores.AddRange(mod1Cores);
                            allCores.AddRange(mod2Cores);
                            allCores.AddRange(mod3Cores);
                            allCores.AddRange(mod4Cores);

                            int totalScore = 0;

                            var grouped = allCores.AsValueEnumerable().GroupBy(x => x.Id);
                            foreach (var group in grouped)
                            {
                                var statMul = GetStatMultiplier(group.Key);
                                var total = group.AsValueEnumerable().Select(y => y.Value).Sum();
                                var breakPointBonus = total switch
                                {
                                    >= 20 => 64,
                                    >= 16 => 32,
                                    >= 12 => 16,
                                    >= 8 => 8,
                                    >= 4 => 4,
                                    >= 1 => 2,
                                    _ => 0
                                };

                                var score = Math.Clamp(total, 0, 20) + breakPointBonus * statMul;
                                totalScore += score;
                            }
                            
                            //.SelectMany(x => x.AsValueEnumerable().Select(y => y.Value).Sum());

                            //var score = baseModuleScores[i] + baseModuleScores[j] + baseModuleScores[k] + baseModuleScores[l];
                            for (int bestIdx = 0; bestIdx < topBest.Length; bestIdx++)
                            {
                                var best = topBest[bestIdx];
                                if (totalScore > best.Score)
                                {
                                    topBest[bestIdx].ModuleSet = combo;
                                    topBest[bestIdx].Score = totalScore;
                                }
                            }

                            /*var mod1 = PlayerModulesPackage.Items[filtered[i]];
                            var mod2 = PlayerModulesPackage.Items[filtered[j]];
                            var mod3 = PlayerModulesPackage.Items[filtered[k]];
                            var mod4 = PlayerModulesPackage.Items[filtered[l]];*/
                        }
                    }
                }

                lock (bestMods)
                {
                    bestMods.AddRange(topBest);
                }
            });
            sw.Stop();

            var top10 = bestMods.DistinctBy(x => $"{x.ModuleSet.Mod1}_{x.ModuleSet.Mod2}_{x.ModuleSet.Mod3}_{x.ModuleSet.Mod4}").OrderByDescending(x => x.Score).Take(10).ToList();
            for (int i1 = 0; i1 < top10.Count; i1++)
            {
                ModComboResult modSet = top10[i1];
                var coreStats = new Dictionary<long, PowerCore>();
                var mods = modSet.ModuleSet.Mods;
                for (int i = 0; i < mods.Length; i++)
                {
                    var modId = filtered[mods[i]];
                    var powerCores = GetModPowerCores(modId);
                    foreach (var powerCore in powerCores)
                    {
                        if (coreStats.TryGetValue(powerCore.Id, out var existingCore))
                        {
                            existingCore.Value += powerCore.Value;
                            coreStats[powerCore.Id] = existingCore;
                        }
                        else
                        {
                            coreStats.Add(powerCore.Id, powerCore);
                        }
                    }
                }

                modSet.Stats = coreStats.Values.OrderByDescending(x => x.Value).ToArray();

                top10[i1] = modSet;
            }

            lock (BestModResults)
            {
                BestModResults = top10;
            }

            lock (FilteredModules)
            {
                FilteredModules = filtered;
            }

            Log.Information($"Combos took: {sw.Elapsed}");

            Log.Information($"filtered: {filtered.Count()}, NumCombos: {numCombos}");
        }

        private static ushort CalcModuleScore(long id)
        {
            var modItem = PlayerModData.ModulesPackage.Items[id];
            var modInfo = PlayerModData.Mod.ModInfos[id];
            ushort score = 0;
            for (int i = 0; i < modItem.ModNewAttr.ModParts.Count; i++)
            {
                var statId = modItem.ModNewAttr.ModParts[i];
                var statValue = modInfo.InitLinkNums[i];
                var statMultiplier = GetStatMultiplier(statId);
                score += (ushort)(statValue * statMultiplier);
            }

            return score;
        }

        private static List<PowerCore> GetModPowerCores(long id)
        {
            var powerCores = new List<PowerCore>();

            var modItem = PlayerModData.ModulesPackage.Items[id];
            var modInfo = PlayerModData.Mod.ModInfos[id];
            for (int i = 0; i < modItem.ModNewAttr.ModParts.Count; i++)
            {
                var statId = modItem.ModNewAttr.ModParts[i];
                var statValue = modInfo.InitLinkNums[i];

                var powerCore = new PowerCore()
                {
                    Id = statId,
                    Value = statValue,
                };

                powerCores.Add(powerCore);
            }

            return powerCores;
        }

        // 44,224,635
        private static List<long> FilterModulesWithStats()
        {
            var modules = new List<long>();
            foreach (var item in PlayerModData.ModulesPackage.Items)
            {
                if (item.Value.ModNewAttr.ModParts.Any(x => CurrentStatPriority.Any(y => y.StatId == x)))
                {
                    modules.Add(item.Key);
                }
            }

            return modules.ToList();
        }

        public static long CountCombinations4(int n)
        {
            if (n < 4) return 0;
            return (long)n * (n - 1) * (n - 2) * (n - 3) / 24;
        }

        private static ushort GetStatMultiplier(int statId)
        {
            for (int i = 0; i < CurrentStatPriority.Count; i++)
            {
                if (CurrentStatPriority[i].StatId == statId)
                {
                    return (ushort)(CurrentStatPriority.Count - i);
                }
            }

            return 0;
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

    public class PlayerModDataSave
    {
        public Package ModulesPackage;
        public Mod Mod;
    }
}
