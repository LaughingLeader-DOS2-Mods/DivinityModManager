using LSLib.LS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	public class DivinityProfileActiveModData
	{
		public string Folder { get; set; }
		public string MD5 { get; set; }
		public string Name { get; set; }
		public string UUID { get; set; }
		public int Version { get; set; }

		private T GetAttribute<T>(Dictionary<string, NodeAttribute> attributes, string name, T fallBack)
		{
			if (attributes.TryGetValue(name, out var attribute))
			{
				var val = (T)attribute.Value;
				if (val != null)
				{
					return val;
				}
			}
			return fallBack;
		}

		public void LoadFromAttributes(Dictionary<string, NodeAttribute> attributes)
		{
			Folder = GetAttribute<string>(attributes, "Folder", "");
			MD5 = GetAttribute<string>(attributes, "MD5", "");
			Name = GetAttribute<string>(attributes, "Name", "");
			UUID = GetAttribute<string>(attributes, "UUID", "");
			Version = GetAttribute<int>(attributes, "Version", -1);

			//DivinityApp.LogMessage($"[DivinityProfileActiveModData] Name({Name}) UUID({UUID})");
		}
    }
}
