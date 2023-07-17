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
		/// <summary>
		/// The path to the root game folder, i.e. SteamLibrary\steamapps\common\Divinity Original Sin 2
		/// </summary>
		[Reactive] public string InstallPath { get; set; }

		/// <summary>
		/// The path to Documents\Larian Studios\Divinity Original Sin 2 Definitive Edition
		/// </summary>
		[Reactive] public string LarianDocumentsFolder { get; set; }

		/// <summary>
		/// The path to Documents\Larian Studios\Divinity Original Sin 2 Definitive Edition\Mods
		/// </summary>
		[Reactive] public string DocumentsModsPath { get; set; }

		/// <summary>
		/// The path to Documents\Larian Studios\Divinity Original Sin 2 Definitive Edition\PlayerProfiles
		/// </summary>
		[Reactive] public string DocumentsProfilesPath { get; set; }

		/// <summary>
		/// The path to Documents\Larian Studios\Divinity Original Sin 2 Definitive Edition\GMCampaigns
		/// </summary>
		[Reactive] public string DocumentsGMCampaignsPath { get; set; }

		[Reactive] public string LastSaveFilePath { get; set; }

		[Reactive] public string ScriptExtenderLatestReleaseUrl { get; set; }
		[Reactive] public string ScriptExtenderLatestReleaseVersion { get; set; }

		public DivinityPathwayData()
		{
			InstallPath = "";
			LarianDocumentsFolder = "";
			DocumentsModsPath = "";
			DocumentsGMCampaignsPath = "";
			LastSaveFilePath = "";
			ScriptExtenderLatestReleaseUrl = "";
			ScriptExtenderLatestReleaseVersion = "";
		}

		public string ScriptExtenderSettingsFile(DivinityModManagerSettings settings)
		{
			if(settings.GameExecutablePath.IsExistingFile())
			{
				return Path.Combine(Path.GetDirectoryName(settings.GameExecutablePath), "ScriptExtenderSettings.json");
			}
			return "";
		}
	}
}
