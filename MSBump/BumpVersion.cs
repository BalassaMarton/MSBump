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
        private int GetNextValue(int oldValue, bool bump, bool reset)
        {
            if (reset)
                return 0;
            if (bump)
                return oldValue + 1;
            return oldValue;
        }

        public override bool Execute()
        {
            var proj = XDocument.Load(ProjectPath, LoadOptions.PreserveWhitespace);
            var xversion = proj.Root.XPathSelectElement("PropertyGroup/Version");
            if (xversion == null)
            {
                BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Version property not found in {ProjectPath}", null, GetType().Name, MessageImportance.High));
                return true;
            }
            BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Old version is {xversion.Value}", null, GetType().Name, MessageImportance.Low));
            var version = new NuGetVersion(xversion.Value);
            int major = version.Major;
            int minor = version.Minor;
            int patch = version.Patch;
            int revision = version.Revision;
            var labels = version.ReleaseLabels.ToList();

            major = GetNextValue(major, BumpMajor, ResetMajor);
            minor = GetNextValue(minor, BumpMinor, ResetMinor);
            patch = GetNextValue(patch, BumpPatch, ResetPatch);
            revision = GetNextValue(revision, BumpRevision, ResetRevision);

            if (!string.IsNullOrEmpty(ResetLabel))
            {
                if (!ResetLabel.All(Char.IsLetterOrDigit))
                {
                    BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Invalid version label for {GetType().Name}: {ResetLabel} - only alphanumeric characters are allowed", null, GetType().Name, MessageImportance.High));
                    return false;
                }
                var regex = new Regex($"^{ResetLabel}(\\d*)$");
                foreach (var label in labels)
                {
                    var match = regex.Match(label);
                    if (match.Success)
                    {
                        labels.Remove(label);
                        break;
                    }
                }
            }
            // Find and modify the release label selected with `BumpLabel`
            // If ResetLabel is true, remove only the specified label.
            else if (!string.IsNullOrEmpty(BumpLabel))
            {
                if (!BumpLabel.All(Char.IsLetterOrDigit))
                {
                    BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Invalid version label for {GetType().Name}: {BumpLabel} - only alphanumeric characters are allowed", null, GetType().Name, MessageImportance.High));
                    return false;
                }
                var regex = new Regex($"^{BumpLabel}(\\d*)$");
                var value = 0;
                foreach (var label in labels)
                {
                    var match = regex.Match(label);
                    if (match.Success)
                    {
                        if (!string.IsNullOrEmpty(match.Groups[1].Value))
                            value = int.Parse(match.Groups[1].Value);
                        labels.Remove(label);
                        break;
                    }
                }
                value++;
                labels.Add(BumpLabel + value.ToString(new string('0', LabelDigits)));
            }
            var newVersion = new NuGetVersion(major, minor, patch, revision, labels, version.Metadata);
            if (newVersion != version)
            {
                BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"Changing package version to {newVersion.ToString()}...", null, GetType().Name, MessageImportance.High));
                xversion.Value = newVersion.ToString();
                using (var stream = File.Create(ProjectPath))
                    proj.Save(stream);
            }
            NewVersion = newVersion.ToString();
            return true;
        }

        [Required]
        public string ProjectPath { get; set; }

        public bool BumpMajor { get; set; }

        public bool BumpMinor { get; set; }

        public bool BumpPatch { get; set; }

        public bool BumpRevision { get; set; }

        public string BumpLabel { get; set; }

        public bool ResetMajor { get; set; }

        public bool ResetMinor { get; set; }

        public bool ResetPatch { get; set; }

        public bool ResetRevision { get; set; }

        public string ResetLabel { get; set; }

        public int LabelDigits { get; set; } = 6;

        [Output]
        public string NewVersion { get; set; }
    }
}
