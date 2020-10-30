using DivinityModManager.Extensions;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	[DataContract]
	public class OsiExtenderSettings : ReactiveObject
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

		private bool enableExtensions = true;

		[DataMember]
		[DefaultValue(true)]
		public bool EnableExtensions
		{
			get => enableExtensions;
			set { this.RaiseAndSetIfChanged(ref enableExtensions, value); }
		}

		private bool createConsole = false;

		[DataMember]
		[DefaultValue(false)]
		public bool CreateConsole
		{
			get => createConsole;
			set { this.RaiseAndSetIfChanged(ref createConsole, value); }
		}

		private bool logFailedCompile = true;

		[DataMember]
		[DefaultValue(true)]
		public bool LogFailedCompile
		{
			get => logFailedCompile;
			set { this.RaiseAndSetIfChanged(ref logFailedCompile, value); }
		}

		private bool enableLogging = false;

		[DataMember]
		[DefaultValue(false)]
		public bool EnableLogging
		{
			get => enableLogging;
			set { this.RaiseAndSetIfChanged(ref enableLogging, value); }
		}

		private bool logCompile = false;

		[DataMember]
		[DefaultValue(false)]
		public bool LogCompile
		{
			get => logCompile;
			set { this.RaiseAndSetIfChanged(ref logCompile, value); }
		}

		private string logDirectory;

		[DataMember]
		public string LogDirectory
		{
			get => logDirectory;
			set { this.RaiseAndSetIfChanged(ref logDirectory, value); }
		}

		private bool logRuntime = false;

		[DataMember]
		[DefaultValue(false)]
		public bool LogRuntime
		{
			get => logRuntime;
			set { this.RaiseAndSetIfChanged(ref logRuntime, value); }
		}

		private bool disableModValidation = true;

		[DataMember]
		[DefaultValue(true)]
		public bool DisableModValidation
		{
			get => disableModValidation;
			set { this.RaiseAndSetIfChanged(ref disableModValidation, value); }
		}

		private bool enableAchievements = true;

		[DataMember]
		[DefaultValue(true)]
		public bool EnableAchievements
		{
			get => enableAchievements;
			set { this.RaiseAndSetIfChanged(ref enableAchievements, value); }
		}

		private bool sendCrashReports = true;

		[DataMember]
		[DefaultValue(true)]
		public bool SendCrashReports
		{
			get => sendCrashReports;
			set { this.RaiseAndSetIfChanged(ref sendCrashReports, value); }
		}

		private bool enableDebugger = false;

		[DataMember]
		[DefaultValue(false)]
		public bool EnableDebugger
		{
			get => enableDebugger;
			set { this.RaiseAndSetIfChanged(ref enableDebugger, value); }
		}

		private int debuggerPort = 9999;

		[DataMember]
		[DefaultValue(9999)]
		public int DebuggerPort
		{
			get => debuggerPort;
			set { this.RaiseAndSetIfChanged(ref debuggerPort, value); }
		}

		private int debuggerFlags = 0;

		[DataMember]
		[DefaultValue(0)]
		public int DebuggerFlags
		{
			get => debuggerFlags;
			set { this.RaiseAndSetIfChanged(ref debuggerFlags, value); }
		}

		private bool dumpNetworkStrings = false;

		[DataMember]
		[DefaultValue(false)]
		public bool DumpNetworkStrings
		{
			get => dumpNetworkStrings;
			set { this.RaiseAndSetIfChanged(ref dumpNetworkStrings, value); }
		}

		private bool developerMode = false;

		[DataMember]
		[DefaultValue(false)]
		public bool DeveloperMode
		{
			get => developerMode;
			set { this.RaiseAndSetIfChanged(ref developerMode, value); }
		}

		private bool enableLuaDebugger = false;

		[DataMember]
		[DefaultValue(false)]
		public bool EnableLuaDebugger
		{
			get => enableLuaDebugger;
			set { this.RaiseAndSetIfChanged(ref enableLuaDebugger, value); }
		}

		public static OsiExtenderSettings DefaultSettings = new OsiExtenderSettings();

		public void SetToDefault()
		{
			EnableExtensions = true;
			CreateConsole = false;
			EnableLogging = false;
			LogFailedCompile = true;
			LogCompile = false;
			LogRuntime = false;
			LogDirectory = "";
			DisableModValidation = true;
			EnableAchievements = true;
			SendCrashReports = true;
			EnableDebugger = false;
			DebuggerPort = 9999;
			DebuggerFlags = 0;
			DeveloperMode = false;
			EnableLuaDebugger = false;
		}

		public void Set(OsiExtenderSettings osirisExtenderSettings)
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
