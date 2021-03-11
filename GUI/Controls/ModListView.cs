using DivinityModManager.Models;
using DivinityModManager.Util.ScreenReader;

using DynamicData.Binding;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
			bool handled = false;

			if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && ItemsSource is ObservableCollectionExtended<DivinityModData> list)
			{
				switch (e.SystemKey)
				{
					case Key.Up:
					case Key.Down:
						var selectedItems = list.Where(x => x.IsSelected).ToList();
						var lastIndexes = selectedItems.ToDictionary(m => m.UUID, m => m.Index);
						int nextIndex = -1;

						if (e.SystemKey == Key.Up)
						{
							for(int i = 0; i < selectedItems.Count; i++)
							{
								var m = selectedItems[i];
								nextIndex = Math.Max(0, m.Index - 1);
								var existingMod = list.ElementAtOrDefault(nextIndex);
								if (existingMod != null && existingMod.IsSelected)
								{
									var lastIndex = lastIndexes[existingMod.UUID];
									if (existingMod.Index == lastIndex)
									{
										// The selected mod at the target index
										// didn't get moved up/down, so skip moving the next one
										continue;
									}
								}
								list.Move(m.Index, nextIndex);
							}
						}
						else
						{
							for (int i = selectedItems.Count - 1; i >= 0; i--)
							{
								var m = selectedItems[i];
								nextIndex = Math.Min(list.Count - 1, m.Index + 1);
								var existingMod = list.ElementAtOrDefault(nextIndex);
								if (existingMod != null && existingMod.IsSelected)
								{
									var lastIndex = lastIndexes[existingMod.UUID];
									if (existingMod.Index == lastIndex)
									{
										continue;
									}
								}
								list.Move(m.Index, nextIndex);
							}
						}
						
						handled = true;
						break;
				}
			}

			if(!handled)
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
}
