using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Interop;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media.Animation;
using Point = System.Drawing.Point;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Color = System.Windows.Media.Color;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Brush = System.Windows.Media.Brush;
using DeskFrame.Util;
using MenuItem = Wpf.Ui.Controls.MenuItem;
using Microsoft.Win32;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using System.Windows.Data;
using System.Text.RegularExpressions;
using System.Collections;
using Windows.Foundation.Collections;
using System.Windows.Shell;
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
        private bool _isBlack = true;
        private bool _checkForChages = false;
        private bool _canAutoClose = true;
        private bool _isLocked = false;
        private bool _isOnEdge = false;
        private double _originalHeight;
        public int neighborFrameCount = 0;
        public bool isMouseDown = false;
        private ICollectionView _collectionView;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private CancellationTokenSource loadFilesCancellationToken = new CancellationTokenSource();
        DeskFrameWindow _wOnLeft = null;
        DeskFrameWindow _wOnRight = null;
        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (!(HwndSource.FromHwnd(hWnd).RootVisual is System.Windows.Window rootVisual))
                return IntPtr.Zero;
            if (msg == 0x0214) // WM_SIZING
            {
                Interop.RECT rect = (Interop.RECT)Marshal.PtrToStructure(lParam, typeof(Interop.RECT));
                double width = rect.Right - rect.Left;
                double height = rect.Bottom - rect.Top;
                
                if (height < 100) 
                    height = 100;
                rect.Bottom = (int)(rect.Top + height);
                Marshal.StructureToPtr(rect, lParam, true);
            }
            else if (msg == 0x0215) // WM_EXITSIZEMOVE - Window sizing complete
            {
                Interop.RECT rect;
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                Interop.GetWindowRect(hwnd, out rect);
                double width = rect.Right - rect.Left;
                double newWidth = Math.Round(width / 85.0) * 85 + 20;
                if (width != newWidth)
                {
                    rect.Right = rect.Left + (int)newWidth;
                    Interop.SetWindowPos(hwnd, IntPtr.Zero, rect.Left, rect.Top, 
                        (int)newWidth, rect.Bottom - rect.Top, 
                        Interop.SWP_NOREDRAW | Interop.SWP_NOACTIVATE | Interop.SWP_NOZORDER);
                    Instance.Width = newWidth;
                }
            }
            else if (msg == 70)
            {
                Interop.WINDOWPOS structure = Marshal.PtrToStructure<Interop.WINDOWPOS>(lParam);
                structure.flags |= 4U;
                Marshal.StructureToPtr<Interop.WINDOWPOS>(structure, lParam, false);
            }
            else if (msg == 0x0003) // WM_MOVE
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
                windowBorder.CornerRadius = new CornerRadius(0, 0, 5, 5);
                _isOnEdge = true;
            }
            else if (Math.Abs(windowBottom - workingArea.Bottom) <= _snapDistance)
            {
                newWindowTop = (int)(workingArea.Bottom - (windowBottom - windowTop));
            }
            else
            {
                _isOnEdge = false;
                windowBorder.CornerRadius = new CornerRadius(5);
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
                windowBorder.CornerRadius = new CornerRadius(0);
                titleBar.CornerRadius = new CornerRadius(0);
            }
            else
            {
                if (_wOnLeft != null && onLeft)
                {
                    if (_wOnLeft._isMinimized)
                    {

                        _wOnLeft.windowBorder.CornerRadius = new CornerRadius(
                            topLeft: _wOnLeft.windowBorder.CornerRadius.TopLeft,
                            topRight: 0,
                            bottomRight: 0,
                            bottomLeft: _wOnLeft.windowBorder.CornerRadius.BottomLeft
                         );
                        _wOnLeft.titleBar.CornerRadius = _wOnLeft.windowBorder.CornerRadius;

                        windowBorder.CornerRadius = new CornerRadius(
                            topLeft: 0,
                            topRight: windowBorder.CornerRadius.TopRight,
                            bottomRight: windowBorder.CornerRadius.BottomRight,
                            bottomLeft: 0
                         );
                        titleBar.CornerRadius = new CornerRadius(
                            topLeft: 0,
                            topRight: titleBar.CornerRadius.TopRight,
                            bottomRight: _isMinimized ? 5 : 0,
                            bottomLeft: 0
                         );
                    }

                    else
                    {
                        _wOnLeft.windowBorder.CornerRadius = new CornerRadius(
                            topLeft: _wOnLeft.windowBorder.CornerRadius.TopLeft,
                            topRight: _wOnLeft.windowBorder.CornerRadius.TopRight,
                            bottomRight: 5,
                            bottomLeft: 5
                         );
                        _wOnLeft.titleBar.CornerRadius = new CornerRadius(
                            topLeft: _wOnLeft.titleBar.CornerRadius.TopLeft,
                            topRight: _wOnLeft.titleBar.CornerRadius.TopRight,
                            bottomRight: 0,
                            bottomLeft: 0
                         );

                        windowBorder.CornerRadius = new CornerRadius(
                            topLeft: 0,
                            topRight: windowBorder.CornerRadius.TopRight,
                            bottomRight: 5,
                            bottomLeft: 5
                          );
                        titleBar.CornerRadius = new CornerRadius(
                            topLeft: 0,
                            topRight: titleBar.CornerRadius.TopRight,
                            bottomRight: _isMinimized ? 5 : 0,
                            bottomLeft: 0
                         );
                    }
                }

                else if (_wOnRight != null && onRight)
                {
                    if (_wOnRight._isMinimized)
                    {
                        _wOnRight.windowBorder.CornerRadius = new CornerRadius(
                            topLeft: 0,
                            topRight: _wOnRight.windowBorder.CornerRadius.TopRight,
                            bottomRight: _wOnRight.windowBorder.CornerRadius.BottomRight,
                            bottomLeft: 0
                        );
                        _wOnRight.titleBar.CornerRadius = _wOnRight.windowBorder.CornerRadius;

                        windowBorder.CornerRadius = new CornerRadius(
                           topLeft: windowBorder.CornerRadius.TopLeft,
                           topRight: 0,
                           bottomRight: 0,
                           bottomLeft: 5
                        );
                        titleBar.CornerRadius = new CornerRadius(
                           topLeft: titleBar.CornerRadius.TopLeft,
                           topRight: 0,
                           bottomRight: 0,
                           bottomLeft: _isMinimized ? 5 : 0
                         );
                    }
                    else
                    {
                        _wOnRight.windowBorder.CornerRadius = new CornerRadius(
                            topLeft: _wOnRight.windowBorder.CornerRadius.TopLeft,
                            topRight: _wOnRight.windowBorder.CornerRadius.TopRight,
                            bottomRight: 5,
                            bottomLeft: 5
                        );

                        _wOnRight.titleBar.CornerRadius = new CornerRadius(
                            topLeft: _wOnRight.titleBar.CornerRadius.TopLeft,
                            topRight: _wOnRight.titleBar.CornerRadius.TopRight,
                            bottomRight: 0,
                            bottomLeft: 0
                        );

                        windowBorder.CornerRadius = new CornerRadius(
                           topLeft: windowBorder.CornerRadius.TopLeft,
                            topRight: 0,
                            bottomRight: 5,
                            bottomLeft: 5
                        );
                        titleBar.CornerRadius = new CornerRadius(
                            topLeft: titleBar.CornerRadius.TopLeft,
                            topRight: 0,
                            bottomRight: 0,
                            bottomLeft: _isMinimized ? 5 : 0
                         );
                    }

                }
            }
            if (neighborFrameCount == 0)
            {
                if (_wOnLeft != null && !onLeft)
                {
                    _wOnLeft.windowBorder.CornerRadius = new CornerRadius(
                        topLeft: _wOnLeft.windowBorder.CornerRadius.TopLeft,
                        topRight: _wOnLeft._isOnEdge ? 0 : 5,
                        bottomRight: 5,
                        bottomLeft: _wOnLeft.windowBorder.CornerRadius.BottomLeft
                     );
                    _wOnLeft.titleBar.CornerRadius = new CornerRadius(
                        topLeft: _wOnLeft.titleBar.CornerRadius.TopLeft,
                        topRight: _wOnLeft._isOnEdge ? 0 : 5,
                        bottomRight: _wOnLeft._isMinimized ? 5 : 0,
                        bottomLeft: _wOnLeft._isMinimized ? 5 : 0
                    );
                    if (_wOnLeft.neighborFrameCount == 0 && !_wOnLeft._isMinimized)
                    {
                        _wOnLeft.titleBar.CornerRadius = new CornerRadius(
                            topLeft: _wOnLeft._isOnEdge ? 0 : 5,
                            topRight: _wOnLeft._isOnEdge ? 0 : 5,
                            bottomRight: 5,
                            bottomLeft: 5
                         );
                    }

                    windowBorder.CornerRadius = new CornerRadius(
                        topLeft: windowBorder.CornerRadius.TopLeft,
                        topRight: _isOnEdge ? 0 : 5,
                        bottomRight: 5,
                        bottomLeft: windowBorder.CornerRadius.BottomLeft
                    );
                    titleBar.CornerRadius = windowBorder.CornerRadius;

                    _wOnLeft._wOnRight = null;
                    _wOnLeft = null;
                }
                if (_wOnRight != null && !onRight)
                {
                    _wOnRight.windowBorder.CornerRadius = new CornerRadius(
                         topLeft: _wOnRight._isOnEdge ? 0 : 5,
                         topRight: _wOnRight.windowBorder.CornerRadius.TopRight,
                         bottomRight: _wOnRight.windowBorder.CornerRadius.BottomRight,
                         bottomLeft: 5.0
                     );
                    _wOnRight.titleBar.CornerRadius = new CornerRadius(
                         topLeft: _wOnRight._isOnEdge ? 0 : 5,
                         topRight: _wOnRight.titleBar.CornerRadius.TopRight,
                         bottomRight: _wOnRight._isMinimized ? 5 : 0,
                         bottomLeft: _wOnRight._isMinimized ? 5 : 0
                    );
                    if (_wOnRight.neighborFrameCount == 0 && !_wOnRight._isMinimized)
                    {
                        _wOnRight.titleBar.CornerRadius = new CornerRadius(
                            topLeft: _wOnRight._isOnEdge ? 0 : 5,
                            topRight: _wOnRight._isOnEdge ? 0 : 5,
                            bottomRight: 5,
                            bottomLeft: 5
                         );
                    }

                    windowBorder.CornerRadius = new CornerRadius(
                         topLeft: _isOnEdge ? 0 : 5,
                         topRight: windowBorder.CornerRadius.TopRight,
                         bottomRight: windowBorder.CornerRadius.BottomRight,
                         bottomLeft: 5
                     );
                    titleBar.CornerRadius = windowBorder.CornerRadius;

                    _wOnRight._wOnLeft = null;
                    _wOnRight = null;
                }
                if (_isMinimized)
                {
                    titleBar.CornerRadius = new CornerRadius(
                            topLeft: _isOnEdge ? 0 : 5,
                            topRight: _isOnEdge ? 0 : 5,
                            bottomRight: 5,
                            bottomLeft: 5
                        );
                }
                else
                {
                    titleBar.CornerRadius = new CornerRadius(
                          topLeft: _isOnEdge ? 0 : 5,
                          topRight: _isOnEdge ? 0 : 5,
                          bottomRight: 0,
                          bottomLeft: 0
                      );
                }
            }
            if (!_isMinimized)
            {
                windowBorder.CornerRadius = new CornerRadius(
                topLeft: windowBorder.CornerRadius.TopLeft,
                topRight: windowBorder.CornerRadius.TopRight,
                bottomRight: 5,
                bottomLeft: 5
                );
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
                Search.Visibility = Visibility.Hidden;
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
            this.Loaded += MainWindow_Loaded;
            this.SourceInitialized += MainWindow_SourceInitialized!;

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
            titleBar.Cursor = _isLocked ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.SizeAll;
            if ((int)instance.Height <= 30) _isMinimized = true;
            if (instance.Minimized)
            {
                _isMinimized = instance.Minimized;
                this.Height = 30;
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

        private System.Threading.Timer? _resizeDebounceTimer;
        private const int RESIZE_DEBOUNCE_MS = 50; // 50ms debounce

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged && !_isMinimized)
            {
                if (this.ActualHeight != 30)
                {
                    Instance.Height = this.ActualHeight;
                }
            }

            // Cancel any pending timer
            _resizeDebounceTimer?.Dispose();
            
            // Create new timer to update corner radius after resize stops
            _resizeDebounceTimer = new System.Threading.Timer(_ =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    HandleWindowMove();
                });
            }, null, RESIZE_DEBOUNCE_MS, System.Threading.Timeout.Infinite);
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
            if (showFolder.Visibility == Visibility.Hidden)
            {
                return;
            }
            if (!_isMinimized)
            {
                _originalHeight = this.ActualHeight;
                _isMinimized = true;
                Instance.Minimized = true;
                Debug.WriteLine("minimize: " + Instance.Height);
                AnimateWindowHeight(30);
            }
            else
            {
                windowBorder.CornerRadius = new CornerRadius(
                         topLeft: windowBorder.CornerRadius.TopLeft,
                         topRight: windowBorder.CornerRadius.TopRight,
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
                if (targetHeight == 30) scrollViewer.ScrollToTop();
            };
            _canAnimate = false;
            this.BeginAnimation(HeightProperty, animation);
        }

        private void InitializeFileWatcher()
        {
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
                LoadFiles(_path);
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

        private void ToggleHiddenFiles()
        {
            Instance.ShowHiddenFiles = !Instance.ShowHiddenFiles;
        }
        private void ToggleIsLocked()
        {
            Instance.IsLocked = !Instance.IsLocked;
        }

        private async void LoadFiles(string path)
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

                var fileEntries = await Task.Run(() =>
                {
                    if (loadFiles_cts.IsCancellationRequested)
                    {
                        return new List<FileSystemInfo>();
                    }
                    var dirInfo = new DirectoryInfo(path);
                    var files = dirInfo.GetFiles();
                    var directories = dirInfo.GetDirectories();
                    var filteredFiles = files.Cast<FileSystemInfo>()
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
                        {
                            return;
                        }
                        var existingItem = FileItems.FirstOrDefault(item => item.FullPath == entry.FullName);

                        if (existingItem == null)
                        {
                            var fileItem = new FileItem
                            {
                                Name = entry.Name,
                                FullPath = entry.FullName,
                                Thumbnail = await GetThumbnailAsync(entry.FullName)
                            };

                            FileItems.Add(fileItem);
                        }
                        else
                        {
                            existingItem.Thumbnail = await GetThumbnailAsync(entry.FullName);
                        }
                    }

                    var sortedList = FileItems.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();
                    FileItems.Clear();
                    foreach (var fileItem in sortedList)
                    {
                        FileItems.Add(fileItem);
                    }
                    Debug.WriteLine("LOADEDDDDDDDD");
                });
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("LoadFiles was canceled.");
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
            return await Task.Run(async () =>
            {
                if (string.IsNullOrWhiteSpace(path) || (!File.Exists(path) && !Directory.Exists(path)))
                {
                    Console.WriteLine("Invalid path: " + path);
                    return null;
                }

                if (Path.GetExtension(path).ToLower() == ".svg")
                {
                    return await LoadSvgThumbnailAsync(path);
                }

                IntPtr hBitmap = IntPtr.Zero;
                Interop.IShellItemImageFactory? factory = null;
                int attempts = 0;
                while (attempts < 2) // Try twice if needed
                {
                    try
                    {
                        Guid shellItemImageFactoryGuid = new Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b");
                        int hr = Interop.SHCreateItemFromParsingName(path, IntPtr.Zero, ref shellItemImageFactoryGuid, out factory);

                        if (hr != 0 || factory == null)
                        {
                            Console.WriteLine($"Failed to create factory (attempt {attempts + 1}). HRESULT: {hr}");
                        }
                        else
                        {
                            int thumbnailSize = Directory.Exists(path) ? 128 : 64;
                            System.Drawing.Size desiredSize = new System.Drawing.Size(thumbnailSize, thumbnailSize);
                            hr = factory.GetImage(desiredSize, 0, out hBitmap);

                            if (hr == 0 && hBitmap != IntPtr.Zero)
                            {
                                return Application.Current.Dispatcher.Invoke(() =>
                                {
                                    BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                                        hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                                    Interop.DeleteObject(hBitmap);  // Clean up the HBitmap
                                    return bitmapSource;
                                });
                            }
                            else
                            {
                                Debug.WriteLine($"Failed to get image (attempt {attempts + 1}). HRESULT: {hr}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Exception occurred (attempt {attempts + 1}): {ex.Message}");
                    }
                    finally
                    {
                        if (factory != null) Marshal.ReleaseComObject(factory);
                        if (hBitmap != IntPtr.Zero) Interop.DeleteObject(hBitmap);
                    }

                    attempts++;
                }

                Debug.WriteLine("Failed to retrieve thumbnail after 2 attempts.");
                return null;
            });
        }



        private async Task<BitmapSource?> LoadSvgThumbnailAsync(string path)
        {
            try
            {
                var svgDocument = Svg.SvgDocument.Open(path);

                using (var bitmap = svgDocument.Draw(64, 64))
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AnimateChevron(_isMinimized, true);
            KeepWindowBehind();
            RegistryHelper rgh = new RegistryHelper("DeskFrame");
            bool toBlur = true;
            if (rgh.KeyExistsRoot("blurBackground"))
            {
                toBlur = (bool)rgh.ReadKeyValueRoot("blurBackground");
            }


            BackgroundType(toBlur);
        }

        public void ChangeBackgroundOpacity(int num)
        {
            windowBorder.Background = _isBlack ? new SolidColorBrush(Color.FromArgb((byte)num, 0, 0, 0)) :
                                                 new SolidColorBrush(Color.FromArgb((byte)num, 255, 255, 255));
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
                ResizeBorderThickness = new Thickness(5, 0, 5, 5),
                CornerRadius = new CornerRadius(5)
            }
         );

        }
        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            KeepWindowBehind();
        }


        private void titleBar_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContextMenu contextMenu = new ContextMenu();

            MenuItem toggleHiddenFiles = new MenuItem { Header = Instance.ShowHiddenFiles ? "Hide hidden Files" : "Show hidden files" };
            toggleHiddenFiles.Click += (s, args) => { ToggleHiddenFiles(); LoadFiles(_path); };

            MenuItem frameSettings = new MenuItem { Header = "Frame Settings" };
            frameSettings.Click += (s, args) =>
            {
                var dialog = new FrameSettingsDialog(this);
                dialog.ShowDialog();
                if (dialog.DialogResult == true)
                {
                    LoadFiles(_path);
                }
            };

            MenuItem reloadItems = new MenuItem { Header = "Reload" };
            reloadItems.Click += (s, args) => { LoadFiles(_path); };

            MenuItem lockFrame = new MenuItem { Header = Instance.IsLocked ? "Unlock Frame" : "Lock Frame" };
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
                           ResizeBorderThickness = new Thickness(5, 0, 5, 5),
                           CornerRadius = new CornerRadius(5)
                       }
                 );

                titleBar.Cursor = _isLocked ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.SizeAll;
            };

            MenuItem exitItem = new MenuItem { Header = "Remove" };
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

            contextMenu.Items.Add(lockFrame);
            contextMenu.Items.Add(reloadItems);
            contextMenu.Items.Add(toggleHiddenFiles);
            contextMenu.Items.Add(frameSettings);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(exitItem);

            contextMenu.IsOpen = true;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            KeepWindowBehind();
            Debug.WriteLine("Window_StateChanged hide");
        }


        public class FileItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            private bool _isSelected;
            private Brush _background = Brushes.Transparent;
            private int _maxHeight = 40;
            private TextTrimming _textTrimming = TextTrimming.CharacterEllipsis;
            private string? _displayName;
            public string? Name { get; set; }
            public string? FullPath { get; set; }
            public BitmapSource? Thumbnail { get; set; }

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

                        // int.MaxValue for full height, 70 for 4 lines
                        // MaxHeight = _isSelected ? 70 : 40;
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

            protected void OnPropertyChanged(string propertyName)
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
    }
}