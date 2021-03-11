using DivinityModManager.Util.ScreenReader;

using System.Reflection;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;

namespace DivinityModManager.Controls
{
	public class ModListView : ListView
	{
		private MethodInfo getInfoMethod;
		private MethodInfo updateAnchorMethod;

		public ModListView() : base() 
		{
			getInfoMethod = typeof(ItemsControl).GetMethod("ItemInfoFromContainer", BindingFlags.NonPublic | BindingFlags.Instance);
			updateAnchorMethod = typeof(ListBox).GetMethod("UpdateAnchorAndActionItem", BindingFlags.NonPublic | BindingFlags.Instance);
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new ModListViewAutomationPeer(this);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			// Fixes CTRL + Arrow keys not updating the anchored item, which then causes Shift selection to select everything between the new and old focused items
			switch (e.Key)
			{
				case Key.Up:
				case Key.Down:
					if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
					{
						var info = getInfoMethod.Invoke(this, new object[] { Keyboard.FocusedElement });
						updateAnchorMethod.Invoke(this, new object[] { info });
					}
					break;
			}
		}
	}
}
