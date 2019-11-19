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
		public string Folder { get; set; }

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
