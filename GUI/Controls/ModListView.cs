using DivinityModManager.Util.ScreenReader;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace DivinityModManager.Controls
{
	public class ModListView : ListView
	{
		public ModListView() : base() { }

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new ModListViewAutomationPeer(this);
		}
	}
}
