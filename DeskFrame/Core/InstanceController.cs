using DeskFrame;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

public class InstanceController
{
    public static string appName = "DeskFrame";
    public List<Instance> Instances = new List<Instance>();
    public RegistryHelper reg = new RegistryHelper(appName);
    public List<DeskFrameWindow> _subWindows = new List<DeskFrameWindow>();

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
                key.SetValue("Minimized", instance.Minimized!);
                key.SetValue("Folder", instance.Folder!);
                key.SetValue("TitleFontFamily", instance.TitleFontFamily!);
                key.SetValue("ShowHiddenFiles", instance.ShowHiddenFiles!);
                key.SetValue("ShowFileExtension", instance.ShowFileExtension!);
                key.SetValue("ShowFileExtensionIcon", instance.ShowFileExtensionIcon!);
                key.SetValue("ShowHiddenFilesIcon", instance.ShowHiddenFilesIcon!);
                key.SetValue("ShowDisplayName", instance.ShowDisplayName!);
                key.SetValue("IsLocked", instance.IsLocked!);
                key.SetValue("ShowInGrid", instance.ShowInGrid!);
                key.SetValue("TitleBarColor", instance.TitleBarColor!);
                key.SetValue("TitleTextColor", instance.TitleTextColor!);
                key.SetValue("TitleTextAlignment", instance.TitleTextAlignment.ToString());
                key.SetValue("TitleText", instance.TitleText);
                key.SetValue("BorderColor", instance.BorderColor!);
                key.SetValue("BorderEnabled", instance.BorderEnabled!);
                key.SetValue("FileFilterRegex", instance.FileFilterRegex!);
                key.SetValue("FileFilterHideRegex", instance.FileFilterHideRegex!);
                key.SetValue("ListViewBackgroundColor", instance.ListViewBackgroundColor!);
                key.SetValue("ListViewFontColor", instance.ListViewFontColor!);
                key.SetValue("ListViewFontShadowColor", instance.ListViewFontShadowColor!);
                key.SetValue("Opacity", instance.Opacity);
                key.SetValue("SortBy", instance.SortBy);
                key.SetValue("FolderOrder", instance.FolderOrder);
                key.SetValue("TitleFontSize", instance.TitleFontSize);
            }
            Registry.CurrentUser.DeleteSubKey(@$"SOFTWARE\{appName}\Instances\{oldKey}", throwOnMissingSubKey: false);
        }
        catch { }
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
            return;
        }
        try
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(instance.GetKeyLocation()))
            {
                key.SetValue("Name", instance.Name!);
                key.SetValue("PosX", instance.PosX!);
                key.SetValue("PosY", instance.PosY!);
                key.SetValue("Width", instance.Width!);
                key.SetValue("Height", instance.Height!);
                key.SetValue("Minimized", instance.Minimized!);
                key.SetValue("Folder", instance.Folder!);
                key.SetValue("TitleFontFamily", instance.TitleFontFamily!);
                key.SetValue("ShowHiddenFiles", instance.ShowHiddenFiles!);
                key.SetValue("ShowFileExtension", instance.ShowFileExtension!);
                key.SetValue("ShowFileExtensionIcon", instance.ShowFileExtensionIcon!);
                key.SetValue("ShowHiddenFilesIcon", instance.ShowHiddenFilesIcon!);
                key.SetValue("ShowDisplayName", instance.ShowDisplayName!);
                key.SetValue("IsLocked", instance.IsLocked!);
                key.SetValue("ShowInGrid", instance.ShowInGrid!);
                key.SetValue("TitleBarColor", instance.TitleBarColor!);
                key.SetValue("TitleTextColor", instance.TitleTextColor!);
                key.SetValue("TitleTextAlignment", instance.TitleTextAlignment.ToString());
                key.SetValue("TitleText", instance.TitleText);
                key.SetValue("BorderColor", instance.BorderColor!);
                key.SetValue("BorderEnabled", instance.BorderEnabled!);
                key.SetValue("FileFilterRegex", instance.FileFilterRegex!);
                key.SetValue("FileFilterHideRegex", instance.FileFilterHideRegex!);
                key.SetValue("ListViewBackgroundColor", instance.ListViewBackgroundColor!);
                key.SetValue("ListViewFontColor", instance.ListViewFontColor!);
                key.SetValue("ListViewFontShadowColor", instance.ListViewFontShadowColor!);
                key.SetValue("Opacity", instance.Opacity);
                key.SetValue("SortBy", instance.SortBy);
                key.SetValue("FolderOrder", instance.FolderOrder);
                key.SetValue("TitleFontSize", instance.TitleFontSize);
            }
        }
        catch { }
    }

    public void AddInstance()
    {
        var existingEmptyInstance = Instances.FirstOrDefault(instance => instance.Name == "empty");

        if (existingEmptyInstance != null)
        {
            Instances.Remove(existingEmptyInstance);
        }

        Instances.Add(new Instance("empty"));
        MainWindow._controller.WriteInstanceToKey(Instances.Last());
        var subWindow = new DeskFrameWindow(Instances.Last());
        subWindow.ChangeBackgroundOpacity(Instances.Last().Opacity);
        _subWindows.Add(subWindow);
        subWindow.Show();
        InitDetails();
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

    public void InitInstances()
    {
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
                                Instance temp = new Instance("");
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
                    Instances.Add(new Instance("empty"));
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
                InitDetails();
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
    }
}