using DeskFrame;
using System.ComponentModel;
using System.Diagnostics;
using static DeskFrame.DeskFrameWindow;
using Forms = System.Windows;

public class Instance : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public bool isWindowClosing = false;

    private double _posX;
    private double _posY;
    private double _width;
    private double _height;
    private double _idleOpacity = 1.0;
    private double _animationSpeed = 1.0;
    private double _maxGrayscaleStrength = 1.0;
    private bool _grayScaleEnabled = false;
    private bool _grayScaleEnabled_InactiveOnly = false;
    private string _name;
    private string _folder;
    private string _titleFontFamily = "Segoe UI";
    private bool _lastAccesedToFirstRow = false;
    private bool _settingDefault;
    private bool _minimized;
    private bool _showHiddenFiles;
    private bool _showFileExtension;
    private bool _showFileExtensionIcon;
    private bool _showHiddenFilesIcon;
    private bool _showDisplayName = true;
    private bool _isLocked;
    private bool _checkFolderSize = false;
    private bool _showInGrid = true;
    private bool _autoExpandonCursor = false;
    private bool _showShortcutArrow = true;
    private bool _folderOpenInsideFrame = true;
    private string _titleBarColor = "#0C000000";
    private string _titleTextColor = "#FFFFFF";
    private string _borderColor = "#FFFFFF";
    private bool _borderEnabled = false;
    private bool _activeBorderEnabled = false;
    private bool _activeBackgroundEnabled = false;
    private bool _activeTitleTextEnabled = false;
    private Forms.HorizontalAlignment _titleTextAlignment = Forms.HorizontalAlignment.Center;
    private string? _titleText;
    private string _fileFilterRegex = "";
    private string _fileFilterHideRegex = "";
    private string _listViewBackgroundColor = "#0C000000";
    private string _activeBackgroundColor = "#0C000000";
    private string _activeBorderColor = "#FFFFFF";
    private string _activeTitleTextColor = "#FFFFFF";
    private string _listViewFontColor = "#FFFFFF";
    private string _listViewFontShadowColor = "#000000";
    private List<string> _lastAccessedFiles = new List<string>();
    private int _opacity = 26;
    private int _sortBy = 1;
    private int _folderOrder = 0;
    private int[] _showOnVirtualDesktops;
    private double _titleFontSize = 13;
    private int _iconSize = 32;
    private bool _isShortcutsOnly = false;
    private bool _snapWidthToIconWidth = false;
    private bool _snapWidthToIconWidth_PlusScrollbarWidth = false;
    private bool _hideTitleBarIconsWhenInactive = false;

    public bool HideTitleBarIconsWhenInactive
    {
        get => _hideTitleBarIconsWhenInactive;
        set
        {
            if (_hideTitleBarIconsWhenInactive != value)
            {
                _hideTitleBarIconsWhenInactive = value;
                OnPropertyChanged(nameof(HideTitleBarIconsWhenInactive), value.ToString());
            }
        }
    }
    public bool SnapWidthToIconWidth
    {
        get => _snapWidthToIconWidth;
        set
        {
            if (_snapWidthToIconWidth != value)
            {
                _snapWidthToIconWidth = value;
                OnPropertyChanged(nameof(SnapWidthToIconWidth), value.ToString());
            }
        }
    }
    public bool SnapWidthToIconWidth_PlusScrollbarWidth
    {
        get => _snapWidthToIconWidth_PlusScrollbarWidth;
        set
        {
            if (_snapWidthToIconWidth_PlusScrollbarWidth != value)
            {
                _snapWidthToIconWidth_PlusScrollbarWidth = value;
                OnPropertyChanged(nameof(SnapWidthToIconWidth_PlusScrollbarWidth), value.ToString());
            }
        }
    }
    public bool IsShortcutsOnly
    {
        get => _isShortcutsOnly;
        set
        {
            if (_isShortcutsOnly != value)
            {
                _isShortcutsOnly = value;
                OnPropertyChanged(nameof(IsShortcutsOnly), value.ToString());
            }
        }
    }
    public int IconSize
    {
        get => _iconSize;
        set
        {
            if (_iconSize != value)
            {
                _iconSize = Math.Clamp(value, 16, 256);
                OnPropertyChanged(nameof(IconSize), value.ToString());
            }
        }
    }
    public List<string> LastAccessedFiles
    {
        get => _lastAccessedFiles;
        set
        {
            if (_lastAccessedFiles != value)
            {
                _lastAccessedFiles = value ?? new List<string>();
                OnPropertyChanged(nameof(LastAccessedFiles), value.ToString());
            }
        }
    }
    public double PosX
    {
        get => _posX;
        set
        {
            if (_posX != value)
            {
                _posX = value;
                OnPropertyChanged(nameof(PosX), value.ToString());
            }
        }
    }

    public double PosY
    {
        get => _posY;
        set
        {
            if (_posY != value)
            {
                _posY = value;
                OnPropertyChanged(nameof(PosY), value.ToString());
            }
        }
    }

    public double Width
    {
        get => _width;
        set
        {
            if (_width != value)
            {
                _width = value;
                OnPropertyChanged(nameof(Width), value.ToString());
            }
        }
    }
    public double Height
    {
        get => _height;
        set
        {
            if (_height != value)
            {
                _height = value;
                OnPropertyChanged(nameof(Height), value.ToString());
            }
        }
    }
    public double IdleOpacity
    {
        get => _idleOpacity;
        set
        {
            if (_idleOpacity != value)
            {
                _idleOpacity = value;
                OnPropertyChanged(nameof(IdleOpacity), value.ToString());
            }
        }
    }
    public double AnimationSpeed
    {
        get => _animationSpeed;
        set
        {
            if (_animationSpeed != value)
            {
                _animationSpeed = value;
                OnPropertyChanged(nameof(AnimationSpeed), value.ToString());
            }
        }
    }
    public double MaxGrayScaleStrength
    {
        get => _maxGrayscaleStrength;
        set
        {
            if (_maxGrayscaleStrength != value)
            {
                _maxGrayscaleStrength = value;
                OnPropertyChanged(nameof(MaxGrayScaleStrength), value.ToString());
            }
        }
    }
    public bool GrayScaleEnabled
    {
        get => _grayScaleEnabled;
        set
        {
            if (_grayScaleEnabled != value)
            {
                _grayScaleEnabled = value;
                OnPropertyChanged(nameof(GrayScaleEnabled), value.ToString());
            }
        }
    }
    public bool GrayScaleEnabled_InactiveOnly
    {
        get => _grayScaleEnabled_InactiveOnly;
        set
        {
            if (_grayScaleEnabled_InactiveOnly != value)
            {
                _grayScaleEnabled_InactiveOnly = value;
                OnPropertyChanged(nameof(GrayScaleEnabled_InactiveOnly), value.ToString());
            }
        }
    }
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name), value);
            }
        }
    }
    public string Folder
    {
        get => _folder;
        set
        {
            if (_folder != value)
            {
                _folder = value;
                OnPropertyChanged(nameof(Folder), value);
            }
        }
    }
    public string TitleFontFamily
    {
        get => _titleFontFamily;
        set
        {
            if (_titleFontFamily != value)
            {
                _titleFontFamily = value;
                OnPropertyChanged(nameof(TitleFontFamily), value);
            }
        }
    }
    public bool Minimized
    {
        get => _minimized;
        set
        {
            if (_minimized != value)
            {
                _minimized = value;
                OnPropertyChanged(nameof(Minimized), value.ToString());
            }
        }
    }
    public bool SettingDefault
    {
        get => _settingDefault;
        set
        {
            if (_settingDefault != value)
            {
                _settingDefault = value;
                OnPropertyChanged(nameof(SettingDefault), value.ToString());
            }
        }
    }

    public bool ShowHiddenFiles
    {
        get => _showHiddenFiles;
        set
        {
            if (_showHiddenFiles != value)
            {
                _showHiddenFiles = value;
                OnPropertyChanged(nameof(ShowHiddenFiles), value.ToString());
            }
        }
    }
    public bool LastAccesedToFirstRow
    {
        get => _lastAccesedToFirstRow;
        set
        {
            if (_lastAccesedToFirstRow != value)
            {
                _lastAccesedToFirstRow = value;
                OnPropertyChanged(nameof(LastAccesedToFirstRow), value.ToString());
            }
        }
    }
    public bool ShowFileExtension
    {
        get => _showFileExtension;
        set
        {
            if (_showFileExtension != value)
            {
                _showFileExtension = value;
                OnPropertyChanged(nameof(ShowFileExtension), value.ToString());
            }
        }
    }

    public bool ShowFileExtensionIcon
    {
        get => _showFileExtensionIcon;
        set
        {
            if (_showFileExtensionIcon != value)
            {
                _showFileExtensionIcon = value;
                OnPropertyChanged(nameof(ShowFileExtensionIcon), value.ToString());
            }
        }
    }

    public bool ShowHiddenFilesIcon
    {
        get => _showHiddenFilesIcon;
        set
        {
            if (_showHiddenFilesIcon != value)
            {
                _showHiddenFilesIcon = value;
                OnPropertyChanged(nameof(ShowHiddenFilesIcon), value.ToString());
            }
        }
    }

    public bool ShowDisplayName
    {
        get => _showDisplayName;
        set
        {
            if (_showDisplayName != value)
            {
                _showDisplayName = value;
                OnPropertyChanged(nameof(ShowDisplayName), value.ToString());
            }
        }
    }

    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (_isLocked != value)
            {
                _isLocked = value;
                OnPropertyChanged(nameof(IsLocked), value.ToString());
            }
        }
    }
    public bool ShowInGrid
    {
        get => _showInGrid;
        set
        {
            if (_showInGrid != value)
            {
                _showInGrid = value;
                OnPropertyChanged(nameof(ShowInGrid), value.ToString());
            }
        }
    }
    public bool AutoExpandonCursor
    {
        get => _autoExpandonCursor;
        set
        {
            if (_autoExpandonCursor != value)
            {
                _autoExpandonCursor = value;
                OnPropertyChanged(nameof(AutoExpandonCursor), value.ToString());
            }
        }
    }
    public bool ShowShortcutArrow
    {
        get => _showShortcutArrow;
        set
        {
            if (_showShortcutArrow != value)
            {
                _showShortcutArrow = value;
                OnPropertyChanged(nameof(ShowShortcutArrow), value.ToString());
            }
        }
    }
    public bool FolderOpenInsideFrame
    {
        get => _folderOpenInsideFrame;
        set
        {
            if (_folderOpenInsideFrame != value)
            {
                _folderOpenInsideFrame = value;
                OnPropertyChanged(nameof(FolderOpenInsideFrame), value.ToString());
            }
        }
    }
    public bool CheckFolderSize
    {
        get => _checkFolderSize;
        set
        {
            if (_checkFolderSize != value)
            {
                _checkFolderSize = value;
                OnPropertyChanged(nameof(CheckFolderSize), value.ToString());
            }
        }
    }
    public string TitleBarColor
    {
        get => _titleBarColor;
        set
        {
            if (_titleBarColor != value)
            {
                _titleBarColor = value;
                OnPropertyChanged(nameof(TitleBarColor), value);
            }
        }
    }

    public string TitleTextColor
    {
        get => _titleTextColor;
        set
        {
            if (_titleTextColor != value)
            {
                _titleTextColor = value;
                OnPropertyChanged(nameof(TitleTextColor), value);
            }
        }
    }

    public string BorderColor
    {
        get => _borderColor;
        set
        {
            if (_borderColor != value)
            {
                _borderColor = value;
                OnPropertyChanged(nameof(BorderColor), value);
            }
        }
    }

    public string ActiveBackgroundColor
    {
        get => _activeBackgroundColor;
        set
        {
            if (_activeBackgroundColor != value)
            {
                _activeBackgroundColor = value;
                OnPropertyChanged(nameof(ActiveBackgroundColor), value);
            }
        }
    }
    public string ActiveBorderColor
    {
        get => _activeBorderColor;
        set
        {
            if (_activeBorderColor != value)
            {
                _activeBorderColor = value;
                OnPropertyChanged(nameof(ActiveBorderColor), value);
            }
        }
    }
    public bool BorderEnabled
    {
        get => _borderEnabled;
        set
        {
            if (_borderEnabled != value)
            {
                _borderEnabled = value;
                OnPropertyChanged(nameof(BorderEnabled), value.ToString());
            }
        }
    }
    public bool ActiveBorderEnabled
    {
        get => _activeBorderEnabled;
        set
        {
            if (_activeBorderEnabled != value)
            {
                _activeBorderEnabled = value;
                OnPropertyChanged(nameof(ActiveBorderEnabled), value.ToString());
            }
        }
    }
    public bool ActiveBackgroundEnabled
    {
        get => _activeBackgroundEnabled;
        set
        {
            if (_activeBackgroundEnabled != value)
            {
                _activeBackgroundEnabled = value;
                OnPropertyChanged(nameof(ActiveBackgroundEnabled), value.ToString());
            }
        }
    }

    public bool ActiveTitleTextEnabled
    {
        get => _activeTitleTextEnabled;
        set
        {
            if (_activeTitleTextEnabled != value)
            {
                _activeTitleTextEnabled = value;
                OnPropertyChanged(nameof(ActiveTitleTextEnabled), value.ToString());
            }
        }
    }
    public string ActiveTitleTextColor
    {
        get => _activeTitleTextColor;
        set
        {
            if (_activeTitleTextColor != value)
            {
                _activeTitleTextColor = value;
                OnPropertyChanged(nameof(ActiveTitleTextColor), value);
            }
        }
    }
    public string? TitleText
    {
        get => _titleText;
        set
        {
            if (_titleText != value)
            {
                _titleText = value;
                OnPropertyChanged(nameof(TitleText), value);
            }
        }
    }

    public Forms.HorizontalAlignment TitleTextAlignment
    {
        get => _titleTextAlignment;
        set
        {
            if (_titleTextAlignment != value)
            {
                _titleTextAlignment = value;
                OnPropertyChanged(nameof(TitleTextAlignment), value.ToString());
            }
        }
    }

    public string FileFilterRegex
    {
        get => _fileFilterRegex;
        set
        {
            if (_fileFilterRegex != value)
            {
                _fileFilterRegex = value;
                OnPropertyChanged(nameof(FileFilterRegex), value);
            }
        }
    }
    public string FileFilterHideRegex
    {
        get => _fileFilterHideRegex;
        set
        {
            if (_fileFilterHideRegex != value)
            {
                _fileFilterHideRegex = value;
                OnPropertyChanged(nameof(FileFilterHideRegex), value);
            }
        }
    }

    public string ListViewBackgroundColor
    {
        get => _listViewBackgroundColor;
        set
        {
            if (_listViewBackgroundColor != value)
            {
                _listViewBackgroundColor = value;
                OnPropertyChanged(nameof(ListViewBackgroundColor), value);
            }
        }
    }
    public string ListViewFontColor
    {
        get => _listViewFontColor;
        set
        {
            if (_listViewFontColor != value)
            {
                _listViewFontColor = value;
                OnPropertyChanged(nameof(ListViewFontColor), value);
            }
        }
    }
    public string ListViewFontShadowColor
    {
        get => _listViewFontShadowColor;
        set
        {
            if (_listViewFontShadowColor != value)
            {
                _listViewFontShadowColor = value;
                OnPropertyChanged(nameof(ListViewFontShadowColor), value);
            }
        }
    }
    public int Opacity
    {
        get => _opacity;
        set
        {
            if (_opacity != value)
            {
                _opacity = value;
                OnPropertyChanged(nameof(Opacity), value.ToString());
            }
        }
    }
    public int SortBy
    {
        get => _sortBy;
        set
        {
            if (_sortBy != value)
            {
                _sortBy = value;
                OnPropertyChanged(nameof(SortBy), value.ToString());
            }
        }
    }
    public int FolderOrder
    {
        get => _folderOrder;
        set
        {
            if (_folderOrder != value)
            {
                _folderOrder = value;
                OnPropertyChanged(nameof(FolderOrder), value.ToString());
            }
        }
    }
    public int[] ShowOnVirtualDesktops
    {
        get => _showOnVirtualDesktops;
        set
        {
            if (_showOnVirtualDesktops != value)
            {
                _showOnVirtualDesktops = value;
                OnPropertyChanged(nameof(ShowOnVirtualDesktops), value?.ToString() ?? "");
            }
        }
    }
    public double TitleFontSize
    {
        get => _titleFontSize;
        set
        {
            if (_titleFontSize != value)
            {
                _titleFontSize = value;
                OnPropertyChanged(nameof(TitleFontSize), value.ToString());
            }
        }
    }
    public Instance(Instance instance, bool settingDefault)
    {
        _settingDefault = settingDefault;
        _posX = instance._posX;
        _posY = instance._posY;
        _width = instance.Width;
        _height = instance._height;
        _name = instance._name;
        _minimized = instance._minimized;
        _folder = instance._folder;
        _showHiddenFiles = instance._showHiddenFiles;
        _showFileExtension = instance._showFileExtension;
        _isLocked = instance._isLocked;
        _titleBarColor = instance._titleBarColor;
        _titleTextColor = instance._titleTextColor;
        _borderColor = instance._borderColor;
        _activeBackgroundColor = instance._activeBackgroundColor;
        _activeBorderColor = instance._activeBorderColor;
        _activeTitleTextColor = instance._activeTitleTextColor;
        _borderEnabled = instance._borderEnabled;
        _activeBorderEnabled = instance._activeBorderEnabled;
        _activeBackgroundEnabled = instance._activeBackgroundEnabled;
        _activeTitleTextEnabled = instance._activeTitleTextEnabled;
        _titleTextAlignment = instance._titleTextAlignment;
        _fileFilterRegex = instance._fileFilterRegex;
        _fileFilterHideRegex = instance._fileFilterHideRegex;
        _listViewBackgroundColor = instance._listViewBackgroundColor;
        _listViewFontColor = instance._listViewFontColor;
        _listViewFontShadowColor = instance.ListViewFontShadowColor;
        _opacity = instance._opacity;
        _sortBy = instance._sortBy;
        _titleFontSize = instance._titleFontSize;
        _titleFontFamily = instance._titleFontFamily;
    }

    public Instance(string name, bool settingDefault) // default instance
    {
        _settingDefault = settingDefault;
        _width = 175;
        _height = 215;
        _posX = Screen.PrimaryScreen!.Bounds.Width / 2 - _width / 2;
        _posY = Screen.PrimaryScreen!.Bounds.Height / 2 - _height / 2;
        _name = name;
        _minimized = false;
        _folder = "empty";
        _showHiddenFiles = false;
        _isLocked = false;
        if (name == "empty" || _settingDefault)
        {
            RegistryHelper helper = new RegistryHelper("DeskFrame");

            var v = helper.ReadKeyValueRoot("IdleOpacity");
            if (v != null) _idleOpacity = double.Parse(v.ToString());

            v = helper.ReadKeyValueRoot("AnimationSpeed");
            if (v != null) _animationSpeed = double.Parse(v.ToString());
           
            v = helper.ReadKeyValueRoot("MaxGrayScaleStrength");
            if (v != null) _maxGrayscaleStrength = double.Parse(v.ToString());
           
            v = helper.ReadKeyValueRoot("GrayScaleEnabled");
            if (v != null) _grayScaleEnabled = (bool)v;
           
            v = helper.ReadKeyValueRoot("GrayScaleEnabled_InactiveOnly");
            if (v != null) _grayScaleEnabled_InactiveOnly = (bool)v;

            v = helper.ReadKeyValueRoot("TitleFontFamily");
            if (v != null) _titleFontFamily = v.ToString();

            v = helper.ReadKeyValueRoot("ShowHiddenFiles");
            if (v != null) _showHiddenFiles = (bool)v;

            v = helper.ReadKeyValueRoot("ShowFileExtension");
            if (v != null) _showFileExtension = (bool)v;

            v = helper.ReadKeyValueRoot("ShowFileExtensionIcon");
            if (v != null) _showFileExtensionIcon = (bool)v;

            v = helper.ReadKeyValueRoot("ShowHiddenFilesIcon");
            if (v != null) _showHiddenFilesIcon = (bool)v;

            v = helper.ReadKeyValueRoot("ShowDisplayName");
            if (v != null) _showDisplayName = (bool)v;

            v = helper.ReadKeyValueRoot("BorderEnabled");
            if (v != null) _borderEnabled = (bool)v;

            v = helper.ReadKeyValueRoot("ActiveBorderEnabled");
            if (v != null) _activeBorderEnabled = (bool)v;

            v = helper.ReadKeyValueRoot("ActiveBackgroundEnabled");
            if (v != null) _activeBackgroundEnabled = (bool)v;

            v = helper.ReadKeyValueRoot("ActiveTitleTextEnabled");
            if (v != null) _activeTitleTextEnabled = (bool)v;

            v = helper.ReadKeyValueRoot("TitleTextAlignment");
            if (v != null) _titleTextAlignment = (Forms.HorizontalAlignment)Enum.Parse(typeof(Forms.HorizontalAlignment), v.ToString());

            v = helper.ReadKeyValueRoot("FileFilterRegex");
            if (v != null) _fileFilterRegex = v.ToString();

            v = helper.ReadKeyValueRoot("FileFilterHideRegex");
            if (v != null) _fileFilterHideRegex = v.ToString();

            v = helper.ReadKeyValueRoot("TitleBarColor");
            if (v != null) _titleBarColor = v.ToString();

            v = helper.ReadKeyValueRoot("TitleTextColor");
            if (v != null) _titleTextColor = v.ToString();

            v = helper.ReadKeyValueRoot("ListViewBackgroundColor");
            if (v != null) _listViewBackgroundColor = v.ToString();

            v = helper.ReadKeyValueRoot("ActiveBackgroundColor");
            if (v != null) _activeBackgroundColor = v.ToString();

            v = helper.ReadKeyValueRoot("ActiveTitleTextColor");
            if (v != null) _activeTitleTextColor = v.ToString();

            v = helper.ReadKeyValueRoot("ActiveBorderColor");
            if (v != null) _activeBorderColor = v.ToString();

            v = helper.ReadKeyValueRoot("ListViewFontColor");
            if (v != null) _listViewFontColor = v.ToString();

            v = helper.ReadKeyValueRoot("ListViewFontShadowColor");
            if (v != null) _listViewFontShadowColor = v.ToString();

            v = helper.ReadKeyValueRoot("Opacity");
            if (v != null) _opacity = int.Parse(v.ToString());

            v = helper.ReadKeyValueRoot("SortBy");
            if (v != null) _sortBy = int.Parse(v.ToString());

            v = helper.ReadKeyValueRoot("FolderOrder");
            if (v != null) _folderOrder = int.Parse(v.ToString());

            v = helper.ReadKeyValueRoot("TitleFontSize");
            if (v != null) _titleFontSize = double.Parse(v.ToString());

            v = helper.ReadKeyValueRoot("IconSize");
            if (v != null) _iconSize = int.Parse(v.ToString());
        }
    }
    protected void OnPropertyChanged(string propertyName, string value)
    {
        if (isWindowClosing) return;
        string[] notGlobalProperties = {
            "PosX",
            "PosY",
            "Width",
            "Height",
            "IconSize",
            "Name",
            "Folder",
            "Minimized",
            "ShowInGrid",
            "IsLocked",
            "CheckFolderSize",
            "TitleText",
            "ShowOnVirtualDesktops",
            "SettingDefault"
        };

        if (propertyName == "Name")
        {
            Debug.WriteLine($"oldname: {_name} \t newname: {Name}");
            if (Name == "empty")
            {
                MainWindow._controller.WriteOverInstanceToKey(this, "empty");

            }
        }
        else
        {
            if ((!propertyName.Contains("Pos")))
            {
                // Debug.WriteLine($"Property {propertyName} has changed.");
            }
            if (!_settingDefault && Name != "empty" && Folder != null)
            {
                if (propertyName == "LastAccessedFiles")
                {
                    MainWindow._controller.reg.WriteMultiLineRegistry(propertyName, LastAccessedFiles, this);
                }
                else if (propertyName != "ShowOnVirtualDesktops")
                {
                    MainWindow._controller.reg.WriteToRegistry(propertyName, value, this);
                }
                else if (propertyName == "ShowOnVirtualDesktops")
                {
                    MainWindow._controller.reg.WriteIntArrayToRegistry(propertyName, ShowOnVirtualDesktops, this);
                }
            }
            if (_settingDefault && !notGlobalProperties.Contains(propertyName))
            {
                MainWindow._controller.reg.WriteToRegistryRoot(propertyName, value);
            }
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public string GetKeyLocation()
    {
        if (_name != null && Name != null)
        {
            return @$"SOFTWARE\DeskFrame\Instances\{Name}";

        }
        return "";
    }
}