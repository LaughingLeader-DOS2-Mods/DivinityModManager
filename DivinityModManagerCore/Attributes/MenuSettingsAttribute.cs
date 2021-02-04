using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager
{
	public class MenuSettingsAttribute : Attribute
	{
		public string DisplayName { get; set; }
		public string Parent { get; set; }
		public MenuSettingsAttribute(string parent = "", string displayName = "")
		{
			DisplayName = displayName;
			Parent = parent;
		}
	}
}
