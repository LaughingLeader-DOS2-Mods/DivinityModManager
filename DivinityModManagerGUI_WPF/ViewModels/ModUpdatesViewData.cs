using DivinityModManager.Models;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

		public void Clear()
		{
			Updates.Clear();
			NewMods.Clear();

			TotalUpdates = 0;
			NewAvailable = UpdatesAvailable = false;
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

			//this.WhenAnyValue(x => x.NewMods.Count).Subscribe((count) =>
			//{
			//	NewAvailable = count > 0;
			//});

			//this.WhenAnyValue(x => x.Updates.Count).Subscribe((count) =>
			//{
			//	UpdatesAvailable = count > 0;
			//});

			this.WhenAnyValue(x => x.NewMods.Count, x => x.Updates.Count, (a, b) => a + b).BindTo(this, x => x.TotalUpdates);
		}
	}
}
