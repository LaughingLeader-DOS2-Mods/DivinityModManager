using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Windows.Input;
using DivinityModManager.Util;
using System.Reactive.Disposables;
using System.Reflection;
using Alphaleonis.Win32.Filesystem;
using DivinityModManager.Models.App;
using System.Reactive;
using ReactiveUI.Fody.Helpers;

namespace DivinityModManager.Models
{
	[DataContract]
	public class DivinityModManagerSettings : ReactiveObject, IDisposable
	{
		[SettingsEntry("Game Data Path", "The path to the Data folder, for loading editor mods.&#x0a;Example: Divinity Original Sin 2/DefEd/Data")]

		[DataMember] [Reactive]
		public string GameDataPath { get; set; } = "";

		[SettingsEntry("Game Executable Path", "The path to the game exe, EoCApp.exe.")]

		[DataMember] [Reactive]
		public string GameExecutablePath { get; set; } = "";

		//Old. Will be read, but not written.
		[DataMember]
		public string DOS2DEGameExecutable { set => GameExecutablePath = value; }

		[SettingsEntry("Enable Story Log", "When launching the game, enable the Osiris story log (osiris.log).")]
		[DataMember] [Reactive] public bool GameStoryLogEnabled { get; set; } = false;


		[SettingsEntry("Workshop Path", "The workshop folder.&#x0a;Used for detecting mod updates and new mods to be copied into the local mods folder.")]
		[DataMember] [Reactive] public string WorkshopPath { get; set; } = "";


		[SettingsEntry("Saved Load Orders Path", "The folder containing mod load orders.")]
		[DataMember] [Reactive] public string LoadOrderPath { get; set; } = "Orders";


		[SettingsEntry("Enable Internal Log", "Enable the log for the mod manager.")]
		[DataMember] [Reactive] public bool LogEnabled { get; set; } = false;

		[SettingsEntry("Auto Add Missing Dependencies When Exporting", "Automatically add dependency mods above their dependents in the exported load order, if omitted from the active order.")]
		[DataMember] [Reactive] public bool AutoAddDependenciesWhenExporting { get; set; } = true;

		[SettingsEntry("Enable Automatic Updates", "Automatically check for updates when the program starts.")]
		[DataMember] [Reactive] public bool CheckForUpdates { get; set; } = true;

		[SettingsEntry("Automatically Load GM Campaign Mods", "When a GM campaign is selected, its dependency mods will automatically be loaded without needing to manually import them.")]
		[DataMember] [Reactive] public bool AutomaticallyLoaGMCampaignMods { get; set; } = false;

		[DataMember][Reactive] public long LastUpdateCheck { get; set; } = -1;
		private string lastOrder = "";

		[DataMember]
		public string LastOrder
		{
			get => lastOrder;
			set { this.RaiseAndSetIfChanged(ref lastOrder, value); }
		}

		private string lastLoadedOrderFilePath = "";

		[DataMember]
		public string LastLoadedOrderFilePath
		{
			get => lastLoadedOrderFilePath;
			set { this.RaiseAndSetIfChanged(ref lastLoadedOrderFilePath, value); }
		}

		private string lastExtractOutputPath = "";

		[DataMember]
		public string LastExtractOutputPath
		{
			get => lastExtractOutputPath;
			set { this.RaiseAndSetIfChanged(ref lastExtractOutputPath, value); }
		}

		private bool darkThemeEnabled = true;

		[DataMember]
		public bool DarkThemeEnabled
		{
			get => darkThemeEnabled;
			set { this.RaiseAndSetIfChanged(ref darkThemeEnabled, value); }
		}

		[SettingsEntry("Shift Focus on Swap", "When moving selected mods to the opposite list with Enter, move focus to that list as well.")]
		[DataMember] [Reactive] public bool ShiftListFocusOnSwap { get; set; } = false;

		private OsiExtenderSettings extenderSettings;

		[DataMember]
		public OsiExtenderSettings ExtenderSettings
		{
			get => extenderSettings;
			set { this.RaiseAndSetIfChanged(ref extenderSettings, value); }
		}

		public string ExtenderLogDirectory
		{
			get
			{
				if (ExtenderSettings == null || String.IsNullOrWhiteSpace(ExtenderSettings.LogDirectory))
				{

					return Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "OsirisLogs");
				}
				return ExtenderSettings.LogDirectory;
			}
		}


		private DivinityGameLaunchWindowAction actionOnGameLaunch = DivinityGameLaunchWindowAction.None;

		[DataMember]
		public DivinityGameLaunchWindowAction ActionOnGameLaunch
		{
			get => actionOnGameLaunch;
			set { this.RaiseAndSetIfChanged(ref actionOnGameLaunch, value); }
		}

		[SettingsEntry("Disable Missing Mod Warnings", "If a load order is missing mods, no warnings will be displayed.")]
		[DataMember] [Reactive] public bool DisableMissingModWarnings { get; set; } = false;

		[SettingsEntry("Disable Checking for Steam Workshop Tags", "The mod manager will try and find mod tags from the workshop by default.")]
		[DataMember] [Reactive] public bool DisableWorkshopTagCheck { get; set; } = false;

		[DataMember] [Reactive] public bool ExportDefaultExtenderSettings { get; set; } = false;

		//Not saved for now

		private bool displayFileNames = false;

		public bool DisplayFileNames
		{
			get => displayFileNames;
			set { this.RaiseAndSetIfChanged(ref displayFileNames, value); }
		}

		private bool debugModeEnabled = false;

		[SettingsEntry("Enable Developer Mode", "This enables features for mod developers, such as being able to copy a mod's UUID in context menus, and additional OsiExtender options.")]
		[DataMember]
		public bool DebugModeEnabled
		{
			get => debugModeEnabled;
			set
			{
				this.RaiseAndSetIfChanged(ref debugModeEnabled, value);
				DivinityApp.DeveloperModeEnabled = value;
			}
		}

		[Reactive] [DataMember] public string GameLaunchParams { get; set; }

		[Reactive] [DataMember] public bool GameMasterModeEnabled { get; set; } = false;
		[Reactive] public bool ExtenderTabIsVisible { get; set; } = false;

		[Reactive] public bool KeybindingsTabIsVisible { get; set; } = false;

		private Hotkey selectedHotkey;

		public Hotkey SelectedHotkey
		{
			get => selectedHotkey;
			set { this.RaiseAndSetIfChanged(ref selectedHotkey, value); }
		}

		[Reactive] public int SelectedTabIndex { get; set; } = 0;

		public ICommand SaveSettingsCommand { get; set; }
		public ICommand OpenSettingsFolderCommand { get; set; }
		public ICommand ExportExtenderSettingsCommand { get; set; }
		public ICommand ResetExtenderSettingsToDefaultCommand { get; set; }
		public ICommand ResetKeybindingsCommand { get; set; }
		public ICommand ClearWorkshopCacheCommand { get; set; }
		public ICommand AddLaunchParamCommand { get; set; }
		public ICommand ClearLaunchParamsCommand { get; set; }

		public CompositeDisposable Disposables { get; internal set; }

		private bool canSaveSettings = false;

		public bool CanSaveSettings
		{
			get => canSaveSettings;
			set { this.RaiseAndSetIfChanged(ref canSaveSettings, value); }
		}

		public bool SettingsWindowIsOpen { get; set; } = false;

		public void Dispose()
		{
			Disposables?.Dispose();
			Disposables = null;
		}

		public DivinityModManagerSettings()
		{
			Disposables = new CompositeDisposable();
			ExtenderSettings = new OsiExtenderSettings();

			var properties = typeof(DivinityModManagerSettings)
			.GetRuntimeProperties()
			.Where(prop => Attribute.IsDefined(prop, typeof(DataMemberAttribute)))
			.Select(prop => prop.Name)
			.ToArray();

			this.WhenAnyPropertyChanged(properties).Subscribe((c) =>
			{
				if (SettingsWindowIsOpen) CanSaveSettings = true;
			}).DisposeWith(Disposables);

			var extender_properties = typeof(OsiExtenderSettings)
			.GetRuntimeProperties()
			.Where(prop => Attribute.IsDefined(prop, typeof(DataMemberAttribute)))
			.Select(prop => prop.Name)
			.ToArray();

			ExtenderSettings.WhenAnyPropertyChanged(extender_properties).Subscribe((c) =>
			{
				if (SettingsWindowIsOpen) CanSaveSettings = true;
				this.RaisePropertyChanged("ExtenderLogDirectory");
			}).DisposeWith(Disposables);

			this.WhenAnyValue(x => x.SelectedTabIndex, (index) => index == 1).BindTo(this, x => x.ExtenderTabIsVisible);
			this.WhenAnyValue(x => x.SelectedTabIndex, (index) => index == 2).BindTo(this, x => x.KeybindingsTabIsVisible);
		}
	}
}
