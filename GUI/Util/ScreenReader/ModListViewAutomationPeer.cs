using DivinityModManager.Controls;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace DivinityModManager.Util.ScreenReader
{
	public class ModListViewAutomationPeer : CachedAutomationPeer
	{
		private ModListView _listView;

		public ModListViewAutomationPeer(ModListView owner) : base(owner)
		{
			_listView = owner;
		}

		protected override string GetNameCore()
		{
			return Owner.GetValue(AutomationProperties.NameProperty) as string ?? string.Empty;
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.List;
		}

		override public bool HasNullChildElement()
		{
			foreach (var c in _listView.Items)
			{
				if (c == null)
				{
					DivinityApp.Log("Found a null entry in ModListViewAutomationPeer");
					return true;
				}
			}
			return false;
		}
	}
}
