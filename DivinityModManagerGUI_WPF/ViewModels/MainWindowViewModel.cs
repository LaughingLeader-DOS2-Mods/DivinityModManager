using DivinityModManager.Models;
using DivinityModManager.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ReactiveUI;
using DynamicData;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Threading.Tasks;
using DynamicData.Binding;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using GongSolutions.Wpf.DragDrop;
using System.Windows;
using System.Windows.Controls;
using System.Reactive;
using System.Reactive.Concurrency;
using ReactiveUI.Legacy;
using System.ComponentModel;
using System.IO;
using System.Reactive.Concurrency;
using Newtonsoft.Json;
using Microsoft.Win32;
using DivinityModManager.Views;
using System.Globalization;
using System.IO.Compression;
using System.Threading;
using SharpCompress.Writers;
using SharpCompress.Common;
using System.Text.RegularExpressions;
using AdonisUI;
using System.Windows.Media;
using System.Reflection;
using AutoUpdaterDotNET;
using DivinityModManager.Extensions;

namespace DivinityModManager.ViewModels
{

	public class ModListDropHandler : DefaultDropHandler
	{
		override public void Drop(IDropInfo dropInfo)
		{
			base.Drop(dropInfo);

			var newSelected = new List<object>();

			if (dropInfo.Data is IEnumerable<object> list)
			{
				newSelected.AddRange(list);
			}
			else if (dropInfo.Data is ISelectable d)
			{
				newSelected.Add(d);
			}

			foreach (var obj in dropInfo.TargetCollection)
			{
				if (!newSelected.Contains(obj))
				{
					if (obj is ISelectable d)
					{
						d.IsSelected = false;
					}
				}
			}

			_viewModel.OnOrderChanged?.Invoke(_viewModel, new EventArgs());
		}

		private MainWindowViewModel _viewModel;

		public ModListDropHandler(MainWindowViewModel vm) : base()
		{
			_viewModel = vm;
		}
	}

	public class ModListDragHandler : DefaultDragHandler
	{
		public override void StartDrag(IDragInfo dragInfo)
		{
			var items = dragInfo.SourceItems.Cast<ISelectable>().Where(x => x.CanDrag).ToList();
			if (items.Count > 1)
			{
				dragInfo.Data = items;
			}
			else
			{
				// special case: if the single item is an enumerable then we can not drop it as single item
				var singleItem = items.FirstOrDefault();
				if (singleItem is System.Collections.IEnumerable)
				{
					dragInfo.Data = items.Cast<ISelectable>().Where(x => x.CanDrag);
				}
				else if(singleItem is ISelectable d && d.CanDrag)
				{
					dragInfo.Data = singleItem;
					Trace.WriteLine("Can drag item");
				}
			}

			dragInfo.Effects = dragInfo.Data != null ? DragDropEffects.Copy | DragDropEffects.Move : DragDropEffects.None;
		}

		public override bool CanStartDrag(IDragInfo dragInfo)
		{
			if(dragInfo.SourceItem is ISelectable d && !d.CanDrag)
			{
				return false;
			}
			return true;
		}
	}

	public class MainWindowViewModel : BaseHistoryViewModel, IActivatableViewModel, IDivinityAppViewModel
	{
		private MainWindow view;
		public MainWindow View => view;
		public ModListDropHandler DropHandler { get; set; }
		public ModListDragHandler DragHandler { get; set; }


		public string Title => "Divinity Mod Manager " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

		protected SourceList<DivinityModData> mods = new SourceList<DivinityModData>();

		protected ReadOnlyObservableCollection<DivinityModData> allMods;
		public ReadOnlyObservableCollection<DivinityModData> Mods => allMods;

		protected ReadOnlyObservableCollection<DivinityModData> selectedPakMods;
		public ReadOnlyObservableCollection<DivinityModData> SelectedPakMods => selectedPakMods;

		protected SourceList<DivinityModData> workshopMods = new SourceList<DivinityModData>();

		protected ReadOnlyObservableCollection<DivinityModData> workshopModsCollection;
		public ReadOnlyObservableCollection<DivinityModData> WorkshopMods => workshopModsCollection;

		public DivinityPathwayData PathwayData { get; private set; } = new DivinityPathwayData();

		public ModUpdatesViewData ModUpdatesViewData { get; private set; } = new ModUpdatesViewData();

		public DivinityModManagerSettings Settings { get; set; }

		public ObservableCollectionExtended<DivinityModData> ActiveMods { get; set; } = new ObservableCollectionExtended<DivinityModData>();
		public ObservableCollectionExtended<DivinityModData> InactiveMods { get; set; } = new ObservableCollectionExtended<DivinityModData>();
		public ObservableCollectionExtended<DivinityProfileData> Profiles { get; set; } = new ObservableCollectionExtended<DivinityProfileData>();

		private readonly ObservableAsPropertyHelper<int> activeSelected;

		public int ActiveSelected => activeSelected.Value;

		private readonly ObservableAsPropertyHelper<int> inactiveSelected;

		public int InactiveSelected => inactiveSelected.Value;

		private string activeModFilterText = "";

		public string ActiveModFilterText
		{
			get => activeModFilterText;
			set { this.RaiseAndSetIfChanged(ref activeModFilterText, value); }
		}

		private string inactiveModFilterText = "";

		public string InactiveModFilterText
		{
			get => inactiveModFilterText;
			set { this.RaiseAndSetIfChanged(ref inactiveModFilterText, value); }
		}

		private int selectedProfileIndex = 0;

		public int SelectedProfileIndex
		{
			get => selectedProfileIndex;
			set
			{
				this.RaiseAndSetIfChanged(ref selectedProfileIndex, value);
				this.RaisePropertyChanged("SelectedProfile");
			}
		}

		private readonly ObservableAsPropertyHelper<DivinityProfileData> selectedprofile;
		public DivinityProfileData SelectedProfile => selectedprofile.Value;

		public ObservableCollectionExtended<DivinityLoadOrder> ModOrderList { get; set; } = new ObservableCollectionExtended<DivinityLoadOrder>();

		private int selectedModOrderIndex = 0;

		public int SelectedModOrderIndex
		{
			get => selectedModOrderIndex;
			set
			{
				if (value != selectedModOrderIndex)
				{
					SelectedModOrder?.DisposeBinding();
				}
				this.RaiseAndSetIfChanged(ref selectedModOrderIndex, value);
				this.RaisePropertyChanged("SelectedModOrder");
			}
		}

		private readonly ObservableAsPropertyHelper<DivinityLoadOrder> selectedModOrder;
		public DivinityLoadOrder SelectedModOrder => selectedModOrder.Value;

		private readonly ObservableAsPropertyHelper<string> selectedModOrderDisplayName;
		public string SelectedModOrderDisplayName => selectedModOrderDisplayName.Value;

		public List<DivinityLoadOrder> SavedModOrderList { get; set; } = new List<DivinityLoadOrder>();

		private int layoutMode = 0;

		public int LayoutMode
		{
			get => layoutMode;
			set { this.RaiseAndSetIfChanged(ref layoutMode, value); }
		}

		private bool canSaveOrder = true;

		public bool CanSaveOrder
		{
			get => canSaveOrder;
			set { this.RaiseAndSetIfChanged(ref canSaveOrder, value); }
		}

		private bool loadingOrder = false;

		public bool LoadingOrder
		{
			get => loadingOrder;
			set { this.RaiseAndSetIfChanged(ref loadingOrder, value); }
		}

		private string statusText;

		public string StatusText
		{
			get => statusText;
			set { this.RaiseAndSetIfChanged(ref statusText, value); }
		}

		private bool conflictCheckerWindowOpen = false;

		public bool ConflictCheckerWindowOpen
		{
			get => conflictCheckerWindowOpen;
			set { this.RaiseAndSetIfChanged(ref conflictCheckerWindowOpen, value); }
		}

		private bool modUpdatesAvailable = false;

		public bool ModUpdatesAvailable
		{
			get => modUpdatesAvailable;
			set { this.RaiseAndSetIfChanged(ref modUpdatesAvailable, value); }
		}

		private bool modUpdatesViewVisible = false;

		public bool ModUpdatesViewVisible
		{
			get => modUpdatesViewVisible;
			set { this.RaiseAndSetIfChanged(ref modUpdatesViewVisible, value); }
		}

		private bool gameDirectoryFound = false;

		public bool GameDirectoryFound
		{
			get => gameDirectoryFound;
			set { this.RaiseAndSetIfChanged(ref gameDirectoryFound, value); }
		}

		#region Progress
		private string mainProgressTitle;

		public string MainProgressTitle
		{
			get => mainProgressTitle;
			set { this.RaiseAndSetIfChanged(ref mainProgressTitle, value); }
		}

		private string mainProgressWorkText;

		public string MainProgressWorkText
		{
			get => mainProgressWorkText;
			set { this.RaiseAndSetIfChanged(ref mainProgressWorkText, value); }
		}

		private bool mainProgressIsActive = true;

		public bool MainProgressIsActive
		{
			get => mainProgressIsActive;
			set { this.RaiseAndSetIfChanged(ref mainProgressIsActive, value); }
		}

		private double mainProgressValue = 0d;

		public double MainProgressValue
		{
			get => mainProgressValue;
			set { this.RaiseAndSetIfChanged(ref mainProgressValue, value); }
		}

		public void IncreaseMainProgressValue(double val, string message = "")
		{
			RxApp.MainThreadScheduler.Schedule(_ => {
				MainProgressValue += val;
				if (!String.IsNullOrEmpty(message)) MainProgressWorkText = message;
			});
		}

		private CancellationTokenSource mainProgressToken;

		public CancellationTokenSource MainProgressToken
		{
			get => mainProgressToken;
			set { this.RaiseAndSetIfChanged(ref mainProgressToken, value); }
		}

		private bool canCancelProgress = true;

		public bool CanCancelProgress
		{
			get => canCancelProgress;
			set { this.RaiseAndSetIfChanged(ref canCancelProgress, value); }
		}
		#endregion

		private IObservable<bool> canSaveSettings;
		private IObservable<bool> canOpenWorkshopFolder;
		private IObservable<bool> canOpenDOS2DEGame;
		private IObservable<bool> canOpenDialogWindow;
		private IObservable<bool> gameExeFoundObservable;

		public ICommand SaveOrderCommand { get; private set; }
		public ICommand SaveOrderAsCommand { get; private set; }
		public ICommand ExportOrderCommand { get; private set; }
		public ICommand AddOrderConfigCommand { get; private set; }
		public ICommand RefreshCommand { get; private set; }
		public ICommand ImportOrderFromSaveCommand { get; private set; }
		public ICommand ImportOrderFromSaveAsNewCommand { get; private set; }
		public ICommand ImportOrderFromFileCommand { get; private set; }
		public ICommand ImportOrderZipFileCommand { get; private set; }
		public ICommand OpenPreferencesCommand { get; set; }
		public ICommand OpenModsFolderCommand { get; private set; }
		public ICommand OpenWorkshopFolderCommand { get; private set; }
		public ICommand OpenDOS2GameCommand { get; private set; }
		public ICommand OpenDonationPageCommand { get; private set; }
		public ICommand OpenRepoPageCommand { get; private set; }
		public ICommand DebugCommand { get; private set; }
		public ICommand OpenConflictCheckerCommand { get; private set; }
		public ICommand ToggleUpdatesViewCommand { get; private set; }
		public ICommand CheckForAppUpdatesCommand { get; set; }
		public ICommand OpenAboutWindowCommand { get; set; }
		public ICommand ExportLoadOrderAsArchiveCommand { get; set; }
		public ICommand ExportLoadOrderAsArchiveToFileCommand { get; set; }
		public ICommand CancelMainProgressCommand { get; set; }
		public ICommand ToggleDisplayNameCommand { get; set; }
		public ICommand ToggleDarkModeCommand { get; set; }
		public ICommand CopyPathToClipboardCommand { get; set; }
		public ICommand ExtractSelectedModsCommands { get; private set; }
		public ICommand RenameSaveCommand { get; private set; }
		public ReactiveCommand<DivinityLoadOrder, Unit> DeleteOrderCommand { get; private set; }

		public bool Loaded { get; set; } = false;
		public EventHandler OnLoaded { get; set; }
		public EventHandler OnRefreshed { get; set; }
		public EventHandler OnOrderChanged { get; set; }

		private void Debug_TraceMods(List<DivinityModData> mods)
		{
			foreach (var mod in mods)
			{
				Trace.WriteLine($"Found mod. Name({mod.Name}) Author({mod.Author}) Version({mod.Version}) UUID({mod.UUID}) Description({mod.Description})");
				foreach (var dependency in mod.Dependencies)
				{
					Console.WriteLine($"  {dependency.ToString()}");
				}
			}
		}

		private void Debug_TraceProfileModOrder(DivinityProfileData p)
		{
			Trace.WriteLine($"Mod Order for Profile {p.Name}");
			Trace.WriteLine("========================================");
			foreach (var uuid in p.ModOrder)
			{
				var modData = Mods.FirstOrDefault(m => m.UUID == uuid);
				if (modData != null)
				{
					Trace.WriteLine($"  {modData.Name}({modData.UUID})");
				}
				else
				{
					Trace.WriteLine($"  NOT FOUND {uuid}");
				}
			}
			Trace.WriteLine("========================================");
		}

#if DEBUG
		public void AddMods(DivinityModData newMod)
		{
			mods.Add(newMod);
		}

		public void AddMods(IEnumerable<DivinityModData> newMods)
		{
			mods.AddRange(newMods);
		}
#endif

		public bool DebugMode { get; set; } = false;

		private TextWriterTraceListener debugLogListener;
		public void ToggleLogging(bool enabled)
		{
			if (enabled || DebugMode)
			{
				if (debugLogListener == null)
				{
					string exePath = Path.GetFullPath(System.AppDomain.CurrentDomain.BaseDirectory);

					string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
					string logsDirectory = exePath + "/_Logs/";
					if (!Alphaleonis.Win32.Filesystem.Directory.Exists(logsDirectory))
					{
						Alphaleonis.Win32.Filesystem.Directory.CreateDirectory(logsDirectory);
						Trace.WriteLine($"Creating logs directory: {logsDirectory} | exe dir: {exePath}");
					}

					string logFileName = Path.Combine(logsDirectory, "debug_" + DateTime.Now.ToString(sysFormat + "_HH-mm-ss") + ".log");
					debugLogListener = new TextWriterTraceListener(logFileName, "DebugLogListener");
					Trace.Listeners.Add(debugLogListener);
					Trace.AutoFlush = true;
				}
			}
			else if (debugLogListener != null && !DebugMode)
			{
				Trace.Listeners.Remove(debugLogListener);
				debugLogListener.Dispose();
				debugLogListener = null;
				Trace.AutoFlush = false;
			}
		}

		private bool LoadSettings()
		{
			if (Settings != null)
			{
				Settings.Dispose();
			}

			bool loaded = false;
			string settingsFile = @"Data\settings.json";
			try
			{
				if (File.Exists(settingsFile))
				{
					using (var reader = File.OpenText(settingsFile))
					{
						var fileText = reader.ReadToEnd();
						Settings = DivinityJsonUtils.SafeDeserialize<DivinityModManagerSettings>(fileText);
						loaded = Settings != null;
					}
				}
			}
			catch (Exception ex)
			{
				view.AlertBar.SetDangerAlert($"Error loading settings at '{settingsFile}': {ex.ToString()}");
				Settings = null;
			}

			if (Settings == null)
			{
				Settings = new DivinityModManagerSettings();
				SaveSettings();
			}

			if (String.IsNullOrEmpty(Settings.DOS2WorkshopPath) || !Directory.Exists(Settings.DOS2WorkshopPath))
			{
				Settings.DOS2WorkshopPath = DivinityRegistryHelper.GetDOS2WorkshopPath().Replace("\\", "/");
				if (!String.IsNullOrEmpty(Settings.DOS2WorkshopPath) && Directory.Exists(Settings.DOS2WorkshopPath))
				{
					Trace.WriteLine($"Invalid workshop path set in settings file. Found DOS2 workshop folder at: '{Settings.DOS2WorkshopPath}'.");
					SaveSettings();
				}
			}
			else
			{
				Trace.WriteLine($"Found DOS2 workshop folder at: '{Settings.DOS2WorkshopPath}'.");
			}

			canSaveSettings = this.WhenAnyValue(x => x.Settings.CanSaveSettings);
			canOpenWorkshopFolder = this.WhenAnyValue(x => x.Settings.DOS2WorkshopPath, (p) => (!String.IsNullOrEmpty(p) && Directory.Exists(p)));
			canOpenDOS2DEGame = this.WhenAnyValue(x => x.Settings.DOS2DEGameExecutable, (p) => !String.IsNullOrEmpty(p) && File.Exists(p));

			Settings.SaveSettingsCommand = ReactiveCommand.Create(() =>
			{
				if (SaveSettings())
				{
					view.AlertBar.SetSuccessAlert($"Saved settings to '{settingsFile}'.", 10);
				}
			}, canSaveSettings).DisposeWith(Settings.Disposables);

			Settings.OpenSettingsFolderCommand = ReactiveCommand.Create(() =>
			{
				Process.Start(DivinityApp.DIR_DATA);
			}).DisposeWith(Settings.Disposables);

			Settings.ExportExtenderSettingsCommand = ReactiveCommand.Create(() =>
			{
				string outputFile = Path.Combine(Path.GetDirectoryName(Settings.DOS2DEGameExecutable), "OsirisExtenderSettings.json");
				try
				{
					string contents = JsonConvert.SerializeObject(Settings.ExtenderSettings, Newtonsoft.Json.Formatting.Indented);
					File.WriteAllText(outputFile, contents);
					view.AlertBar.SetSuccessAlert($"Saved Osiris Extender settings to '{outputFile}'.", 20);
				}
				catch (Exception ex)
				{
					view.AlertBar.SetDangerAlert($"Error saving Osiris Extender settings to '{outputFile}':\n{ex.ToString()}");
				}
			}).DisposeWith(Settings.Disposables);

			this.WhenAnyValue(x => x.Settings.LogEnabled).Subscribe((logEnabled) =>
			{
				ToggleLogging(logEnabled);
			}).DisposeWith(Settings.Disposables);

			this.WhenAnyValue(x => x.Settings.DarkThemeEnabled).ObserveOn(RxApp.MainThreadScheduler).Subscribe((b) =>
			{
				ResourceLocator.SetColorScheme(view.Resources, !b ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme);
				SaveSettings();
			}).DisposeWith(Settings.Disposables);

			this.WhenAnyValue(x => x.Settings.DisplayFileNames).Subscribe((b) =>
			{
				if (b)
				{
					view.EditToggleFileNameDisplayMenuItem.Header = "Show Display Names for Mods";
				}
				else
				{
					view.EditToggleFileNameDisplayMenuItem.Header = "Show File Names for Mods";
				}
			}).DisposeWith(Settings.Disposables);

			if (Settings.LogEnabled)
			{
				ToggleLogging(true);
			}

			SetDOS2Pathways(Settings.GameDataPath);

			if (loaded)
			{
				Settings.CanSaveSettings = false;
				view.AlertBar.SetSuccessAlert($"Loaded settings from '{settingsFile}'.", 5);
			}

			return loaded;
		}

		private void OnOrderNameChanged(object sender, OrderNameChangedArgs e)
		{
			if (Settings.LastOrder == e.LastName)
			{
				Settings.LastOrder = e.NewName;
				SaveSettings();
			}
		}

		public bool SaveSettings()
		{
			string settingsFile = @"Data\settings.json";

			try
			{
				Directory.CreateDirectory("Data");
				string contents = JsonConvert.SerializeObject(Settings, Newtonsoft.Json.Formatting.Indented);
				File.WriteAllText(settingsFile, contents);
				Settings.CanSaveSettings = false;
				return true;
			}
			catch (Exception ex)
			{
				view.AlertBar.SetDangerAlert($"Error saving settings at '{settingsFile}': {ex.ToString()}");
			}
			return false;
		}

		public void CheckForModUpdates()
		{
			ModUpdatesViewData.Clear();

			int count = 0;
			foreach (var workshopMod in WorkshopMods)
			{
				workshopMod.UpdateDisplayName();
				DivinityModData pakMod = Mods.FirstOrDefault(x => x.UUID == workshopMod.UUID && !x.IsClassicMod);
				if (pakMod != null)
				{
					if (!pakMod.IsEditorMod)
					{
						//Trace.WriteLine($"Comparing versions for ({pakMod.Name}): Workshop({workshopMod.Version.VersionInt})({workshopMod.Version.Version}) Local({pakMod.Version.VersionInt})({pakMod.Version.Version})");
						if (workshopMod.Version.VersionInt > pakMod.Version.VersionInt || workshopMod.LastModified > pakMod.LastModified)
						{
							ModUpdatesViewData.Updates.Add(new DivinityModUpdateData()
							{
								LocalMod = pakMod,
								WorkshopMod = workshopMod
							});
							count++;
						}
					}
					else
					{
						Trace.WriteLine($"[***WARNING***] An editor mod has a local workshop pak! ({pakMod.Name}):");
						Trace.WriteLine($"--- Editor Version({pakMod.Version.Version}) | Workshop Version({workshopMod.Version.Version})");
					}
				}
				else
				{
					ModUpdatesViewData.NewMods.Add(workshopMod);
					count++;
				}
			}
			if (count > 0)
			{
				ModUpdatesViewData.SelectAll(true);
				Trace.WriteLine($"'{count}' mod updates pending.");
			}

			ModUpdatesViewData.OnLoaded?.Invoke();
		}

		public void LoadWorkshopMods()
		{
			if (Directory.Exists(Settings.DOS2WorkshopPath))
			{
				List<DivinityModData> modPakData = DivinityModDataLoader.LoadModPackageData(Settings.DOS2WorkshopPath);
				if (modPakData.Count > 0)
				{
					//Ignore Classic mods since they share the same workshop folder
					var sortedWorkshopMods = modPakData.Where(x => !x.IsClassicMod).OrderBy(m => m.Name);
					workshopMods.Clear();
					workshopMods.AddRange(sortedWorkshopMods);

					Trace.WriteLine($"Loaded '{workshopMods.Count}' workshop mods from '{Settings.DOS2WorkshopPath}'.");
				}
			}
		}

		public async Task<List<DivinityModData>> LoadWorkshopModsAsync()
		{
			List<DivinityModData> workshopMods = new List<DivinityModData>();

			if (Directory.Exists(Settings.DOS2WorkshopPath))
			{
				workshopMods = await DivinityModDataLoader.LoadModPackageDataAsync(Settings.DOS2WorkshopPath);
				return workshopMods.OrderBy(m => m.Name).ToList();
			}

			return workshopMods;
		}

		private void SetDOS2Pathways(string currentGameDataPath)
		{
			string documentsFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

			string larianDocumentsFolder = Path.Combine(documentsFolder, @"Larian Studios\Divinity Original Sin 2 Definitive Edition");
			if (Directory.Exists(larianDocumentsFolder))
			{
				PathwayData.LarianDocumentsFolder = larianDocumentsFolder;
				Trace.WriteLine($"Larian documents folder set to '{larianDocumentsFolder}'.");
			}

			string modPakFolder = Path.Combine(larianDocumentsFolder, "Mods");
			if (Directory.Exists(modPakFolder))
			{
				PathwayData.DocumentsModsPath = modPakFolder;
				Trace.WriteLine($"Mods folder set to '{modPakFolder}'.");
			}
			else
			{
				Trace.WriteLine($"No mods folder found at '{modPakFolder}'.");
			}

			string profileFolder = (Path.Combine(larianDocumentsFolder, "PlayerProfiles"));
			if (Directory.Exists(profileFolder))
			{
				PathwayData.DocumentsProfilesPath = profileFolder;
				Trace.WriteLine($"Larian profile folder set to '{profileFolder}'.");
			}

			if (String.IsNullOrEmpty(currentGameDataPath) || !Directory.Exists(currentGameDataPath))
			{
				string installPath = DivinityRegistryHelper.GetDOS2Path();
				if (Directory.Exists(installPath))
				{
					PathwayData.InstallPath = installPath;
					if (!File.Exists(Settings.DOS2DEGameExecutable))
					{
						string exePath = Path.Combine(installPath, "DefEd\\bin\\EoCApp.exe");
						if (File.Exists(exePath))
						{
							Settings.DOS2DEGameExecutable = exePath.Replace("\\", "/");
							Trace.WriteLine($"DOS2DE Exe path set to '{exePath}'.");
						}
					}

					string gameDataPath = Path.Combine(installPath, "DefEd/Data").Replace("\\", "/");
					Trace.WriteLine($"Set game data path to '{gameDataPath}'.");
					Settings.GameDataPath = gameDataPath;
					SaveSettings();
				}
			}
			else
			{
				string installPath = Path.GetFullPath(Path.Combine(Settings.GameDataPath, @"..\..\"));
				PathwayData.InstallPath = installPath;
				if (!File.Exists(Settings.DOS2DEGameExecutable))
				{
					string exePath = Path.Combine(installPath, "DefEd\\bin\\EoCApp.exe");
					if (File.Exists(exePath))
					{
						Settings.DOS2DEGameExecutable = exePath.Replace("\\", "/");
						Trace.WriteLine($"DOS2DE Exe path set to '{exePath}'.");
					}
				}
			}

			if (File.Exists(Settings.DOS2DEGameExecutable))
			{
				string extenderSettingsJson = PathwayData.OsirisExtenderSettingsFile(Settings);
				if(extenderSettingsJson.IsExistingFile())
				{
					var osirisExtenderSettings = DivinityJsonUtils.SafeDeserializeFromPath<OsiExtenderSettings>(extenderSettingsJson);
					if(osirisExtenderSettings != null)
					{
						Trace.WriteLine($"Loaded '{extenderSettingsJson}'.");
						Settings.ExtenderSettings.Set(osirisExtenderSettings);
					}
				}

				string extenderUpdaterPath = Path.Combine(Path.GetDirectoryName(Settings.DOS2DEGameExecutable), "DXGI.dll");
				Trace.WriteLine($"Looking for OsiExtender at '{extenderUpdaterPath}'.");
				if (File.Exists(extenderUpdaterPath))
				{
					try
					{
						using (var stream = File.Open(extenderUpdaterPath, FileMode.Open))
						{
							byte[] bytes = DivinityStreamUtils.ReadToEnd(stream);
							if (bytes.IndexOf(Encoding.ASCII.GetBytes("Osiris Extender")) >= 0)
							{
								Settings.ExtenderSettings.ExtenderIsAvailable = true;
								Trace.WriteLine($"Found the OsiExtender at '{extenderUpdaterPath}'.");
							}
						}
					}
					catch (Exception ex)
					{
						Trace.WriteLine($"Error reading: '{extenderUpdaterPath}'\n\t{ex.ToString()}");
					}

					if (Settings.ExtenderSettings.ExtenderIsAvailable)
					{
						string extenderAppFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OsirisExtender/OsiExtenderEoCApp.dll");
						if (File.Exists(extenderAppFile))
						{
							try
							{
								FileVersionInfo extenderInfo = FileVersionInfo.GetVersionInfo(extenderAppFile);
								if (!String.IsNullOrEmpty(extenderInfo.FileVersion))
								{
									var version = extenderInfo.FileVersion.Split('.')[0];
									if (int.TryParse(version, out int intVersion))
									{
										Settings.ExtenderSettings.ExtenderVersion = intVersion;
										Trace.WriteLine($"Current OsiExtender version found: '{Settings.ExtenderSettings.ExtenderVersion}'.");
									}
									else
									{
										Settings.ExtenderSettings.ExtenderVersion = -1;
									}
								}
							}
							catch (Exception ex)
							{
								Trace.WriteLine($"Error getting file info from: '{extenderAppFile}'\n\t{ex.ToString()}");
							}
						}
					}
				}
			}
		}

		public void LoadMods()
		{
			List<DivinityModData> modPakData = null;
			List<DivinityModData> projects = null;

			if (Directory.Exists(PathwayData.DocumentsModsPath))
			{
				Trace.WriteLine($"Loading mods from '{PathwayData.DocumentsModsPath}'.");
				modPakData = DivinityModDataLoader.LoadModPackageData(PathwayData.DocumentsModsPath);
			}

			GameDirectoryFound = Directory.Exists(Settings.GameDataPath);

			if (GameDirectoryFound)
			{
				GameDirectoryFound = true;
				string modsDirectory = Path.Combine(Settings.GameDataPath, "Mods");
				if (Directory.Exists(modsDirectory))
				{
					Trace.WriteLine($"Loading mod projects from '{modsDirectory}'.");
					projects = DivinityModDataLoader.LoadEditorProjects(modsDirectory);
				}
			}

			if (modPakData == null)
			{
				modPakData = new List<DivinityModData>();
			}

			if (projects == null)
			{
				projects = new List<DivinityModData>();
			}

			//var finalMods = projects.Concat(modPakData.Where(m => !projects.Any(p => p.UUID == m.UUID))).Concat(DivinityModDataLoader.Larian_Mods).OrderBy(m => m.Name);
			var finalMods = projects.Concat(modPakData.Where(m => !projects.Any(p => p.UUID == m.UUID))).OrderBy(m => m.Name);

			mods.Clear();
			mods.AddRange(finalMods);

			Trace.WriteLine($"Loaded '{mods.Count}' mods.");
			//Trace.WriteLine($"Mods: {String.Join("\n\t", mods.Items.Select(x => x.Name))}");

			//foreach(var mod in mods.Items.Where(m => m.HasDependencies))
			//{
			//	for(var i = 0; i < mod.Dependencies.Count;i++)
			//	{
			//		DivinityModDependencyData dependencyData = mod.Dependencies[i];
			//		dependencyData.IsAvailable = mods.Keys.Any(k => k == dependencyData.UUID) || DivinityModDataLoader.IgnoredMods.Any(im => im.UUID == dependencyData.UUID);
			//	}
			//}
		}

		public async Task<List<DivinityModData>> LoadModsAsync()
		{
			List<DivinityModData> modPakData = null;
			List<DivinityModData> projects = null;

			if (Directory.Exists(PathwayData.DocumentsModsPath))
			{
				Trace.WriteLine($"Loading mods from '{PathwayData.DocumentsModsPath}'.");
				modPakData = await DivinityModDataLoader.LoadModPackageDataAsync(PathwayData.DocumentsModsPath);
			}

			GameDirectoryFound = Directory.Exists(Settings.GameDataPath);

			if (GameDirectoryFound)
			{
				GameDirectoryFound = true;
				string modsDirectory = Path.Combine(Settings.GameDataPath, "Mods");
				if (Directory.Exists(modsDirectory))
				{
					Trace.WriteLine($"Loading mod projects from '{modsDirectory}'.");
					projects = await DivinityModDataLoader.LoadEditorProjectsAsync(modsDirectory);
				}
			}

			if (modPakData == null) modPakData = new List<DivinityModData>();
			if (projects == null) projects = new List<DivinityModData>();

			var finalMods = projects.Concat(modPakData.Where(m => !projects.Any(p => p.UUID == m.UUID))).
				OrderBy(m => m.Name).ToList();
			Trace.WriteLine($"Loaded '{finalMods.Count}' mods.");
			return finalMods;
		}

		public bool ModIsAvailable(IDivinityModData divinityModData)
		{
			return mods.Items.Any(k => k.UUID == divinityModData.UUID) || DivinityModDataLoader.IgnoredMods.Any(im => im.UUID == divinityModData.UUID);
		}

		public void LoadProfiles()
		{
			if (Directory.Exists(PathwayData.DocumentsProfilesPath))
			{
				Trace.WriteLine($"Loading profiles from '{PathwayData.DocumentsProfilesPath}'.");

				var profiles = DivinityModDataLoader.LoadProfileData(PathwayData.DocumentsProfilesPath);
				Profiles.AddRange(profiles);

				Trace.WriteLine($"Loaded '{Profiles.Count}' profiles.");

				var selectedUUID = DivinityModDataLoader.GetSelectedProfileUUID(PathwayData.DocumentsProfilesPath);
				if (!String.IsNullOrWhiteSpace(selectedUUID))
				{
					var index = Profiles.IndexOf(Profiles.FirstOrDefault(p => p.UUID == selectedUUID));
					if (index > -1)
					{
						SelectedProfileIndex = index;
						Debug_TraceProfileModOrder(Profiles[index]);
					}
				}

				Trace.WriteLine($"Last selected UUID: {selectedUUID}");
			}
			else
			{
				Trace.WriteLine($"Larian DOS2DE profile folder not found at '{PathwayData.DocumentsProfilesPath}'.");
			}
		}

		public async Task<List<DivinityProfileData>> LoadProfilesAsync()
		{
			if (Directory.Exists(PathwayData.DocumentsProfilesPath))
			{
				Trace.WriteLine($"Loading profiles from '{PathwayData.DocumentsProfilesPath}'.");

				var profiles = await DivinityModDataLoader.LoadProfileDataAsync(PathwayData.DocumentsProfilesPath);
				Trace.WriteLine($"Loaded '{profiles.Count}' profiles.");
				return profiles;
			}
			else
			{
				Trace.WriteLine($"Larian DOS2DE profile folder not found at '{PathwayData.DocumentsProfilesPath}'.");
			}
			return null;
		}

		public void BuildModOrderList(bool selectLast = false)
		{
			if (SelectedProfile != null)
			{
				LoadingOrder = true;

				if (SelectedProfile.SavedLoadOrder == null)
				{
					DivinityLoadOrder currentOrder = new DivinityLoadOrder() { Name = "Current", FilePath = Path.Combine(SelectedProfile.Folder, "modsettings.lsx") };

					foreach (var uuid in SelectedProfile.ModOrder)
					{
						if (SelectedProfile.ActiveMods.Contains(uuid, StringComparer.OrdinalIgnoreCase))
						{
							var mod = mods.Items.FirstOrDefault(m => m.UUID.Equals(uuid, StringComparison.OrdinalIgnoreCase));
							if (mod != null)
							{
								currentOrder.Order.Add(mod.ToOrderEntry());
							}
						}
					}

					SelectedProfile.SavedLoadOrder = currentOrder;
				}

				ModOrderList.Clear();
				ModOrderList.Add(SelectedProfile.SavedLoadOrder);
				SelectedProfile.SavedLoadOrder.Name = "Current";
				ModOrderList.AddRange(SavedModOrderList);

				int nextOrderIndex = 0;

				if (selectLast)
				{
					if (ModOrderList.Count > 0) nextOrderIndex = ModOrderList.Count - 1;
				}
				else
				{
					if (!String.IsNullOrWhiteSpace(Settings.LastOrder) && Settings.LastOrder != "Current")
					{
						var order = ModOrderList.FirstOrDefault(x => x.Name.Equals(Settings.LastOrder, StringComparison.OrdinalIgnoreCase));
						if (order != null)
						{
							int index = ModOrderList.IndexOf(order);
							if (index > -1) nextOrderIndex = index;
							Trace.WriteLine($"Set active load order to last order '{index}' - '{order.Name}'");
						}
					}
				}

				RxApp.MainThreadScheduler.Schedule(_ =>
				{
					Trace.WriteLine($"Setting next order index to '{nextOrderIndex}'.");
					try
					{
						SelectedModOrderIndex = nextOrderIndex;
						LoadModOrder(SelectedModOrder);
						Settings.LastOrder = SelectedModOrder.Name;
					}
					catch (Exception ex)
					{
						Trace.WriteLine($"Error setting next load order: {ex.ToString()}");
					}
					LoadingOrder = false;
				});
			}
		}

		private int GetModOrder(DivinityModData mod, DivinityLoadOrder loadOrder)
		{
			var entry = loadOrder.Order.FirstOrDefault(o => o.UUID == mod.UUID);
			int index = -1;
			if (mod != null)
			{
				index = loadOrder.Order.IndexOf(entry);
			}
			return index > -1 ? index : 99999999;
		}

		private void AddNewModOrder(DivinityLoadOrder newOrder = null)
		{
			var lastIndex = SelectedModOrderIndex;
			var lastOrders = ModOrderList.ToList();

			var nextOrders = new List<DivinityLoadOrder>();
			nextOrders.Add(SelectedProfile.SavedLoadOrder);
			nextOrders.AddRange(SavedModOrderList);

			void undo()
			{
				SavedModOrderList.Clear();
				SavedModOrderList.AddRange(lastOrders);
				BuildModOrderList();
				SelectedModOrderIndex = lastIndex;
			};

			void redo()
			{
				if (newOrder == null)
				{
					newOrder = new DivinityLoadOrder()
					{
						Name = "New" + nextOrders.Count,
						Order = ActiveMods.Select(m => m.ToOrderEntry()).ToList()
					};
				}
				SavedModOrderList.Add(newOrder);
				BuildModOrderList(true);
			};

			this.CreateSnapshot(undo, redo);

			redo();
		}

		public void LoadModOrder(DivinityLoadOrder order)
		{
			if (order == null) return;

			LoadingOrder = true;
			//var orderedMods = mods.Items.Where(m => order.Order.Any(o => o.UUID == m.UUID)).ToList();
			//var unOrderedMods = mods.Items.Where(m => !orderedMods.Contains(m)).ToList();
			//var sorted = orderedMods.OrderBy(m => GetModOrder(m, order)).ToList();

			ActiveMods.Clear();
			InactiveMods.Clear();

			var loadFrom = order.Order;

			//if(order.SavedOrder != null)
			//{
			//	loadFrom = order.SavedOrder;
			//}

			List<DivinityMissingModData> missingMods = new List<DivinityMissingModData>();

			int i = 0;
			foreach (var entry in loadFrom)
			{
				var mod = mods.Items.FirstOrDefault(m => m.UUID == entry.UUID);
				if (mod != null)
				{
					ActiveMods.Add(mod);
				}
				else
				{
					var x = new DivinityMissingModData
					{
						Name = entry.Name,
						Index = i,
						UUID = entry.UUID
					};
					missingMods.Add(x);
				}
				i++;
			}

			if (missingMods.Count > 0)
			{
				view.MainWindowMessageBox.ShowMessageBox(String.Join("\n", missingMods.OrderBy(x => x.Index)),
					"Missing Mods in Load Order", MessageBoxButton.OK);
			}

			List<DivinityModData> inactive = new List<DivinityModData>();

			foreach (var mod in mods.Items)
			{
				if (ActiveMods.Any(m => m.UUID == mod.UUID))
				{
					mod.IsActive = true;
					//Trace.WriteLine("Added mod " + mod.Name + " to active order");
				}
				else
				{
					mod.IsActive = false;
					mod.Index = -1;
					inactive.Add(mod);
				}
			}

			InactiveMods.AddRange(inactive.OrderBy(m => m.Name));

			LoadingOrder = false;

			if (!Loaded)
			{
				Loaded = true;
				OnLoaded?.Invoke(this, new EventArgs());
			}

			OnOrderChanged?.Invoke(this, new EventArgs());
		}

		private bool refreshing = false;

		public bool Refreshing
		{
			get => refreshing;
			set { this.RaiseAndSetIfChanged(ref refreshing, value); }
		}

		public void Refresh()
		{
			Refreshing = true;
			double taskStepAmount = 1.0 / 6;

			List<DivinityLoadOrderEntry> lastActiveOrder = null;
			int lastOrderIndex = -1;
			if (SelectedModOrder != null)
			{
				lastActiveOrder = SelectedModOrder.Order.ToList();
				lastOrderIndex = SelectedModOrderIndex;
			}
			mods.Clear();
			Profiles.Clear();
			LoadMods();
			MainProgressValue += taskStepAmount;

			LoadProfiles();
			MainProgressValue += taskStepAmount;

			SavedModOrderList = LoadExternalLoadOrders();
			MainProgressValue += taskStepAmount;

			BuildModOrderList();
			MainProgressValue += taskStepAmount;

			if (lastActiveOrder != null)
			{
				// Just in case a mod disappears
				var restoredOrder = lastActiveOrder.Where(x => Mods.Any(y => y.UUID == x.UUID));
				SelectedModOrderIndex = lastOrderIndex;
				SelectedModOrder.SetOrder(lastActiveOrder);
			}
			LoadWorkshopMods();
			MainProgressValue += taskStepAmount;

			CheckForModUpdates();
			MainProgressValue += taskStepAmount;

			Refreshing = false;

			OnRefreshed?.Invoke(this, new EventArgs());
			OnMainProgressComplete(250);
		}

		private List<DivinityLoadOrder> LoadExternalLoadOrders()
		{
			string loadOrderDirectory = Settings.LoadOrderPath;
			if (String.IsNullOrWhiteSpace(loadOrderDirectory))
			{
				//Settings.LoadOrderPath = Path.Combine(Path.GetFullPath(System.AppDomain.CurrentDomain.BaseDirectory), @"Data\ModOrder");
				//Settings.LoadOrderPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, @"Data\ModOrder");
				loadOrderDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
			}
			else if (Uri.IsWellFormedUriString(loadOrderDirectory, UriKind.Relative))
			{
				loadOrderDirectory = Path.GetFullPath(loadOrderDirectory);
			}

			Trace.WriteLine($"Attempting to load saved load orders from '{loadOrderDirectory}'.");
			var savedOrderList = DivinityModDataLoader.FindLoadOrderFilesInDirectory(loadOrderDirectory);
			return savedOrderList;
		}

		private async Task<IDisposable> RefreshAsync(IScheduler ctrl, CancellationToken t)
		{
			Trace.WriteLine($"Refreshing data asynchronously...");

			double taskStepAmount = 1.0 / 8;

			List<DivinityLoadOrderEntry> lastActiveOrder = null;
			int lastOrderIndex = -1;
			if (SelectedModOrder != null)
			{
				lastActiveOrder = SelectedModOrder.Order.ToList();
				lastOrderIndex = SelectedModOrderIndex;
			}

			if (Directory.Exists(PathwayData.LarianDocumentsFolder))
			{
				RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = "Loading mods...");
				var loadedMods = await LoadModsAsync();
				IncreaseMainProgressValue(taskStepAmount);

				RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = "Loading workshop mods...");
				var loadedWorkshopMods = await LoadWorkshopModsAsync();
				IncreaseMainProgressValue(taskStepAmount);

				RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = "Loading profiles...");
				var loadedProfiles = await LoadProfilesAsync();
				IncreaseMainProgressValue(taskStepAmount);

				string selectedProfileUUID = "";

				if (loadedProfiles != null && loadedProfiles.Count > 0)
				{
					RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = "Loading current profile...");
					selectedProfileUUID = await DivinityModDataLoader.GetSelectedProfileUUIDAsync(PathwayData.DocumentsProfilesPath);
					IncreaseMainProgressValue(taskStepAmount);
				}
				else
				{
					IncreaseMainProgressValue(taskStepAmount);
				}

				RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = "Loading external load orders...");
				var savedModOrderList = await LoadExternalLoadOrdersAsync();
				IncreaseMainProgressValue(taskStepAmount);

				if (savedModOrderList.Count > 0)
				{
					Trace.WriteLine($"{savedModOrderList.Count} saved load orders found.");
				}
				else
				{
					Trace.WriteLine("No saved orders found.");
				}

				RxApp.MainThreadScheduler.Schedule(_ =>
				{
					mods.AddRange(loadedMods);
					workshopMods.AddRange(loadedWorkshopMods);
					Profiles.AddRange(loadedProfiles);

					SavedModOrderList = savedModOrderList;

					if (!String.IsNullOrWhiteSpace(selectedProfileUUID))
					{
						var index = Profiles.IndexOf(Profiles.FirstOrDefault(p => p.UUID == selectedProfileUUID));
						if (index > -1)
						{
							SelectedProfileIndex = index;
						}
						else
						{
							SelectedProfileIndex = 0;
							Trace.WriteLine($"Profile '{selectedProfileUUID}' not found {Profiles.Count}/{loadedProfiles.Count}.");
						}
					}
					else
					{
						SelectedProfileIndex = 0;
					}

					MainProgressWorkText = "Building mod order list...";
					BuildModOrderList();
					MainProgressValue += taskStepAmount;

					if (lastActiveOrder != null && lastActiveOrder.Count > 0)
					{
						SelectedModOrderIndex = lastOrderIndex;
						SelectedModOrder.SetOrder(lastActiveOrder);
					}

					Trace.WriteLine($"Loaded '{workshopMods.Count}' workshop mods from '{Settings.DOS2WorkshopPath}'.");
					MainProgressWorkText = "Checking for mod updates...";
					CheckForModUpdates();
					MainProgressValue += taskStepAmount;
				});

				IncreaseMainProgressValue(taskStepAmount);
			}
			else
			{
				Trace.WriteLine($"[*ERROR*] Larian documents folder not found!");
			}

			await ctrl.Yield();

			RxApp.MainThreadScheduler.Schedule(_ =>
			{
				Refreshing = false;
				OnMainProgressComplete();
				OnRefreshed?.Invoke(this, new EventArgs());
			});

			return Disposable.Empty;
		}

		public void RefreshAsync_Start(string title = "Refreshing...")
		{
			MainProgressTitle = title;
			MainProgressValue = 0d;
			CanCancelProgress = false;
			MainProgressIsActive = true;
			Refreshing = true;
			mods.Clear();
			Profiles.Clear();
			workshopMods.Clear();
			RxApp.TaskpoolScheduler.ScheduleAsync(RefreshAsync);
		}

		private async Task<List<DivinityLoadOrder>> LoadExternalLoadOrdersAsync()
		{
			string loadOrderDirectory = Settings.LoadOrderPath;
			if (String.IsNullOrWhiteSpace(loadOrderDirectory))
			{
				//Settings.LoadOrderPath = Path.Combine(Path.GetFullPath(System.AppDomain.CurrentDomain.BaseDirectory), @"Data\ModOrder");
				//Settings.LoadOrderPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, @"Data\ModOrder");
				loadOrderDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
			}
			else if (Uri.IsWellFormedUriString(loadOrderDirectory, UriKind.Relative))
			{
				loadOrderDirectory = Path.GetFullPath(loadOrderDirectory);
			}

			Trace.WriteLine($"Attempting to load saved load orders from '{loadOrderDirectory}'.");
			var savedOrderList = await DivinityModDataLoader.FindLoadOrderFilesInDirectoryAsync(loadOrderDirectory);
			return savedOrderList;
		}

		private void ActiveMods_SetItemIndex(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			//Trace.WriteLine($"[ActiveMods_SetItemIndex] Active collection changed. New({e.NewItems}) Old({e.OldItems}) Action({e.Action})");
			foreach (var mod in ActiveMods)
			{
				mod.Index = ActiveMods.IndexOf(mod);
			}

			if (SelectedModOrder != null && !LoadingOrder)
			{
				if (e.OldItems != null)
				{
					//Trace.WriteLine($"[ActiveMods_SetItemIndex] Removing {e.OldItems.Count} old items from order.");
					foreach (DivinityModData m in e.OldItems)
					{
						if (SelectedModOrder.Order.Any(x => x.UUID == m.UUID))
						{
							SelectedModOrder.Order.RemoveAll(x => x.UUID == m.UUID);
						}

						m.IsActive = false;
					}
				}
				if (e.NewItems != null)
				{
					foreach (DivinityModData m in e.NewItems)
					{
						m.IsActive = true;
						SelectedModOrder.Order.Add(m.ToOrderEntry());

						//Trace.WriteLine($"[ActiveMods_SetItemIndex] Mod {m.Name} became inactive.");
					}

					SelectedModOrder.Order = SelectedModOrder.Order.OrderBy(mEntry => ActiveMods.IndexOf(ActiveMods.First(mod => mod.UUID == mEntry.UUID))).ToList();
					//Trace.WriteLine($"[ActiveMods_SetItemIndex] Reordered saved order {String.Join(",", SelectedModOrder.Order.Select(x => x.Name))}.");
				}
			}

			OnFilterTextChanged(ActiveModFilterText, ActiveMods);
		}

		private void InactiveMods_SetItemIndex(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
			{
				foreach (DivinityModData m in e.NewItems)
				{
					m.IsActive = false;
					m.Index = -1;
					//Trace.WriteLine($"[InactiveMods_SetItemIndex] Mod {m.Name} became inactive.");
				}

				OnFilterTextChanged(InactiveModFilterText, InactiveMods);
			}
		}

		private void SaveLoadOrder()
		{
			view.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(async () =>
			{
				await SaveLoadOrderAsync();
			}));
		}

		private async Task<bool> SaveLoadOrderAsync()
		{
			bool result = false;
			if (SelectedProfile != null && SelectedModOrder != null)
			{
				string outputDirectory = Settings.LoadOrderPath;

				if (String.IsNullOrWhiteSpace(outputDirectory))
				{
					outputDirectory = AppDomain.CurrentDomain.BaseDirectory;
				}

				if (!Directory.Exists(outputDirectory))
				{
					Directory.CreateDirectory(outputDirectory);
				}

				string outputPath = SelectedModOrder.FilePath;

				if (String.IsNullOrWhiteSpace(outputPath))
				{
					string outputName = DivinityModDataLoader.MakeSafeFilename(Path.Combine(SelectedModOrder.Name + ".json"), '_');
					outputPath = Path.Combine(outputDirectory, outputName);
				}

				try
				{
					if (SelectedModOrder.Name.Equals("Current"))
					{
						//When saving the "Current" order, write this to modsettings.lsx instead of a json file.
						result = await ExportLoadOrderAsync();
						outputPath = Path.Combine(SelectedProfile.Folder, "modsettings.lsx");
						/*
						outputName = DivinityModDataLoader.MakeSafeFilename(Path.Combine($"{SelectedProfile.Name}_{SelectedModOrder.Name}.json"), '_');
						DivinityLoadOrder tempOrder = SelectedModOrder.Clone();
						tempOrder.Name = $"Current ({SelectedProfile.Name})";

						outputPath = Path.Combine(outputDirectory, outputName);
						result = await DivinityModDataLoader.ExportLoadOrderToFileAsync(outputPath, tempOrder);
						*/
					}
					else
					{
						result = await DivinityModDataLoader.ExportLoadOrderToFileAsync(outputPath, SelectedModOrder);
					}
				}
				catch (Exception ex)
				{
					view.AlertBar.SetDangerAlert($"Failed to save mod load order to '{outputPath}': {ex.Message}");
					result = false;
				}

				if (result)
				{
					view.AlertBar.SetSuccessAlert($"Saved mod load order to '{outputPath}'", 10);
				}
			}

			return result;
		}

		private void SaveLoadOrderAs()
		{
			var startDirectory = Path.GetFullPath(!String.IsNullOrEmpty(Settings.LoadOrderPath) ? Settings.LoadOrderPath : Directory.GetCurrentDirectory());

			Trace.WriteLine(startDirectory);

			if (!Directory.Exists(startDirectory))
			{
				Directory.CreateDirectory(startDirectory);
			}

			var dialog = new SaveFileDialog();
			dialog.AddExtension = true;
			dialog.DefaultExt = ".json";
			dialog.Filter = "JSON file (*.json)|*.json";
			dialog.InitialDirectory = startDirectory;

			string outputName = Path.Combine(SelectedModOrder.Name + ".json");
			if (SelectedModOrder.Name.Equals("Current", StringComparison.OrdinalIgnoreCase))
			{
				outputName = $"{SelectedProfile.Name}_{SelectedModOrder.Name}.json";
			}

			//dialog.RestoreDirectory = true;
			dialog.FileName = DivinityModDataLoader.MakeSafeFilename(outputName, '_');
			dialog.CheckFileExists = false;
			dialog.CheckPathExists = false;
			dialog.OverwritePrompt = true;
			dialog.Title = "Save Load Order As...";

			if (dialog.ShowDialog(view) == true)
			{
				bool result = false;
				if (SelectedModOrder.Name.Equals("Current", StringComparison.OrdinalIgnoreCase))
				{
					DivinityLoadOrder tempOrder = SelectedModOrder.Clone();
					tempOrder.Name = $"Current ({SelectedProfile.Name})";

					result = DivinityModDataLoader.ExportLoadOrderToFile(dialog.FileName, tempOrder);
				}
				else
				{
					result = DivinityModDataLoader.ExportLoadOrderToFile(dialog.FileName, SelectedModOrder);
				}

				if (result)
				{
					view.AlertBar.SetSuccessAlert($"Saved mod load order to '{dialog.FileName}'", 10);
				}
				else
				{
					view.AlertBar.SetDangerAlert($"Failed to save mod load order to '{dialog.FileName}'");
				}
			}
		}

		private async Task<bool> ExportLoadOrderAsync()
		{
			if (SelectedProfile != null && SelectedModOrder != null)
			{
				string outputPath = Path.Combine(SelectedProfile.Folder, "modsettings.lsx");
				var result = await DivinityModDataLoader.ExportModSettingsToFileAsync(SelectedProfile.Folder, SelectedModOrder,
					mods.Items, Settings.AutoAddDependenciesWhenExporting);

				if (result)
				{
					view.AlertBar.SetSuccessAlert($"Exported load order to '{outputPath}'", 15);

					//Update "Current" order
					if (SelectedModOrder.Name != "Current")
					{
						var currentOrder = this.ModOrderList.FirstOrDefault(x => x.Name == "Current");
						currentOrder.SetOrder(SelectedModOrder.Order);
						Trace.WriteLine("Updated 'Current' load order to exported order.");
						return true;
					}
				}
				else
				{
					string msg = $"Problem exporting load order to '{outputPath}'";
					view.AlertBar.SetDangerAlert(msg);
					view.MainWindowMessageBox.ShowMessageBox(msg, "Mod Order Export Failed", MessageBoxButton.OK);
				}
			}
			else
			{
				view.AlertBar.SetDangerAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order.");
			}
			return false;
		}

		private void MainProgressStart()
		{
			MainProgressValue = 0d;
			MainProgressIsActive = true;
		}

		private void OnMainProgressComplete(double delay = 0)
		{
			TimeSpan delaySpan = TimeSpan.Zero;
			if (delay > 0) delaySpan = TimeSpan.FromMilliseconds(delay);

			MainProgressWorkText = "Finished.";
			MainProgressValue = 1d;
			if (MainProgressToken != null)
			{
				MainProgressToken.Dispose();
				MainProgressToken = null;
			}

			RxApp.MainThreadScheduler.Schedule(delaySpan, _ => {
				MainProgressIsActive = false;
				CanCancelProgress = true;

				if (Settings.CheckForUpdates)
				{
					if (Settings.LastUpdateCheck == -1 || (DateTimeOffset.Now.ToUnixTimeSeconds() - Settings.LastUpdateCheck >= 43200))
					{
						AutoUpdater.Start(DivinityApp.URL_UPDATE);
					}
				}
			});
		}

		private void ExportLoadOrderToArchive_Start()
		{
			//view.MainWindowMessageBox.Text = "Add active mods to a zip file?";
			//view.MainWindowMessageBox.Caption = "Depending on the number of mods, this may take some time.";
			view.MainWindowMessageBox.Closed += MainWindowMessageBox_Closed_ExportLoadOrderToArchive;
			view.MainWindowMessageBox.ShowMessageBox($"Save active mods to a zip file?{Environment.NewLine}Depending on the number of mods, this may take some time.", "Confirm Archive Creation", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
		}

		private void MainWindowMessageBox_Closed_ExportLoadOrderToArchive(object sender, EventArgs e)
		{
			view.MainWindowMessageBox.Closed -= MainWindowMessageBox_Closed_ExportLoadOrderToArchive;
			if (view.MainWindowMessageBox.MessageBoxResult == MessageBoxResult.OK)
			{
				MainProgressTitle = "Adding active mods to zip...";
				MainProgressWorkText = "";
				MainProgressValue = 0d;
				MainProgressIsActive = true;
				RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
				{
					MainProgressToken = new CancellationTokenSource();
					await ExportLoadOrderToArchiveAsync("", MainProgressToken.Token);
					await ctrl.Yield();
					RxApp.MainThreadScheduler.Schedule(_ => OnMainProgressComplete());
					return Disposable.Empty;
				});
			}
		}

		private async Task<bool> ExportLoadOrderToArchiveAsync(string outputPath, CancellationToken t)
		{
			bool success = false;
			if (SelectedProfile != null && SelectedModOrder != null)
			{
				string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
				string gameDataFolder = Path.GetFullPath(Settings.GameDataPath);
				string appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
				string tempDir = Path.Combine(appDir, "_Temp_" + DateTime.Now.ToString(sysFormat + "_HH-mm-ss"));
				Directory.CreateDirectory(tempDir);

				if (String.IsNullOrEmpty(outputPath))
				{
					string baseOrderName = SelectedModOrder.Name;
					if (SelectedModOrder.Name.Equals("Current", StringComparison.OrdinalIgnoreCase))
					{
						baseOrderName = $"{SelectedProfile.Name}_{SelectedModOrder.Name}";
					}
					outputPath = $"Export/{baseOrderName}-{ DateTime.Now.ToString(sysFormat + "_HH-mm-ss")}.zip";

					var exportDirectory = Path.Combine(appDir, "Export");
					if (!Directory.Exists(exportDirectory))
					{
						Directory.CreateDirectory(exportDirectory);
					}
				}

				var modPaks = new List<DivinityModData>(Mods.Where(x => SelectedModOrder.Order.Any(o => o.UUID == x.UUID)));

				double incrementProgress = 1d / modPaks.Count;

				try
				{
					using (var zip = File.OpenWrite(outputPath))
					using (var zipWriter = WriterFactory.Open(zip, ArchiveType.Zip, CompressionType.Deflate))
					{
						foreach (var mod in modPaks)
						{
							if (t.IsCancellationRequested) return false;
							if (!mod.IsEditorMod)
							{
								string fileName = Path.GetFileName(mod.FilePath);
								await WriteZipAsync(zipWriter, fileName, mod.FilePath, t);
							}
							else
							{
								string outputPackage = Path.ChangeExtension(Path.Combine(tempDir, mod.Folder), "pak");
								//Imported Classic Projects
								if (!mod.Folder.Contains(mod.UUID))
								{
									outputPackage = Path.ChangeExtension(Path.Combine(tempDir, mod.Folder + "_" + mod.UUID), "pak");
								}

								var sourceFolders = new List<string>();

								string modsFolder = Path.Combine(gameDataFolder, $"Mods/{mod.Folder}");
								string publicFolder = Path.Combine(gameDataFolder, $"Public/{mod.Folder}");

								if (Directory.Exists(modsFolder)) sourceFolders.Add(modsFolder);
								if (Directory.Exists(publicFolder)) sourceFolders.Add(publicFolder);

								Trace.WriteLine($"Creating package for editor mod '{mod.Name}' - '{outputPackage}'.");

								if (await DivinityFileUtils.CreatePackageAsync(gameDataFolder, sourceFolders, outputPackage, DivinityFileUtils.IgnoredPackageFiles, t))
								{
									string fileName = Path.GetFileName(outputPackage);
									await WriteZipAsync(zipWriter, fileName, outputPackage, t);
									File.Delete(outputPackage);
								}
							}

							RxApp.MainThreadScheduler.Schedule(_ => MainProgressValue += incrementProgress);
						}
					}

					RxApp.MainThreadScheduler.Schedule(() =>
					{
						var dir = Path.GetDirectoryName(outputPath);
						Process.Start(dir);
						view.AlertBar.SetSuccessAlert($"Exported load order to '{outputPath}'.", 15);
					});

					success = true;
				}
				catch (Exception ex)
				{
					RxApp.MainThreadScheduler.Schedule(() =>
					{
						string msg = $"Error writing load order archive '{outputPath}': {ex.ToString()}";
						Trace.WriteLine(msg);
						view.AlertBar.SetDangerAlert(msg);
					});
				}

				Directory.Delete(tempDir);
			}
			else
			{
				RxApp.MainThreadScheduler.Schedule(() =>
				{
					view.AlertBar.SetDangerAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order.");
				});
			}

			return success;
		}

		private static Task WriteZipAsync(IWriter writer, string entryName, string source, CancellationToken token)
		{
			if (token.IsCancellationRequested)
			{
				return Task.FromCanceled(token);
			}

			var task = Task.Run(async () =>
			{
				// execute actual operation in child task
				var childTask = Task.Factory.StartNew(() =>
				{
					try
					{
						writer.Write(entryName, source);
					}
					catch (Exception)
					{
						// ignored because an exception on a cancellation request 
						// cannot be avoided if the stream gets disposed afterwards 
					}
				}, TaskCreationOptions.AttachedToParent);

				var awaiter = childTask.GetAwaiter();
				while (!awaiter.IsCompleted)
				{
					await Task.Delay(0, token);
				}
			}, token);

			return task;
		}

		private void ExportLoadOrderToArchiveAs()
		{
			if (SelectedProfile != null && SelectedModOrder != null)
			{
				var startDirectory = Path.GetFullPath(Directory.GetCurrentDirectory());

				var dialog = new SaveFileDialog();
				dialog.AddExtension = true;
				dialog.DefaultExt = ".zip";
				dialog.Filter = "Archive file (*.zip)|*.zip";
				dialog.InitialDirectory = startDirectory;

				string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
				string baseOrderName = SelectedModOrder.Name;
				if (SelectedModOrder.Name.Equals("Current", StringComparison.OrdinalIgnoreCase))
				{
					baseOrderName = $"{SelectedProfile.Name}_{SelectedModOrder.Name}";
				}
				string outputName = $"{baseOrderName}-{ DateTime.Now.ToString(sysFormat + "_HH-mm-ss")}.zip";

				//dialog.RestoreDirectory = true;
				dialog.FileName = DivinityModDataLoader.MakeSafeFilename(outputName, '_');
				dialog.CheckFileExists = false;
				dialog.CheckPathExists = false;
				dialog.OverwritePrompt = true;
				dialog.Title = "Export Load Order As...";

				if (dialog.ShowDialog(view) == true)
				{
					MainProgressTitle = "Adding active mods to zip...";
					MainProgressWorkText = "";
					MainProgressValue = 0d;
					MainProgressIsActive = true;

					RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
					{
						MainProgressToken = new CancellationTokenSource();
						await ExportLoadOrderToArchiveAsync(dialog.FileName, MainProgressToken.Token);
						await ctrl.Yield();
						RxApp.MainThreadScheduler.Schedule(_ => OnMainProgressComplete());
						return Disposable.Empty;
					});
				}
			}
			else
			{
				view.AlertBar.SetDangerAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order.");
			}

		}

		private DivinityLoadOrder ImportOrderFromSave()
		{
			var dialog = new OpenFileDialog();
			dialog.CheckFileExists = true;
			dialog.CheckPathExists = true;
			dialog.DefaultExt = ".lsv";
			dialog.Filter = "Larian Save file (*.lsv)|*.lsv";
			dialog.Title = "Load Mod Order From Save...";

			if (!String.IsNullOrEmpty(PathwayData.LastSaveFilePath) && Directory.Exists(PathwayData.LastSaveFilePath))
			{
				dialog.InitialDirectory = PathwayData.LastSaveFilePath;
			}
			else
			{
				if (SelectedProfile != null)
				{
					dialog.InitialDirectory = Path.GetFullPath(Path.Combine(SelectedProfile.Folder, "Savegames"));
				}
				else
				{
					dialog.InitialDirectory = Path.GetFullPath(PathwayData.LarianDocumentsFolder);
				}
			}

			if (dialog.ShowDialog(view) == true)
			{
				PathwayData.LastSaveFilePath = Path.GetDirectoryName(dialog.FileName);
				Trace.WriteLine($"Loading order from '{dialog.FileName}'.");
				var newOrder = DivinityModDataLoader.GetLoadOrderFromSave(dialog.FileName);
				if (newOrder != null)
				{
					Trace.WriteLine($"Imported mod order: {String.Join(@"\n\t", newOrder.Order.Select(x => x.Name))}");
					return newOrder;
				}
				else
				{
					Trace.WriteLine($"Failed to load order from '{dialog.FileName}'.");
				}
			}
			return null;
		}

		private void ImportOrderFromSaveAsNew()
		{
			var order = ImportOrderFromSave();
			if (order != null)
			{
				AddNewModOrder(order);
			}
		}

		private void ImportOrderFromSaveToCurrent()
		{
			var order = ImportOrderFromSave();
			if (order != null)
			{
				if (SelectedModOrder != null)
				{
					SelectedModOrder.SetOrder(order);
					LoadModOrder(SelectedModOrder);
				}
				else
				{
					AddNewModOrder(order);
				}
			}
		}

		private void ImportOrderFromFile()
		{
			var dialog = new OpenFileDialog();
			dialog.CheckFileExists = true;
			dialog.CheckPathExists = true;
			dialog.DefaultExt = ".json";
			dialog.Filter = "JSON file (*.json)|*.json";
			dialog.Title = "Load Mod Order From File...";

			if (!String.IsNullOrEmpty(Settings.LastLoadedOrderFilePath) && (Directory.Exists(Settings.LastLoadedOrderFilePath) | File.Exists(Settings.LastLoadedOrderFilePath)))
			{
				if (Directory.Exists(Settings.LastLoadedOrderFilePath))
				{
					dialog.InitialDirectory = Settings.LastLoadedOrderFilePath;
				}
				else if (File.Exists(Settings.LastLoadedOrderFilePath))
				{
					dialog.InitialDirectory = Path.GetDirectoryName(Settings.LastLoadedOrderFilePath);
				}
			}
			else
			{
				if (Directory.Exists(Settings.LoadOrderPath))
				{
					dialog.InitialDirectory = Settings.LoadOrderPath;
				}
				else
				{
					dialog.InitialDirectory = Environment.CurrentDirectory;
				}
			}

			if (dialog.ShowDialog(view) == true)
			{
				Settings.LastLoadedOrderFilePath = Path.GetDirectoryName(dialog.FileName);
				SaveSettings();
				Trace.WriteLine($"Loading order from '{dialog.FileName}'.");
				var newOrder = DivinityModDataLoader.LoadOrderFromFile(dialog.FileName);
				if (newOrder != null)
				{
					Trace.WriteLine($"Imported mod order: {String.Join(@"\n\t", newOrder.Order.Select(x => x.Name))}");
					AddNewModOrder(newOrder);
				}
				else
				{
					Trace.WriteLine($"Failed to load order from '{dialog.FileName}'.");
				}
			}
		}

		private void ImportOrderZipFile()
		{

		}

		private void RenameSave_Start()
		{
			string profileSavesDirectory = "";
			if (SelectedProfile != null)
			{
				profileSavesDirectory = Path.GetFullPath(Path.Combine(SelectedProfile.Folder, "Savegames"));
			}
			var dialog = new OpenFileDialog();
			dialog.CheckFileExists = true;
			dialog.CheckPathExists = true;
			dialog.DefaultExt = ".lsv";
			dialog.Filter = "Larian Save file (*.lsv)|*.lsv";
			dialog.Title = "Pick Save to Rename...";

			if (!String.IsNullOrEmpty(PathwayData.LastSaveFilePath) && Directory.Exists(PathwayData.LastSaveFilePath))
			{
				dialog.InitialDirectory = PathwayData.LastSaveFilePath;
			}
			else
			{
				if (SelectedProfile != null)
				{
					dialog.InitialDirectory = profileSavesDirectory;
				}
				else
				{
					dialog.InitialDirectory = Path.GetFullPath(PathwayData.LarianDocumentsFolder);
				}
			}

			if (dialog.ShowDialog(view) == true)
			{
				string rootFolder = Path.GetDirectoryName(dialog.FileName);
				string rootFileName = Path.GetFileNameWithoutExtension(dialog.FileName);
				PathwayData.LastSaveFilePath = rootFolder;

				var renameDialog = new SaveFileDialog();
				renameDialog.CheckFileExists = false;
				renameDialog.CheckPathExists = false;
				renameDialog.DefaultExt = ".lsv";
				renameDialog.Filter = "Larian Save file (*.lsv)|*.lsv";
				renameDialog.Title = "Rename Save As...";
				renameDialog.InitialDirectory = rootFolder;
				renameDialog.FileName = rootFileName + "_1.lsv";

				if (renameDialog.ShowDialog(view) == true)
				{
					rootFolder = Path.GetDirectoryName(renameDialog.FileName);
					PathwayData.LastSaveFilePath = rootFolder;
					Trace.WriteLine($"Renaming '{dialog.FileName}' to '{renameDialog.FileName}'.");

					if (DivinitySaveTools.RenameSave(dialog.FileName, renameDialog.FileName))
					{
						//Trace.WriteLine($"Successfully renamed '{dialog.FileName}' to '{renameDialog.FileName}'.");

						try
						{
							string previewImage = Path.Combine(rootFolder, rootFileName + ".png");
							string renamedImage = Path.Combine(rootFolder, Path.GetFileNameWithoutExtension(renameDialog.FileName) + ".png");
							if (File.Exists(previewImage))
							{
								File.Move(previewImage, renamedImage);
								Trace.WriteLine($"Renamed save screenshot '{previewImage}' to '{renamedImage}'.");
							}

							string originalDirectory = Path.GetDirectoryName(dialog.FileName);
							string desiredDirectory = Path.GetDirectoryName(renameDialog.FileName);

							if (!String.IsNullOrEmpty(profileSavesDirectory) && DivinityFileUtils.IsSubdirectoryOf(profileSavesDirectory, desiredDirectory))
							{
								if (originalDirectory == desiredDirectory)
								{
									var dirInfo = new DirectoryInfo(originalDirectory);
									if (dirInfo.Name.Equals(Path.GetFileNameWithoutExtension(dialog.FileName)))
									{
										desiredDirectory = Path.Combine(dirInfo.Parent.FullName, Path.GetFileNameWithoutExtension(renameDialog.FileName));
										RecycleBinHelper.DeleteFile(dialog.FileName, false, false);
										Directory.Move(originalDirectory, desiredDirectory);
										Trace.WriteLine($"Renamed save folder '{originalDirectory}' to '{desiredDirectory}'.");
									}
								}
							}

							view.AlertBar.SetSuccessAlert($"Successfully renamed '{dialog.FileName}' to '{renameDialog.FileName}'.", 15);
						}
						catch (Exception ex)
						{
							Trace.WriteLine($"Failed to rename '{dialog.FileName}' to '{renameDialog.FileName}':\n" + ex.ToString());
						}
					}
					else
					{
						Trace.WriteLine($"Failed to rename '{dialog.FileName}' to '{renameDialog.FileName}'.");
					}
				}
			}
		}

		public void OnViewActivated(MainWindow parentView)
		{
			view = parentView;

			DivinityApp.Commands.SetViewModel(this);

			OpenConflictCheckerCommand = ReactiveCommand.Create(() =>
			{
				view.ToggleConflictChecker(!ConflictCheckerWindowOpen);
			});

			if (DebugMode)
			{
				this.WhenAnyValue(x => x.MainProgressWorkText, x => x.MainProgressValue).Subscribe((ob) =>
				{
					Trace.WriteLine($"Progress: {MainProgressValue} - {MainProgressWorkText}");
				});
			}

			LoadSettings();
			RefreshAsync_Start("Loading...");
			//Refresh();
			SaveSettings(); // New values
		}

		public bool AutoChangedOrder { get; set; } = false;
		public ViewModelActivator Activator { get; }

		private Regex filterPropertyPattern = new Regex("@([^\\s]+?)([\\s]+)([^@\\s]*)");
		private Regex filterPropertyPatternWithQuotes = new Regex("@([^\\s]+?)([\\s\"]+)([^@\"]*)");

		private void OnFilterTextChanged(string searchText, IEnumerable<DivinityModData> modDataList)
		{
			//Trace.WriteLine("Filtering mod list with search term " + searchText);
			if (String.IsNullOrWhiteSpace(searchText))
			{
				foreach (var m in modDataList)
				{
					m.Visibility = Visibility.Visible;
				}
			}
			else
			{
				if (searchText.IndexOf("@") > -1)
				{
					string remainingSearch = searchText;
					List<DivinityModFilterData> searchProps = new List<DivinityModFilterData>();

					MatchCollection matches;

					if (searchText.IndexOf("\"") > -1)
					{
						matches = filterPropertyPatternWithQuotes.Matches(searchText);
					}
					else
					{
						matches = filterPropertyPattern.Matches(searchText);
					}

					if (matches.Count > 0)
					{
						foreach (Match match in matches)
						{
							if (match.Success)
							{
								var prop = match.Groups[1]?.Value;
								var value = match.Groups[3]?.Value;
								if (String.IsNullOrEmpty(value)) value = "";
								if (!String.IsNullOrWhiteSpace(prop))
								{
									searchProps.Add(new DivinityModFilterData()
									{
										FilterProperty = prop,
										FilterValue = value
									});

									remainingSearch = remainingSearch.Replace(match.Value, "");
								}
							}
						}
					}

					remainingSearch = remainingSearch.Replace("\"", "");

					//If no Name property is specified, use the remaining unmatched text for that
					if (!String.IsNullOrWhiteSpace(remainingSearch) && !searchProps.Any(f => f.PropertyContains("Name")))
					{
						remainingSearch = remainingSearch.Trim();
						searchProps.Add(new DivinityModFilterData()
						{
							FilterProperty = "Name",
							FilterValue = remainingSearch
						});
					}

					foreach (var mod in modDataList)
					{
						//@Mode GM @Author Leader
						int totalMatches = 0;
						foreach (var f in searchProps)
						{
							if (f.Match(mod))
							{
								totalMatches += 1;
							}
						}
						if (totalMatches >= searchProps.Count)
						{
							mod.Visibility = Visibility.Visible;
						}
						else
						{
							mod.Visibility = Visibility.Collapsed;
						}
					}
				}
				else
				{
					foreach (var m in modDataList)
					{
						if (CultureInfo.CurrentCulture.CompareInfo.IndexOf(m.Name, searchText, CompareOptions.IgnoreCase) >= 0)
						{
							m.Visibility = Visibility.Visible;
						}
						else
						{
							m.Visibility = Visibility.Collapsed;
						}
					}
				}
			}
		}

		private MainWindowExceptionHandler exceptionHandler;

		public void ShowAlert(string message, int alertType = 0, int timeout = 0)
		{
			switch (alertType)
			{
				case -1:
					view.AlertBar.SetDangerAlert(message, timeout);
					break;
				case 0:
					view.AlertBar.SetInformationAlert(message, timeout);
					break;
				case 1:
					view.AlertBar.SetSuccessAlert(message, timeout);
					break;
				case 2:
					view.AlertBar.SetWarningAlert(message, timeout);
					break;
			}
		}

		private Unit DeleteOrder(DivinityLoadOrder order)
		{
			MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(view, $"Delete load order '{order.Name}'? This cannot be undone.", "Confirm Order Deletion",
				MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, view.MainWindowMessageBox.Style);
			if (result == MessageBoxResult.Yes)
			{
				SelectedModOrderIndex = 0;
				this.ModOrderList.Remove(order);
				if (!String.IsNullOrEmpty(order.FilePath) && File.Exists(order.FilePath))
				{
					RecycleBinHelper.DeleteFile(order.FilePath, false, false);
					view.AlertBar.SetWarningAlert($"Sent load order '{order.FilePath}' to the recycle bin.", 25);
				}
			}
			return Unit.Default;
		}

		private void ExtractSelectedMods_ChooseFolder()
		{
			var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
			dialog.ShowNewFolderButton = true;
			dialog.UseDescriptionForTitle = true;
			dialog.Description = "Select folder to extract mod to...";

			if (Settings.LastExtractOutputPath.IsExistingDirectory())
			{
				dialog.SelectedPath = Settings.LastExtractOutputPath;
			}
			else if (PathwayData.LastSaveFilePath.IsExistingDirectory())
			{
				dialog.SelectedPath = PathwayData.LastSaveFilePath;
			}
			else
			{
				dialog.RootFolder = Environment.SpecialFolder.Desktop;
			}

			if (dialog.ShowDialog(view) == true)
			{
				Settings.LastExtractOutputPath = dialog.SelectedPath;
				SaveSettings();

				string outputDirectory = dialog.SelectedPath;
				Trace.WriteLine($"Extracting selected mods to '{outputDirectory}'.");

				int totalWork = SelectedPakMods.Count;
				double taskStepAmount = 1.0 / totalWork;
				MainProgressTitle = $"Extracting {totalWork} mods...";
				MainProgressValue = 0d;
				MainProgressToken = new CancellationTokenSource();
				CanCancelProgress = true;
				MainProgressIsActive = true;

				RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
				{
					int successes = 0;
					foreach (var path in SelectedPakMods.Select(x => x.FilePath))
					{
						if (MainProgressToken.IsCancellationRequested) break;
						try
						{
							//Put each pak into its own folder
							string pakName = Path.GetFileNameWithoutExtension(path);
							RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = $"Extracting {pakName}...");
							string destination = Path.Combine(outputDirectory, pakName);

							//Unless the foldername == the pak name and we're only extracting one pak
							if (totalWork == 1 && Path.GetDirectoryName(outputDirectory).Equals(pakName))
							{
								destination = outputDirectory;
							}
							var success = await DivinityFileUtils.ExtractPackageAsync(path, destination, MainProgressToken.Token);
							if (success) successes += 1;
						}
						catch (Exception ex)
						{
							Trace.WriteLine($"Error extracting package: {ex.ToString()}");
						}
						IncreaseMainProgressValue(taskStepAmount);
					}
					await ctrl.Yield();
					RxApp.MainThreadScheduler.Schedule(_ => OnMainProgressComplete());

					RxApp.MainThreadScheduler.Schedule(() =>
					{
						if (successes >= totalWork)
						{
							view.AlertBar.SetSuccessAlert($"Successfully extracted all selected mods to '{dialog.SelectedPath}'.", 20);
						}
						else
						{
							view.AlertBar.SetDangerAlert($"Error occurred when extracting selected mods to '{dialog.SelectedPath}'.", 30);
						}
					});

					return Disposable.Empty;
				});
			}
		}

		private void ExtractSelectedMods_Start()
		{
			//var selectedMods = Mods.Where(x => x.IsSelected && !x.IsEditorMod).ToList();

			if (SelectedPakMods.Count == 1)
			{
				ExtractSelectedMods_ChooseFolder();
			}
			else
			{
				MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(view, $"Extract the following mods?\n'{String.Join("\n", SelectedPakMods.Select(x => $"{x.DisplayName}"))}", "Extract Mods?",
				MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, view.MainWindowMessageBox.Style);
				if (result == MessageBoxResult.Yes)
				{
					ExtractSelectedMods_ChooseFolder();
				}
			}
		}

		public MainWindowViewModel() : base()
		{
			exceptionHandler = new MainWindowExceptionHandler(this);
			RxApp.DefaultExceptionHandler = exceptionHandler;

			this.DropHandler = new ModListDropHandler(this);
			this.DragHandler = new ModListDragHandler();

			Activator = new ViewModelActivator();

			this.WhenActivated((CompositeDisposable disposables) =>
			{
				if (!disposables.Contains(this.Disposables)) disposables.Add(this.Disposables);
			});

			gameExeFoundObservable = this.WhenAnyValue(x => x.Settings.DOS2DEGameExecutable, (path) => path.IsExistingFile());

			var canExecuteSaveCommand = this.WhenAnyValue(x => x.CanSaveOrder, (canSave) => canSave == true);
			SaveOrderCommand = ReactiveCommand.Create(SaveLoadOrder, canExecuteSaveCommand);

			var canExecuteSaveAsCommand = this.WhenAnyValue(x => x.CanSaveOrder, x => x.MainProgressIsActive, (canSave, p) => canSave && !p);
			SaveOrderAsCommand = ReactiveCommand.Create(SaveLoadOrderAs, canExecuteSaveAsCommand);

			ExportOrderCommand = ReactiveCommand.CreateFromTask(ExportLoadOrderAsync);

			IObservable<bool> canStartExport = this.WhenAny(x => x.MainProgressToken, (t) => t != null);
			ExportLoadOrderAsArchiveCommand = ReactiveCommand.Create(ExportLoadOrderToArchive_Start, canStartExport);
			ExportLoadOrderAsArchiveToFileCommand = ReactiveCommand.Create(ExportLoadOrderToArchiveAs, canStartExport);

			AddOrderConfigCommand = ReactiveCommand.Create(new Action( () => { AddNewModOrder(); }));

			canOpenDialogWindow = this.WhenAnyValue(x => x.MainProgressIsActive, (b) => !b);
			ImportOrderFromSaveCommand = ReactiveCommand.Create(ImportOrderFromSaveToCurrent, canOpenDialogWindow);
			ImportOrderFromSaveAsNewCommand = ReactiveCommand.Create(ImportOrderFromSaveAsNew, canOpenDialogWindow);
			ImportOrderFromFileCommand = ReactiveCommand.Create(ImportOrderFromFile, canOpenDialogWindow);
			ImportOrderZipFileCommand = ReactiveCommand.Create(ImportOrderZipFile, canOpenDialogWindow);

			DeleteOrderCommand = ReactiveCommand.Create<DivinityLoadOrder, Unit>(DeleteOrder, canOpenDialogWindow);

			ToggleUpdatesViewCommand = ReactiveCommand.Create(() => { ModUpdatesViewVisible = !ModUpdatesViewVisible; });

			IObservable<bool> canCancelProgress = this.WhenAnyValue(x => x.CanCancelProgress);
			CancelMainProgressCommand = ReactiveCommand.Create(() =>
			{
				if (MainProgressToken != null && MainProgressToken.Token.CanBeCanceled)
				{
					MainProgressToken.Token.Register(() => { MainProgressIsActive = false; });
					MainProgressToken.Cancel();
				}
			}, canCancelProgress);

			var canRefreshObservable = this.WhenAnyValue(x => x.Refreshing, (r) => r == false);
			RefreshCommand = ReactiveCommand.Create(() => RefreshAsync_Start(), canRefreshObservable);

			CopyPathToClipboardCommand = ReactiveCommand.Create((string path) =>
			{
				if (!String.IsNullOrWhiteSpace(path))
				{
					Clipboard.SetText(path);
					ShowAlert($"Copied '{path}' to clipboard.", 0, 10);
				}
				else
				{
					ShowAlert($"Path not found.", -1, 30);
				}
			});

			var canOpenModsFolder = this.WhenAnyValue(x => x.PathwayData.DocumentsModsPath, (p) => !String.IsNullOrEmpty(p) && Directory.Exists(p));
			OpenModsFolderCommand = ReactiveCommand.Create(() =>
			{
				Process.Start(PathwayData.DocumentsModsPath);
			}, canOpenModsFolder);

			//canOpenWorkshopFolder.Subscribe((b) =>
			//{
			//	Trace.WriteLine($"Workshop folder exists: {b} | {Settings.DOS2WorkshopPath}");
			//});

			OpenWorkshopFolderCommand = ReactiveCommand.Create(() =>
			{
				Process.Start(Settings.DOS2WorkshopPath);
			}, canOpenWorkshopFolder);

			OpenDOS2GameCommand = ReactiveCommand.Create(() =>
			{
				if (!Settings.GameStoryLogEnabled)
				{
					Process.Start(Settings.DOS2DEGameExecutable);
				}
				else
				{
					Process.Start(Settings.DOS2DEGameExecutable, "-storylog 1");
				}
				
			}, canOpenDOS2DEGame);

			OpenDonationPageCommand = ReactiveCommand.Create(() =>
			{
				Process.Start(DivinityApp.URL_DONATION);
			});

			OpenRepoPageCommand = ReactiveCommand.Create(() =>
			{
				Process.Start(DivinityApp.URL_REPO);
			});

			ToggleDarkModeCommand = ReactiveCommand.Create(() =>
			{
				if (Settings != null)
				{
					Settings.DarkThemeEnabled = !Settings.DarkThemeEnabled;
				}
			});

			ToggleDisplayNameCommand = ReactiveCommand.Create(() =>
			{
				if (Settings != null)
				{
					Settings.DisplayFileNames = !Settings.DisplayFileNames;

					foreach(var m in Mods)
					{
						m.DisplayFileForName = Settings.DisplayFileNames;
						m.UpdateDisplayName();
					}
				}
				else
				{
					foreach (var m in Mods)
					{
						m.DisplayFileForName = !m.DisplayFileForName;
						m.UpdateDisplayName();
					}
				}
			});

			RenameSaveCommand = ReactiveCommand.Create(RenameSave_Start, canOpenDialogWindow);

			this.WhenAnyValue(x => x.SelectedProfileIndex, x => x.Profiles.Count, (index, count) => index >= 0 && count > 0 && index < count).Where(b => b == true).
				Select(x => Profiles[SelectedProfileIndex]).ToProperty(this, x => x.SelectedProfile, out selectedprofile).DisposeWith(this.Disposables);

			var selectedOrderObservable = this.WhenAnyValue(x => x.SelectedModOrderIndex, x => x.ModOrderList.Count, 
				(index, count) => index >= 0 && count > 0 && index < count).Where(b => b == true);

			selectedOrderObservable.Select(x => ModOrderList[SelectedModOrderIndex]).ToProperty(this, x => x.SelectedModOrder, out selectedModOrder).DisposeWith(this.Disposables);
			//selectedOrderObservable.Select(x => ModOrderList[SelectedModOrderIndex].DisplayName).ToProperty(this, x => x.SelectedModOrderDisplayName, out selectedModOrderDisplayName);

			var indexChanged = this.WhenAnyValue(vm => vm.SelectedModOrderIndex);
			//Throttle in case the index changes quickly in a short timespan
			indexChanged.Throttle(TimeSpan.FromMilliseconds(10)).ObserveOn(RxApp.MainThreadScheduler).Subscribe((selectedOrder) => {
				if (selectedOrder > -1 && !LoadingOrder)
				{
					LoadModOrder(SelectedModOrder);
				}
			});

			this.WhenAnyValue(vm => vm.SelectedProfileIndex, (index) => index > -1 && index < Profiles.Count).Subscribe((b) =>
			{
				if (b)
				{
					BuildModOrderList();
				}
			});

			var modsConnecton = mods.Connect();
			modsConnecton.Bind(out allMods).DisposeMany().Subscribe();
			workshopMods.Connect().Bind(out workshopModsCollection).DisposeMany().Subscribe();

			modsConnecton.WhenAnyPropertyChanged("Name", "IsClassicMod").Subscribe((mod) =>
			{
				mod.UpdateDisplayName();
			});

			modsConnecton.AutoRefresh(x => x.IsSelected).Filter(x => x.IsSelected && !x.IsEditorMod && File.Exists(x.FilePath)).Bind(out selectedPakMods).Subscribe();

			var anyPakModSelectedObservable = this.WhenAnyValue(x => x.SelectedPakMods.Count, (count) => count > 0);
			ExtractSelectedModsCommands = ReactiveCommand.Create(ExtractSelectedMods_Start, anyPakModSelectedObservable);

			this.WhenAnyValue(x => x.ModUpdatesViewData.NewAvailable, 
				x => x.ModUpdatesViewData.UpdatesAvailable, (b1, b2) => b1 || b2).BindTo(this, x => x.ModUpdatesAvailable);

			//this.WhenAnyValue(x => x.ModUpdatesAvailable).Subscribe((b) =>
			//{
			//	Trace.WriteLine("Updates available: " + b.ToString());
			//});

			ModUpdatesViewData.CloseView = new Action<bool>((bool refresh) =>
			{
				ModUpdatesViewData.Clear();
				if (refresh) RefreshAsync_Start();
				ModUpdatesViewVisible = false;
				view.Activate();
			});

			//this.WhenAnyValue(x => x.ModUpdatesViewData.JustUpdated).Subscribe((b) =>
			//{
			//	if(b)
			//	{
			//		ModUpdatesViewVisible = false;
			//		ModUpdatesViewData.Clear();
			//		Refresh();
			//	}
			//});

			DebugCommand = ReactiveCommand.Create(() => InactiveMods.Add(new DivinityModData() { Name = "Test" }));

			//Throttle filters so they only happen when typing stops for 500ms

			this.WhenAnyValue(x => x.ActiveModFilterText).Throttle(TimeSpan.FromMilliseconds(500)).ObserveOn(RxApp.MainThreadScheduler).
				Subscribe((s) => { OnFilterTextChanged(s, ActiveMods); });

			this.WhenAnyValue(x => x.InactiveModFilterText).Throttle(TimeSpan.FromMilliseconds(500)).ObserveOn(RxApp.MainThreadScheduler).
				Subscribe((s) => { OnFilterTextChanged(s, InactiveMods); });

			ActiveMods.CollectionChanged += ActiveMods_SetItemIndex;
			InactiveMods.CollectionChanged += InactiveMods_SetItemIndex;

			this.ActiveMods.ToObservableChangeSet().AutoRefresh(x => x.IsSelected).
				ToCollection().Select(x => x.Count(y => y.IsSelected)).ToProperty(this, x => x.ActiveSelected, out activeSelected);

			this.InactiveMods.ToObservableChangeSet().AutoRefresh(x => x.IsSelected).
				ToCollection().Select(x => x.Count(y => y.IsSelected)).ToProperty(this, x => x.InactiveSelected, out inactiveSelected);

			DivinityApp.Events.OrderNameChanged += OnOrderNameChanged;

#if DEBUG
			//this.WhenAnyValue(x => x.ActiveSelected).Subscribe((x) =>
			//{
			//	Trace.WriteLine($"Total selected active mods: {x}");
			//});
			//this.WhenAnyValue(x => x.InactiveSelected).Subscribe((x) =>
			//{
			//	Trace.WriteLine($"Total selected inactive mods: {x}");
			//});
#endif
			//ActiveModOrder.ObserveCollectionChanges().Subscribe(e =>
			//{
			//	if(e.EventArgs.OldItems != null)
			//	{
			//		string str = "";
			//		for (var i = 0; i < e.EventArgs.OldItems.Count; i++)
			//		{
			//			var obj = e.EventArgs.OldItems[i];
			//			str += obj.ToString();
			//			if (i < e.EventArgs.OldItems.Count - 1) str += ",";
			//		}
			//		Trace.WriteLine($"[OldItems] {str}");
			//	}
			//	if (e.EventArgs.NewItems != null)
			//	{
			//		string str = "";
			//		for(var i = 0; i < e.EventArgs.NewItems.Count; i++)
			//		{
			//			var obj = e.EventArgs.NewItems[i];
			//			str += obj.ToString();
			//			if (i < e.EventArgs.NewItems.Count - 1) str += ",";
			//		}
			//		Trace.WriteLine($"[NewItems] {str}");
			//	}
			//});
		}
    }
}
