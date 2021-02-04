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
		public bool AddSeparator { get; set; }
		public string Tooltip { get; set; }
		public string Style { get; set; }
		public MenuSettingsAttribute(string parent = "", string displayName = "", bool addSeparatorAfter = false, string tooltip = "")
		{
			DisplayName = displayName;
			Parent = parent;
			AddSeparator = addSeparatorAfter;
			Tooltip = tooltip;
		}
	}
}
