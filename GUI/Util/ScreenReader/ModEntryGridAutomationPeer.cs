using DivinityModManager.Controls;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace DivinityModManager.Util.ScreenReader
{
	public class ModEntryGridAutomationPeer : CachedAutomationPeer
	{
		private ModEntryGrid grid;
		public ModEntryGridAutomationPeer(ModEntryGrid owner) : base(owner)
		{
			grid = owner;
		}

		protected override string GetNameCore()
		{
			return grid.GetValue(AutomationProperties.NameProperty) as string ?? string.Empty;
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.ListItem;
		}

		private AutomationPeer _textPeer;

		override public bool HasNullChildElement()
		{
			var text = ElementHelper.FindChild<TextBlock>(grid, "ModNameText");
			if (text != null)
			{
				var peer = UIElementAutomationPeer.CreatePeerForElement(text);
				if (peer != null)
				{
					_textPeer = peer;
					return true;
				}
			}
			return true;
		}

		override public List<AutomationPeer> GetPeersFromElements()
		{
			return new List<AutomationPeer>(1) { _textPeer };
		}
	}
}
