#define smoothresize

using DeskFrame.Util;
using Microsoft.Win32;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using DataFormats = System.Windows.DataFormats;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MenuItem = Wpf.Ui.Controls.MenuItem;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Drawing.Point;
using TextBlock = Wpf.Ui.Controls.TextBlock;
using ListView = Wpf.Ui.Controls.ListView;
using ListViewItem = System.Windows.Controls.ListViewItem;
using WindowsDesktop;
using Microsoft.WindowsAPICodePack.Shell;
namespace DeskFrame
{
    public partial class DeskFrameWindow : System.Windows.Window
    {
        ShellContextMenu scm = new ShellContextMenu();
        public Instance Instance { get; set; }
        public string _path;
        private FileSystemWatcher _fileWatcher = new FileSystemWatcher();
        public ObservableCollection<FileItem> FileItems { get; set; }
        private bool _isMinimized = false;
        private int _snapDistance = 8;
        private int _currentVD;
        private bool _isBlack = true;
        private bool _checkForChages = false;
        private bool _canAutoClose = true;
        private bool _isLocked = false;
        private bool _isOnEdge = false;
        private double _originalHeight;
        private int _iconSize = 32; // Default icon size
        private const int MIN_ICON_SIZE = 16;
        private const int MAX_ICON_SIZE = 256;
        public int neighborFrameCount = 0;
        public bool isMouseDown = false;
        private ICollectionView _collectionView;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private CancellationTokenSource loadFilesCancellationToken = new CancellationTokenSource();
        DeskFrameWindow _wOnLeft = null;
        DeskFrameWindow _wOnRight = null;
        MenuItem nameMenuItem;
        MenuItem dateModifiedMenuItem;
        MenuItem dateCreatedMenuItem;
        MenuItem fileTypeMenuItem;
        MenuItem fileSizeMenuItem;
        MenuItem ascendingMenuItem;
        MenuItem descendingMenuItem;
        MenuItem folderOrderMenuItem;
        MenuItem folderFirstMenuItem;
        MenuItem folderLastMenuItem;
        MenuItem folderNoneMenuItem;

        private string _fileCount;
        private int _folderCount = 0;
        private DateTime _lastUpdated;
        private string _folderSize;

        public enum SortBy
        {
            NameAsc = 1,
            NameDesc = 2,
            DateModifiedAsc = 3,
            DateModifiedDesc = 4,
            DateCreatedAsc = 5,
            DateCreatedDesc = 6,
            FileTypeAsc = 7,
            FileTypeDesc = 8,
            ItemSizeAsc = 9,
            ItemSizeDesc = 10,
        }

        public static ObservableCollection<FileItem> SortFileItems(ObservableCollection<FileItem> fileItems, int sortBy, int folderOrder)
        {
            IEnumerable<FileItem> items = fileItems;

            var sortOptions = new Dictionary<int, Func<IEnumerable<FileItem>, IOrderedEnumerable<FileItem>>>
            {
                { (int)SortBy.NameAsc, x => x.OrderBy(i => string.IsNullOrEmpty(i.Name) ? string.Empty: Path.GetFileNameWithoutExtension(i.Name), StringComparer.OrdinalIgnoreCase)},
                { (int)SortBy.NameDesc, x => x.OrderByDescending(i => string.IsNullOrEmpty(i.Name) ? string.Empty: Path.GetFileNameWithoutExtension(i.Name), StringComparer.OrdinalIgnoreCase)},
                { (int)SortBy.DateModifiedAsc, x => x.OrderBy(i => i.DateModified) },
                { (int)SortBy.DateModifiedDesc, x => x.OrderByDescending(i => i.DateModified) },
                { (int)SortBy.DateCreatedAsc, x => x.OrderBy(i => i.DateCreated) },
                { (int)SortBy.DateCreatedDesc, x => x.OrderByDescending(i => i.DateCreated) },
                { (int)SortBy.FileTypeAsc, x => x.OrderBy(i => i.FileType) },
                { (int)SortBy.FileTypeDesc, x => x.OrderByDescending(i => i.FileType) },
                { (int)SortBy.ItemSizeAsc, x => x.OrderBy(i => i.ItemSize) },
                { (int)SortBy.ItemSizeDesc, x => x.OrderByDescending(i => i.ItemSize) },
            };

            if (sortOptions.TryGetValue(sortBy, out var sorter))
                items = sorter(items);

            if (folderOrder == 1)
                items = items.OrderBy(i => i.IsFolder);
            else if (folderOrder == 2)
                items = items.OrderBy(i => !i.IsFolder);

            return new ObservableCollection<FileItem>(items);
        }



        public async Task<List<FileSystemInfo>> SortFileItemsToList(List<FileSystemInfo> fileItems, int sortBy, int folderOrder)
        {
            var fileItemSizes = new List<(FileSystemInfo item, long size)>();

            foreach (var item in fileItems)
            {
                long size = await GetItemSizeAsync(item);
                fileItemSizes.Add((item, size));
            }

            var sortOptions = new Dictionary<int, Func<List<(FileSystemInfo item, long size)>, IOrderedEnumerable<(FileSystemInfo item, long size)>>>
                {
                    { (int)SortBy.NameAsc, x => x.OrderBy(i => string.IsNullOrEmpty(i.item.Name) ? string.Empty : Path.GetFileNameWithoutExtension(i.item.Name), StringComparer.OrdinalIgnoreCase) },
                    { (int)SortBy.NameDesc, x => x.OrderByDescending(i => string.IsNullOrEmpty(i.item.Name) ? string.Empty : Path.GetFileNameWithoutExtension(i.item.Name), StringComparer.OrdinalIgnoreCase) },
                    { (int)SortBy.DateModifiedAsc, x => x.OrderBy(i => i.item.LastWriteTime) },
                    { (int)SortBy.DateModifiedDesc, x => x.OrderByDescending(i => i.item.LastWriteTime) },
                    { (int)SortBy.DateCreatedAsc, x => x.OrderBy(i => i.item.CreationTime) },
                    { (int)SortBy.DateCreatedDesc, x => x.OrderByDescending(i => i.item.CreationTime) },
                    { (int)SortBy.FileTypeAsc, x => x.OrderBy(i => i.item.Extension) },
                    { (int)SortBy.FileTypeDesc, x => x.OrderByDescending(i => i.item.Extension) },
                    { (int)SortBy.ItemSizeAsc, x => x.OrderBy(i => i.size) },
                    { (int)SortBy.ItemSizeDesc, x => x.OrderByDescending(i => i.size) },
                };

            var sortedItems = sortOptions.TryGetValue(sortBy, out var sorter)
                ? sorter(fileItemSizes).ToList()
                : fileItemSizes.ToList();

            if (folderOrder == 1)
                sortedItems = sortedItems.OrderBy(i => i.item is FileInfo).ToList();
            else if (folderOrder == 2)
                sortedItems = sortedItems.OrderBy(i => i.item is DirectoryInfo).ToList();

            return sortedItems.Select(x => x.item).ToList();
        }

        private async Task<long> GetItemSizeAsync(FileSystemInfo entry, CancellationToken token = default)
        {
            if (entry is FileInfo fileInfo)
            {
                return fileInfo.Length;
            }
            else if (entry is DirectoryInfo directoryInfo && Instance.CheckFolderSize)
            {
                return await Task.Run(() => GetDirectorySize(directoryInfo, token), token);
            }

            return 0;
        }
        private long GetDirectorySize(DirectoryInfo directory, CancellationToken token)
        {
            long size = 0;

            try
            {
                foreach (var file in directory.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                {
                    token.ThrowIfCancellationRequested();
                    size += file.Length;
                }

                Parallel.ForEach(directory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly), (subDir) =>
                {
                    token.ThrowIfCancellationRequested();
                    Interlocked.Add(ref size, GetDirectorySize(subDir, token));
                });
            }
            catch
            {
            }

            return size;
        }



        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (!(HwndSource.FromHwnd(hWnd).RootVisual is System.Windows.Window rootVisual))
                return IntPtr.Zero;
            if (msg == 0x0214) // WM_SIZING
            {
                Interop.RECT rect = (Interop.RECT)Marshal.PtrToStructure(lParam, typeof(Interop.RECT));
                double width = rect.Right - rect.Left;

                #if (smoothresize)
                  double newWidth = width;
                  if (true)
                #else
                  double newWidth = (Math.Round(width / 85.0) * 85 + 13);
                  if (width != newWidth)
                #endif
                {
                    if (wParam.ToInt32() == 2) // WMSZ_RIGHT
                    {
                        rect.Right = rect.Left + (int)newWidth;
                    }
                    else
                    {
                        rect.Left = rect.Right - (int)newWidth;
                    }
                    Marshal.StructureToPtr(rect, lParam, true);
                    Instance.Width = this.Width;
                }
                double height = rect.Bottom - rect.Top;
                if (height <= 102)
                {
                    this.Height = 102;
                    rect.Bottom = rect.Top + 102;
                    Marshal.StructureToPtr(rect, lParam, true);
                    handled = true;
                    return (IntPtr)4;
                }
                ResizeBottomAnimation(height, rect, lParam);

            }

            if (msg == 70)
            {
                Interop.WINDOWPOS structure = Marshal.PtrToStructure<Interop.WINDOWPOS>(lParam);
                structure.flags |= 4U;
                Marshal.StructureToPtr<Interop.WINDOWPOS>(structure, lParam, false);
            }
            if (msg == 0x0003) // WM_MOVE
            {
                HandleWindowMove();

                if (_wOnLeft != null)
                {
                    _wOnLeft.HandleWindowMove();
                }
                if (_wOnRight != null)
                {
                    _wOnRight.HandleWindowMove();
                }
            }

            return IntPtr.Zero;
        }
        private void ResizeBottomAnimation(double targetBottom, Interop.RECT rect, IntPtr lParam)
        {
            if (!_canAnimate) return;
            var animation = new DoubleAnimation
            {
                To = targetBottom,
                Duration = TimeSpan.FromMilliseconds(10),
                FillBehavior = FillBehavior.Stop
            };

            animation.Completed += (s, e) =>
            {
                _canAnimate = true;
                Marshal.StructureToPtr(rect, lParam, true);
            };
            _canAnimate = false;
            this.BeginAnimation(HeightProperty, animation);
        }

        public void HandleWindowMove()
        {
            Interop.RECT windowRect;
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            Interop.GetWindowRect(hwnd, out windowRect);

            int windowLeft = windowRect.Left;
            int windowTop = windowRect.Top;
            int windowRight = windowRect.Right;
            int windowBottom = windowRect.Bottom;

            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null) return;

            int newWindowLeft = windowLeft;
            int newWindowTop = windowTop;

            var workingArea = SystemParameters.WorkArea;
            //if (Math.Abs(windowLeft - workingArea.Left) <= _snapDistance)
            //{
            //    newWindowLeft = (int)workingArea.Left;
            //    _isOnEdge = true;
            //}
            //else if (Math.Abs(windowRight - workingArea.Right) <= _snapDistance)
            //{
            //    newWindowLeft = (int)(workingArea.Right - (windowRight - windowLeft));
            //    _isOnEdge = true;
            //}
            //else
            //{
            //    _isOnEdge = false;
            //}

            if (Math.Abs(windowTop - workingArea.Top) <= _snapDistance)
            {
                newWindowTop = (int)workingArea.Top;
                WindowBackground.CornerRadius = new CornerRadius(0, 0, 5, 5);
                _isOnEdge = true;
            }
            else if (Math.Abs(windowBottom - workingArea.Bottom) <= _snapDistance)
            {
                newWindowTop = (int)(workingArea.Bottom - (windowBottom - windowTop));
            }
            else
            {
                _isOnEdge = false;
                WindowBackground.CornerRadius = new CornerRadius(5);
                titleBar.CornerRadius = new CornerRadius(5, 5, 0, 0);
            }
            neighborFrameCount = 0;

            bool onLeft = false;
            bool onRight = false;
            foreach (var otherWindow in MainWindow._controller._subWindows)
            {
                if (otherWindow == this) continue;

                IntPtr otherHwnd = new WindowInteropHelper(otherWindow).Handle;
                Interop.RECT otherWindowRect;
                Interop.GetWindowRect(otherHwnd, out otherWindowRect);

                int otherLeft = otherWindowRect.Left;
                int otherTop = otherWindowRect.Top;
                int otherRight = otherWindowRect.Right;
                int otherBottom = otherWindowRect.Bottom;

                if (Math.Abs(windowLeft - otherRight) <= _snapDistance && Math.Abs(windowTop - otherTop) <= _snapDistance)
                {
                    newWindowLeft = otherRight;
                    newWindowTop = otherTop;
                    _wOnLeft = otherWindow;
                    onLeft = true;
                    neighborFrameCount++;
                }
                else if (Math.Abs(windowRight - otherLeft) <= _snapDistance && Math.Abs(windowTop - otherTop) <= _snapDistance)
                {
                    newWindowLeft = otherLeft - (windowRight - windowLeft);
                    newWindowTop = otherTop;
                    _wOnRight = otherWindow;
                    onRight = true;
                    neighborFrameCount++;
                }


                if (Math.Abs(windowTop - otherBottom) <= _snapDistance && Math.Abs(windowLeft - otherLeft) <= _snapDistance)
                {
                    newWindowTop = otherBottom;
                }
                else if (Math.Abs(windowBottom - otherTop) <= _snapDistance && Math.Abs(windowLeft - otherLeft) <= _snapDistance)
                {
                    newWindowTop = otherTop - (windowBottom - windowTop);
                }
                if (neighborFrameCount == 2) break;
            }
            if (neighborFrameCount == 2)
            {
                WindowBackground.CornerRadius = new CornerRadius(0);
                titleBar.CornerRadius = new CornerRadius(0);
            }
            if (neighborFrameCount == 0)
            {
                if (_wOnLeft != null && !onLeft)
                {
                    if (!_wOnLeft._isMinimized)
                    {
                        _wOnLeft.WindowBorder.CornerRadius = new CornerRadius(
                            topLeft: _wOnLeft._isOnEdge ? 0 : _wOnLeft._wOnLeft == null ? 5 : 0,
                            topRight: _wOnLeft._isOnEdge ? 0 : 5,
                            bottomRight: 5,
                            bottomLeft: 5
                        );
                        _wOnLeft.titleBar.CornerRadius = new CornerRadius(
                            topLeft: _wOnLeft.WindowBorder.CornerRadius.TopLeft,
                            topRight: _wOnLeft.WindowBorder.CornerRadius.TopRight,
                            bottomRight: 0,
                            bottomLeft: 0
                        );
                    }
                    else
                    {
                        _wOnLeft.WindowBorder.CornerRadius = new CornerRadius(
                            topLeft: _wOnLeft._isOnEdge ? 0 : _wOnLeft._wOnLeft == null ? 5 : 0,
                            topRight: _wOnLeft._isOnEdge ? 0 : 5,
                            bottomRight: 5,
                            bottomLeft: _wOnLeft._wOnLeft == null ? 5 : 0
                        );
                        _wOnLeft.titleBar.CornerRadius = _wOnLeft.WindowBorder.CornerRadius;

                    }
                    _wOnLeft.WindowBackground.CornerRadius = _wOnLeft.WindowBorder.CornerRadius;
                    _wOnLeft._wOnRight = null;
                    _wOnLeft = null;
                }
                if (_wOnRight != null && !onRight)
                {
                    if (!_wOnRight._isMinimized)
                    {
                        _wOnRight.WindowBorder.CornerRadius = new CornerRadius(
                            topLeft: _wOnRight._isOnEdge ? 0 : 5,
                            topRight: _wOnRight._isOnEdge ? 0 : _wOnRight._wOnRight == null ? 5 : 0,
                            bottomRight: 5,
                            bottomLeft: 5
                        );
                        _wOnRight.titleBar.CornerRadius = new CornerRadius(
                            topLeft: _wOnRight.WindowBorder.CornerRadius.TopLeft,
                            topRight: _wOnRight.WindowBorder.CornerRadius.TopRight,
                            bottomRight: 0,
                            bottomLeft: 0
                        );
                    }
                    else
                    {
                        _wOnRight.WindowBorder.CornerRadius = new CornerRadius(
                        topLeft: _wOnRight._isOnEdge ? 0 : 5,
                        topRight: _wOnRight._isOnEdge ? 0 : _wOnRight._wOnRight == null ? 5 : 0,
                        bottomRight: _wOnRight._wOnRight == null ? 5 : 0,
                        bottomLeft: 5
                        );
                        _wOnRight.titleBar.CornerRadius = _wOnRight.WindowBorder.CornerRadius;

                    }
                    _wOnRight.WindowBackground.CornerRadius = _wOnRight.WindowBorder.CornerRadius;
                    _wOnRight._wOnLeft = null;
                    _wOnRight = null;
                }

            }
            if (!_isMinimized)
            {
                WindowBorder.CornerRadius = new CornerRadius(
                    topLeft: _isOnEdge ? 0 : _wOnLeft == null ? 5 : 0,
                    topRight: _isOnEdge ? 0 : _wOnRight == null ? 5 : 0,
                    bottomRight: 5,
                    bottomLeft: 5
                );
                WindowBackground.CornerRadius = WindowBorder.CornerRadius;
                titleBar.CornerRadius = new CornerRadius(
                    topLeft: WindowBorder.CornerRadius.TopLeft,
                    topRight: WindowBorder.CornerRadius.TopRight,
                    bottomRight: 0,
                    bottomLeft: 0
                );
            }
            else
            {
                WindowBorder.CornerRadius = new CornerRadius(
                    topLeft: _isOnEdge ? 0 : _wOnLeft == null ? 5 : 0,
                    topRight: _isOnEdge ? 0 : _wOnRight == null ? 5 : 0,
                    bottomRight: _wOnRight == null ? 5 : 0,
                    bottomLeft: _wOnLeft == null ? 5 : 0
                );
                WindowBackground.CornerRadius = WindowBorder.CornerRadius;
                titleBar.CornerRadius = WindowBorder.CornerRadius;
            }



            if (newWindowLeft != windowLeft || newWindowTop != windowTop)
            {
                Interop.SetWindowPos(hwnd, IntPtr.Zero, newWindowLeft, newWindowTop, 0, 0, Interop.SWP_NOREDRAW | Interop.SWP_NOACTIVATE | Interop.SWP_NOZORDER | Interop.SWP_NOSIZE);
            }
        }

        public void SetCornerRadius(Border border, double topLeft, double topRight, double bottomLeft, double bottomRight)
        {
            border.CornerRadius = new CornerRadius(topLeft, topRight, bottomLeft, bottomRight);
        }

        private void SetAsDesktopChild()
        {
            ArrayList windowHandles = new ArrayList();
            Interop.EnumedWindow callback = Interop.EnumWindowCallback;
            Interop.EnumWindows(callback, windowHandles);

            foreach (IntPtr windowHandle in windowHandles)
            {
                IntPtr progmanHandle = Interop.FindWindowEx(windowHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (progmanHandle != IntPtr.Zero)
                {
                    var interopHelper = new WindowInteropHelper(this);
                    interopHelper.EnsureHandle();
                    interopHelper.Owner = progmanHandle;
                    break;
                }
            }
        }
        public void SetAsToolWindow()
        {
            WindowInteropHelper wih = new WindowInteropHelper(this);
            IntPtr dwNew = new IntPtr(((long)Interop.GetWindowLong(wih.Handle, Interop.GWL_EXSTYLE).ToInt32() | 128L | 0x00200000L) & 4294705151L);
            Interop.SetWindowLong((nint)new HandleRef(this, wih.Handle), Interop.GWL_EXSTYLE, dwNew);
        }
        public void SetNoActivate()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            IntPtr style = Interop.GetWindowLong(hwnd, Interop.GWL_EXSTYLE);
            IntPtr newStyle = new IntPtr(style.ToInt64() | Interop.WS_EX_NOACTIVATE);
            Interop.SetWindowLong(hwnd, Interop.GWL_EXSTYLE, newStyle);
        }
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                FilterTextBox.Text = null;
            }
            FilterTextBox.Focus();
            return;
        }

        private async void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(FilterTextBox.Text))
            {
                Search.Visibility = Visibility.Collapsed;
                title.Visibility = Visibility.Visible;
            }
            else
            {
                Search.Visibility = Visibility.Visible;
                title.Visibility = Visibility.Hidden;
            }

            searchQuery.Content = FilterTextBox.Text;

            if (_collectionView == null)
                return;


            string filter = FilterTextBox.Text;
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                await Task.Delay(50, token);
                foreach (var fileItem in FileItems)
                {
                    fileItem.IsSelected = false;
                    fileItem.Background = Brushes.Transparent;
                }
                string regexPattern = Regex.Escape(filter).Replace("\\*", ".*"); // Escape other regex special chars and replace '*' with '.*'

                var filteredItems = await Task.Run(() =>
                {
                    return new Predicate<object>(item =>
                    {
                        if (token.IsCancellationRequested) return false;
                        var fileItem = item as FileItem;
                        return string.IsNullOrWhiteSpace(filter) ||
                               Regex.IsMatch(fileItem.Name!, regexPattern, RegexOptions.IgnoreCase);
                    });
                }, token);

                if (!token.IsCancellationRequested)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        _collectionView.Filter = filteredItems;
                        _collectionView.Refresh();
                    });
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = (int)Interop.GetWindowLong(hwnd, Interop.GWL_EXSTYLE);
            Interop.SetWindowLong(hwnd, Interop.GWL_EXSTYLE, exStyle | Interop.WS_EX_NOACTIVATE);
            KeepWindowBehind();
            SetAsDesktopChild();
            SetNoActivate();
            SetAsToolWindow();
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(WndProc);
        }
        public DeskFrameWindow(Instance instance)
        {
            InitializeComponent();
            this.MinWidth = 98;
            this.Loaded += MainWindow_Loaded;
            this.SourceInitialized += MainWindow_SourceInitialized!;
            this.Closing += Window_Closing;

            this.StateChanged += (sender, args) =>
            {
                this.WindowState = WindowState.Normal;
            };

            Instance = instance;
            this.Width = instance.Width;
            _path = instance.Folder;
            _isLocked = instance.IsLocked;
            this.Top = instance.PosY;
            this.Left = instance.PosX;

            title.FontSize = Instance.TitleFontSize;
            title.TextWrapping = TextWrapping.Wrap;

            double titleBarHeight = Math.Max(30, Instance.TitleFontSize * 1.5);
            titleBar.Height = titleBarHeight;

            double scrollViewerMargin = titleBarHeight + 5;
            scrollViewer.Margin = new Thickness(0, scrollViewerMargin, 0, 0);

            titleBar.Cursor = _isLocked ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.SizeAll;
            if ((int)instance.Height <= 30) _isMinimized = true;
            if (instance.Minimized)
            {
                _isMinimized = instance.Minimized;
                this.Height = titleBarHeight;
            }
            else
            {
                this.Height = instance.Height;
            }

            _checkForChages = true;
            FileItems = new ObservableCollection<FileItem>();
            if (instance.Folder == "empty")
            {
                showFolder.Visibility = Visibility.Hidden;
                addFolder.Visibility = Visibility.Visible;
            }
            else
            {
                LoadFiles(instance.Folder);
                title.Text = Instance.TitleText ?? Instance.Name;

                DataContext = this;
                InitializeFileWatcher();
            }
            _collectionView = CollectionViewSource.GetDefaultView(FileItems);
            _originalHeight = this.Height;
            titleBar.Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(Instance.TitleBarColor));
            title.Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(Instance.TitleTextColor));
            if (Instance.TitleFontFamily != null)
            {
                try
                {
                    title.FontFamily = new System.Windows.Media.FontFamily(Instance.TitleFontFamily);
                }
                catch
                {
                }
            }
            if (Instance.ShowInGrid)
            {
                showFolder.Visibility = Visibility.Visible;
                showFolderInGrid.Visibility = Visibility.Hidden;
            }
            else
            {
                showFolder.Visibility = Visibility.Hidden;
                showFolderInGrid.Visibility = Visibility.Visible;
            }
            ChangeBackgroundOpacity(Instance.Opacity);
        }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                KeepWindowBehind();
                if (!_isLocked)
                {
                    this.DragMove();
                }
                Debug.WriteLine("win left hide");
                return;
            }
        }


        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged && !_isMinimized)
            {
                if (this.ActualHeight != 30)
                {
                    Instance.Height = this.ActualHeight;
                }
            }
        }
        private void AnimateChevron(bool flip, bool onLoad)
        {


            var rotateTransform = ChevronRotate;

            int angleToAnimateTo;
            int duration;
            if (onLoad)
            {
                angleToAnimateTo = flip ? 0 : 180;
                duration = 10;
            }
            else
            {
                angleToAnimateTo = (rotateTransform.Angle == 180) ? 0 : 180;
                duration = 200;
            }
            if (_isLocked) duration = 100;

            var rotateAnimation = new DoubleAnimation
            {
                From = rotateTransform.Angle,
                To = angleToAnimateTo,
                Duration = new Duration(TimeSpan.FromMilliseconds(duration)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            _canAnimate = false;
            rotateAnimation.Completed += (s, e) => _canAnimate = true;

            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
        }

        bool _canAnimate = true;
        private void Minimize_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            AnimateChevron(_isMinimized, false);
            if (showFolder.Visibility == Visibility.Hidden && showFolderInGrid.Visibility == Visibility.Hidden)
            {
                return;
            }
            if (!_isMinimized)
            {
                _originalHeight = this.ActualHeight;
                _isMinimized = true;
                Instance.Minimized = true;
                Debug.WriteLine("minimize: " + Instance.Height);
                AnimateWindowHeight(titleBar.Height);
            }
            else
            {
                WindowBackground.CornerRadius = new CornerRadius(
                         topLeft: WindowBackground.CornerRadius.TopLeft,
                         topRight: WindowBackground.CornerRadius.TopRight,
                         bottomRight: 5.0,
                         bottomLeft: 5.0
                      );
                _isMinimized = false;
                Instance.Minimized = false;

                Debug.WriteLine("unminimize: " + Instance.Height);
                AnimateWindowHeight(Instance.Height);
            }
            HandleWindowMove();
        }

        private void ToggleFileExtension_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ToggleFileExtension();
            LoadFiles(_path);
            UpdateFileExtensionIcon();
        }

        private void ToggleHiddenFiles_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ToggleHiddenFiles();
            LoadFiles(_path);
            UpdateHiddenFilesIcon();
        }
        private void OpenFolder()
        {
            try
            {
                Process.Start(new ProcessStartInfo(_path) { UseShellExecute = true });
            }
            catch
            { }
        }
        private void UpdateFileExtensionIcon()
        {
            if (Instance.ShowFileExtension)
            {
                FileExtensionIcon.Symbol = SymbolRegular.DocumentSplitHint24;
            }
            else
            {
                FileExtensionIcon.Symbol = SymbolRegular.DocumentSplitHintOff24;
            }
        }

        private void UpdateHiddenFilesIcon()
        {
            if (Instance.ShowHiddenFiles)
            {
                HiddenFilesIcon.Symbol = SymbolRegular.Eye24;
            }
            else
            {
                HiddenFilesIcon.Symbol = SymbolRegular.EyeOff24;
            }
        }

        private void AnimateWindowHeight(double targetHeight)
        {
            var animation = new DoubleAnimation
            {
                To = targetHeight,
                Duration = (_isLocked) ? TimeSpan.FromSeconds(0.1) : TimeSpan.FromSeconds(0.2),
                EasingFunction = new QuadraticEase()
            };
            animation.Completed += (s, e) =>
            {
                _canAnimate = true;
                if (targetHeight == 30)
                {
                    scrollViewer.ScrollToTop();
                }
                WindowChrome.SetWindowChrome(this, Instance.IsLocked ?
                    new WindowChrome
                    {
                        ResizeBorderThickness = new Thickness(0),
                        CaptionHeight = 0
                    } :
                    new WindowChrome
                    {
                        GlassFrameThickness = new Thickness(5),
                        CaptionHeight = 0,
                        ResizeBorderThickness = new Thickness(5, 0, 5, Instance.Minimized ? 0 : 5),
                        CornerRadius = new CornerRadius(5)
                    }
                );
            };
            _canAnimate = false;
            this.BeginAnimation(HeightProperty, animation);
        }

        public void InitializeFileWatcher()
        {
            _fileWatcher = null;
            _fileWatcher = new FileSystemWatcher(_path)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };
            _fileWatcher.Created += OnFileChanged;
            _fileWatcher.Deleted += OnFileChanged;
            _fileWatcher.Renamed += OnFileRenamed;
            _fileWatcher.Changed += OnFileChanged;
        }
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"File changed: {e.ChangeType} - {e.FullPath}");
                LoadFiles(_path);
            });
        }
        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"File renamed: {e.OldFullPath} to {e.FullPath}");
                var renamedItem = FileItems.FirstOrDefault(item => item.Name == Path.GetFileName(e.OldFullPath));

                if (renamedItem != null)
                {
                    renamedItem.Name = Path.GetFileName(e.FullPath);
                }
                SortItems();
            });
        }
        private void KeepWindowBehind()
        {
            IntPtr HWND_BOTTOM = new IntPtr(1);
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            Interop.SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, Interop.SWP_NOREDRAW | Interop.SWP_NOACTIVATE | Interop.SWP_NOMOVE | Interop.SWP_NOSIZE);
        }
        //public void KeepWindowBehind()
        //{
        //    bool keepOnBottom = this._keepOnBottom;
        //    this._keepOnBottom = false;
        //    Interop.SetWindowPos(new WindowInteropHelper(this).Handle, 1, 0, 0, 0, 0, 19U);
        //    this._keepOnBottom = keepOnBottom;
        //}

        private void ToggleHiddenFiles() => Instance.ShowHiddenFiles = !Instance.ShowHiddenFiles;
        private void ToggleIsLocked() => Instance.IsLocked = !Instance.IsLocked;
        private void ToggleFileExtension() => Instance.ShowFileExtension = !Instance.ShowFileExtension;

        public async void LoadFiles(string path)
        {
            loadFilesCancellationToken.Cancel();
            loadFilesCancellationToken.Dispose();
            loadFilesCancellationToken = new CancellationTokenSource();
            CancellationToken loadFiles_cts = loadFilesCancellationToken.Token;
            try
            {
                if (!Directory.Exists(path))
                {
                    return;
                }

                _iconSize = Instance.IconSize;                

                var fileEntries = await Task.Run(() =>
                {
                    if (loadFiles_cts.IsCancellationRequested)
                    {
                        return new List<FileSystemInfo>();
                    }
                    var dirInfo = new DirectoryInfo(path);
                    var files = dirInfo.GetFiles();
                    var directories = dirInfo.GetDirectories();
                    _folderCount = directories.Count();
                    _fileCount = dirInfo.GetFiles().Count().ToString();
                    _folderSize = !Instance.CheckFolderSize ? "" : Task.Run(() => BytesToStringAsync(dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length))).Result; var filteredFiles = files.Cast<FileSystemInfo>()
                                .Concat(directories)
                                .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                                .ToList();
                    if (!Instance.ShowHiddenFiles)
                        filteredFiles = filteredFiles.Where(entry => !entry.Attributes.HasFlag(FileAttributes.Hidden)).ToList();
                    if (Instance.FileFilterRegex != null)
                    {
                        var regex = new Regex(Instance.FileFilterRegex);
                        filteredFiles = filteredFiles.Where(entry => regex.IsMatch(entry.Name)).ToList();
                    }
                    return filteredFiles;
                }, loadFiles_cts);

                if (loadFiles_cts.IsCancellationRequested)
                {
                    return;
                }

                fileEntries = await SortFileItemsToList(fileEntries, (int)Instance.SortBy, Instance.FolderOrder);
                if (Instance.FolderOrder == 1)
                {
                    fileEntries = fileEntries.OrderBy(x => x is FileInfo).ToList();
                }
                else if (Instance.FolderOrder == 2)
                {
                    fileEntries = fileEntries.OrderByDescending(x => x is FileInfo).ToList();
                }
                var fileNames = new HashSet<string>(fileEntries.Select(f => f.Name));


                await Dispatcher.InvokeAsync(async () =>
                {
                    if (loadFiles_cts.IsCancellationRequested)
                    {
                        return;
                    }
                    for (int i = FileItems.Count - 1; i >= 0; i--)  // Remove item that no longer exist
                    {
                        if (loadFiles_cts.IsCancellationRequested)
                        {
                            return;
                        }
                        if (!fileNames.Contains(Path.GetFileName(FileItems[i].FullPath!)))
                        {
                            FileItems.RemoveAt(i);
                        }
                    }

                    foreach (var entry in fileEntries)
                    {
                        if (loadFiles_cts.IsCancellationRequested)
                            return;

                        var existingItem = FileItems.FirstOrDefault(item => item.FullPath == entry.FullName);

                        long size = 0;
                        if (entry is FileInfo fileInfo)
                            size = fileInfo.Length;
                        else if (entry is DirectoryInfo directoryInfo && Instance.CheckFolderSize)
                            size = await Task.Run(() => GetDirectorySize(directoryInfo, loadFiles_cts));
                        size = size > int.MaxValue ? int.MaxValue : size;

                        string displaySize = entry is FileInfo ? await BytesToStringAsync(size)
                                                               : Instance.CheckFolderSize ? await BytesToStringAsync(size)
                                                                                          : "";
                        var thumbnail = await GetThumbnailAsync(entry.FullName);

                        if (existingItem == null)
                        {
                            if (!string.IsNullOrEmpty(Instance.FileFilterHideRegex) &&
                                new Regex(Instance.FileFilterHideRegex).IsMatch(entry.Name))
                            {
                                continue;
                            }

                            FileItems.Add(new FileItem
                            {
                                Name = Instance.ShowFileExtension
                                ? entry.Name
                                : (!string.IsNullOrEmpty(Path.GetFileNameWithoutExtension(entry.FullName))
                                    ? Path.GetFileNameWithoutExtension(entry.FullName)
                                    : entry.Name),
                                FullPath = entry.FullName,
                                IsFolder = entry is FileInfo,
                                DateModified = entry.LastWriteTime,
                                DateCreated = entry.CreationTime,
                                FileType = entry is FileInfo ? entry.Extension : string.Empty,
                                ItemSize = (int)size,
                                DisplaySize = displaySize,
                                Thumbnail = thumbnail
                            });
                        }
                        else
                        {
                            existingItem.Name = Instance.ShowFileExtension
                                ? entry.Name
                                : (!string.IsNullOrEmpty(Path.GetFileNameWithoutExtension(entry.FullName))
                                    ? Path.GetFileNameWithoutExtension(entry.FullName)
                                    : entry.Name);
                            existingItem.FullPath = entry.FullName;
                            existingItem.IsFolder = entry is FileInfo;
                            existingItem.DateModified = entry.LastWriteTime;
                            existingItem.DateCreated = entry.CreationTime;
                            existingItem.FileType = entry is FileInfo ? entry.Extension : string.Empty;
                            existingItem.ItemSize = (int)size;
                            existingItem.DisplaySize = displaySize;
                            existingItem.Thumbnail = thumbnail;
                        }
                    }
                    var sortedList = FileItems.ToList();

                    FileItems.Clear();
                    foreach (var fileItem in sortedList)
                    {
                        if (Instance.FileFilterHideRegex != null && Instance.FileFilterHideRegex != ""
                          && new Regex(Instance.FileFilterHideRegex).IsMatch(fileItem.Name))
                        {
                            continue;
                        }
                        FileItems.Add(fileItem);
                    }
                    _lastUpdated = DateTime.Now;
                    int hiddenCount = Int32.Parse(_fileCount) - (FileItems.Count - _folderCount);
                    if (hiddenCount > 0)
                    {
                        _fileCount += $" ({hiddenCount} hidden)";
                    }
                    SortItems();
                    Debug.WriteLine("LOADEDDDDDDDD");
                });
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("LoadFiles was canceled.");
            }
        }
        public void SortItems()
        {
            var sortedList = SortFileItems(FileItems, (int)Instance.SortBy, Instance.FolderOrder);
            FileItems.Clear();
            foreach (var fileItem in sortedList)
            {
                FileItems.Add(fileItem);
            }
        }
        private void FileListView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _canAutoClose = false;
            if (sender is not ListView listView)
                return;
            var point = e.GetPosition(listView);
            var element = listView.InputHitTest(point) as DependencyObject;
            while (element != null && element is not ListViewItem)
            {
                element = VisualTreeHelper.GetParent(element);
            }
            if (element is ListViewItem item && item.DataContext is FileItem clickedItem)
            {
                if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount != 2)
                {
                    DataObject data = new DataObject(DataFormats.FileDrop, new string[] { clickedItem.FullPath! });
                    Task.Run(() =>
                    {
                        Thread.Sleep(5);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DragDrop.DoDragDrop(listView, data, DragDropEffects.Copy | DragDropEffects.Move);
                        });
                    });
                }
            }
        }
        private void FileListView_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListView listView)
                return;
            var point = e.GetPosition(listView);
            var element = listView.InputHitTest(point) as DependencyObject;
            while (element != null && element is not ListViewItem)
            {
                element = VisualTreeHelper.GetParent(element);
            }
            if (element is ListViewItem item && item.DataContext is FileItem clickedItem)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(clickedItem.FullPath!) { UseShellExecute = true });
                }
                catch
                {
                }
            }
        }
        private void FileListView_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _canAutoClose = false;
            if (sender is not ListView listView)
                return;
            var point = e.GetPosition(listView);
            var element = listView.InputHitTest(point) as DependencyObject;
            while (element != null && element is not ListViewItem)
            {
                element = VisualTreeHelper.GetParent(element);
            }
            if (element is ListViewItem item && item.DataContext is FileItem clickedItem)
            {
                var windowHelper = new WindowInteropHelper(this);
                FileInfo[] files = new FileInfo[1];
                files[0] = new FileInfo(clickedItem.FullPath!);

                Point cursorPosition = System.Windows.Forms.Cursor.Position;
                System.Windows.Point wpfPoint = new System.Windows.Point(cursorPosition.X, cursorPosition.Y);
                Point drawingPoint = new Point((int)wpfPoint.X, (int)wpfPoint.Y);
                scm.ShowContextMenu(windowHelper.Handle, files, drawingPoint);
            }
        }
        private void Window_Drop(object sender, DragEventArgs e)
        {
            _canAutoClose = false;
            Task.Run(async () =>
            {
                Thread.Sleep(300);
                _canAutoClose = true;
            });
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (var file in files)
                {
                    string destinationPath = Path.Combine(_path, Path.GetFileName(file));

                    if (Path.GetDirectoryName(file) == _path)
                    {
                        return;
                    }
                    try
                    {
                        if (Directory.Exists(file))
                        {

                            Debug.WriteLine("Folder detected: " + file);
                            if (_path == "empty")
                            {
                                _path = file;
                                title.Text = Path.GetFileName(_path);
                                Instance.Folder = file;
                                Instance.Name = Path.GetFileName(_path);
                                MainWindow._controller.WriteInstanceToKey(Instance);
                                LoadFiles(_path);
                                DataContext = this;
                                InitializeFileWatcher();
                                showFolder.Visibility = Visibility.Visible;
                                addFolder.Visibility = Visibility.Hidden;

                            }
                            Directory.Move(file, destinationPath);

                        }
                        else
                        {
                            Debug.WriteLine("File detected: " + file);
                            File.Move(file, destinationPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error moving file: " + ex.Message);

                    }
                }
            }
        }


        private void FileItem_Click(object sender, MouseButtonEventArgs e)
        {
            var clickedFileItem = (sender as Border)?.DataContext as FileItem;

            if (clickedFileItem != null)
            {
                clickedFileItem.IsSelected = !clickedFileItem.IsSelected;

                foreach (var fileItem in FileItems)
                {
                    if (fileItem != clickedFileItem)
                    {
                        fileItem.IsSelected = false;
                    }
                }
            }
            if (e.ClickCount == 2 && sender is Border border && border.DataContext is FileItem clickedItem)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(clickedItem.FullPath!) { UseShellExecute = true });
                }
                catch //(Exception ex)
                {
                    //  MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (e.LeftButton == MouseButtonState.Pressed && sender is Border dragBorder)
            {
                if (dragBorder.DataContext is FileItem fileItem)
                {

                    DataObject data = new DataObject(DataFormats.FileDrop, new string[] { fileItem.FullPath! });
                    DragDrop.DoDragDrop(dragBorder, data, DragDropEffects.Copy | DragDropEffects.Move);
                }
            }

        }


        private void FileItem_RightClick(object sender, MouseButtonEventArgs e)
        {
            _canAutoClose = false;
            var clickedFileItem = (sender as Border)?.DataContext as FileItem;

            if (clickedFileItem != null)
            {
                clickedFileItem.IsSelected = !clickedFileItem.IsSelected;

                foreach (var fileItem in FileItems)
                {
                    if (fileItem != clickedFileItem)
                    {
                        fileItem.IsSelected = false;
                    }
                }
            }
            if (sender is Border border && border.DataContext is FileItem clickedItem)
            {
                var windowHelper = new WindowInteropHelper(this);


                FileInfo[] files = new FileInfo[1];
                files[0] = new FileInfo(clickedItem.FullPath!);

                Point cursorPosition = System.Windows.Forms.Cursor.Position;
                System.Windows.Point wpfPoint = new System.Windows.Point(cursorPosition.X, cursorPosition.Y);
                Point drawingPoint = new Point((int)wpfPoint.X, (int)wpfPoint.Y);
                scm.ShowContextMenu(windowHelper.Handle, files, drawingPoint);
            }
        }

        private void FileItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.DataContext is FileItem fileItem)
            {
                fileItem.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
            }
        }

        private void FileItem_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.DataContext is FileItem fileItem)
            {
                if (!fileItem.IsSelected)
                {
                    fileItem.Background = fileItem.IsSelected ? new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)) : Brushes.Transparent;
                }
                else
                {
                    fileItem.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
                }
            }
        }
        private async Task<BitmapSource?> GetThumbnailAsync(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    using (var shellFile = ShellObject.FromParsingName(path))
                    {
                        var bitmap = shellFile.Thumbnail.ExtraLargeBitmapSource;
                        if (bitmap != null)
                        {
                            return ResizeBitmap(bitmap, _iconSize, _iconSize);
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting thumbnail for {path}: {ex.Message}");
                return null;
            }
        }

        private BitmapSource ResizeBitmap(BitmapSource source, int width, int height)
        {
            var scaleX = (double)width / source.PixelWidth;
            var scaleY = (double)height / source.PixelHeight;
            
            var scaleTransform = new ScaleTransform(scaleX, scaleY);
            var transformedBitmap = new TransformedBitmap(source, scaleTransform);
            
            // Create a new BitmapSource with the exact dimensions we want
            var finalBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
            var stride = width * 4; // 4 bytes per pixel for Pbgra32
            var pixels = new byte[stride * height];
            transformedBitmap.CopyPixels(pixels, stride, 0);
            finalBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            
            return finalBitmap;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadIconSizeFromRegistry();
            UpdateFileExtensionIcon();
            UpdateHiddenFilesIcon();
            UpdateIconVisibility();
            AnimateChevron(_isMinimized, true);
            KeepWindowBehind();
            RegistryHelper rgh = new RegistryHelper("DeskFrame");
            bool toBlur = true;
            if (rgh.KeyExistsRoot("blurBackground"))
            {
                toBlur = (bool)rgh.ReadKeyValueRoot("blurBackground");
            }

            BackgroundType(toBlur);

            if (Instance != null)
            {
                Instance.IconSize = _iconSize;
            }

            if (FileItems != null && FileItems.Count > 0)
            {
                Dispatcher.InvokeAsync(async () =>
                {
                    var batchSize = 5;
                    var items = FileItems.ToList();
                    
                    for (int i = 0; i < items.Count; i += batchSize)
                    {
                        var batch = items.Skip(i).Take(batchSize).ToList();
                        foreach (var item in batch)
                        {
                            var newThumbnail = await GetThumbnailAsync(item.FullPath!);
                            if (newThumbnail != null)
                            {
                                item.Thumbnail = newThumbnail;
                                item.OnPropertyChanged(nameof(item.Thumbnail));
                            }
                        }
                        await Task.Delay(50);
                    }
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        public void ChangeBackgroundOpacity(int num)
        {
            try
            {
                var c = (Color)System.Windows.Media.ColorConverter.ConvertFromString(Instance.ListViewBackgroundColor);
                WindowBackground.Background = new SolidColorBrush(Color.FromArgb((byte)Instance.Opacity, c.R, c.G, c.B));
            }
            catch
            {

            }
        }
        public void ChangeIsBlack(bool value)
        {
            _isBlack = value;
        }
        public void BackgroundType(bool toBlur)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var accent = new Interop.AccentPolicy
            {
                AccentState = toBlur ? Interop.AccentState.ACCENT_ENABLE_BLURBEHIND :
                                       Interop.AccentState.ACCENT_DISABLED
            };

            var data = new Interop.WindowCompositionAttributeData
            {
                Attribute = Interop.WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = Marshal.SizeOf(accent),
                Data = Marshal.AllocHGlobal(Marshal.SizeOf(accent))
            };

            Marshal.StructureToPtr(accent, data.Data, false);
            Interop.SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(data.Data);
        }



        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var cursorPos = System.Windows.Forms.Cursor.Position;
            var windowPos = this.PointToScreen(new System.Windows.Point(0, 0));
            var windowWidth = this.ActualWidth;
            var windowHeight = this.ActualHeight;
            if (cursorPos.X - 10 < windowPos.X || cursorPos.X + 10 > windowPos.X + windowWidth ||
                cursorPos.Y - 10 < windowPos.Y || cursorPos.Y + 10 > windowPos.Y + windowHeight)
            {
                foreach (var fileItem in FileItems)
                {
                    fileItem.IsSelected = false;
                }
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            Instance.PosX = this.Left;
            Instance.PosY = this.Top;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            KeepWindowBehind();
            WindowChrome.SetWindowChrome(this, Instance.IsLocked ?
            new WindowChrome
            {
                ResizeBorderThickness = new Thickness(0),
                CaptionHeight = 0
            }
            : new WindowChrome
            {
                GlassFrameThickness = new Thickness(5),
                CaptionHeight = 0,
                ResizeBorderThickness = new Thickness(5, 0, 5, Instance.Minimized ? 0 : 5),
                CornerRadius = new CornerRadius(5)
            }
         );

            _currentVD = Array.IndexOf(VirtualDesktop.GetDesktops(), VirtualDesktop.Current) + 1;
            Debug.WriteLine($"Start to desktop number: {_currentVD}");
            if (Instance.ShowOnVirtualDesktops != null  && Instance.ShowOnVirtualDesktops.Length != 0 && !Instance.ShowOnVirtualDesktops.Contains(_currentVD))
            {
                this.Hide();
            }
            else
            {
                this.Show();
            }
            VirtualDesktop.CurrentChanged += (sender, args) =>
            {
                var newDesktop = args.NewDesktop;
                _currentVD = Array.IndexOf(VirtualDesktop.GetDesktops(), newDesktop) + 1;
                if (Instance.ShowOnVirtualDesktops != null && Instance.ShowOnVirtualDesktops.Length != 0 && !Instance.ShowOnVirtualDesktops.Contains(_currentVD))
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        this.Hide();
                    });
                }
                else
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        this.Show();
                    });
                }
                Debug.WriteLine($"Switched to virtual desktop: {_currentVD}");
            };
        }
        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            KeepWindowBehind();
        }

        private void UpdateIcons()
        {
            nameMenuItem.Icon = (Instance.SortBy == 1 || Instance.SortBy == 2)
                ? new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Filled = true }
                : new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Foreground = Brushes.Transparent };

            dateModifiedMenuItem.Icon = (Instance.SortBy == 3 || Instance.SortBy == 4)
                ? new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Filled = true }
                : new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Foreground = Brushes.Transparent };

            dateCreatedMenuItem.Icon = (Instance.SortBy == 5 || Instance.SortBy == 6)
                ? new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Filled = true }
                : new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Foreground = Brushes.Transparent };

            fileTypeMenuItem.Icon = (Instance.SortBy == 7 || Instance.SortBy == 8)
                ? new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Filled = true }
                : new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Foreground = Brushes.Transparent };

            fileSizeMenuItem.Icon = (Instance.SortBy == 9 || Instance.SortBy == 10)
                ? new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Filled = true }
                : new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Foreground = Brushes.Transparent };

            ascendingMenuItem.Icon = (Instance.SortBy % 2 != 0)
                ? new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Filled = true }
                : new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Foreground = Brushes.Transparent };

            descendingMenuItem.Icon = (Instance.SortBy % 2 == 0)
                ? new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Filled = true }
                : new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Foreground = Brushes.Transparent };

            if (folderNoneMenuItem != null)
            {
                folderNoneMenuItem.Icon = (Instance.FolderOrder == 0)
                    ? new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Filled = true }
                    : new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Foreground = Brushes.Transparent };
            }
            if (folderFirstMenuItem != null)
            {
                folderFirstMenuItem.Icon = (Instance.FolderOrder == 1)
                    ? new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Filled = true }
                    : new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Foreground = Brushes.Transparent };
            }
            if (folderLastMenuItem != null)
            {
                folderLastMenuItem.Icon = (Instance.FolderOrder == 2)
                    ? new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Filled = true }
                    : new SymbolIcon { Symbol = SymbolRegular.CircleSmall20, Foreground = Brushes.Transparent };
            }
        }
        private void titleBar_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContextMenu contextMenu = new ContextMenu();

            ToggleSwitch toggleHiddenFiles = new ToggleSwitch { Content = "Hidden files" };
            toggleHiddenFiles.Click += (s, args) => { ToggleHiddenFiles(); LoadFiles(_path); };

            ToggleSwitch toggleFileExtension = new ToggleSwitch { Content = "File extensions" };
            toggleFileExtension.Click += (_, _) => { ToggleFileExtension(); LoadFiles(_path); };

            toggleHiddenFiles.IsChecked = Instance.ShowHiddenFiles;
            toggleFileExtension.IsChecked = Instance.ShowFileExtension;

            MenuItem frameSettings = new MenuItem
            {
                Header = "Frame Settings",
                Height = 34,
                Icon = new SymbolIcon(SymbolRegular.Settings20)
            };
            frameSettings.Click += (s, args) =>
            {
                bool itWasMin = _isMinimized;
                if (itWasMin)
                {
                    Minimize_MouseLeftButtonDown(null, null);
                }
                var dialog = new FrameSettingsDialog(this);
                dialog.ShowDialog();
                if (dialog.DialogResult == true)
                {
                    if (itWasMin)
                    {
                        Minimize_MouseLeftButtonDown(null, null);
                    }
                    LoadFiles(_path);
                }
            };

            MenuItem reloadItems = new MenuItem
            {
                Header = "Reload",
                Height = 34,
                Icon = new SymbolIcon(SymbolRegular.ArrowSync20)
            };
            reloadItems.Click += (s, args) => { LoadFiles(_path); };

            MenuItem lockFrame = new MenuItem
            {
                Header = Instance.IsLocked ? "Unlock frame" : "Lock frame",
                Height = 34,
                Icon = Instance.IsLocked ? new SymbolIcon(SymbolRegular.LockClosed20) : new SymbolIcon(SymbolRegular.LockOpen20)
            };
            lockFrame.Click += (s, args) =>
            {
                _isLocked = !_isLocked;
                ToggleIsLocked();
                HandleWindowMove();
                WindowChrome.SetWindowChrome(this, Instance.IsLocked ?
                       new WindowChrome
                       {
                           ResizeBorderThickness = new Thickness(0),
                           CaptionHeight = 0

                       }
                       : new WindowChrome
                       {
                           GlassFrameThickness = new Thickness(5),
                           CaptionHeight = 0,
                           ResizeBorderThickness = new Thickness(5, 0, 5, Instance.Minimized ? 0 : 5),
                           CornerRadius = new CornerRadius(5)
                       }
                 );

                titleBar.Cursor = _isLocked ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.SizeAll;
            };

            MenuItem exitItem = new MenuItem
            {
                Header = "Remove",
                Height = 34,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFC6060")),
                Icon = new SymbolIcon(SymbolRegular.Delete20)

            };

            exitItem.Click += async (s, args) =>
            {
                var dialog = new MessageBox
                {
                    Title = "Confirm",
                    Content = "Are you sure you want to remove it?",
                    PrimaryButtonText = "Yes",
                    CloseButtonText = "No"
                };

                var result = await dialog.ShowDialogAsync();

                if (result == MessageBoxResult.Primary)
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(Instance.GetKeyLocation(), true)!;
                    if (key != null)
                    {
                        Registry.CurrentUser.DeleteSubKeyTree(Instance.GetKeyLocation());
                    }
                    this.Close();

                }
            };

            MenuItem sortByMenuItem = new MenuItem
            {
                Header = "Sort by",
                Height = 34,
                Icon = new SymbolIcon(SymbolRegular.ArrowSort20)
            };
            nameMenuItem = new MenuItem { Header = "Name", Height = 34, StaysOpenOnClick = true };
            dateModifiedMenuItem = new MenuItem { Header = "Date modified", Height = 34, StaysOpenOnClick = true };
            dateCreatedMenuItem = new MenuItem { Header = "Date created", Height = 34, StaysOpenOnClick = true };
            fileTypeMenuItem = new MenuItem { Header = "File type", Height = 34, StaysOpenOnClick = true };
            fileSizeMenuItem = new MenuItem { Header = "File size", Height = 34, StaysOpenOnClick = true };
            ascendingMenuItem = new MenuItem { Header = "Ascending", Height = 34, StaysOpenOnClick = true };
            descendingMenuItem = new MenuItem { Header = "Descending", Height = 34, StaysOpenOnClick = true };



            nameMenuItem.Click += async (s, args) =>
            {
                if (Instance.SortBy % 2 != 0 || Instance.SortBy != 1) Instance.SortBy = 1;
                else Instance.SortBy = 2;
                UpdateIcons();
                SortItems();
            };
            dateModifiedMenuItem.Click += async (s, args) =>
            {
                if (Instance.SortBy % 2 != 0 || Instance.SortBy != 3) Instance.SortBy = 3;
                else Instance.SortBy = 4;
                UpdateIcons();
                SortItems();
            };

            dateCreatedMenuItem.Click += async (s, args) =>
            {
                if (Instance.SortBy % 2 != 0 || Instance.SortBy != 5) Instance.SortBy = 5;
                else Instance.SortBy = 6;
                UpdateIcons();
                SortItems();
            };
            fileTypeMenuItem.Click += async (s, args) =>
            {
                if (Instance.SortBy % 2 != 0 || Instance.SortBy != 7) Instance.SortBy = 7;
                else Instance.SortBy = 8;
                UpdateIcons();
                SortItems();
            };
            fileSizeMenuItem.Click += (s, args) =>
            {
                if (Instance.SortBy % 2 != 0 && Instance.SortBy != 9) Instance.SortBy = 9;
                else Instance.SortBy = 10;
                UpdateIcons();
                SortItems();
            };

            ascendingMenuItem.Click += async (s, args) =>
            {
                if (Instance.SortBy % 2 == 0) Instance.SortBy -= 1;
                UpdateIcons();
                SortItems();
            };

            descendingMenuItem.Click += async (s, args) =>
            {
                if (Instance.SortBy % 2 != 0) Instance.SortBy += 1;
                UpdateIcons();
                SortItems();
            };

            MenuItem FrameInfoItem = new MenuItem
            {
                StaysOpenOnClick = true,
                IsEnabled = false,
            };
            TextBlock InfoText = new TextBlock
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
            };

            InfoText.Inlines.Add(new Run($"Files: ") { Foreground = Brushes.White });
            InfoText.Inlines.Add(new Run($"{_fileCount}") { Foreground = Brushes.CornflowerBlue });
            InfoText.Inlines.Add(new Run("\n"));

            InfoText.Inlines.Add(new Run($"Folders: ") { Foreground = Brushes.White });
            InfoText.Inlines.Add(new Run($"{_folderCount}") { Foreground = Brushes.CornflowerBlue });
            InfoText.Inlines.Add(new Run("\n"));

            InfoText.Inlines.Add(new Run($"Folder Size: ") { Foreground = Brushes.White });
            InfoText.Inlines.Add(new Run($"{_folderSize}") { Foreground = Brushes.CornflowerBlue });
            InfoText.Inlines.Add(new Run("\n"));

            InfoText.Inlines.Add(new Run($"Last Updated: ") { Foreground = Brushes.White });
            InfoText.Inlines.Add(new Run($"{_lastUpdated.ToString("hh:mm tt")}") { Foreground = Brushes.CornflowerBlue });

            FrameInfoItem.Header = InfoText;


            folderOrderMenuItem = new MenuItem
            {
                Header = "Folder order",
                Height = 36,
                StaysOpenOnClick = true,
                Icon = new SymbolIcon { Symbol = SymbolRegular.Folder20 }
            };

            folderNoneMenuItem = new MenuItem { Header = "None", Height = 34, StaysOpenOnClick = true };
            folderFirstMenuItem = new MenuItem { Header = "First", Height = 34, StaysOpenOnClick = true };
            folderLastMenuItem = new MenuItem { Header = "Last", Height = 34, StaysOpenOnClick = true };

            folderNoneMenuItem.Click += (s, args) =>
            {
                Instance.FolderOrder = 0;
                UpdateIcons();
                SortItems();
            };
            folderFirstMenuItem.Click += (s, args) =>
            {
                Instance.FolderOrder = 1;
                UpdateIcons();
                SortItems();
            };
            folderLastMenuItem.Click += (s, args) =>
            {
                Instance.FolderOrder = 2;
                UpdateIcons();
                SortItems();
            };

            UpdateIcons();

            MenuItem openInExplorerMenuItem = new MenuItem
            {
                Header = "Open folder",
                Icon = new SymbolIcon { Symbol = SymbolRegular.FolderOpen20 }
            };
            openInExplorerMenuItem.Click += (_, _) => { OpenFolder(); };


            MenuItem changeItemView = new MenuItem
            {
                Header = "Change view"
            };
            if (showFolder.Visibility == Visibility.Visible)
            {
                changeItemView.Header = "Grid view";
                changeItemView.Icon = new SymbolIcon { Symbol = SymbolRegular.Grid20 };
            }
            else
            {
                changeItemView.Header = "Details view";
                changeItemView.Icon = new SymbolIcon { Symbol = SymbolRegular.AppsList20 };
            }
            changeItemView.Click += (_, _) =>
            {
                if (showFolder.Visibility == Visibility.Visible)
                {
                    changeItemView.Header = "Grid view";
                    changeItemView.Icon = new SymbolIcon { Symbol = SymbolRegular.Grid20 };
                    showFolderInGrid.Visibility = Visibility.Visible;
                    showFolder.Visibility = Visibility.Hidden;
                    Instance.ShowInGrid = !Instance.ShowInGrid;
                }
                else
                {
                    Instance.ShowInGrid = !Instance.ShowInGrid;
                    showFolder.Visibility = Visibility.Visible;
                    showFolderInGrid.Visibility = Visibility.Hidden;
                    changeItemView.Header = "Details view";
                    changeItemView.Icon = new SymbolIcon { Symbol = SymbolRegular.AppsList20 };
                }
            };

            folderOrderMenuItem.Items.Add(folderNoneMenuItem);
            folderOrderMenuItem.Items.Add(folderFirstMenuItem);
            folderOrderMenuItem.Items.Add(folderLastMenuItem);


            sortByMenuItem.Items.Add(folderOrderMenuItem);
            sortByMenuItem.Items.Add(new Separator());
            sortByMenuItem.Items.Add(nameMenuItem);
            sortByMenuItem.Items.Add(dateModifiedMenuItem);
            sortByMenuItem.Items.Add(dateCreatedMenuItem);
            sortByMenuItem.Items.Add(fileTypeMenuItem);
            sortByMenuItem.Items.Add(fileSizeMenuItem);
            sortByMenuItem.Items.Add(new Separator());
            sortByMenuItem.Items.Add(ascendingMenuItem);
            sortByMenuItem.Items.Add(descendingMenuItem);

            contextMenu.Items.Add(sortByMenuItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(lockFrame);
            contextMenu.Items.Add(reloadItems);
            contextMenu.Items.Add(openInExplorerMenuItem);
            contextMenu.Items.Add(changeItemView);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(toggleHiddenFiles);
            contextMenu.Items.Add(toggleFileExtension);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(frameSettings);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(FrameInfoItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(exitItem);

            contextMenu.IsOpen = true;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            KeepWindowBehind();
            Debug.WriteLine("Window_StateChanged hide");
        }
        public Task<string> BytesToStringAsync(long byteCount)
        {
            return Task.Run(() =>
            {
                double kilobytes = byteCount / 1024.0;
                string formattedKilobytes;
                try
                {
                    formattedKilobytes = kilobytes.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ");
                }
                catch
                {
                    try
                    {
                        formattedKilobytes = kilobytes.ToString("#,0", System.Globalization.CultureInfo.CurrentCulture).Replace(",", " ");
                    }
                    catch
                    {
                        formattedKilobytes = kilobytes.ToString("#,0").Replace(",", " ");
                    }
                }
                return formattedKilobytes + " KB";
            });
        }

        private void FileListView_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var point = e.GetPosition(FileListView);
            var hit = VisualTreeHelper.HitTest(FileListView, point)?.VisualHit;

            while (hit != null && hit is not GridViewColumnHeader)
                hit = VisualTreeHelper.GetParent(hit);

            if (hit is not GridViewColumnHeader header || header.Column == null)
                return;

            int newSort = Instance.SortBy;

            if (header.Column == NameGridColumn)
                newSort = Instance.SortBy != 1 ? 1 : 2;
            else if (header.Column == DateModifiedGridColumn)
                newSort = Instance.SortBy != 3 ? 3 : 4;
            else if (header.Column == SizeGridColumn)
                newSort = Instance.SortBy != 9 ? 9 : 10;

            if (newSort != Instance.SortBy)
            {
                Instance.SortBy = newSort;
                SortItems();
            }
        }

        public class FileItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            private bool _isSelected;
            public bool IsFolder { get; set; }
            private Brush _background = Brushes.Transparent;
            private int _maxHeight = 40;
            private TextTrimming _textTrimming = TextTrimming.CharacterEllipsis;
            private string? _displayName;
            public string? Name { get; set; }
            public string? FullPath { get; set; }
            public BitmapSource? Thumbnail { get; set; }
            public DateTime DateModified { get; set; }
            public DateTime DateCreated { get; set; }
            public string? FileType { get; set; }
            public long ItemSize { get; set; }
            public string DisplaySize { get; set; }

            public string DisplayName
            {
                get => Name;

                private set
                {
                    _displayName = value;
                    OnPropertyChanged(nameof(DisplayName));
                }
            }

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
                        Background = _isSelected ? new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)) : Brushes.Transparent;
                        MaxHeight = _isSelected ? 40 : 40;
                        TextTrimming = _isSelected ? TextTrimming.CharacterEllipsis : TextTrimming.CharacterEllipsis;

                        OnPropertyChanged(nameof(IsSelected));
                        OnPropertyChanged(nameof(Background));
                        OnPropertyChanged(nameof(MaxHeight));
                        OnPropertyChanged(nameof(TextTrimming));
                        OnPropertyChanged(nameof(DisplayName));
                    }
                }
            }

            public Brush Background
            {
                get => _background;
                set
                {
                    _background = value;
                    OnPropertyChanged(nameof(Background));
                }
            }

            public int MaxHeight
            {
                get => _maxHeight;
                private set
                {
                    _maxHeight = value;
                    OnPropertyChanged(nameof(MaxHeight));
                }
            }

            public TextTrimming TextTrimming
            {
                get => _textTrimming;
                private set
                {
                    _textTrimming = value;
                    OnPropertyChanged(nameof(TextTrimming));
                }
            }

            public void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Activate();
            _canAutoClose = true;
            if (_isOnEdge && _isMinimized)
            {
                if (!_canAnimate) return;
                Minimize_MouseLeftButtonDown(null, null);
            }
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_canAutoClose) FilterTextBox.Text = null;
            this.SetNoActivate();
            if (_isOnEdge && !_isMinimized)
            {
                if (!_canAutoClose) return;

                Task.Run(() =>
                {
                    Thread.Sleep(150);
                    foreach (var fileItem in FileItems)
                    {
                        fileItem.IsSelected = false;
                        fileItem.Background = Brushes.Transparent;

                    }
                    Dispatcher.InvokeAsync(() =>
                   {
                       Minimize_MouseLeftButtonDown(null, null);
                   });
                });
            }
        }

        public void UpdateIconVisibility()
        {
            if (FileExtensionIcon != null)
            {
                FileExtensionIconGrid.Visibility = Instance.ShowFileExtensionIcon ? Visibility.Visible : Visibility.Collapsed;
            }
            if (HiddenFilesIcon != null)
            {
                HiddenFilesIconGrid.Visibility = Instance.ShowHiddenFilesIcon ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && showFolder.Visibility == Visibility.Visible)
            {
                int delta = e.Delta > 0 ? 8 : -8;
                int newSize = Math.Clamp(Instance.IconSize + delta, MIN_ICON_SIZE, MAX_ICON_SIZE);
                
                if (newSize != Instance.IconSize)
                {
                    _iconSize = newSize;
                    Instance.IconSize = newSize; 
                    SaveIconSizeToRegistry();
                    
                    Dispatcher.InvokeAsync(async () =>
                    {
                        var batchSize = 5;
                        var items = FileItems.ToList();
                        
                        for (int i = 0; i < items.Count; i += batchSize)
                        {
                            var batch = items.Skip(i).Take(batchSize).ToList();
                            foreach (var item in batch)
                            {
                                var newThumbnail = await GetThumbnailAsync(item.FullPath!);
                                if (newThumbnail != null)
                                {
                                    item.Thumbnail = newThumbnail;
                                    item.OnPropertyChanged(nameof(item.Thumbnail));
                                }
                            }
                            await Task.Delay(50);
                        }
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        private void SaveIconSizeToRegistry()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\DeskFrame"))
                {
                    if (key != null)
                    {
                        key.SetValue($"IconSize_{Instance.Id}", _iconSize, RegistryValueKind.DWord);
                        Debug.WriteLine($"Saved icon size {_iconSize} for instance {Instance.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save icon size to registry: {ex.Message}");
            }
        }

        private void LoadIconSizeFromRegistry()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\DeskFrame"))
                {
                    if (key != null)
                    {
                        object value = key.GetValue($"IconSize_{Instance.Id}");
                        if (value != null)
                        {
                            int size = Math.Clamp(Convert.ToInt32(value), MIN_ICON_SIZE, MAX_ICON_SIZE);
                            _iconSize = size;
                            Instance.IconSize = size;
                            Debug.WriteLine($"Loaded icon size {size} for instance {Instance.Id}");
                        }
                        else
                        {
                            Debug.WriteLine($"No saved icon size found for instance {Instance.Id}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load icon size from registry: {ex.Message}");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveIconSizeToRegistry();
        }
    }
}