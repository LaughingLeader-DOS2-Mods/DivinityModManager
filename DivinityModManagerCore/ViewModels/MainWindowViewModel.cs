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

namespace DivinityModManager.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
		public string Title => "Divinity Mod Manager 1.0.0.0";

		public string Greeting => "Hello World!";

		public static MainWindowViewModel self { get; set; }

		public List<DivinityModData> Mods { get; set; } = new List<DivinityModData>();
		public ObservableCollection<IDivinityModListEntry> ModOrder { get; set; } = new ObservableCollection<IDivinityModListEntry>();

		public ObservableCollection<DivinityProfileData> Profiles { get; set; } = new ObservableCollection<DivinityProfileData>();

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

		public ObservableCollection<DivinityLoadOrder> ModOrderList { get; set; } = new ObservableCollection<DivinityLoadOrder>();

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

			ModOrder = new ObservableCollection<IDivinityModListEntry>(sorted);
			ModOrder.Add(new DivinityModSeparator());
			ModOrder.AddRange(unOrderedMods.OrderBy(m => m.Name));

			MaxOrderIndex = ModOrder.Count - 2;
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

		public MainWindowViewModel() : base()
		{
			var indexChanged = this.WhenAnyValue(vm => vm.SelectedModOrder);
			indexChanged.Subscribe((selectedOrder) => {
				if(selectedOrder != null)
				{
					OrderModsByModOrder(selectedOrder);
				}
			});

			self = this;
		}
    }
}
