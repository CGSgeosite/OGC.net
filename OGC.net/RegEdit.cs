using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace Geosite
{
    static class RegEdit
    {
        static string _registerKeyName;

        private static string Registerkey =>
            _registerKeyName ??= Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule?.FileName);

        public static string Getkey(string keyname, string defaultvalue = "")
        {
            using var oldRegistryKey = Registry.CurrentUser.OpenSubKey(Registerkey, false);
            return oldRegistryKey?.GetValue(keyname, defaultvalue).ToString();
        }

        public static void Setkey(string keyname, string defaultvalue = "")
        {
            using var oldRegistryKey = Registry.CurrentUser.OpenSubKey(Registerkey, true);
            if (oldRegistryKey != null)
            {
                oldRegistryKey.SetValue(keyname, defaultvalue);
            }
            else
            {
                using var newRegistryKey = Registry.CurrentUser.CreateSubKey(Registerkey);
                newRegistryKey?.SetValue(keyname, defaultvalue);
            }
        }
    }
}
