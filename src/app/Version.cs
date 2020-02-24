using System;
using System.Globalization;

namespace WebValidationApp
{
    /// <summary>
    /// Assembly Versioning
    /// </summary>
    public sealed class Version
    {
        // cache the assembly version
        static string _version = string.Empty;

        public static string AssemblyVersion
        {
            get
            {
                if (string.IsNullOrEmpty(_version))
                {
                    // use reflection to get the assembly version
                    string file = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    DateTime dt = System.IO.File.GetCreationTime(file);
                    System.Version aVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                    // use major.minor and the build date as the version
                    _version = string.Format(CultureInfo.InvariantCulture, $"{aVer.Major}.{aVer.Minor}.{dt.ToString("MMdd.HHmm", CultureInfo.InvariantCulture)}");
                }

                return _version;
            }
        }
    }
}