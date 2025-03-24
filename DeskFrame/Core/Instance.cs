﻿using DeskFrame;
using System.ComponentModel;
using System.Diagnostics;


public class Instance : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private double _posX;
    private double _posY;
    private double _width;
    private double _height;
    private string _name;
    private string _folder;
    private bool _minimized;

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

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name),value);
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
    public Instance(Instance instance)
    {
        _posX = instance._posX;
        _posY = instance._posY;
        _width = instance.Width;
        _height = instance._height;
        _name = instance._name;
        _minimized = instance._minimized;
        _folder = instance._folder;
    }

    public Instance(string name) // default instance
    {
        _width = 175;
        _height = 215;
        _posX = Screen.PrimaryScreen!.Bounds.Width / 2 - _width / 2;
        _posY = Screen.PrimaryScreen!.Bounds.Height / 2 - _height / 2;
        _name = name;
        _minimized = false;
        _folder = "empty";
    }

    protected void OnPropertyChanged(string propertyName, string value)
    {
      
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
            //  Debug.WriteLine($"Property {propertyName} has changed.");

            if (Name != "empty")
            {
                MainWindow._controller.reg.WriteToRegistry(propertyName, value, this);

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
