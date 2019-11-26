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

		public ObservableCollectionExtended<DivinityModUpdateData> Updates { get; set; } = new ObservableCollectionExtended<DivinityModUpdateData>();
		public ObservableCollectionExtended<DivinityModData> NewMods { get; set; } = new ObservableCollectionExtended<DivinityModData>();

		public void Clear()
		{
			Updates.Clear();
			NewMods.Clear();
		}
	}
}
