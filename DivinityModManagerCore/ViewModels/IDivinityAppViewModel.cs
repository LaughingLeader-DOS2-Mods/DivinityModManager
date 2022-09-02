using DivinityModManager.Models;

using DynamicData.Binding;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.ViewModels
{
	public interface IDivinityAppViewModel
	{
		ObservableCollectionExtended<DivinityModData> ActiveMods { get; }
		ObservableCollectionExtended<DivinityModData> InactiveMods { get; }
		ObservableCollectionExtended<DivinityProfileData> Profiles { get; }
		ReadOnlyObservableCollection<DivinityModData> Mods { get; }
		ReadOnlyObservableCollection<DivinityModData> WorkshopMods { get; }

		int ActiveSelected { get; }
		int InactiveSelected { get; }

		void ShowAlert(string message, AlertType alertType = AlertType.Info, int timeout = 0);
		void ConfirmDeleteMod(DivinityModData mod);
		void ClearMissingMods();
	}
}
