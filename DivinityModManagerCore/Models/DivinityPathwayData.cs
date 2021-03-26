using Alphaleonis.Win32.Filesystem;
using DivinityModManager.Extensions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	public class DivinityPathwayData : ReactiveObject
	{
		[Reactive] public string InstallPath { get; set; } = "";
		[Reactive] public string LarianDocumentsFolder { get; set; } = "";
		[Reactive] public string DocumentsModsPath { get; set; } = "";
		[Reactive] public string DocumentsProfilesPath { get; set; } = "";
		[Reactive] public string LastSaveFilePath { get; set; } = "";
		[Reactive] public string DocumentsGMCampaignsPath { get; set; } = "";

		[Reactive] public string OsirisExtenderLatestReleaseUrl { get; set; } = "";
		[Reactive] public string OsirisExtenderLatestReleaseVersion { get; set; } = "";

		public string OsirisExtenderSettingsFile(DivinityModManagerSettings settings)
		{
			if(settings.GameExecutablePath.IsExistingFile())
			{
				return Path.Combine(Path.GetDirectoryName(settings.GameExecutablePath), "OsirisExtenderSettings.json");
			}
			return "";
		}
	}
}
