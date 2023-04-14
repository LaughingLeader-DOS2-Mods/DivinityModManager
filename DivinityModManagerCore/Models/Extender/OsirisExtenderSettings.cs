using DivinityModManager.Extensions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models.Extender
{
    [DataContract]
    public class OsirisExtenderSettings : ReactiveObject
    {
        private bool extenderIsAvailable = false;

        public bool ExtenderIsAvailable
        {
            get => extenderIsAvailable;
            set { this.RaiseAndSetIfChanged(ref extenderIsAvailable, value); }
        }

        private bool extenderUpdaterIsAvailable = false;

        public bool ExtenderUpdaterIsAvailable
        {
            get => extenderUpdaterIsAvailable;
            set { this.RaiseAndSetIfChanged(ref extenderUpdaterIsAvailable, value); }
        }

        private int extenderVersion = -1;

        public int ExtenderVersion
        {
            get => extenderVersion;
            set { this.RaiseAndSetIfChanged(ref extenderVersion, value); }
        }

        [SettingsEntry("Enable Extensions", "Make the Osiris extension functionality available ingame or in the editor")]
        [Reactive]
        [DataMember]
        [DefaultValue(true)]
        public bool EnableExtensions { get; set; } = true;

        [SettingsEntry("Create Console", "Creates a console window that logs extender internals\nMainly useful for debugging")]
        [Reactive]
        [DataMember]
        [DefaultValue(false)]
        public bool CreateConsole { get; set; }

        [SettingsEntry("Log Working Story Errors", "Log errors during Osiris story compilation to a log file (LogFailedCompile)")]
        [Reactive]
        [DataMember]
        [DefaultValue(true)]
        public bool LogFailedCompile { get; set; } = true;

        [SettingsEntry("Enable Osiris Logging", "Enable logging of Osiris activity (rule evaluation, queries, etc.) to a log file")]
        [Reactive]
        [DataMember]
        [DefaultValue(false)]
        public bool EnableLogging { get; set; }

        [SettingsEntry("Log Script Compilation", "Log Osiris story compilation to a log file")]
        [Reactive]
        [DataMember]
        [DefaultValue(false)]
        public bool LogCompile { get; set; }

        [SettingsEntry("Log Directory", "Directory where the generated Osiris logs will be stored\nDefault is Documents\\OsirisLogs")]
        [Reactive]
        [DataMember]
        public string LogDirectory { get; set; } = "";

        [SettingsEntry("Log Runtime", "Log extender console and script output to a log file")]
        [Reactive]
        [DataMember]
        [DefaultValue(false)]
        public bool LogRuntime { get; set; }

        [SettingsEntry("Disable Mod Validation", "Disable module hashing when loading mods\nSpeeds up mod loading with no drawbacks")]
        [Reactive]
        [DataMember]
        [DefaultValue(true)]
        public bool DisableModValidation { get; set; } = true;

        [SettingsEntry("Enable Achievements", "Re-enable achievements for modded games")]
        [Reactive]
        [DataMember]
        [DefaultValue(true)]
        public bool EnableAchievements { get; set; } = true;

        [SettingsEntry("Send Crash Reports", "Upload minidumps to the crash report collection server after a game crash")]
        [Reactive]
        [DataMember]
        [DefaultValue(true)]
        public bool SendCrashReports { get; set; } = true;

        [SettingsEntry("Enable Osiris Debugger", "Enables the Osiris debugger interface (vscode extension)", true)]
        [Reactive]
        [DataMember]
        [DefaultValue(false)]
        public bool EnableDebugger { get; set; }

        [SettingsEntry("Osiris Debugger Port", "Port number the Osiris debugger will listen on\nDefault: 9999", true)]
        [Reactive]
        [DataMember]
        [DefaultValue(9999)]
        public int DebuggerPort { get; set; } = 9999;

        [SettingsEntry("Dump Network Strings", "Dumps the NetworkFixedString table to LogDirectory\nMainly useful for debugging desync issues", true)]
        [Reactive]
        [DataMember]
        [DefaultValue(false)]
        public bool DumpNetworkStrings { get; set; }

        [SettingsEntry("Osiris Debugger Flags", "Debugger flags to set\nDefault: 0")]
        [Reactive]
        [DataMember]
        [DefaultValue(0)]
        public int DebuggerFlags { get; set; } = 0;

        [SettingsEntry("Enable Developer Mode", "Enables various debug functionality for development purposes\nThis can be checked by mods to enable additional log messages and more")]
        [Reactive]
        [DataMember]
        [DefaultValue(false)]
        public bool DeveloperMode { get; set; }

        [SettingsEntry("Enable Lua Debugger", "Enables the Lua debugger interface (vscode extension)", true)]
        [Reactive]
        [DataMember]
        [DefaultValue(false)]
        public bool EnableLuaDebugger { get; set; }

        [SettingsEntry("Lua Builtin Directory", "An additional directory where the Script Extender will check for builtin scripts\nThis setting is meant for ositools developers, to make it easier to test builtin script changes", true)]
        [Reactive]
        [DataMember]
        [DefaultValue("")]
        public string LuaBuiltinResourceDirectory { get; set; } = "";

		[SettingsEntry("Default to Client Side", "Defaults the extender console to the client-side\nThis is setting is intended for developers", true)]
		[Reactive]
		[DataMember]
		[DefaultValue(false)]
		public bool DefaultToClientConsole { get; set; }

		[SettingsEntry("Show Performance Warnings", "Print warnings to the extender console window, which indicates when the server-side part of the game lags behind (a.k.a. warnings about ticks taking too long).", true)]
		[Reactive]
		[DataMember]
		[DefaultValue(false)]
		public bool ShowPerfWarnings { get; set; }

		public static OsirisExtenderSettings DefaultSettings = new OsirisExtenderSettings();

        public void SetToDefault()
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(GetType());
            foreach (PropertyDescriptor pr in props)
            {
                if (pr.CanResetValue(this))
                {
                    pr.ResetValue(this);
                }
            }
        }

        public void Set(OsirisExtenderSettings osirisExtenderSettings)
        {
            EnableExtensions = osirisExtenderSettings.EnableExtensions;
            CreateConsole = osirisExtenderSettings.CreateConsole;
            EnableLogging = osirisExtenderSettings.EnableLogging;
            LogCompile = osirisExtenderSettings.LogCompile;
            if (osirisExtenderSettings.LogDirectory.IsExistingDirectory()) LogDirectory = osirisExtenderSettings.LogDirectory;
            DisableModValidation = osirisExtenderSettings.DisableModValidation;
            EnableAchievements = osirisExtenderSettings.EnableAchievements;
            SendCrashReports = osirisExtenderSettings.SendCrashReports;
            EnableDebugger = osirisExtenderSettings.EnableDebugger;
            DebuggerPort = osirisExtenderSettings.DebuggerPort;
            DebuggerFlags = osirisExtenderSettings.DebuggerFlags;
            DeveloperMode = osirisExtenderSettings.DeveloperMode;
        }
    }
}
