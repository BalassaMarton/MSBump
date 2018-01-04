using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;

namespace MSBump
{
    public class BumpVersion : Task
    {
        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.Low, "MSBump task started");
            try
            {
                var proj = XDocument.Load(ProjectPath, LoadOptions.PreserveWhitespace);

                Settings settings = LoadSettingsFromFile(Path.ChangeExtension(ProjectPath, ".msbump")) ??
                                    LoadSettingsFromFile(Path.Combine(Path.GetDirectoryName(ProjectPath), ".msbump")) ??
                                    new Settings
                                    {
                                        BumpMajor = BumpMajor,
                                        BumpMinor = BumpMinor,
                                        BumpPatch = BumpPatch,
                                        BumpRevision = BumpRevision,
                                        BumpLabel = BumpLabel,
                                        ResetMajor = ResetMajor,
                                        ResetMinor = ResetMinor,
                                        ResetPatch = ResetPatch,
                                        ResetRevision = ResetRevision,
                                        ResetLabel = ResetLabel,
                                        LabelDigits = LabelDigits == 0 ? Settings.DefaultLabelDigits : LabelDigits
                                    };

				Log.LogMessage(MessageImportance.Low, $"MSBump settings = {JObject.FromObject(settings).ToString()}");

				if (TryBump(proj, "Version", settings) | TryBump(proj, "AssemblyVersion", settings))
				{
                    Log.LogMessage(MessageImportance.Low, "Saving project file");
                    using (var stream = File.Create(ProjectPath))
                    {
                        stream.Flush();
                        proj.Save(stream);
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                return false;
            }
            return true;
        }

        private Settings LoadSettingsFromFile(string settingsFilePath)
        {
            if (File.Exists(settingsFilePath))
            {
                Settings settings = null;
                Log.LogMessage(MessageImportance.Low, $"Loading MSBump settings from file \"{settingsFilePath}\"");
                var settingsCollection = JsonSerializer.Create()
                    .Deserialize<SettingsCollection>(new JsonTextReader(File.OpenText(settingsFilePath)));
                if (!string.IsNullOrEmpty(Configuration))
                    settingsCollection.Configurations?.TryGetValue(Configuration, out settings);
                return settings ?? settingsCollection;
            }
            Log.LogMessage(MessageImportance.Low, $"MSBump settings file \"{settingsFilePath}\" not found");
            return null;
        }

        private bool TryBump(XDocument proj, string tagName, Settings settings)
        {
            // ReSharper disable once PossibleNullReferenceException
	        var defaultNamespace = proj.Root.GetDefaultNamespace();
	        var defaultNamespacePrefix = "ns";
	        var xmlNamespaceManager = new XmlNamespaceManager(new NameTable());

	        xmlNamespaceManager.AddNamespace(defaultNamespacePrefix, defaultNamespace.NamespaceName);

	        var element = proj.Root.XPathSelectElement($"{defaultNamespacePrefix}:PropertyGroup/{defaultNamespacePrefix}:{tagName}", xmlNamespaceManager);

            if (element == null)
                return false;
            var oldVersion = new NuGetVersion(element.Value);
            Log.LogMessage(MessageImportance.Low, $"Old {tagName} is {element.Value}");

            int GetNextValue(int oldValue, bool bump, bool reset)
            {
                if (reset)
                    return 0;
                if (bump)
                    return oldValue + 1;
                return oldValue;
            }

            var major = GetNextValue(oldVersion.Major, settings.BumpMajor, settings.ResetMajor);
            var minor = GetNextValue(oldVersion.Minor, settings.BumpMinor, settings.ResetMinor);
            var patch = GetNextValue(oldVersion.Patch, settings.BumpPatch, settings.ResetPatch);
            var revision = GetNextValue(oldVersion.Revision, settings.BumpRevision, settings.ResetRevision);

            var labels = oldVersion.ReleaseLabels.ToList();
            if (!string.IsNullOrEmpty(settings.ResetLabel))
            {
                if (!settings.ResetLabel.All(Char.IsLetterOrDigit))
                {
                    Log.LogError(
                        $"Invalid version label for {GetType().Name}: {settings.ResetLabel} - only alphanumeric characters are allowed");
                    return false;
                }
                var regex = new Regex($"^{settings.ResetLabel}(\\d*)$");
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
            if (!string.IsNullOrEmpty(settings.BumpLabel) && settings.BumpLabel != settings.ResetLabel)
            {
                if (!settings.BumpLabel.All(Char.IsLetterOrDigit))
                {
                    Log.LogError(
                        $"Invalid version label for {GetType().Name}: {settings.BumpLabel} - only alphanumeric characters are allowed");
                    return false;
                }
                var regex = new Regex($"^{settings.BumpLabel}(\\d*)$");
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
                labels.Add(settings.BumpLabel + value.ToString(new string('0', settings.LabelDigits)));
            }

            var newVersion = new NuGetVersion(major, minor, patch, revision, labels, oldVersion.Metadata);

            // Modify the project file and set output properties
            if (newVersion != oldVersion)
            {
                var newVersionStr = newVersion.ToString();
                Log.LogMessage(MessageImportance.High, $"Changing {tagName} to {newVersionStr}...");
                element.Value = newVersionStr;
                GetRequiredPropertyInfo("New" + tagName).SetValue(this, newVersionStr);
                return true;
            }
            return false;
        }

        private PropertyInfo GetRequiredPropertyInfo(string propertyName)
        {
            return GetType().GetProperty(propertyName) ??
                   throw new Exception($"Property {propertyName} is missing from type {GetType().Name}");
        }

        [Required]
        public string ProjectPath { get; set; }

        public string Configuration { get; set; }

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

		[Output]
		public string NewAssemblyVersion { get; set; }
	}
}