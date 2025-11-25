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
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using static DeskFrame.Util.Interop;
using IWshRuntimeLibrary;
using File = System.IO.File;
using DeskFrame.Core;
using DeskFrame.Properties;
using Microsoft.WindowsAPICodePack.Shell;
using Binding = System.Windows.Data.Binding;
namespace DeskFrame
{
    public partial class DeskFrameWindow : System.Windows.Window
    {
        ShellContextMenu scm = new ShellContextMenu();
        public Instance Instance { get; set; }
        public string _currentFolderPath;
        private FileSystemWatcher _fileWatcher = new FileSystemWatcher();
        public ObservableCollection<FileItem> FileItems { get; set; }

        public bool VirtualDesktopSupported;
        IntPtr hwnd;
        IntPtr shellView = IntPtr.Zero;

        private bool _dragdropIntoFolder;
        public int _itemPerRow;
        public int ItemPerRow
        {
            get => _itemPerRow;
            set
            {
                if (_itemPerRow != value)
                {
                    _itemPerRow = value;
                }
            }
        }
        string _dropIntoFolderPath;
        FrameworkElement _lastBorder;
        private bool _mouseIsOver;
        private bool _contextMenuIsOpen = false;
        private bool _fixIsOnBottomInit = true;
        private bool _didFixIsOnBottom = false;
        private bool _isMinimized = false;
        private bool _isIngrid = true;
        private bool _grabbedOnLeft;
        private int _snapDistance = 8;
        private int _gridSnapDistance = 10;
        private int _currentVD;
        int _oriPosX, _oriPosY;
        private bool _isBlack = true;
        private bool _checkForChages = false;
        private bool _canAutoClose = true;
        private bool _isLocked = false;
        private bool _isOnTop = false;
        private bool _isOnBottom = false;
        private bool _isLeftButtonDown = false;
        bool _canAnimate = true;
        private double _originalHeight;
        public int neighborFrameCount = 0;
        public int _previousItemPerRow = 0;
        private double _previousHeight = -1;
        public bool isMouseDown = false;
        private ICollectionView _collectionView;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private CancellationTokenSource loadFilesCancellationToken = new CancellationTokenSource();
        private CancellationTokenSource _changeIconSizeCts = new CancellationTokenSource();
        private CancellationTokenSource _adjustPositionCts;
        public DeskFrameWindow WonRight = null;
        public DeskFrameWindow WonLeft = null;

        ContextMenu contextMenu = new ContextMenu();
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
        private double _itemWidth;
        private double _windowsScalingFactor;

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
                { (int)SortBy.NameAsc, x => x.OrderBy(i => Regex.Replace(i.Name ?? "", @"\d+", m => m.Value.PadLeft(10, '0')), StringComparer.OrdinalIgnoreCase)},
                { (int)SortBy.NameDesc,x => x .OrderByDescending(i => Regex.Replace(i.Name ?? "", @"\d+", m => m.Value.PadLeft(10, '0')), StringComparer.OrdinalIgnoreCase)},
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
                items = items.OrderBy(i => !i.IsFolder);
            else if (folderOrder == 2)
                items = items.OrderBy(i => i.IsFolder);

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
                    { (int)SortBy.NameAsc, x => x.OrderBy(i => Regex.Replace(i.item.Name ?? "", @"\d+", m => m.Value.PadLeft(10, '0')), StringComparer.OrdinalIgnoreCase)},
                    { (int)SortBy.NameDesc,x => x .OrderByDescending(i => Regex.Replace(i.item.Name ?? "", @"\d+", m => m.Value.PadLeft(10, '0')), StringComparer.OrdinalIgnoreCase)},
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


            var sortedFileInfos = sortedItems.Select(x => x.item).ToList();
            if (Instance.LastAccesedToFirstRow)
            {
                FirstRowByLastAccessed(sortedFileInfos, Instance.LastAccessedFiles, ItemPerRow);
            }
            return sortedFileInfos;
        }
        public void FirstRowByLastAccessed(List<FileSystemInfo> items, List<string> lastAccessedFileIds, int topN)
        {
            if (items == null || items.Count == 0 || lastAccessedFileIds == null || lastAccessedFileIds.Count == 0 || topN <= 0)
                return;

            var fileLookup = items
                .Where(f => f.FullName != null)
                .GroupBy(f => GetFileId(f.FullName).ToString())
                .ToDictionary(g => g.Key, g => g.ToList());

            var topIds = lastAccessedFileIds
                .Where(id => fileLookup.ContainsKey(id))
                .Take(topN)
                .ToList();

            var topFiles = new List<FileSystemInfo>();
            foreach (var id in topIds)
            {
                if (!fileLookup.ContainsKey(id))
                    continue;
                topFiles.AddRange(fileLookup[id]);
            }

            var remainingFiles = items.Except(topFiles).ToList();
            items.Clear();
            items.AddRange(topFiles);
            items.AddRange(remainingFiles);
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
        private void MouseLeaveWindow(bool animateActiveColor = true)
        {
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 1;
            timer.Tick += (s, e) =>
            {
                if (animateActiveColor && !IsCursorWithinWindowBounds())
                {
                    _mouseIsOver = false;
                    AnimateActiveColor(Instance.AnimationSpeed);
                    if (Instance.HideTitleBarIconsWhenInactive)
                    {
                        TitleBarIconsFadeAnimation(false);
                    }
                }
                if (!IsCursorWithinWindowBounds() && (GetAsyncKeyState(0x01) & 0x8000) == 0) // Left mouse button is not down
                {

                    if (_canAutoClose)
                    {
                        FilterTextBox.Text = null;
                        FilterTextBox.Visibility = Visibility.Collapsed;
                    }
                    this.SetNoActivate();
                    if (_didFixIsOnBottom) _fixIsOnBottomInit = false;

                    var timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(1)
                    };
                    timer.Tick += (s, args) =>
                    {
                        if (!_dragdropIntoFolder) ;
                        {
                            Dispatcher.InvokeAsync(() =>
                            {
                                FileListView.SelectedIndex = -1;
                                foreach (var item in FileListView.Items)
                                {
                                    var container = FileListView.ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
                                    if (container != null) container.IsSelected = false;
                                }
                            });
                            timer.Stop();
                        }
                    };
                    timer.Start();

                    if ((Instance.AutoExpandonCursor) && !_isMinimized && _canAutoClose)
                    {
                        AnimateWindowOpacity(Instance.IdleOpacity, Instance.AnimationSpeed);
                        Minimize_MouseLeftButtonDown(null, null);
                        Task.Run(() =>
                        {
                            try
                            {
                                foreach (var fileItem in FileItems)
                                {
                                    fileItem.IsSelected = false;
                                    fileItem.Background = Brushes.Transparent;
                                }
                            }
                            catch { }
                        });
                    }
                    else
                    {
                        AnimateWindowOpacity(Instance.IdleOpacity, Instance.AnimationSpeed);
                    }
                }
                if (!_mouseIsOver)
                {
                    timer.Stop();
                }
            };
            timer.Start();
        }
        private void HandleRightClick(Window root, IntPtr lParam)
        {
            POINT pt = new POINT
            {
                X = (short)(lParam.ToInt32() & 0xFFFF),
                Y = (short)((lParam.ToInt32() >> 16) & 0xFFFF)
            };

            System.Windows.Point relativePt = root.PointFromScreen(new System.Windows.Point(pt.X, pt.Y));

            if (root.InputHitTest(relativePt) is DependencyObject hit)
            {
                var listView = FindParentOrChild<ListView>(hit);
                if (listView != null)
                {
                    var mouseArgs = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Right)
                    {
                        RoutedEvent = UIElement.MouseRightButtonUpEvent,
                        Source = listView
                    };
                    FileListView_MouseRightButtonUp(listView, mouseArgs);
                }
            }
        }
        public void FirstRowByLastAccessed(ObservableCollection<FileItem> items, List<string> lastAccessedFiles, int topN)
        {
            var wrapPanel = FindParentOrChild<WrapPanel>(FileWrapPanel);
            if (wrapPanel != null)
            {
                double itemWidth = wrapPanel.ItemWidth;
                ItemPerRow = (int)((this.Width) / itemWidth);
            }
            if (items == null || items.Count == 0 || lastAccessedFiles == null || lastAccessedFiles.Count == 0 || topN <= 0)
                return;
            var fileLookup = items
                .Where(i => i.FullPath != null)
                .GroupBy(i => GetFileId(i.FullPath!).ToString())
                .ToDictionary(g => g.Key, g => g.ToList());

            var topIds = lastAccessedFiles
                .Where(id => fileLookup.ContainsKey(id))
                .Take(topN)
                .ToList();

            int insertIndex = 0;
            foreach (var id in topIds)
            {
                foreach (var item in fileLookup[id])
                {
                    int oldIndex = items.IndexOf(item);
                    if (oldIndex >= 0 && oldIndex != insertIndex)
                        items.Move(oldIndex, insertIndex);
                    insertIndex++;
                }
            }
            var remainingItems = new ObservableCollection<FileItem>(items.Skip(insertIndex));
            var sortedRemaining = SortFileItems(remainingItems, (int)Instance.SortBy, Instance.FolderOrder);
            for (int i = 0; i < sortedRemaining.Count; i++)
            {
                int oldIndex = items.IndexOf(sortedRemaining[i]);
                if (oldIndex >= 0 && oldIndex != insertIndex + i)
                    items.Move(oldIndex, insertIndex + i);
            }
        }
        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (!(HwndSource.FromHwnd(hWnd).RootVisual is Window rootVisual))
                return IntPtr.Zero;
            if (msg == 0x020A && (GetAsyncKeyState(0x11) & 0x8000) != 0) // WM_MOUSEWHEEL && control down
            {
                _changeIconSizeCts.Cancel();
                _changeIconSizeCts = new CancellationTokenSource();
                var token = _changeIconSizeCts.Token;
                int delta = (short)((int)wParam >> 16);
                if (delta < 0) Instance.IconSize -= 4;
                else if (delta > 0) Instance.IconSize += 4;
                Task.Run(async () =>
                {
                    await Task.Delay(500, token);
                    if (!token.IsCancellationRequested)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LoadingProgressRingFade(true);
                        });
                        foreach (var item in FileItems)
                        {
                            item.Thumbnail = await GetThumbnailAsync(item.FullPath!);
                        }
                        Dispatcher.Invoke(() =>
                        {
                            FileWrapPanel.Items.Refresh();
                            Task.Run(async () =>
                            {
                                await Task.Delay(200, token);
                                Dispatcher.Invoke(() =>
                                {
                                    LoadingProgressRingFade(false);
                                });
                            });
                        });
                    }
                });
                handled = true;
                return 4;
            }
            if (msg == 0x0201) // WM_LBUTTONDOWN
            {
                _isLeftButtonDown = true;
                _grabbedOnLeft = Mouse.GetPosition(this).X < this.Width / 2;
            }
            if (msg == 0x0202) // WM_LBUTTONUP
            {
                _isLeftButtonDown = false;
            }
            if (msg == 0x0205) // WM_RBUTTONUP
            {
                HandleRightClick(rootVisual, lParam);
                handled = true;
            }
            if (msg == 0x0205) // WM_RBUTTONUP
            {
                int x = lParam.ToInt32() & 0xFFFF;
                int y = (lParam.ToInt32() >> 16) & 0xFFFF;
                var screenPoint = new System.Windows.Point(x, y);
                var relativePoint = FileWrapPanel.PointFromScreen(screenPoint);
                if (VisualTreeHelper.HitTest(FileWrapPanel, relativePoint) == null)
                {
                    var curPos = System.Windows.Forms.Cursor.Position;
                    try
                    {
                        var windowHelper = new WindowInteropHelper(this);
                        Point cursorPosition = System.Windows.Forms.Cursor.Position;
                        System.Windows.Point wpfPoint = new System.Windows.Point(cursorPosition.X, cursorPosition.Y);
                        Point drawingPoint = new Point((int)wpfPoint.X, (int)wpfPoint.Y);
                        DirectoryInfo folder = new DirectoryInfo(_currentFolderPath);
                        _contextMenuIsOpen = true;
                        scm.ContextMenuClosed += () =>
                        {
                            _contextMenuIsOpen = false;
                        };
                        scm.ShowContextMenu(windowHelper.Handle, new DirectoryInfo(_currentFolderPath), drawingPoint);
                        handled = true;
                    }
                    catch
                    {
                    }

                }
            }


            if (msg == 0x0214) // WM_SIZING
            {
                int edge = wParam.ToInt32();
                if (_isMinimized && (edge != 1 && edge != 2)) // block resizing except left or right edges
                {
                    var hwnd = new WindowInteropHelper(this).Handle;
                    Interop.RECT currentRect;
                    Interop.GetWindowRect(hwnd, out currentRect);
                    Marshal.StructureToPtr(currentRect, lParam, true);
                    handled = true;
                    return IntPtr.Zero;
                }
                Interop.RECT rect = (Interop.RECT)Marshal.PtrToStructure(lParam, typeof(Interop.RECT));

                Instance.PosX = this.Left;
                Instance.PosY = this.Top;

                Instance.Width = this.Width;
                double height = rect.Bottom - rect.Top;
                if (height <= 102 && !_isMinimized)
                {
                    this.Height = 102;
                    rect.Bottom = rect.Top + 102;
                    Marshal.StructureToPtr(rect, lParam, true);
                    handled = true;
                    return (IntPtr)4;
                }
                else if (!_isMinimized && this.ActualHeight != titleBar.Height && _canAnimate)
                {
                    Instance.Height = this.ActualHeight;
                }

                if (Instance.LastAccesedToFirstRow)
                {
                    var wrapPanel = FindParentOrChild<WrapPanel>(FileWrapPanel);
                    if (wrapPanel != null)
                    {
                        double width = rect.Right - rect.Left;
                        double newWidth = rect.Right - rect.Left;

                        if (Instance.SnapWidthToIconWidth)
                        {
                            newWidth = Math.Round(width / wrapPanel.ItemWidth) * wrapPanel.ItemWidth + 4; // +4 margin
                            if (Instance.SnapWidthToIconWidth)
                            {
                                FileWrapPanel.Margin = new Thickness(6, 5, 0, 5);
                                newWidth += 15;
                            }
                        }
                        if (!Instance.SnapWidthToIconWidth)
                        {
                            FileWrapPanel.Margin = new Thickness(0, 0, 0, 0);
                        }
                        int newItemPerRow = (int)Math.Floor(newWidth / wrapPanel.ItemWidth);
                        if (_previousItemPerRow != newItemPerRow)
                        {
                            ItemPerRow = newItemPerRow;
                            FirstRowByLastAccessed(FileItems, Instance.LastAccessedFiles, ItemPerRow);
                            _previousItemPerRow = newItemPerRow;
                        }
                    }
                }

                if (Instance.SnapWidthToIconWidth)
                {
                    double width = rect.Right - rect.Left;
                    var item = FindParentOrChild<WrapPanel>(FileWrapPanel);
                    double newWidth = Math.Round(width / item.ItemWidth) * item.ItemWidth + 4; // +4 margin

                    if (Instance.SnapWidthToIconWidth_PlusScrollbarWidth)
                    {
                        newWidth += 15;
                        FileWrapPanel.Margin = new Thickness(6, 5, 0, 5);
                    }
                    else
                    {
                        FileWrapPanel.Margin = new Thickness(0, 0, 0, 0);
                    }
                    if (width != newWidth)
                    {
                        int diff = (int)(newWidth - width);
                        int w = (int)wParam;

                        if (w == 1 || w == 5 || w == 7) // left sides
                        {
                            rect.Left -= diff;
                        }
                        if (w == 2 || w == 6 || w == 8) // right sides
                        {
                            rect.Right += diff;
                        }

                        Marshal.StructureToPtr(rect, lParam, true);
                        Instance.Width = this.Width;
                    }
                }
            }

            if (msg == 0x0005 && _isOnBottom) // WM_SIZE
            {
                double newHeight = (lParam.ToInt32() >> 16) & 0xFFFF;
                if (_previousHeight != -1 && _previousHeight != newHeight)
                {
                    IntPtr hwnd = new WindowInteropHelper(this).Handle;

                    var workingArea = Screen.FromPoint(System.Windows.Forms.Control.MousePosition).WorkingArea;

                    Interop.GetWindowRect(hwnd, out RECT windowRect);
                    POINT pt = new POINT { X = windowRect.Left, Y = windowRect.Top };
                    ScreenToClient(GetParent(hwnd), ref pt);
                    double delta = newHeight - _previousHeight;
                    int newTop = (int)((pt.Y - delta) - windowRect.Bottom <= workingArea.Bottom ?
                        (int)(pt.Y -= (int)delta) :
                        Instance.Height - workingArea.Bottom - titleBar.Height);

                    if (delta > 0) // UP
                    {
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            Interop.SetWindowPos(hwnd, IntPtr.Zero, pt.X,
                                    newTop,
                                    0, 0,
                                   SWP_NOSIZE
                                  );

                        }, DispatcherPriority.Normal);
                    }
                    else
                    {
                        Interop.SetWindowPos(hwnd, IntPtr.Zero, pt.X,
                                newTop,
                                0, 0,
                               SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOREDRAW
                              );
                    }
                    if (this.Top + titleBar.Height > workingArea.Bottom)
                    {
                        // this.Top = workingArea.Bottom - 30;
                        _didFixIsOnBottom = true;
                        Interop.SetWindowPos(hwnd, IntPtr.Zero, pt.X,
                              (int)(workingArea.Bottom - titleBar.Height),
                              0, 0,
                             SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOREDRAW
                            );
                    }
                    if (_fixIsOnBottomInit && pt.Y + this.Height != workingArea.Bottom)
                    {
                        Interop.SetWindowPos(hwnd, IntPtr.Zero, pt.X,
                           (int)(workingArea.Bottom - this.Height + 1), // +1 pixel because otherwise it hovers  by 1 px above the desktop
                           0, 0,
                          SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOREDRAW
                         );
                    }
                }
                _previousHeight = newHeight;
                return 4;
            }

            if (msg == 70)
            {
                Interop.WINDOWPOS structure = Marshal.PtrToStructure<Interop.WINDOWPOS>(lParam);
                structure.flags |= 4U;
                Marshal.StructureToPtr<Interop.WINDOWPOS>(structure, lParam, false);
            }
            if (msg == 0x0003 &&  // WM_MOVE
                ((GetAsyncKeyState(0xA4) & 0x8000) == 0 && (GetAsyncKeyState(0xA5) & 0x8000) == 0)) // left and right alt isn't down
            {
                _isIngrid = false;

                HandleWindowMove(false);
                if (WonRight != null)
                {
                    WonRight.HandleWindowMove(false);
                }
                if (WonLeft != null)
                {
                    WonLeft.HandleWindowMove(false);
                }
            }
            if (_isLeftButtonDown &&
                ((GetAsyncKeyState(0xA4) & 0x8000) != 0 || (GetAsyncKeyState(0xA5) & 0x8000) != 0) && // left or right is alt down
                msg == 0x0003) // WM_MOVE
            {
                SnapToGrid();
            }

            return IntPtr.Zero;
        }
        private void SnapToGrid()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;

            Interop.RECT windowRect;
            Interop.GetWindowRect(hwnd, out windowRect);

            int windowLeft = windowRect.Left;
            int windowTop = windowRect.Top;
            int windowRight = windowRect.Right;
            int windowBottom = windowRect.Bottom;

            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null) return;

            int newWindowLeft = windowLeft;
            int newWindowTop = windowTop;
            int newWindowBottom = windowBottom;
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
                bool didSnap = false;
                if (Math.Abs(windowLeft - otherRight) <= _gridSnapDistance && Math.Abs(windowTop - otherTop) <= titleBar.Height)
                {
                    newWindowLeft = otherRight + _gridSnapDistance;
                    newWindowTop = otherTop;
                    if (_grabbedOnLeft) didSnap = true;
                }
                else if (Math.Abs(windowRight - otherLeft) <= _gridSnapDistance && Math.Abs(windowTop - otherTop) <= titleBar.Height)
                {
                    newWindowLeft = otherLeft - (windowRight - windowLeft) - _gridSnapDistance;
                    newWindowTop = otherTop;
                    if (_grabbedOnLeft) didSnap = true;
                }
                if (_grabbedOnLeft && !didSnap)
                {
                    if (Math.Abs(windowTop - otherBottom) <= _gridSnapDistance && Math.Abs(windowLeft - otherLeft) <= _snapDistance)
                    {
                        newWindowTop = otherBottom + _gridSnapDistance;
                        newWindowLeft = otherLeft;

                    }
                    else if (Math.Abs(windowBottom - otherTop) <= _gridSnapDistance && Math.Abs(windowLeft - otherLeft) <= _snapDistance)
                    {
                        newWindowTop = otherTop - (windowBottom - windowTop) - _gridSnapDistance;
                        newWindowLeft = otherLeft;
                    }
                }

                if (Math.Abs(windowRight - otherRight) <= _gridSnapDistance && Math.Abs(windowTop - otherBottom) <= _snapDistance)
                {
                    newWindowTop = otherBottom + _gridSnapDistance;
                    newWindowLeft = otherRight - (windowRight - windowLeft);
                }
                else if (Math.Abs(windowRight - otherRight) <= _gridSnapDistance && Math.Abs(windowBottom - otherTop) <= _snapDistance)
                {
                    newWindowTop = otherTop - (windowBottom - windowTop) - _gridSnapDistance;
                    newWindowLeft = otherRight - (windowRight - windowLeft);
                }
            }

            if (newWindowLeft != windowLeft || newWindowTop != windowTop || newWindowBottom != windowBottom)
            {
                POINT pt = new POINT { X = newWindowLeft, Y = newWindowTop };
                ScreenToClient(GetParent(hwnd), ref pt);
                SetWindowPos(hwnd, IntPtr.Zero, pt.X, pt.Y, 0, 0,
                             SWP_NOZORDER | SWP_NOSIZE | SWP_NOACTIVATE);

                HandleWindowMove(false);
                _isIngrid = true;
            }
            else
            {
                _isIngrid = false;
            }
        }
        public void HandleWindowMove(bool initWindow)
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
            int newWindowBottom = windowBottom;


            var workingArea = Screen.FromPoint(System.Windows.Forms.Control.MousePosition).WorkingArea;

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
            // Debug.WriteLine(windowBottom + " " + (workingArea.Bottom <= windowBottom));
            if (_isLeftButtonDown || initWindow)
            {
                POINT pt = new POINT { X = newWindowLeft, Y = newWindowTop };
                ScreenToClient(GetParent(hwnd), ref pt);
                windowTop = pt.Y;
                windowBottom = pt.Y + (windowBottom - windowTop);
                if (Math.Abs(windowTop - workingArea.Top) <= _snapDistance)
                {
                    newWindowTop = (int)workingArea.Top;
                    WindowBackground.CornerRadius = new CornerRadius(0, 0, 5, 5);
                    _isOnBottom = false;
                    _isOnTop = true;
                }
                else if (Math.Abs(windowBottom - workingArea.Bottom) - 2 <= _snapDistance
                   || (Math.Abs(windowBottom - workingArea.Bottom + Instance.Height - titleBar.Height) - 2 <= _snapDistance && initWindow)
                   )
                {
                    newWindowTop = (int)(workingArea.Bottom - (windowBottom - windowTop));
                    newWindowBottom = (int)workingArea.Bottom;
                    WindowBackground.CornerRadius = new CornerRadius(5, 5, 0, 0);
                    _isOnTop = false;
                    _isOnBottom = true;
                }
                else if (!_isOnBottom)
                {
                    _isOnTop = false;
                    WindowBackground.CornerRadius = new CornerRadius(5);
                    titleBar.CornerRadius = new CornerRadius(5, 5, 0, 0);
                }
                if (workingArea.Bottom <= windowBottom)
                {
                    newWindowBottom = (int)workingArea.Bottom;
                    WindowBackground.CornerRadius = new CornerRadius(5, 5, 0, 0);
                    _isOnTop = false;
                    _isOnBottom = true;
                }
                else if (_isLeftButtonDown)
                {
                    _isOnBottom = false;
                }
                Debug.WriteLine(windowRight);
                if (Math.Abs(windowLeft - workingArea.Left) <= _snapDistance)
                {
                    newWindowLeft = workingArea.Left;
                }
                else if (Math.Abs(workingArea.Right - windowRight) <= _snapDistance)
                {
                    newWindowLeft = (int)(workingArea.Right - this.ActualWidth);
                }
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
                    WonRight = otherWindow;
                    onLeft = true;
                    neighborFrameCount++;
                }
                else if (Math.Abs(windowRight - otherLeft) <= _snapDistance && Math.Abs(windowTop - otherTop) <= _snapDistance)
                {
                    newWindowLeft = otherLeft - (windowRight - windowLeft);
                    newWindowTop = otherTop;
                    WonLeft = otherWindow;
                    onRight = true;
                    neighborFrameCount++;
                }

                if (Math.Abs(windowLeft - otherRight) <= _snapDistance && Math.Abs(windowBottom - otherBottom) <= _snapDistance)
                {
                    newWindowLeft = otherRight;
                    newWindowBottom = (int)workingArea.Bottom;
                    WonRight = otherWindow;
                    onLeft = true;
                    neighborFrameCount++;
                }
                else if (Math.Abs(windowRight - otherLeft) <= _snapDistance && Math.Abs(windowBottom - otherBottom) <= _snapDistance)
                {
                    newWindowLeft = otherLeft - (windowRight - windowLeft);
                    newWindowBottom = (int)workingArea.Bottom;
                    WonLeft = otherWindow;
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
            }
            if (neighborFrameCount >= 2)
            {
                WindowBackground.CornerRadius = new CornerRadius(0);
                titleBar.CornerRadius = new CornerRadius(0);
            }
            if (neighborFrameCount == 0)
            {
                if (WonRight != null && !onLeft)
                {
                    if (!WonRight._isMinimized)
                    {
                        WonRight.WindowBorder.CornerRadius = new CornerRadius(
                            topLeft: WonRight._isOnTop ? 0 : WonRight.WonRight == null ? 5 : 0,
                            topRight: WonRight._isOnTop ? 0 : (WonRight._isOnBottom ? 0 : 5),
                            bottomRight: WonRight._isOnBottom ? 0 : 5,
                            bottomLeft: WonRight._isOnBottom ? 0 : 5
                        );
                        WonRight.titleBar.CornerRadius = new CornerRadius(
                            topLeft: WonRight.WindowBorder.CornerRadius.TopLeft,
                            topRight: WonRight.WindowBorder.CornerRadius.TopRight,
                            bottomRight: 0,
                            bottomLeft: 0
                        );
                    }
                    else
                    {
                        WonRight.WindowBorder.CornerRadius = new CornerRadius(
                            topLeft: WonRight._isOnTop ? 0 : WonRight.WonRight == null ? 5 : 0,
                            topRight: WonRight._isOnTop ? 0 : (WonRight._isOnBottom ? 0 : 5),
                            bottomRight: WonRight._isOnBottom ? 0 : 5,
                            bottomLeft: WonRight.WonRight == null ? (WonRight._isOnBottom ? 0 : 5) : 0
                        );
                        WonRight.titleBar.CornerRadius = WonRight.WindowBorder.CornerRadius;

                    }
                    WonRight.WindowBackground.CornerRadius = WonRight.WindowBorder.CornerRadius;
                    WonRight.WonLeft = null;
                    WonRight = null;
                }
                if (WonLeft != null && !onRight)
                {
                    if (!WonLeft._isMinimized)
                    {
                        WonLeft.WindowBorder.CornerRadius = new CornerRadius(
                            topLeft: WonLeft._isOnTop ? 0 : (WonLeft._isOnBottom ? 0 : 5),
                            topRight: WonLeft._isOnTop ? 0 : WonLeft.WonLeft == null ? 5 : 0,
                            bottomRight: WonLeft._isOnBottom ? 0 : 5,
                            bottomLeft: WonLeft._isOnBottom ? 0 : 5
                        );
                        WonLeft.titleBar.CornerRadius = new CornerRadius(
                            topLeft: WonLeft.WindowBorder.CornerRadius.TopLeft,
                            topRight: WonLeft.WindowBorder.CornerRadius.TopRight,
                            bottomRight: 0,
                            bottomLeft: 0
                        );
                    }
                    else
                    {
                        WonLeft.WindowBorder.CornerRadius = new CornerRadius(
                            topLeft: WonLeft._isOnTop ? 0 : (WonLeft._isOnBottom ? 0 : 5),
                            topRight: WonLeft._isOnTop ? 0 : WonLeft.WonLeft == null ? 5 : 0,
                            bottomRight: WonLeft.WonLeft == null ? (WonLeft._isOnBottom ? 0 : 5) : 0,
                            bottomLeft: WonLeft._isOnBottom ? 0 : 5
                        );
                        WonLeft.titleBar.CornerRadius = WonLeft.WindowBorder.CornerRadius;

                    }
                    WonLeft.WindowBackground.CornerRadius = WonLeft.WindowBorder.CornerRadius;
                    WonLeft.WonRight = null;
                    WonLeft = null;
                }

            }
            if (!_isMinimized)
            {
                if (_isOnBottom)
                {
                    WindowBorder.CornerRadius = new CornerRadius(
                        topLeft: 5,
                        topRight: 5,
                        bottomRight: 0,
                        bottomLeft: 0
                    );
                    WindowBackground.CornerRadius = WindowBorder.CornerRadius;
                    titleBar.CornerRadius = new CornerRadius(
                        topLeft: WindowBorder.CornerRadius.TopLeft,
                        topRight: WindowBorder.CornerRadius.TopRight,
                        bottomRight: 5,
                        bottomLeft: 5
                    );
                }
                else
                {
                    WindowBorder.CornerRadius = new CornerRadius(
                        topLeft: _isOnTop ? 0 : WonRight == null ? 5 : 0,
                        topRight: _isOnTop ? 0 : WonLeft == null ? 5 : 0,
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
            }
            else
            {
                if (_isOnBottom)
                {
                    WindowBorder.CornerRadius = new CornerRadius(
                        topLeft: WonRight == null ? 5 : 0,
                        topRight: WonLeft == null ? 5 : 0,
                        bottomRight: 0,
                        bottomLeft: 0
                   );
                }
                else
                {
                    WindowBorder.CornerRadius = new CornerRadius(
                        topLeft: _isOnTop ? 0 : WonRight == null ? 5 : 0,
                        topRight: _isOnTop ? 0 : WonLeft == null ? 5 : 0,
                        bottomRight: WonLeft == null ? 5 : 0,
                        bottomLeft: WonRight == null ? 5 : 0
                    );
                }
                WindowBackground.CornerRadius = WindowBorder.CornerRadius;
                titleBar.CornerRadius = WindowBorder.CornerRadius;
            }



            if ((initWindow && _isOnBottom) ||
                (!_isIngrid && !_isOnBottom
                    && (newWindowLeft != windowLeft || newWindowTop != windowTop || newWindowBottom != windowBottom && !_isLeftButtonDown)))
            {
                POINT pt = new POINT { X = newWindowLeft, Y = newWindowTop };
                ScreenToClient(GetParent(hwnd), ref pt);
                SetWindowPos(hwnd, IntPtr.Zero, pt.X, pt.Y, 0, 0,
                             SWP_NOZORDER | SWP_NOSIZE | SWP_NOACTIVATE);
            }

        }

        public void SetCornerRadius(Border border, double topLeft, double topRight, double bottomLeft, double bottomRight)
        {
            border.CornerRadius = new CornerRadius(topLeft, topRight, bottomLeft, bottomRight);
        }

        private void SetAsDesktopChild()
        {
            while (true)
            {
                while (shellView == IntPtr.Zero)
                {
                    EnumWindows((tophandle, _) =>
                    {
                        IntPtr shellViewIntPtr = FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (shellViewIntPtr != IntPtr.Zero)
                        {
                            shellView = shellViewIntPtr;
                            return false;
                        }
                        return true;
                    }, IntPtr.Zero);
                }
                if (shellView == IntPtr.Zero) Thread.Sleep(1000);
                else break;
            }
            if (shellView == IntPtr.Zero) throw new InvalidOperationException("SHELLDLL_DefView not found.");

            var interopHelper = new WindowInteropHelper(this);
            interopHelper.EnsureHandle();
            IntPtr hwnd = interopHelper.Handle;
            SetParent(hwnd, shellView);

            int style = (int)GetWindowLong(hwnd, GWL_STYLE);
            style &= ~WS_POPUP; // remove flag, to make sure it doesn't interfere
            style |= WS_CHILD; // add flag
            SetWindowLong(hwnd, GWL_STYLE, style);

            // convert coords to parent-relative coords
            uint dpi = GetDpiForWindow(hwnd);
            _windowsScalingFactor = dpi / 96.0;
            POINT pt = new POINT
            {
                X = (int)(Instance.PosX * _windowsScalingFactor),
                Y = (int)(Instance.PosY * _windowsScalingFactor)
            };
            ScreenToClient(shellView, ref pt);

            SetWindowPos(hwnd, IntPtr.Zero,
                         pt.X, pt.Y,
                         0, 0,
                         SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        public async Task AdjustPositionAsync()
        {
            _adjustPositionCts?.Cancel();
            if (isMouseDown) return;

            _adjustPositionCts = new CancellationTokenSource();
            var token = _adjustPositionCts.Token;
            var interopHelper = new WindowInteropHelper(this);
            interopHelper.EnsureHandle();
            IntPtr hwnd = interopHelper.Handle;
            double posX = Instance.PosX;
            double posY = Instance.PosY;

            try
            {
                await Task.Run(() =>
                {
                    if (token.IsCancellationRequested) return;

                    uint dpi = GetDpiForWindow(hwnd);
                    _windowsScalingFactor = dpi / 96.0;

                    POINT pt = new POINT
                    {
                        X = (int)(posX * _windowsScalingFactor),
                        Y = (int)(posY * _windowsScalingFactor)
                    };

                    if (token.IsCancellationRequested) return;
                    ScreenToClient(shellView, ref pt);

                    SetWindowPos(hwnd, IntPtr.Zero,
                                 pt.X, pt.Y,
                                 0, 0,
                                 SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                }, token);
            }
            catch { }
        }
        public async void AdjustPosition()
        {
            SetParent(hwnd, IntPtr.Zero);
            SetAsDesktopChild();
            if (Instance.Minimized)
            {
                this.Height = titleBar.Height;
            }
            var interopHelper = new WindowInteropHelper(this);
            interopHelper.EnsureHandle();
            IntPtr _hwnd = interopHelper.Handle;
            double currentScale = GetDpiForWindow(_hwnd) / 96.0;
            if (_windowsScalingFactor != currentScale)
            {
                _windowsScalingFactor = currentScale;
                foreach (var item in FileItems)
                {
                    item.Thumbnail = await GetThumbnailAsync(item.FullPath!);
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
            if (e.Key == Key.Escape || !_mouseIsOver)
            {
                FilterTextBox.Text = null;
                FilterTextBox.Visibility = Visibility.Collapsed;
            }
            else if (e.Key != Key.Escape)
            {
                FilterTextBox.Visibility = Visibility.Visible;
                Search.Visibility = Visibility.Visible;
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
            else if (_mouseIsOver)
            {
                Search.Visibility = Visibility.Visible;
                title.Visibility = Visibility.Collapsed;
            }


            if (_collectionView == null)
                return;

            string filter = _mouseIsOver ? FilterTextBox.Text : "";
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
            WindowChrome.SetWindowChrome(this, Instance.IsLocked ?
                new WindowChrome
                {
                    ResizeBorderThickness = new Thickness(0),
                    CaptionHeight = 0,
                    CornerRadius = new CornerRadius(0)
                } :
                new WindowChrome
                {
                    GlassFrameThickness = new Thickness(0),
                    CaptionHeight = 0,
                    ResizeBorderThickness = new Thickness(5),
                    CornerRadius = new CornerRadius(0)
                }
            );
            KeepWindowBehind();
            SetAsDesktopChild();
            SetNoActivate();
            SetAsToolWindow();
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(WndProc);
            MouseLeaveWindow(false);
            FileListView.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
        }
        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (FileListView.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                foreach (var item in FileListView.Items)
                {
                    var container = FileListView.ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
                    if (container != null)
                    {
                        container.MouseEnter += ListViewItem_MouseEnter;
                        container.MouseLeave += ListViewItem_MouseLeave;
                        container.Selected += ListViewItem_Selected;
                        container.Unselected += ListViewItem_Unselected;
                    }
                }
            }
        }
        public DeskFrameWindow(Instance instance)
        {
            InitializeComponent();
            this.MinWidth = 98;
            this.Loaded += MainWindow_Loaded;
            this.SourceInitialized += MainWindow_SourceInitialized!;
            hwnd = new WindowInteropHelper(this).Handle;
            this.StateChanged += (sender, args) =>
            {
                this.WindowState = WindowState.Normal;
            };

            Instance = instance;
            this.Width = instance.Width;
            this.Opacity = Instance.IdleOpacity;
            _currentFolderPath = instance.Folder;
            _isLocked = instance.IsLocked;
            _oriPosX = (int)instance.PosX;
            _oriPosY = (int)instance.PosY;
            this.Top = instance.PosY;
            this.Left = instance.PosX;

            title.FontSize = Instance.TitleFontSize;
            title.TextWrapping = TextWrapping.Wrap;
            double titleBarHeight = Math.Max(30, Instance.TitleFontSize * 1.5);
            titleBar.Height = titleBarHeight;

            double scrollViewerMargin = titleBarHeight + 5;
            scrollViewer.Margin = new Thickness(0, scrollViewerMargin, 0, 0);

            titleBar.Cursor = _isLocked ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.SizeAll;
            title.Cursor = _isLocked ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.SizeAll;
            frameTypeSymbol.Cursor = _isLocked ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.SizeAll;
            titleStackPanel.Cursor = _isLocked ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.SizeAll;

            if ((int)instance.Height <= titleBar.Height) _isMinimized = true;
            if (instance.Minimized)
            {
                _isMinimized = instance.Minimized;
                this.Height = titleBarHeight;
            }
            else
            {
                this.Height = instance.Height;
            }
            titleStackPanel.MouseEnter += (s, e) => AnimateSymbolIcon(frameTypeSymbol, Instance.TitleFontSize, 1, 5);
            titleStackPanel.MouseLeave += (s, e) => AnimateSymbolIcon(frameTypeSymbol, 0, 0, 0);

            _checkForChages = true;
            FileItems = new ObservableCollection<FileItem>();
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeskFrame"
            );

            if (instance.Folder.StartsWith(appDataPath, StringComparison.OrdinalIgnoreCase))
            {
                Instance.IsShortcutsOnly = true;
                Instance.ShowShortcutArrow = false;
            }
            if (instance.Folder == "empty")
            {
                showFolder.Visibility = Visibility.Hidden;
                addFolder.Visibility = Visibility.Visible;
            }
            else
            {
                LoadingProgressRing.Visibility = Visibility.Visible;
                LoadFiles(instance.Folder);
                title.Text = Instance.TitleText ?? Instance.Name;

                DataContext = this;
                InitializeFileWatcher();
            }
            if (Instance.SnapWidthToIconWidth_PlusScrollbarWidth)
            {
                FileWrapPanel.Margin = new Thickness(6, 5, 0, 5);
            }
            else
            {
                FileWrapPanel.Margin = new Thickness(0, 0, 0, 0);
            }

            _collectionView = CollectionViewSource.GetDefaultView(FileItems);
            _originalHeight = Instance.Height;
            titleBar.Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(Instance.TitleBarColor));
            title.Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(Instance.TitleTextColor));
            titleBarIcons.Opacity = Instance.HideTitleBarIconsWhenInactive ? 0 : 1;

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
        private void AnimateSymbolIcon(UIElement target, double widthTo, double opacityTo, double marginTo)
        {
            var marginAnimation = new ThicknessAnimation
            {
                To = new Thickness(0, 0, marginTo, 0),
                Duration = TimeSpan.FromSeconds(0.1),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            var widthAnimation = new DoubleAnimation
            {
                To = widthTo,
                Duration = TimeSpan.FromSeconds(0.1),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            var opacityAnimation = new DoubleAnimation
            {
                To = opacityTo,
                Duration = TimeSpan.FromSeconds(0.1),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            target.BeginAnimation(FrameworkElement.MarginProperty, marginAnimation);
            target.BeginAnimation(FrameworkElement.WidthProperty, widthAnimation);
            target.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
        }

        private void AnimateChevron(bool flip, bool onLoad, double animationSpeed)
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
                duration = (int)(200 / animationSpeed);
            }
            if (_isLocked) duration = (int)(200 / animationSpeed);

            var rotateAnimation = new DoubleAnimation
            {
                From = rotateTransform.Angle,
                To = angleToAnimateTo,
                Duration = (animationSpeed == 0) ?
                    TimeSpan.FromMilliseconds(40) :
                    TimeSpan.FromMilliseconds(duration),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            _canAnimate = false;
            rotateAnimation.Completed += (s, e) => _canAnimate = true;

            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
        }

        private void Minimize_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            AnimateChevron(_isMinimized, false, Instance.AnimationSpeed);
            if (showFolder.Visibility == Visibility.Hidden && showFolderInGrid.Visibility == Visibility.Hidden)
            {
                return;
            }
            if (!_isMinimized)
            {
                _originalHeight = this.ActualHeight;
                _isMinimized = true;
                Instance.Minimized = true;
                // Debug.WriteLine("minimize: " + Instance.Height);
                AnimateWindowHeight(titleBar.Height, Instance.AnimationSpeed);
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

                // Debug.WriteLine("unminimize: " + Instance.Height);
                AnimateWindowHeight(Instance.Height, Instance.AnimationSpeed);
            }
            HandleWindowMove(false);
        }

        private void ToggleFileExtension_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ToggleFileExtension();
            LoadFiles(_currentFolderPath);
            UpdateFileExtensionIcon();
        }

        private void ToggleHiddenFiles_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ToggleHiddenFiles();
            LoadFiles(_currentFolderPath);
            UpdateHiddenFilesIcon();
        }
        private void OpenFolder()
        {
            try
            {
                Process.Start(new ProcessStartInfo(_currentFolderPath) { UseShellExecute = true });
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
        public void AnimateWindowOpacity(double value, double animationSpeed)
        {
            var animation = new DoubleAnimation
            {
                To = value,
                Duration = animationSpeed == 0 ?
                    TimeSpan.FromSeconds(0.1) :
                    TimeSpan.FromSeconds(0.2 / animationSpeed),
            };
            this.BeginAnimation(OpacityProperty, animation);
        }
        private void AnimateActiveColor(double animationSpeed)
        {
            if (Instance.ActiveBackgroundEnabled || Instance.ActiveBorderEnabled || Instance.ActiveTitleTextEnabled)
            {
                _mouseIsOver = IsCursorWithinWindowBounds();
            }
            if (Instance.ActiveBorderEnabled)
            {
                if (!Instance.BorderEnabled)
                {
                    WindowBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00000000"));
                    WindowBorder.BorderThickness = new Thickness(1.3);
                }
                var backgroundColorAnimation = new ColorAnimation
                {
                    From = _mouseIsOver ? !Instance.BorderEnabled
                                            ? (Color)ColorConverter.ConvertFromString("#00000000")
                                            : (Color)ColorConverter.ConvertFromString(Instance.BorderColor)
                                        : (Color)ColorConverter.ConvertFromString(Instance.ActiveBorderColor),

                    To = _mouseIsOver ? (Color)ColorConverter.ConvertFromString(Instance.ActiveBorderColor)
                                        : !Instance.BorderEnabled
                                            ? (Color)ColorConverter.ConvertFromString("#00000000")
                                            : (Color)ColorConverter.ConvertFromString(Instance.BorderColor),

                    Duration = animationSpeed == 0 ? TimeSpan.FromSeconds(0)
                                                   : TimeSpan.FromSeconds(0.2 / animationSpeed)
                };
                WindowBorder.BorderBrush.BeginAnimation(SolidColorBrush.ColorProperty, backgroundColorAnimation);
                backgroundColorAnimation.Completed += (sender, e) =>
                {
                    WindowBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Instance.BorderColor));

                    WindowBorder.SetBinding(Border.BorderThicknessProperty, new Binding("Instance.BorderEnabled")
                    {
                        Source = this,
                        Converter = (IValueConverter)Resources["BooleanToBorderThicknessConverter"]
                    });
                };
            }
            else
            {
                WindowBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Instance.BorderColor));

                WindowBorder.SetBinding(Border.BorderThicknessProperty, new Binding("Instance.BorderEnabled")
                {
                    Source = this,
                    Converter = (IValueConverter)Resources["BooleanToBorderThicknessConverter"]
                });
            }

            if (Instance.ActiveBackgroundEnabled)
            {
                var borderColorAnimation = new ColorAnimation
                {
                    From = _mouseIsOver ? (Color)ColorConverter.ConvertFromString(Instance.ListViewBackgroundColor)
                                        : (Color)ColorConverter.ConvertFromString(Instance.ActiveBackgroundColor),
                    To = _mouseIsOver ? (Color)ColorConverter.ConvertFromString(Instance.ActiveBackgroundColor)
                                        : (Color)ColorConverter.ConvertFromString(Instance.ListViewBackgroundColor),
                    Duration = animationSpeed == 0 ? TimeSpan.FromSeconds(0)
                                                   : TimeSpan.FromSeconds(0.2 / animationSpeed)
                };
                WindowBackground.Background.BeginAnimation(SolidColorBrush.ColorProperty, borderColorAnimation);
            }
            if (Instance.ActiveTitleTextEnabled)
            {
                var titleBarItemsColorAnimation = new ColorAnimation
                {
                    From = _mouseIsOver ? (Color)ColorConverter.ConvertFromString(Instance.TitleTextColor)
                                       : (Color)ColorConverter.ConvertFromString(Instance.ActiveTitleTextColor),
                    To = _mouseIsOver ? (Color)ColorConverter.ConvertFromString(Instance.ActiveTitleTextColor)
                                       : (Color)ColorConverter.ConvertFromString(Instance.TitleTextColor),
                    Duration = animationSpeed == 0 ? TimeSpan.FromSeconds(0)
                                                  : TimeSpan.FromSeconds(0.2 / animationSpeed)
                };
                title.Foreground.BeginAnimation(SolidColorBrush.ColorProperty, titleBarItemsColorAnimation);
            }
        }

        private void AnimateWindowHeight(double targetHeight, double animationSpeed)
        {
            double currentHeight = this.ActualHeight;

            var freezeAnimation = new DoubleAnimation
            {
                To = currentHeight,
                Duration = TimeSpan.Zero,
                FillBehavior = FillBehavior.HoldEnd
            };
            this.BeginAnimation(HeightProperty, freezeAnimation);

            var animation = new DoubleAnimation
            {
                To = targetHeight,
                Duration = animationSpeed == 0 ?
                    TimeSpan.FromSeconds(0) :
                    TimeSpan.FromSeconds(0.2 / animationSpeed),
                EasingFunction = new QuadraticEase()
            };
            animation.Completed += (s, e) =>
            {
                _canAnimate = true;
                if (targetHeight == titleBar.Height)
                {
                    scrollViewer.ScrollToTop();
                }
                //WindowChrome.SetWindowChrome(this, Instance.IsLocked ?
                //new WindowChrome
                //{
                //    ResizeBorderThickness = new Thickness(0),
                //    CaptionHeight = 0
                //}
                //: _isOnBottom ?
                //    new WindowChrome
                //    {
                //        GlassFrameThickness = new Thickness(5),
                //        CaptionHeight = 0,
                //        ResizeBorderThickness = new Thickness(0, Instance.Minimized ? 0 : 5, 5, 0),
                //        CornerRadius = new CornerRadius(5)
                //    } :
                //    new WindowChrome
                //    {
                //        GlassFrameThickness = new Thickness(5),
                //        CaptionHeight = 0,
                //        ResizeBorderThickness = new Thickness(5, 0, 5, Instance.Minimized ? 0 : 5),
                //        CornerRadius = new CornerRadius(5)
                //    }
                // );
            };
            _canAnimate = false;
            this.BeginAnimation(HeightProperty, animation);
        }

        public void InitializeFileWatcher()
        {
            _fileWatcher = null;
            _fileWatcher = new FileSystemWatcher(_currentFolderPath)
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
                LoadFiles(_currentFolderPath);
            });
        }
        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"File renamed: {e.OldFullPath} to {e.FullPath}");
                var renamedItem = FileItems.FirstOrDefault(item => item.FullPath == e.OldFullPath);

                if (renamedItem != null)
                {
                    renamedItem.FullPath = e.FullPath;

                    string fileName = Path.GetFileName(e.FullPath);
                    Debug.WriteLine("FILENAME:: " + fileName);
                    if (renamedItem is FileInfo)
                    {
                        Debug.WriteLine("NOT");
                        string actualExt = Path.GetExtension(fileName);
                        renamedItem.Name = Instance.ShowFileExtension || string.IsNullOrEmpty(actualExt)
                             ? fileName
                             : fileName.Substring(0, fileName.Length - actualExt.Length);
                    }
                    else
                    {
                        Debug.WriteLine("FOLDER");
                        renamedItem.Name = fileName;
                    }
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
        private void ToggleIsLocked()
        {
            Instance.IsLocked = !Instance.IsLocked;
            var interopHelper = new WindowInteropHelper(this);
            interopHelper.EnsureHandle();
            IntPtr hwnd = interopHelper.Handle;
            SetParent(hwnd, IntPtr.Zero);
            WindowChrome.SetWindowChrome(this, Instance.IsLocked ?
                new WindowChrome
                {
                    ResizeBorderThickness = new Thickness(0),
                    CaptionHeight = 0,
                    CornerRadius = new CornerRadius(0)
                } :
                new WindowChrome
                {
                    GlassFrameThickness = new Thickness(0),
                    CaptionHeight = 0,
                    ResizeBorderThickness = new Thickness(5),
                    CornerRadius = new CornerRadius(0)
                }
            );
            SetAsDesktopChild();
            HandleWindowMove(true);

            this.Width += 1;
            this.Width -= 1;
        }
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
                LoadingProgressRingFade(true);

                var fileEntries = await Task.Run(() =>
                {
                    if (loadFiles_cts.IsCancellationRequested)
                    {
                        LoadingProgressRingFade(false);
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
                    LoadingProgressRingFade(false);
                    return;
                }
                if (Instance.LastAccesedToFirstRow)
                {
                    var wrapPanel = FindParentOrChild<WrapPanel>(FileWrapPanel);
                    if (wrapPanel != null)
                    {
                        double itemWidth = wrapPanel.ItemWidth;
                        ItemPerRow = (int)((this.Width) / itemWidth);
                    }
                    _previousItemPerRow = ItemPerRow;
                }
                fileEntries = await SortFileItemsToList(fileEntries, (int)Instance.SortBy, Instance.FolderOrder);
                var fileNames = new HashSet<string>(fileEntries.Select(f => f.Name));


                await Dispatcher.InvokeAsync(async () =>
                {
                    if (loadFiles_cts.IsCancellationRequested)
                    {
                        LoadingProgressRingFade(false);
                        return;
                    }
                    for (int i = FileItems.Count - 1; i >= 0; i--)  // Remove item that no longer exist
                    {
                        if (loadFiles_cts.IsCancellationRequested)
                        {
                            LoadingProgressRingFade(false);
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
                        {
                            LoadingProgressRingFade(false);
                            return;
                        }

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
                        bool isFile = entry is FileInfo;
                        string actualExt = isFile ? Path.GetExtension(entry.Name) : string.Empty;
                        if (existingItem == null)
                        {
                            if (!string.IsNullOrEmpty(Instance.FileFilterHideRegex) &&
                                new Regex(Instance.FileFilterHideRegex).IsMatch(entry.Name))
                            {
                                continue;
                            }

                            FileItems.Add(new FileItem
                            {
                                Name = Instance.ShowFileExtension || string.IsNullOrEmpty(actualExt)
                                    ? entry.Name
                                    : entry.Name.Substring(0, entry.Name.Length - actualExt.Length),
                                FullPath = entry.FullName,
                                IsFolder = !isFile,
                                DateModified = entry.LastWriteTime,
                                DateCreated = entry.CreationTime,
                                FileType = isFile ? actualExt : string.Empty,
                                ItemSize = (int)size,
                                DisplaySize = displaySize,
                                Thumbnail = thumbnail
                            });
                        }
                        else
                        {
                            existingItem.Name = Instance.ShowFileExtension || string.IsNullOrEmpty(actualExt)
                                    ? entry.Name
                                    : entry.Name.Substring(0, entry.Name.Length - actualExt.Length);
                            existingItem.FullPath = entry.FullName;
                            existingItem.IsFolder = string.IsNullOrEmpty(Path.GetExtension(entry.FullName));
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
                    if (Instance.LastAccesedToFirstRow)
                    {
                        FirstRowByLastAccessed(FileItems, Instance.LastAccessedFiles, ItemPerRow);
                    }
                    _lastUpdated = DateTime.Now;
                    int hiddenCount = Int32.Parse(_fileCount) - (FileItems.Count - _folderCount);
                    if (hiddenCount > 0)
                    {
                        _fileCount += $" ({hiddenCount} hidden)";
                    }
                    SortItems();
                    await Task.Run(async () =>
                    {
                        await Task.Delay(200);
                        Dispatcher.Invoke(() =>
                        {
                            LoadingProgressRingFade(false);
                        });
                    });
                    Debug.WriteLine("LOADEDDDDDDDD");
                });
            }
            catch (OperationCanceledException)
            {
                LoadingProgressRingFade(false);
                Debug.WriteLine("LoadFiles was canceled.");
            }
        }
        private void LoadingProgressRingFade(bool showLoading)
        {
            Storyboard fadeOut = (Storyboard)this.Resources["FadeOutLoadingProgressRingStoryboard"];
            Storyboard fadeIn = (Storyboard)this.Resources["FadeInLoadingProgressRingStoryboard"];

            if (showLoading)
            {
                LoadingProgressRing.IsIndeterminate = true;
                fadeIn.Begin();
            }
            else
            {
                fadeOut.Completed += (s, e) =>
                {
                    fadeOut.Completed -= (s, e) => { }; // cleanup
                    LoadingProgressRing.IsIndeterminate = false;
                };
                fadeOut.Begin();
            }
        }

        public void TitleBarIconsFadeAnimation(bool show)
        {
            Storyboard fadeIn = (Storyboard)this.Resources["FadeIn_titleBarIcons_Storyboard"];
            Storyboard fadeOut = (Storyboard)this.Resources["FadeOut_titleBarIcons_Storyboard"];

            if (show)
            {
                fadeIn.Begin();
            }
            else
            {
                fadeOut.Completed += (s, e) =>
                {
                    fadeOut.Completed -= (s, e) => { }; // cleanup
                };
                fadeOut.Begin();
            }
        }
        public void SortItems()
        {
            var sortedList = SortFileItems(FileItems, (int)Instance.SortBy, Instance.FolderOrder);

            if (Instance.LastAccesedToFirstRow)
            {
                FirstRowByLastAccessed(sortedList, Instance.LastAccessedFiles, ItemPerRow);
            }
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
                _contextMenuIsOpen = true;
                scm.ContextMenuClosed += () =>
                {
                    _contextMenuIsOpen = false;
                };
                scm.ShowContextMenu(windowHelper.Handle, files, drawingPoint);
            }
        }
        private void Window_Drop(object sender, DragEventArgs e)
        {
            _dragdropIntoFolder = false;
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
                    string destinationPath = Path.Combine(_currentFolderPath, Path.GetFileName(file));
                    if (Path.GetDirectoryName(file) == _currentFolderPath &&
                        _dragdropIntoFolder &&
                        string.IsNullOrEmpty(_currentFolderPath) &&
                        _currentFolderPath == "empty")
                    {
                        Debug.WriteLine("Dropped into invalid path, returning.");
                        return;
                    }
                    try
                    {
                        if (Directory.Exists(file))
                        {

                            Debug.WriteLine("Folder detected: " + file);
                            if (_currentFolderPath == "empty")
                            {
                                _currentFolderPath = file;
                                title.Text = Path.GetFileName(_currentFolderPath);
                                Instance.Folder = file;
                                Instance.Name = Path.GetFileName(_currentFolderPath);
                                MainWindow._controller.WriteInstanceToKey(Instance);
                                LoadFiles(_currentFolderPath);
                                DataContext = this;
                                InitializeFileWatcher();
                                showFolder.Visibility = Visibility.Visible;
                                LoadingProgressRing.Visibility = Visibility.Visible;
                                addFolder.Visibility = Visibility.Hidden;

                            }

                            if (!Instance.IsShortcutsOnly)
                            {
                                Directory.Move(file,
                                  !string.IsNullOrEmpty(_dropIntoFolderPath)
                                      ? Path.Combine(_dropIntoFolderPath, Path.GetFileName(destinationPath))
                                      : destinationPath);
                            }
                            else
                            {
                                CreateShortcut(file, _currentFolderPath);
                            }
                        }
                        else
                        {
                            Debug.WriteLine("File detected: " + file);
                            if (!Instance.IsShortcutsOnly)
                            {
                                File.Move(file,
                                    !string.IsNullOrEmpty(_dropIntoFolderPath)
                                        ? Path.Combine(_dropIntoFolderPath, Path.GetFileName(destinationPath))
                                        : destinationPath);
                            }
                            else
                            {
                                CreateShortcut(file, _currentFolderPath);

                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error moving file: " + ex.Message);
                        if (addFolder.Visibility == Visibility.Visible)
                        {

                            Instance.Folder = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                "DeskFrame",
                                Guid.NewGuid().ToString()
                            );
                            _currentFolderPath = Instance.Folder;
                            Directory.CreateDirectory(Instance.Folder);
                            Instance.IsShortcutsOnly = true;
                            Instance.ShowShortcutArrow = false;
                            CreateShortcut(file, _currentFolderPath);

                            title.Text = "File frame";
                            Instance.TitleText = "File frame";
                            Instance.Name = Path.GetFileName(Instance.Folder);
                            MainWindow._controller.WriteInstanceToKey(Instance);
                            LoadFiles(_currentFolderPath);
                            DataContext = this;
                            InitializeFileWatcher();
                            showFolder.Visibility = Visibility.Visible;
                            LoadingProgressRing.Visibility = Visibility.Visible;
                            addFolder.Visibility = Visibility.Hidden;

                        }
                    }
                }
            }
        }


        void CreateShortcut(string filePath, string shortcutFolder = null)
        {
            if (Path.GetExtension(filePath) == ".url")
            {
                File.Copy(filePath, Path.Combine(shortcutFolder, Path.GetFileName(filePath)));
                return;
            }
            string folder = !string.IsNullOrEmpty(shortcutFolder) ? shortcutFolder : Path.GetDirectoryName(filePath);
            string shortcutPath = Path.Combine(folder, Path.GetFileNameWithoutExtension(filePath) + ".lnk");

            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

            shortcut.TargetPath = filePath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(filePath);
            try
            {
                shortcut.Description = Path.GetFileName(filePath);
            }
            catch
            {
            }
            shortcut.Save();
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
                    if (Instance.FolderOpenInsideFrame && clickedItem.IsFolder)
                    {
                        _currentFolderPath = clickedItem.FullPath;
                        PathToBackButton.Visibility = _currentFolderPath == Instance.Folder
                            ? Visibility.Collapsed : Visibility.Visible;
                        InitializeFileWatcher();
                        FileItems.Clear();
                        LoadFiles(clickedItem.FullPath);
                    }
                    else
                    {
                        Process.Start(new ProcessStartInfo(clickedItem.FullPath!) { UseShellExecute = true });
                    }
                    if (Instance.LastAccesedToFirstRow)
                    {
                        var fileId = GetFileId(clickedFileItem.FullPath!).ToString();
                        var newList = new List<string>(Instance.LastAccessedFiles);
                        newList.Remove(fileId);
                        newList.Insert(0, fileId);
                        Instance.LastAccessedFiles = newList;
                        var wrapPanel = FindParentOrChild<WrapPanel>(FileWrapPanel);
                        if (wrapPanel != null)
                        {
                            double itemWidth = wrapPanel.ItemWidth;
                            ItemPerRow = (int)((this.Width) / itemWidth);
                        }
                        FirstRowByLastAccessed(FileItems, Instance.LastAccessedFiles, ItemPerRow);
                    }
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
                _contextMenuIsOpen = true;
                scm.ContextMenuClosed += () =>
                {
                    _contextMenuIsOpen = false;
                };
                scm.ShowContextMenu(windowHelper.Handle, files, drawingPoint);
            }
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            if (_isMinimized)
            {
                AnimateWindowHeight(titleBar.Height, Instance.AnimationSpeed);
            }
            AnimateWindowOpacity(Instance.IdleOpacity, Instance.AnimationSpeed);
            _dragdropIntoFolder = false;
        }
        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            AnimateWindowHeight(Instance.Height, Instance.AnimationSpeed); AnimateWindowOpacity(1, Instance.AnimationSpeed);
            var sourceElement = e.OriginalSource as DependencyObject;
            var currentBorder = new Border();
            if (showFolderInGrid.Visibility == Visibility.Visible)
            {
                currentBorder = sourceElement as Border ?? FindParentOrChild<Border>(sourceElement);
            }
            else
            {
                currentBorder = sourceElement as Border ?? FindParent<Border>(sourceElement);
            }
            _dragdropIntoFolder = true;
            if (currentBorder != _lastBorder)
            {
                if (_lastBorder != null)
                {
                    FileItem_MouseLeave(_lastBorder, null);
                }
                _lastBorder = currentBorder;
            }
            if (currentBorder != null)
            {
                FileItem_MouseEnter(currentBorder, null);
            }
        }
        private T? FindParentOrChild<T>(DependencyObject element) where T : DependencyObject
        {
            if (element is T targetElement) return targetElement;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                if (child is T childElement) return childElement;

                var nestedChild = FindParentOrChild<T>(child);
                if (nestedChild != null) return nestedChild;
            }
            return FindParent<T>(element);
        }

        private T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }
        private void ListViewItem_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is FileItem fileItem)
            {
                fileItem.IsSelected = true;
            }
        }
        private void ListViewItem_Unselected(object sender, RoutedEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is FileItem fileItem)
            {
                fileItem.IsSelected = false;
                var sourceElement = e.OriginalSource as DependencyObject;
                var currentBorder = sourceElement as Border ?? FindParentOrChild<Border>(sourceElement);
                if (currentBorder != null) currentBorder.Background = Brushes.Transparent;
            }
        }
        private void ListViewItem_MouseEnter(object sender, MouseEventArgs e)
        {
            _dropIntoFolderPath = "";

            if (sender is ListViewItem item && item.DataContext is FileItem fileItem)
            {
                var sourceElement = e.OriginalSource as DependencyObject;
                var currentBorder = sourceElement as Border ?? FindParentOrChild<Border>(sourceElement);

                if (currentBorder != null)
                {
                    if (!fileItem.IsSelected)
                    {
                        currentBorder.Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255));
                    }
                }
            }
        }
        private void ListViewItem_MouseLeave(object sender, MouseEventArgs e)
        {
            _dropIntoFolderPath = "";
            if (sender is ListViewItem item && item.DataContext is FileItem fileItem)
            {
                var sourceElement = e.OriginalSource as DependencyObject;
                var currentBorder = sourceElement as Border ?? FindParentOrChild<Border>(sourceElement);

                if (currentBorder != null)
                {
                    if (!fileItem.IsSelected)
                    {
                        currentBorder.Background = Brushes.Transparent;
                    }
                }
            }
        }
        private void FileItem_MouseEnter(object sender, MouseEventArgs? e)
        {
            if (sender is Border border && border.DataContext is FileItem fileItem)
            {
                if (_dragdropIntoFolder && fileItem.IsFolder)
                {
                    _dropIntoFolderPath = fileItem.FullPath + "\\";
                    if (showFolderInGrid.Visibility == Visibility.Visible)
                    {
                        border.Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255));
                    }
                    else
                    {
                        fileItem.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
                    }
                }
                else if (!_dragdropIntoFolder)
                {
                    if (showFolderInGrid.Visibility == Visibility.Visible)
                    {
                        border.Background = fileItem.IsSelected ? new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)) : Brushes.Transparent;
                    }
                    else
                    {
                        fileItem.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
                    }
                }
                if (showFolderInGrid.Visibility == Visibility.Visible && !fileItem.IsSelected && fileItem.IsFolder)
                {
                    border.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
                }
            }
        }

        private void FileItem_MouseLeave(object sender, MouseEventArgs? e)
        {
            if (sender is Border border && border.DataContext is FileItem fileItem)
            {
                _dropIntoFolderPath = "";
                if (!fileItem.IsSelected)
                {
                    fileItem.Background = fileItem.IsSelected ? new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)) : Brushes.Transparent;
                }
                else
                {
                    fileItem.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
                }
                if (showFolderInGrid.Visibility == Visibility.Visible && !fileItem.IsSelected /*&& !fileItem.IsFolder*/)
                {
                    border.Background = fileItem.IsSelected ? new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)) : Brushes.Transparent;
                }
            }
        }

        public BitmapSource? GetThumbnail(string filePath, int size)
        {
            try
            {
                ShellObject shellObject = ShellObject.FromParsingName(filePath);
                ShellThumbnail shellThumbnail = shellObject.Thumbnail;
                shellThumbnail.CurrentSize = new System.Windows.Size(size, size);
                BitmapSource thumbnail = shellThumbnail.BitmapSource;
                thumbnail.Freeze();
                return thumbnail;
            }
            catch
            {
                return null;
            }
        }

        private async Task<BitmapSource?> GetThumbnailAsync(string path)
        {
            return await Task.Run(async () =>
            {
                if (string.IsNullOrWhiteSpace(path) || (!File.Exists(path) && !Directory.Exists(path)))
                {
                    Console.WriteLine("Invalid path: " + path);
                    return null;
                }
                IntPtr hBitmap = IntPtr.Zero;
                BitmapSource? thumbnail = null;
                int iconSize = (int)(Instance.IconSize * _windowsScalingFactor);
                if (Path.GetExtension(path).ToLower() == ".svg")
                {
                    thumbnail = await LoadSvgThumbnailAsync(path, iconSize);
                    return thumbnail;
                }
                string ext = Path.GetExtension(path).ToLowerInvariant();
                bool isLink = ext == ".lnk" || ext == ".url";

                if (isLink)
                {
                    try
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            thumbnail = GetThumbnail(path, iconSize);
                        });
                        if (Instance.ShowShortcutArrow)
                        {
                            return Application.Current.Dispatcher.Invoke(() =>
                            {
                                IntPtr[] overlayIcons = new IntPtr[1];
                                int overlayExtracted = ExtractIconEx(
                                    Environment.SystemDirectory + "\\shell32.dll",
                                    29,
                                    overlayIcons,
                                    null,
                                    1);

                                if (overlayExtracted > 0 && overlayIcons[0] != IntPtr.Zero)
                                {
                                    var overlay = Imaging.CreateBitmapSourceFromHIcon(
                                                  overlayIcons[0],
                                                  Int32Rect.Empty,
                                                  BitmapSizeOptions.FromEmptyOptions());
                                    DestroyIcon(overlayIcons[0]);

                                    var visual = new DrawingVisual();
                                    using (var dc = visual.RenderOpen())
                                    {
                                        Debug.WriteLine("iconsize: " + iconSize);
                                        double scale = iconSize / Math.Max(thumbnail.PixelWidth, thumbnail.PixelHeight);
                                        double thumbnailWidth = thumbnail.PixelWidth * scale;
                                        double thumbnailHeight = thumbnail.PixelHeight * scale;

                                        double thumbnailX = (iconSize - thumbnailWidth) / 2.0;
                                        double thumbnailY = (iconSize - thumbnailHeight) / 2.0;

                                        dc.DrawImage(
                                            thumbnail,
                                            new Rect(
                                                thumbnailX,
                                                thumbnailY,
                                                thumbnailWidth,
                                                thumbnailHeight)
                                        );
                                        double overlayScale = (iconSize < 32 ? iconSize / 32.0 : 1.0);
                                        if (_windowsScalingFactor != 1.0)
                                        {
                                            overlayScale *= (1 / _windowsScalingFactor);
                                        }
                                        if (overlayScale != 1.0)
                                        {
                                            overlay = new TransformedBitmap(overlay, new ScaleTransform(overlayScale, overlayScale));
                                            overlay.Freeze();
                                        }
                                        double overlayX = thumbnailX;
                                        double overlayY = thumbnailY + thumbnailHeight - overlay.PixelHeight;
                                        dc.DrawImage(overlay,
                                            new Rect(
                                            overlayX,
                                            overlayY,
                                            overlay.PixelWidth,
                                            overlay.PixelHeight)
                                        );
                                    }

                                    var rtb = new RenderTargetBitmap(
                                        iconSize,
                                        iconSize,
                                        thumbnail.DpiX,
                                        thumbnail.DpiY,
                                        PixelFormats.Pbgra32);
                                    rtb.Render(visual);
                                    rtb.Freeze();
                                    return rtb;
                                }
                                return thumbnail;
                            });
                        }
                        return thumbnail;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }
                else
                {
                    int attempt = 0;
                    while (attempt < 3 && thumbnail == null)
                    {
                        ShellObject? shellObj = null;
                        shellObj = Directory.Exists(path) ? ShellObject.FromParsingName(path) : ShellFile.FromFilePath(path);
                        if (shellObj != null)
                        {
                            try
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    thumbnail = GetThumbnail(path, iconSize);
                                });
                                if (thumbnail != null)
                                {
                                    return thumbnail;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Failed to fetch thumbnail:" + ex.Message);
                            }
                            finally
                            {
                                shellObj?.Dispose();
                            }
                        }
                        attempt++;
                    }
                }
                if (thumbnail != null)
                {
                    return thumbnail;
                }

                Debug.WriteLine("Failed to retrieve thumbnail after 3 attempts.");
                return null;
            });
        }



        private async Task<BitmapSource?> LoadSvgThumbnailAsync(string path, int iconSize)
        {
            try
            {
                var svgDocument = Svg.SvgDocument.Open(path);

                using (var bitmap = svgDocument.Draw(iconSize, iconSize))
                {
                    using (var ms = new MemoryStream())
                    {
                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        ms.Seek(0, SeekOrigin.Begin);

                        BitmapImage bitmapImage = null;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.StreamSource = ms;
                            bitmapImage.DecodePixelWidth = 64;
                            bitmapImage.DecodePixelHeight = 64;
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.EndInit();
                        });
                        return bitmapImage;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load SVG thumbnail: {ex.Message}");
                return null;
            }
        }
        public async Task<BitmapSource?> LoadUrlIconAsync(string path)
        {
            try
            {
                string iconFile = "";
                int iconIndex = 0;
                bool hasHttp = false;
                bool hasHttps = false;
                foreach (var line in File.ReadAllLines(path))
                {
                    // Debug.WriteLine(line);
                    if (line.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase))
                    {
                        iconFile = line.Substring("IconFile=".Length).Trim();
                    }
                    else if (line.StartsWith("IconIndex=", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(line.Substring("IconIndex=".Length).Trim(), out int i))
                        {
                            iconIndex = i;
                        }
                    }
                    else if (iconFile == "")
                    {
                        if (line.StartsWith("URL=http://"))
                        {
                            hasHttp = true;
                            break;
                        }
                        else if (line.StartsWith("URL=https://"))
                        {
                            hasHttps = true;
                            break;
                        }
                    }
                }
                if (iconFile == "")
                {
                    if (hasHttp)
                    {
                        iconFile = GetDefaultBrowserPath("http");
                    }
                    else if (hasHttps)
                    {
                        iconFile = GetDefaultBrowserPath("https");
                    }
                }
                if (!string.IsNullOrEmpty(iconFile) && File.Exists(iconFile))
                {
                    return await Task.Run(() =>
                    {
                        IntPtr[] icons = new IntPtr[1];
                        int extracted = Interop.ExtractIconEx(iconFile, iconIndex, icons, null, 1);
                        if (extracted > 0 && icons[0] != IntPtr.Zero)
                        {
                            var source = Imaging.CreateBitmapSourceFromHIcon(
                                icons[0],
                                Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions());
                            Interop.DestroyIcon(icons[0]);
                            if (Instance.ShowShortcutArrow)
                            {
                                IntPtr[] overlayIcons = new IntPtr[1];
                                int overlayExtracted = Interop.ExtractIconEx(
                                    Environment.SystemDirectory + "\\shell32.dll",
                                    29,
                                    overlayIcons,
                                    null,
                                    1);

                                if (overlayExtracted > 0 && overlayIcons[0] != IntPtr.Zero)
                                {
                                    var overlay = Imaging.CreateBitmapSourceFromHIcon(
                                        overlayIcons[0],
                                        Int32Rect.Empty,
                                        BitmapSizeOptions.FromEmptyOptions());
                                    Interop.DestroyIcon(overlayIcons[0]);

                                    var visual = new DrawingVisual();
                                    using (var dc = visual.RenderOpen())
                                    {
                                        dc.DrawImage(source, new Rect(0, 0, source.PixelWidth, source.PixelHeight));
                                        dc.DrawImage(overlay, new Rect(
                                            source.PixelWidth - overlay.PixelWidth,
                                            source.PixelHeight - overlay.PixelHeight,
                                            overlay.PixelWidth,
                                            overlay.PixelHeight));
                                    }

                                    var rtb = new RenderTargetBitmap(
                                        source.PixelWidth,
                                        source.PixelHeight,
                                        source.DpiX,
                                        source.DpiY,
                                        PixelFormats.Pbgra32);
                                    rtb.Render(visual);
                                    rtb.Freeze();

                                    return rtb;
                                }
                            }
                            source.Freeze();
                            return source;
                        }
                        return null;
                    });
                }
                return null;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error loading URL icon: " + e.Message);
                return await GetThumbnailAsync(path);
            }
        }
        private string GetDefaultBrowserPath(string protocol)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@$"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\{protocol}\UserChoice"))
                {
                    if (key != null)
                    {
                        object progId = key.GetValue("Progid");

                        if (progId == null)
                        {
                            return "";
                        }
                        using (RegistryKey commandKey = Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command"))
                        {
                            if (commandKey != null)
                            {
                                object command = commandKey.GetValue("");

                                if (command == null)
                                {
                                    return "";
                                }
                                return Regex.Match(command.ToString()!, "^\"([^\"]+)\"").Groups[1].Value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return "";
            }
            return "";
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateFileExtensionIcon();
            UpdateHiddenFilesIcon();
            UpdateIconVisibility();
            AnimateChevron(_isMinimized, true, 0.01); // When 0 docked window won't open
            KeepWindowBehind();
            RegistryHelper rgh = new RegistryHelper("DeskFrame");
            bool toBlur = true;
            //if (rgh.KeyExistsRoot("blurBackground"))
            //{
            //    toBlur = (bool)rgh.ReadKeyValueRoot("blurBackground");
            //}
            // BackgroundType(toBlur);
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
            if (_isLeftButtonDown)
            {
                Instance.PosX = this.Left;
                Instance.PosY = this.Top;
            }
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            KeepWindowBehind();
            //WindowChrome.SetWindowChrome(this, Instance.IsLocked ?
            //new WindowChrome
            //{
            //    ResizeBorderThickness = new Thickness(0),
            //    CaptionHeight = 0
            //}
            //: _isOnBottom ?
            //    new WindowChrome
            //    {
            //        GlassFrameThickness = new Thickness(5),
            //        CaptionHeight = 0,
            //        ResizeBorderThickness = new Thickness(0, Instance.Minimized ? 0 : 5, 5, 0),
            //        CornerRadius = new CornerRadius(5)
            //    } :
            //    new WindowChrome
            //    {
            //        GlassFrameThickness = new Thickness(5),
            //        CaptionHeight = 0,
            //        ResizeBorderThickness = new Thickness(5, 0, 5, Instance.Minimized ? 0 : 5),
            //        CornerRadius = new CornerRadius(5)
            //    }
            //);
            HandleWindowMove(true);
            try
            {

                _currentVD = Array.IndexOf(VirtualDesktop.GetDesktops(), VirtualDesktop.Current) + 1;
                Debug.WriteLine($"Start to desktop number: {_currentVD}");
                if (Instance.ShowOnVirtualDesktops != null && Instance.ShowOnVirtualDesktops.Length != 0 && !Instance.ShowOnVirtualDesktops.Contains(_currentVD))
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
                VirtualDesktopSupported = true;
            }
            catch
            {
                VirtualDesktopSupported = false;
            }
        }
        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            _previousHeight = Instance.Height;
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
             contextMenu = new ContextMenu();

            ToggleSwitch toggleHiddenFiles = new ToggleSwitch { Content = Lang.TitleBarContextMenu_HiddenFiles };
            toggleHiddenFiles.Click += (s, args) => { ToggleHiddenFiles(); LoadFiles(_currentFolderPath); };

            ToggleSwitch toggleFileExtension = new ToggleSwitch { Content = Lang.TitleBarContextMenu_FileExtensions };
            toggleFileExtension.Click += (_, _) => { ToggleFileExtension(); LoadFiles(_currentFolderPath); };

            toggleHiddenFiles.IsChecked = Instance.ShowHiddenFiles;
            toggleFileExtension.IsChecked = Instance.ShowFileExtension;

            MenuItem frameSettings = new MenuItem
            {
                Header = Lang.TitleBarContextMenu_FrameSettings,
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
                    LoadFiles(_currentFolderPath);
                }
            };

            MenuItem reloadItems = new MenuItem
            {
                Header = Lang.TitleBarContextMenu_Reload,
                Height = 34,
                Icon = new SymbolIcon(SymbolRegular.ArrowSync20)
            };
            reloadItems.Click += (s, args) =>
            {
                FileItems.Clear();
                LoadFiles(Instance.Folder);
                _currentFolderPath = Instance.Folder;
                InitializeFileWatcher();

            };
            reloadItems.Visibility = (Instance.Folder == "empty" || string.IsNullOrEmpty(Instance.Folder)) ? Visibility.Collapsed : Visibility.Visible;

            MenuItem lockFrame = new MenuItem
            {
                Header = Instance.IsLocked ? Lang.TitleBarContextMenu_UnlockFrame : Lang.TitleBarContextMenu_LockFrame,
                Height = 34,
                Icon = Instance.IsLocked ? new SymbolIcon(SymbolRegular.LockClosed20) : new SymbolIcon(SymbolRegular.LockOpen20)
            };
            lockFrame.Click += (s, args) =>
            {
                _isLocked = !_isLocked;
                ToggleIsLocked();
                //WindowChrome.SetWindowChrome(this, Instance.IsLocked ?
                //new WindowChrome
                //{
                //    ResizeBorderThickness = new Thickness(0),
                //    CaptionHeight = 0
                //}
                //: _isOnBottom ?
                //    new WindowChrome
                //    {
                //        GlassFrameThickness = new Thickness(5),
                //        CaptionHeight = 0,
                //        ResizeBorderThickness = new Thickness(5, Instance.Minimized ? 0 : 5, 5, 0),
                //        CornerRadius = new CornerRadius(5)
                //    } :
                //    new WindowChrome
                //    {
                //        GlassFrameThickness = new Thickness(5),
                //        CaptionHeight = 0,
                //        ResizeBorderThickness = new Thickness(5, 0, 5, Instance.Minimized ? 0 : 5),
                //        CornerRadius = new CornerRadius(5)
                //    }
                //);

                titleBar.Cursor = _isLocked ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.SizeAll;
                title.Cursor = _isLocked ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.SizeAll;
                frameTypeSymbol.Cursor = _isLocked ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.SizeAll;
                titleStackPanel.Cursor = _isLocked ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.SizeAll;
            };

            MenuItem exitItem = new MenuItem
            {
                Header = Lang.TitleBarContextMenu_Remove,
                Height = 34,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFC6060")),
                Icon = new SymbolIcon(SymbolRegular.Delete20)

            };

            exitItem.Click += async (s, args) =>
            {
                var dialog = new MessageBox
                {
                    Title = Lang.TitleBarContextMenu_RemoveMessageBox_Title,
                    Content = Lang.TitleBarContextMenu_RemoveMessageBox_Content,
                    PrimaryButtonText = Lang.TitleBarContextMenu_RemoveMessageBox_Yes,
                    CloseButtonText = Lang.TitleBarContextMenu_RemoveMessageBox_No
                };

                var result = await dialog.ShowDialogAsync();

                if (result == MessageBoxResult.Primary)
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(Instance.GetKeyLocation(), true)!;
                    if (key != null)
                    {
                        Registry.CurrentUser.DeleteSubKeyTree(Instance.GetKeyLocation());
                    }
                    MainWindow._controller.RemoveInstance(Instance, this);
                    this.Close();

                }
            };

            MenuItem sortByMenuItem = new MenuItem
            {
                Header = Lang.TitleBarContextMenu_Sortby,
                Height = 34,
                Icon = new SymbolIcon(SymbolRegular.ArrowSort20)
            };
            nameMenuItem = new MenuItem { Header = Lang.TitleBarContextMenu_Sortby_Name, Height = 34, StaysOpenOnClick = true };
            dateModifiedMenuItem = new MenuItem { Header = Lang.TitleBarContextMenu_Sortby_DateModified, Height = 34, StaysOpenOnClick = true };
            dateCreatedMenuItem = new MenuItem { Header = Lang.TitleBarContextMenu_Sortby_DateCreated, Height = 34, StaysOpenOnClick = true };
            fileTypeMenuItem = new MenuItem { Header = Lang.TitleBarContextMenu_Sortby_FileType, Height = 34, StaysOpenOnClick = true };
            fileSizeMenuItem = new MenuItem { Header = Lang.TitleBarContextMenu_Sortby_FileSize, Height = 34, StaysOpenOnClick = true };
            ascendingMenuItem = new MenuItem { Header = Lang.TitleBarContextMenu_Sortby_Ascending, Height = 34, StaysOpenOnClick = true };
            descendingMenuItem = new MenuItem { Header = Lang.TitleBarContextMenu_Sortby_Descending, Height = 34, StaysOpenOnClick = true };



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

            InfoText.Inlines.Add(new Run(Lang.TitleBarContextMenu_Info_Files) { Foreground = Brushes.White });
            InfoText.Inlines.Add(new Run($"{_fileCount}") { Foreground = Brushes.CornflowerBlue });
            InfoText.Inlines.Add(new Run("\n"));

            InfoText.Inlines.Add(new Run(Lang.TitleBarContextMenu_Info_Folders) { Foreground = Brushes.White });
            InfoText.Inlines.Add(new Run($"{_folderCount}") { Foreground = Brushes.CornflowerBlue });
            InfoText.Inlines.Add(new Run("\n"));
            if (Instance.CheckFolderSize)
            {
                InfoText.Inlines.Add(new Run(Lang.TitleBarContextMenu_Info_FolderSize) { Foreground = Brushes.White });
                InfoText.Inlines.Add(new Run($"{_folderSize}") { Foreground = Brushes.CornflowerBlue });
                InfoText.Inlines.Add(new Run("\n"));
            }

            InfoText.Inlines.Add(new Run(Lang.TitleBarContextMenu_Info_LastUpdated) { Foreground = Brushes.White });
            InfoText.Inlines.Add(new Run($"{_lastUpdated.ToString("hh:mm tt")}") { Foreground = Brushes.CornflowerBlue });

            FrameInfoItem.Header = InfoText;


            folderOrderMenuItem = new MenuItem
            {
                Header = Lang.TitleBarContextMenu_Sortby_FolderOrder,
                Height = 36,
                StaysOpenOnClick = true,
                Icon = new SymbolIcon { Symbol = SymbolRegular.Folder20 }
            };

            folderNoneMenuItem = new MenuItem { Header = Lang.TitleBarContextMenu_Sortby_FolderIder_None, Height = 34, StaysOpenOnClick = true };
            folderFirstMenuItem = new MenuItem { Header = Lang.TitleBarContextMenu_Sortby_FolderIder_First, Height = 34, StaysOpenOnClick = true };
            folderLastMenuItem = new MenuItem { Header = Lang.TitleBarContextMenu_Sortby_FolderIder_Last, Height = 34, StaysOpenOnClick = true };

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
                Header = Lang.TitleBarContextMenu_OpenFolder,
                Icon = new SymbolIcon { Symbol = SymbolRegular.FolderOpen20 }
            };
            openInExplorerMenuItem.Visibility = (Instance.Folder == "empty" || string.IsNullOrEmpty(Instance.Folder)) ? Visibility.Collapsed : Visibility.Visible;
            openInExplorerMenuItem.Click += (_, _) => { OpenFolder(); };


            MenuItem changeItemView = new MenuItem
            {
                Header = Lang.TitleBarContextMenu_ChangeView
            };
            if (showFolder.Visibility == Visibility.Visible)
            {
                changeItemView.Header = Lang.TitleBarContextMenu_GridView;
                changeItemView.Icon = new SymbolIcon { Symbol = SymbolRegular.Grid20 };
            }
            else
            {
                changeItemView.Header = Lang.TitleBarContextMenu_DetailsView;
                changeItemView.Icon = new SymbolIcon { Symbol = SymbolRegular.AppsList20 };
            }
            changeItemView.Click += (_, _) =>
            {
                if (showFolder.Visibility == Visibility.Visible)
                {
                    changeItemView.Header = Lang.TitleBarContextMenu_GridView;
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
                    changeItemView.Header = Lang.TitleBarContextMenu_DetailsView;
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

        private int GetZIndex(IntPtr hwnd)
        {
            IntPtr h = GetTopWindow(IntPtr.Zero);
            int z = 0;

            while (h != IntPtr.Zero)
            {
                if (h == hwnd)
                    return z;

                h = Interop.GetWindow(h, GW_HWNDNEXT);
                z++;
            }
            return -1;
        }
        IntPtr GetWindowWithMinZIndex(List<IntPtr> windowHandles)
        {
            IntPtr lowestWindow = IntPtr.Zero;
            int lowestZ = int.MaxValue;

            foreach (var hwnd in windowHandles)
            {
                int z = GetZIndex(hwnd);
                if (z >= 0 && z < lowestZ)
                {
                    lowestZ = z;
                    lowestWindow = hwnd;
                }
            }
            return lowestWindow;
        }
        void BringFrameToFront(IntPtr hwnd)
        {
            IntPtr hwndLower = Interop.GetWindow(GetWindowWithMinZIndex(MainWindow._controller._subWindowsPtr), GW_HWNDPREV);
            IntPtr insertAfter = hwndLower != IntPtr.Zero ? hwndLower : IntPtr.Zero;
            SendMessage(hwnd, WM_SETREDRAW, 0, IntPtr.Zero);

            SetWindowPos(hwnd, insertAfter, 0, 0, 0, 0,
               SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOSENDCHANGING);

            SendMessage(hwnd, WM_SETREDRAW, 1, IntPtr.Zero);
        }
        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!_mouseIsOver && IsCursorWithinWindowBounds())
            {
                AnimateActiveColor(Instance.AnimationSpeed);
                if (Instance.HideTitleBarIconsWhenInactive)
                {
                    TitleBarIconsFadeAnimation(true);
                }
            }
            _mouseIsOver = true;
            var hwnd = new WindowInteropHelper(this).Handle;
            BringFrameToFront(hwnd);
            SetForegroundWindow(hwnd);
            this.Activate();
            SetFocus(hwnd);
            this.Focus();
            _canAutoClose = true;
            AnimateWindowOpacity(1, Instance.AnimationSpeed);
            if ((Instance.AutoExpandonCursor) && _isMinimized)
            {
                Minimize_MouseLeftButtonDown(null, null);
            }
        }
        public bool IsCursorWithinWindowBounds()
        {
            Point cursor = System.Windows.Forms.Cursor.Position;
            bool cursorIsOverTheWindow = WindowFromPoint(new POINT { X = cursor.X, Y = cursor.Y }) == new WindowInteropHelper(this).Handle;

            Interop.GetWindowRect(new WindowInteropHelper(this).Handle, out RECT rect);
            Point point = System.Windows.Forms.Cursor.Position;
            var curPoint = new Point((int)point.X, (int)point.Y);
            bool cursorIsWithinWindowBounds = point.X + 1 > rect.Left && point.X - 1 < rect.Right && point.Y + 1 > rect.Top && point.Y - 1 < rect.Bottom;
            if (_contextMenuIsOpen || contextMenu.IsOpen) return true;
            if (!_contextMenuIsOpen)
            {
                if (cursorIsOverTheWindow)
                {
                    return true;
                }
                
                return false;
            }
            return false;
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

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            MouseLeaveWindow();
        }

        private void PathToBackButton_Click(object sender, RoutedEventArgs e)
        {
            var parentPath = Path.GetDirectoryName(_currentFolderPath) == Instance.Folder
                ? Instance.Folder : Path.GetDirectoryName(_currentFolderPath);
            Debug.WriteLine(parentPath);
            PathToBackButton.Visibility = parentPath == Instance.Folder
                ? Visibility.Collapsed : Visibility.Visible;

            FileItems.Clear();
            LoadFiles(parentPath!);
            _currentFolderPath = parentPath!;
            InitializeFileWatcher();
        }

        private void SymbolIcon_MouseEnter(object sender, MouseEventArgs e)
        {
            InfoFlyout.IsOpen = true;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Instance.isWindowClosing = true;
        }
    }
}