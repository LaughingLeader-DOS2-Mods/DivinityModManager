using DivinityModManager.Views;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;

namespace DivinityModManager.Util.ScreenReader
{
	public class MainWindowAutomationPeer : CachedAutomationPeer
	{
		private MainWindow mainWindow;
		public MainWindowAutomationPeer(MainWindow owner) : base(owner)
		{
			mainWindow = owner;
		}

		protected override string GetNameCore()
		{
			if(mainWindow.ViewModel != null)
			{
				return mainWindow.ViewModel.Title;
			}
			else
			{
				return "Divinity Mod Manager";
			}
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Window;
		}
	}
}
