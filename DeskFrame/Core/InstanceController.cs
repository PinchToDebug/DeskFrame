using DeskFrame;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows.Interop;

public class InstanceController
{
    public static string appName = "DeskFrame";
    public bool isInitializingInstances = false;
    public List<Instance> Instances = new List<Instance>();
    public RegistryHelper reg = new RegistryHelper(appName);
    public List<DeskFrameWindow> _subWindows = new List<DeskFrameWindow>();
    public List<IntPtr> _subWindowsPtr = new List<IntPtr>();
    private bool Visible = true;
    public void WriteOverInstanceToKey(Instance instance, string oldKey)
    {

        try
        {
            Debug.WriteLine($"old: {oldKey}\t{instance.Name}");

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@$"SOFTWARE\{appName}\Instances\{instance.Name}"))
            {
                key.SetValue("Name", instance.Name!);
                key.SetValue("PosX", instance.PosX!);
                key.SetValue("PosY", instance.PosY!);
                key.SetValue("Width", instance.Width!);
                key.SetValue("Height", instance.Height!);
                key.SetValue("IconSize", instance.IconSize!);
                key.SetValue("IdleOpacity", instance.IdleOpacity!);
                key.SetValue("AnimationSpeed", instance.AnimationSpeed!);
                key.SetValue("MaxGrayScaleStrength", instance.MaxGrayScaleStrength!);
                key.SetValue("GrayScaleEnabled", instance.GrayScaleEnabled!);
                key.SetValue("GrayScaleEnabled_InactiveOnly", instance.GrayScaleEnabled_InactiveOnly!);
                key.SetValue("Minimized", instance.Minimized!);
                key.SetValue("Folder", instance.Folder!);
                key.SetValue("TitleFontFamily", instance.TitleFontFamily!);
                key.SetValue("ShowHiddenFiles", instance.ShowHiddenFiles!);
                key.SetValue("LastAccesedToFirstRow", instance.LastAccesedToFirstRow);
                key.SetValue("ShowFileExtension", instance.ShowFileExtension!);
                key.SetValue("ShowFileExtensionIcon", instance.ShowFileExtensionIcon!);
                key.SetValue("ShowHiddenFilesIcon", instance.ShowHiddenFilesIcon!);
                key.SetValue("ShowDisplayName", instance.ShowDisplayName!);
                key.SetValue("IsLocked", instance.IsLocked!);
                key.SetValue("ShowInGrid", instance.ShowInGrid!);
                key.SetValue("AutoExpandonCursor", instance.AutoExpandonCursor);
                key.SetValue("ShowShortcutArrow", instance.ShowShortcutArrow);
                key.SetValue("FolderOpenInsideFrame", instance.FolderOpenInsideFrame);
                key.SetValue("HideTitleBarIconsWhenInactive", instance.HideTitleBarIconsWhenInactive);
                key.SetValue("SnapWidthToIconWidth", instance.SnapWidthToIconWidth);
                key.SetValue("SnapWidthToIconWidth_PlusScrollbarWidth", instance.SnapWidthToIconWidth_PlusScrollbarWidth);
                key.SetValue("CheckFolderSize", instance.CheckFolderSize);
                key.SetValue("TitleBarColor", instance.TitleBarColor!);
                key.SetValue("TitleTextColor", instance.TitleTextColor!);
                key.SetValue("ActiveTitleTextColor", instance.ActiveTitleTextColor!);
                key.SetValue("TitleTextAlignment", instance.TitleTextAlignment.ToString());
                key.SetValue("TitleText", instance.TitleText != null ? instance.TitleText : instance.Name);
                key.SetValue("BorderColor", instance.BorderColor!);
                key.SetValue("BorderEnabled", instance.BorderEnabled!);
                key.SetValue("ActiveBorderEnabled", instance.ActiveBorderEnabled!);
                key.SetValue("ActiveBackgroundEnabled", instance.ActiveBackgroundEnabled!);
                key.SetValue("ActiveTitleTextEnabled", instance.ActiveTitleTextEnabled!);
                key.SetValue("FileFilterRegex", instance.FileFilterRegex!);
                key.SetValue("FileFilterHideRegex", instance.FileFilterHideRegex!);
                key.SetValue("ListViewBackgroundColor", instance.ListViewBackgroundColor!);
                key.SetValue("ActiveBackgroundColor", instance.ActiveBackgroundColor!);
                key.SetValue("ActiveBorderColor", instance.ActiveBorderColor!);
                key.SetValue("ListViewFontColor", instance.ListViewFontColor!);
                key.SetValue("ListViewFontShadowColor", instance.ListViewFontShadowColor!);
                key.SetValue("Opacity", instance.Opacity);
                key.SetValue("SortBy", instance.SortBy);
                key.SetValue("FolderOrder", instance.FolderOrder);
                if (instance.ShowOnVirtualDesktops != null && instance.ShowOnVirtualDesktops.Length > 0)
                {
                    key.SetValue("ShowOnVirtualDesktops", string.Join(",", instance.ShowOnVirtualDesktops));
                }
                if (instance.LastAccessedFiles != null && instance.LastAccessedFiles.Count > 0)
                {
                    key.SetValue("LastAccessedFiles", instance.LastAccessedFiles.ToArray(), RegistryValueKind.MultiString);
                }
                key.SetValue("TitleFontSize", instance.TitleFontSize);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WriteOverInstanceToKey failed: {ex.Message}");
        }
        Registry.CurrentUser.DeleteSubKey(@$"SOFTWARE\{appName}\Instances\{oldKey}", throwOnMissingSubKey: false);
    }
    private void InitDetails()
    {
        if (reg.KeyExistsRoot("blurBackground"))
        {
            ChangeBlur((bool)reg.ReadKeyValueRoot("blurBackground"));
        }
    }
    public void WriteInstanceToKey(Instance instance)
    {
        if (string.IsNullOrEmpty(instance.Name))
        {
            Debug.WriteLine("instance.Name is null, Instance is not written to key");
            return;
        }
        try
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(instance.GetKeyLocation()))
            {
                if (instance.Name != null) key.SetValue("Name", instance.Name);
                key.SetValue("PosX", instance.PosX);
                key.SetValue("PosY", instance.PosY);
                key.SetValue("Width", instance.Width);
                key.SetValue("Height", instance.Height);
                key.SetValue("IconSize", instance.IconSize);
                key.SetValue("IdleOpacity", instance.IdleOpacity);
                key.SetValue("AnimationSpeed", instance.AnimationSpeed);
                key.SetValue("MaxGrayScaleStrength", instance.MaxGrayScaleStrength!);
                key.SetValue("GrayScaleEnabled", instance.GrayScaleEnabled!);
                key.SetValue("GrayScaleEnabled_InactiveOnly", instance.GrayScaleEnabled_InactiveOnly!);
                key.SetValue("Minimized", instance.Minimized);
                if (instance.Folder != null) key.SetValue("Folder", instance.Folder);
                if (instance.TitleFontFamily != null) key.SetValue("TitleFontFamily", instance.TitleFontFamily);
                key.SetValue("ShowHiddenFiles", instance.ShowHiddenFiles);
                key.SetValue("LastAccesedToFirstRow", instance.LastAccesedToFirstRow);
                key.SetValue("ShowFileExtension", instance.ShowFileExtension);
                key.SetValue("ShowFileExtensionIcon", instance.ShowFileExtensionIcon);
                key.SetValue("ShowHiddenFilesIcon", instance.ShowHiddenFilesIcon);
                key.SetValue("ShowDisplayName", instance.ShowDisplayName);
                key.SetValue("IsLocked", instance.IsLocked);
                key.SetValue("ShowInGrid", instance.ShowInGrid);
                key.SetValue("AutoExpandonCursor", instance.AutoExpandonCursor);
                key.SetValue("ShowShortcutArrow", instance.ShowShortcutArrow);
                key.SetValue("FolderOpenInsideFrame", instance.FolderOpenInsideFrame);
                key.SetValue("HideTitleBarIconsWhenInactive", instance.HideTitleBarIconsWhenInactive);
                key.SetValue("SnapWidthToIconWidth", instance.SnapWidthToIconWidth);
                key.SetValue("SnapWidthToIconWidth_PlusScrollbarWidth", instance.SnapWidthToIconWidth_PlusScrollbarWidth);
                key.SetValue("CheckFolderSize", instance.CheckFolderSize);
                if (instance.TitleBarColor != null) key.SetValue("TitleBarColor", instance.TitleBarColor);
                if (instance.TitleTextColor != null) key.SetValue("TitleTextColor", instance.TitleTextColor);
                if (instance.ActiveTitleTextColor != null) key.SetValue("ActiveTitleTextColor", instance.ActiveTitleTextColor);
                key.SetValue("TitleTextAlignment", instance.TitleTextAlignment.ToString());
                if (instance.TitleText != null) key.SetValue("TitleText", instance.TitleText);
                if (instance.BorderColor != null) key.SetValue("BorderColor", instance.BorderColor);
                key.SetValue("BorderEnabled", instance.BorderEnabled);
                key.SetValue("ActiveBorderEnabled", instance.ActiveBorderEnabled!);
                key.SetValue("ActiveBackgroundEnabled", instance.ActiveBackgroundEnabled!);
                key.SetValue("ActiveTitleTextEnabled", instance.ActiveTitleTextEnabled!);
                if (instance.FileFilterRegex != null) key.SetValue("FileFilterRegex", instance.FileFilterRegex);
                if (instance.FileFilterHideRegex != null) key.SetValue("FileFilterHideRegex", instance.FileFilterHideRegex);
                if (instance.ListViewBackgroundColor != null) key.SetValue("ListViewBackgroundColor", instance.ListViewBackgroundColor);
                if (instance.ActiveBackgroundColor != null) key.SetValue("ActiveBackgroundColor", instance.ActiveBackgroundColor);
                if (instance.ActiveBorderColor != null) key.SetValue("ActiveBorderColor", instance.ActiveBorderColor);
                if (instance.ListViewFontColor != null) key.SetValue("ListViewFontColor", instance.ListViewFontColor);
                if (instance.ListViewFontShadowColor != null) key.SetValue("ListViewFontShadowColor", instance.ListViewFontShadowColor);
                key.SetValue("Opacity", instance.Opacity);
                key.SetValue("SortBy", instance.SortBy);
                key.SetValue("FolderOrder", instance.FolderOrder);
                if (instance.ShowOnVirtualDesktops != null) key.SetValue("ShowOnVirtualDesktops", string.Join(",", instance.ShowOnVirtualDesktops));
                if (instance.LastAccessedFiles != null && instance.LastAccessedFiles.Count > 0)
                {
                    key.SetValue("LastAccessedFiles", instance.LastAccessedFiles.ToArray(), RegistryValueKind.MultiString);
                }
                key.SetValue("TitleFontSize", instance.TitleFontSize);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WriteInstanceToKey failed: {ex.Message}");
        }
    }

    public void AddInstance()
    {
        var existingEmptyInstance = Instances.FirstOrDefault(instance => instance.Name == "empty");

        if (existingEmptyInstance != null)
        {
            Instances.Remove(existingEmptyInstance);
        }

        Instances.Add(new Instance("empty", false));
        MainWindow._controller.WriteInstanceToKey(Instances.Last());
        var subWindow = new DeskFrameWindow(Instances.Last());
        subWindow.ChangeBackgroundOpacity(Instances.Last().Opacity);
        _subWindows.Add(subWindow);
        subWindow.Show();
        _subWindowsPtr.Add(new WindowInteropHelper(subWindow).Handle);
        InitDetails();
    }
    public void RemoveInstance(Instance instance, DeskFrameWindow window)
    {
        Instances.Remove(instance);
        _subWindows.Remove(window);
    }
    public void ChangeBlur(bool toBlur)
    {
        foreach (DeskFrameWindow window in _subWindows)
        {
            window.BackgroundType(toBlur);
        }
    }

    public void ChangeBackgroundOpacity(int num)
    {
        foreach (DeskFrameWindow window in _subWindows)
        {
            window.ChangeBackgroundOpacity(num);
        }
    }
    public void CheckFrameWindowsLive()
    {
        int closedCount = 0;
        foreach (var window in _subWindows)
        {
            if (new WindowInteropHelper(window).Handle == IntPtr.Zero) closedCount++;
        }
        if (_subWindows.Count != 0 && !isInitializingInstances)
        {
            if (closedCount == _subWindows.Count)
            {
                foreach (var window in _subWindows)
                {
                    window.Close();
                }
                _subWindows.Clear();
                _subWindowsPtr.Clear();
                Instances.Clear();
                InitInstances();
            }
            if (closedCount != _subWindows.Count)
            {
                foreach (var window in _subWindows)
                {
                    window.AdjustPosition();
                }
            }
        }
    }
    public void ChangeVisibility()
    {
        if (Visible)
        {
            foreach (var window in _subWindows)
            {
                window.Hide();
            }
            Visible = false;
        }
        else
        {
            foreach (var window in _subWindows)
            {
                window.Show();
            }
            Visible = true;
        }
    }
    public void InitInstances()
    {
        isInitializingInstances = true;
        Debug.WriteLine("Init...");
        try
        {
            using (RegistryKey instancesKey = Registry.CurrentUser.OpenSubKey(@$"SOFTWARE\{appName}\Instances")!)
            {
                if (instancesKey != null)
                {
                    string[] instanceNames = instancesKey.GetSubKeyNames();
                    Debug.WriteLine($"\n");

                    foreach (var item in instanceNames)
                    {
                        Debug.WriteLine($"subkeyname: {item}");
                    }

                    foreach (string instance in instanceNames)
                    {

                        Debug.WriteLine($"instanceNames: {instance}");


                        using (RegistryKey instanceKey = Registry.CurrentUser.OpenSubKey(@$"SOFTWARE\{appName}\Instances\{instance}")!)
                        {
                            Debug.WriteLine("valid");
                            if (instanceKey != null)
                            {
                                Instance temp = new Instance("", false);
                                Debug.WriteLine("valied 2");

                                foreach (var valueName in instanceKey.GetValueNames())   // Read all values under the current subkey
                                {
                                    object value = instanceKey.GetValue(valueName)!;

                                    if (value != null)
                                    {
                                        switch (valueName)
                                        {
                                            case "PosX":
                                                if (double.TryParse(value.ToString(), out double parsedPosX))
                                                {
                                                    temp.PosX = parsedPosX;
                                                    Debug.WriteLine($"PosX added: {temp.PosX}");
                                                }
                                                else
                                                {
                                                    Debug.WriteLine("Failed to parse PosX.");
                                                }
                                                break;
                                            case "PosY":
                                                if (double.TryParse(value.ToString(), out double parsedPosY))
                                                {
                                                    temp.PosY = parsedPosY;
                                                    Debug.WriteLine($"PosY added: {temp.PosY}");
                                                }
                                                else
                                                {
                                                    Debug.WriteLine("Failed to parse PosY.");
                                                }
                                                break;

                                            case "Width":
                                                if (double.TryParse(value.ToString(), out double parsedWidth))
                                                {
                                                    temp.Width = parsedWidth;
                                                    Debug.WriteLine($"Width added: {temp.Width}");
                                                }
                                                else
                                                {
                                                    Debug.WriteLine("Failed to parse Width.");
                                                }
                                                break;

                                            case "Height":
                                                if (double.TryParse(value.ToString(), out double parsedHeight))
                                                {
                                                    temp.Height = parsedHeight;
                                                }
                                                break;
                                            case "IconSize":
                                                if (int.TryParse(value.ToString(), out int parseIconSize))
                                                {
                                                    temp.IconSize = parseIconSize;
                                                }
                                                break;
                                            case "IdleOpacity":
                                                if (double.TryParse(value.ToString(), out double parsedIdleOpacity))
                                                {
                                                    temp.IdleOpacity = parsedIdleOpacity;
                                                }

                                                break;
                                            case "AnimationSpeed":
                                                if (double.TryParse(value.ToString(), out double parsedAnimationSpeed))
                                                {
                                                    temp.AnimationSpeed = parsedAnimationSpeed;
                                                }
                                                break;
                                           
                                            case "MaxGrayScaleStrength":
                                                if (double.TryParse(value.ToString(), out double parsedMaxGrayScaleStrength))
                                                {
                                                    temp.MaxGrayScaleStrength = parsedMaxGrayScaleStrength;
                                                }
                                                break;
                                            case "GrayScaleEnabled":
                                                temp.GrayScaleEnabled = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"GrayScaleEnabled added\t{temp.GrayScaleEnabled}");
                                                break;
                                            case "GrayScaleEnabled_InactiveOnly":
                                                temp.GrayScaleEnabled_InactiveOnly = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"GrayScaleEnabled_InactiveOnly added\t{temp.GrayScaleEnabled_InactiveOnly}");
                                                break;
                                            case "Name":
                                                temp.Name = value.ToString()!;
                                                Debug.WriteLine($"Name added\t{temp.Name}");
                                                break;
                                            case "Folder":
                                                Debug.WriteLine("+trest:  " + value.ToString());
                                                temp.Folder = value.ToString()!;
                                                Debug.WriteLine($"Folder added\t{temp.Folder}");
                                                break;
                                            case "TitleFontFamily":
                                                temp.TitleFontFamily = value.ToString()!;
                                                Debug.WriteLine($"TitleFontFamily added\t{temp.TitleFontFamily}");
                                                break;
                                            case "Minimized":
                                                temp.Minimized = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"Minimized added\t{temp.Minimized}");
                                                break;
                                            case "ShowHiddenFiles":
                                                temp.ShowHiddenFiles = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"ShowHiddenFiles added\t{temp.ShowHiddenFiles}");
                                                break;
                                            case "LastAccesedToFirstRow":
                                                temp.LastAccesedToFirstRow = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"LastAccesedToFirstRow added\t{temp.LastAccesedToFirstRow}");
                                                break;
                                            case "ShowFileExtension":
                                                temp.ShowFileExtension = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"ShowFileExtension added\t{temp.ShowFileExtension}");
                                                break;
                                            case "ShowFileExtensionIcon":
                                                temp.ShowFileExtensionIcon = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"ShowFileExtensionIcon added\t{temp.ShowFileExtensionIcon}");
                                                break;
                                            case "ShowHiddenFilesIcon":
                                                temp.ShowHiddenFilesIcon = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"ShowHiddenFilesIcon added\t{temp.ShowHiddenFilesIcon}");
                                                break;
                                            case "ShowDisplayName":
                                                temp.ShowDisplayName = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"ShowDisplayName added\t{temp.ShowDisplayName}");
                                                break;
                                            case "IsLocked":
                                                temp.IsLocked = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"IsLocked added\t{temp.IsLocked}");
                                                break;
                                            case "ShowInGrid":
                                                temp.ShowInGrid = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"ShowInGrid added\t{temp.ShowInGrid}");
                                                break;
                                            case "AutoExpandonCursor":
                                                temp.AutoExpandonCursor = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"AutoExpandonCursor added\t{temp.AutoExpandonCursor}");
                                                break;
                                            case "ShowShortcutArrow":
                                                temp.ShowShortcutArrow = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"ShowShortcutArrow added\t{temp.ShowShortcutArrow}");
                                                break;
                                            case "FolderOpenInsideFrame":
                                                temp.FolderOpenInsideFrame = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"FolderOpenInsideFrame added\t{temp.FolderOpenInsideFrame}");
                                                break;
                                            case "HideTitleBarIconsWhenInactive":
                                                temp.HideTitleBarIconsWhenInactive = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"HideTitleBarIconsWhenInactive added\t{temp.HideTitleBarIconsWhenInactive}");
                                                break;
                                            case "SnapWidthToIconWidth":
                                                temp.SnapWidthToIconWidth = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"SnapWidthToIconWidth added\t{temp.SnapWidthToIconWidth}");
                                                break;
                                            case "SnapWidthToIconWidth_PlusScrollbarWidth":
                                                temp.SnapWidthToIconWidth_PlusScrollbarWidth = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"SnapWidthToIconWidth_PlusScrollbarWidth added\t{temp.SnapWidthToIconWidth_PlusScrollbarWidth}");
                                                break;
                                            case "CheckFolderSize":
                                                temp.CheckFolderSize = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"CheckFolderSize added\t{temp.CheckFolderSize}");
                                                break;
                                            case "TitleBarColor":
                                                temp.TitleBarColor = value.ToString()!;
                                                Debug.WriteLine($"TitleBarColor added\t{temp.TitleBarColor}");
                                                break;
                                            case "TitleTextColor":
                                                temp.TitleTextColor = value.ToString()!;
                                                Debug.WriteLine($"TitleTextColor added\t{temp.TitleTextColor}");
                                                break;
                                            case "TitleText":
                                                temp.TitleText = value.ToString();
                                                Debug.WriteLine($"TitleText added\t{temp.TitleText}");
                                                break;
                                            case "TitleTextAlignment":
                                                if (Enum.TryParse<System.Windows.HorizontalAlignment>(value.ToString()!, out var alignment))
                                                {
                                                    temp.TitleTextAlignment = alignment;
                                                }
                                                break;
                                            case "BorderColor":
                                                temp.BorderColor = value.ToString()!;
                                                Debug.WriteLine($"BorderColor added\t{temp.BorderColor}");
                                                break;
                                            case "BorderEnabled":
                                                temp.BorderEnabled = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"BorderEnabled added\t{temp.BorderEnabled}");
                                                break;
                                            case "ActiveBorderEnabled":
                                                temp.ActiveBorderEnabled = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"ActiveBorderEnabled added\t{temp.ActiveBorderEnabled}");
                                                break;
                                            case "ActiveBackgroundEnabled":
                                                temp.ActiveBackgroundEnabled = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"ActiveBackgroundEnabled added\t{temp.ActiveBackgroundEnabled}");
                                                break;
                                            case "ActiveTitleTextEnabled":
                                                temp.ActiveTitleTextEnabled = bool.Parse(value.ToString()!);
                                                Debug.WriteLine($"ActiveTitleTextEnabled added\t{temp.ActiveTitleTextEnabled}");
                                                break;
                                            case "FileFilterRegex":
                                                temp.FileFilterRegex = value.ToString()!;
                                                Debug.WriteLine($"FileFilterRegex added\t{temp.FileFilterRegex}");
                                                break;
                                            case "FileFilterHideRegex":
                                                temp.FileFilterHideRegex = value.ToString()!;
                                                Debug.WriteLine($"FileFilterHideRegex added\t{temp.FileFilterHideRegex}");
                                                break;
                                            case "ListViewBackgroundColor":
                                                temp.ListViewBackgroundColor = value.ToString()!;
                                                Debug.WriteLine($"ListViewBackgroundColor added\t{temp.ListViewBackgroundColor}");
                                                break;
                                            case "ActiveBackgroundColor":
                                                temp.ActiveBackgroundColor = value.ToString()!;
                                                Debug.WriteLine($"ActiveBackgroundColor added\t{temp.ActiveBackgroundColor}");
                                                break;
                                            case "ActiveBorderColor":
                                                temp.ActiveBorderColor = value.ToString()!;
                                                Debug.WriteLine($"ActiveBorderColor added\t{temp.ActiveBorderColor}");
                                                break;
                                            case "ActiveTitleTextColor":
                                                temp.ActiveTitleTextColor = value.ToString()!;
                                                Debug.WriteLine($"ActiveTitleTextColor added\t{temp.ActiveTitleTextColor}");
                                                break;
                                            case "ListViewFontColor":
                                                temp.ListViewFontColor = value.ToString()!;
                                                Debug.WriteLine($"ListViewFontColor added\t{temp.ListViewFontColor}");
                                                break;
                                            case "ListViewFontShadowColor":
                                                temp.ListViewFontShadowColor = value.ToString()!;
                                                Debug.WriteLine($"ListViewFontShadowColor added\t{temp.ListViewFontShadowColor}");
                                                break;
                                            case "Opacity":
                                                if (int.TryParse(value.ToString(), out int parsedOpacity))
                                                {
                                                    temp.Opacity = parsedOpacity;
                                                    Debug.WriteLine($"Opacity added\t{temp.Opacity}");
                                                }
                                                break;
                                            case "SortBy":
                                                if (Int32.TryParse(value.ToString(), out int parsedSortBy))
                                                {
                                                    temp.SortBy = parsedSortBy;
                                                }
                                                else
                                                {
                                                    temp.SortBy = 1;
                                                }
                                                break;
                                            case "FolderOrder":
                                                if (Int32.TryParse(value.ToString(), out int parsedFolderOrder))
                                                {
                                                    temp.FolderOrder = parsedFolderOrder;
                                                }
                                                break;
                                            case "ShowOnVirtualDesktops":
                                                if (value is string stringShowOnVirtualDesktops)
                                                {
                                                    try
                                                    {
                                                        int[] parsedArray = stringShowOnVirtualDesktops
                                                            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                                            .Select(s => int.Parse(s))
                                                            .ToArray();

                                                        temp.ShowOnVirtualDesktops = parsedArray;
                                                        Debug.WriteLine($"ShowOnVirtualDesktops added\t{temp.ShowOnVirtualDesktops}");
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Debug.WriteLine($"ShowOnVirtualDesktops failed to parse:\t{ex.Message}");
                                                    }
                                                }
                                                break; ;
                                            case "LastAccessedFiles":
                                                if (value is string[] strArray)
                                                {
                                                    temp.LastAccessedFiles = new List<string>(strArray);
                                                }
                                                break;

                                            case "TitleFontSize":
                                                if (double.TryParse(value.ToString(), out double parsedFontSize))
                                                {
                                                    temp.TitleFontSize = parsedFontSize;
                                                    Debug.WriteLine($"TitleFontSize loaded: {temp.TitleFontSize}");
                                                }
                                                break;
                                            default:
                                                Debug.WriteLine($"Unknown value: {valueName}");
                                                break;
                                        }
                                    }

                                }
                                if (temp.Name != "empty")
                                {
                                    if (Path.Exists(temp.Folder))
                                    {
                                        Instances.Add(temp);
                                    }
                                    else
                                    {
                                        RegistryKey key = Registry.CurrentUser.OpenSubKey(temp.GetKeyLocation(), true)!;
                                        if (key != null)
                                        {
                                            Registry.CurrentUser.DeleteSubKeyTree(temp.GetKeyLocation());
                                        }
                                    }
                                }

                            }
                            else
                            {
                                Debug.WriteLine("instance not valid");
                            }
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("try add an empty");
                    Instances.Add(new Instance("empty", false));
                    MainWindow._controller.WriteInstanceToKey(Instances[0]);

                }
            }
            Debug.WriteLine("Showing windows...");
            foreach (var Instance in Instances)
            {
                var subWindow = new DeskFrameWindow(Instance);
                _subWindows.Add(subWindow);
                subWindow.ChangeBackgroundOpacity(Instance.Opacity);
                subWindow.Show();
                _subWindowsPtr.Add(new WindowInteropHelper(subWindow).Handle);
                InitDetails();
            }
            foreach (var window in _subWindows)
            {
                window.HandleWindowMove(true);
                if (window.WonRight != null)
                {
                    window.WonRight.HandleWindowMove(false);
                }
                if (window.WonLeft != null)
                {
                    window.WonLeft.HandleWindowMove(false);
                }
            }
            if (Instances.Count == 0)
            {
                AddInstance();
            }
            Debug.WriteLine("Showing windows DONE");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ERROR reading key: {ex.Message}");
        }
        isInitializingInstances = false;
    }
}