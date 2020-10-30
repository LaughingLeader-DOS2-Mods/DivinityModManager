using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	public class IgnoredModsData
	{
		public List<string> IgnoreDependencies { get; set; } = new List<string>();
		public List<Dictionary<string,object>> Mods { get; set; } = new List<Dictionary<string, object>>();
	}
}
