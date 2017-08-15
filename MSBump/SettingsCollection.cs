using System.Collections.Generic;

namespace MSBump
{
    public class SettingsCollection : Settings
    {
        public Dictionary<string, Settings> Configurations { get; set; }
    }
}