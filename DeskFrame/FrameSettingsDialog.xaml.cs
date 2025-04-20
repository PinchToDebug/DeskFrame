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
            DataContext = this;
            _originalInstance = new Instance(frame.Instance);
            _lastInstanceName = _originalInstance.Name;
            _instance = frame.Instance;
            _frame = frame;
            ShowOnVirtualDesktopTextBox.Text = _instance.ShowOnVirtualDesktops != null
                  ? string.Join(",", _instance.ShowOnVirtualDesktops)
                  : string.Empty;
            _originalInstance.ShowOnVirtualDesktops = _instance.ShowOnVirtualDesktops;
            TitleBarColorTextBox.Text = _instance.TitleBarColor;
            TitleTextColorTextBox.Text = _instance.TitleTextColor;
            ListViewBackgroundColorTextBox.Text = _instance.ListViewBackgroundColor;
            ListViewFontColorTextBox.Text = _instance.ListViewFontColor;
            ListViewFontShadowColorTextBox.Text = _instance.ListViewFontShadowColor;
            BorderColorTextBox.Text = _instance.BorderColor;
            BorderEnabledCheckBox.IsChecked = _instance.BorderEnabled;
            TitleTextBox.Text = _instance.TitleText ?? _instance.Name;
            TitleFontSizeNumberBox.Value = _instance.TitleFontSize;
            _originalInstance.TitleText = TitleTextBox.Text;
            FileFilterRegexTextBox.Text = _instance.FileFilterRegex;
            FileFilterHideRegexTextBox.Text = _instance.FileFilterHideRegex;
            TitleTextAlignmentComboBox.SelectedIndex = (int)_instance.TitleTextAlignment;
            ShowFileExtensionIconCheckBox.IsChecked = _instance.ShowFileExtensionIcon;
            ShowHiddenFilesIconCheckBox.IsChecked = _instance.ShowHiddenFilesIcon;
            ShowDisplayNameCheckBox.IsChecked = _instance.ShowDisplayName;
            TitleTextAutoSuggestionBox.Text = _instance.TitleFontFamily;
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

            UpdateBorderColorEnabled();
            ValidateSettings();

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
            TitleBarColorTextBoxIcon.Cursor = System.Windows.Input.Cursors.Hand;
            TitleTextColorTextBoxIcon.Cursor = System.Windows.Input.Cursors.Hand;
            ListViewBackgroundColorTextBoxIcon.Cursor = System.Windows.Input.Cursors.Hand;
            ListViewFontColorTextBoxIcon.Cursor = System.Windows.Input.Cursors.Hand;
            ListViewFontShadowColorTextBoxIcon.Cursor = System.Windows.Input.Cursors.Hand;
            BorderColorTextBoxIcon.Cursor = System.Windows.Input.Cursors.Hand;

            _initDone = true;
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
            UpdateBorderColorEnabled();
            ValidateSettings();
        }

        private void UpdateBorderColorEnabled() => BorderColorTextBox.IsEnabled = BorderEnabledCheckBox.IsChecked == true;
        private bool ValidateVirtualDesktop(string strValue)
        {
            return strValue
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .All(s => int.TryParse(s, out _));
        }
        private void ValidateSettings()
        {
            if (_isReverting) return;

            _isValidTitleBarColor = TryParseColor(string.IsNullOrEmpty(TitleBarColorTextBox.Text) ? "#0C000000" : TitleBarColorTextBox.Text, TitleBarColorTextBox);
            _isValidTitleTextColor = TryParseColor(string.IsNullOrEmpty(TitleTextColorTextBox.Text) ? "#FFFFFF" : TitleTextColorTextBox.Text, TitleTextColorTextBox);
            _isValidBorderColor = BorderEnabledCheckBox.IsChecked == true ? TryParseColor(string.IsNullOrEmpty(BorderColorTextBox.Text) ? "#FFFFFF" : BorderColorTextBox.Text, BorderColorTextBox) : true;
            _isValidFileFilterRegex = TryParseRegex(FileFilterRegexTextBox.Text, FileFilterRegexTextBox);
            _isValidFileFilterHideRegex = TryParseRegex(FileFilterHideRegexTextBox.Text, FileFilterHideRegexTextBox);

            _isValidTitleTextAlignment = TitleTextAlignmentComboBox.SelectedIndex >= 0;
            _isValidListViewBackgroundColor = TryParseColor(string.IsNullOrEmpty(ListViewBackgroundColorTextBox.Text) ? "#0C000000" : ListViewBackgroundColorTextBox.Text, ListViewBackgroundColorTextBox);
            _isValidListViewFontColor = TryParseColor(string.IsNullOrEmpty(ListViewFontColorTextBox.Text) ? "#FFFFFF" : ListViewFontColorTextBox.Text, ListViewFontColorTextBox);
            _isValidListViewFontShadowColor = TryParseColor(string.IsNullOrEmpty(ListViewFontShadowColorTextBox.Text) ? "#000000" : ListViewFontShadowColorTextBox.Text, ListViewFontShadowColorTextBox);

            _isValidShowOnVirtualDesktops = ValidateVirtualDesktop(ShowOnVirtualDesktopTextBox.Text);

            if (_isValidTitleBarColor && _isValidTitleTextColor && _isValidTitleTextAlignment &&
                _isValidBorderColor && _isValidFileFilterRegex && _isValidFileFilterHideRegex &&
                _isValidListViewBackgroundColor && _isValidListViewFontColor && _isValidListViewFontShadowColor &&
                _isValidShowOnVirtualDesktops)
            {
                _instance.TitleBarColor = string.IsNullOrEmpty(TitleBarColorTextBox.Text) ? "#0C000000" : TitleBarColorTextBox.Text;
                _instance.TitleTextColor = string.IsNullOrEmpty(TitleTextColorTextBox.Text) ? "#FFFFFF" : TitleTextColorTextBox.Text;

                _instance.BorderColor = BorderColorTextBox.Text;
                _instance.BorderEnabled = BorderEnabledCheckBox.IsChecked == true;
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
                _instance.BorderEnabled = _originalInstance.BorderEnabled;
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
                _instance.ShowOnVirtualDesktops = _originalInstance.ShowOnVirtualDesktops;
                if (_originalInstance.Folder != _instance.Folder)
                {
                    _instance.Folder = _originalInstance.Folder;
                    _frame._path = _originalInstance.Folder;
                    string name = _instance.Name;

                    _frame.title.Text = Path.GetFileName(_frame._path);
                    _instance.Name = Path.GetFileName(_originalInstance.Name);

                    MainWindow._controller.WriteOverInstanceToKey(_instance, name);
                    _frame.LoadFiles(_frame._path);
                    DataContext = this;
                    _frame.InitializeFileWatcher();

                }
                _instance.Folder = _originalInstance.Folder;
                _instance.Name = _originalInstance.Name;
                _instance.TitleText = _originalInstance.TitleText;

                TitleBarColorTextBox.Text = _instance.TitleBarColor;
                TitleTextColorTextBox.Text = _instance.TitleTextColor;
                BorderColorTextBox.Text = _instance.BorderColor;
                BorderEnabledCheckBox.IsChecked = _instance.BorderEnabled;
                TitleTextBox.Text = _instance.TitleText ?? _instance.Name;
                FileFilterRegexTextBox.Text = _instance.FileFilterRegex;
                FileFilterHideRegexTextBox.Text = _instance.FileFilterHideRegex;

                TitleTextAlignmentComboBox.SelectedIndex = (int)_instance.TitleTextAlignment;
                ListViewBackgroundColorTextBox.Text = _instance.ListViewBackgroundColor;
                ListViewFontColorTextBox.Text = _instance.ListViewFontColor;
                ListViewFontShadowColorTextBox.Text = _instance.ListViewFontShadowColor;
                TitleFontSizeNumberBox.Value = _instance.TitleFontSize;
                TitleTextAutoSuggestionBox.Text = _instance.TitleFontFamily;
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
                UpdateBorderColorEnabled();
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
                _frame._path = _instance.Folder;
                _frame.title.Text = Path.GetFileName(_frame._path);
                _instance.Name = Path.GetFileName(folderDialog.SelectedPath);
                MainWindow._controller.WriteOverInstanceToKey(_instance, _lastInstanceName);
                _lastInstanceName = _instance.Name;
                _frame.LoadFiles(_frame._path);
                DataContext = this;
                _frame.InitializeFileWatcher();
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
    }
}