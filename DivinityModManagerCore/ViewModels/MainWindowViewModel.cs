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

namespace DivinityModManager.ViewModels
{
    public class MainWindowViewModel : BaseHistoryViewModel
    {
		public string Title => "Divinity Mod Manager 1.0.0.0";

		public string Greeting => "Hello World!";

		public List<DivinityModData> Mods { get; set; } = new List<DivinityModData>();

		private SourceList<DivinityModData> activeModOrder = new SourceList<DivinityModData>();
		private SourceList<DivinityModData> inactiveMods = new SourceList<DivinityModData>();
		private SourceList<DivinityProfileData> profiles = new SourceList<DivinityProfileData>();


		public ObservableCollectionExtended<DivinityModData> ActiveModOrder { get; set; } = new ObservableCollectionExtended<DivinityModData>();

		public ObservableCollectionExtended<DivinityModData> InactiveMods { get; set; } = new ObservableCollectionExtended<DivinityModData>();

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

		public DivinityProfileData SelectedProfile { get => Profiles[SelectedModOrderIndex]; }

		public ObservableCollectionExtended<DivinityLoadOrder> ModOrderList { get; set; } = new ObservableCollectionExtended<DivinityLoadOrder>();

		private int selectedModOrderIndex = 0;

		public int SelectedModOrderIndex
		{
			get => selectedModOrderIndex;
			set
			{
				this.RaiseAndSetIfChanged(ref selectedModOrderIndex, value);
				this.RaisePropertyChanged("SelectedModOrder");
			}
		}

		public DivinityLoadOrder SelectedModOrder
		{
			get
			{
				return (SelectedModOrderIndex < ModOrderList.Count && SelectedModOrderIndex >= 0) ? ModOrderList[SelectedModOrderIndex] : null;
			}
		}

		public List<DivinityLoadOrder> SavedModOrderList { get; set; } = new List<DivinityLoadOrder>();

		private int maxOrderIndex = 0;

		public int MaxOrderIndex
		{
			get => maxOrderIndex;
			set { this.RaiseAndSetIfChanged(ref maxOrderIndex, value); }
		}

		public static int StaticMaxOrderIndex { get; set; } = 30;

		private int layoutMode = 0;

		public int LayoutMode
		{
			get => layoutMode;
			set { this.RaiseAndSetIfChanged(ref layoutMode, value); }
		}

		private bool canSaveOrder = false;

		public bool CanSaveOrder
		{
			get => canSaveOrder;
			set { this.RaiseAndSetIfChanged(ref canSaveOrder, value); }
		}

		public ICommand SaveOrderCommand { get; set; }
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

		public void LoadMods()
		{
			var projects = DivinityModDataLoader.LoadEditorProjects(@"G:\Divinity Original Sin 2\DefEd\Data\Mods");
			Mods.AddRange(projects);
			//traceNewMods(projects);

			var modPakData = DivinityModDataLoader.LoadModPackageData(@"D:\Users\LaughingLeader\Documents\Larian Studios\Divinity Original Sin 2 Definitive Edition\Mods");
			var pakEditorConflicts = modPakData.Where(m => projects.Any(p => p.UUID == m.UUID));
			//Editor mods have priority over paks
			Mods.AddRange(modPakData.Where(m => !pakEditorConflicts.Contains(m)));
			//traceNewMods(modPakData);

			Mods = Mods.OrderBy(m => m.Name).ToList();
		}

		public void LoadProfiles()
		{
			var profiles = DivinityModDataLoader.LoadProfileData(@"D:\Users\LaughingLeader\Documents\Larian Studios\Divinity Original Sin 2 Definitive Edition\PlayerProfiles");
			Profiles.AddRange(profiles);

			var selectedUUID = DivinityModDataLoader.GetSelectedProfileUUID(@"D:\Users\LaughingLeader\Documents\Larian Studios\Divinity Original Sin 2 Definitive Edition\PlayerProfiles");
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

		public void BuildModOrderList()
		{
			if (SelectedProfile != null)
			{
				ModOrderList.Clear();
				DivinityLoadOrder currentOrder = new DivinityLoadOrder() { Name = "Current" };
				foreach (var uuid in SelectedProfile.ModOrder)
				{
					var modData = Mods.FirstOrDefault(m => m.UUID == uuid);
					if (modData != null)
					{
						currentOrder.Order.AddOrUpdate(modData);
					}
				}
				ModOrderList.Add(currentOrder);
				ModOrderList.AddRange(SavedModOrderList);
				SelectedModOrderIndex = 0;
			}
		}

		private int GetModOrder(int indexVal)
		{
			return indexVal > -1 ? indexVal : 99999999;
		}

		public void OrderModsByModOrder(DivinityLoadOrder order)
		{
			var orderedMods = Mods.Where(m => order.Order.Items.Contains(m));
			var unOrderedMods = Mods.Where(m => !order.Order.Items.Contains(m)).ToList();
			var sorted = orderedMods.OrderBy(m => GetModOrder(order.Order.Items.IndexOf(m))).ToList();

			for(var i = 0; i < sorted.Count; i++)
			{
				var m = sorted[i];
				m.Index = i;
			}

			unOrderedMods.ForEach(m => m.Index = -1);

			ActiveModOrder.Clear();
			ActiveModOrder.AddRange(sorted);

			InactiveMods.Clear();
			InactiveMods.AddRange(unOrderedMods.OrderBy(m => m.Name));

			MaxOrderIndex = ActiveModOrder.Count - 1;
			StaticMaxOrderIndex = MaxOrderIndex;

			Trace.WriteLine($"MaxOrderIndex is {MaxOrderIndex}");
		}

		public void Refresh()
		{
			Mods.Clear();
			Profiles.Clear();
			LoadMods();
			LoadProfiles();
			BuildModOrderList();
		}

		private void ActiveMods_SetItemIndex(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if(e.OldItems != null && e.NewItems != null)
			{
				
			}

			if(e.NewItems != null)
			{
				foreach (DivinityModData item in ActiveModOrder)
				{
					item.Index = ActiveModOrder.IndexOf(item);
				}
			}
		}

		private void InactiveMods_SetItemIndex(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
			{
				foreach (DivinityModData item in InactiveMods)
				{
					item.Index = InactiveMods.IndexOf(item);
				}
			}
		}

		private async Task<bool> SaveLoadOrderToFile()
		{
			var loadOrder = SelectedModOrder;
			var profile = SelectedProfile;
			if (loadOrder != null && profile != null)
			{
				loadOrder.Order.Clear();
				foreach (var mod in ActiveModOrder)
				{
					loadOrder.Order.AddOrUpdate(mod);
				}

				var result = await DivinityModDataLoader.SaveModSettings(profile.Folder, loadOrder, Mods);
				if (result)
				{
					Trace.WriteLine($"Saved modsettings.lsx to '{profile.Folder}'.");
					return true;
				}
				else
				{
					Trace.WriteLine($"Failed to save modsettings.lsx to '{profile.Folder}'.");
				}
			}

			return false;
		}

		private bool ActiveModsChanging(DivinityModData m)
		{
			Trace.WriteLine($"[ActiveModsChanging] Mod {m.Name}");
			return true;
		}

		public MainWindowViewModel() : base()
		{
			var indexChanged = this.WhenAnyValue(vm => vm.SelectedModOrder);
			indexChanged.Subscribe((selectedOrder) => {
				if(selectedOrder != null)
				{
					OrderModsByModOrder(selectedOrder);
				}
			});

			activeModOrder.Preview().OnItemAdded((item) =>
			{
				Trace.WriteLine("Taking snapshot of mod order");
				var mods = activeModOrder.Items;
				History.Snapshot(() =>
				{
					activeModOrder.Clear();
					activeModOrder.AddRange(mods);
					InactiveMods.Add(item);
				}, () =>
				{
					activeModOrder.Add(item);
				});
			}).Bind(ActiveModOrder).Subscribe(o => {
				Trace.WriteLine($"[Preview().OnItemAdded] Changeset: {String.Join(",", o)}");
			});

			DebugCommand = ReactiveCommand.Create(() => activeModOrder.Add(new DivinityModData() { Name = "Test" }));

			//var connection = activeModOrder.Connect(ActiveModsChanging);
			//var b = connection.ObserveOn(RxApp.MainThreadScheduler).Bind(ActiveModOrder);

			//ActiveModOrder.CollectionChanged += ActiveMods_SetItemIndex;
			//InactiveMods.CollectionChanged += InactiveMods_SetItemIndex;

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

			var canExecuteSaveCommand = this.WhenAnyValue(x => x.CanSaveOrder, (canSave) => canSave == true);
			SaveOrderCommand = ReactiveCommand.CreateFromTask(SaveLoadOrderToFile, canExecuteSaveCommand);
		}
    }
}
