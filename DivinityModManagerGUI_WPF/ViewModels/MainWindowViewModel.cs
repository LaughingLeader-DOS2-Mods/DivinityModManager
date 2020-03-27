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
using Newtonsoft.Json.Linq;
using GongSolutions.Wpf.DragDrop.Utilities;

namespace DivinityModManager.ViewModels
{

	public class ModListDropHandler : DefaultDropHandler
	{
		override public void Drop(IDropInfo dropInfo)
		{
			if (dropInfo == null || dropInfo.DragInfo == null)
			{
				return;
			}

			var insertIndex = dropInfo.UnfilteredInsertIndex;

			var itemsControl = dropInfo.VisualTarget as ItemsControl;
			if (itemsControl != null)
			{
				var editableItems = itemsControl.Items as IEditableCollectionView;
				if (editableItems != null)
				{
					var newItemPlaceholderPosition = editableItems.NewItemPlaceholderPosition;
					if (newItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning && insertIndex == 0)
					{
						++insertIndex;
					}
					else if (newItemPlaceholderPosition == NewItemPlaceholderPosition.AtEnd && insertIndex == itemsControl.Items.Count)
					{
						--insertIndex;
					}
				}
			}

			var destinationList = dropInfo.TargetCollection.TryGetList();
			var data = ExtractData(dropInfo.Data).OfType<DivinityModData>().ToList();

			var sourceList = dropInfo.DragInfo.SourceCollection.TryGetList();
			if (sourceList != null)
			{
				foreach (var o in data)
				{
					var index = sourceList.IndexOf(o);
					if (index != -1)
					{
						sourceList.RemoveAt(index);
						// so, is the source list the destination list too ?
						if (destinationList != null && Equals(sourceList, destinationList) && index < insertIndex)
						{
							--insertIndex;
						}
					}
				}
			}

			if (destinationList != null)
			{
				var objects2Insert = new List<object>();
				foreach (var o in data)
				{
					var obj2Insert = o;
					objects2Insert.Add(obj2Insert);
					destinationList.Insert(insertIndex++, obj2Insert);
				}

				var selectDroppedItems = itemsControl is TabControl || (itemsControl != null && GongSolutions.Wpf.DragDrop.DragDrop.GetSelectDroppedItems(itemsControl));
				if (selectDroppedItems)
				{
					SelectDroppedItems(dropInfo, objects2Insert);
				}
			}

			bool isActive = dropInfo.TargetCollection == _viewModel.ActiveMods;

			foreach (var mod in _viewModel.Mods)
			{
				if(data.Contains(mod))
				{
					mod.IsActive = isActive;
					mod.IsSelected = true;
				}
				else
				{
					mod.IsSelected = false;
				}
			}

			if (isActive)
			{
				_viewModel.Layout.UpdateViewSelection(_viewModel.ActiveMods);
				_viewModel.OnFilterTextChanged(_viewModel.ActiveModFilterText, _viewModel.ActiveMods);
				//_viewModel.Layout.FixActiveModsScrollbar();
			}
			else
			{
				_viewModel.Layout.UpdateViewSelection(_viewModel.InactiveMods);
				_viewModel.OnFilterTextChanged(_viewModel.InactiveModFilterText, _viewModel.InactiveMods);
			}

			if (_viewModel.SelectedModOrder != null)
			{
				_viewModel.SelectedModOrder.Order.Clear();
				foreach (var x in _viewModel.ActiveMods)
				{
					_viewModel.SelectedModOrder.Add(x);
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
		private MainWindowViewModel _viewModel;

		public ModListDragHandler(MainWindowViewModel vm) : base()
		{
			_viewModel = vm;
		}

		public override void StartDrag(IDragInfo dragInfo)
		{
			//base.StartDrag(dragInfo);
			if(dragInfo != null)
			{
				dragInfo.Data = null;
				if (dragInfo.SourceCollection == _viewModel.ActiveMods)
				{
					var selected = _viewModel.ActiveMods.Where(x => x.IsSelected);
					dragInfo.Data = selected;
					//Trace.WriteLine($"Drag source is ActiveMods | {selected.Count()}");
				}
				else if(dragInfo.SourceCollection == _viewModel.InactiveMods)
				{
					var selected = _viewModel.InactiveMods.Where(x => x.IsSelected && x.CanDrag);
					dragInfo.Data = selected;
					//Trace.WriteLine($"Drag source is InactiveMods | {selected.Count()} | Classic: {selected.Where(x => x.IsClassicMod && x.CanDrag).Count()}");
				}
				dragInfo.Effects = dragInfo.Data != null ? DragDropEffects.Copy | DragDropEffects.Move : DragDropEffects.None;
			}
		}

		public override bool CanStartDrag(IDragInfo dragInfo)
		{
			if(dragInfo.Data is ISelectable d && !d.CanDrag)
			{
				return false;
			}
			else if(dragInfo.Data is IEnumerable<DivinityModData> modData)
			{
				if(modData.All(x => !x.CanDrag))
				{
					return false;
				}
			}
			return true;
		}
	}

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

		public string Title => "Divinity Mod Manager " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
		private bool IsInitialized { get; set; } = false;

		protected SourceList<DivinityModData> mods = new SourceList<DivinityModData>();

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

		public DivinityLoadOrder SelectedModOrder
		{
			get => ModOrderList.ElementAtOrDefault(SelectedModOrderIndex);
		}

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

		private bool isRenamingOrder = false;

		public bool IsRenamingOrder
		{
			get => isRenamingOrder;
			set { this.RaiseAndSetIfChanged(ref isRenamingOrder, value); }
		}

		public IObservable<bool> canRenameOrder;

		private IObservable<bool> canSaveSettings;
		private IObservable<bool> canOpenWorkshopFolder;
		private IObservable<bool> canOpenDOS2DEGame;
		private IObservable<bool> canOpenDialogWindow;
		private IObservable<bool> gameExeFoundObservable;
		private IObservable<bool> canInstallOsiExtender;

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
		public ICommand ExportLoadOrderAsTextFileCommand { get; set; }
		public ICommand CancelMainProgressCommand { get; set; }
		public ICommand ToggleDisplayNameCommand { get; set; }
		public ICommand ToggleDarkModeCommand { get; set; }
		public ICommand CopyPathToClipboardCommand { get; set; }
		public ICommand DownloadAndInstallOsiExtenderCommand { get; private set; }
		public ICommand ExtractSelectedModsCommand { get; private set; }
		public ICommand RenameSaveCommand { get; private set; }
		public ICommand ExportOrderAsListCommand { get; private set; }
		public ICommand CopyOrderToClipboardCommand { get; private set; }
		public ICommand OpenAdventureModInFileExplorerCommand { get; private set; }
		public ICommand CopyAdventureModPathToClipboardCommand { get; private set; }
		public ReactiveCommand<DivinityLoadOrder, Unit> DeleteOrderCommand { get; private set; }
		public ICommand ToggleOrderRenamingCommand { get; set; }

		private DivinityGameLaunchWindowAction actionOnGameLaunch = DivinityGameLaunchWindowAction.None;
		public DivinityGameLaunchWindowAction ActionOnGameLaunch
		{
			get => actionOnGameLaunch;
			set { this.RaiseAndSetIfChanged(ref actionOnGameLaunch, value); }
		}
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

		private async Task<Unit> LoadExtenderSettingsAsync(CancellationToken t)
		{
			try
			{
				string latestReleaseZipUrl = "";
				Trace.WriteLine($"Checking for latest DXGI.dll release at 'Norbyte/ositools'.");
				var latestReleaseData = await GithubHelper.GetLatestReleaseDataAsync("Norbyte/ositools");
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
					var lines = jsonData.Select(kvp => kvp.Key + ": " + kvp.Value.ToString());
					Trace.WriteLine($"Releases Data:\n{String.Join(Environment.NewLine, lines)}");
	#endif
				}
				if (!String.IsNullOrEmpty(latestReleaseZipUrl))
				{
					PathwayData.OsirisExtenderLatestReleaseUrl = latestReleaseZipUrl;
					Trace.WriteLine($"OsiTools latest release url found: {latestReleaseZipUrl}");
				}
				else
				{
					Trace.WriteLine($"OsiTools latest release not found.");
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Error checking for latest OsiExtender release: {ex.ToString()}");
			}

			try
			{
				string extenderSettingsJson = PathwayData.OsirisExtenderSettingsFile(Settings);
				if (extenderSettingsJson.IsExistingFile())
				{
					var osirisExtenderSettings = DivinityJsonUtils.SafeDeserializeFromPath<OsiExtenderSettings>(extenderSettingsJson);
					if (osirisExtenderSettings != null)
					{
						Trace.WriteLine($"Loaded extender settings from '{extenderSettingsJson}'.");
						Settings.ExtenderSettings.Set(osirisExtenderSettings);
					}
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Error loading extender settings: {ex.ToString()}");
			}

			string extenderUpdaterPath = Path.Combine(Path.GetDirectoryName(Settings.DOS2DEGameExecutable), "DXGI.dll");
			Trace.WriteLine($"Looking for OsiExtender at '{extenderUpdaterPath}'.");
			if (File.Exists(extenderUpdaterPath))
			{
				Trace.WriteLine($"Checking DXGI.dll for Osiris ASCII bytes.");
				try
				{
					using (var stream = new FileStream(extenderUpdaterPath, FileMode.Open, FileAccess.Read, FileShare.Read))
					{
						byte[] bytes = DivinityStreamUtils.ReadToEnd(stream);
						if (bytes.IndexOf(Encoding.ASCII.GetBytes("Osiris")) >= 0)
						{
							Settings.ExtenderSettings.ExtenderUpdaterIsAvailable = true;
							Trace.WriteLine($"Found the OsiExtender at '{extenderUpdaterPath}'.");
						}
						else
						{
							Trace.WriteLine($"Failed to find ASCII bytes in '{extenderUpdaterPath}'.");
						}
					}
				}
				catch (System.IO.IOException ex)
				{
					// This can happen if the game locks up the dll.
					// Assume it's the extender for now.
					Settings.ExtenderSettings.ExtenderUpdaterIsAvailable = true;
					Trace.WriteLine($"WARNING: {extenderUpdaterPath} is locked by a process.");
				}
				catch (Exception ex)
				{
					Trace.WriteLine($"Error reading: '{extenderUpdaterPath}'\n\t{ex.ToString()}");
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
			else
			{
				Trace.WriteLine($"DXGI.dll not found. Skipping extender check.");
			}
			return Unit.Default;
		}

		private void LoadExtenderSettings()
		{
			if (File.Exists(Settings.DOS2DEGameExecutable))
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

			gameExeFoundObservable = this.WhenAnyValue(x => x.Settings.DOS2DEGameExecutable, (path) => path.IsExistingFile());
			canInstallOsiExtender = this.WhenAnyValue(x => x.PathwayData.OsirisExtenderLatestReleaseUrl, x => x.Settings.DOS2DEGameExecutable,
				(url, exe) => !String.IsNullOrWhiteSpace(url) && exe.IsExistingFile()).ObserveOn(RxApp.MainThreadScheduler);

			DownloadAndInstallOsiExtenderCommand = ReactiveCommand.Create(InstallOsiExtender_Start, canInstallOsiExtender).DisposeWith(Settings.Disposables);

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

			var canResetExtenderSettingsObservable = this.WhenAny(x => x.Settings.ExtenderSettings, (extenderSettings) => extenderSettings != null);
			Settings.ResetExtenderSettingsToDefaultCommand = ReactiveCommand.Create(() =>
			{
				MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(view.SettingsWindow, $"Reset Extender Settings to Default?\nCurrent Extender Settings will be lost.", "Confirm Extender Settings Reset",
					MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, view.MainWindowMessageBox_OK.Style);
				if (result == MessageBoxResult.Yes)
				{
					Settings.ExtenderSettings.SetToDefault();
				}
			}, canResetExtenderSettingsObservable).DisposeWith(Settings.Disposables);

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
				DivinityModData pakMod = mods.Items.FirstOrDefault(x => x.UUID == workshopMod.UUID && !x.IsClassicMod);
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
				List<DivinityModData> modPakData = DivinityModDataLoader.LoadModPackageData(Settings.DOS2WorkshopPath, true);
				if (modPakData.Count > 0)
				{
					//Ignore Classic mods since they share the same workshop folder
					var sortedWorkshopMods = modPakData.OrderBy(m => m.Name);
					workshopMods.Clear();
					workshopMods.AddRange(sortedWorkshopMods);

					Trace.WriteLine($"Loaded '{workshopMods.Count}' workshop mods from '{Settings.DOS2WorkshopPath}'.");
				}
			}
		}

		public async Task<List<DivinityModData>> LoadWorkshopModsAsync()
		{
			List<DivinityModData> newWorkshopMods = new List<DivinityModData>();

			if (Directory.Exists(Settings.DOS2WorkshopPath))
			{
				newWorkshopMods = await DivinityModDataLoader.LoadModPackageDataAsync(Settings.DOS2WorkshopPath, true);
				return newWorkshopMods.OrderBy(m => m.Name).ToList();
			}

			return newWorkshopMods;
		}

		private void SetDOS2Pathways(string currentGameDataPath)
		{
			try
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

				LoadExtenderSettings();
			}
			catch(Exception ex)
			{
				Trace.WriteLine($"Error setting up game pathways: {ex.ToString()}");
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
			mods.AddRange(DivinityApp.MODS_Larian_All);
			mods.AddRange(finalMods);

			Trace.WriteLine($"Loaded '{finalMods.Count()}' mods.");
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
			return mods.Items.Any(k => k.UUID == divinityModData.UUID) || DivinityApp.IgnoredMods.Any(im => im.UUID == divinityModData.UUID);
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
							Trace.WriteLine($"UUID {uuid} is missing from the profile's active mod list.");
						}
					}

					SelectedProfile.SavedLoadOrder = currentOrder;
				}

				Trace.WriteLine($"Profile order: {String.Join(";", SelectedProfile.SavedLoadOrder.Order.Select(x => x.Name))}");

				ModOrderList.Clear();
				ModOrderList.Add(SelectedProfile.SavedLoadOrder);
				ModOrderList.AddRange(SavedModOrderList);

				RxApp.MainThreadScheduler.Schedule(_ =>
				{
					if(selectIndex != -1)
					{
						if (selectIndex >= ModOrderList.Count) selectIndex = ModOrderList.Count - 1;
						Trace.WriteLine($"Setting next order index to [{selectIndex}/{ModOrderList.Count-1}].");
						try
						{
							SelectedModOrderIndex = selectIndex;
							var nextOrder = ModOrderList.ElementAtOrDefault(selectedModOrderIndex);
							LoadModOrder(nextOrder, missingMods);
							Settings.LastOrder = nextOrder?.Name;
						}
						catch (Exception ex)
						{
							Trace.WriteLine($"Error setting next load order:\n{ex.ToString()}");
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

			Trace.WriteLine($"Loading mod order '{order.Name}'.");
			List<DivinityMissingModData> missingMods = new List<DivinityMissingModData>();
			if(missingModsFromProfileOrder != null && missingModsFromProfileOrder.Count > 0)
			{
				missingMods.AddRange(missingModsFromProfileOrder);
				Trace.WriteLine($"Missing mods (from profile): {String.Join(";", missingModsFromProfileOrder)}");
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
						foreach (var dependency in mod.Dependencies)
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
				Trace.WriteLine($"Missing mods: {String.Join(";", missingMods)}");
				if(Settings?.DisableMissingModWarnings == true)
				{
					Trace.WriteLine("Skipping missing mod display.");
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
			if(sender is Xceed.Wpf.Toolkit.MessageBox messageBox)
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

			if (lastActiveOrder != null)
			{
				// Just in case a mod disappears
				//var restoredOrder = lastActiveOrder.Where(x => Mods.Any(y => y.UUID == x.UUID));
				SelectedModOrder.SetOrder(lastActiveOrder);
				BuildModOrderList(lastOrderIndex);
			}
			else
			{
				BuildModOrderList(0);
			}

			MainProgressValue += taskStepAmount;

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

		private void CheckExtenderData()
		{
			if (Settings != null && Mods.Count > 0)
			{
				foreach (var mod in Mods)
				{
					if (mod.OsiExtenderData != null && mod.OsiExtenderData.HasAnySettings)
					{
						// Lua support without Osiris makes it optional
						bool onlyUsesLua = mod.OsiExtenderData.FeatureFlags.Contains("Lua") && !mod.OsiExtenderData.FeatureFlags.Contains("OsirisExtensions");

						if (!mod.OsiExtenderData.FeatureFlags.Contains("Preprocessor") && !onlyUsesLua)
						{
							if(!Settings.ExtenderSettings.EnableExtensions)
							{
								mod.ExtenderModStatus = DivinityExtenderModStatus.REQUIRED_DISABLED;
							}
							else
							{
								if (Settings.ExtenderSettings != null && Settings.ExtenderSettings.ExtenderVersion > -1)
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

			string lastAdventureMod = null;
			if (SelectedAdventureMod != null) lastAdventureMod = SelectedAdventureMod.UUID;

			string selectedProfileUUID = "";
			if(SelectedProfile != null)
			{
				selectedProfileUUID = SelectedProfile.UUID;
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

				if (String.IsNullOrEmpty(selectedProfileUUID) && (loadedProfiles != null && loadedProfiles.Count > 0))
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
					mods.AddRange(DivinityApp.MODS_Larian_All);
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
				if (lastAdventureMod != null && AdventureMods != null && AdventureMods.Count > 0)
				{
					Trace.WriteLine($"Setting selected adventure mod.");
					var nextAdventureMod = AdventureMods.FirstOrDefault(x => x.UUID == lastAdventureMod);
					if (nextAdventureMod != null)
					{
						SelectedAdventureModIndex = AdventureMods.IndexOf(nextAdventureMod);
					}
					SelectedAdventureModIndex = 0;
				}
				else
				{
					SelectedAdventureModIndex = 0;
				}

				Trace.WriteLine($"Finishing up refresh.");

				Refreshing = false;
				OnMainProgressComplete();
				OnRefreshed?.Invoke(this, new EventArgs());
				if (this.IsInitialized)
				{
					Trace.WriteLine($"Loading extender settings.");
					LoadExtenderSettings();
				}
				else
				{
					Trace.WriteLine($"Checking extender data.");
					CheckExtenderData();
				}
			});

			/*
			RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), () =>
			{
				OnFilterTextChanged(ActiveModFilterText, ActiveMods);
				OnFilterTextChanged(InactiveModFilterText, InactiveMods);
			});
			*/

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

				Trace.WriteLine($"Attempting to load saved load orders from '{loadOrderDirectory}'.");
				return await DivinityModDataLoader.FindLoadOrderFilesInDirectoryAsync(loadOrderDirectory);
			}
			catch(Exception ex)
			{
				Trace.WriteLine($"Error loading external load orders: {ex.ToString()}.");
				return null;
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
					}
					else
					{
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

				for (int i = 0; i < order.Order.Count;i++)
				{
					var entry = order.Order[i];
					var mod = mods.Items.FirstOrDefault(m => m.UUID == entry.UUID);
					if (mod != null)
					{
						if (mod.Dependencies.Count > 0)
						{
							foreach (var dependency in mod.Dependencies)
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

			if(displayExtenderModWarning)
			{
				//Trace.WriteLine($"Mod Order: {String.Join("\n", order.Order.Select(x => x.Name))}");
				Trace.WriteLine("Checking mods for extender requirements.");
				List<DivinityMissingModData> extenderRequiredMods = new List<DivinityMissingModData>();
				for (int i = 0; i < order.Order.Count; i++)
				{
					var entry = order.Order[i];
					var mod = ActiveMods.FirstOrDefault(m => m.UUID == entry.UUID);
					if (mod != null)
					{
						Trace.WriteLine($"{mod.Name} | ExtenderModStatus: {mod.ExtenderModStatus}");

						if (mod.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_DISABLED || mod.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_MISSING)
						{
							extenderRequiredMods.Add(new DivinityMissingModData{
								Index = mod.Index,
								Name = mod.DisplayName,
								UUID = mod.UUID,
								Dependency = false
							});

							if (mod.Dependencies.Count > 0)
							{
								foreach (var dependency in mod.Dependencies)
								{
									var dependencyMod = mods.Items.FirstOrDefault(m => m.UUID == dependency.UUID);
									// Dependencies not in the order that require the extender
									if (dependencyMod.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_DISABLED || dependencyMod.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_MISSING)
									{
										extenderRequiredMods.Add(new DivinityMissingModData{
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
					Trace.WriteLine("Displaying mods that require the extender.");
					view.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
					view.MainWindowMessageBox_OK.Closed += MainWindowMessageBox_Closed_ResetColor;
					view.MainWindowMessageBox_OK.ShowMessageBox(String.Join("\n", extenderRequiredMods.OrderBy(x => x.Index)),
						"Mods Require the Script Extender - Install it with the Tools menu!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
				}
			}
		}

		private DivinityProfileActiveModData ProfileActiveModDataFromUUID(string uuid)
		{
			var modData = mods.Items.FirstOrDefault(x => x.UUID == uuid);
			if(modData != null)
			{
				modData.ToProfileModData();
			}
			return new DivinityProfileActiveModData()
			{
				UUID = uuid
			};
		}

		private async Task<bool> ExportLoadOrderAsync()
		{
			if (SelectedProfile != null && SelectedModOrder != null)
			{
				string outputPath = Path.Combine(SelectedProfile.Folder, "modsettings.lsx");
				var result = await DivinityModDataLoader.ExportModSettingsToFileAsync(SelectedProfile.Folder, SelectedModOrder,
					mods.Items, Settings.AutoAddDependenciesWhenExporting, SelectedAdventureMod);

				if (result)
				{
					view.AlertBar.SetSuccessAlert($"Exported load order to '{outputPath}'", 15);

					if (DivinityModDataLoader.ExportedSelectedProfile(PathwayData.DocumentsProfilesPath, SelectedProfile.UUID))
					{
						Trace.WriteLine($"Set active profile to '{SelectedProfile.Name}'.");
					}
					else
					{
						Trace.WriteLine($"Could not set active profile to '{SelectedProfile.Name}'.");
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

					RxApp.MainThreadScheduler.Schedule(_ => DisplayMissingMods(SelectedModOrder));
					return true;
				}
				else
				{
					string msg = $"Problem exporting load order to '{outputPath}'";
					view.AlertBar.SetDangerAlert(msg);
					view.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
					view.MainWindowMessageBox_OK.Closed += MainWindowMessageBox_Closed_ResetColor;
					view.MainWindowMessageBox_OK.ShowMessageBox(msg, "Mod Order Export Failed", MessageBoxButton.OK);
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
			Trace.WriteLine($"Main progress is complete.");
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
						try
						{
							AutoUpdater.Start(DivinityApp.URL_UPDATE);
						}
						catch(Exception ex)
						{
							Trace.WriteLine($"Error running AutoUpdater:\n{ex.ToString()}");
						}
					}
				}
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
						outputText = "Index\tName\tAuthor\tFileName\tType\tModes\tDependencies\n";
						outputText += String.Join("\n", ActiveMods.Select(x => $"{x.Index}\t{x.Name}\t{x.Author}\t{x.OutputPakName}\t{x.Type}\t{String.Join(", ", x.Modes)}\t{String.Join(", ", x.Dependencies.Select(y => y.Name))}"));
					}
					else
					{
						//Text file format
						outputText = String.Join("\n", ActiveMods.Select(x => $"{x.Index}. {x.Name} ({x.OutputPakName})"));
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
				Trace.WriteLine($"SelectedProfile({SelectedProfile}) SelectedModOrder({SelectedModOrder})");
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
					if (LoadModOrder(SelectedModOrder))
					{
						Trace.WriteLine($"Successfully re-loaded order {SelectedModOrder.Name} with save order.");
					}
					else
					{
						Trace.WriteLine($"Failed to load order {SelectedModOrder.Name}.");
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
							totalHidden += 1;
						}
					}
				}
			}
		
			if(modDataList == ActiveMods)
			{
				TotalActiveModsHidden = totalHidden;
			}
			else if(modDataList == InactiveMods)
			{
				TotalInactiveModsHidden = totalHidden;
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
							text += $"{mod.Index+1}. {mod.DisplayName}";
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
					Trace.WriteLine($"Error extracting package: {ex.ToString()}");
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
						Settings.ExtenderSettings.ExtenderIsAvailable = true;
						if(!String.IsNullOrWhiteSpace(PathwayData.OsirisExtenderLatestReleaseVersion))
						{
							var re = new Regex("v([0-9]+)");
							var m = re.Match(PathwayData.OsirisExtenderLatestReleaseVersion);
							if (m.Success)
							{
								if (int.TryParse(m.Groups[1].Value, out int version))
								{
									Settings.ExtenderSettings.ExtenderVersion = version;
									Trace.WriteLine($"Set extender version to v{version},");
								}
							}
						}
						else if(PathwayData.OsirisExtenderLatestReleaseUrl.Contains("v"))
						{
							var re = new Regex("v([0-9]+).*.zip");
							var m = re.Match(PathwayData.OsirisExtenderLatestReleaseUrl);
							if(m.Success)
							{
								if(int.TryParse(m.Groups[1].Value, out int version))
								{
									Settings.ExtenderSettings.ExtenderVersion = version;
									Trace.WriteLine($"Set extender version to v{version},");
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
			string exeDir = Path.GetDirectoryName(Settings.DOS2DEGameExecutable);
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

		private int SortModOrder(DivinityLoadOrderEntry a, DivinityLoadOrderEntry b)
		{
			if(a != null && b != null)
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

		public MainWindowViewModel() : base()
		{
			exceptionHandler = new MainWindowExceptionHandler(this);
			RxApp.DefaultExceptionHandler = exceptionHandler;

			this.DropHandler = new ModListDropHandler(this);
			this.DragHandler = new ModListDragHandler(this);

			Activator = new ViewModelActivator();

			this.WhenActivated((CompositeDisposable disposables) =>
			{
				if (!disposables.Contains(this.Disposables)) disposables.Add(this.Disposables);
			});

			var canExecuteSaveCommand = this.WhenAnyValue(x => x.CanSaveOrder, (canSave) => canSave == true);
			SaveOrderCommand = ReactiveCommand.Create(SaveLoadOrder, canExecuteSaveCommand);

			var canExecuteSaveAsCommand = this.WhenAnyValue(x => x.CanSaveOrder, x => x.MainProgressIsActive, (canSave, p) => canSave && !p);
			SaveOrderAsCommand = ReactiveCommand.Create(SaveLoadOrderAs, canExecuteSaveAsCommand);

			ExportOrderCommand = ReactiveCommand.CreateFromTask(ExportLoadOrderAsync);

			IObservable<bool> canStartExport = this.WhenAny(x => x.MainProgressToken, (t) => t != null);
			ExportLoadOrderAsArchiveCommand = ReactiveCommand.Create(ExportLoadOrderToArchive_Start, canStartExport);
			ExportLoadOrderAsArchiveToFileCommand = ReactiveCommand.Create(ExportLoadOrderToArchiveAs, canStartExport);

			var anyActiveObservable = this.WhenAnyValue(x => x.ActiveMods.Count, (c) => c > 0);
			ExportLoadOrderAsTextFileCommand = ReactiveCommand.Create(ExportLoadOrderToTextFileAs, anyActiveObservable);

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

				if (Settings.ActionOnGameLaunch != DivinityGameLaunchWindowAction.None)
				{
					switch(Settings.ActionOnGameLaunch)
					{
						case DivinityGameLaunchWindowAction.Minimize:
							view.WindowState = WindowState.Minimized;
							break;
						case DivinityGameLaunchWindowAction.Close:
							App.Current.Shutdown();
							break;
					}
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

			CopyOrderToClipboardCommand = ReactiveCommand.Create(() =>
			{
				try
				{
					if(ActiveMods.Count > 0)
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
					view.AlertBar.SetDangerAlert($"Error copying order to clipboard: {ex.ToString()}", 15);
				}
			});

			ExportOrderAsListCommand = ReactiveCommand.Create(ExportOrderToListAs, canExecuteSaveAsCommand);

			var profileChanged = this.WhenAnyValue(x => x.SelectedProfileIndex, x => x.Profiles.Count, (index, count) => index >= 0 && count > 0 && index < count).Where(b => b == true).
				Select(x => Profiles.ElementAtOrDefault(SelectedProfileIndex));
			profileChanged.ToProperty(this, x => x.SelectedProfile, out selectedprofile).DisposeWith(this.Disposables);

			profileChanged.Subscribe((profile) =>
			{
				if (profile != null && profile.ActiveMods != null && profile.ActiveMods.Count > 0)
				{
					var adventureModData = AdventureMods.FirstOrDefault(x => profile.ActiveMods.Any(y => y.UUID == x.UUID));
					if (adventureModData != null)
					{
						var nextAdventure = AdventureMods.IndexOf(adventureModData);
						Trace.WriteLine($"Found adventure mod in profile: {adventureModData.Name} | {nextAdventure}");
						if (nextAdventure > -1)
						{
							SelectedAdventureModIndex = nextAdventure;
						}
					}
				}
			});

			//var selectedOrderObservable = this.WhenAnyValue(x => x.SelectedModOrderIndex, x => x.ModOrderList.Count, 
			//	(index, count) => index >= 0 && count > 0 && index < count).Where(b => b == true);

			//selectedOrderObservable.Select(x => ModOrderList[SelectedModOrderIndex]).ToProperty(this, x => x.SelectedModOrder, out selectedModOrder).DisposeWith(this.Disposables);
			//selectedOrderObservable.Select(x => ModOrderList[SelectedModOrderIndex].DisplayName).ToProperty(this, x => x.SelectedModOrderDisplayName, out selectedModOrderDisplayName);

			//Throttle in case the index changes quickly in a short timespan
			this.WhenAnyValue(vm => vm.SelectedModOrderIndex).ObserveOn(RxApp.MainThreadScheduler).Subscribe((_) => {
				if (SelectedModOrderIndex > -1)
				{
					if (SelectedModOrder != null && !LoadingOrder)
					{
						if (!SelectedModOrder.OrderEquals(ActiveMods.Select(x => x.UUID)))
						{
							if(LoadModOrder(SelectedModOrder))
							{
								Trace.WriteLine($"Successfully loaded order {SelectedModOrder.Name}.");
							}
							else
							{
								Trace.WriteLine($"Failed to load order {SelectedModOrder.Name}.");
							}
						}
						else
						{
							Trace.WriteLine($"Order changed to {SelectedModOrder.Name}. Skipping list loading since the orders match.");
						}
					}
				}
			});

			this.WhenAnyValue(vm => vm.SelectedProfileIndex, (index) => index > -1 && index < Profiles.Count).Subscribe((b) =>
			{
				if (b)
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

			modsConnecton.Filter(x => x.Type == "Adventure" && !x.IsHidden).Bind(out adventureMods).DisposeMany().Subscribe();
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
					ShowAlert($"Path not found.", -1, 30);
				}
			}, adventureModCanOpenObservable);

			canRenameOrder = this.WhenAnyValue(x => x.SelectedModOrderIndex, (i) => i > 0);

			ToggleOrderRenamingCommand = ReactiveCommand.CreateFromTask<object>(async (object control) =>
			{
				IsRenamingOrder = !IsRenamingOrder;

				await Task.Delay(50);
				RxApp.MainThreadScheduler.Schedule(() =>
				{
					if (control is ComboBox comboBox)
					{
						var tb = comboBox.FindVisualChildren<TextBox>().FirstOrDefault();
						if(tb != null)
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
						Trace.WriteLine("Can't find OrdersComboBox!");
					}
				});
			}, canRenameOrder);

			workshopMods.Connect().Bind(out workshopModsCollection).DisposeMany().Subscribe();

			modsConnecton.WhenAnyPropertyChanged("Name", "IsClassicMod").Subscribe((mod) =>
			{
				mod.UpdateDisplayName();
			});

			modsConnecton.AutoRefresh(x => x.IsSelected).Filter(x => x.IsSelected && !x.IsEditorMod && File.Exists(x.FilePath)).Bind(out selectedPakMods).Subscribe();

			var anyPakModSelectedObservable = this.WhenAnyValue(x => x.SelectedPakMods.Count, (count) => count > 0);
			ExtractSelectedModsCommand = ReactiveCommand.Create(ExtractSelectedMods_Start, anyPakModSelectedObservable);

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

			var activeModsConnection = this.ActiveMods.ToObservableChangeSet().ObserveOn(RxApp.MainThreadScheduler);
			var inactiveModsConnection = this.InactiveMods.ToObservableChangeSet().ObserveOn(RxApp.MainThreadScheduler);

			activeModsConnection.Subscribe((x) =>
			{
				for(int i = 0; i < ActiveMods.Count; i++)
				{
					var mod = ActiveMods[i];
					mod.Index = i;
				}
			});

			activeModsConnection.WhenAnyPropertyChanged("Index").Throttle(TimeSpan.FromMilliseconds(25)).Subscribe(_ => {
				if (SelectedModOrder != null)
				{
					SelectedModOrder.Sort(SortModOrder);
				}
			});

			//.Buffer(TimeSpan.FromMilliseconds(50)).Distinct().SelectMany(x => x)
			/*
			activeModsConnection.WhereReasonsAre(ListChangeReason.Add, ListChangeReason.AddRange).ForEachItemChange((x) =>
			{
				if (x != null && x.Current != null)
				{
					x.Current.IsActive = true;
					//if (SelectedModOrder != null)
					//{
					//	SelectedModOrder.Add(x.Current);
					//}
				}
				//x.Current.Index = x.CurrentIndex;
			}).Throttle(TimeSpan.FromMilliseconds(5)).Subscribe(_ =>
			{
				OnFilterTextChanged(ActiveModFilterText, ActiveMods);
				if (SelectedModOrder != null)
				{
					SelectedModOrder.SetOrder(ActiveMods.Select(x => x.ToOrderEntry()));
				}
			});

			inactiveModsConnection.WhereReasonsAre(ListChangeReason.Add, ListChangeReason.AddRange).ForEachItemChange((x) =>
			{
				if (x != null && x.Current != null)
				{
					x.Current.IsActive = false;
					if (SelectedModOrder != null)
					{
						//SelectedModOrder.Remove(x.Current);
					}
				}
			}).Throttle(TimeSpan.FromMilliseconds(5)).Subscribe(_ =>
			{
				OnFilterTextChanged(InactiveModFilterText, InactiveMods);
				if (SelectedModOrder != null)
				{
					SelectedModOrder.SetOrder(ActiveMods.Select(x => x.ToOrderEntry()));
				}
			});
			*/

			activeModsConnection.AutoRefresh(x => x.IsSelected).
				ToCollection().Select(x => x.Count(y => y.IsSelected)).ToProperty(this, x => x.ActiveSelected, out activeSelected);

			inactiveModsConnection.AutoRefresh(x => x.IsSelected).
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
		}
	}
}
