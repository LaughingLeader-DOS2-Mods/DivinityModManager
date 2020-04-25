using DivinityModManager.Models;
using DynamicData.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.ViewModels
{
	public interface IDivinityAppViewModel
	{
		ObservableCollectionExtended<DivinityModData> ActiveMods { get; set; }
		ObservableCollectionExtended<DivinityModData> InactiveMods { get; set; }
		ObservableCollectionExtended<DivinityProfileData> Profiles { get; set; }

		int ActiveSelected { get; }
		int InactiveSelected { get; }

		void ShowAlert(string message, int alertType = 0, int timeout = 0);
		void ClearMissingMods();
	}
}
