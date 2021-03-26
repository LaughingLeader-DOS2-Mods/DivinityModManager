using DivinityModManager.Models;
using DivinityModManager.Util.ScreenReader;

using DynamicData.Binding;

using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Concurrency;
using System.Windows;

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

			DivinityApp.Log($"IsKeyboardFocused({IsKeyboardFocused}) IsKeyboardFocusWithin({IsKeyboardFocusWithin}) IsFocused({IsFocused})");

			if (SelectedItem != null && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && ItemsSource is ObservableCollectionExtended<DivinityModData> list)
			{
				var key = e.SystemKey;
				switch (key)
				{
					case Key.Up:
					case Key.Down:
					case Key.Right:
					case Key.Left:
						var selectedItems = list.Where(x => x.IsSelected).ToList();
						var lastIndexes = selectedItems.ToDictionary(m => m.UUID, m => list.IndexOf(m));
						int nextIndex = -1;
						int targetScrollIndex = -1;

						if (key == Key.Up)
						{
							for (int i = 0; i < selectedItems.Count; i++)
							{
								var m = selectedItems[i];
								int modIndex = list.IndexOf(m);
								nextIndex = Math.Max(0, modIndex - 1);
								var existingMod = list.ElementAtOrDefault(nextIndex);
								if (existingMod != null && existingMod.IsSelected)
								{
									var lastIndex = lastIndexes[existingMod.UUID];
									if (list.IndexOf(existingMod) == lastIndex)
									{
										// The selected mod at the target index
										// didn't get moved up/down, so skip moving the next one
										continue;
									}
								}
								if (targetScrollIndex == -1) targetScrollIndex = nextIndex;
								list.Move(modIndex, nextIndex);
							}
						}
						else if(key == Key.Down)
						{
							for (int i = selectedItems.Count - 1; i >= 0; i--)
							{
								var m = selectedItems[i];
								int modIndex = list.IndexOf(m);
								nextIndex = Math.Min(list.Count - 1, modIndex + 1);
								var existingMod = list.ElementAtOrDefault(nextIndex);
								if (existingMod != null && existingMod.IsSelected)
								{
									var lastIndex = lastIndexes[existingMod.UUID];
									if (list.IndexOf(existingMod) == lastIndex)
									{
										continue;
									}
								}
								if (targetScrollIndex == -1) targetScrollIndex = nextIndex;
								list.Move(modIndex, nextIndex);
							}
						}

						if(targetScrollIndex > -1)
						{
							var item = Items.GetItemAt(targetScrollIndex);
							ScrollIntoView(item);
							//RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(50), _ =>
							//{
							//	var item = Items.GetItemAt(targetScrollIndex);
							//	ScrollIntoView(item);
							//});
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
