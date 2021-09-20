using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace geosite
{
    static class RegEdit
    {
        static string _registerKeyName;

        private static string registerkey =>
            _registerKeyName ??= Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule?.FileName);

        public static string getkey(string keyname, string defaultvalue = "")
        {
            using var oldRegistryKey = Registry.CurrentUser.OpenSubKey(registerkey, false);
            return oldRegistryKey?.GetValue(keyname, defaultvalue).ToString();
        }

        public static void setkey(string keyname, string defaultvalue = "")
        {
            using var oldRegistryKey = Registry.CurrentUser.OpenSubKey(registerkey, true);
            if (oldRegistryKey != null)
            {
                oldRegistryKey.SetValue(keyname, defaultvalue);
            }
            else
            {
                using RegistryKey newRegistryKey = Registry.CurrentUser.CreateSubKey(registerkey);
                newRegistryKey?.SetValue(keyname, defaultvalue);
            }
        }
    }
}
