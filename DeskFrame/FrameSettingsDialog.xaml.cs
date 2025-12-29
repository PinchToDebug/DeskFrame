using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Color = System.Windows.Media.Color;
using DeskFrame.ColorPicker;
using System.IO;
using System.Drawing.Text;
using System.Collections.ObjectModel;
using TextBox = Wpf.Ui.Controls.TextBox;
using Brush = System.Windows.Media.Brush;
using WindowsDesktop;
using System.Windows.Controls;
using MenuItem = Wpf.Ui.Controls.MenuItem;
namespace DeskFrame
{
    public partial class FrameSettingsDialog : FluentWindow
    {
        private DeskFrameWindow _frame;
        private Instance _instance;
        private Instance _originalInstance;
        private bool _isValidTitleBarColor = false;
        private bool _isValidTitleTextColor = false;
        private bool _isValidTitleTextAlignment = true;
        private bool _isValidBorderColor = false;
        private bool _isValidActiveBorderColor = false;
        private bool _isValidActiveBackgroundColor = false;
        private bool _isValidActiveTitleTextColor = false;
        private bool _isValidFileFilterRegex = true;
        private bool _isValidFileFilterHideRegex = true;
        private bool _isValidListViewBackgroundColor = true;
        private bool _isValidListViewFontColor = true;
        private bool _isValidListViewFontShadowColor = true;
        private bool _isValidShowOnVirtualDesktops = true;
        private bool _isReverting = false;
        private bool _initDone = false;
        string _lastInstanceName;
        private Brush _borderBrush;
        private Brush _backgroundBrush;
        public ObservableCollection<string> FontList;

        public FrameSettingsDialog(DeskFrameWindow frame)
        {
            InitializeComponent();
            _backgroundBrush = TitleBarColorTextBox.Background;
            _borderBrush = TitleBarColorTextBox.BorderBrush;
            // DataContext = this;
            _originalInstance = new Instance(frame.Instance, frame.Instance.SettingDefault);
            _lastInstanceName = _originalInstance.Name;
            _frame = frame;
            _instance = frame.Instance;
            DataContext = _instance;

            if (_instance.Folder == "empty")
            {
                ChangeFolderButton.Visibility = Visibility.Collapsed;
            }
            if (_instance.SettingDefault)
            {
                TitleTextBox.Visibility = Visibility.Hidden;
                ShowOnVirtualDesktopTextBox.Visibility = Visibility.Hidden;
                FileFilterRegexTextBox.Visibility = Visibility.Hidden;
                FileFilterHideRegexTextBox.Visibility = Visibility.Hidden;
            }
            AnimationSpeedSlider.Value = _instance.AnimationSpeed * 4;
            AnimationSpeedLabel.Content = _instance.AnimationSpeed == 0.0 ? "OFF" : "x" + _instance.AnimationSpeed;
            IdleOpacitySlider.Value = _instance.IdleOpacity * 10;
            IdleOpacityLabel.Content = _instance.IdleOpacity * 100 + "%";
            IconSizeSlider.Value = _instance.IconSize / 4;
            IconSizeLabel.Content = _instance.IconSize;
            GrayScaleEnabled_CheckBox.IsChecked = _instance.GrayScaleEnabled;
            GrayScaleEnabled_InactiveOnly_CheckBox.IsChecked = _instance.GrayScaleEnabled_InactiveOnly;
            MaxGrayScaleStrengthSlider.Value = _instance.MaxGrayScaleStrength * 10;

            frame.AnimateWindowOpacity(_instance.IdleOpacity, _instance.AnimationSpeed);
            if (!frame.VirtualDesktopSupported)
            {
                ShowOnVirtualDesktopTextBox.Text = "Not available";
                ShowOnVirtualDesktopTextBox.IsEnabled = false;
            }
            else
            {
                ShowOnVirtualDesktopTextBox.Text = _instance.ShowOnVirtualDesktops != null
                    ? string.Join(",", _instance.ShowOnVirtualDesktops)
                    : string.Empty;
            }
            _originalInstance.ShowOnVirtualDesktops = _instance.ShowOnVirtualDesktops;
            _originalInstance.AnimationSpeed = _instance.AnimationSpeed;
            _originalInstance.IdleOpacity = _instance.IdleOpacity;
            _originalInstance.IconSize = _instance.IconSize;
            _originalInstance.HideTitleBarIconsWhenInactive = _instance.HideTitleBarIconsWhenInactive;
            _originalInstance.SnapWidthToIconWidth = _instance.SnapWidthToIconWidth;
            _originalInstance.SnapWidthToIconWidth_PlusScrollbarWidth = _instance.SnapWidthToIconWidth_PlusScrollbarWidth;
            _originalInstance.ShowShortcutArrow = _instance.ShowShortcutArrow;

            _originalInstance.MaxGrayScaleStrength = _instance.MaxGrayScaleStrength;


            TitleBarColorTextBox.Text = _instance.TitleBarColor;
            TitleTextColorTextBox.Text = _instance.TitleTextColor;
            ListViewBackgroundColorTextBox.Text = _instance.ListViewBackgroundColor;
            ListViewFontColorTextBox.Text = _instance.ListViewFontColor;
            ListViewFontShadowColorTextBox.Text = _instance.ListViewFontShadowColor;
            BorderColorTextBox.Text = _instance.BorderColor;
            ActiveBorderColorTextBox.Text = _instance.ActiveBorderColor;
            ActiveBackgroundColorTextBox.Text = _instance.ActiveBackgroundColor;
            ActiveTitleTextColorTextBox.Text = _instance.ActiveTitleTextColor;
            BorderEnabledCheckBox.IsChecked = _instance.BorderEnabled;
            ActiveBorderEnabledCheckBox.IsChecked = _instance.ActiveBorderEnabled;
            ActiveBackgroundEnabledCheckBox.IsChecked = _instance.ActiveBackgroundEnabled;
            ActiveTitleTextEnabledCheckBox.IsChecked = _instance.ActiveTitleTextEnabled;
            TitleTextBox.Text = _instance.TitleText ?? _instance.Name;
            TitleFontSizeNumberBox.Value = _instance.TitleFontSize;
            if (!_instance.SettingDefault)
            {
                _originalInstance.TitleText = TitleTextBox.Text;
            }
            FileFilterRegexTextBox.Text = _instance.FileFilterRegex;
            FileFilterHideRegexTextBox.Text = _instance.FileFilterHideRegex;
            TitleTextAlignmentComboBox.SelectedIndex = (int)_instance.TitleTextAlignment;

            ShowFileExtensionIconCheckBox.IsChecked = _instance.ShowFileExtensionIcon;
            ShowHiddenFilesIconCheckBox.IsChecked = _instance.ShowHiddenFilesIcon;
            ShowDisplayNameCheckBox.IsChecked = _instance.ShowDisplayName;
            HideTitleBarIconsWhenInactive_CheckBox.IsChecked = _instance.HideTitleBarIconsWhenInactive;
            SnapWidthToIconWidth_CheckBox.IsChecked = _instance.SnapWidthToIconWidth;
            SnapWidthToIconWidth_PlusScrollbarWidth_CheckBox.IsChecked = _instance.SnapWidthToIconWidth_PlusScrollbarWidth;
            SnapWidthToIconWidth_PlusScrollbarWidth_CheckBox.Visibility = _instance.SnapWidthToIconWidth ? Visibility.Visible : Visibility.Collapsed;
            AutoExpandonCursorCheckBox.IsChecked = _instance.AutoExpandonCursor;
            ShowShortcutArrowCheckBox.IsChecked = _instance.ShowShortcutArrow;
            FolderOpenInsideFrameCheckBox.IsChecked = _instance.FolderOpenInsideFrame;
            CheckFolderSizeCheckBox.IsChecked = _instance.CheckFolderSize;
            ShowLastAccessedToFirstRowCheckBox.IsChecked = _instance.LastAccesedToFirstRow;

            TitleTextAutoSuggestionBox.Text = _instance.TitleFontFamily;
            ItemTextAutoSuggestionBox.Text = _instance.ItemFontFamily;
            _frame.title.FontSize = _instance.TitleFontSize;
            _frame.title.TextWrapping = TextWrapping.Wrap;

            double titleBarHeight = Math.Max(30, _instance.TitleFontSize * 1.5);
            _frame.titleBar.Height = titleBarHeight;
            double scrollViewerMargin = titleBarHeight + 5;
            _frame.scrollViewer.Margin = new Thickness(0, scrollViewerMargin, 0, 0);

            TitleFontSizeNumberBox.ValueChanged += (sender, args) =>
            {
                if (args.NewValue.HasValue)
                {
                    _instance.TitleFontSize = args.NewValue.Value;
                    _frame.title.FontSize = args.NewValue.Value;
                    _frame.title.TextWrapping = TextWrapping.Wrap;

                    double titleBarHeight = Math.Max(30, args.NewValue.Value * 1.5);
                    _frame.titleBar.Height = titleBarHeight;

                    double scrollViewerMargin = titleBarHeight + 5;
                    _frame.scrollViewer.Margin = new Thickness(0, scrollViewerMargin, 0, 0);
                }
            };


            FontList = new ObservableCollection<string>();
            InstalledFontCollection fonts = new InstalledFontCollection();
            foreach (System.Drawing.FontFamily font in fonts.Families)
            {
                FontList.Add(font.Name);
            }

            TitleTextAutoSuggestionBox.OriginalItemsSource = FontList;
            TitleTextAutoSuggestionBox.TextChanged += (sender, args) =>
            {
                if (TitleTextAutoSuggestionBox.Text != null)
                {
                    _frame.title.FontFamily = new System.Windows.Media.FontFamily(TitleTextAutoSuggestionBox.Text);
                    _instance.TitleFontFamily = TitleTextAutoSuggestionBox.Text;
                }
                else
                {
                    _frame.title.FontFamily = new System.Windows.Media.FontFamily(TitleTextAutoSuggestionBox.Text);

                }
            };
            ItemTextAutoSuggestionBox.OriginalItemsSource = FontList;
            ItemTextAutoSuggestionBox.TextChanged += (sender, args) =>
            {
                if (ItemTextAutoSuggestionBox.Text != null)
                {
                    _frame.Resources["ItemFont"] = new System.Windows.Media.FontFamily(ItemTextAutoSuggestionBox.Text);
                    _instance.ItemFontFamily = ItemTextAutoSuggestionBox.Text;
                }
                else
                {
                    _frame.Resources["ItemFont"] = new System.Windows.Media.FontFamily(ItemTextAutoSuggestionBox.Text);
                }
            };
            _initDone = true;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_initDone) return;
            ValidateSettings();
        }
        private void TextChangedHandler(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ValidateSettings();
        }
        private void TitleTextAlignmentComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ValidateSettings();
        }

        private void BorderEnabledCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //  BorderColorTextBox.IsEnabled = BorderEnabledCheckBox.IsChecked == true;
            ValidateSettings();
        }
        private void ActiveBorderEnabledCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // ActiveBorderColorTextBox.IsEnabled = ActiveBorderEnabledCheckBox.IsChecked == true;
            ValidateSettings();
        }
        private void ActiveTitleTextEnabledCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //  ActiveTitleTextColorTextBox.IsEnabled = ActiveTitleTextEnabledCheckBox.IsChecked == true;
            ValidateSettings();
        }
        private void ActiveBackgroundEnabledCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //  ActiveBackgroundColorTextBox.IsEnabled = ActiveBackgroundEnabledCheckBox.IsChecked == true;
            ValidateSettings();
        }

        private bool ValidateVirtualDesktop(string strValue)
        {
            return strValue
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .All(s => int.TryParse(s, out _));
        }
        private void ValidateSettings()
        {
            if (_isReverting) return;
            _instance.AnimationSpeed = AnimationSpeedSlider.Value * 0.25;
            AnimationSpeedLabel.Content = _instance.AnimationSpeed == 0 ? "OFF" : "x" + _instance.AnimationSpeed;
            _instance.IdleOpacity = IdleOpacitySlider.Value / 10;
            IdleOpacityLabel.Content = _instance.IdleOpacity * 100 + "%";
            _instance.IconSize = (int)(IconSizeSlider.Value * 4);
            IconSizeLabel.Content = _instance.IconSize;

            _frame.AnimateWindowOpacity(_instance.IdleOpacity, _instance.AnimationSpeed);
            _isValidTitleBarColor = TryParseColor(string.IsNullOrEmpty(TitleBarColorTextBox.Text) ? "#0C000000" : TitleBarColorTextBox.Text, TitleBarColorTextBox);
            _isValidTitleTextColor = TryParseColor(string.IsNullOrEmpty(TitleTextColorTextBox.Text) ? "#FFFFFF" : TitleTextColorTextBox.Text, TitleTextColorTextBox);
            _isValidBorderColor = BorderEnabledCheckBox.IsChecked == true ? TryParseColor(string.IsNullOrEmpty(BorderColorTextBox.Text) ? "#FFFFFF" : BorderColorTextBox.Text, BorderColorTextBox) : true;
            _isValidActiveBorderColor = ActiveBorderEnabledCheckBox.IsChecked == true ? TryParseColor(string.IsNullOrEmpty(ActiveBorderColorTextBox.Text) ? "#FFFFFF" : ActiveBorderColorTextBox.Text, ActiveBorderColorTextBox) : true;
            _isValidActiveBackgroundColor = ActiveBackgroundEnabledCheckBox.IsChecked == true ? TryParseColor(string.IsNullOrEmpty(ActiveBackgroundColorTextBox.Text) ? "#FFFFFF" : ActiveBackgroundColorTextBox.Text, ActiveBackgroundColorTextBox) : true;
            _isValidActiveTitleTextColor = ActiveTitleTextEnabledCheckBox.IsChecked == true ? TryParseColor(string.IsNullOrEmpty(ActiveTitleTextColorTextBox.Text) ? "#FFFFFF" : ActiveTitleTextColorTextBox.Text, ActiveTitleTextColorTextBox) : true;
            _isValidFileFilterRegex = TryParseRegex(FileFilterRegexTextBox.Text, FileFilterRegexTextBox);
            _isValidFileFilterHideRegex = TryParseRegex(FileFilterHideRegexTextBox.Text, FileFilterHideRegexTextBox);

            _isValidTitleTextAlignment = TitleTextAlignmentComboBox.SelectedIndex >= 0;
            _isValidListViewBackgroundColor = TryParseColor(string.IsNullOrEmpty(ListViewBackgroundColorTextBox.Text) ? "#0C000000" : ListViewBackgroundColorTextBox.Text, ListViewBackgroundColorTextBox);
            _isValidListViewFontColor = TryParseColor(string.IsNullOrEmpty(ListViewFontColorTextBox.Text) ? "#FFFFFF" : ListViewFontColorTextBox.Text, ListViewFontColorTextBox);
            _isValidListViewFontShadowColor = TryParseColor(string.IsNullOrEmpty(ListViewFontShadowColorTextBox.Text) ? "#000000" : ListViewFontShadowColorTextBox.Text, ListViewFontShadowColorTextBox);

            _isValidShowOnVirtualDesktops = ValidateVirtualDesktop(ShowOnVirtualDesktopTextBox.Text);

            if (_isValidTitleBarColor && _isValidTitleTextColor && _isValidTitleTextAlignment &&
                _isValidBorderColor && _isValidActiveBorderColor && _isValidActiveBackgroundColor && _isValidActiveTitleTextColor && _isValidFileFilterRegex && _isValidFileFilterHideRegex &&
                _isValidListViewBackgroundColor && _isValidListViewFontColor && _isValidListViewFontShadowColor &&
                _isValidShowOnVirtualDesktops)
            {
                _instance.TitleBarColor = string.IsNullOrEmpty(TitleBarColorTextBox.Text) ? "#0C000000" : TitleBarColorTextBox.Text;
                _instance.TitleTextColor = string.IsNullOrEmpty(TitleTextColorTextBox.Text) ? "#FFFFFF" : TitleTextColorTextBox.Text;

                _instance.BorderColor = BorderColorTextBox.Text;
                _instance.ActiveBorderColor = ActiveBorderColorTextBox.Text;
                _instance.ActiveBackgroundColor = ActiveBackgroundColorTextBox.Text;
                _instance.ActiveTitleTextColor = ActiveTitleTextColorTextBox.Text;
                _instance.BorderEnabled = BorderEnabledCheckBox.IsChecked == true;
                _instance.ActiveBorderEnabled = ActiveBorderEnabledCheckBox.IsChecked == true;
                _instance.ActiveBackgroundEnabled = ActiveBackgroundEnabledCheckBox.IsChecked == true;
                _instance.ActiveTitleTextEnabled = ActiveTitleTextEnabledCheckBox.IsChecked == true;
                _instance.TitleTextAlignment = (System.Windows.HorizontalAlignment)TitleTextAlignmentComboBox.SelectedIndex;
                _instance.TitleText = TitleTextBox.Text;
                _instance.FileFilterRegex = FileFilterRegexTextBox.Text;
                _instance.FileFilterHideRegex = FileFilterHideRegexTextBox.Text;

                _instance.ListViewBackgroundColor = string.IsNullOrEmpty(ListViewBackgroundColorTextBox.Text) ? "#0C000000" : ListViewBackgroundColorTextBox.Text;
                _instance.ListViewFontColor = string.IsNullOrEmpty(ListViewFontColorTextBox.Text) ? "#FFFFFF" : ListViewFontColorTextBox.Text;
                _instance.ListViewFontShadowColor = string.IsNullOrEmpty(ListViewFontShadowColorTextBox.Text) ? "#000000" : ListViewFontShadowColorTextBox.Text;
                _instance.Opacity = ((Color)System.Windows.Media.ColorConverter.ConvertFromString(_instance.ListViewBackgroundColor)).A;
                _instance.TitleFontSize = TitleFontSizeNumberBox.Value ?? 12;

                var parts = ShowOnVirtualDesktopTextBox.Text
                    .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                _instance.ShowOnVirtualDesktops = parts.Length > 0
                    ? parts.Select(s => int.Parse(s)).ToArray()
                    : null;

                if (_instance.ShowOnVirtualDesktops != null && !_instance.ShowOnVirtualDesktops.Contains(Array.IndexOf(VirtualDesktop.GetDesktops(), VirtualDesktop.Current) + 1))
                {
                    _frame.Hide();
                }
                else
                {
                    _frame.Show();
                    this.Activate();
                }
                _frame.titleBar.Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_instance.TitleBarColor));
                _frame.title.Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_instance.TitleTextColor));
                _frame.title.Text = TitleTextBox.Text ?? _frame.Instance.Name;
                _frame.WindowBackground.Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_instance.ListViewBackgroundColor)); ;


                TitleBarColorTextBox.Icon!.Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_instance.TitleBarColor));
                TitleTextColorTextBox.Icon!.Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_instance.TitleTextColor));
                ListViewBackgroundColorTextBox.Icon!.Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_instance.ListViewBackgroundColor));
                ListViewFontColorTextBox.Icon!.Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_instance.ListViewFontColor));
                ListViewFontShadowColorTextBox.Icon!.Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_instance.ListViewFontShadowColor));
                BorderColorTextBox.Icon!.Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(TryParseColor(BorderColorTextBox.Text, BorderColorTextBox) ? _instance.BorderColor : "#FFFFFF"));
                ActiveBorderColorTextBox.Icon!.Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(TryParseColor(ActiveBorderColorTextBox.Text, ActiveBorderColorTextBox) ? _instance.ActiveBorderColor : "#FFFFFF"));
                ActiveBackgroundColorTextBox.Icon!.Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(TryParseColor(ActiveBackgroundColorTextBox.Text, ActiveBackgroundColorTextBox) ? _instance.ActiveBackgroundColor : "#FFFFFF"));
                ActiveTitleTextColorTextBox.Icon!.Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(TryParseColor(ActiveTitleTextColorTextBox.Text, ActiveTitleTextColorTextBox) ? _instance.ActiveTitleTextColor : "#FFFFFF"));
            }
        }

        private bool TryParseColor(string colorText, TextBox tb)
        {
            try
            {
                new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(colorText));
                tb.BorderBrush = _borderBrush;
                tb.Background = _backgroundBrush;
                return true;
            }
            catch
            {
                tb.BorderBrush = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFF96A6A"));
                tb.Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#0FD82B2B"));
                return false;
            }
        }

        private bool TryParseRegex(string regexText, TextBox tb)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(regexText))
                {
                    new System.Text.RegularExpressions.Regex(regexText);
                }
                tb.BorderBrush = _borderBrush;
                tb.Background = _backgroundBrush;
                return true;
            }
            catch
            {
                tb.BorderBrush = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFF96A6A"));
                tb.Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#0FD82B2B"));
                return false;
            }
        }

        private async void RevertButton_Click(object sender, RoutedEventArgs e)
        {

            var dialog = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Confirm",
                Content = "Are you sure you want to revert it?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No"
            };

            var result = await dialog.ShowDialogAsync();

            if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                _isReverting = true;
                _instance.TitleBarColor = _originalInstance.TitleBarColor;
                _instance.TitleTextColor = _originalInstance.TitleTextColor;
                _instance.BorderColor = _originalInstance.BorderColor;
                _instance.ActiveBorderColor = _originalInstance.ActiveBorderColor;
                _instance.ActiveBackgroundColor = _originalInstance.ActiveBackgroundColor;
                _instance.ActiveTitleTextColor = _originalInstance.ActiveTitleTextColor;
                _instance.BorderEnabled = _originalInstance.BorderEnabled;
                _instance.ActiveBorderEnabled = _originalInstance.ActiveBorderEnabled;
                _instance.ActiveBackgroundEnabled = _originalInstance.ActiveBackgroundEnabled;
                _instance.ActiveTitleTextEnabled = _originalInstance.ActiveTitleTextEnabled;
                _instance.TitleText = _originalInstance.TitleText ?? _originalInstance.Name;
                _instance.FileFilterRegex = _originalInstance.FileFilterRegex;
                _instance.FileFilterHideRegex = _originalInstance.FileFilterHideRegex;
                _instance.TitleTextAlignment = _originalInstance.TitleTextAlignment;
                _instance.ListViewBackgroundColor = _originalInstance.ListViewBackgroundColor;
                _instance.ListViewFontColor = _originalInstance.ListViewFontColor;
                _instance.ListViewFontShadowColor = _originalInstance.ListViewFontShadowColor;
                _instance.Opacity = _originalInstance.Opacity;
                _instance.TitleFontSize = _originalInstance.TitleFontSize;
                _instance.TitleFontFamily = _originalInstance.TitleFontFamily;
                _instance.ItemFontFamily = _originalInstance.ItemFontFamily;
                _instance.ShowOnVirtualDesktops = _originalInstance.ShowOnVirtualDesktops;
                _instance.IdleOpacity = _originalInstance.IdleOpacity;
                _instance.IconSize = _originalInstance.IconSize;
                _instance.AnimationSpeed = _originalInstance.AnimationSpeed;
                _instance.AutoExpandonCursor = _originalInstance.AutoExpandonCursor;
                _instance.ShowShortcutArrow = _originalInstance.ShowShortcutArrow;
                _instance.FolderOpenInsideFrame = _originalInstance.FolderOpenInsideFrame;
                _instance.CheckFolderSize = _originalInstance.CheckFolderSize;
                _instance.HideTitleBarIconsWhenInactive = _originalInstance.HideTitleBarIconsWhenInactive;
                _instance.SnapWidthToIconWidth = _originalInstance.SnapWidthToIconWidth;
                _instance.SnapWidthToIconWidth_PlusScrollbarWidth = _originalInstance.SnapWidthToIconWidth_PlusScrollbarWidth;
                SnapWidthToIconWidth_PlusScrollbarWidth_CheckBox.Visibility = _instance.SnapWidthToIconWidth ? Visibility.Visible : Visibility.Collapsed;

                _instance.MaxGrayScaleStrength = _originalInstance.MaxGrayScaleStrength;
                _instance.GrayScaleEnabled = _originalInstance.GrayScaleEnabled;
                _instance.GrayScaleEnabled_InactiveOnly = _originalInstance.GrayScaleEnabled_InactiveOnly;

                _instance.LastAccesedToFirstRow = _originalInstance.LastAccesedToFirstRow;

                if (_originalInstance.Folder != _instance.Folder)
                {
                    _instance.Folder = _originalInstance.Folder;
                    _frame._currentFolderPath = _originalInstance.Folder;
                    string name = _instance.Name;

                    _frame.title.Text = Path.GetFileName(_frame._currentFolderPath);
                    _instance.Name = Path.GetFileName(_originalInstance.Name);

                    MainWindow._controller.WriteOverInstanceToKey(_instance, name);
                    _frame.LoadFiles(_frame._currentFolderPath);
                    _frame.InitializeFileWatcher();

                }


                _frame.TitleBarIconsFadeAnimation(!_instance.HideTitleBarIconsWhenInactive);
                AnimationSpeedSlider.Value = _originalInstance.AnimationSpeed * 4;
                AnimationSpeedLabel.Content = _originalInstance.AnimationSpeed == 0.0 ? "OFF" : "x" + _originalInstance.AnimationSpeed;
                IdleOpacitySlider.Value = _instance.IdleOpacity * 10;
                _frame.AnimateWindowOpacity(_instance.IdleOpacity, _instance.AnimationSpeed);
                IdleOpacityLabel.Content = _instance.IdleOpacity * 100 + "%";
                IconSizeSlider.Value = _instance.IconSize / 4;
                IconSizeLabel.Content = _instance.IconSize;

                PositionX_NumberBox.Value = _originalInstance.PosX;
                PositionY_NumberBox.Value = _originalInstance.PosY;

                _instance.Folder = _originalInstance.Folder;
                _instance.Name = _originalInstance.Name;
                _instance.TitleText = _originalInstance.TitleText;

                TitleBarColorTextBox.Text = _instance.TitleBarColor;
                TitleTextColorTextBox.Text = _instance.TitleTextColor;
                BorderColorTextBox.Text = _instance.BorderColor;
                ActiveBorderColorTextBox.Text = _instance.ActiveBorderColor;
                ActiveBackgroundColorTextBox.Text = _instance.ActiveBackgroundColor;
                ActiveTitleTextColorTextBox.Text = _instance.ActiveTitleTextColor;
                BorderEnabledCheckBox.IsChecked = _instance.BorderEnabled;
                ActiveBorderEnabledCheckBox.IsChecked = _instance.ActiveBorderEnabled;
                ActiveBackgroundEnabledCheckBox.IsChecked = _instance.ActiveBackgroundEnabled;
                ActiveTitleTextEnabledCheckBox.IsChecked = _instance.ActiveTitleTextEnabled;

                GrayScaleEnabled_CheckBox.IsChecked = _instance.GrayScaleEnabled;
                GrayScaleEnabled_InactiveOnly_CheckBox.IsChecked = _instance.GrayScaleEnabled_InactiveOnly;
                MaxGrayScaleStrengthSlider.Value = _instance.MaxGrayScaleStrength * 10;

                TitleTextBox.Text = _instance.TitleText ?? _instance.Name;
                FileFilterRegexTextBox.Text = _instance.FileFilterRegex;
                FileFilterHideRegexTextBox.Text = _instance.FileFilterHideRegex;

                TitleTextAlignmentComboBox.SelectedIndex = (int)_instance.TitleTextAlignment;
                ListViewBackgroundColorTextBox.Text = _instance.ListViewBackgroundColor;
                ListViewFontColorTextBox.Text = _instance.ListViewFontColor;
                ListViewFontShadowColorTextBox.Text = _instance.ListViewFontShadowColor;
                TitleFontSizeNumberBox.Value = _instance.TitleFontSize;
                TitleTextAutoSuggestionBox.Text = _instance.TitleFontFamily;
                ItemTextAutoSuggestionBox.Text = _instance.ItemFontFamily;

                AutoExpandonCursorCheckBox.IsChecked = _instance.AutoExpandonCursor;
                ShowShortcutArrowCheckBox.IsChecked = _instance.ShowShortcutArrow;
                FolderOpenInsideFrameCheckBox.IsChecked = _instance.FolderOpenInsideFrame;
                CheckFolderSizeCheckBox.IsChecked = _instance.CheckFolderSize;

                HideTitleBarIconsWhenInactive_CheckBox.IsChecked = _instance.HideTitleBarIconsWhenInactive;

                SnapWidthToIconWidth_PlusScrollbarWidth_CheckBox.IsChecked = _instance.SnapWidthToIconWidth_PlusScrollbarWidth;
                SnapWidthToIconWidth_CheckBox.IsChecked = _instance.SnapWidthToIconWidth;
                SnapWidthToIconWidth_PlusScrollbarWidth_CheckBox.Visibility = _instance.SnapWidthToIconWidth ? Visibility.Visible : Visibility.Collapsed;

                ShowLastAccessedToFirstRowCheckBox.IsChecked = _instance.LastAccesedToFirstRow;


                ShowOnVirtualDesktopTextBox.Text = _instance.ShowOnVirtualDesktops != null
                      ? string.Join(",", _instance.ShowOnVirtualDesktops)
                      : string.Empty;
                if (_instance.ShowOnVirtualDesktops != null && !_instance.ShowOnVirtualDesktops.Contains(Array.IndexOf(VirtualDesktop.GetDesktops(), VirtualDesktop.Current) + 1))
                {
                    _frame.Hide();
                }
                else
                {
                    _frame.Show();
                    this.Activate();
                }
                _isReverting = false;
                ValidateSettings();
            }

        }
        private void OpenColorPicker(System.Windows.Controls.TextBox textbox)
        {
            ColorCard.Children.Clear();
            var colorPicker = new ColorPicker.ColorPicker(textbox);
            ColorCard.Children.Add(colorPicker);
            uiFlyout.IsOpen = true;
        }

        private void BorderColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (BorderEnabledCheckBox.IsChecked == false) return;
            OpenColorPicker(BorderColorTextBox);
        }

        private void ActiveBorderColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveBorderEnabledCheckBox.IsChecked == false) return;
            OpenColorPicker(ActiveBorderColorTextBox);
        }
        private void ActiveBackgroundColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveBackgroundEnabledCheckBox.IsChecked == false) return;
            OpenColorPicker(ActiveBackgroundColorTextBox);
        }
        private void ActiveTitleTextColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveTitleTextEnabledCheckBox.IsChecked == false) return;
            OpenColorPicker(ActiveTitleTextColorTextBox);
        }
        private void FilesBackgroundColorButton_Click(object sender, RoutedEventArgs e)
        {
            ColorCard.Children.Clear();
            OpenColorPicker(ListViewBackgroundColorTextBox);
        }

        private void TitleTextColorButton_Click(object sender, RoutedEventArgs e)
        {
            OpenColorPicker(TitleTextColorTextBox);
        }

        private void TitleBarColorButton_Click(object sender, RoutedEventArgs e)
        {
            OpenColorPicker(TitleBarColorTextBox);
        }

        private void Titlebar_CloseClicked(TitleBar sender, RoutedEventArgs args)
        {
            this.DialogResult = true;
        }

        private void ShowFileExtensionIconCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _instance.ShowFileExtensionIcon = ShowFileExtensionIconCheckBox.IsChecked ?? false;
            _frame.UpdateIconVisibility();
        }

        private void ShowHiddenFilesIconCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _instance.ShowHiddenFilesIcon = ShowHiddenFilesIconCheckBox.IsChecked ?? false;
            _frame.UpdateIconVisibility();
        }

        private void ShowDisplayNameCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _instance.ShowDisplayName = ShowDisplayNameCheckBox.IsChecked ?? true;
            _frame.UpdateIconVisibility();
        }
        private void HideTitleBarIconsWhenInactive_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _instance.HideTitleBarIconsWhenInactive = HideTitleBarIconsWhenInactive_CheckBox.IsChecked ?? true;
            _frame.TitleBarIconsFadeAnimation(!_instance.HideTitleBarIconsWhenInactive);
        }
        private void SnapWidthToIconWidth_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _instance.SnapWidthToIconWidth = SnapWidthToIconWidth_CheckBox.IsChecked ?? true;
            SnapWidthToIconWidth_PlusScrollbarWidth_CheckBox.Visibility = _instance.SnapWidthToIconWidth ? Visibility.Visible : Visibility.Collapsed;
        }
        private void SnapWidthToIconWidth_PlusScrollbarWidth_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _instance.SnapWidthToIconWidth_PlusScrollbarWidth = SnapWidthToIconWidth_PlusScrollbarWidth_CheckBox.IsChecked ?? true;
        }
        private void AutoExpandonCursorCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _instance.AutoExpandonCursor = true;
        }
        private void GrayScaleEnabled_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _instance.GrayScaleEnabled = (bool)GrayScaleEnabled_CheckBox.IsChecked!;
            GrayScaleEnabled_InactiveOnly_CheckBox.Visibility =
                _instance.GrayScaleEnabled ? Visibility.Visible : Visibility.Collapsed;
            if (!_instance.GrayScaleEnabled)
            {
                _frame.AnimateGrayScale(_instance.MaxGrayScaleStrength, 0);
            }
            else if (_instance.GrayScaleEnabled != null)
            {
                _frame.AnimateGrayScale(0, _instance.MaxGrayScaleStrength);
            }
        }
        private void GrayScaleEnabled_InactiveOnly_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _instance.GrayScaleEnabled_InactiveOnly = (bool)GrayScaleEnabled_InactiveOnly_CheckBox.IsChecked!;
        }

        private void AutoExpandonCursorCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _instance.AutoExpandonCursor = false;
        }
        private void ShowShortcutArrowCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _instance.ShowShortcutArrow = true;
        }
        private void ShowShortcutArrowCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _instance.ShowShortcutArrow = false;
        }
        private void FolderOpenInsideFrameCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _instance.FolderOpenInsideFrame = true;
        }
        private void FolderOpenInsideFrameCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _instance.FolderOpenInsideFrame = false;
        }
        private void CheckFolderSizeCheckBox_Checkked(object sender, RoutedEventArgs e)
        {
            _instance.CheckFolderSize = true;
        }
        private void CheckFolderSizeCheckBox_Uncheckked(object sender, RoutedEventArgs e)
        {
            _instance.CheckFolderSize = false;
        }
        private void ShowLastAccessedToFirstRowCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _instance.LastAccesedToFirstRow = true;
            _frame.SortItems();
        }
        private void ShowLastAccessedToFirstRowCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _instance.LastAccesedToFirstRow = false;
            _frame.SortItems();
        }
        private void ChangeFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new FolderBrowserDialog
            {
                Description = "Select a folder",
                ShowNewFolderButton = true
            };
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _instance.Folder = folderDialog.SelectedPath;
                _frame._currentFolderPath = _instance.Folder;
                _instance.Name = Path.GetFileName(folderDialog.SelectedPath);
                MainWindow._controller.WriteOverInstanceToKey(_instance, _lastInstanceName);
                _lastInstanceName = _instance.Name;
                _frame.LoadFiles(_frame._currentFolderPath);
                _frame.title.Text = _instance.TitleText;
                _instance.TitleText = _instance.TitleText;
                TitleTextBox.Text = _instance.TitleText;
                // DataContext = this;
                _frame.InitializeFileWatcher();
                _frame.PathToBackButton.Visibility = Visibility.Collapsed;
            }
        }

        private void ListViewFontColorButton_Click(object sender, RoutedEventArgs e)
        {
            OpenColorPicker(ListViewFontColorTextBox);
        }

        private void ListViewFontShadowColorButton_Click(object sender, RoutedEventArgs e)
        {
            OpenColorPicker(ListViewFontShadowColorTextBox);
        }

        private void ListViewFontShadowColorTextBoxIcon_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenColorPicker(ListViewFontShadowColorTextBox);
        }

        private void ListViewFontColorTextBoxIcon_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenColorPicker(ListViewFontColorTextBox);
        }

        private void TitleTextColorTextBox_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenColorPicker(TitleTextColorTextBox);
        }

        private void TitleTextColorTextBoxIcon_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenColorPicker(TitleTextColorTextBox);
        }

        private void TitleBarColorTextBoxIcon_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenColorPicker(TitleBarColorTextBox);
        }

        private void ListViewBackgroundColorTextBoxIcon_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenColorPicker(ListViewBackgroundColorTextBox);
        }

        private void BorderColorTextBoxIcon_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenColorPicker(BorderColorTextBox);
        }

        private void ActiveBorderColorTextBoxIcon_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenColorPicker(ActiveBorderColorTextBox);
        }
        private void ActiveBackgroundColorTextBoxIcon_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenColorPicker(ActiveBackgroundColorTextBox);
        }
        private void ActiveTitleTextColorTextBoxIcon_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenColorPicker(ActiveTitleTextColorTextBox);
        }
        private void ChangeStyleDropDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow._controller.Instances.Count <= 1)
                return;

            ContextMenu contextMenu = new ContextMenu();
            foreach (var instance in MainWindow._controller.Instances)
            {
                if (instance.GetHashCode() == _instance.GetHashCode()) continue;
                var menuItem = new MenuItem
                {
                    Header = instance.TitleText ?? instance.Name,
                };
                menuItem.Click += (s, e) =>
                {
                    AnimationSpeedSlider.Value = instance.AnimationSpeed * 4;
                    AnimationSpeedLabel.Content = instance.AnimationSpeed == 0.0 ? "OFF" : "x" + instance.AnimationSpeed;
                    IdleOpacitySlider.Value = instance.IdleOpacity * 10;
                    IdleOpacityLabel.Content = instance.IdleOpacity * 100 + "%";
                    IconSizeSlider.Value = instance.IconSize / 4;
                    IconSizeLabel.Content = instance.IconSize;
                    _backgroundBrush = TitleBarColorTextBox.Background;
                    _borderBrush = TitleBarColorTextBox.BorderBrush;
                    TitleBarColorTextBox.Text = instance.TitleBarColor;
                    TitleTextColorTextBox.Text = instance.TitleTextColor;
                    ListViewBackgroundColorTextBox.Text = instance.ListViewBackgroundColor;
                    ListViewFontColorTextBox.Text = instance.ListViewFontColor;
                    ListViewFontShadowColorTextBox.Text = instance.ListViewFontShadowColor;
                    BorderColorTextBox.Text = instance.BorderColor;
                    ActiveBorderColorTextBox.Text = instance.ActiveBorderColor;
                    ActiveBackgroundColorTextBox.Text = instance.ActiveBackgroundColor;
                    ActiveTitleTextColorTextBox.Text = instance.ActiveTitleTextColor;
                    BorderEnabledCheckBox.IsChecked = instance.BorderEnabled;
                    ActiveBorderEnabledCheckBox.IsChecked = instance.ActiveBorderEnabled;
                    ActiveBackgroundEnabledCheckBox.IsChecked = instance.ActiveBackgroundEnabled;
                    ActiveTitleTextEnabledCheckBox.IsChecked = instance.ActiveTitleTextEnabled;
                    TitleFontSizeNumberBox.Value = instance.TitleFontSize;
                    TitleTextAutoSuggestionBox.Text = instance.TitleFontFamily;
                    ItemTextAutoSuggestionBox.Text = instance.ItemFontFamily;
                    TitleTextAlignmentComboBox.SelectedIndex = (int)instance.TitleTextAlignment;
                    AutoExpandonCursorCheckBox.IsChecked = instance.AutoExpandonCursor;
                    ShowShortcutArrowCheckBox.IsChecked = instance.ShowShortcutArrow;
                    FolderOpenInsideFrameCheckBox.IsChecked = instance.FolderOpenInsideFrame;
                    CheckFolderSizeCheckBox.IsChecked = instance.CheckFolderSize;
                    ShowLastAccessedToFirstRowCheckBox.IsChecked = instance.LastAccesedToFirstRow;

                };
                contextMenu.Items.Add(menuItem);
            }
            ChangeStyleDropDownButton.Flyout = contextMenu;
        }
        private async void ChangePosition(object sender, NumberBoxValueChangedEventArgs args)
        {
            await _frame.AdjustPositionAsync();
        }

        private void KofiButtonImage_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                ProcessStartInfo sInfo = new ProcessStartInfo($"https://ko-fi.com/J3J61PAH6H") { UseShellExecute = true };
                _ = Process.Start(sInfo);
            }
            catch
            {
            }
        }

        private void MaxGrayScaleStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _instance.MaxGrayScaleStrength = MaxGrayScaleStrengthSlider.Value / 10;
            MaxGrayScaleStrengthLabel.Content = (_instance.MaxGrayScaleStrength * 100).ToString("F0") + "%";
            if (_instance.GrayScaleEnabled)
            {
                _frame.AnimateGrayScale(e.OldValue / 10, e.NewValue / 10);
            }
            else if (!_instance.GrayScaleEnabled)
            {
                _frame.AnimateGrayScale(_instance.MaxGrayScaleStrength, 0);
            }
        }

    }
}