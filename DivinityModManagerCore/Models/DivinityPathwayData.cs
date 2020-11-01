using Alphaleonis.Win32.Filesystem;
using DivinityModManager.Extensions;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	public class DivinityPathwayData : ReactiveObject
	{
		private string installPath = "";

		public string InstallPath
		{
			get => installPath;
			set { this.RaiseAndSetIfChanged(ref installPath, value); }
		}

		private string larianDocumentsFolder = "";

		public string LarianDocumentsFolder
		{
			get => larianDocumentsFolder;
			set { this.RaiseAndSetIfChanged(ref larianDocumentsFolder, value); }
		}

		private string documentsModsPath = "";

		public string DocumentsModsPath
		{
			get => documentsModsPath;
			set { this.RaiseAndSetIfChanged(ref documentsModsPath, value); }
		}

		private string documentsProfilesPath = "";

		public string DocumentsProfilesPath
		{
			get => documentsProfilesPath;
			set { this.RaiseAndSetIfChanged(ref documentsProfilesPath, value); }
		}

		private string lastSaveFilePath = "";

		public string LastSaveFilePath
		{
			get => lastSaveFilePath;
			set { this.RaiseAndSetIfChanged(ref lastSaveFilePath, value); }
		}

		public string OsirisExtenderSettingsFile(DivinityModManagerSettings settings)
		{
			if(settings.GameExecutablePath.IsExistingFile())
			{
				return Path.Combine(Path.GetDirectoryName(settings.GameExecutablePath), "OsirisExtenderSettings.json");
			}
			return "";
		}

		private string osirisExtenderLatestReleaseUrl = "";

		public string OsirisExtenderLatestReleaseUrl
		{
			get => osirisExtenderLatestReleaseUrl;
			set { this.RaiseAndSetIfChanged(ref osirisExtenderLatestReleaseUrl, value); }
		}

		public string OsirisExtenderLatestReleaseVersion { get; set; } = "";
	}
}
