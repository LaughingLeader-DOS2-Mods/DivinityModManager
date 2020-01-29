using Alphaleonis.Win32.Filesystem;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace DivinityModManager.Models
{
	public class DivinityProfileData : ReactiveObject
	{
		public string Name { get; set; }

		/// <summary>
		/// The stored name in the profile.lsb file.
		/// </summary>
		public string ProfileName { get; set; }
		public string UUID { get; set; }

		private string folder;

		public string Folder
		{
			get => folder;
			set 
			{
				if(value != folder)
				{
					ModSettingsFile = Path.Combine(value, "modsettings.lsx");
				}
				this.RaiseAndSetIfChanged(ref folder, value);
			}
		}

		private string modSettingsFile;

		public string ModSettingsFile
		{
			get => modSettingsFile;
			set { this.RaiseAndSetIfChanged(ref modSettingsFile, value); }
		}

		/// <summary>
		/// The saved load order from modsettings.lsx
		/// </summary>
		public List<string> ModOrder { get; set; } = new List<string>();

		/// <summary>
		/// The mod UUIDs under the Mods node, from modsettings.lsx.
		/// </summary>
		public List<string> ActiveMods { get; set; } = new List<string>();

		/// <summary>
		/// The ModOrder transformed into a DivinityLoadOrder. This is the "Current" order.
		/// </summary>
		public DivinityLoadOrder SavedLoadOrder { get; set; }
	}
}
