using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace DivinityModManager.Models
{
	public struct DivinityModDependency
	{
		public string UUID { get; set; }
		public string Name { get; set; }
		public string Version { get; set; }

		public override string ToString()
		{
			return $"Dependency|Name({Name}) UUID({UUID}) Version({Version})";
		}
	}

	public class DivinityModData : ReactiveObject
	{
		public string UUID { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string Author { get; set; }
		public string Version { get; set; }
		public string Folder { get; set; }

		public List<DivinityModDependency> Dependencies { get; set; } = new List<DivinityModDependency>();
	}
}
