using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	public struct DivinityModDependencyData : IDivinityModData
	{
		public string UUID { get; set; }
		public string Name { get; set; }
		public string Folder { get; set; }
		public string MD5 { get; set; }
		public DivinityModVersion Version { get; set; }

		public override string ToString()
		{
			return $"Dependency|Name({Name}) UUID({UUID}) Version({Version?.Version})";
		}
	}
}
