using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Versioning;

namespace MSBump
{
    public class BumpVersion : Task
    {
        private int Bump(int oldValue, string action)
        {
            if (string.IsNullOrEmpty(action))
                return oldValue;
            action = action.ToUpper();
            switch (action)
            {
                case "INCREMENT":
                case "+":
                    return oldValue + 1;
            }
            throw new ArgumentException($"{action} is not a valid action for {GetType().Name}");
        }
        public override bool Execute()
        {
            var proj = XDocument.Load(ProjectPath, LoadOptions.PreserveWhitespace);
            var xversion = proj.Root.XPathSelectElement("PropertyGroup/Version");
            if (xversion == null)
            {
                BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Version property not found in {ProjectPath}", null, GetType().Name, MessageImportance.High));
                return false;
            }
            BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Old version is {xversion.Value}", null, GetType().Name, MessageImportance.Low));
            var version = new NuGetVersion(xversion.Value);
            int major = version.Major;
            int minor = version.Minor;
            int patch = version.Patch;
            int revision = version.Revision;
            var labels = version.ReleaseLabels.ToList();

            major = Bump(major, Major);
            minor = Bump(minor, Minor);
            patch = Bump(patch, Patch);
            revision = Bump(revision, Revision);
            if (!string.IsNullOrEmpty(Label) && !string.IsNullOrEmpty(LabelAction))
            {
                if (!Label.All(Char.IsLetterOrDigit))
                {
                    BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Invalid version label for {GetType().Name}: {Label} - only alphanumeric characters are allowed", null, GetType().Name, MessageImportance.High));
                    return false;
                }
                var regex = new Regex($"^{Label}(\\d+)$");
                int idx = -1;
                foreach (var label in labels)
                {
                    var match = regex.Match(label);
                    if (match.Success)
                    {
                        var value = Bump(int.Parse(match.Groups[1].Value), LabelAction);
                        idx = labels.IndexOf(label);
                        labels[idx] = Label + value.ToString(new string('0', LabelDigits));
                        break;
                    }
                }
                if (idx < 0)
                {
                    var label = Label + new string('0', LabelDigits - 1) + "1";
                    labels.Add(label);
                }

            }
            var newVersion = new NuGetVersion(major, minor, patch, revision, labels, version.Metadata);
            if (newVersion != version)
            {
                BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Changing package version to {newVersion.ToString()}...", null, GetType().Name, MessageImportance.High));
                xversion.Value = newVersion.ToString();
                using (var stream = File.Create(ProjectPath))
                    proj.Save(stream);
            }
            return true;
        }

        [Required]
        public string ProjectPath { get; set; }

        public string Major { get; set; }

        public string Minor { get; set; }

        public string Patch { get; set; }

        public string Revision { get; set; }

        public string Label { get; set; }

        public string LabelAction { get; set; }

        public int LabelDigits { get; set; } = 6;
    }
}
