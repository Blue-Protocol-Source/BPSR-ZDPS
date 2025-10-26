using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static log4net.Appender.ColoredConsoleAppender;

namespace BPSR_ZDPS
{
    public class ImFileBrowser
    {
        public const string POPUP_LAYER_ID = "ImFileBrowserLayer";
        public const string POPUP_ID = "###ImFileBrowserWindow";
        const string OVERWRITE_EXISTING_FILE_POPUP_ID = "###ImFileBrowserOverwriteExistingFileDialog";
        public static bool IsOpen = false;

        public static string CurrentDir = Environment.CurrentDirectory;
        public static string LastDir = Environment.CurrentDirectory;
        public static Stack<string> DirHistory = new Stack<string>();
        public static Mode DialogMode = Mode.OpenFile;
        public static bool ShowOverwriteFilePopup = false;
        public static bool PromptOverwriteFilePopup = false;
        public static string Title = "";

        public static List<EntryInfo> CurrentEntries = new();
        public static string SaveFilePath = "";
        public static List<DriveEntryInfo> SideBarEntries = new();

        private static Action<string> OpenFileCb = null;
        private static Action<string[]> OpenFilesCb = null;
        private static Action<string> SelectDirCb = null;
        private static Action<string> SaveFileCb = null;
        private static Action CancelCb = null;
        // https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/Microsoft/Win32/FileDialog.cs
        // https://github.com/microsoft/Windows-classic-samples/blob/main/Samples/Win7Samples/winui/shell/appplatform/commonfiledialog/CommonFileDialogApp.cpp
        // https://github.com/mono/mono/blob/main/mcs/class/System.Windows.Forms/System.Windows.Forms/FileDialog.cs#L3716
        private static string Filter = "";
        private static int FilterIndex;
        private static string DefaultExt = "";
        private static ImGuiTableSortSpecsPtr TableSortSpecs = null;
        private static bool NeedsToUpdateSorts = true;
        private static bool IsTrackingOpenState = false;

        // TODO: Setup Cancel operation callback

        public static void OpenFile(Action<string> openFileCb, string title = "", string baseDir = null, string filter = null, int filterIndex = 0, string defaultExt = null)
        {
            DialogMode = Mode.OpenFile;
            OpenFileCb = openFileCb;
            Open(title, baseDir, filter, filterIndex, defaultExt);
        }

        public static void OpenFiles(Action<string[]> openFilesCb, string title = "", string baseDir = null, string filter = null, int filterIndex = 0, string defaultExt = null)
        {
            DialogMode = Mode.OpenMultiple;
            OpenFilesCb = openFilesCb;
            Open(title, baseDir, filter, filterIndex, defaultExt);
        }

        public static void SaveFile(Action<string> saveFileCb, string title = "", string baseDir = null, string filter = null, int filterIndex = 0, string defaultExt = null, string defaultSaveFileName = "")
        {
            if (!string.IsNullOrWhiteSpace(defaultSaveFileName))
            {
                SaveFilePath = Path.Combine(CurrentDir, Path.GetFileName(defaultSaveFileName));
            }
            else
            {
                SaveFilePath = "";
            }
            DialogMode = Mode.SaveFile;
            SaveFileCb = saveFileCb;
            Open(title, baseDir, filter, filterIndex, defaultExt);
        }

        public static void SelectDir(Action<string> selectDirCb, Action cancelCb = null, string title = "", string baseDir = null)
        {
            DialogMode = Mode.SelectDir;
            SelectDirCb = selectDirCb;
            CancelCb = cancelCb;
            Open(title, baseDir);
        }

        private static void Open(string title = "", string baseDir = null, string filter = null, int filterIndex = 0, string defaultExt = null)
        {
            Title = title;
            Filter = filter;
            FilterIndex = filterIndex;

            if (defaultExt != null)
            {
                if (defaultExt.StartsWith(".", StringComparison.Ordinal))
                {
                    defaultExt = defaultExt.Substring(1);
                }
            }
            DefaultExt = defaultExt ?? "";
            NeedsToUpdateSorts = true;

            if (!Directory.Exists(baseDir))
            {
                baseDir = null;
            }

            if (baseDir == null && !Directory.Exists(LastDir))
            {
                LastDir = Environment.CurrentDirectory;
            }

            DirHistory.Clear();
            ChangeDir(baseDir ?? LastDir);
            RefreshSideBar();
            IsOpen = true;

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(POPUP_LAYER_ID));
            ImGui.OpenPopup(POPUP_ID);
            ImGui.PopID();
        }

        public static void Draw()
        {
            if (!IsOpen)
            {
                if (IsTrackingOpenState)
                {
                    IsTrackingOpenState = false;

                    if (CancelCb != null)
                    {
                        CancelCb();
                    }
                }

                return;
            }

            ImGui.SetNextWindowSize(new Vector2(820, 450), ImGuiCond.Appearing);
            ImGui.SetNextWindowSizeConstraints(new Vector2(650, 350), new Vector2(float.PositiveInfinity, float.PositiveInfinity));
            ImGui.SetNextWindowPos(ImGui.GetMainViewport().Size * 0.5f, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(POPUP_LAYER_ID));
            if (ImGui.BeginPopupModal($"{(string.IsNullOrEmpty(Title) ? "File Browser" : Title)}{POPUP_ID}", ref IsOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking))
            {
                IsTrackingOpenState = true;

                ImGui.AlignTextToFramePadding();

                ImGui.BeginDisabled(DirHistory.Count <= 0);
                ImGui.PushFont(HelperMethods.Fonts["FASIcons"], 13.0f);
                // chevron-left Unicode: F053
                if (ImGui.Button($"{FASIcons.ChevronLeft}", new Vector2(26, 26)))
                {
                    GoBack();
                }
                ImGui.SameLine();
                ImGui.EndDisabled();

                // chevron-up Unicode: F077
                if (ImGui.Button($"{FASIcons.ChevronUp}", new Vector2(26, 26)))
                {
                    GoUp();
                }
                ImGui.SameLine();
                ImGui.PopFont();

                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputText("###CurrentDir", ref CurrentDir, 400, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    ChangeDir(CurrentDir);
                }

                float ExtraLines = 1.1f;
                if (GetFilterItems().Count > 0)
                {
                    ExtraLines = 2.0f;
                }
                float TopPanelHeight = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() * ExtraLines - ImGui.GetStyle().ItemSpacing.Y * ExtraLines;
                DrawSidePanel(TopPanelHeight);
                ImGui.SameLine();

                // Draw the entries
                if (ImGui.BeginTable("Dir Entries", 3,
                    ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Sortable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchSame,
                    new Vector2(0, TopPanelHeight)))
                {
                    // freeze header row
                    ImGui.TableSetupScrollFreeze(0, 1);

                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.DefaultSort, 4f, 0);
                    ImGui.TableSetupColumn("Date Modified", ImGuiTableColumnFlags.DefaultSort, 1.4f);
                    ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.DefaultSort, 0.8f);
                    ImGui.TableHeadersRow();

                    if (NeedsToUpdateSorts)
                    {
                        TableSortSpecs = ImGui.TableGetSortSpecs();
                        NeedsToUpdateSorts = false;
                    }
                    if (TableSortSpecs.SpecsDirty)
                    {
                        SortEntrys(TableSortSpecs);
                    }

                    for (int entryIdx = 0; entryIdx < CurrentEntries.Count; entryIdx++)
                    {
                        var entry = CurrentEntries[entryIdx];
                        ImGui.TableNextColumn();
                        ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0 / 255f, 85 / 255f, 180 / 255f, 1.0f)); // TODO: Remove hardcoding colors and make dynamic
                        if (ImGui.Selectable($"##SelectEntry_{entry.Name}_{entryIdx}", entry.IsSelected,
                            ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick | ImGuiSelectableFlags.NoAutoClosePopups))
                        {
                            if (HandleSelection(entry))
                            {
                                // Reset color early so we can safely break out of the loop for the handler to properly apply
                                // There may be a single frame of bad drawing caused by this but it won't be visible to most users
                                ImGui.PopStyleColor();
                                return;
                            }
                        }
                        ImGui.PopStyleColor();

                        ImGui.SameLine();

                        ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                        if (entry.EntryType == EntryInfo.EntryTypes.Dir)
                        {
                            // folder-open Unicode: F07C, F115, 01F4C2, 01F5C1
                            // folder Unicode: F07B, F114, 01F4C1, 01F5BF
                            ImGui.PushStyleColor(ImGuiCol.Text, Colors.Wheat);
                            ImGui.Text($"{FASIcons.Folder}");
                            ImGui.PopStyleColor();
                            //ImGui.SameLine(28f);
                        }
                        else
                        {
                            if (entry.Extension == ".zip")
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, Colors.Orchid);
                                ImGui.Text($"{FASIcons.FileZipper}");
                                ImGui.PopStyleColor();
                            }
                            else if (entry.Extension == ".json")
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, Colors.LightGreen);
                                ImGui.Text($"{FASIcons.FileCode}");
                                ImGui.PopStyleColor();
                            }
                            else
                            {
                                // file Unicode: F016, F15B, 01F4C4, 01F5CB
                                ImGui.Text($"{FASIcons.File}");
                            }
                        }
                        ImGui.PopFont();

                        ImGui.SameLine();

                        ImGui.Text(entry.Name);
                        ImGui.TableNextColumn();
                        ImGui.Text(entry.DateModified);
                        ImGui.TableNextColumn();
                        ImGui.Text(entry.SizeStr);
                    }
                    ImGui.EndTable();
                }
                DrawBottomBar();

                if (PromptOverwriteFilePopup)
                {
                    ImGui.OpenPopup(OVERWRITE_EXISTING_FILE_POPUP_ID);
                    PromptOverwriteFilePopup = false;
                }

                // Confirm Override popup
                if (ImGui.BeginPopupModal($"Overwrite Existing File?{OVERWRITE_EXISTING_FILE_POPUP_ID}", ref ShowOverwriteFilePopup))
                {
                    ImGui.Text("The selected file already exists, are you sure you want to overwrite?");
                    if (ImGui.Button("Overwrite"))
                    {
                        LastDir = CurrentDir;
                        SaveFileCb(SaveFilePath);
                        IsOpen = false;
                        ShowOverwriteFilePopup = false;
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Cancel"))
                    {
                        ShowOverwriteFilePopup = false;
                    }

                    ImGui.EndPopup();
                }

                ImGui.EndPopup();
            }
            ImGui.PopID();
        }

        private static bool HandleSelection(EntryInfo entry)
        {
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) || ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            {
                if (entry.EntryType == EntryInfo.EntryTypes.Dir)
                {
                    ChangeDir(entry.Path);
                    return true;
                }
                else if (entry.EntryType == EntryInfo.EntryTypes.File)
                {
                    if (DialogMode == Mode.OpenFile)
                    {
                        LastDir = CurrentDir;
                        OpenFileCb(entry.Path);
                        IsOpen = false;
                    }

                    if (DialogMode == Mode.OpenMultiple)
                    {
                        LastDir = CurrentDir;
                        OpenFilesCb([entry.Path]);
                        IsOpen = false;
                    }

                    if (DialogMode == Mode.SaveFile)
                    {
                        CheckAndHandleExistingFile(entry.Path);
                    }
                }
            }

            // When CTRL is held and we're in multi-file mode, add to the selected items
            // TODO: Support holding Shift to select from Previous entry to this new one
            if (DialogMode == Mode.OpenMultiple && ImGui.GetIO().KeyCtrl)
            {
                // Throw away action if the user Ctrl+Clicked a Directory in File Mode
                if (entry.EntryType == EntryInfo.EntryTypes.Dir)
                {
                    return false;
                }
                entry.IsSelected = !entry.IsSelected;
                return false;
            }
            else
            {
                ClearSelected();
            }

            if (entry.EntryType == EntryInfo.EntryTypes.Dir && DialogMode == Mode.SelectDir ||
                entry.EntryType == EntryInfo.EntryTypes.File && DialogMode != Mode.SelectDir)
            {
                entry.IsSelected = true;
            }
            if (entry.EntryType == EntryInfo.EntryTypes.File && DialogMode == Mode.SaveFile)
            {
                SaveFilePath = entry.Path;
            }

            return false;
        }

        private static void CheckAndHandleExistingFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                PromptOverwriteFilePopup = true;
                ShowOverwriteFilePopup = true;
            }
            else
            {
                LastDir = CurrentDir;
                SaveFileCb(filePath);
                ShowOverwriteFilePopup = false;
                IsOpen = false;
            }
        }

        private static void DrawBottomBar()
        {
            ImGui.BeginGroup();
            ImGui.Dummy(new Vector2(1, 1)); // Small bit of extra padding
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"{(DialogMode == Mode.SelectDir ? "Folder" : "File")} Name: ");
            ImGui.SameLine();
            string FilePathList = "";
            if (DialogMode == Mode.OpenFile || DialogMode == Mode.SelectDir)
            {
                var selected = CurrentEntries.FirstOrDefault(item => item.IsSelected);
                if (selected != default)
                {
                    FilePathList = selected.Name;
                }
            }
            else if (DialogMode == Mode.SaveFile)
            {
                FilePathList = Path.GetFileName(SaveFilePath);
            }
            else if (DialogMode == Mode.OpenMultiple)
            {
                var SelectedEntries = CurrentEntries.Where(item => item.IsSelected);
                if (SelectedEntries.Count() > 1)
                {
                    FilePathList = $"{SelectedEntries.Count()} Files Selected";
                }
                else if (SelectedEntries.Count() == 1)
                {
                    FilePathList = SelectedEntries.First().Name;
                }
            }
            float FilterComboWidth = 175f;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - FilterComboWidth);
            if (ImGui.InputText("##SelectedFilePathNames", ref FilePathList, 255, DialogMode == Mode.SaveFile ? ImGuiInputTextFlags.None : ImGuiInputTextFlags.ReadOnly))
            {
                if (DialogMode == Mode.SaveFile)
                {
                    SaveFilePath = Path.Combine(CurrentDir, Path.GetFileName(FilePathList));
                    Console.WriteLine($"Built New Path: {SaveFilePath}");
                }
            }

            ImGui.SameLine();

            var FilterItems = GetFilterItems();
            if (FilterItems.Count > 0)
            {
                float CursorStartPos = ImGui.GetCursorPosX();
                ImGui.SetNextItemWidth(FilterComboWidth - ImGui.GetStyle().ItemSpacing.X);
                if (ImGui.Combo("##FilterCombo", ref FilterIndex, FilterItems.ToArray(), FilterItems.Count))
                {
                    ChangeDir(CurrentDir);
                }
                ImGui.SetCursorPosX(CursorStartPos);
            }

            float ButtonWidth = (FilterComboWidth - ImGui.GetStyle().ItemSpacing.X * 2) / 2;
            if (ImGui.Button("OK##OKBtn", new Vector2(ButtonWidth, 0)))
            {
                if (DialogMode == Mode.OpenFile || DialogMode == Mode.OpenMultiple)
                {
                    if (DialogMode == Mode.OpenFile)
                    {
                        var selected = CurrentEntries.FirstOrDefault(x => x.IsSelected);
                        if (selected != default)
                        {
                            LastDir = CurrentDir;
                            OpenFileCb(selected.Path);
                            IsOpen = false;
                        }
                    }
                    else
                    {
                        var paths = CurrentEntries.Where(x => x.IsSelected).Select(x => x.Path).ToArray();
                        if (paths.Length > 0)
                        {
                            LastDir = CurrentDir;
                            OpenFilesCb(paths);
                            IsOpen = false;
                        }
                    }
                }
                else if (DialogMode == Mode.SaveFile)
                {
                    SaveFilePath = Path.Combine(CurrentDir, Path.GetFileName(FilePathList));

                    // Ensure the file ends with the selected extension filter if possible
                    var AllowedExts = GetFilterExtensions();
                    if (AllowedExts.Count > 0)
                    {
                        if (!AllowedExts.First().Contains("*"))
                        {
                            var RequiredExt = $".{AllowedExts.First()}";
                            if (!AllowedExts.Contains(Path.GetExtension(SaveFilePath).TrimStart('.')))
                            {
                                SaveFilePath = Path.ChangeExtension(SaveFilePath, RequiredExt);
                            }
                        }
                    }

                    CheckAndHandleExistingFile(SaveFilePath);
                }
                else if (DialogMode == Mode.SelectDir)
                {
                    var selected = CurrentEntries.FirstOrDefault(x => x.IsSelected);
                    if (selected != default)
                    {
                        LastDir = CurrentDir;
                        SelectDirCb(selected.Path);
                        IsOpen = false;
                    }
                    else
                    {
                        // Use current directory as the "selected" one
                        LastDir = CurrentDir;
                        SelectDirCb(CurrentDir);
                        IsOpen = false;
                    }
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel##CancelBtn", new Vector2(ButtonWidth, 0)))
            {
                IsOpen = false;
            }

            ImGui.EndGroup();
        }

        private static void DrawSidePanel(float Height)
        {
            if (ImGui.BeginChild("##FileBrowserSidePanel", new Vector2(130, Height), ImGuiChildFlags.Borders))
            {
                if (ImGui.BeginTable("##SidePanelItems", 1, ImGuiTableFlags.ScrollY))
                {
                    foreach (var entry in SideBarEntries)
                    {
                        ImGui.TableNextColumn();

                        bool is_selected = false;
                        if (ImGui.Selectable($"##SideBarItem_{entry.Name}", ref is_selected, ImGuiSelectableFlags.SpanAllColumns))
                        {
                            ChangeDir(entry.Path);
                        }

                        ImGui.SameLine();

                        ImGui.PushFont(HelperMethods.Fonts["FASIcons"], ImGui.GetFontSize());
                        ImGui.Text(entry.IconPrefixCode);
                        ImGui.PopFont();
                        ImGui.SameLine(32f);
                        ImGui.Text(entry.Name);
                    }
                    ImGui.EndTable();
                }
                ImGui.EndChild();
            }
        }

        private static void SortEntrys(ImGuiTableSortSpecsPtr sorts)
        {
            var OrderedDirectories = CurrentEntries.Where(item => item.EntryType == EntryInfo.EntryTypes.Dir).OrderBy(x =>
            {
                return sorts.Specs.ColumnIndex switch
                {
                    0 => x.Name,
                    1 => x.DateModified,
                    2 => x.Size as object,
                    _ => x.Name
                };
            }).ToList();

            var OrderedFiles = CurrentEntries.Where(item => item.EntryType == EntryInfo.EntryTypes.File).OrderBy(x =>
            {
                return sorts.Specs.ColumnIndex switch
                {
                    0 => x.Name,
                    1 => x.DateModified,
                    2 => x.Size as object,
                    _ => x.Name
                };
            }).ToList();

            CurrentEntries = OrderedDirectories.Concat(OrderedFiles).ToList();

            if (sorts.Specs.SortDirection == ImGuiSortDirection.Descending)
            {
                CurrentEntries.Reverse();
            }

            sorts.SpecsDirty = false;
        }

        public static void GoBack()
        {
            if (DirHistory.TryPop(out string dir))
            {
                ChangeDir(dir, true);
            }
        }

        public static void GoUp()
        {
            try
            {
                var parentDir = new DirectoryInfo(CurrentDir);
                if (parentDir.Parent != null)
                {
                    ChangeDir(parentDir.Parent.FullName);
                }
                else
                {
                    CurrentEntries.Clear();
                    var drives = Directory.GetLogicalDrives();
                    foreach (var drive in drives)
                    {
                        var dirEntry = new EntryInfo
                        {
                            EntryType = EntryInfo.EntryTypes.Dir,
                            Name = drive,
                            Path = drive,
                        };
                        CurrentEntries.Add(dirEntry);
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        public static void RefreshSideBar()
        {
            SideBarEntries.Clear();
            foreach (var Drive in DriveInfo.GetDrives().Where(info => info != null))
            {
                if (Drive != null)
                {
                    var NewEntry = new DriveEntryInfo()
                    {
                        Name = Drive.RootDirectory.FullName,
                        Path = Drive.RootDirectory.FullName,
                        IconPrefixCode = $"{FASIcons.HardDrive}" // hard-drive Unicode: F0A0, 01F5B4
                    };
                    SideBarEntries.Add(NewEntry);
                }
            }

            var DesktopEntry = new DriveEntryInfo()
            {
                Name = "Desktop",
                Path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                IconPrefixCode = $"{FASIcons.Desktop}" // desktop Unicode: F108, F390, 01F5A5
            };
            SideBarEntries.Add(DesktopEntry);

            var DocumentsEntry = new DriveEntryInfo()
            {
                Name = "Documents",
                Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                IconPrefixCode = $"{FASIcons.FileLines}" // file-lines Unicode: F0F6, F15C, 01F5B9, 01F5CE
            };
            SideBarEntries.Add(DocumentsEntry);
        }

        public static void ChangeDir(string dir, bool noHistory = false)
        {
            try
            {
                var dirs = Directory.GetDirectories(dir);
                const string DateTimeFormat = "yyyy/MM/dd hh:mm tt";

                CurrentEntries.Clear();
                foreach (var dirStr in dirs)
                {
                    var dirInfo = new DirectoryInfo(dirStr);
                    var dirEntry = new EntryInfo
                    {
                        EntryType = EntryInfo.EntryTypes.Dir,
                        Name = dirInfo.Name,
                        Path = dirInfo.FullName,
                        DateCreated = $"{dirInfo.CreationTime.ToString(DateTimeFormat)}",
                        DateModified = $"{dirInfo.LastWriteTime.ToString(DateTimeFormat)}"
                    };
                    CurrentEntries.Add(dirEntry);
                }

                if (DialogMode != Mode.SelectDir)
                {
                    var filesList = new List<string>();

                    // Allow multiple patterns
                    if (Filter != null)
                    {
                        foreach (var pattern in GetFilterExtensions(true))
                        {
                            var foundFiles = Directory.GetFiles(dir, pattern);
                            filesList.AddRange(foundFiles);
                        }
                    }
                    else
                    {
                        var files = Directory.GetFiles(dir, "*");
                        filesList.AddRange(files);
                    }

                    foreach (var fileStr in filesList)
                    {
                        var fileInfo = new FileInfo(fileStr);
                        var fileEntry = new EntryInfo
                        {
                            EntryType = EntryInfo.EntryTypes.File,
                            Name = fileInfo.Name,
                            Path = fileInfo.FullName,
                            DateCreated = $"{fileInfo.CreationTime.ToString(DateTimeFormat)}",
                            DateModified = $"{fileInfo.LastWriteTime.ToString(DateTimeFormat)}",
                            Extension = fileInfo.Extension,
                            Size = fileInfo.Length,
                            SizeStr = $"{FormatFileSize(fileInfo.Length)}"
                        };
                        CurrentEntries.Add(fileEntry);
                    }
                }

                if (!noHistory)
                {
                    DirHistory.Push(CurrentDir);
                }

                CurrentDir = dir;

                // Update entry sorting every time we load into a new directory
                if (!NeedsToUpdateSorts)
                {
                    SortEntrys(TableSortSpecs);
                }

                // Always reset the browser scroll to top of directory
                ImGui.SetScrollY(0);
            }
            catch (Exception e)
            {
                string lastDir = DirHistory.Last();
                ChangeDir(lastDir, true);
            }
        }

        static List<string> GetFilterItems(bool GetPatterns = false)
        {
            string filter = Filter;
            List<string> extensions = new List<string>();

            if (!string.IsNullOrEmpty(filter))
            {
                string[] tokens = filter.Split('|');
                if (tokens.Length % 2 == 0)
                {
                    for (int i = 1; i < tokens.Length; i += 2)
                    {
                        // For patterns: Add(tokens[i])
                        if (GetPatterns)
                        {
                            extensions.Add(tokens[i]);
                        }
                        else
                        {
                            extensions.Add(tokens[i - 1]);
                        }
                    }
                }
            }

            return extensions;
        }

        static List<string> GetFilterExtensions(bool GetPatterns = false)
        {
            string filter = Filter;
            int filterIndex = FilterIndex + 1;
            //              "Archives (*.rar;*.zip;*.gzip)|*.rar;*.zip;*.gzip|JSON Files (*.json)|*.json|All Files (*.*)|*.*";
            // tokens[]:                 0                           1                 2            3           4         5
            // Index is SelectedFilter + 1 because win32 supports a filter feature we don't and stores it at 0 (outside of this we act like it's 0 based though)

            List<string> extensions = new List<string>();

            string defaultExtension = DefaultExt;
            if (!string.IsNullOrEmpty(defaultExtension))
            {
                if (GetPatterns)
                {
                    // Add wildcard back to the extension to maintain pattern format
                    extensions.Add($"*{defaultExtension}");
                }
                else
                {
                    extensions.Add(defaultExtension);
                }
            }

            if (filter != null)
            {
                string[] tokens = filter.Split('|', StringSplitOptions.RemoveEmptyEntries);

                int indexOfExtension = filterIndex * 2 - 1;

                if (indexOfExtension >= tokens.Length)
                {
                    // throw new InvalidOperationException(SR.FileDialogInvalidFilterIndex);
                }

                if (filterIndex > 0)
                {
                    string[] exts = tokens[indexOfExtension].Split(';');

                    foreach (string ext in exts)
                    {
                        if (GetPatterns)
                        {
                            extensions.Add(ext);
                        }
                        else
                        {
                            int i = ext.LastIndexOf(".");

                            if (i >= 0)
                            {
                                extensions.Add(ext.Substring(i + 1, ext.Length - (i + 1)));
                            }
                        }
                    }
                }
            }

            return extensions;
        }

        public static void ClearSelected()
        {
            foreach (var entry in CurrentEntries)
            {
                entry.IsSelected = false;
            }
        }

        public static string FormatFileSize(long bytes)
        {
            var unit = 1024;
            if (bytes < unit)
            {
                return $"{bytes} B";
            }

            var exp = (int)(Math.Log(bytes) / Math.Log(unit));
            return $"{bytes / Math.Pow(unit, exp):F2} {"KMGTPE"[exp - 1]}B";
        }

        public class EntryInfo
        {
            public enum EntryTypes
            {
                File,
                Dir
            }

            public string Name;
            public string Path;
            public EntryTypes EntryType;
            public long Size;
            public string SizeStr = "";
            public string DateCreated = "";
            public string DateModified = "";
            public string Extension = "";
            public bool IsSelected;
        }

        public class DriveEntryInfo
        {
            public string Name;
            public string Path;
            public string IconPrefixCode = "";
        }

        public enum Mode : byte
        {
            OpenFile,
            OpenMultiple,
            SelectDir,
            SaveFile
        }
    }
}
