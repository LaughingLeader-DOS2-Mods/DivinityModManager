using DivinityModManager.Models;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DivinityModManager.ViewModels
{
	public class ModUpdatesViewData : ReactiveObject
	{
		private bool newAvailable;

		public bool NewAvailable
		{
			get => newAvailable;
			set { this.RaiseAndSetIfChanged(ref newAvailable, value); }
		}

		private bool updatesAvailable;

		public bool UpdatesAvailable
		{
			get => updatesAvailable;
			set { this.RaiseAndSetIfChanged(ref updatesAvailable, value); }
		}

		public ObservableCollectionExtended<DivinityModData> NewMods { get; set; } = new ObservableCollectionExtended<DivinityModData>();
		public ObservableCollectionExtended<DivinityModUpdateData> Updates { get; set; } = new ObservableCollectionExtended<DivinityModUpdateData>();

		private int totalUpdates;

		public int TotalUpdates
		{
			get => totalUpdates;
			set { this.RaiseAndSetIfChanged(ref totalUpdates, value); }
		}

		private bool anySelected = false;

		public bool AnySelected
		{
			get => anySelected;
			set { this.RaiseAndSetIfChanged(ref anySelected, value); }
		}

		public ICommand CopySelectedModsCommand { get; set; }
		public ICommand SelectAllNewModsCommand { get; set; }
		public ICommand SelectAllUpdatesCommand { get; set; }

		public void Clear()
		{
			Updates.Clear();
			NewMods.Clear();

			TotalUpdates = 0;
			NewAvailable = UpdatesAvailable = false;
		}

		public void CopySelectedMods()
		{

		}
		public void SelectAll(bool select)
		{
			foreach(var x in NewMods)
			{
				x.IsSelected = select;
			}
			foreach(var x in Updates)
			{
				x.IsSelected = select;
			}
		}

		public ModUpdatesViewData()
		{
			NewMods.CollectionChanged += delegate
			{
				NewAvailable = NewMods.Count > 0;
			};

			Updates.CollectionChanged += delegate
			{
				UpdatesAvailable = Updates.Count > 0;
			};

			var anySelectedObservable = this.WhenAnyValue(x => x.AnySelected);

			CopySelectedModsCommand = ReactiveCommand.Create(CopySelectedMods, anySelectedObservable);
			SelectAllNewModsCommand = ReactiveCommand.Create<bool>((b) =>
			{
				foreach (var x in NewMods)
				{
					x.IsSelected = b;
				}
			});
			SelectAllUpdatesCommand = ReactiveCommand.Create<bool>((b) =>
			{
				foreach(var x in Updates)
				{
					x.IsSelected = b;
				}
			});

			//this.WhenAnyValue(x => x.NewMods.Count).Subscribe((count) =>
			//{
			//	NewAvailable = count > 0;
			//});

			//this.WhenAnyValue(x => x.Updates.Count).Subscribe((count) =>
			//{
			//	UpdatesAvailable = count > 0;
			//});

			this.WhenAnyValue(x => x.NewMods.Count, x => x.Updates.Count, (a, b) => a + b).BindTo(this, x => x.TotalUpdates);
			NewMods.ToObservableChangeSet().AutoRefresh(x => x.IsSelected).ToCollection().Subscribe((c) =>
			{
				bool nextAnySelected = c.Any(x => x.IsSelected);
				if(!nextAnySelected)
				{
					AnySelected = Updates.Any(x => x.IsSelected);
				}
				else
				{
					AnySelected = true;
				}
			});
			Updates.ToObservableChangeSet().AutoRefresh(x => x.IsSelected).ToCollection().Subscribe((c) =>
			{
				bool nextAnySelected = c.Any(x => x.IsSelected);
				if (!nextAnySelected)
				{
					AnySelected = NewMods.Any(x => x.IsSelected);
				}
				else
				{
					AnySelected = true;
				}
			});
		}
	}
}
