using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace DivinityModManager.Models
{
	public class DivinityProfileData : ReactiveObject
	{
		public string Name { get; set; }
		public string UUID { get; set; }
		public string Folder { get; set; }

		public List<string> ModOrder { get; set; } = new List<string>();
	}
}
