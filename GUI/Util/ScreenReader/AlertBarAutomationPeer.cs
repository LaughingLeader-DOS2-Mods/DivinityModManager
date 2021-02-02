using DivinityModManager.Controls;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace DivinityModManager.Util.ScreenReader
{
	public class AlertBarAutomationPeer : FrameworkElementAutomationPeer
	{
		private AlertBar alertBar;

		public AlertBarAutomationPeer(AlertBar owner) : base(owner)
		{
			alertBar = owner;
		}
		protected override string GetNameCore()
		{
			return alertBar.GetText();
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.StatusBar;
		}

		protected override List<AutomationPeer> GetChildrenCore()
		{
			List<AutomationPeer> peers = new List<AutomationPeer>();
			var textElements = alertBar.GetTextElements();
			if(textElements.Count > 0)
			{
				foreach(var element in textElements)
				{
					var peer = UIElementAutomationPeer.CreatePeerForElement(element);
					if(peer != null)
					{
						peers.Add(peer);
					}
				}
			}
			return peers;
		}
	}
}
