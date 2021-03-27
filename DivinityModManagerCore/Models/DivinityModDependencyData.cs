using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	[JsonObject(MemberSerialization.OptIn)]
	public struct DivinityModDependencyData : IDivinityModData
	{
		[JsonProperty] public string UUID { get; set; }
		[JsonProperty] public string Name { get; set; }
		public string Folder { get; set; }
		public string MD5 { get; set; }
		[JsonProperty] public DivinityModVersion Version { get; set; }

		public override string ToString()
		{
			return $"Dependency|Name({Name}) UUID({UUID}) Version({Version?.Version})";
		}

		public static DivinityModDependencyData FromModData(DivinityModData m)
		{
			return new DivinityModDependencyData
			{
				Folder = m.Folder,
				Name = m.Name,
				UUID = m.UUID,
				MD5 = m.MD5,
				Version = m.Version
			};
		}
	}
}
