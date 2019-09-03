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
using ReactiveUI.Legacy;
using System.ComponentModel;
using System.IO;
using System.Reactive.Concurrency;
using Newtonsoft.Json;
using Microsoft.Win32;

namespace DivinityModManager.ViewModels
{

	public class ModListDropHandler : DefaultDropHandler
	{
		private ObservableAsPropertyHelper<int> nextIndex;

		override public void Drop(IDropInfo dropInfo)
		{
			base.Drop(dropInfo);

			//if(dropInfo.Data is DivinityModData modData)
			//{
			//	modData.Index = dropInfo.InsertIndex;
			//}
		}
	}

	public class MainWindowViewModel : BaseHistoryViewModel, ISupportsActivation
	{
		public string Title => "Divinity Mod Manager 1.0.0.0";

		protected SourceCache<DivinityModData, string> mods = new SourceCache<DivinityModData, string>(m => m.UUID);

		protected ReadOnlyObservableCollection<DivinityModData> allMods;
		public ReadOnlyObservableCollection<DivinityModData> Mods => allMods;

		private SourceList<DivinityProfileData> profiles = new SourceList<DivinityProfileData>();

		public ObservableCollection<DivinityModData> ActiveMods { get; set; } = new ObservableCollection<DivinityModData>();
		public ObservableCollection<DivinityModData> InactiveMods { get; set; } = new ObservableCollection<DivinityModData>();

		public ObservableCollectionExtended<DivinityProfileData> Profiles { get; set; } = new ObservableCollectionExtended<DivinityProfileData>();

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

		public ViewModelActivator Activator { get; }

		private Window view;

		public DivinityModManagerSettings Settings { get; set; }

		public ICommand SaveOrderCommand { get; set; }
		public ICommand SaveOrderAsCommand { get; set; }
		public ICommand ExportOrderCommand { get; set; }
		public ICommand AddOrderConfigCommand { get; set; }
		public ICommand RefreshCommand { get; set; }
		public ICommand DebugCommand { get; set; }

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
			foreach(var uuid in p.ModOrder)
			{
				var modData = Mods.FirstOrDefault(m => m.UUID == uuid);
				if(modData != null)
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
			mods.AddOrUpdate(newMod);
		}

		public void AddMods(IEnumerable<DivinityModData> newMods)
		{
			mods.AddOrUpdate(newMods);
		}
#endif

		private bool LoadSettings()
		{
			string settingsFile = @"Data\settings.json";
			try
			{
				if (File.Exists(settingsFile))
				{
					using (var reader = File.OpenText(settingsFile))
					{
						var fileText = reader.ReadToEnd();
						Settings = JsonConvert.DeserializeObject<DivinityModManagerSettings>(fileText);
					}
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Error loading settings at '{settingsFile}': {ex.ToString()}");
			}
			if (Settings == null)
			{
				Settings = new DivinityModManagerSettings();
				SaveSettings();
			}
			else
			{
				return true;
			}
			return false;
		}

		public bool SaveSettings()
		{
			string settingsFile = @"Data\settings.json";

			try
			{
				Directory.CreateDirectory("Data");

				string contents = JsonConvert.SerializeObject(Settings, Newtonsoft.Json.Formatting.Indented);
				File.WriteAllText(settingsFile, contents);
				return true;
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Error saving settings at '{settingsFile}': {ex.ToString()}");
			}
			return false;
		}

		public void LoadMods()
		{
			string documentsFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			string modPakFolder = (Path.Combine(documentsFolder, @"Larian Studios\Divinity Original Sin 2 Definitive Edition\Mods"));

			List<DivinityModData> modPakData = null;
			List<DivinityModData> projects = null;

			if(Directory.Exists(modPakFolder))
			{
				Trace.WriteLine($"Loading mods from '{modPakFolder}'.");
				modPakData = DivinityModDataLoader.LoadModPackageData(modPakFolder);
			}

			if (Directory.Exists(Settings.GameDataPath))
			{
				string modsDirectory = Path.Combine(Settings.GameDataPath, "Mods");
				if(Directory.Exists(modsDirectory))
				{
					Trace.WriteLine($"Loading mod projects from '{modsDirectory}'.");
					projects = DivinityModDataLoader.LoadEditorProjects(modsDirectory);
				}
			}

			if(modPakData == null)
			{
				modPakData = new List<DivinityModData>();
			}

			if(projects == null)
			{
				projects = new List<DivinityModData>();
			}

			var finalMods = projects.Concat(modPakData.Where(m => !projects.Any(p => p.UUID == m.UUID))).OrderBy(m => m.Name);

			mods.Clear();
			mods.AddOrUpdate(finalMods);

			Trace.WriteLine($"Loaded '{mods.Count}' mods.");

			//foreach(var mod in mods.Items.Where(m => m.HasDependencies))
			//{
			//	for(var i = 0; i < mod.Dependencies.Count;i++)
			//	{
			//		DivinityModDependencyData dependencyData = mod.Dependencies[i];
			//		dependencyData.IsAvailable = mods.Keys.Any(k => k == dependencyData.UUID) || DivinityModDataLoader.IgnoredMods.Any(im => im.UUID == dependencyData.UUID);
			//	}
			//}
		}

		public bool ModIsAvailable(IDivinityModData divinityModData)
		{
			return mods.Keys.Any(k => k == divinityModData.UUID) || DivinityModDataLoader.IgnoredMods.Any(im => im.UUID == divinityModData.UUID);
		}

		public void LoadProfiles()
		{
			string documentsFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			string profileFolder = (Path.Combine(documentsFolder, @"Larian Studios\Divinity Original Sin 2 Definitive Edition\PlayerProfiles"));

			if(Directory.Exists(profileFolder))
			{
				Trace.WriteLine($"Loading profiles from '{profileFolder}'.");

				var profiles = DivinityModDataLoader.LoadProfileData(profileFolder);
				Profiles.AddRange(profiles);

				Trace.WriteLine($"Loaded '{Profiles.Count}' profiles.");

				var selectedUUID = DivinityModDataLoader.GetSelectedProfileUUID(profileFolder);
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
				Trace.WriteLine($"Larian DOS2DE profile folder not found at '{profileFolder}'.");
			}
		}

		public void BuildModOrderList()
		{
			if (SelectedProfile != null)
			{
				if (SelectedProfile.SavedLoadOrder == null)
				{
					DivinityLoadOrder currentOrder = new DivinityLoadOrder() { Name = "Current" };

					foreach (var uuid in SelectedProfile.ModOrder)
					{
						var mod = mods.Items.FirstOrDefault(m => m.UUID == uuid);
						if (mod != null)
						{
							currentOrder.Order.Add(mod.ToOrderEntry());
						}
					}

					SelectedProfile.SavedLoadOrder = currentOrder;
				}

				ModOrderList.Clear();
				ModOrderList.Add(SelectedProfile.SavedLoadOrder);
				ModOrderList.AddRange(SavedModOrderList);
				SelectedModOrderIndex = 0;

				Trace.WriteLine($"{SelectedProfile.SavedLoadOrder.Name}");

				//LoadModOrder(SelectedProfile.SavedLoadOrder);
			}
		}

		private int GetModOrder(DivinityModData mod, DivinityLoadOrder loadOrder)
		{
			var entry = loadOrder.Order.FirstOrDefault(o => o.UUID == mod.UUID);
			int index = -1;
			if(mod != null)
			{
				index = loadOrder.Order.IndexOf(entry);
			}
			return index > -1 ? index : 99999999;
		}

		private void AddNewOrderConfig()
		{
			var lastIndex = SelectedModOrderIndex;
			var lastOrders = ModOrderList.ToList();

			var nextOrders = new List<DivinityLoadOrder>();
			nextOrders.Add(SelectedProfile.SavedLoadOrder);
			nextOrders.AddRange(SavedModOrderList);

			void undo()
			{
				ModOrderList.Clear();
				ModOrderList.AddRange(lastOrders);
				SelectedModOrderIndex = lastIndex;
			};

			void redo()
			{
				ModOrderList.Clear();
				ModOrderList.AddRange(nextOrders);

				DivinityLoadOrder newOrder = new DivinityLoadOrder()
				{
					Name = "New" + nextOrders.Count,
					Order = ActiveMods.Select(m => m.ToOrderEntry()).ToList()
				};

				ModOrderList.Add(newOrder);

				SelectedModOrderIndex = ModOrderList.IndexOf(newOrder);
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
			

			IEnumerable<DivinityLoadOrderEntry> loadFrom = order.Order;

			//if(order.SavedOrder != null)
			//{
			//	loadFrom = order.SavedOrder;
			//}

			foreach(var entry in loadFrom)
			{
				var mod = mods.Items.FirstOrDefault(m => m.UUID == entry.UUID);
				if (mod != null)
				{
					ActiveMods.Add(mod);
				}
			}

			List<DivinityModData> inactive = new List<DivinityModData>();

			foreach (var mod in mods.Items)
			{
				if(ActiveMods.Any(m => m.UUID == mod.UUID))
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
			//order.CreateActiveOrderBind(ActiveMods.ToObservableChangeSet());

			//orderedMods.ForEach(m => {
			//	m.IsActive = true;
			//	Trace.WriteLine($"Mod {m.Name} is active.");
			//});

			//unOrderedMods.ForEach(m => {
			//	m.IsActive = false;
			//	Trace.WriteLine($"Mod {m.Name} is inactive.");
			//});

			Trace.WriteLine($"Loaded mod order: {order.Name}");

			LoadingOrder = false;
		}

		public void Refresh()
		{
			Trace.WriteLine($"Refreshing view.");
			mods.Clear();
			Profiles.Clear();
			LoadMods();
			LoadProfiles();
			BuildModOrderList();
		}

		private void ActiveMods_SetItemIndex(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			//Trace.WriteLine($"[ActiveMods_SetItemIndex] Active collection changed. New({e.NewItems}) Old({e.OldItems}) Action({e.Action})");
			foreach (var mod in ActiveMods)
			{
				mod.Index = ActiveMods.IndexOf(mod);
			}

			if(SelectedModOrder != null && !LoadingOrder)
			{
				if(e.OldItems != null)
				{
					//Trace.WriteLine($"[ActiveMods_SetItemIndex] Removing {e.OldItems.Count} old items from order.");
					foreach (DivinityModData m in e.OldItems)
					{
						SelectedModOrder.Order.Remove(SelectedModOrder.Order.FirstOrDefault(x => x.UUID == m.UUID));
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
			}
		}

		private async Task<bool> SaveLoadOrder()
		{
			bool result = false;
			if (SelectedProfile != null && SelectedModOrder != null)
			{
				if (!Directory.Exists(Settings.LoadOrderPath))
				{
					Directory.CreateDirectory(Settings.LoadOrderPath);
				}

				string outputName = DivinityModDataLoader.MakeSafeFilename(Path.Combine(SelectedModOrder.Name + ".json"), '_');
				if (SelectedModOrder.Name.Equals("Current", StringComparison.OrdinalIgnoreCase))
				{
					outputName = DivinityModDataLoader.MakeSafeFilename(Path.Combine($"{SelectedProfile.Name}_{SelectedModOrder.Name}.json"), '_');
					DivinityLoadOrder tempOrder = SelectedModOrder.Clone();
					tempOrder.Name = $"Current ({SelectedProfile.Name})";

					result = await DivinityModDataLoader.ExportLoadOrderToFileAsync(outputName, tempOrder);
				}
				else
				{
					result = await DivinityModDataLoader.ExportLoadOrderToFileAsync(outputName, SelectedModOrder);
				}

				if (result)
				{
					StatusText = $"Saved mod load order to '{outputName}'";
				}
				else
				{
					StatusText = $"Failed to save mod load order to '{outputName}'";
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
					StatusText = $"Saved mod load order to '{dialog.FileName}'";
				}
				else
				{
					StatusText = $"Failed to save mod load order to '{dialog.FileName}'";
				}
			}
		}
		private async Task<bool> ExportLoadOrder()
		{
			if (SelectedProfile != null && SelectedModOrder != null)
			{
				string outputPath = Path.Combine(SelectedProfile.Folder, "modsettings.lsx");
				var result = await DivinityModDataLoader.ExportModSettingsToFileAsync(SelectedProfile.Folder, SelectedModOrder, mods.Items);
				if(result)
				{
					//MessageBox.Show(view, $"Saved mod settings to '{outputPath}'", "Mod Order Exported");
					StatusText = $"Exported load order to '{outputPath}'";
				}
				else
				{
					string msg = $"Problem exporting load order to '{outputPath}'";
					StatusText = msg;
					MessageBox.Show(view, msg, "Mod Order Export Failed");
				}
			}
			return false;
		}

		private bool ActiveModsChanging(DivinityModData m)
		{
			Trace.WriteLine($"[ActiveModsChanging] Mod {m.Name}");
			return true;
		}

		private void HandleActivation()
		{
			var canExecuteSaveCommand = this.WhenAnyValue(x => x.CanSaveOrder, (canSave) => canSave == true);
			SaveOrderCommand = ReactiveCommand.CreateFromTask(SaveLoadOrder, canExecuteSaveCommand);
			SaveOrderAsCommand = ReactiveCommand.Create(SaveLoadOrderAs, canExecuteSaveCommand);

			ExportOrderCommand = ReactiveCommand.CreateFromTask(ExportLoadOrder);

			AddOrderConfigCommand = ReactiveCommand.Create(AddNewOrderConfig);
		}

		private void HandleDeactivation()
		{

		}

		public ModListDropHandler DropHandler { get; set; } = new ModListDropHandler();

		public void OnViewActivated(Window parentView)
		{
			view = parentView;
			LoadSettings();
			Refresh();
		}

		public MainWindowViewModel() : base()
		{
			Activator = new ViewModelActivator();

			this.WhenActivated(disposables =>
			{
				this.HandleActivation();

				Disposable.Create(() => this.HandleDeactivation()).DisposeWith(disposables);

				//activeModOrder.Preview().OnItemAdded((item) =>
				//{
				//	Trace.WriteLine("Taking snapshot of mod order");
				//	var mods = activeModOrder.Items;
				//	History.Snapshot(() =>
				//	{
				//		activeModOrder.Clear();
				//		activeModOrder.AddRange(mods);
				//		InactiveMods.Add(item);
				//	}, () =>
				//	{
				//		activeModOrder.Add(item);
				//	});
				//}).Bind(ActiveModOrder).Subscribe(o =>
				//{
				//	Trace.WriteLine($"[Preview().OnItemAdded] Changeset: {String.Join(",", o)}");
				//}).DisposeWith(disposables);

				var indexChanged = this.WhenAnyValue(vm => vm.SelectedModOrderIndex);
				indexChanged.Subscribe((selectedOrder) => {
					if (SelectedModOrderIndex > -1)
					{
						LoadModOrder(SelectedModOrder);
					}
				}).DisposeWith(disposables);

				this.WhenAnyValue(vm => vm.SelectedProfileIndex, (index) => index > -1 && index < Profiles.Count).Subscribe((b) =>
				{
					if (b)
					{
						BuildModOrderList();
					}
				}).DisposeWith(disposables);

				mods.Connect().Bind(out allMods).DisposeMany().Subscribe().DisposeWith(disposables);

				RxApp.MainThreadScheduler.Schedule(async () =>
				{
					if(String.IsNullOrWhiteSpace(Settings.LoadOrderPath))
					{
						//Settings.LoadOrderPath = Path.Combine(Path.GetFullPath(System.AppDomain.CurrentDomain.BaseDirectory), @"Data\ModOrder");
						Settings.LoadOrderPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, @"Data\ModOrder");
					}

					string loadOrderDirectory = Path.GetFullPath(Settings.LoadOrderPath);

					Trace.WriteLine($"Attempting to load saved load orders from '{loadOrderDirectory}'.");
					SavedModOrderList = await DivinityModDataLoader.FindLoadOrderFilesInDirectoryAsync(loadOrderDirectory);
					if (SavedModOrderList.Count > 0)
					{
						Trace.WriteLine($"{SavedModOrderList.Count} load orders found. Building mod order list.");
						BuildModOrderList();
					}
					else
					{
						Trace.WriteLine("No saved orders found.");
					}
				});
			});

			/*
			var sortActiveMods = SortExpressionComparer<DivinityModData>.Ascending(m => m.Index);
			var sortInactiveMods = SortExpressionComparer<DivinityModData>.Ascending(m => m.Name);

			mods.Connect().Bind(out modList).DisposeMany().Subscribe();

			var shouldResort = mods.Connect().WhenPropertyChanged(x => x.Index).Throttle(TimeSpan.FromMilliseconds(250)).Select(_ => Unit.Default);

			mods.Connect().AutoRefresh(m => m.IsActive).Filter(m => m.IsActive).Sort(sortActiveMods, shouldResort).ObserveOn(RxApp.MainThreadScheduler).Bind(activeMods).Subscribe(c =>
			{
				//foreach(var obj in c.SortedItems)
				//{

				//}
				//foreach (var obj in mods.Items)
				//{
				//	obj.Index = ActiveMods.IndexOf(obj);
				//}
			});
			mods.Connect().AutoRefresh(m => m.IsActive).Filter(m => !m.IsActive).Sort(sortInactiveMods, shouldResort).ObserveOn(RxApp.MainThreadScheduler).Bind(inactiveMods).Subscribe(c =>
			{
				//foreach (var obj in mods.Items)
				//{
				//	obj.Index = InactiveMods.IndexOf(obj);
				//}
			});

			ActiveMods.WhenAnyValue(x => x.Count).Throttle(TimeSpan.FromMilliseconds(2000)).Subscribe(x =>
			{
				if (x > lastCount)
				{
					foreach (var item in ActiveMods)
					{
						item.Index = ActiveMods.IndexOf(item);
					}
					lastCount = x;
				}
			});
			*/

			this.WhenAnyValue(x => x.SelectedProfileIndex, x => x.Profiles.Count, (index, count) => index >= 0 && count > 0 && index < count).Where(b => b == true).
				Select(x => Profiles[SelectedProfileIndex]).ToProperty(this, x => x.SelectedProfile, out selectedprofile);

			var selectedOrderObservable = this.WhenAnyValue(x => x.SelectedModOrderIndex, x => x.ModOrderList.Count, 
				(index, count) => index >= 0 && count > 0 && index < count).Where(b => b == true);

			selectedOrderObservable.Select(x => ModOrderList[SelectedModOrderIndex]).ToProperty(this, x => x.SelectedModOrder, out selectedModOrder);
			//selectedOrderObservable.Select(x => ModOrderList[SelectedModOrderIndex].DisplayName).ToProperty(this, x => x.SelectedModOrderDisplayName, out selectedModOrderDisplayName);
			

			DebugCommand = ReactiveCommand.Create(() => InactiveMods.Add(new DivinityModData() { Name = "Test" }));

			ActiveMods.CollectionChanged += ActiveMods_SetItemIndex;
			InactiveMods.CollectionChanged += InactiveMods_SetItemIndex;

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
