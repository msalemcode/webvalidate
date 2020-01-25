using System;
using System.Globalization;

namespace Helium
{
    /// <summary>
    /// Assembly Versioning
    /// </summary>
    public sealed class Version
    {
        static string _version = string.Empty;

        public static string AssemblyVersion
        {
            get
            {
                if (string.IsNullOrEmpty(_version))
                {
                    string file = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    DateTime dt = System.IO.File.GetCreationTime(file);

                    System.Version aVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                    _version = string.Format(CultureInfo.InvariantCulture, $"{aVer.Major}.{aVer.Minor}.{dt.ToString("MMdd.HHmm", CultureInfo.InvariantCulture)}");
                }

                return _version;
            }
        }
    }
}