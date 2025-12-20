using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using Brushes = System.Windows.Media.Brushes;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace DeskFrame.Core
{
    public class FileItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isMoveBarVisible;
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

        public bool IsMoveBarVisible
        {
            get => _isMoveBarVisible;
            set
            {
                if (_isMoveBarVisible != value)
                {
                    _isMoveBarVisible = value;
                    OnPropertyChanged(nameof(IsMoveBarVisible));
                }
            }
        }
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
                    //MaxHeight = _isSelected ? 40 : 40;
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
}
