using DivinityModManager.Models;
using DivinityModManager.Models.Conflicts;
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
using DivinityModManager.Views;

namespace DivinityModManager.ViewModels
{
	public class ConflictCheckerWindowViewModel : BaseViewModel
	{
		private ConflictCheckerWindow view;
		private MainWindowViewModel mainWindowViewModel;

		public ObservableCollectionExtended<DivinityConflictGroup> ConflictGroups { get; set; } = new ObservableCollectionExtended<DivinityConflictGroup>();

		private int selectedGroupIndex = 0;

		public int SelectedGroupIndex
		{
			get => selectedGroupIndex;
			set { this.RaiseAndSetIfChanged(ref selectedGroupIndex, value); }
		}

		private readonly ObservableAsPropertyHelper<DivinityConflictGroup> selectedGroup;

		public DivinityConflictGroup SelectedGroup => selectedGroup.Value;

		private readonly ObservableAsPropertyHelper<DivinityConflictEntryData> selectedConflictEntry;

		public DivinityConflictEntryData SelectedConflictEntry => selectedConflictEntry.Value;

		#region Profiles & Load Order

		private ReadOnlyObservableCollection<DivinityProfileData> _profiles;
		public ReadOnlyObservableCollection<DivinityProfileData> Profiles => _profiles;

		private ReadOnlyObservableCollection<DivinityProfileData> _loadOrders;
		public ReadOnlyObservableCollection<DivinityProfileData> LoadOrders => _loadOrders;

		private ObservableAsPropertyHelper<DivinityLoadOrder> _selectedLoadOrder;
		public DivinityLoadOrder SelectedLoadOrder => _selectedLoadOrder.Value;

		private int selectedProfileIndex;

		public int SelectedProfileIndex
		{
			get => selectedProfileIndex;
			set { this.RaiseAndSetIfChanged(ref selectedProfileIndex, value); }
		}

		private int selectedLoadOrderIndex;

		public int SelectedLoadOrderIndex
		{
			get => selectedLoadOrderIndex;
			set { this.RaiseAndSetIfChanged(ref selectedLoadOrderIndex, value); }
		}


		#endregion

		public ConflictCheckerWindowViewModel() : base()
		{
			// Selected Group by index
			this.WhenAnyValue(x => x.SelectedGroupIndex).Select(x => x < ConflictGroups.Count ? ConflictGroups[x] : null).
				ToProperty(this, x => x.SelectedGroup, out selectedGroup).DisposeWith(this.Disposables);

			//Selected entry within group, by index
			this.WhenAnyValue(x => x.SelectedGroup.SelectedConflictIndex).Select(x => x < SelectedGroup.Conflicts.Count ? SelectedGroup.Conflicts[x] : null).
				ToProperty(this, x => x.SelectedConflictEntry, out selectedConflictEntry).DisposeWith(this.Disposables);
		}

		public void OnActivated(ConflictCheckerWindow v, MainWindowViewModel vm)
		{
			view = v;
			mainWindowViewModel = vm;

			if(_selectedLoadOrder == null)
			{
				_selectedLoadOrder = mainWindowViewModel.WhenAnyValue(x => x.SelectedModOrder).ToProperty(this, x => x.SelectedLoadOrder);
				//mainWindowViewModel.Profiles.ToObservableChangeSet().Bind(out _profiles);
			}
			
			
			//this.WhenAnyValue(x => x.SelectedGroupIndex)
		}
	}
}
