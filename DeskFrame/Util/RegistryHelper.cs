using Microsoft.Win32;
using System.Diagnostics;

public class RegistryHelper
{
    public string regKeyName;
    public RegistryHelper(string regkeyname)
    {
        this.regKeyName = regkeyname;
    }
    public void WriteToRegistry(string keyName, object value, Instance instance)
    {
        try
        {
            if (string.IsNullOrEmpty(instance.Name))
            {
                Debug.WriteLine("Instance is null reg not writen");
                return;
            }
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(instance.GetKeyLocation()))
            {
                key.SetValue(keyName, value);
            }
            //  Debug.WriteLine($"wrote key: {keyName}\t value: {value}\nto: {instance.GetKeyLocation()}");
        }
        catch { }
    }
    public void WriteIntArrayToRegistry(string keyName, int[] values, Instance instance)
    {
        try
        {
            if (string.IsNullOrEmpty(instance?.Name))
            {
                Debug.WriteLine("Instance is null reg not writen");
                return;
            }
            if (values == null)
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(instance.GetKeyLocation()))
                {
                    key.SetValue(keyName, "");
                }
                return;
            }
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(instance.GetKeyLocation()))
            {
                key.SetValue(keyName, string.Join(",", values));
            }
        }
        catch { }
    }
    public void WriteToRegistryRoot(string keyName, object value)
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey($"SOFTWARE\\{regKeyName}"))
            {
                key.SetValue(keyName, value);
            }
        }
        catch { }
    }
    public bool KeyExists(string keyName, Instance instance)
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(instance.GetKeyLocation())!)
            {
                if (key != null)
                {
                    if (key.GetValue(keyName) != null)
                    {
                        Debug.WriteLine($"exists: {keyName},{key.GetValue(keyName)}");

                        return true;
                    }
                }
            }
            Debug.WriteLine($"doesnt exist: {keyName}");
            return false;
        }
        catch
        {
            Debug.WriteLine($"error opening HKCU\\SOFTWARE\\{regKeyName}, {keyName}");
            return false;
        }
    }

    public bool KeyExistsRoot(string keyName)
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{regKeyName}")!)
            {
                if (key != null)
                {
                    if (key.GetValue(keyName) != null)
                    {
                        Debug.WriteLine($"exists: {keyName},{key.GetValue(keyName)}");

                        return true;
                    }
                }
            }
            Debug.WriteLine($"doesnt exist: {keyName}");
            return false;
        }
        catch
        {
            Debug.WriteLine($"error opening HKCU\\SOFTWARE\\{regKeyName}, {keyName}");
            return false;
        }
    }
    public object ReadKeyValueRoot(string keyName)
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{regKeyName}")!)
            {
                if (key != null)
                {
                    object value = key.GetValue(keyName)!;
                    if (value != null)
                    {
                        if (value is bool)
                        {
                            Debug.WriteLine($"returned bool for {keyName}");
                            return (bool)value;
                        }
                        else if (bool.TryParse(value.ToString(), out bool boolValue))
                        {
                            Debug.WriteLine($"returned Parsed bool for {keyName}");
                            return boolValue;
                        }
                        else
                        {
                            Debug.WriteLine($"returned string for {keyName}");
                            return (string)value;
                        }

                    }
                }
            }
            Debug.WriteLine($"couldn't return for {keyName}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error ReadKeyValueRoot: " + ex.Message);
            return false;
        }
    }
    public object ReadKeyValueRootInt(string keyName)
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{regKeyName}")!)
            {
                if (key != null)
                {
                    return Int32.Parse(key.GetValue(keyName).ToString());
                }
            }

            Debug.WriteLine($"couldn't return value for {keyName}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error ReadKeyValueRootInt: " + ex.Message);
            return null;
        }
    }

    public void AddToAutoRun(string appName, string appPath)
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)!)
            {
                key.SetValue(appName, appPath);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error adding to autorun: " + ex.Message);
        }
    }

    public void RemoveFromAutoRun(string appName)
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)!)
            {
                key.DeleteValue(appName, false);
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error removing from autorun: " + ex.Message);
        }
    }



}

