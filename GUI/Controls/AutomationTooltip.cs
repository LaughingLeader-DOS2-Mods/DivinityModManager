using DivinityModManager.Util;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace DivinityModManager.Controls
{
	public class AutomationTooltip : ToolTip
	{
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new AutomationTooltipPeer(this);
		}
	}
}
