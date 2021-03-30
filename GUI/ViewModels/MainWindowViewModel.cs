using AutoUpdaterDotNET;

using DivinityModManager.Extensions;
using DivinityModManager.Models;
using DivinityModManager.Models.App;
using DivinityModManager.Util;
using DivinityModManager.Views;

using DynamicData;
using DynamicData.Binding;

using Microsoft.Win32;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using SharpCompress.Common;
using SharpCompress.Writers;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DivinityModManager.ViewModels
{
	public class MainWindowViewModel : BaseHistoryViewModel, IActivatableViewModel, IDivinityAppViewModel
	{
		private MainWindow view;
		public MainWindow View => view;

		public ModViewLayout Layout { get; set; }

		private ModListDropHandler dropHandler;

		public ModListDropHandler DropHandler
		{
			get => dropHandler;
			set { this.RaiseAndSetIfChanged(ref dropHandler, value); }
		}

		private ModListDragHandler dragHandler;

		public ModListDragHandler DragHandler
		{
			get => dragHandler;
			set { this.RaiseAndSetIfChanged(ref dragHandler, value); }
		}

		private string title;

		public string Title
		{
			get => title;
			set { this.RaiseAndSetIfChanged(ref title, value); }
		}

		public string Version { get; set; } = "";

		public AppKeys Keys { get; private set; } = new AppKeys();

		private bool IsInitialized { get; set; } = false;

		protected SourceList<DivinityModData> mods = new SourceList<DivinityModData>();

		private List<DivinityModData> userMods = new List<DivinityModData>();

		protected ReadOnlyObservableCollection<DivinityModData> allMods;
		public ReadOnlyObservableCollection<DivinityModData> Mods => allMods;

		protected ReadOnlyObservableCollection<DivinityModData> adventureMods;
		public ReadOnlyObservableCollection<DivinityModData> AdventureMods => adventureMods;

		private int selectedAdventureModIndex = 0;

		public int SelectedAdventureModIndex
		{
			get => selectedAdventureModIndex;
			set
			{
				this.RaiseAndSetIfChanged(ref selectedAdventureModIndex, value);
				this.RaisePropertyChanged("SelectedAdventureMod");
			}
		}

		private readonly ObservableAsPropertyHelper<DivinityModData> selectedAdventureMod;
		public DivinityModData SelectedAdventureMod => selectedAdventureMod.Value;

		private readonly ObservableAsPropertyHelper<Visibility> adventureModBoxVisibility;
		public Visibility AdventureModBoxVisibility => adventureModBoxVisibility.Value;

		protected ReadOnlyObservableCollection<DivinityModData> selectedPakMods;
		public ReadOnlyObservableCollection<DivinityModData> SelectedPakMods => selectedPakMods;

		protected SourceList<DivinityModData> workshopMods = new SourceList<DivinityModData>();

		protected ReadOnlyObservableCollection<DivinityModData> workshopModsCollection;
		public ReadOnlyObservableCollection<DivinityModData> WorkshopMods => workshopModsCollection;

		private DivinityModManagerCachedWorkshopData CachedWorkshopData { get; set; } = new DivinityModManagerCachedWorkshopData();

		public DivinityPathwayData PathwayData { get; private set; } = new DivinityPathwayData();

		public ModUpdatesViewData ModUpdatesViewData { get; private set; }

		private IgnoredModsData ignoredModsData;

		public IgnoredModsData IgnoredMods => ignoredModsData;

		private AppSettings appSettings = new AppSettings();

		public AppSettings AppSettings => appSettings;

		[Reactive] public DivinityModManagerSettings Settings { get; set; }

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

		private ObservableAsPropertyHelper<DivinityLoadOrder> selectedModOrder;
		public DivinityLoadOrder SelectedModOrder => selectedModOrder.Value;

		private ObservableAsPropertyHelper<bool> isBaseLoadOrder;
		public bool IsBaseLoadOrder => isBaseLoadOrder.Value;

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

		private bool checkingForWorkshopUpdates = false;

		public bool CheckingForWorkshopUpdates
		{
			get => checkingForWorkshopUpdates;
			set { this.RaiseAndSetIfChanged(ref checkingForWorkshopUpdates, value); }
		}

		private bool highlightExtenderDownload = false;

		public bool HighlightExtenderDownload
		{
			get => highlightExtenderDownload;
			set { this.RaiseAndSetIfChanged(ref highlightExtenderDownload, value); }
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

		public async Task<Unit> IncreaseMainProgressValueAsync(double val, string message = "")
		{
			return await Observable.Start(() => {
				MainProgressValue += val;
				if (!String.IsNullOrEmpty(message)) MainProgressWorkText = message;
				return Unit.Default;
			}, RxApp.MainThreadScheduler);
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

		private bool isRenamingOrder = false;

		public bool IsRenamingOrder
		{
			get => isRenamingOrder;
			set { this.RaiseAndSetIfChanged(ref isRenamingOrder, value); }
		}

		private string statusBarRightText = "";

		public string StatusBarRightText
		{
			get => statusBarRightText;
			set { this.RaiseAndSetIfChanged(ref statusBarRightText, value); }
		}

		private Visibility statusBarBusyIndicatorVisibility = Visibility.Collapsed;

		public Visibility StatusBarBusyIndicatorVisibility
		{
			get => statusBarBusyIndicatorVisibility;
			set { this.RaiseAndSetIfChanged(ref statusBarBusyIndicatorVisibility, value); }
		}

		private bool workshopSupportEnabled;

		public bool WorkshopSupportEnabled
		{
			get => workshopSupportEnabled;
			set { this.RaiseAndSetIfChanged(ref workshopSupportEnabled, value); }
		}

		public IObservable<bool> canRenameOrder;

		//private IObservable<bool> canSaveSettings;
		private IObservable<bool> canOpenWorkshopFolder;
		private IObservable<bool> canOpenGameExe;
		private IObservable<bool> canOpenDialogWindow;
		private IObservable<bool> gameExeFoundObservable;
		//private IObservable<bool> canInstallOsiExtender;
		private IObservable<bool> canOpenLogDirectory;

		private bool OpenRepoLinkToDownload { get; set; } = false;
		public ICommand DebugCommand { get; private set; }
		public ICommand ToggleUpdatesViewCommand { get; private set; }
		public ICommand CheckForAppUpdatesCommand { get; set; }
		public ICommand CancelMainProgressCommand { get; set; }
		public ICommand CopyPathToClipboardCommand { get; set; }
		public ICommand DownloadAndInstallOsiExtenderCommand { get; private set; }
		public ICommand RenameSaveCommand { get; private set; }
		public ICommand CopyOrderToClipboardCommand { get; private set; }
		public ICommand OpenAdventureModInFileExplorerCommand { get; private set; }
		public ICommand CopyAdventureModPathToClipboardCommand { get; private set; }
		public ICommand ConfirmCommand { get; set; }
		public ICommand FocusFilterCommand { get; set; }
		public ICommand SaveSettingsSilentlyCommand { get; private set; }
		public ReactiveCommand<DivinityLoadOrder, Unit> DeleteOrderCommand { get; private set; }
		public ReactiveCommand<object, Unit> ToggleOrderRenamingCommand { get; set; }

		private DivinityGameLaunchWindowAction actionOnGameLaunch = DivinityGameLaunchWindowAction.None;
		public DivinityGameLaunchWindowAction ActionOnGameLaunch
		{
			get => actionOnGameLaunch;
			set { this.RaiseAndSetIfChanged(ref actionOnGameLaunch, value); }
		}
		public EventHandler OnRefreshed { get; set; }
		public EventHandler OnOrderChanged { get; set; }

		#region GameMaster Support

		private readonly ObservableAsPropertyHelper<Visibility> gameMasterModeVisibility;
		public Visibility GameMasterModeVisibility => gameMasterModeVisibility.Value;

		protected SourceList<DivinityGameMasterCampaign> gameMasterCampaigns = new SourceList<DivinityGameMasterCampaign>();

		private readonly ReadOnlyObservableCollection<DivinityGameMasterCampaign> gameMasterCampaignsData;
		public ReadOnlyObservableCollection<DivinityGameMasterCampaign> GameMasterCampaigns => gameMasterCampaignsData;

		private int selectedGameMasterCampaignIndex = 0;

		public int SelectedGameMasterCampaignIndex
		{
			get => selectedGameMasterCampaignIndex;
			set
			{
				this.RaiseAndSetIfChanged(ref selectedGameMasterCampaignIndex, value);
				this.RaisePropertyChanged("SelectedGameMasterCampaign");
			}
		}
		public bool UserChangedSelectedGMCampaign { get; set; } = false;

		private readonly ObservableAsPropertyHelper<DivinityGameMasterCampaign> selectedGameMasterCampaign;
		public DivinityGameMasterCampaign SelectedGameMasterCampaign => selectedGameMasterCampaign.Value;
		public ICommand OpenGameMasterCampaignInFileExplorerCommand { get; private set; }
		public ICommand CopyGameMasterCampaignPathToClipboardCommand { get; private set; }

		private void SetLoadedGMCampaigns(IEnumerable<DivinityGameMasterCampaign> data)
		{
			string lastSelectedCampaignUUID = "";
			if(UserChangedSelectedGMCampaign && SelectedGameMasterCampaign != null)
			{
				lastSelectedCampaignUUID = SelectedGameMasterCampaign.UUID;
			}

			gameMasterCampaigns.Clear();
			gameMasterCampaigns.AddRange(data);

			DivinityGameMasterCampaign nextSelected = null;

			if(String.IsNullOrEmpty(lastSelectedCampaignUUID) || !IsInitialized)
			{
				nextSelected = gameMasterCampaigns.Items.OrderByDescending(x => x.LastModified.HasValue ? x.LastModified.Value : DateTime.MinValue).FirstOrDefault();

			}
			else
			{
				nextSelected = gameMasterCampaigns.Items.FirstOrDefault(x => x.UUID == lastSelectedCampaignUUID);
			}

			if (nextSelected != null)
			{
				SelectedGameMasterCampaignIndex = gameMasterCampaigns.Items.IndexOf(nextSelected);
			}
			else
			{
				SelectedGameMasterCampaignIndex = 0;
			}
		}

		public bool LoadGameMasterCampaignModOrder(DivinityGameMasterCampaign campaign)
		{
			if (campaign.Dependencies == null) return false;

			var currentOrder = ModOrderList.First();
			currentOrder.Order.Clear();

			List<DivinityMissingModData> missingMods = new List<DivinityMissingModData>();
			if(campaign.Dependencies.Count > 0)
			{
				int index = 0;
				foreach (var entry in campaign.Dependencies)
				{
					var mod = mods.Items.FirstOrDefault(m => m.UUID == entry.UUID);
					if (mod != null && !mod.IsClassicMod)
					{
						ActiveMods.Add(mod);
						if (mod.Dependencies.Count > 0)
						{
							foreach (var dependency in mod.Dependencies.Items)
							{
								if (!DivinityModDataLoader.IgnoreMod(dependency.UUID) && !mods.Items.Any(x => x.UUID == dependency.UUID) &&
									!missingMods.Any(x => x.UUID == dependency.UUID))
								{
									var x = new DivinityMissingModData
									{
										Index = -1,
										Name = dependency.Name,
										UUID = dependency.UUID,
										Dependency = true
									};
									missingMods.Add(x);
								}
							}
						}
					}
					else if (!DivinityModDataLoader.IgnoreMod(entry.UUID) && !missingMods.Any(x => x.UUID == entry.UUID))
					{
						var x = new DivinityMissingModData
						{
							Index = index,
							Name = entry.Name,
							UUID = entry.UUID
						};
						missingMods.Add(x);
					}
					currentOrder.Order.Add(new DivinityLoadOrderEntry()
					{
						Name = entry.Name,
						UUID = entry.UUID,
						Missing = mod == null
					});
					index++;
				}
			}

			DivinityApp.Log($"Updated 'Current' with dependencies from GM campaign {campaign.Name}.");

			if (SelectedModOrderIndex == 0)
			{
				DivinityApp.Log($"Loading mod order for GM campaign {campaign.Name}.");
				LoadModOrder(currentOrder, missingMods);
			}

			return true;
		}

		#endregion

		private void Debug_TraceMods(List<DivinityModData> mods)
		{
			foreach (var mod in mods)
			{
				DivinityApp.Log($"Found mod. Name({mod.Name}) Author({mod.Author}) Version({mod.Version}) UUID({mod.UUID}) Description({mod.Description})");
				foreach (var dependency in mod.Dependencies.Items)
				{
					Console.WriteLine($"  {dependency.ToString()}");
				}
			}
		}

		private void Debug_TraceProfileModOrder(DivinityProfileData p)
		{
			DivinityApp.Log($"Mod Order for Profile {p.Name}");
			DivinityApp.Log("========================================");
			foreach (var uuid in p.ModOrder)
			{
				var modData = Mods.FirstOrDefault(m => m.UUID == uuid);
				if (modData != null)
				{
					DivinityApp.Log($"  {modData.Name}({modData.UUID})");
				}
				else
				{
					DivinityApp.Log($"  NOT FOUND {uuid}");
				}
			}
			DivinityApp.Log("========================================");
		}

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
						DivinityApp.Log($"Creating logs directory: {logsDirectory} | exe dir: {exePath}");
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

		private async Task<Unit> LoadExtenderSettingsAsync(CancellationToken t)
		{
			try
			{
				string latestReleaseZipUrl = "";
				DivinityApp.Log($"Checking for latest DXGI.dll release at 'Norbyte/ositools'.");
				var latestReleaseData = await GithubHelper.GetLatestReleaseDataAsync("Norbyte/ositools");
				if (!String.IsNullOrEmpty(latestReleaseData))
				{
					var jsonData = DivinityJsonUtils.SafeDeserialize<Dictionary<string, object>>(latestReleaseData);
					if (jsonData != null)
					{
						if (jsonData.TryGetValue("assets", out var assetsArray))
						{
							JArray assets = (JArray)assetsArray;
							foreach (var obj in assets.Children<JObject>())
							{
								if (obj.TryGetValue("browser_download_url", StringComparison.OrdinalIgnoreCase, out var browserUrl))
								{
									latestReleaseZipUrl = browserUrl.ToString();
								}
							}
						}
						if (jsonData.TryGetValue("tag_name", out var tagName))
						{
							PathwayData.OsirisExtenderLatestReleaseVersion = (string)tagName;
						}
#if DEBUG
						//var lines = jsonData.Select(kvp => kvp.Key + ": " + kvp.Value.ToString());
						//DivinityApp.LogMessage($"Releases Data:\n{String.Join(Environment.NewLine, lines)}");
#endif
					}
					if (!String.IsNullOrEmpty(latestReleaseZipUrl))
					{
						OpenRepoLinkToDownload = false;
						PathwayData.OsirisExtenderLatestReleaseUrl = latestReleaseZipUrl;
						DivinityApp.Log($"OsiTools latest release url found: {latestReleaseZipUrl}");
					}
					else
					{
						DivinityApp.Log($"OsiTools latest release not found.");
					}
				}
				else
				{
					OpenRepoLinkToDownload = true;
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error checking for latest OsiExtender release: {ex.ToString()}");

				OpenRepoLinkToDownload = true;
			}

			try
			{
				string extenderSettingsJson = PathwayData.OsirisExtenderSettingsFile(Settings);
				if (extenderSettingsJson.IsExistingFile())
				{
					var osirisExtenderSettings = DivinityJsonUtils.SafeDeserializeFromPath<OsiExtenderSettings>(extenderSettingsJson);
					if (osirisExtenderSettings != null)
					{
						DivinityApp.Log($"Loaded extender settings from '{extenderSettingsJson}'.");
						Settings.ExtenderSettings.Set(osirisExtenderSettings);
					}
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error loading extender settings: {ex.ToString()}");
			}

			string extenderUpdaterPath = Path.Combine(Path.GetDirectoryName(Settings.GameExecutablePath), "DXGI.dll");
			DivinityApp.Log($"Looking for OsiExtender at '{extenderUpdaterPath}'.");
			if (File.Exists(extenderUpdaterPath))
			{
				DivinityApp.Log($"Checking DXGI.dll for Osiris ASCII bytes.");
				try
				{
					using (var stream = new FileStream(extenderUpdaterPath, FileMode.Open, FileAccess.Read, FileShare.Read))
					{
						byte[] bytes = DivinityStreamUtils.ReadToEnd(stream);
						if (bytes.IndexOf(Encoding.ASCII.GetBytes("Osiris")) >= 0)
						{
							Settings.ExtenderSettings.ExtenderUpdaterIsAvailable = true;
							DivinityApp.Log($"Found the OsiExtender at '{extenderUpdaterPath}'.");
						}
						else
						{
							DivinityApp.Log($"Failed to find ASCII bytes in '{extenderUpdaterPath}'.");
						}
					}
				}
				catch (System.IO.IOException ex)
				{
					// This can happen if the game locks up the dll.
					// Assume it's the extender for now.
					Settings.ExtenderSettings.ExtenderUpdaterIsAvailable = true;
					DivinityApp.Log($"WARNING: {extenderUpdaterPath} is locked by a process.");
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error reading: '{extenderUpdaterPath}'\n\t{ex.ToString()}");
				}
			}
			else
			{
				Settings.ExtenderSettings.ExtenderUpdaterIsAvailable = false;
				DivinityApp.Log($"Extender DXGI.dll not found.");
			}

			string extenderAppFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OsirisExtender/OsiExtenderEoCApp.dll");
			if (File.Exists(extenderAppFile))
			{
				Settings.ExtenderSettings.ExtenderIsAvailable = true;
				try
				{
					FileVersionInfo extenderInfo = FileVersionInfo.GetVersionInfo(extenderAppFile);
					if (!String.IsNullOrEmpty(extenderInfo.FileVersion))
					{
						var version = extenderInfo.FileVersion.Split('.')[0];
						if (int.TryParse(version, out int intVersion))
						{
							Settings.ExtenderSettings.ExtenderVersion = intVersion;
							DivinityApp.Log($"Current OsiExtender version found: '{Settings.ExtenderSettings.ExtenderVersion}'.");
						}
						else
						{
							Settings.ExtenderSettings.ExtenderVersion = -1;
						}
					}
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error getting file info from: '{extenderAppFile}'\n\t{ex.ToString()}");
				}
			}
			return Unit.Default;
		}

		private void LoadExtenderSettings()
		{
			if (File.Exists(Settings.GameExecutablePath))
			{
				RxApp.TaskpoolScheduler.ScheduleAsync(async (c, t) =>
				{
					await LoadExtenderSettingsAsync(t);
					await c.Yield();
					RxApp.MainThreadScheduler.Schedule(CheckExtenderData);
					return Disposable.Empty;
				});
			}
			else
			{
				CheckExtenderData();
			}
		}

		private bool FilterDependencies(DivinityModDependencyData x, bool devMode)
		{
			if (!devMode)
			{
				return !DivinityModDataLoader.IgnoreModDependency(x.UUID);
			}
			return true;
		}

		private Func<DivinityModDependencyData, bool> MakeDependencyFilter(bool b)
		{
			return (x) => FilterDependencies(x, b);
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

			LoadAppConfig();

			canOpenWorkshopFolder = this.WhenAnyValue(x => x.WorkshopSupportEnabled, x => x.Settings.WorkshopPath,
				(b, p) => (b && !String.IsNullOrEmpty(p) && Directory.Exists(p))).StartWith(false);

			if (AppSettings.FeatureEnabled("Workshop"))
			{
				if (String.IsNullOrEmpty(Settings.WorkshopPath) || !Directory.Exists(Settings.WorkshopPath))
				{
					Settings.WorkshopPath = DivinityRegistryHelper.GetWorkshopPath(AppSettings.DefaultPathways.Steam.AppID).Replace("\\", "/");
					if (!String.IsNullOrEmpty(Settings.WorkshopPath) && Directory.Exists(Settings.WorkshopPath))
					{
						DivinityApp.Log($"Invalid workshop path set in settings file. Found workshop folder at: '{Settings.WorkshopPath}'.");
						SaveSettings();
					}
				}
				else if (Directory.Exists(Settings.WorkshopPath))
				{
					DivinityApp.Log($"Found workshop folder at: '{Settings.WorkshopPath}'.");
				}
				WorkshopSupportEnabled = true;
			}
			else
			{
				WorkshopSupportEnabled = false;
				Settings.WorkshopPath = "";
			}

			//canSaveSettings = this.WhenAnyValue(x => x.Settings.CanSaveSettings).StartWith(false);
			canOpenGameExe = this.WhenAnyValue(x => x.Settings.GameExecutablePath, (p) => !String.IsNullOrEmpty(p) && File.Exists(p)).StartWith(false);
			canOpenLogDirectory = this.WhenAnyValue(x => x.Settings.ExtenderLogDirectory, (f) => Directory.Exists(f)).StartWith(false);
			gameExeFoundObservable = this.WhenAnyValue(x => x.Settings.GameExecutablePath, (path) => path.IsExistingFile()).StartWith(false);
			//canInstallOsiExtender = this.WhenAnyValue(x => x.PathwayData.OsirisExtenderLatestReleaseUrl, x => x.Settings.GameExecutablePath,
			//	(url, exe) => !String.IsNullOrWhiteSpace(url) && exe.IsExistingFile()).ObserveOn(RxApp.MainThreadScheduler);
			DownloadAndInstallOsiExtenderCommand = ReactiveCommand.Create(InstallOsiExtender_Start).DisposeWith(Settings.Disposables);

			Keys.OpenLogsFolder.AddAction(() =>
			{
				Process.Start(Settings.ExtenderLogDirectory);
			}, canOpenLogDirectory, true);

			Keys.OpenWorkshopFolder.AddAction(() =>
			{
				//DivinityApp.Log($"WorkshopSupportEnabled:{WorkshopSupportEnabled} canOpenWorkshopFolder CanExecute:{OpenWorkshopFolderCommand.CanExecute(null)}");
				if (!String.IsNullOrEmpty(Settings.WorkshopPath) && Directory.Exists(Settings.WorkshopPath))
				{
					Process.Start(Settings.WorkshopPath);
				}
			}, canOpenWorkshopFolder, true);

			Keys.LaunchGame.AddAction(() =>
			{
				if (!File.Exists(Settings.GameExecutablePath))
				{
					if(String.IsNullOrWhiteSpace(Settings.GameExecutablePath))
					{
						ShowAlert("No game executable path set.", AlertType.Danger, 30);
					}
					else
					{
						ShowAlert($"Failed to find game exe at, \"{Settings.GameExecutablePath}\"", AlertType.Danger, 90);
					}
					return;
				}
				string launchParams = Settings.GameLaunchParams;
				if (String.IsNullOrEmpty(launchParams)) launchParams = "";

				if (Settings.GameStoryLogEnabled && launchParams.IndexOf("storylog") < 0)
				{
					if(String.IsNullOrWhiteSpace(launchParams))
					{
						launchParams = "-storylog 1";
					}
					else
					{
						launchParams = launchParams + " " + "-storylog 1";
					}
				}

				DivinityApp.Log($"Opening game exe at: {Settings.GameExecutablePath} with args {launchParams}");
				Process proc = new Process();
				proc.StartInfo.FileName = Settings.GameExecutablePath;
				proc.StartInfo.Arguments = launchParams;
				proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(Settings.GameExecutablePath);
				proc.Start();

				if (Settings.ActionOnGameLaunch != DivinityGameLaunchWindowAction.None)
				{
					switch (Settings.ActionOnGameLaunch)
					{
						case DivinityGameLaunchWindowAction.Minimize:
							view.WindowState = WindowState.Minimized;
							break;
						case DivinityGameLaunchWindowAction.Close:
							App.Current.Shutdown();
							break;
					}
				}

			}, canOpenGameExe, true);

			Settings.SaveSettingsCommand = ReactiveCommand.Create(() =>
			{
				try
				{
					System.IO.FileAttributes attr = File.GetAttributes(Settings.GameExecutablePath);

					if (attr.HasFlag(System.IO.FileAttributes.Directory))
					{
						string exeName = "";
						if (!DivinityRegistryHelper.IsGOG)
						{
							exeName = Path.GetFileName(AppSettings.DefaultPathways.Steam.ExePath);
						}
						else
						{
							exeName = Path.GetFileName(AppSettings.DefaultPathways.GOG.ExePath);
						}
						
						var exe = Path.Combine(Settings.GameExecutablePath, exeName);
						if (File.Exists(exe))
						{
							Settings.GameExecutablePath = exe;
						}
					}
				}
				catch (Exception ex) { }
				if (SaveSettings())
				{
					view.AlertBar.SetSuccessAlert($"Saved settings to '{settingsFile}'.", 10);
				}
			}).DisposeWith(Settings.Disposables);

			Settings.OpenSettingsFolderCommand = ReactiveCommand.Create(() =>
			{
				Process.Start(DivinityApp.DIR_DATA);
			}).DisposeWith(Settings.Disposables);

			Settings.ExportExtenderSettingsCommand = ReactiveCommand.Create(() =>
			{
				string outputFile = Path.Combine(Path.GetDirectoryName(Settings.GameExecutablePath), "OsirisExtenderSettings.json");
				try
				{
					var jsonSettings = new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore, Formatting = Formatting.Indented };
					if (Settings.ExportDefaultExtenderSettings)
					{
						jsonSettings.DefaultValueHandling = DefaultValueHandling.Include;
					}
					string contents = JsonConvert.SerializeObject(Settings.ExtenderSettings, jsonSettings);
					File.WriteAllText(outputFile, contents);
					view.AlertBar.SetSuccessAlert($"Saved Osiris Extender settings to '{outputFile}'.", 20);
				}
				catch (Exception ex)
				{
					view.AlertBar.SetDangerAlert($"Error saving Osiris Extender settings to '{outputFile}':\n{ex.ToString()}");
				}
			}).DisposeWith(Settings.Disposables);

			var canResetExtenderSettingsObservable = this.WhenAny(x => x.Settings.ExtenderSettings, (extenderSettings) => extenderSettings != null).StartWith(false);
			Settings.ResetExtenderSettingsToDefaultCommand = ReactiveCommand.Create(() =>
			{
				MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(view.SettingsWindow, $"Reset Extender Settings to Default?\nCurrent Extender Settings will be lost.", "Confirm Extender Settings Reset",
					MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, view.MainWindowMessageBox_OK.Style);
				if (result == MessageBoxResult.Yes)
				{
					Settings.ExportDefaultExtenderSettings = false;
					Settings.ExtenderSettings.SetToDefault();
				}
			}, canResetExtenderSettingsObservable).DisposeWith(Settings.Disposables);

			Settings.ResetKeybindingsCommand = ReactiveCommand.Create(() =>
			{
				MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(view.SettingsWindow, $"Reset Keybindings to Default?\nCurrent keybindings may be lost.", "Confirm Reset",
					MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, view.MainWindowMessageBox_OK.Style);
				if (result == MessageBoxResult.Yes)
				{
					Keys.SetToDefault();
				}
			});

			Settings.ClearWorkshopCacheCommand = ReactiveCommand.Create(() =>
			{
				if (File.Exists("Data\\workshopdata.json"))
				{
					MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(view.SettingsWindow, $"Delete local workshop cache?\nThis cannot be undone.\nRefresh to download tag data once more.", "Confirm Delete Cache",
					MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, view.MainWindowMessageBox_OK.Style);
					if (result == MessageBoxResult.Yes)
					{
						try
						{
							var fullFilePath = Path.GetFullPath("Data\\workshopdata.json");
							RecycleBinHelper.DeleteFile(fullFilePath, false, true);
							view.AlertBar.SetSuccessAlert($"Deleted local workshop cache at '{fullFilePath}'.", 20);
						}
						catch (Exception ex)
						{
							view.AlertBar.SetDangerAlert($"Error deleting workshop cache:\n{ex.ToString()}");
						}
					}
				}
			}).DisposeWith(Settings.Disposables);

			Settings.AddLaunchParamCommand = ReactiveCommand.Create((string param) =>
			{
				if (Settings.GameLaunchParams == null) Settings.GameLaunchParams = "";
				if (Settings.GameLaunchParams.IndexOf(param) < 0)
				{
					if(String.IsNullOrWhiteSpace(Settings.GameLaunchParams))
					{
						Settings.GameLaunchParams = param;
					}
					else
					{
						Settings.GameLaunchParams = Settings.GameLaunchParams + " " + param;
					}
				}
			}).DisposeWith(Settings.Disposables);

			Settings.ClearLaunchParamsCommand = ReactiveCommand.Create(() =>
			{
				Settings.GameLaunchParams = "";
			}).DisposeWith(Settings.Disposables);

			this.WhenAnyValue(x => x.Settings.LogEnabled).Subscribe((logEnabled) =>
			{
				ToggleLogging(logEnabled);
			}).DisposeWith(Settings.Disposables);

			this.WhenAnyValue(x => x.Settings.DarkThemeEnabled).ObserveOn(RxApp.MainThreadScheduler).Subscribe((b) =>
			{
				view.UpdateColorTheme(b);
				SaveSettings();
			}).DisposeWith(Settings.Disposables);

			// Updating extender requirement display
			this.WhenAnyValue(x => x.Settings.ExtenderSettings.EnableExtensions).ObserveOn(RxApp.MainThreadScheduler).Subscribe((b) =>
			{
				CheckExtenderData();
			}).DisposeWith(Settings.Disposables);

			ActionOnGameLaunch = Settings.ActionOnGameLaunch;

			var actionLaunchChanged = this.WhenAnyValue(x => x.ActionOnGameLaunch).ObserveOn(RxApp.MainThreadScheduler);
			actionLaunchChanged.Subscribe((action) =>
			{
				SaveSettings();
			}).DisposeWith(Settings.Disposables);
			actionLaunchChanged.BindTo(this, x => x.Settings.ActionOnGameLaunch).DisposeWith(Settings.Disposables);

			this.WhenAnyValue(x => x.Settings.DisplayFileNames).Subscribe((b) =>
			{
				if (view.MenuItems.TryGetValue("ToggleFileNameDisplay", out var menuItem))
				{
					if (b)
					{
						menuItem.Header = "Show Display Names for Mods";
					}
					else
					{
						menuItem.Header = "Show File Names for Mods";
					}
				}
			}).DisposeWith(Settings.Disposables);

			//DivinityApp.DependencyFilter = this.WhenAnyValue(x => x.Settings.DebugModeEnabled).Select(MakeDependencyFilter);
			//DisposeWith(Settings.Disposables);

			if (Settings.LogEnabled)
			{
				ToggleLogging(true);
			}

			SetGamePathways(Settings.GameDataPath);

			if (loaded)
			{
				Settings.CanSaveSettings = false;
				//view.AlertBar.SetSuccessAlert($"Loaded settings from '{settingsFile}'.", 5);
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
				Keys.SaveKeybindings(this);
				return true;
			}
			catch (Exception ex)
			{
				view.AlertBar.SetDangerAlert($"Error saving settings at '{settingsFile}': {ex.ToString()}");
			}
			return false;
		}

		public void LoadWorkshopMods()
		{
			if (Directory.Exists(Settings.WorkshopPath))
			{
				List<DivinityModData> modPakData = DivinityModDataLoader.LoadModPackageData(Settings.WorkshopPath, true);
				if (modPakData.Count > 0)
				{
					foreach (var workshopMod in modPakData)
					{
						string workshopID = Directory.GetParent(workshopMod.FilePath)?.Name;
						if (!String.IsNullOrEmpty(workshopID))
						{
							workshopMod.WorkshopData.ID = workshopID;
						}
					}
					//Ignore Classic mods since they share the same workshop folder
					var sortedWorkshopMods = modPakData.OrderBy(m => m.Name);
					workshopMods.Clear();
					workshopMods.AddRange(sortedWorkshopMods);

					DivinityApp.Log($"Loaded '{workshopMods.Count}' workshop mods from '{Settings.WorkshopPath}'.");
				}
			}
		}

		public async Task<List<DivinityModData>> LoadWorkshopModsAsync(CancellationToken? token = null)
		{
			List<DivinityModData> newWorkshopMods = new List<DivinityModData>();

			if (Directory.Exists(Settings.WorkshopPath))
			{
				newWorkshopMods = await DivinityModDataLoader.LoadModPackageDataAsync(Settings.WorkshopPath, true, token);
				if (token.HasValue && token.Value.IsCancellationRequested)
				{
					return newWorkshopMods;
				}
				foreach (var workshopMod in newWorkshopMods)
				{
					string workshopID = Directory.GetParent(workshopMod.FilePath)?.Name;
					if (!String.IsNullOrEmpty(workshopID))
					{
						workshopMod.WorkshopData.ID = workshopID;
					}
				}

				return newWorkshopMods.OrderBy(m => m.Name).ToList();
			}

			return newWorkshopMods;
		}

		public void CheckForModUpdates(CancellationToken? token = null)
		{
			ModUpdatesViewData.Clear();

			int count = 0;
			foreach (var workshopMod in WorkshopMods)
			{
				if (token.HasValue && token.Value.IsCancellationRequested)
				{
					break;
				}
				DivinityModData pakMod = mods.Items.FirstOrDefault(x => x.UUID == workshopMod.UUID && !x.IsClassicMod);

				if (pakMod != null)
				{
					pakMod.WorkshopData.ID = workshopMod.WorkshopData.ID;
					if (!pakMod.IsEditorMod)
					{
						//DivinityApp.LogMessage($"Comparing versions for ({pakMod.Name}): Workshop({workshopMod.Version.VersionInt})({workshopMod.Version.Version}) Local({pakMod.Version.VersionInt})({pakMod.Version.Version})");
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
						DivinityApp.Log($"[***WARNING***] An editor mod has a local workshop pak! ({pakMod.Name}):");
						DivinityApp.Log($"--- Editor Version({pakMod.Version.Version}) | Workshop Version({workshopMod.Version.Version})");
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
				DivinityApp.Log($"'{count}' mod updates pending.");
			}
			ModUpdatesViewData.OnLoaded?.Invoke();
			CheckingForWorkshopUpdates = false;
		}

		private void SetGamePathways(string currentGameDataPath)
		{
			try
			{
				string documentsFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				string larianDocumentsFolder = Path.Combine(documentsFolder, AppSettings.DefaultPathways.DocumentsGameFolder);
				PathwayData.LarianDocumentsFolder = larianDocumentsFolder;
				DivinityApp.Log($"Larian documents folder set to '{larianDocumentsFolder}'.");
				if (!Directory.Exists(larianDocumentsFolder))
				{
					Directory.CreateDirectory(larianDocumentsFolder);
				}

				string modPakFolder = Path.Combine(larianDocumentsFolder, "Mods");
				PathwayData.DocumentsModsPath = modPakFolder;
				if (!Directory.Exists(modPakFolder))
				{
					DivinityApp.Log($"No mods folder found at '{modPakFolder}'. Creating folder.");
					Directory.CreateDirectory(modPakFolder);
				}

				string gmCampaignsFolder = Path.Combine(larianDocumentsFolder, "GMCampaigns");
				PathwayData.DocumentsGMCampaignsPath = gmCampaignsFolder;
				if (!Directory.Exists(gmCampaignsFolder))
				{
					DivinityApp.Log($"No GM campaigns folder found at '{gmCampaignsFolder}'.");
				}

				string profileFolder = (Path.Combine(larianDocumentsFolder, "PlayerProfiles"));
				PathwayData.DocumentsProfilesPath = profileFolder;
				if (!Directory.Exists(profileFolder))
				{
					DivinityApp.Log($"Creating profile folder at '{profileFolder}'.");
					Directory.CreateDirectory(profileFolder);
				}

				if (String.IsNullOrEmpty(currentGameDataPath) || !Directory.Exists(currentGameDataPath))
				{
					string installPath = DivinityRegistryHelper.GetGameInstallPath(AppSettings.DefaultPathways.Steam.RootFolderName,
						AppSettings.DefaultPathways.GOG.Registry_32, AppSettings.DefaultPathways.GOG.Registry_64);
					if (Directory.Exists(installPath))
					{
						PathwayData.InstallPath = installPath;
						if (!File.Exists(Settings.GameExecutablePath))
						{
							string exePath = "";
							if (!DivinityRegistryHelper.IsGOG)
							{
								exePath = Path.Combine(installPath, AppSettings.DefaultPathways.Steam.ExePath);
							}
							else
							{
								exePath = Path.Combine(installPath, AppSettings.DefaultPathways.GOG.ExePath);
							}
							if (File.Exists(exePath))
							{
								Settings.GameExecutablePath = exePath.Replace("\\", "/");
								DivinityApp.Log($"Exe path set to '{exePath}'.");
							}
						}

						string gameDataPath = Path.Combine(installPath, AppSettings.DefaultPathways.GameDataFolder).Replace("\\", "/");
						DivinityApp.Log($"Set game data path to '{gameDataPath}'.");
						Settings.GameDataPath = gameDataPath;
						SaveSettings();
					}
				}
				else
				{
					string installPath = Path.GetFullPath(Path.Combine(Settings.GameDataPath, @"..\..\"));
					PathwayData.InstallPath = installPath;
					if (!File.Exists(Settings.GameExecutablePath))
					{
						string exePath = "";
						if (!DivinityRegistryHelper.IsGOG)
						{
							exePath = Path.Combine(installPath, AppSettings.DefaultPathways.Steam.ExePath);
						}
						else
						{
							exePath = Path.Combine(installPath, AppSettings.DefaultPathways.GOG.ExePath);
						}
						if (File.Exists(exePath))
						{
							Settings.GameExecutablePath = exePath.Replace("\\", "/");
							DivinityApp.Log($"Exe path set to '{exePath}'.");
						}
					}
				}

				if (AppSettings.FeatureEnabled("ScriptExtender"))
				{
					LoadExtenderSettings();
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error setting up game pathways: {ex.ToString()}");
			}
		}

		private void SetLoadedMods(IEnumerable<DivinityModData> loadedMods)
		{
			mods.Clear();
			foreach (var m in DivinityApp.IgnoredMods)
			{
				mods.Add(m);
				DivinityApp.Log($"Added ignored mod: Name({m.Name}) UUID({m.UUID}) Type({m.Type}) Version({m.Version.VersionInt})");
			}
			foreach (var m in loadedMods)
			{
				if (m.IsLarianMod)
				{
					var existingIgnoredMod = DivinityApp.IgnoredMods.FirstOrDefault(x => x.UUID == m.UUID);
					if (existingIgnoredMod != null)
					{
						DivinityApp.IgnoredMods.Remove(existingIgnoredMod);
					}
					DivinityApp.IgnoredMods.Add(m);
				}
				var existingMod = mods.Items.FirstOrDefault(x => x.UUID == m.UUID);
				if (existingMod == null)
				{
					mods.Add(m);
				}
				else
				{
					if (m.Version.VersionInt > existingMod.Version.VersionInt)
					{
						mods.Remove(existingMod);
						mods.Add(m);
						DivinityApp.Log($"Updated mod data from pak: Name({m.Name}) UUID({m.UUID}) Type({m.Type}) Version({m.Version.VersionInt})");
					}
				}
			}
			userMods.Clear();
			userMods.AddRange(loadedMods);
		}

		public List<DivinityModData> LoadMods()
		{
			List<DivinityModData> finalMods = new List<DivinityModData>();
			List<DivinityModData> modPakData = null;
			List<DivinityModData> projects = null;
			List<DivinityModData> baseMods = null;

			if (Directory.Exists(PathwayData.DocumentsModsPath))
			{
				DivinityApp.Log($"Loading mods from '{PathwayData.DocumentsModsPath}'.");
				modPakData = DivinityModDataLoader.LoadModPackageData(PathwayData.DocumentsModsPath);
			}

			GameDirectoryFound = Directory.Exists(Settings.GameDataPath);

			if (GameDirectoryFound)
			{
				GameDirectoryFound = true;
				string modsDirectory = Path.Combine(Settings.GameDataPath, "Mods");
				if (Directory.Exists(modsDirectory))
				{
					DivinityApp.Log($"Loading mod projects from '{modsDirectory}'.");
					projects = DivinityModDataLoader.LoadEditorProjects(modsDirectory);
				}

				baseMods = DivinityModDataLoader.LoadBuiltinMods(Settings.GameDataPath);
			}

			if (baseMods != null) MergeModLists(finalMods, baseMods);
			if (modPakData != null) MergeModLists(finalMods, modPakData);
			if (projects != null) MergeModLists(finalMods, projects);

			finalMods = finalMods.OrderBy(m => m.Name).ToList();
			DivinityApp.Log($"Loaded '{finalMods.Count}' mods.");
			return finalMods;
		}

		private void MergeModLists(List<DivinityModData> finalMods, List<DivinityModData> newMods)
		{
			foreach (var mod in newMods)
			{
				var existing = finalMods.FirstOrDefault(x => x.UUID == mod.UUID);
				if (existing != null)
				{
					if (existing.Version.VersionInt < mod.Version.VersionInt)
					{
						finalMods.Remove(existing);
						finalMods.Add(existing);
					}
				}
				else
				{
					finalMods.Add(mod);
				}
			}
		}

		private CancellationTokenSource GetCancellationToken(int delay, CancellationTokenSource last=null)
		{
			CancellationTokenSource token = new CancellationTokenSource();
			if (last != null && last.IsCancellationRequested)
			{
				last.Dispose();
			}
			token.CancelAfter(delay);
			return token;
		}

		private async Task<TResult> RunTask<TResult>(Task<TResult> task, TResult defaultValue)
		{
			try
			{
				return await task;
			}
			catch (OperationCanceledException ex)
			{
				DivinityApp.Log("Operation timed out/canceled.");
			}
			catch (TimeoutException ex)
			{
				DivinityApp.Log("Operation timed out.");
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error awaiting task:\n{ex.ToString()}");
			}
			return defaultValue;
		}

		public async Task<List<DivinityModData>> LoadModsAsync(double taskStepAmount = 0.1d)
		{
			List<DivinityModData> finalMods = new List<DivinityModData>();
			List<DivinityModData> modPakData = null;
			List<DivinityModData> projects = null;
			List<DivinityModData> baseMods = null;

			var cancelTokenSource = GetCancellationToken(int.MaxValue);

			if (Directory.Exists(PathwayData.DocumentsModsPath))
			{
				DivinityApp.Log($"Loading mods from '{PathwayData.DocumentsModsPath}'.");
				await SetMainProgressTextAsync("Loading mods from documents folder...");
				cancelTokenSource.CancelAfter(60000);
				modPakData = await RunTask(DivinityModDataLoader.LoadModPackageDataAsync(PathwayData.DocumentsModsPath, false, cancelTokenSource.Token), null);
				cancelTokenSource = GetCancellationToken(int.MaxValue);
				await IncreaseMainProgressValueAsync(taskStepAmount);
			}

			GameDirectoryFound = Directory.Exists(Settings.GameDataPath);

			if (GameDirectoryFound)
			{
				GameDirectoryFound = true;
				string modsDirectory = Path.Combine(Settings.GameDataPath, "Mods");
				if (Directory.Exists(modsDirectory))
				{
					DivinityApp.Log($"Loading mod projects from '{modsDirectory}'.");
					await SetMainProgressTextAsync("Loading editor project mods...");
					cancelTokenSource = GetCancellationToken(30000);
					projects = await RunTask(DivinityModDataLoader.LoadEditorProjectsAsync(modsDirectory, cancelTokenSource.Token), null);
					cancelTokenSource = GetCancellationToken(int.MaxValue);
					await IncreaseMainProgressValueAsync(taskStepAmount);
				}

				await SetMainProgressTextAsync("Loading base game mods from data folder...");
				cancelTokenSource = GetCancellationToken(30000);
				baseMods = await RunTask(DivinityModDataLoader.LoadBuiltinModsAsync(Settings.GameDataPath, cancelTokenSource.Token), null);
				cancelTokenSource = GetCancellationToken(int.MaxValue);
				await IncreaseMainProgressValueAsync(taskStepAmount);
			}

			if (baseMods != null) MergeModLists(finalMods, baseMods);
			if (modPakData != null) MergeModLists(finalMods, modPakData);
			if (projects != null) MergeModLists(finalMods, projects);

			finalMods = finalMods.OrderBy(m => m.Name).ToList();
			DivinityApp.Log($"Loaded '{finalMods.Count}' mods.");
			return finalMods;
		}

		public async Task<List<DivinityGameMasterCampaign>> LoadGameMasterCampaignsAsync(double taskStepAmount = 0.1d)
		{
			List<DivinityGameMasterCampaign> data = null;

			var cancelTokenSource = GetCancellationToken(int.MaxValue);

			if (Directory.Exists(PathwayData.DocumentsGMCampaignsPath))
			{
				DivinityApp.Log($"Loading gamemaster campaigns from '{PathwayData.DocumentsGMCampaignsPath}'.");
				await SetMainProgressTextAsync("Loading GM Campaigns from documents folder...");
				cancelTokenSource.CancelAfter(60000);
				data = await RunTask(DivinityModDataLoader.LoadGameMasterDataAsync(PathwayData.DocumentsGMCampaignsPath, cancelTokenSource.Token), null);
				cancelTokenSource = GetCancellationToken(int.MaxValue);
				await IncreaseMainProgressValueAsync(taskStepAmount);
			}

			if (data != null)
			{
				data = data.OrderBy(m => m.Name).ToList();
			}
			
			DivinityApp.Log($"Loaded '{data.Count}' GM campaigns.");
			return data;
		}

		public bool ModIsAvailable(IDivinityModData divinityModData)
		{
			return mods.Items.Any(k => k.UUID == divinityModData.UUID) 
				|| DivinityApp.IgnoredMods.Any(im => im.UUID == divinityModData.UUID) 
				|| DivinityApp.IgnoredDependencyMods.Any(d => d.UUID == divinityModData.UUID);
		}

		public void LoadProfiles()
		{
			if (Directory.Exists(PathwayData.DocumentsProfilesPath))
			{
				DivinityApp.Log($"Loading profiles from '{PathwayData.DocumentsProfilesPath}'.");

				var profiles = DivinityModDataLoader.LoadProfileData(PathwayData.DocumentsProfilesPath);
				Profiles.AddRange(profiles);

				DivinityApp.Log($"Loaded '{Profiles.Count}' profiles.");

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

				DivinityApp.Log($"Last selected UUID: {selectedUUID}");
			}
			else
			{
				DivinityApp.Log($"Profile folder not found at '{PathwayData.DocumentsProfilesPath}'.");
			}
		}

		public async Task<List<DivinityProfileData>> LoadProfilesAsync()
		{
			if (Directory.Exists(PathwayData.DocumentsProfilesPath))
			{
				DivinityApp.Log($"Loading profiles from '{PathwayData.DocumentsProfilesPath}'.");

				var profiles = await DivinityModDataLoader.LoadProfileDataAsync(PathwayData.DocumentsProfilesPath);
				DivinityApp.Log($"Loaded '{profiles.Count}' profiles.");
				return profiles;
			}
			else
			{
				DivinityApp.Log($"Profile folder not found at '{PathwayData.DocumentsProfilesPath}'.");
			}
			return null;
		}

		public void BuildModOrderList(int selectIndex = -1)
		{
			if (SelectedProfile != null)
			{
				LoadingOrder = true;

				List<DivinityMissingModData> missingMods = new List<DivinityMissingModData>();

				if (SelectedProfile.SavedLoadOrder == null)
				{
					DivinityLoadOrder currentOrder = new DivinityLoadOrder() { Name = "Current", FilePath = Path.Combine(SelectedProfile.Folder, "modsettings.lsx") };

					foreach (var uuid in SelectedProfile.ModOrder)
					{
						var activeModData = SelectedProfile.ActiveMods.FirstOrDefault(y => y.UUID == uuid);
						if (activeModData != null)
						{
							var mod = mods.Items.FirstOrDefault(m => m.UUID.Equals(uuid, StringComparison.OrdinalIgnoreCase));
							if (mod != null)
							{
								currentOrder.Add(mod);
							}
							else
							{
								var x = new DivinityMissingModData
								{
									Index = SelectedProfile.ModOrder.IndexOf(uuid),
									Name = activeModData.Name,
									UUID = activeModData.UUID
								};
								missingMods.Add(x);
							}
						}
						else
						{
							DivinityApp.Log($"UUID {uuid} is missing from the profile's active mod list.");
						}
					}

					SelectedProfile.SavedLoadOrder = currentOrder;
				}

				DivinityApp.Log($"Profile order: {String.Join(";", SelectedProfile.SavedLoadOrder.Order.Select(x => x.Name))}");

				ModOrderList.Clear();
				ModOrderList.Add(SelectedProfile.SavedLoadOrder);
				ModOrderList.AddRange(SavedModOrderList);

				RxApp.MainThreadScheduler.Schedule(_ =>
				{
					if (selectIndex != -1)
					{
						if (selectIndex >= ModOrderList.Count) selectIndex = ModOrderList.Count - 1;
						DivinityApp.Log($"Setting next order index to [{selectIndex}/{ModOrderList.Count - 1}].");
						try
						{
							SelectedModOrderIndex = selectIndex;
							var nextOrder = ModOrderList.ElementAtOrDefault(selectedModOrderIndex);
							if(nextOrder.Name == "Current" && Settings.GameMasterModeEnabled && SelectedGameMasterCampaign != null)
							{
								LoadGameMasterCampaignModOrder(SelectedGameMasterCampaign);
							}
							else
							{
								LoadModOrder(nextOrder, missingMods);
							}
							
							Settings.LastOrder = nextOrder?.Name;
						}
						catch (Exception ex)
						{
							DivinityApp.Log($"Error setting next load order:\n{ex.ToString()}");
						}
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
				BuildModOrderList(lastIndex);
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
				BuildModOrderList(SavedModOrderList.Count);
			};

			this.CreateSnapshot(undo, redo);

			redo();
		}

		public bool LoadModOrder(DivinityLoadOrder order, List<DivinityMissingModData> missingModsFromProfileOrder = null)
		{
			if (order == null) return false;

			LoadingOrder = true;

			ActiveMods.Clear();
			InactiveMods.Clear();

			var loadFrom = order.Order;

			DivinityApp.Log($"Loading mod order '{order.Name}'.");
			List<DivinityMissingModData> missingMods = new List<DivinityMissingModData>();
			if (missingModsFromProfileOrder != null && missingModsFromProfileOrder.Count > 0)
			{
				missingMods.AddRange(missingModsFromProfileOrder);
				DivinityApp.Log($"Missing mods (from profile): {String.Join(";", missingModsFromProfileOrder)}");
			}

			for (int i = 0; i < loadFrom.Count; i++)
			{
				var entry = loadFrom[i];
				var mod = mods.Items.FirstOrDefault(m => m.UUID == entry.UUID);
				if (mod != null && !mod.IsClassicMod)
				{
					ActiveMods.Add(mod);
					if (mod.Dependencies.Count > 0)
					{
						foreach (var dependency in mod.Dependencies.Items)
						{
							if (!DivinityModDataLoader.IgnoreMod(dependency.UUID) && !mods.Items.Any(x => x.UUID == dependency.UUID) &&
								!missingMods.Any(x => x.UUID == dependency.UUID))
							{
								var x = new DivinityMissingModData
								{
									Index = -1,
									Name = dependency.Name,
									UUID = dependency.UUID,
									Dependency = true
								};
								missingMods.Add(x);
							}
						}
					}
				}
				else if (!DivinityModDataLoader.IgnoreMod(entry.UUID) && !missingMods.Any(x => x.UUID == entry.UUID))
				{
					var x = new DivinityMissingModData
					{
						Index = i,
						Name = entry.Name,
						UUID = entry.UUID
					};
					missingMods.Add(x);
					entry.Missing = true;
				}
			}

			List<DivinityModData> inactive = new List<DivinityModData>();

			for (int i = 0; i < Mods.Count; i++)
			{
				var mod = Mods[i];
				if (ActiveMods.Any(m => m.UUID == mod.UUID))
				{
					mod.IsActive = true;
				}
				else
				{
					mod.IsActive = false;
					mod.Index = -1;
					inactive.Add(mod);
				}
			}

			InactiveMods.AddRange(inactive.OrderBy(m => m.Name));

			OnFilterTextChanged(ActiveModFilterText, ActiveMods);
			OnFilterTextChanged(InactiveModFilterText, InactiveMods);

			OnOrderChanged?.Invoke(this, new EventArgs());

			if (missingMods.Count > 0)
			{
				DivinityApp.Log($"Missing mods: {String.Join(";", missingMods)}");
				if (Settings?.DisableMissingModWarnings == true)
				{
					DivinityApp.Log("Skipping missing mod display.");
				}
				else
				{
					view.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
					view.MainWindowMessageBox_OK.Closed += MainWindowMessageBox_Closed_ResetColor;
					view.MainWindowMessageBox_OK.ShowMessageBox(String.Join("\n", missingMods.OrderBy(x => x.Index)),
						"Missing Mods in Load Order", MessageBoxButton.OK);
				}
			}

			LoadingOrder = false;
			return true;
		}

		private void MainWindowMessageBox_Closed_ResetColor(object sender, EventArgs e)
		{
			if (sender is Xceed.Wpf.Toolkit.MessageBox messageBox)
			{
				messageBox.WindowBackground = new SolidColorBrush(Color.FromRgb(78, 56, 201));
				messageBox.Closed -= MainWindowMessageBox_Closed_ResetColor;
			}
		}

		private bool refreshing = false;

		public bool Refreshing
		{
			get => refreshing;
			set { this.RaiseAndSetIfChanged(ref refreshing, value); }
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

			DivinityApp.Log($"Attempting to load saved load orders from '{loadOrderDirectory}'.");
			var savedOrderList = DivinityModDataLoader.FindLoadOrderFilesInDirectory(loadOrderDirectory);
			return savedOrderList;
		}

		private void CheckExtenderData()
		{
			if (Settings != null && Mods.Count > 0)
			{
				foreach (var mod in Mods)
				{
					if (mod.OsiExtenderData != null && mod.OsiExtenderData.HasAnySettings)
					{
						// Assume an Lua-only mod actually requires the extender, otherwise functionality is limited.
						bool onlyUsesLua = mod.OsiExtenderData.FeatureFlags.Contains("Lua") && !mod.OsiExtenderData.FeatureFlags.Contains("OsirisExtensions");

						if (!mod.OsiExtenderData.FeatureFlags.Contains("Preprocessor") || onlyUsesLua)
						{
							if (!Settings.ExtenderSettings.EnableExtensions)
							{
								mod.ExtenderModStatus = DivinityExtenderModStatus.REQUIRED_DISABLED;
							}
							else
							{
								if (Settings.ExtenderSettings != null && Settings.ExtenderSettings.ExtenderVersion > -1 && Settings.ExtenderSettings.ExtenderUpdaterIsAvailable)
								{
									if (mod.OsiExtenderData.RequiredExtensionVersion > -1 && Settings.ExtenderSettings.ExtenderVersion < mod.OsiExtenderData.RequiredExtensionVersion)
									{
										mod.ExtenderModStatus = DivinityExtenderModStatus.REQUIRED_OLD;
									}
									else
									{
										mod.ExtenderModStatus = DivinityExtenderModStatus.REQUIRED;
									}
								}
								else
								{
									mod.ExtenderModStatus = DivinityExtenderModStatus.REQUIRED_MISSING;
								}
							}
						}
						else
						{
							mod.ExtenderModStatus = DivinityExtenderModStatus.SUPPORTS;
						}
					}
					else
					{
						mod.ExtenderModStatus = DivinityExtenderModStatus.NONE;
					}
				}
			}
		}

		private async Task<Unit> SetMainProgressTextAsync(string text)
		{
			return await Observable.Start(() => {
				MainProgressWorkText = text;
				return Unit.Default;
			}, RxApp.MainThreadScheduler);
		}

		private CancellationToken? workshopModLoadingCancelToken;

		private List<string> ignoredModProjectNames = new List<string> { "Test", "Debug" };
		private bool CanFetchWorkshopData(DivinityModData mod)
		{
			if (CachedWorkshopData.NonWorkshopMods.Contains(mod.UUID))
			{
				return false;
			}
			if (mod.IsEditorMod && (ignoredModProjectNames.Any(x => mod.Folder.IndexOf(x, StringComparison.OrdinalIgnoreCase) > -1) ||
				String.IsNullOrEmpty(mod.Author) || String.IsNullOrEmpty(mod.Description)))
			{
				return false;
			}
			else if (mod.Author == "Larian" || String.IsNullOrEmpty(mod.DisplayName))
			{
				return false;
			}
			return String.IsNullOrEmpty(mod.WorkshopData.ID) || !CachedWorkshopData.Mods.Any(x => x.UUID == mod.UUID);
		}

		private async Task<bool> CacheAllWorkshopModsAsync()
		{
			var success = await DivinityWorkshopDataLoader.GetAllWorkshopDataAsync(CachedWorkshopData, AppSettings.DefaultPathways.Steam.AppID);
			if (success)
			{
				var nonWorkshopMods = userMods.Where(x => !CachedWorkshopData.Mods.Any(y => y.UUID == x.UUID)).ToList();
				if(nonWorkshopMods.Count > 0)
				{
					foreach(var m in nonWorkshopMods)
					{
						CachedWorkshopData.AddNonWorkshopMod(m.UUID);
					}
				}
			}
			return success;
		}


		private void UpdateModDataWithCachedData()
		{
			foreach (var mod in userMods)
			{
				var cachedMods = CachedWorkshopData.Mods.Where(x => x.UUID == mod.UUID);
				if (cachedMods != null)
				{
					foreach (var cachedMod in cachedMods)
					{
						if (String.IsNullOrEmpty(mod.WorkshopData.ID) || mod.WorkshopData.ID == cachedMod.WorkshopID)
						{
							mod.WorkshopData.ID = cachedMod.WorkshopID;
							mod.WorkshopData.CreatedDate = DateUtils.UnixTimeStampToDateTime(cachedMod.Created);
							mod.WorkshopData.UpdatedDate = DateUtils.UnixTimeStampToDateTime(cachedMod.LastUpdated);
							mod.WorkshopData.Tags = cachedMod.Tags;
							mod.AddTags(cachedMod.Tags);
							if (cachedMod.LastUpdated > 0)
							{
								mod.LastUpdated = mod.WorkshopData.UpdatedDate;
							}
						}
					}
				}
			}
		}

		private void LoadWorkshopModDataBackground()
		{
			bool workshopCacheFound = false;
			CheckingForWorkshopUpdates = true;

			RxApp.TaskpoolScheduler.ScheduleAsync(async (s, token) =>
			{
				workshopModLoadingCancelToken = token;
				var loadedWorkshopMods = await LoadWorkshopModsAsync(workshopModLoadingCancelToken);
				await Observable.Start(() => {
					workshopMods.AddRange(loadedWorkshopMods);
					DivinityApp.Log($"Loaded '{workshopMods.Count}' workshop mods from '{Settings.WorkshopPath}'.");
					if (!workshopModLoadingCancelToken.Value.IsCancellationRequested)
					{
						CheckForModUpdates(workshopModLoadingCancelToken);
					}
					return Unit.Default;
				}, RxApp.MainThreadScheduler);

				if (File.Exists("Data\\workshopdata.json"))
				{
					DivinityModManagerCachedWorkshopData cachedData = DivinityJsonUtils.SafeDeserializeFromPath<DivinityModManagerCachedWorkshopData>("Data\\workshopdata.json");
					if (cachedData != null)
					{
						CachedWorkshopData = cachedData;
						if(String.IsNullOrEmpty(CachedWorkshopData.LastVersion) || CachedWorkshopData.LastVersion != this.Version)
						{
							CachedWorkshopData.LastUpdated = -1;
						}
						UpdateModDataWithCachedData();
						workshopCacheFound = true;
					}
				} 

				if (!Settings.DisableWorkshopTagCheck)
				{
					if (!workshopCacheFound || CachedWorkshopData.LastUpdated == -1 || (DateTimeOffset.Now.ToUnixTimeSeconds() - CachedWorkshopData.LastUpdated >= 3600))
					{
						RxApp.MainThreadScheduler.Schedule(() =>
						{
							StatusBarRightText = "Downloading workshop data...";
							StatusBarBusyIndicatorVisibility = Visibility.Visible;
						});
						bool success = await CacheAllWorkshopModsAsync();
						if(success)
						{
							UpdateModDataWithCachedData();
							CachedWorkshopData.LastUpdated = DateTimeOffset.Now.ToUnixTimeSeconds();
						}
					}
					else
					{
						DivinityApp.Log("Checking for mods missing workshop data.");
						var targetMods = userMods.Where(x => CanFetchWorkshopData(x)).ToList();
						if (targetMods.Count > 0)
						{
							RxApp.MainThreadScheduler.Schedule(() =>
							{
								StatusBarRightText = $"Downloading workshop data for {targetMods.Count} mods...";
								StatusBarBusyIndicatorVisibility = Visibility.Visible;
							});
							var totalSuccesses = await DivinityWorkshopDataLoader.FindWorkshopDataAsync(targetMods, CachedWorkshopData, AppSettings.DefaultPathways.Steam.AppID);
							if(totalSuccesses > 0)
							{
								UpdateModDataWithCachedData();
							}
						}
					}

					CachedWorkshopData.LastVersion = this.Version;

					RxApp.MainThreadScheduler.Schedule(() =>
					{
						StatusBarRightText = "";
						StatusBarBusyIndicatorVisibility = Visibility.Collapsed;
						string updateMessage = !CachedWorkshopData.CacheUpdated ? "cached " : "";
						view.AlertBar.SetSuccessAlert($"Loaded {updateMessage}workshop data ({CachedWorkshopData.Mods.Count} mods).", 60);
					});

					if (CachedWorkshopData.CacheUpdated)
					{
						await DivinityFileUtils.WriteFileAsync("Data\\workshopdata.json", CachedWorkshopData.Serialize());
						CachedWorkshopData.CacheUpdated = false;
					}
				}
			});
		}

		private async Task<IDisposable> RefreshAsync(IScheduler ctrl, CancellationToken t)
		{
			DivinityApp.Log($"Refreshing data asynchronously...");

			double taskStepAmount = 1.0 / 11;

			List<DivinityLoadOrderEntry> lastActiveOrder = null;
			int lastOrderIndex = -1;
			if (SelectedModOrder != null)
			{
				lastActiveOrder = SelectedModOrder.Order.ToList();
				lastOrderIndex = SelectedModOrderIndex;
			}

			string lastAdventureMod = null;
			if (SelectedAdventureMod != null) lastAdventureMod = SelectedAdventureMod.UUID;

			string selectedProfileUUID = "";
			if (SelectedProfile != null)
			{
				selectedProfileUUID = SelectedProfile.UUID;
			}

			if (Directory.Exists(PathwayData.LarianDocumentsFolder))
			{
				await SetMainProgressTextAsync("Loading mods...");
				var loadedMods = await LoadModsAsync(taskStepAmount);
				await IncreaseMainProgressValueAsync(taskStepAmount);

				await SetMainProgressTextAsync("Loading profiles...");
				var loadedProfiles = await LoadProfilesAsync();
				await IncreaseMainProgressValueAsync(taskStepAmount);

				if (String.IsNullOrEmpty(selectedProfileUUID) && (loadedProfiles != null && loadedProfiles.Count > 0))
				{
					await SetMainProgressTextAsync("Loading current profile...");
					selectedProfileUUID = await DivinityModDataLoader.GetSelectedProfileUUIDAsync(PathwayData.DocumentsProfilesPath);
					await IncreaseMainProgressValueAsync(taskStepAmount);
				}
				else
				{
					await IncreaseMainProgressValueAsync(taskStepAmount);
				}

				await SetMainProgressTextAsync("Loading GM Campaigns...");
				var loadedGMCampaigns = await LoadGameMasterCampaignsAsync(taskStepAmount);
				await IncreaseMainProgressValueAsync(taskStepAmount);

				await SetMainProgressTextAsync("Loading external load orders...");
				var savedModOrderList = await RunTask(LoadExternalLoadOrdersAsync(), new List<DivinityLoadOrder>());
				await IncreaseMainProgressValueAsync(taskStepAmount);

				if (savedModOrderList.Count > 0)
				{
					DivinityApp.Log($"{savedModOrderList.Count} saved load orders found.");
				}
				else
				{
					DivinityApp.Log("No saved orders found.");
				}

				await SetMainProgressTextAsync("Setting up mod lists...");

				await Observable.Start(() => {
					LoadAppConfig();
					SetLoadedMods(loadedMods);
					SetLoadedGMCampaigns(loadedGMCampaigns);

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
							DivinityApp.Log($"Profile '{selectedProfileUUID}' not found {Profiles.Count}/{loadedProfiles.Count}.");
						}
					}
					else
					{
						SelectedProfileIndex = 0;
					}

					MainProgressWorkText = "Building mod order list...";

					if (lastActiveOrder != null && lastActiveOrder.Count > 0)
					{
						if (SelectedModOrder != null) SelectedModOrder.SetOrder(lastActiveOrder);
						BuildModOrderList(lastOrderIndex);
					}
					else
					{
						BuildModOrderList(0);
					}
					MainProgressValue += taskStepAmount;
					return Unit.Default;
				}, RxApp.MainThreadScheduler);

				await SetMainProgressTextAsync("Finishing up...");
				await IncreaseMainProgressValueAsync(taskStepAmount);
			}
			else
			{
				DivinityApp.Log($"[*ERROR*] Larian documents folder not found!");
			}

			await Observable.Start(() => {
				int defaultAdventureIndex = AdventureMods.IndexOf(AdventureMods.FirstOrDefault(x => x.UUID == DivinityApp.ORIGINS_UUID));
				if (defaultAdventureIndex == -1) defaultAdventureIndex = 0;
				if (lastAdventureMod != null && AdventureMods != null && AdventureMods.Count > 0)
				{
					DivinityApp.Log($"Setting selected adventure mod.");
					var nextAdventureMod = AdventureMods.FirstOrDefault(x => x.UUID == lastAdventureMod);
					if (nextAdventureMod != null)
					{
						SelectedAdventureModIndex = AdventureMods.IndexOf(nextAdventureMod);
						if(nextAdventureMod.UUID == DivinityApp.GAMEMASTER_UUID)
						{
							Settings.GameMasterModeEnabled = true;
						}
					}
					else
					{

						SelectedAdventureModIndex = defaultAdventureIndex;
					}
				}
				else
				{
					SelectedAdventureModIndex = defaultAdventureIndex;
				}

				DivinityApp.Log($"Finishing up refresh.");

				Refreshing = false;
				OnMainProgressComplete();
				OnRefreshed?.Invoke(this, new EventArgs());

				if (AppSettings.FeatureEnabled("ScriptExtender"))
				{
					if (this.IsInitialized)
					{
						DivinityApp.Log($"Loading extender settings.");
						LoadExtenderSettings();
					}
					else
					{
						DivinityApp.Log($"Checking extender data.");
						CheckExtenderData();
					}
				}

				return Unit.Default;
			}, RxApp.MainThreadScheduler);

			if (AppSettings.FeatureEnabled("Workshop"))
			{
				LoadWorkshopModDataBackground();
			}

			/*
			RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), () =>
			{
				OnFilterTextChanged(ActiveModFilterText, ActiveMods);
				OnFilterTextChanged(InactiveModFilterText, InactiveMods);
			});
			*/
#if DEBUG
			//DivinityApp.Log("Mods (" + mods.Count + ")");
			//DivinityApp.Log(String.Join("\n", mods.Items.Select(x => $"{x.UUID}|{x.Name}|{x.Type}")));
			//DivinityApp.Log("Ignored Mods (" + DivinityApp.IgnoredMods.Count + ")");
			//DivinityApp.Log(String.Join("\n", DivinityApp.IgnoredMods.Select(x => $"{x.UUID}|{x.Name}|{x.Type}")));
			//DivinityApp.Log("Adventure Mods (" + AdventureMods.Count + ")");
			//DivinityApp.Log(String.Join("\n", AdventureMods.Select(x => $"{x.UUID}|{x.Name}")));
#endif
			return Disposable.Empty;
		}

		public void RefreshAsync_Start(string title = "Refreshing...")
		{
			if (ModUpdatesViewData != null)
			{
				ModUpdatesViewData.Clear();
			}
			ModUpdatesViewVisible = ModUpdatesAvailable = false;
			MainProgressTitle = title;
			MainProgressValue = 0d;
			CanCancelProgress = false;
			MainProgressIsActive = true;
			Refreshing = true;
			mods.Clear();
			gameMasterCampaigns.Clear();
			userMods.Clear();
			Profiles.Clear();
			workshopMods.Clear();
			RxApp.TaskpoolScheduler.ScheduleAsync(RefreshAsync);
		}

		private async Task<List<DivinityLoadOrder>> LoadExternalLoadOrdersAsync()
		{
			try
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

				DivinityApp.Log($"Attempting to load saved load orders from '{loadOrderDirectory}'.");
				return await DivinityModDataLoader.FindLoadOrderFilesInDirectoryAsync(loadOrderDirectory);
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error loading external load orders: {ex.ToString()}.");
				return new List<DivinityLoadOrder>();
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
				try
				{
					if (SelectedModOrder.Name.Equals("Current"))
					{
						//When saving the "Current" order, write this to modsettings.lsx instead of a json file.
						result = await ExportLoadOrderAsync();
						outputPath = Path.Combine(SelectedProfile.Folder, "modsettings.lsx");
					}
					else
					{
						string outputName = DivinityModDataLoader.MakeSafeFilename(Path.Combine(SelectedModOrder.Name + ".json"), '_');

						// Renaming existing files
						if (!String.IsNullOrWhiteSpace(outputPath) && !String.IsNullOrWhiteSpace(SelectedModOrder.Name) && File.Exists(outputPath))
						{
							string baseName = Path.GetFileNameWithoutExtension(outputPath);
							if (baseName != SelectedModOrder.Name)
							{
								var lastPath = outputPath;
								outputPath = Path.Combine(outputDirectory, outputName);
								try
								{
									if (File.Exists(outputPath))
									{
										MessageBoxResult messageBoxResult = Xceed.Wpf.Toolkit.MessageBox.Show(view, $"Overwrite saved load order file with new name?{Environment.NewLine}Renaming {baseName}.json to {SelectedModOrder.Name}.json", "Confirm Overwrite",
											MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel, view.MainWindowMessageBox_OK.Style);
										if (messageBoxResult == MessageBoxResult.OK)
										{
											DivinityApp.Log($"Renaming load order file '{lastPath}' to '{outputPath}'.");
											File.Move(lastPath, outputPath);
										}
									}
									else
									{
										DivinityApp.Log($"Renaming load order file '{lastPath}' to '{outputPath}'.");
										File.Move(lastPath, outputPath);
									}
								}
								catch (Exception ex)
								{
									DivinityApp.Log($"Error renaming file:\n{ex.ToString()}");
								}
							}
						}

						// Save mods that aren't missing
						var tempOrder = new DivinityLoadOrder
						{
							Name = SelectedModOrder.Name,
						};
						tempOrder.Order.AddRange(SelectedModOrder.Order.Where(x => Mods.Any(y => y.UUID == x.UUID)));

						result = await DivinityModDataLoader.ExportLoadOrderToFileAsync(outputPath, tempOrder);
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
				// Save mods that aren't missing
				var tempOrder = new DivinityLoadOrder
				{
					Name = SelectedModOrder.Name,
				};
				tempOrder.Order.AddRange(SelectedModOrder.Order.Where(x => Mods.Any(y => y.UUID == x.UUID)));
				bool result = false;
				if (SelectedModOrder.Name.Equals("Current", StringComparison.OrdinalIgnoreCase))
				{
					tempOrder.Name = $"Current ({SelectedProfile.Name})";
					result = DivinityModDataLoader.ExportLoadOrderToFile(dialog.FileName, tempOrder);
				}
				else
				{
					result = DivinityModDataLoader.ExportLoadOrderToFile(dialog.FileName, tempOrder);
				}

				if (result)
				{
					view.AlertBar.SetSuccessAlert($"Saved mod load order to '{dialog.FileName}'", 10);
					foreach (var order in this.ModOrderList)
					{
						if (order.FilePath == dialog.FileName)
						{
							order.SetOrder(tempOrder);
							DivinityApp.Log($"Updated saved order '{order.Name}' from '{dialog.FileName}'.");
						}
					}
				}
				else
				{
					view.AlertBar.SetDangerAlert($"Failed to save mod load order to '{dialog.FileName}'");
				}
			}
		}

		private void DisplayMissingMods(DivinityLoadOrder order = null)
		{
			bool displayExtenderModWarning = false;

			if (order == null) order = SelectedModOrder;
			if (order != null && Settings?.DisableMissingModWarnings != true)
			{
				List<DivinityMissingModData> missingMods = new List<DivinityMissingModData>();

				for (int i = 0; i < order.Order.Count; i++)
				{
					var entry = order.Order[i];
					var mod = mods.Items.FirstOrDefault(m => m.UUID == entry.UUID);
					if (mod != null)
					{
						if (mod.Dependencies.Count > 0)
						{
							foreach (var dependency in mod.Dependencies.Items)
							{
								if (!DivinityModDataLoader.IgnoreMod(dependency.UUID) && !mods.Items.Any(x => x.UUID == dependency.UUID) &&
									!missingMods.Any(x => x.UUID == dependency.UUID))
								{
									var x = new DivinityMissingModData
									{
										Index = -1,
										Name = dependency.Name,
										UUID = dependency.UUID,
										Dependency = true
									};
									missingMods.Add(x);
								}
							}
						}
					}
					else if (!DivinityModDataLoader.IgnoreMod(entry.UUID))
					{
						var x = new DivinityMissingModData
						{
							Index = i,
							Name = entry.Name,
							UUID = entry.UUID
						};
						missingMods.Add(x);
						entry.Missing = true;
					}
				}

				if (missingMods.Count > 0)
				{
					view.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
					view.MainWindowMessageBox_OK.Closed += MainWindowMessageBox_Closed_ResetColor;
					view.MainWindowMessageBox_OK.ShowMessageBox(String.Join("\n", missingMods.OrderBy(x => x.Index)),
						"Missing Mods in Load Order", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
				}
				else
				{
					displayExtenderModWarning = true;
				}
			}
			else
			{
				displayExtenderModWarning = true;
			}

			if (AppSettings.FeatureEnabled("ScriptExtender"))
			{
				if (displayExtenderModWarning)
				{
					//DivinityApp.LogMessage($"Mod Order: {String.Join("\n", order.Order.Select(x => x.Name))}");
					DivinityApp.Log("Checking mods for extender requirements.");
					List<DivinityMissingModData> extenderRequiredMods = new List<DivinityMissingModData>();
					for (int i = 0; i < order.Order.Count; i++)
					{
						var entry = order.Order[i];
						var mod = ActiveMods.FirstOrDefault(m => m.UUID == entry.UUID);
						if (mod != null)
						{
							DivinityApp.Log($"{mod.Name} | ExtenderModStatus: {mod.ExtenderModStatus}");

							if (mod.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_DISABLED || mod.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_MISSING)
							{
								extenderRequiredMods.Add(new DivinityMissingModData
								{
									Index = mod.Index,
									Name = mod.DisplayName,
									UUID = mod.UUID,
									Dependency = false
								});

								if (mod.Dependencies.Count > 0)
								{
									foreach (var dependency in mod.Dependencies.Items)
									{
										var dependencyMod = mods.Items.FirstOrDefault(m => m.UUID == dependency.UUID);
										// Dependencies not in the order that require the extender
										if (dependencyMod.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_DISABLED || dependencyMod.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_MISSING)
										{
											extenderRequiredMods.Add(new DivinityMissingModData
											{
												Index = mod.Index - 1,
												Name = dependencyMod.DisplayName,
												UUID = dependencyMod.UUID,
												Dependency = true
											});
										}
									}
								}
							}
						}
					}

					if (extenderRequiredMods.Count > 0)
					{
						DivinityApp.Log("Displaying mods that require the extender.");
						view.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
						view.MainWindowMessageBox_OK.Closed += MainWindowMessageBox_Closed_ResetColor;
						view.MainWindowMessageBox_OK.ShowMessageBox(String.Join("\n", extenderRequiredMods.OrderBy(x => x.Index)),
							"Mods Require the Script Extender - Install it with the Tools menu!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
					}
				}
			}
		}

		private DivinityProfileActiveModData ProfileActiveModDataFromUUID(string uuid)
		{
			var modData = mods.Items.FirstOrDefault(x => x.UUID == uuid);
			if (modData != null)
			{
				modData.ToProfileModData();
			}
			return new DivinityProfileActiveModData()
			{
				UUID = uuid
			};
		}

		private void ExportLoadOrder()
		{
			RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
			{
				await ExportLoadOrderAsync();
				return Disposable.Empty;
			});
		}

		private async Task<bool> ExportLoadOrderAsync()
		{
			if(!Settings.GameMasterModeEnabled)
			{
				if (SelectedProfile != null && SelectedModOrder != null)
				{
					string outputPath = Path.Combine(SelectedProfile.Folder, "modsettings.lsx");
					var finalOrder = DivinityModDataLoader.BuildOutputList(SelectedModOrder.Order, mods.Items, Settings.AutoAddDependenciesWhenExporting, SelectedAdventureMod);
					var result = await DivinityModDataLoader.ExportModSettingsToFileAsync(SelectedProfile.Folder, finalOrder);

					if (result)
					{
						await Observable.Start(() => {
							ShowAlert($"Exported load order to '{outputPath}'", AlertType.Success, 15);

							if (DivinityModDataLoader.ExportedSelectedProfile(PathwayData.DocumentsProfilesPath, SelectedProfile.UUID))
							{
								DivinityApp.Log($"Set active profile to '{SelectedProfile.Name}'.");
							}
							else
							{
								DivinityApp.Log($"Could not set active profile to '{SelectedProfile.Name}'.");
							}

							//Update "Current" order
							if (SelectedModOrder.Name != "Current")
							{
								var currentOrder = this.ModOrderList.FirstOrDefault(x => x.Name == "Current");
								currentOrder.SetOrder(SelectedModOrder.Order);
							}

							List<string> orderList = new List<string>();
							if (SelectedAdventureMod != null) orderList.Add(SelectedAdventureMod.UUID);
							orderList.AddRange(SelectedModOrder.Order.Select(x => x.UUID));

							SelectedProfile.ModOrder.Clear();
							SelectedProfile.ModOrder.AddRange(orderList);
							SelectedProfile.ActiveMods.Clear();
							SelectedProfile.ActiveMods.AddRange(orderList.Select(x => ProfileActiveModDataFromUUID(x)));
							DisplayMissingMods(SelectedModOrder);

							return Unit.Default;
						}, RxApp.MainThreadScheduler);
						return true;
					}
					else
					{
						await Observable.Start(() => {
							string msg = $"Problem exporting load order to '{outputPath}'";
							ShowAlert(msg, AlertType.Danger);
							view.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
							view.MainWindowMessageBox_OK.Closed += MainWindowMessageBox_Closed_ResetColor;
							view.MainWindowMessageBox_OK.ShowMessageBox(msg, "Mod Order Export Failed", MessageBoxButton.OK);
							return Unit.Default;
						}, RxApp.MainThreadScheduler);
					}
				}
				else
				{
					await Observable.Start(() => {
						ShowAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order.", AlertType.Danger);
						return Unit.Default;
					}, RxApp.MainThreadScheduler);
				}
			}
			else
			{
				if(SelectedGameMasterCampaign != null)
				{
					var gmAdventureMod = mods.Items.FirstOrDefault(x => x.UUID == DivinityApp.GAMEMASTER_UUID);
					var finalOrder = DivinityModDataLoader.BuildOutputList(SelectedModOrder.Order, mods.Items, Settings.AutoAddDependenciesWhenExporting);
					if (SelectedGameMasterCampaign.Export(finalOrder))
					{
						// Need to still write to modsettings.lsx
						finalOrder.Insert(0, gmAdventureMod);
						await DivinityModDataLoader.ExportModSettingsToFileAsync(SelectedProfile.Folder, finalOrder);

						await Observable.Start(() => {
							ShowAlert($"Exported load order to '{SelectedGameMasterCampaign.FilePath}'", AlertType.Success, 15);

							if (DivinityModDataLoader.ExportedSelectedProfile(PathwayData.DocumentsProfilesPath, SelectedProfile.UUID))
							{
								DivinityApp.Log($"Set active profile to '{SelectedProfile.Name}'.");
							}
							else
							{
								DivinityApp.Log($"Could not set active profile to '{SelectedProfile.Name}'.");
							}

							//Update the campaign's saved dependencies
							SelectedGameMasterCampaign.Dependencies.Clear();
							SelectedGameMasterCampaign.Dependencies.AddRange(finalOrder.Select(x => DivinityModDependencyData.FromModData(x)));

							List<string> orderList = new List<string>();
							if (SelectedAdventureMod != null) orderList.Add(SelectedAdventureMod.UUID);
							orderList.AddRange(SelectedModOrder.Order.Select(x => x.UUID));

							SelectedProfile.ModOrder.Clear();
							SelectedProfile.ModOrder.AddRange(orderList);
							SelectedProfile.ActiveMods.Clear();
							SelectedProfile.ActiveMods.AddRange(orderList.Select(x => ProfileActiveModDataFromUUID(x)));
							DisplayMissingMods(SelectedModOrder);

							return Unit.Default;
						}, RxApp.MainThreadScheduler);
						return true;
					}
					else
					{
						await Observable.Start(() => {
							string msg = $"Problem exporting load order to '{SelectedGameMasterCampaign.FilePath}'";
							ShowAlert(msg, AlertType.Danger);
							view.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
							view.MainWindowMessageBox_OK.Closed += MainWindowMessageBox_Closed_ResetColor;
							view.MainWindowMessageBox_OK.ShowMessageBox(msg, "Mod Order Export Failed", MessageBoxButton.OK);
							return Unit.Default;
						}, RxApp.MainThreadScheduler);
					}
				}
				else
				{
					await Observable.Start(() => {
						ShowAlert("SelectedGameMasterCampaign is null! Failed to export mod order.", AlertType.Danger);
						return Unit.Default;
					}, RxApp.MainThreadScheduler);
				}
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
			DivinityApp.Log($"Main progress is complete.");
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
			});
		}

		private void ExportLoadOrderToArchive_Start()
		{
			//view.MainWindowMessageBox.Text = "Add active mods to a zip file?";
			//view.MainWindowMessageBox.Caption = "Depending on the number of mods, this may take some time.";
			MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(view, $"Save active mods to a zip file?{Environment.NewLine}Depending on the number of mods, this may take some time.", "Confirm Archive Creation",
				MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel, view.MainWindowMessageBox_OK.Style);
			if (result == MessageBoxResult.OK)
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

								DivinityApp.Log($"Creating package for editor mod '{mod.Name}' - '{outputPackage}'.");

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
						DivinityApp.Log(msg);
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

		private void ExportLoadOrderToTextFileAs()
		{
			if (SelectedProfile != null && SelectedModOrder != null)
			{
				var startDirectory = Path.GetFullPath(Directory.GetCurrentDirectory());

				var dialog = new SaveFileDialog();
				dialog.AddExtension = true;
				dialog.DefaultExt = ".txt";
				dialog.Filter = "Text file (*.txt)|*.txt|TSV file (*.tsv)|*.tsv|JSON file (*.json)|*.json";
				dialog.InitialDirectory = startDirectory;

				string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
				string baseOrderName = SelectedModOrder.Name;
				if (SelectedModOrder.Name.Equals("Current", StringComparison.OrdinalIgnoreCase))
				{
					baseOrderName = $"{SelectedProfile.Name}_{SelectedModOrder.Name}";
				}
				string outputName = $"{baseOrderName}-{ DateTime.Now.ToString(sysFormat + "_HH-mm-ss")}.txt";

				//dialog.RestoreDirectory = true;
				dialog.FileName = DivinityModDataLoader.MakeSafeFilename(outputName, '_');
				dialog.CheckFileExists = false;
				dialog.CheckPathExists = false;
				dialog.OverwritePrompt = true;
				dialog.Title = "Export Load Order As Text File...";

				if (dialog.ShowDialog(view) == true)
				{
					var fileType = Path.GetExtension(dialog.FileName);
					string outputText = "";
					if (fileType.Equals(".json", StringComparison.OrdinalIgnoreCase))
					{
						outputText = JsonConvert.SerializeObject(ActiveMods, Formatting.Indented, new JsonSerializerSettings
						{
							NullValueHandling = NullValueHandling.Ignore
						});
					}
					else if (fileType.Equals(".tsv", StringComparison.OrdinalIgnoreCase))
					{
						if (WorkshopSupportEnabled)
						{
							outputText = "Index\tName\tAuthor\tFileName\tTags\tDependencies\tURL\n";
							outputText += String.Join("\n", ActiveMods.Select(x => $"{x.Index}\t{x.Name}\t{x.Author}\t{x.OutputPakName}\t{String.Join(", ", x.Tags)}\t{String.Join(", ", x.Dependencies.Items.Select(y => y.Name))}\t{x.GetURL()}"));
						}
						else
						{
							outputText = "Index\tName\tAuthor\tFileName\tTags\tDependencies\n";
							outputText += String.Join("\n", ActiveMods.Select(x => $"{x.Index}\t{x.Name}\t{x.Author}\t{x.OutputPakName}\t{String.Join(", ", x.Tags)}\t{String.Join(", ", x.Dependencies.Items.Select(y => y.Name))}"));
						}
					}
					else
					{
						//Text file format
						outputText = String.Join("\n", ActiveMods.Select(x => $"{x.Index}. {x.Name} ({x.OutputPakName}) {x.GetURL()}"));
					}
					try
					{
						File.WriteAllText(dialog.FileName, outputText);
						view.AlertBar.SetSuccessAlert($"Exported order to '{dialog.FileName}'", 20);
					}
					catch (Exception ex)
					{
						view.AlertBar.SetDangerAlert($"Error exporting mod order to '{dialog.FileName}':\n{ex.ToString()}");
					}
				}
			}
			else
			{
				DivinityApp.Log($"SelectedProfile({SelectedProfile}) SelectedModOrder({SelectedModOrder})");
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
					string profilePath = Path.GetFullPath(Path.Combine(SelectedProfile.Folder, "Savegames"));
					string storyPath = Path.Combine(profilePath, "Story");
					if (Directory.Exists(storyPath))
					{
						dialog.InitialDirectory = storyPath;
					}
					else
					{
						dialog.InitialDirectory = profilePath;
					}
				}
				else
				{
					dialog.InitialDirectory = Path.GetFullPath(PathwayData.LarianDocumentsFolder);
				}
			}

			if (dialog.ShowDialog(view) == true)
			{
				PathwayData.LastSaveFilePath = Path.GetDirectoryName(dialog.FileName);
				DivinityApp.Log($"Loading order from '{dialog.FileName}'.");
				var newOrder = DivinityModDataLoader.GetLoadOrderFromSave(dialog.FileName);
				if (newOrder != null)
				{
					DivinityApp.Log($"Imported mod order: {String.Join(@"\n\t", newOrder.Order.Select(x => x.Name))}");
					return newOrder;
				}
				else
				{
					DivinityApp.Log($"Failed to load order from '{dialog.FileName}'.");
					ShowAlert($"No mod order found in save \"{Path.GetFileNameWithoutExtension(dialog.FileName)}\".", AlertType.Danger, 30);
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
					if (LoadModOrder(SelectedModOrder))
					{
						DivinityApp.Log($"Successfully re-loaded order {SelectedModOrder.Name} with save order.");
					}
					else
					{
						DivinityApp.Log($"Failed to load order {SelectedModOrder.Name}.");
					}
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
				DivinityApp.Log($"Loading order from '{dialog.FileName}'.");
				var newOrder = DivinityModDataLoader.LoadOrderFromFile(dialog.FileName);
				if (newOrder != null)
				{
					DivinityApp.Log($"Imported mod order: {String.Join(@"\n\t", newOrder.Order.Select(x => x.Name))}");
					AddNewModOrder(newOrder);
				}
				else
				{
					DivinityApp.Log($"Failed to load order from '{dialog.FileName}'.");
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
					DivinityApp.Log($"Renaming '{dialog.FileName}' to '{renameDialog.FileName}'.");

					if (DivinitySaveTools.RenameSave(dialog.FileName, renameDialog.FileName))
					{
						//DivinityApp.LogMessage($"Successfully renamed '{dialog.FileName}' to '{renameDialog.FileName}'.");

						try
						{
							string previewImage = Path.Combine(rootFolder, rootFileName + ".png");
							string renamedImage = Path.Combine(rootFolder, Path.GetFileNameWithoutExtension(renameDialog.FileName) + ".png");
							if (File.Exists(previewImage))
							{
								File.Move(previewImage, renamedImage);
								DivinityApp.Log($"Renamed save screenshot '{previewImage}' to '{renamedImage}'.");
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
										DivinityApp.Log($"Renamed save folder '{originalDirectory}' to '{desiredDirectory}'.");
									}
								}
							}

							view.AlertBar.SetSuccessAlert($"Successfully renamed '{dialog.FileName}' to '{renameDialog.FileName}'.", 15);
						}
						catch (Exception ex)
						{
							DivinityApp.Log($"Failed to rename '{dialog.FileName}' to '{renameDialog.FileName}':\n" + ex.ToString());
						}
					}
					else
					{
						DivinityApp.Log($"Failed to rename '{dialog.FileName}' to '{renameDialog.FileName}'.");
					}
				}
			}
		}

		public void CheckForUpdates(bool force = false)
		{
			if (!force)
			{
				if (Settings.LastUpdateCheck == -1 || (DateTimeOffset.Now.ToUnixTimeSeconds() - Settings.LastUpdateCheck >= 43200))
				{
					try
					{
						AutoUpdater.Start(DivinityApp.URL_UPDATE);
					}
					catch (Exception ex)
					{
						DivinityApp.Log($"Error running AutoUpdater:\n{ex.ToString()}");
					}
				}
			}
			else
			{
				AutoUpdater.ReportErrors = true;
				AutoUpdater.Start(DivinityApp.URL_UPDATE);
				Settings.LastUpdateCheck = DateTimeOffset.Now.ToUnixTimeSeconds();
				SaveSettings();
				Task.Delay(1000).ContinueWith(_ =>
				{
					AutoUpdater.ReportErrors = false;
				});
			}
		}

		public void OnViewActivated(MainWindow parentView)
		{
			view = parentView;
			DivinityApp.Commands.SetViewModel(this);

			if (DebugMode)
			{
				string lastMessage = "";
				this.WhenAnyValue(x => x.MainProgressWorkText, x => x.MainProgressValue).Subscribe((ob) =>
				{
					if(lastMessage != ob.Item1)
					{
						DivinityApp.Log($"Loading({ob.Item2 * 100}%): {ob.Item1}");
						lastMessage = ob.Item1;
					}
					else
					{
						DivinityApp.Log($"Loading({ob.Item2 * 100}%)");
					}
				});
			}

			LoadSettings();
			Keys.LoadKeybindings(this);
			if (Settings.CheckForUpdates)
			{
				CheckForUpdates();
			}
			RefreshAsync_Start("Loading...");
			//Refresh();
			SaveSettings(); // New values
			IsInitialized = true;
		}

		public bool AutoChangedOrder { get; set; } = false;
		public ViewModelActivator Activator { get; }

		private Regex filterPropertyPattern = new Regex("@([^\\s]+?)([\\s]+)([^@\\s]*)");
		private Regex filterPropertyPatternWithQuotes = new Regex("@([^\\s]+?)([\\s\"]+)([^@\"]*)");

		private int totalActiveModsHidden = 0;

		public int TotalActiveModsHidden
		{
			get => totalActiveModsHidden;
			set { this.RaiseAndSetIfChanged(ref totalActiveModsHidden, value); }
		}

		private int totalInactiveModsHidden = 0;

		public int TotalInactiveModsHidden
		{
			get => totalInactiveModsHidden;
			set { this.RaiseAndSetIfChanged(ref totalInactiveModsHidden, value); }
		}

		public void OnFilterTextChanged(string searchText, IEnumerable<DivinityModData> modDataList)
		{
			int totalHidden = 0;
			//DivinityApp.LogMessage("Filtering mod list with search term " + searchText);
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
							mod.IsSelected = false;
							totalHidden += 1;
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
							m.IsSelected = false;
							totalHidden += 1;
						}
					}
				}
			}

			if (modDataList == ActiveMods)
			{
				TotalActiveModsHidden = totalHidden;
			}
			else if (modDataList == InactiveMods)
			{
				TotalInactiveModsHidden = totalHidden;
			}
		}

		private MainWindowExceptionHandler exceptionHandler;

		public void ShowAlert(string message, AlertType alertType = AlertType.Info, int timeout = 0)
		{
			if (timeout < 0) timeout = 0;
			switch (alertType)
			{
				case AlertType.Danger:
					view.AlertBar.SetDangerAlert(message, timeout);
					break;
				case AlertType.Warning:
					view.AlertBar.SetWarningAlert(message, timeout);
					break;
				case AlertType.Success:
					view.AlertBar.SetSuccessAlert(message, timeout);
					break;
				case AlertType.Info:
				default:
					view.AlertBar.SetInformationAlert(message, timeout);
					break;
			}
		}

		private Unit DeleteOrder(DivinityLoadOrder order)
		{
			MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(view, $"Delete load order '{order.Name}'? This cannot be undone.", "Confirm Order Deletion",
				MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, view.MainWindowMessageBox_OK.Style);
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
				dialog.SelectedPath = Settings.LastExtractOutputPath + "\\";
			}
			else if (PathwayData.LastSaveFilePath.IsExistingDirectory())
			{
				dialog.SelectedPath = PathwayData.LastSaveFilePath + "\\";
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
				DivinityApp.Log($"Extracting selected mods to '{outputDirectory}'.");

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
							DivinityApp.Log($"Error extracting package: {ex.ToString()}");
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
							Process.Start(dialog.SelectedPath);
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
				MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, view.MainWindowMessageBox_OK.Style);
				if (result == MessageBoxResult.Yes)
				{
					ExtractSelectedMods_ChooseFolder();
				}
			}
		}

		private void ExportOrderToListAs()
		{
			if (ActiveMods.Count > 0)
			{
				var startDirectory = Path.GetFullPath(!String.IsNullOrEmpty(Settings.LoadOrderPath) ? Settings.LoadOrderPath : Directory.GetCurrentDirectory());

				if (!Directory.Exists(startDirectory))
				{
					Directory.CreateDirectory(startDirectory);
				}

				var dialog = new SaveFileDialog();
				dialog.AddExtension = true;
				dialog.DefaultExt = ".txt";
				dialog.Filter = "Text file (*.txt)|*.txt";
				dialog.InitialDirectory = startDirectory;

				string outputName = Path.Combine(SelectedModOrder.Name + ".txt");
				if (SelectedModOrder.Name.Equals("Current", StringComparison.OrdinalIgnoreCase))
				{
					outputName = $"{SelectedProfile.Name}_{SelectedModOrder.Name}.txt";
				}

				//dialog.RestoreDirectory = true;
				dialog.FileName = DivinityModDataLoader.MakeSafeFilename(outputName, '_');
				dialog.CheckFileExists = false;
				dialog.CheckPathExists = false;
				dialog.OverwritePrompt = true;
				dialog.Title = "Export Load Order List As...";

				if (dialog.ShowDialog(view) == true)
				{
					try
					{
						string text = "";
						for (int i = 0; i < ActiveMods.Count; i++)
						{
							var mod = ActiveMods[i];
							text += $"{mod.Index + 1}. {mod.DisplayName}";
							if (i < ActiveMods.Count - 1) text += Environment.NewLine;
						}
						File.WriteAllText(dialog.FileName, text);
						DivinityApp.Commands.OpenInFileExplorer(dialog.FileName);
						view.AlertBar.SetSuccessAlert($"Saved mod load order to '{dialog.FileName}'", 10);
					}
					catch (Exception ex)
					{
						view.AlertBar.SetDangerAlert($"Failed to save mod load order to '{dialog.FileName}'", 20);
					}
				}
			}
			else
			{
				view.AlertBar.SetWarningAlert("Current order is empty.", 10);
			}
		}

		private void InstallOsiExtender_DownloadStart(string exeDir)
		{
			double taskStepAmount = 1.0 / 3;
			MainProgressTitle = $"Setting up the Osiris Extender...";
			MainProgressValue = 0d;
			MainProgressToken = new CancellationTokenSource();
			CanCancelProgress = true;
			MainProgressIsActive = true;

			string dllDestination = Path.Combine(exeDir, "DXGI.dll");

			RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
			{
				int successes = 0;
				Stream webStream = null;
				Stream unzippedEntryStream = null;
				try
				{
					RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = $"Downloading {PathwayData.OsirisExtenderLatestReleaseUrl}...");
					webStream = await WebHelper.DownloadFileAsStreamAsync(PathwayData.OsirisExtenderLatestReleaseUrl, MainProgressToken.Token);
					if (webStream != null)
					{
						successes += 1;
						IncreaseMainProgressValue(taskStepAmount);
						RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = $"Extracting zip to {exeDir}...");
						ZipArchive archive = new ZipArchive(webStream);
						foreach (ZipArchiveEntry entry in archive.Entries)
						{
							if (MainProgressToken.IsCancellationRequested) break;
							if (entry.Name.Equals("DXGI.dll", StringComparison.OrdinalIgnoreCase))
							{
								unzippedEntryStream = entry.Open(); // .Open will return a stream
								using (var fs = File.Create(dllDestination, 4096, FileOptions.Asynchronous))
								{
									await unzippedEntryStream.CopyToAsync(fs, 4096, MainProgressToken.Token);
									successes += 1;
								}
								break;
							}
						}
						IncreaseMainProgressValue(taskStepAmount);
					}
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error extracting package: {ex.ToString()}");
				}
				finally
				{
					RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = $"Cleaning up...");
					if (webStream != null) webStream.Close();
					if (unzippedEntryStream != null) unzippedEntryStream.Close();
					successes += 1;
					IncreaseMainProgressValue(taskStepAmount);
				}
				await ctrl.Yield();
				RxApp.MainThreadScheduler.Schedule(_ => OnMainProgressComplete());

				RxApp.MainThreadScheduler.Schedule(() =>
				{
					if (successes >= 3)
					{
						view.AlertBar.SetSuccessAlert($"Successfully installed the Osiris Extender DXGI.dll to '{exeDir}'.", 20);
						HighlightExtenderDownload = false;
						Settings.ExtenderSettings.ExtenderUpdaterIsAvailable = true;
						if (Settings.ExtenderSettings.ExtenderVersion <= -1)
						{
							if (!String.IsNullOrWhiteSpace(PathwayData.OsirisExtenderLatestReleaseVersion))
							{
								var re = new Regex("v([0-9]+)");
								var m = re.Match(PathwayData.OsirisExtenderLatestReleaseVersion);
								if (m.Success)
								{
									if (int.TryParse(m.Groups[1].Value, out int version))
									{
										Settings.ExtenderSettings.ExtenderVersion = version;
										DivinityApp.Log($"Set extender version to v{version},");
									}
								}
							}
							else if (PathwayData.OsirisExtenderLatestReleaseUrl.Contains("v"))
							{
								var re = new Regex("v([0-9]+).*.zip");
								var m = re.Match(PathwayData.OsirisExtenderLatestReleaseUrl);
								if (m.Success)
								{
									if (int.TryParse(m.Groups[1].Value, out int version))
									{
										Settings.ExtenderSettings.ExtenderVersion = version;
										DivinityApp.Log($"Set extender version to v{version},");
									}
								}
							}
						}
						CheckExtenderData();
					}
					else
					{
						view.AlertBar.SetDangerAlert($"Error occurred when installing the Osiris Extender DXGI.dll. Check the log.", 30);
					}
				});

				return Disposable.Empty;
			});
		}

		private void InstallOsiExtender_Start()
		{
			if (!OpenRepoLinkToDownload)
			{
				string exeDir = Path.GetDirectoryName(Settings.GameExecutablePath);
				string messageText = String.Format(@"Download and install the Osiris Extender?
The Osiris Extender is used by various mods to extend the scripting language of the game, allowing new functionality.
The extenders needs to only be installed once, as it can auto-update itself automatically when you launch the game.
Download url: 
{0}
Directory the zip will be extracted to:
{1}", PathwayData.OsirisExtenderLatestReleaseUrl, exeDir);

				MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(view, messageText, "Download & Install the Osiris Extender?",
					MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, view.MainWindowMessageBox_OK.Style);
				if (result == MessageBoxResult.Yes)
				{
					InstallOsiExtender_DownloadStart(exeDir);
				}
			}
			else
			{
				DivinityApp.Log($"Getting a release download link failed for some reason. Opening repo url: https://github.com/Norbyte/ositools/releases/latest");
				Process.Start("https://github.com/Norbyte/ositools/releases/latest");
			}
		}

		private int SortModOrder(DivinityLoadOrderEntry a, DivinityLoadOrderEntry b)
		{
			if (a != null && b != null)
			{
				var moda = mods.Items.FirstOrDefault(x => x.UUID == a.UUID);
				var modb = mods.Items.FirstOrDefault(x => x.UUID == b.UUID);
				if (moda != null && modb != null)
				{
					return moda.Index.CompareTo(modb.Index);
				}
				else if (moda != null)
				{
					return 1;
				}
				else if (modb != null)
				{
					return -1;
				}
			}
			else if (a != null)
			{
				return 1;
			}
			else if (b != null)
			{
				return -1;
			}
			return 0;
		}

		private string LastRenamingOrderName { get; set; } = "";

		public void StopRenaming(bool cancel = false)
		{
			if (IsRenamingOrder)
			{
				if (!cancel)
				{
					LastRenamingOrderName = "";
				}
				else if (!String.IsNullOrEmpty(LastRenamingOrderName))
				{
					SelectedModOrder.Name = LastRenamingOrderName;
					LastRenamingOrderName = "";
				}
				IsRenamingOrder = false;
			}
		}

		private async Task<Unit> ToggleRenamingLoadOrder(object control)
		{
			IsRenamingOrder = !IsRenamingOrder;

			if (IsRenamingOrder)
			{
				LastRenamingOrderName = SelectedModOrder.Name;
			}

			await Task.Delay(50);
			RxApp.MainThreadScheduler.Schedule(() =>
			{
				if (control is ComboBox comboBox)
				{
					var tb = comboBox.FindVisualChildren<TextBox>().FirstOrDefault();
					if (tb != null)
					{
						tb.Focus();
						if (IsRenamingOrder)
						{
							tb.SelectAll();
						}
						else
						{
							tb.Select(0, 0);
						}
					}
				}
				else if (control is TextBox tb)
				{
					if (IsRenamingOrder)
					{
						tb.SelectAll();

					}
					else
					{
						tb.Select(0, 0);
					}
				}
				else
				{
					DivinityApp.Log("Can't find OrdersComboBox!");
				}
			});
			return Unit.Default;
		}

		public void ClearMissingMods()
		{
			//if (SelectedProfile.SavedLoadOrder == null)
			//{
			//	foreach (var uuid in SelectedProfile.ModOrder.ToList())
			//	{
			//		var activeModData = SelectedProfile.ActiveMods.FirstOrDefault(y => y.UUID == uuid);
			//		if (activeModData != null)
			//		{
			//			var mod = mods.Items.FirstOrDefault(m => m.UUID.Equals(uuid, StringComparison.OrdinalIgnoreCase));
			//			if (mod == null)
			//			{
			//				SelectedProfile.ActiveMods.Remove(activeModData);
			//			}
			//		}
			//	}
			//}

			int totalRemoved = 0;

			if (SelectedModOrder != null)
			{
				foreach (var entry in SelectedModOrder.Order.ToList())
				{
					var mod = mods.Items.FirstOrDefault(m => m.UUID == entry.UUID);
					if (mod == null)
					{
						SelectedModOrder.Order.Remove(entry);
						totalRemoved++;
					}
				}
			}

			if (totalRemoved > 0)
			{
				ShowAlert($"Removed {totalRemoved} missing mods from the current order. Save to confirm.", AlertType.Warning);
			}
		}

		private void LoadAppConfig()
		{
			if (File.Exists(DivinityApp.PATH_APP_FEATURES))
			{
				var appFeaturesDict = DivinityJsonUtils.SafeDeserializeFromPath<Dictionary<string, bool>>(DivinityApp.PATH_APP_FEATURES);
				if (appFeaturesDict != null)
				{
					foreach (var kvp in appFeaturesDict)
					{
						try
						{
							if (!String.IsNullOrEmpty(kvp.Key))
							{
								AppSettings.Features[kvp.Key.ToLower()] = kvp.Value;
							}
						}
						catch (Exception ex)
						{
							DivinityApp.Log("Error setting feature key:");
							DivinityApp.Log(ex.ToString());
						}
					}
				}
			}

			if (File.Exists(DivinityApp.PATH_DEFAULT_PATHWAYS))
			{
				AppSettings.DefaultPathways = DivinityJsonUtils.SafeDeserializeFromPath<DefaultPathwayData>(DivinityApp.PATH_DEFAULT_PATHWAYS);
			}

			if (File.Exists(DivinityApp.PATH_IGNORED_MODS))
			{
				ignoredModsData = DivinityJsonUtils.SafeDeserializeFromPath<IgnoredModsData>(DivinityApp.PATH_IGNORED_MODS);
				if (ignoredModsData != null)
				{
					DivinityApp.IgnoredMods.Clear();
					foreach (var dict in ignoredModsData.Mods)
					{
						var mod = new DivinityModData(true);
						if (dict.TryGetValue("UUID", out var uuid))
						{
							mod.UUID = (string)uuid;

							if (dict.TryGetValue("Name", out var name))
							{
								mod.Name = (string)name;
							}
							if (dict.TryGetValue("Description", out var desc))
							{
								mod.Description = (string)desc;
							}
							if (dict.TryGetValue("Folder", out var folder))
							{
								mod.Folder = (string)folder;
							}
							if (dict.TryGetValue("Type", out var modType))
							{
								mod.Type = (string)modType;
							}
							if (dict.TryGetValue("Author", out var author))
							{
								mod.Author = (string)author;
							}
							if (dict.TryGetValue("Targets", out var targets))
							{
								string tstr = (string)targets;
								if (!String.IsNullOrEmpty(tstr))
								{
									mod.Modes.Clear();
									var strTargets = tstr.Split(';');
									foreach (var t in strTargets)
									{
										mod.Modes.Add(t);
									}
								}
							}
							if (dict.TryGetValue("Version", out var vObj))
							{
								int version;
								if (vObj is string vStr)
								{
									version = int.Parse(vStr);
								}
								else
								{
									version = Convert.ToInt32(vObj);
								}
								mod.Version = new DivinityModVersion(version);
							}
							if (dict.TryGetValue("Tags", out var tags))
							{
								if(tags is string tagsText && !String.IsNullOrWhiteSpace(tagsText))
								{
									mod.AddTags(tagsText.Split(';'));
								}
							}
							DivinityApp.IgnoredMods.Add(mod);
							//DivinityApp.LogMessage($"Ignored mod added: Name({mod.Name}) UUID({mod.UUID}) Type({mod.Type})");
						}
					}

					foreach (var uuid in ignoredModsData.IgnoreDependencies)
					{
						var mod = DivinityApp.IgnoredMods.FirstOrDefault(x => x.UUID.ToLower() == uuid.ToLower());
						if (mod != null)
						{
							DivinityApp.IgnoredDependencyMods.Add(mod);
						}
					}

					//DivinityApp.LogMessage("Ignored mods:\n" + String.Join("\n", DivinityApp.IgnoredMods.Select(x => x.Name)));
				}
			}
		}

		public MainWindowViewModel() : base()
		{
			exceptionHandler = new MainWindowExceptionHandler(this);
			RxApp.DefaultExceptionHandler = exceptionHandler;

			this.ModUpdatesViewData = new ModUpdatesViewData(this);

			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			var productName = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute), false)).Product;
			Version = assembly.GetName().Version.ToString();
			Title = $"{productName} {this.Version}";

			this.DropHandler = new ModListDropHandler(this);
			this.DragHandler = new ModListDragHandler(this);

			Activator = new ViewModelActivator();

			DivinityApp.DependencyFilter = this.WhenAnyValue(x => x.Settings.DebugModeEnabled).Select(MakeDependencyFilter);

			this.WhenActivated((CompositeDisposable disposables) =>
			{
				if (!disposables.Contains(this.Disposables)) disposables.Add(this.Disposables);
			});

			#region Keys Setup
			Keys.SaveDefaultKeybindings();

			var canExecuteSaveCommand = this.WhenAnyValue(x => x.CanSaveOrder, (canSave) => canSave == true);
			Keys.Save.AddAction(SaveLoadOrder, canExecuteSaveCommand);

			var canExecuteSaveAsCommand = this.WhenAnyValue(x => x.CanSaveOrder, x => x.MainProgressIsActive, (canSave, p) => canSave && !p);
			Keys.SaveAs.AddAction(SaveLoadOrderAs, canExecuteSaveAsCommand);
			Keys.NewOrder.AddAction(() => AddNewModOrder());
			Keys.ExportOrderToGame.AddAction(ExportLoadOrder);

			var canRefreshObservable = this.WhenAnyValue(x => x.Refreshing, (r) => r == false).StartWith(true);
			Keys.Refresh.AddAction(() => RefreshAsync_Start(), canRefreshObservable);

			IObservable<bool> canStartExport = this.WhenAny(x => x.MainProgressToken, (t) => t != null).StartWith(false);
			Keys.ExportOrderToZip.AddAction(ExportLoadOrderToArchive_Start, canStartExport);
			Keys.ExportOrderToArchiveAs.AddAction(ExportLoadOrderToArchiveAs, canStartExport);

			var anyActiveObservable = this.WhenAnyValue(x => x.ActiveMods.Count, (c) => c > 0);
			Keys.ExportOrderToList.AddAction(ExportLoadOrderToTextFileAs, anyActiveObservable);

			canOpenDialogWindow = this.WhenAnyValue(x => x.MainProgressIsActive, (b) => !b);
			Keys.ImportOrderFromSave.AddAction(ImportOrderFromSaveToCurrent, canOpenDialogWindow);
			Keys.ImportOrderFromSaveAsNew.AddAction(ImportOrderFromSaveAsNew, canOpenDialogWindow);
			Keys.ImportOrderFromFile.AddAction(ImportOrderFromFile, canOpenDialogWindow);
			Keys.ImportOrderFromZipFile.AddAction(ImportOrderZipFile, canOpenDialogWindow);


			Keys.OpenDonationLink.AddAction(() =>
			{
				Process.Start(DivinityApp.URL_DONATION);
			});

			Keys.OpenRepositoryPage.AddAction(() =>
			{
				Process.Start(DivinityApp.URL_REPO);
			});

			Keys.ToggleViewTheme.AddAction(() =>
			{
				if (Settings != null)
				{
					Settings.DarkThemeEnabled = !Settings.DarkThemeEnabled;
				}
			});

			Keys.ToggleFileNameDisplay.AddAction(() =>
			{
				if (Settings != null)
				{
					Settings.DisplayFileNames = !Settings.DisplayFileNames;

					foreach (var m in Mods)
					{
						m.DisplayFileForName = Settings.DisplayFileNames;
					}
				}
				else
				{
					foreach (var m in Mods)
					{
						m.DisplayFileForName = !m.DisplayFileForName;
					}
				}
			});

			#endregion

			DeleteOrderCommand = ReactiveCommand.Create<DivinityLoadOrder, Unit>(DeleteOrder, canOpenDialogWindow);

			var canToggleUpdatesView = this.WhenAnyValue(x => x.ModUpdatesViewVisible, x => x.ModUpdatesAvailable, (isVisible, hasUpdates) => isVisible || hasUpdates);
			void toggleUpdatesView(){
				ModUpdatesViewVisible = !ModUpdatesViewVisible;
			};
			Keys.ToggleUpdatesView.AddAction(toggleUpdatesView, canToggleUpdatesView);
			ToggleUpdatesViewCommand = ReactiveCommand.Create(toggleUpdatesView, canToggleUpdatesView);

			IObservable<bool> canCancelProgress = this.WhenAnyValue(x => x.CanCancelProgress).StartWith(true);
			CancelMainProgressCommand = ReactiveCommand.Create(() =>
			{
				if (MainProgressToken != null && MainProgressToken.Token.CanBeCanceled)
				{
					MainProgressToken.Token.Register(() => { MainProgressIsActive = false; });
					MainProgressToken.Cancel();
				}
			}, canCancelProgress);


			CopyPathToClipboardCommand = ReactiveCommand.Create((string path) =>
			{
				if (!String.IsNullOrWhiteSpace(path))
				{
					Clipboard.SetText(path);
					ShowAlert($"Copied '{path}' to clipboard.", 0, 10);
				}
				else
				{
					ShowAlert($"Path not found.", AlertType.Danger, 30);
				}
			});

			var canOpenModsFolder = this.WhenAnyValue(x => x.PathwayData.DocumentsModsPath, (p) => !String.IsNullOrEmpty(p) && Directory.Exists(p));
			Keys.OpenModsFolder.AddAction(() =>
			{
				Process.Start(PathwayData.DocumentsModsPath);
			}, canOpenModsFolder);

			RenameSaveCommand = ReactiveCommand.Create(RenameSave_Start, canOpenDialogWindow);

			CopyOrderToClipboardCommand = ReactiveCommand.Create(() =>
			{
				try
				{
					if (ActiveMods.Count > 0)
					{
						string text = "";
						for (int i = 0; i < ActiveMods.Count; i++)
						{
							var mod = ActiveMods[i];
							text += $"{mod.Index}. {mod.DisplayName}";
							if (i < ActiveMods.Count - 1) text += Environment.NewLine;
						}
						Clipboard.SetText(text);
						view.AlertBar.SetInformationAlert("Copied mod order to clipboard.", 10);
					}
					else
					{
						view.AlertBar.SetWarningAlert("Current order is empty.", 10);
					}
				}
				catch (Exception ex)
				{
					view.AlertBar.SetDangerAlert($"Error copying order to clipboard: {ex}", 15);
				}
			});

			var profileChanged = this.WhenAnyValue(x => x.SelectedProfileIndex, x => x.Profiles.Count).Select(x => Profiles.ElementAtOrDefault(x.Item1));
			selectedprofile = profileChanged.ToProperty(this, nameof(SelectedProfile)).DisposeWith(this.Disposables);

			profileChanged.Subscribe((profile) =>
			{
				if (profile != null && profile.ActiveMods != null && profile.ActiveMods.Count > 0)
				{
					var adventureModData = AdventureMods.FirstOrDefault(x => profile.ActiveMods.Any(y => y.UUID == x.UUID));
					if (adventureModData != null)
					{
						var nextAdventure = AdventureMods.IndexOf(adventureModData);
						DivinityApp.Log($"Found adventure mod in profile: {adventureModData.Name} | {nextAdventure}");
						if (nextAdventure > -1)
						{
							SelectedAdventureModIndex = nextAdventure;
						}
					}
				}
			});

			selectedModOrder = this.WhenAnyValue(x => x.SelectedModOrderIndex, x => x.ModOrderList.Count).
				Select(x => ModOrderList.ElementAtOrDefault(x.Item1)).ToProperty(this, nameof(SelectedModOrder));
			isBaseLoadOrder = this.WhenAnyValue(x => x.SelectedModOrder).Select(x => x != null && x.Name == "Current").ToProperty(this, nameof(IsBaseLoadOrder));

			//Throttle in case the index changes quickly in a short timespan
			this.WhenAnyValue(vm => vm.SelectedModOrderIndex).ObserveOn(RxApp.MainThreadScheduler).Subscribe((_) => {
				if (!this.Refreshing && SelectedModOrderIndex > -1)
				{
					if (SelectedModOrder != null && !LoadingOrder)
					{
						if (!SelectedModOrder.OrderEquals(ActiveMods.Select(x => x.UUID)))
						{
							if (LoadModOrder(SelectedModOrder))
							{
								DivinityApp.Log($"Successfully loaded order {SelectedModOrder.Name}.");
							}
							else
							{
								DivinityApp.Log($"Failed to load order {SelectedModOrder.Name}.");
							}
						}
						else
						{
							DivinityApp.Log($"Order changed to {SelectedModOrder.Name}. Skipping list loading since the orders match.");
						}
					}
				}
			});

			this.WhenAnyValue(vm => vm.SelectedProfileIndex, (index) => index > -1 && index < Profiles.Count).Subscribe((b) =>
			{
				if (!this.Refreshing && b)
				{
					if (SelectedModOrder != null)
					{
						BuildModOrderList(SelectedModOrderIndex);
					}
					else
					{
						BuildModOrderList(0);
					}
				}
			});

			var modsConnecton = mods.Connect();
			modsConnecton.Filter(x => !x.IsLarianMod && x.Type != "Adventure").Bind(out allMods).DisposeMany().Subscribe();

			modsConnecton.Filter(x => x.Type == "Adventure" && (!x.IsHidden || x.UUID == DivinityApp.ORIGINS_UUID)).Bind(out adventureMods).DisposeMany().Subscribe();
			this.WhenAnyValue(x => x.SelectedAdventureModIndex, x => x.AdventureMods.Count, (index, count) => index >= 0 && count > 0 && index < count).
				Where(b => b == true).Select(x => AdventureMods[SelectedAdventureModIndex]).
				ToProperty(this, x => x.SelectedAdventureMod, out selectedAdventureMod).DisposeWith(this.Disposables);

			var adventureModCanOpenObservable = this.WhenAnyValue(x => x.SelectedAdventureMod, (mod) => mod != null && !mod.IsLarianMod);
			adventureModCanOpenObservable.Subscribe();

			this.WhenAnyValue(x => x.SelectedAdventureModIndex).Throttle(TimeSpan.FromMilliseconds(50)).Subscribe((i) =>
			{
				if (AdventureMods != null && SelectedAdventureMod != null && SelectedProfile != null && SelectedProfile.ActiveMods != null)
				{
					if (!SelectedProfile.ActiveMods.Any(m => m.UUID == SelectedAdventureMod.UUID))
					{
						SelectedProfile.ActiveMods.RemoveAll(r => AdventureMods.Any(y => y.UUID == r.UUID));
						SelectedProfile.ActiveMods.Insert(0, SelectedAdventureMod.ToProfileModData());
					}
				}
			});

			OpenAdventureModInFileExplorerCommand = ReactiveCommand.Create<string>((path) =>
			{
				DivinityApp.Commands.OpenInFileExplorer(path);
			}, adventureModCanOpenObservable);

			CopyAdventureModPathToClipboardCommand = ReactiveCommand.Create<string>((path) =>
			{
				if (!String.IsNullOrWhiteSpace(path))
				{
					Clipboard.SetText(path);
					ShowAlert($"Copied '{path}' to clipboard.", 0, 10);
				}
				else
				{
					ShowAlert($"Path not found.", AlertType.Danger, 30);
				}
			}, adventureModCanOpenObservable);

			var canCheckForUpdates = this.WhenAnyValue(x => x.MainProgressIsActive, b => b == false);
			void checkForUpdatesAction()
			{
				CheckForUpdates(true);
			}
			CheckForAppUpdatesCommand = ReactiveCommand.Create(checkForUpdatesAction, canCheckForUpdates);
			Keys.CheckForUpdates.AddAction(checkForUpdatesAction, canCheckForUpdates);

			canRenameOrder = this.WhenAnyValue(x => x.SelectedModOrderIndex, (i) => i > 0);

			ToggleOrderRenamingCommand = ReactiveUI.ReactiveCommand.CreateFromTask<object, Unit>(ToggleRenamingLoadOrder, canRenameOrder);

			workshopMods.Connect().Bind(out workshopModsCollection).DisposeMany().Subscribe();

			//modsConnecton.WhenAnyPropertyChanged("Name", "IsClassicMod").Subscribe((mod) =>
			//{
			//	mod.UpdateDisplayName();
			//});

			modsConnecton.AutoRefresh(x => x.IsSelected).Filter(x => x.IsSelected && !x.IsEditorMod && File.Exists(x.FilePath)).Bind(out selectedPakMods).Subscribe();

			// Blinky animation on the tools/download buttons if the extender is required by mods and is missing
			if (AppSettings.FeatureEnabled("ScriptExtender"))
			{
				modsConnecton.ObserveOn(RxApp.MainThreadScheduler).AutoRefresh(x => x.ExtenderModStatus).
					Filter(x => x.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_MISSING || x.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_DISABLED).
					Select(x => x.Count).Subscribe(totalWithRequirements => {
						if (totalWithRequirements > 0)
						{
							if (Settings.ExtenderSettings != null)
							{
								HighlightExtenderDownload = !Settings.ExtenderSettings.ExtenderUpdaterIsAvailable;
							}
							else
							{
								HighlightExtenderDownload = true;
							}
						}
						else
						{
							HighlightExtenderDownload = false;
						}
					});
			}

			var anyPakModSelectedObservable = this.WhenAnyValue(x => x.SelectedPakMods.Count, (count) => count > 0);
			Keys.ExtractSelectedMods.AddAction(ExtractSelectedMods_Start, anyPakModSelectedObservable);

			this.WhenAnyValue(x => x.ModUpdatesViewData.NewAvailable,
				x => x.ModUpdatesViewData.UpdatesAvailable, (b1, b2) => b1 || b2).BindTo(this, x => x.ModUpdatesAvailable);

			ModUpdatesViewData.CloseView = new Action<bool>((bool refresh) =>
			{
				ModUpdatesViewData.Clear();
				if (refresh) RefreshAsync_Start();
				ModUpdatesViewVisible = false;
				view.Activate();
			});

			DebugCommand = ReactiveCommand.Create(() => InactiveMods.Add(new DivinityModData() { Name = "Test" }));

			//Throttle filters so they only happen when typing stops for 500ms

			this.WhenAnyValue(x => x.ActiveModFilterText).Throttle(TimeSpan.FromMilliseconds(500)).ObserveOn(RxApp.MainThreadScheduler).
				Subscribe((s) => { OnFilterTextChanged(s, ActiveMods); });

			this.WhenAnyValue(x => x.InactiveModFilterText).Throttle(TimeSpan.FromMilliseconds(500)).ObserveOn(RxApp.MainThreadScheduler).
				Subscribe((s) => { OnFilterTextChanged(s, InactiveMods); });

			var activeModsConnection = this.ActiveMods.ToObservableChangeSet().ObserveOn(RxApp.MainThreadScheduler);
			var inactiveModsConnection = this.InactiveMods.ToObservableChangeSet().ObserveOn(RxApp.MainThreadScheduler);

			activeModsConnection.Subscribe((x) =>
			{
				for (int i = 0; i < ActiveMods.Count; i++)
				{
					var mod = ActiveMods[i];
					mod.Index = i;
				}
			});

			activeModsConnection.WhenAnyPropertyChanged("Index").Throttle(TimeSpan.FromMilliseconds(25)).Subscribe(mod => {
				if (SelectedModOrder != null)
				{
					SelectedModOrder.Sort(SortModOrder);
				}
			});

			activeModsConnection.AutoRefresh(x => x.IsSelected).
				ToCollection().Select(x => x.Count(y => y.IsSelected)).ToProperty(this, x => x.ActiveSelected, out activeSelected);

			inactiveModsConnection.AutoRefresh(x => x.IsSelected).
				ToCollection().Select(x => x.Count(y => y.IsSelected)).ToProperty(this, x => x.InactiveSelected, out inactiveSelected);

			DivinityApp.Events.OrderNameChanged += OnOrderNameChanged;

			//var canSpeakOrder = this.WhenAnyValue(x => x.ActiveMods.Count, (c) => c > 0);

			Keys.SpeakActiveModOrder.AddAction(() =>
			{
				if(ActiveMods.Count > 0)
				{
					string text = String.Join(", ", ActiveMods.Select(x => x.DisplayName));
					ScreenReaderHelper.Speak($"{ActiveMods.Count} mods in the active order, including:", true);
					ScreenReaderHelper.Speak(text, false);
					//ShowAlert($"Active mods: {text}", AlertType.Info, 10);
				}
				else
				{
					//ShowAlert($"No mods in active order.", AlertType.Warning, 10);
					ScreenReaderHelper.Speak($"The active mods order is empty.");
				}
			});

			SaveSettingsSilentlyCommand = ReactiveCommand.Create(SaveSettings);

			#region GameMaster Support

			var gmModeChanged = this.WhenAnyValue(x => x.Settings.GameMasterModeEnabled);
			adventureModBoxVisibility = gmModeChanged.Select(x => !x ? Visibility.Visible : Visibility.Collapsed).StartWith(Visibility.Visible).ToProperty(this, nameof(AdventureModBoxVisibility));
			gameMasterModeVisibility = gmModeChanged.Select(x => x ? Visibility.Visible : Visibility.Collapsed).StartWith(Visibility.Collapsed).ToProperty(this, nameof(GameMasterModeVisibility));

			//int lastSelectedAdventureModIndex = 0;
			//gmModeChanged.Subscribe((b) =>
			//{
			//	//if(b)
			//	//{
			//	//	lastSelectedAdventureModIndex = SelectedAdventureModIndex;
			//	//}
			//	//else
			//	//{
			//	//	if(lastSelectedAdventureModIndex < AdventureMods.Count)
			//	//	{
			//	//		SelectedAdventureModIndex = lastSelectedAdventureModIndex;
			//	//	}
			//	//	else
			//	//	{
			//	//		SelectedAdventureModIndex = 0;
			//	//	}
			//	//}
			//});

			gameMasterCampaigns.Connect().Bind(out gameMasterCampaignsData).Subscribe();
			//this.WhenAnyValue(x => x.SelectedGameMasterCampaignIndex).Select(x => GameMasterCampaigns.ElementAtOrDefault(x)).ToProperty(this, nameof(SelectedGameMasterCampaign));
			var justSelectedGameMasterCampaign = this.WhenAnyValue(x => x.SelectedGameMasterCampaignIndex, x => x.GameMasterCampaigns.Count);
			selectedGameMasterCampaign = justSelectedGameMasterCampaign.Select(x => GameMasterCampaigns.ElementAtOrDefault(x.Item1)).ToProperty(this, nameof(SelectedGameMasterCampaign));

			//var gmCampaignChanged = this.WhenAnyValue(x => x.SelectedGameMasterCampaignIndex, x => x.GameMasterCampaigns.Count, (index, count) => index >= 0 && count > 0 && index < count).
			//	Where(b => b == true).
			//	Select(x => GameMasterCampaigns.ElementAtOrDefault(SelectedGameMasterCampaignIndex));
			Keys.ImportOrderFromSelectedGMCampaign.AddAction(() => LoadGameMasterCampaignModOrder(SelectedGameMasterCampaign), gmModeChanged);
			justSelectedGameMasterCampaign.ObserveOn(RxApp.MainThreadScheduler).Subscribe((d) => {
				if (!this.Refreshing && IsInitialized && (Settings != null && Settings.AutomaticallyLoaGMCampaignMods) && d.Item1 > -1)
				{
					var selectedCampaign = GameMasterCampaigns.ElementAtOrDefault(d.Item1);
					if (selectedCampaign != null && !LoadingOrder)
					{
						if (LoadGameMasterCampaignModOrder(selectedCampaign))
						{
							DivinityApp.Log($"Successfully loaded GM campaign order {selectedCampaign.Name}.");
						}
						else
						{
							DivinityApp.Log($"Failed to load GM campaign order {selectedCampaign.Name}.");
						}
					}
				}
			});
			#endregion
		}
	}
}
