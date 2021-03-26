using Alphaleonis.Win32.Filesystem;
using DivinityModManager.Models;
using DivinityModManager.Util;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;

namespace DivinityModManager
{
	public static class DivinityApp
	{
		public const string DIR_DATA = "Data\\";
		public const string URL_REPO = @"https://github.com/LaughingLeader-DOS2-Mods/DivinityModManager";
		public const string URL_CHANGELOG = @"https://github.com/LaughingLeader-DOS2-Mods/DivinityModManager/wiki/Changelog";
		public const string URL_UPDATE = @"https://raw.githubusercontent.com/LaughingLeader-DOS2-Mods/DivinityModManager/master/Update.xml";
		public const string URL_AUTHOR = @"https://github.com/LaughingLeader";
		public const string URL_ISSUES = @"https://github.com/LaughingLeader-DOS2-Mods/DivinityModManager/issues";
		public const string URL_LICENSE = @"https://github.com/LaughingLeader-DOS2-Mods/DivinityModManager/blob/master/LICENSE";
		public const string URL_DONATION = @"https://ko-fi.com/laughingleader";

		public const string XML_MOD_ORDER_MODULE = @"<node id=""Module""><attribute id=""UUID"" value=""{0}"" type=""22""/></node>";
		public const string XML_MODULE_SHORT_DESC = @"<node id=""ModuleShortDesc""><attribute id=""Folder"" value=""{0}"" type=""30""/><attribute id=""MD5"" value=""{1}"" type=""23""/><attribute id=""Name"" value=""{2}"" type=""22""/><attribute id=""UUID"" value=""{3}"" type=""22"" /><attribute id=""Version"" value=""{4}"" type=""4""/></node>";
		public const string XML_MOD_SETTINGS_TEMPLATE = @"<?xml version=""1.0"" encoding=""UTF-8""?><save><header version=""2""/><version major=""3"" minor=""6"" revision=""9"" build=""0""/><region id=""ModuleSettings""><node id=""root""><children><node id=""ModOrder""><children>{0}</children></node><node id=""Mods""><children>{1}</children></node></children></node></region></save>";
		
		public const string PATH_APP_FEATURES = @"Resources/AppFeatures.json";
		public const string PATH_DEFAULT_PATHWAYS = @"Resources/DefaultPathways.json";
		public const string PATH_IGNORED_MODS = @"Resources/IgnoredMods.json";

		public const string ORIGINS_UUID = "1301db3d-1f54-4e98-9be5-5094030916e4";
		public const string GAMEMASTER_UUID = "00550ab2-ac92-410c-8d94-742f7629de0e";

		public static readonly Uri LightTheme = new Uri("pack://application:,,,/DivinityModManager;component/Themes/Light.xaml", UriKind.Absolute);
		public static readonly Uri DarkTheme = new Uri("pack://application:,,,/DivinityModManager;component/Themes/Dark.xaml", UriKind.Absolute);

		public static HashSet<DivinityModData> IgnoredMods { get; set; } = new HashSet<DivinityModData>();
		public static HashSet<DivinityModData> IgnoredDependencyMods { get; set; } = new HashSet<DivinityModData>();

		public static DivinityGlobalCommands Commands { get; private set; } = new DivinityGlobalCommands();
		public static DivinityGlobalEvents Events { get; private set; } = new DivinityGlobalEvents();

		public static event PropertyChangedEventHandler StaticPropertyChanged;

		private static void NotifyStaticPropertyChanged([CallerMemberName] string name = null)
		{
			StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(name));
		}

		private static bool developerModeEnabled = false;

		public static bool DeveloperModeEnabled
		{
			get => developerModeEnabled;
			set 
			{ 
				developerModeEnabled = value;
				NotifyStaticPropertyChanged();
			}
		}

		private static bool isKeyboardNavigating = false;

		public static bool IsKeyboardNavigating
		{
			get => isKeyboardNavigating;
			set {
				isKeyboardNavigating = value;
				Log($"isKeyboardNavigating({isKeyboardNavigating})");
				NotifyStaticPropertyChanged();
			}
		}

		public static IObservable<Func<DivinityModDependencyData, bool>> DependencyFilter { get; set; }

		public static string DateTimeColumnFormat { get; set; } = "MM/dd/yyyy";
		public static string DateTimeTooltipFormat { get; set; } = "MMMM dd, yyyy";

		public static void Log(string msg, [CallerMemberName] string mName = "", [CallerFilePath] string path = "", [CallerLineNumber] int line = 0)
		{
			System.Diagnostics.Trace.WriteLine($"[{Path.GetFileName(path)}:{mName}({line})] {msg}");
		}

		[DllImport("user32.dll")]
		static extern bool SystemParametersInfo(int iAction, int iParam, out bool bActive, int iUpdate);

		public static bool IsScreenReaderActive()
		{
			int iAction = 70; // SPI_GETSCREENREADER constant;
			int iParam = 0;
			int iUpdate = 0;
			bool bActive = false;
			bool bReturn = SystemParametersInfo(iAction, iParam, out bActive, iUpdate);
			return bReturn && bActive;
			//if (AutomationPeer.ListenerExists(AutomationEvents.AutomationFocusChanged) || AutomationPeer.ListenerExists(AutomationEvents.LiveRegionChanged))
			//{
			//	return true;
			//}
			//return false;
		}
	}
}
