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
		IEnumerable<DivinityModData> ActiveMods { get; }
		IEnumerable<DivinityModData> InactiveMods { get; }
		ObservableCollectionExtended<DivinityProfileData> Profiles { get; }
		ReadOnlyObservableCollection<DivinityModData> Mods { get; }
		ReadOnlyObservableCollection<DivinityModData> WorkshopMods { get; }

		bool IsDragging { get; }
		bool IsRefreshing { get; }
		bool IsLocked { get; }

		int ActiveSelected { get; }
		int InactiveSelected { get; }

		void ShowAlert(string message, AlertType alertType = AlertType.Info, int timeout = 0);
		void DeleteMod(DivinityModData mod);
		void ClearMissingMods();
	}
}
