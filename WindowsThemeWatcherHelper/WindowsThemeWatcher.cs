using System;
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Security.Principal;
using Microsoft.Win32;

namespace WindowsThemeWatcherHelper
{
    public static class WindowsThemeWatcher
    {
        const string registryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        const string registryValueName = "AppsUseLightTheme";

        public static EventHandler? Changed;

        static WindowsThemeWatcher()
        {
            var registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\", false);
            if (registryKey is null)
                return;

            var majorValue = registryKey.GetValue("CurrentMajorVersionNumber");

            if (majorValue is < 10)
                return;

            if (Environment.OSVersion.Version.Major < 10)
                return;

            var currentUser = WindowsIdentity.GetCurrent();

            if (currentUser.User == null)
                throw new NullReferenceException();

            var query = string.Format(
                CultureInfo.InvariantCulture,
                @"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = '{0}\\{1}' AND ValueName = '{2}'",
                currentUser.User.Value,
                registryKeyPath.Replace(@"\", @"\\"),
                registryValueName);

            try
            {
                var themeWatcher = new ManagementEventWatcher(query);

                themeWatcher.EventArrived += delegate
                {
                    Changed?.Invoke(themeWatcher, EventArgs.Empty);
                };

                themeWatcher.Start();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

        public static bool IsDark()
        {
            using var key = Registry.CurrentUser.OpenSubKey(registryKeyPath);
            var registryValueObject = key?.GetValue(registryValueName);
            var registryValue = (int?)registryValueObject;

            return registryValue <= 0;
        }
    }
}
