using DivinityModManager.Controls;
using DivinityModManager.Converters;
using DivinityModManager.Models;
using DivinityModManager.ViewModels;

using DynamicData.Binding;

using GongSolutions.Wpf.DragDrop.Utilities;

using ReactiveUI;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO.Packaging;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace DivinityModManager.Views
{
	public interface ModViewLayout
	{
		void UpdateViewSelection(IEnumerable<ISelectable> dataList, ListView listView = null);
		void SelectMods(IEnumerable<ISelectable> dataList, bool activeMods);
		void FixActiveModsScrollbar();
	}

	public class HorizontalModLayoutBase : ReactiveUserControl<MainWindowViewModel> { }

	/// <summary>
	/// Interaction logic for HorizonalModLayout.xaml
	/// </summary>
	public partial class HorizontalModLayout : HorizontalModLayoutBase, ModViewLayout
	{
		private object _focusedList = null;

		private bool ListHasFocus(ListView listView)
		{
			if (_focusedList == listView || listView.IsFocused || listView.IsKeyboardFocused)
			{
				return true;
			}
			if (listView.SelectedItem is ListViewItem item && (item.IsFocused || item.IsKeyboardFocused))
			{
				return true;
			}
			return false;
		}

		private bool FocusSelectedItem(ListView lv)
		{
			try
			{
				var listBoxItem = (ListBoxItem)lv.ItemContainerGenerator.ContainerFromItem(lv.SelectedItem);
				if (listBoxItem == null)
				{
					var firstItem = lv.Items.GetItemAt(0);
					if (firstItem != null)
					{
						listBoxItem = (ListBoxItem)lv.ItemContainerGenerator.ContainerFromItem(firstItem);
					}
				}
				if (listBoxItem != null)
				{
					listBoxItem.Focus();
					Keyboard.Focus(listBoxItem);
					return true;
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"{ex}");
			}
			return false;
		}

		private void FocusList(ListView listView)
		{
			if (!FocusSelectedItem(listView))
			{
				listView.Focus();
			}
		}

		private void TraceBackground(Control c)
		{
			DivinityApp.Log($"{c} Background({c.Background})");
			foreach (var c2 in c.FindVisualChildren<Control>())
			{
				if (c2.Background != null && !c2.Background.Equals(Brushes.Transparent))
				{
					TraceBackground(c2);
				}
			}
		}

		private void SetupListView(ListView listView)
		{
			listView.InputBindings.Add(new KeyBinding(ApplicationCommands.SelectAll, new KeyGesture(Key.A, ModifierKeys.Control)));
			listView.CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, (_sender, _e) =>
			{
				listView.SelectAll();
			}));

			listView.InputBindings.Add(new KeyBinding(ReactiveCommand.Create(() =>
			{
				listView.SelectedItems.Clear();

			}), new KeyGesture(Key.D, ModifierKeys.Control)));

			listView.ItemContainerStyle = this.FindResource("ListViewItemMouseEvents") as Style;
			listView.GotFocus += (object sender, RoutedEventArgs e) =>
			{
				_focusedList = sender;
			};
			listView.LostFocus += (object sender, RoutedEventArgs e) =>
			{
				if (_focusedList == sender)
				{
					_focusedList = null;
				}
			};
		}

		public void FixActiveModsScrollbar()
		{
			if (ActiveModsListView.FindVisualChildren<ScrollViewer>().FirstOrDefault() is ScrollViewer sv)
			{
				sv.ScrollToHorizontalOffset(0d);
			}
		}

		public void UpdateViewSelection(IEnumerable<ISelectable> dataList, ListView listView = null)
		{
			if (dataList != null)
			{
				if (listView == null)
				{
					if (dataList == ViewModel.ActiveMods)
					{
						listView = ActiveModsListView;
					}
					else
					{
						listView = InactiveModsListView;
					}
				}

				if (listView != null && dataList.Count() > 0)
				{
					IInputElement focusedItem = FocusManager.GetFocusedElement(listView);
					foreach (var mod in dataList)
					{
						var listItem = (ListViewItem)listView.ItemContainerGenerator.ContainerFromItem(mod);
						if (listItem != null)
						{
							if(mod.Visibility == Visibility.Visible)
							{
								listItem.IsSelected = mod.IsSelected;
								if(listView.IsFocused && focusedItem == null && mod.IsSelected)
								{
									focusedItem = listItem;
									FocusManager.SetFocusedElement(listView, focusedItem);
								}
							}
							else
							{
								listItem.IsSelected = false;
							}
						}
					}
				}
			}
		}

		public void SelectMods(IEnumerable<ISelectable> dataList, bool activeMods)
		{
			if (dataList != null)
			{
				var listView = activeMods ? ActiveModsListView : InactiveModsListView;
				foreach (var mod in dataList)
				{
					var listItem = (ListViewItem)listView.ItemContainerGenerator.ContainerFromItem(mod);
					if (listItem != null)
					{
						listItem.IsSelected = mod.Visibility == Visibility.Visible;
					}
				}
			}
		}

		private void UpdateIsSelected(SelectionChangedEventArgs e, IEnumerable<DivinityModData> list)
		{
			if (e != null && list != null)
			{
				var targetUUIDs = list.Select(x => x.UUID).ToHashSet();

				if (e.RemovedItems != null && e.RemovedItems.Count > 0)
				{
					foreach (var removedItem in e.RemovedItems.Cast<DivinityModData>())
					{
						if(targetUUIDs.Contains(removedItem.UUID))
						{
							removedItem.IsSelected = false;
						}
					}
				}

				if (e.AddedItems != null && e.AddedItems.Count > 0)
				{
					foreach (var addedItem in e.AddedItems.Cast<DivinityModData>())
					{
						addedItem.IsSelected = true;
					}
				}
			}
		}

		private IDisposable updatingActiveViewSelection;
		private IDisposable updatingInactiveViewSelection;

		private void ActiveModListView_ItemContainerStatusChanged(EventArgs e)
		{
			if (ActiveModsListView.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
			{
				if (updatingActiveViewSelection == null)
				{
					updatingActiveViewSelection = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(25), () =>
					{
						UpdateViewSelection(ViewModel.ActiveMods, ActiveModsListView);
						updatingActiveViewSelection.Dispose();
						updatingActiveViewSelection = null;
					});
				}
			}
		}

		private void InactiveModListView_ItemContainerStatusChanged(EventArgs e)
		{
			if (InactiveModsListView.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
			{
				if (updatingInactiveViewSelection == null)
				{
					updatingInactiveViewSelection = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(25), () =>
					{
						UpdateViewSelection(ViewModel.InactiveMods, InactiveModsListView);
						updatingInactiveViewSelection.Dispose();
						updatingInactiveViewSelection = null;
					});
				}

			}
		}

		private IDisposable _updateScroll;

		private void MoveSelectedMods()
		{
			if (ListHasFocus(ActiveModsListView))
			{
				var selectedMods = ViewModel.ActiveMods.Where(x => x.IsSelected).ToList();

				var selectedMod = selectedMods.First();
				var nextSelectedIndex = ViewModel.ActiveMods.IndexOf(selectedMod);

				var scrollTargetIndex = InactiveModsListView.SelectedIndex;
				var dropInfo = new ManualDropInfo(selectedMods, InactiveModsListView.SelectedIndex, InactiveModsListView, ViewModel.InactiveMods, ViewModel.ActiveMods);
				InactiveModsListView.UnselectAll();
				ViewModel.DropHandler.Drop(dropInfo);
				string countSuffix = selectedMods.Count > 1 ? "mods" : "mod";
				string text = $"Moved {selectedMods.Count} {countSuffix} to the inactive mods list.";
				ScreenReaderHelper.Speak(text);
				ViewModel.ShowAlert(text, AlertType.Info, 10);
				ViewModel.CanMoveSelectedMods = false;

				if (ViewModel.Settings.ShiftListFocusOnSwap)
				{
					InactiveModsListView.Focus();
				}

				_updateScroll?.Dispose();

				_updateScroll = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), _ =>
				{
					//InactiveModsListView.UpdateLayout();
					if (scrollTargetIndex <= 0)
					{
						ScrollToTop(InactiveModsListView);
					}
					else if (scrollTargetIndex >= InactiveModsListView.Items.Count)
					{
						ScrollToBottom(InactiveModsListView);
					}
					else
					{
						ScrollToMod(InactiveModsListView, selectedMod);
						//FocusMod(InactiveModsListView, selectedMod);
					}

					if (nextSelectedIndex >= ViewModel.ActiveMods.Count)
					{
						nextSelectedIndex = ViewModel.ActiveMods.Count - 1;
					}

					ActiveModsListView.SelectedIndex = nextSelectedIndex;
					//FocusMod(ActiveModsListView, ActiveModsListView.SelectedItem);
				});
			}
			else if (ListHasFocus(InactiveModsListView))
			{
				var selectedMods = ViewModel.InactiveMods.Where(x => x.IsSelected).ToList();

				var nextSelectedIndex = ViewModel.InactiveMods.IndexOf(selectedMods.First());

				var scrollTargetIndex = ActiveModsListView.SelectedIndex;
				var dropInfo = new ManualDropInfo(selectedMods, ActiveModsListView.SelectedIndex, ActiveModsListView, ViewModel.ActiveMods, ViewModel.InactiveMods);
				ActiveModsListView.UnselectAll();
				ViewModel.DropHandler.Drop(dropInfo);

				string countSuffix = selectedMods.Count > 1 ? "mods" : "mod";
				string text = $"Moved {selectedMods.Count} {countSuffix} to the active mods list.";
				ScreenReaderHelper.Speak(text);
				ViewModel.ShowAlert(text, AlertType.Info, 10);
				ViewModel.CanMoveSelectedMods = false;

				if (ViewModel.Settings.ShiftListFocusOnSwap)
				{
					ActiveModsListView.Focus();
				}

				_updateScroll?.Dispose();

				var selectedMod = selectedMods.First();

				_updateScroll = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), _ =>
				{
					//ActiveModsListView.UpdateLayout();
					if (scrollTargetIndex <= 0)
					{
						ScrollToTop(ActiveModsListView);
					}
					else if (scrollTargetIndex >= ActiveModsListView.Items.Count)
					{
						ScrollToBottom(ActiveModsListView);
					}
					else
					{
						ScrollToMod(ActiveModsListView, selectedMod);
						//FocusMod(ActiveModsListView, selectedMod);
					}

					if (nextSelectedIndex >= ViewModel.InactiveMods.Count)
					{
						nextSelectedIndex = ViewModel.InactiveMods.Count - 1;
					}

					InactiveModsListView.SelectedIndex = nextSelectedIndex;
					//FocusMod(InactiveModsListView, InactiveModsListView.SelectedItem);
				});
			}
		}

		public void FocusInitialActiveSelected()
		{
			if (ViewModel.ActiveSelected <= 0)
			{
				ActiveModsListView.SelectedIndex = 0;
			}
			try
			{
				ListViewItem item = (ListViewItem)ActiveModsListView.ItemContainerGenerator.ContainerFromItem(ActiveModsListView.SelectedItem);
				if (item != null)
				{
					Keyboard.Focus(item);
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error focusing selected item:{ex}");
			}
		}

		public bool FocusMod(ModListView modListView, object mod)
		{
			if (modListView.ItemContainerGenerator.ContainerFromItem(mod) is ListViewItem item)
			{
				FocusManager.SetFocusedElement(modListView, item);
				//item.BringIntoView();
				return true;
			}
			return false;
		}

		public void ScrollToMod(ModListView modListView, DivinityModData mod)
		{
			var index = modListView.Items.IndexOf(mod);
			if(index > -1)
			{
				modListView.UpdateLayout();
				modListView.ScrollIntoView(modListView.Items[index]);
			}
		}

		public void ScrollToTop(ModListView modListView)
		{
			if(modListView.GetVisualDescendent<ScrollViewer>() is ScrollViewer scrollViewer)
			{
				scrollViewer.ScrollToTop();
			}
		}

		public void ScrollToBottom(ModListView modListView)
		{
			if(modListView.GetVisualDescendent<ScrollViewer>() is ScrollViewer scrollViewer)
			{
				scrollViewer.ScrollToBottom();
			}
		}

		public HorizontalModLayout()
		{
			InitializeComponent();

			SetupListView(ActiveModsListView);
			SetupListView(InactiveModsListView);

			bool setInitialFocus = true;

			this.WhenActivated(d =>
			{
				if (ViewModel != null)
				{
					d(this.Events().KeyUp.Select(e => e.Key != Key.System ? e.Key : e.SystemKey).Subscribe(ViewModel.OnKeyUp));
					d(this.Events().KeyDown.Select(e => e.Key != Key.System ? e.Key : e.SystemKey).Subscribe(key =>
					{
						ViewModel.OnKeyDown(key);
						HorizontalModLayout_KeyDown(key);
					}));
					d(this.Events().LostFocus.Subscribe((e) => ViewModel.CanMoveSelectedMods = true));
					d(this.Events().Loaded.ObserveOn(RxApp.MainThreadScheduler).Subscribe((e) =>
					{
						if (setInitialFocus)
						{
							this.ActiveModsListView.Focus();
							setInitialFocus = false;
						}
					}));

					d(this.ActiveModsListView.ItemContainerGenerator.Events().StatusChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe(ActiveModListView_ItemContainerStatusChanged));
					d(this.InactiveModsListView.ItemContainerGenerator.Events().StatusChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe(InactiveModListView_ItemContainerStatusChanged));

					d(Observable.FromEventPattern<SelectionChangedEventArgs>(ActiveModsListView, "SelectionChanged")
					.ObserveOn(RxApp.MainThreadScheduler)
					.Subscribe((e) =>
					{
						UpdateIsSelected(e.EventArgs, ViewModel.ActiveMods);
					}));

					d(Observable.FromEventPattern<SelectionChangedEventArgs>(InactiveModsListView, "SelectionChanged")
					.ObserveOn(RxApp.MainThreadScheduler)
					.Subscribe((e) =>
					{
						UpdateIsSelected(e.EventArgs, ViewModel.InactiveMods);
					}));

					d(this.ViewModel.WhenAnyValue(x => x.OrderJustLoaded).ObserveOn(RxApp.MainThreadScheduler).Subscribe((b) =>
					{
						if(b)
						{
							this.AutoSizeNameColumn_ActiveMods();
							this.AutoSizeNameColumn_InactiveMods();
						}
					}));

					ViewModel.Layout = this;

					d(this.OneWayBind(ViewModel, vm => vm.ActiveMods, v => v.ActiveModsListView.ItemsSource));
					d(this.OneWayBind(ViewModel, vm => vm.InactiveMods, v => v.InactiveModsListView.ItemsSource));
					d(this.OneWayBind(ViewModel, vm => vm.ForceLoadedMods, v => v.ForceLoadedModsListView.ItemsSource));

					d(this.OneWayBind(ViewModel, vm => vm.HasForceLoadedMods, v => v.ForceLoadedModsListView.Visibility, BoolToVisibilityConverter.FromBool));
					d(this.OneWayBind(ViewModel, vm => vm.HasForceLoadedMods, v => v.ActiveModListViewGridSplitter.Visibility, BoolToVisibilityConverter.FromBool));

					d(this.Bind(ViewModel, vm => vm.ActiveModFilterText, v => v.ActiveModsFilterTextBox.Text));
					d(this.Bind(ViewModel, vm => vm.InactiveModFilterText, v => v.InactiveModsFilterTextBox.Text));

					d(this.OneWayBind(ViewModel, vm => vm.ActiveModsFilterResultText, v => v.ActiveModsFilterResultText.Text));
					d(this.OneWayBind(ViewModel, vm => vm.InactiveModsFilterResultText, v => v.InactiveModsFilterResultText.Text));
					d(this.OneWayBind(ViewModel, vm => vm.TotalActiveModsHidden, v => v.ActiveModsFilterResultText.Visibility, IntToVisibilityConverter.FromInt));
					d(this.OneWayBind(ViewModel, vm => vm.TotalInactiveModsHidden, v => v.InactiveModsFilterResultText.Visibility, IntToVisibilityConverter.FromInt));

					d(this.OneWayBind(ViewModel, vm => vm.ActiveSelectedText, v => v.ActiveSelectedText.Text));
					d(this.OneWayBind(ViewModel, vm => vm.ActiveSelected, v => v.ActiveSelectedText.Visibility, IntToVisibilityConverter.FromInt));
					d(this.OneWayBind(ViewModel, vm => vm.InactiveSelectedText, v => v.InactiveSelectedText.Text));
					d(this.OneWayBind(ViewModel, vm => vm.InactiveSelected, v => v.InactiveSelectedText.Visibility, IntToVisibilityConverter.FromInt));

					var gridLengthConverter = new GridLengthConverter();
					var zeroHeight = (GridLength)gridLengthConverter.ConvertFrom(0);
					var forceModsHeight = (GridLength)gridLengthConverter.ConvertFrom("1*");

					d(ViewModel.WhenAnyValue(x => x.HasForceLoadedMods).ObserveOn(RxApp.MainThreadScheduler).Subscribe((b) =>
					{
						foreach(var row in this.ActiveModListGrid.RowDefinitions.Where(x => x.Name != "ActiveModsListRow"))
						{
							if (b)
							{
								if(row.Name == "ActiveModsListGridRow")
								{
									row.Height = GridLength.Auto;
								}
								else if(row.Name == "ActiveModsListForcedModsRow")
								{
									row.Height = forceModsHeight;
								}
							}
							else
							{
								row.Height = zeroHeight;
							}
						}
					}));

					ViewModel.Keys.MoveFocusLeft.AddAction(() =>
					{
						DivinityApp.IsKeyboardNavigating = true;
						this.ActiveModsListView.Focus();

						if (ViewModel != null)
						{
							if (ViewModel.ActiveSelected <= 0)
							{
								ActiveModsListView.SelectedIndex = 0;
							}
						}

						//InactiveModsListView.UnselectAll();
						FocusList(ActiveModsListView);
					});

					ViewModel.Keys.MoveFocusRight.AddAction(() =>
					{
						DivinityApp.IsKeyboardNavigating = true;
						InactiveModsListView.Focus();
						if (ViewModel != null)
						{
							if (ViewModel.ActiveSelected <= 0)
							{
								InactiveModsListView.SelectedIndex = 0;
							}
						}
						//ActiveModsListView.UnselectAll();
						FocusList(InactiveModsListView);
					});


					ViewModel.Keys.SwapListFocus.AddAction(() =>
					{
						if (ListHasFocus(InactiveModsListView))
						{
							DivinityApp.IsKeyboardNavigating = true;
							FocusList(ActiveModsListView);
						}
						else if (ListHasFocus(ActiveModsListView))
						{
							DivinityApp.IsKeyboardNavigating = true;
							FocusList(InactiveModsListView);
						}
					});

					//InactiveModsListView.InputBindings.Add(new InputBinding(ViewModel.MoveLeftCommand, new KeyGesture(Key.Left)));
					ViewModel.Keys.ToggleFilterFocus.AddAction(() =>
					{
						if (ListHasFocus(ActiveModsListView))
						{
							if (!this.ActiveModsFilterTextBox.IsFocused)
							{
								this.ActiveModsFilterTextBox.Focus();
							}
							else
							{
								FocusSelectedItem(ActiveModsListView);
							}
						}
						else
						{
							if (!this.InactiveModsFilterTextBox.IsFocused)
							{
								this.InactiveModsFilterTextBox.Focus();
							}
							else
							{
								FocusSelectedItem(InactiveModsListView);
							}
						}
					});

					//ActiveModsListView.InputBindings.Add(new InputBinding(ViewModel.MoveRightCommand, new KeyGesture(Key.Right)));

					d(ViewModel.WhenAnyValue(x => x.ActiveSelected).Subscribe((c) =>
					{
						if (c > 1 && DivinityApp.IsScreenReaderActive())
						{
							var peer = UIElementAutomationPeer.FromElement(this.ActiveSelectedText);
							if (peer == null)
							{
								peer = UIElementAutomationPeer.CreatePeerForElement(this.ActiveSelectedText);
							}
							peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
						}
					}));

					d(ViewModel.WhenAnyValue(x => x.InactiveSelected).Subscribe((c) =>
					{
						if (c > 1 && DivinityApp.IsScreenReaderActive())
						{
							var peer = UIElementAutomationPeer.FromElement(this.InactiveSelectedText);
							if (peer == null)
							{
								peer = UIElementAutomationPeer.CreatePeerForElement(this.InactiveSelectedText);
							}
							peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
						}
					}));
				}
				//BindingHelper.CreateCommandBinding(ViewModel.View.EditFocusActiveListMenuItem, "MoveLeftCommand", ViewModel);
			});
		}

		private void HorizontalModLayout_KeyDown(Key key)
		{
			var keyIsDown = key == ViewModel.Keys.Confirm.Key && (ViewModel.Keys.Confirm.Modifiers == ModifierKeys.None || Keyboard.Modifiers.HasFlag(ViewModel.Keys.Confirm.Modifiers));
			if (!keyIsDown && (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
			{
				if (key == Key.Right && ActiveModsListView.IsKeyboardFocusWithin)
				{
					keyIsDown = true;
				}
				else if (key == Key.Left && InactiveModsListView.IsKeyboardFocusWithin)
				{
					keyIsDown = true;
				}
			}
			if (ViewModel.CanMoveSelectedMods && keyIsDown)
			{
				DivinityApp.IsKeyboardNavigating = true;
				if(ViewModel.ActiveSelected > 0 || ViewModel.InactiveSelected > 0)
				{
					MoveSelectedMods();
				}
			}
		}

		GridViewColumnHeader _lastHeaderClicked = null;
		ListSortDirection _lastDirection = ListSortDirection.Ascending;

		private void ListView_Click(object sender, RoutedEventArgs e)
		{
			GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
			ListSortDirection direction;

			if (headerClicked != null)
			{
				if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
				{
					if (headerClicked != _lastHeaderClicked)
					{
						direction = ListSortDirection.Ascending;
					}
					else
					{
						if (_lastDirection == ListSortDirection.Ascending)
						{
							direction = ListSortDirection.Descending;
						}
						else
						{
							direction = ListSortDirection.Ascending;
						}
					}

					string header = "";

					if (headerClicked.Column.Header is TextBlock textBlock)
					{
						header = textBlock.Text;
					}
					else if (headerClicked.Column.Header is string gridHeader)
					{
						header = gridHeader;
					}

					Sort(header, direction, sender);

					_lastHeaderClicked = headerClicked;
					_lastDirection = direction;
				}
			}
		}

		private void Sort(string sortBy, ListSortDirection direction, object sender)
		{
			if (sortBy == "Version") sortBy = "Version.Version";
			if (sortBy == "#") sortBy = "Index";
			if (sortBy == "Name") sortBy = "DisplayName";
			if (sortBy == "Modes") sortBy = "Targets";
			if (sortBy == "Last Updated") sortBy = "LastUpdated";

			try
			{
				ListView lv = sender as ListView;
				ICollectionView dataView =
				  CollectionViewSource.GetDefaultView(lv.ItemsSource);

				dataView.SortDescriptions.Clear();
				SortDescription sd = new SortDescription(sortBy, direction);
				dataView.SortDescriptions.Add(sd);
				dataView.Refresh();
			}
			catch (Exception ex)
			{
				DivinityApp.Log("Error sorting mods:");
				DivinityApp.Log(ex.ToString());
			}
		}

		private int _FontSizeMeasurePadding = 36;

		public void AutoSizeNameColumn_ActiveMods()
		{
			if (ViewModel == null || ActiveModsListView.UserResizedColumns) return;
			if (ViewModel.ActiveMods.Count > 0 && ActiveModsListView.View is GridView gridView && gridView.Columns.Count >= 2)
			{
				RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), () =>
				{
					if (ViewModel.ActiveMods.Count > 0)
					{
						var longestName = ViewModel.ActiveMods.OrderByDescending(m => m.Name.Length).FirstOrDefault()?.Name;
						if (!String.IsNullOrEmpty(longestName))
						{
							//DivinityApp.LogMessage($"Autosizing active mods grid for name {longestName}");
							var targetWidth = MeasureText(ActiveModsListView, longestName,
								ActiveModsListView.FontFamily,
								ActiveModsListView.FontStyle,
								ActiveModsListView.FontWeight,
								ActiveModsListView.FontStretch,
								ActiveModsListView.FontSize).Width + _FontSizeMeasurePadding;
							if (Math.Abs(gridView.Columns[1].Width - targetWidth) >= 30)
							{
								ActiveModsListView.Resizing = true;
								gridView.Columns[1].Width = targetWidth;
							}
						}
					}
				});
			}
		}

		public void AutoSizeNameColumn_InactiveMods()
		{
			if (ViewModel == null || InactiveModsListView.UserResizedColumns) return;
			if (ViewModel.InactiveMods.Count > 0 && InactiveModsListView.View is GridView gridView && gridView.Columns.Count >= 2)
			{
				var longestName = ViewModel.InactiveMods.OrderByDescending(m => m.Name.Length).FirstOrDefault()?.Name;
				if (!String.IsNullOrEmpty(longestName))
				{
					InactiveModsListView.Resizing = true;
					//DivinityApp.LogMessage($"Autosizing inactive mods grid for name {longestName}");
					gridView.Columns[0].Width = MeasureText(InactiveModsListView, longestName,
						InactiveModsListView.FontFamily,
						InactiveModsListView.FontStyle,
						InactiveModsListView.FontWeight,
						InactiveModsListView.FontStretch,
						InactiveModsListView.FontSize).Width + _FontSizeMeasurePadding;
				}
			}
		}

		// Source: https://stackoverflow.com/a/22420728
		private static Size MeasureTextSize(Visual target, string text, FontFamily fontFamily, FontStyle fontStyle,
			FontWeight fontWeight, FontStretch fontStretch, double fontSize)
		{
			var typeFace = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
			var ft = new FormattedText(text, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeFace, fontSize, Brushes.Black, VisualTreeHelper.GetDpi(target).PixelsPerDip);
			return new Size(ft.Width, ft.Height);
		}

		private static Size MeasureText(Visual target, string text,
			FontFamily fontFamily,
			FontStyle fontStyle,
			FontWeight fontWeight,
			FontStretch fontStretch, double fontSize)
		{
			Typeface typeface = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
			GlyphTypeface glyphTypeface;

			if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
			{
				return MeasureTextSize(target, text, fontFamily, fontStyle, fontWeight, fontStretch, fontSize);
			}

			double totalWidth = 0;
			double height = 0;

			for (int n = 0; n < text.Length; n++)
			{
				try
				{
					ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[text[n]];

					double width = glyphTypeface.AdvanceWidths[glyphIndex] * fontSize;

					double glyphHeight = glyphTypeface.AdvanceHeights[glyphIndex] * fontSize;

					if (glyphHeight > height)
					{
						height = glyphHeight;
					}

					totalWidth += width;
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error measuring text:\n{ex}");
				}
			}

			return new Size(totalWidth, height);
		}

		private void ListViewItem_ModifySelection(object sender, MouseButtonEventArgs e)
		{
			//Fix for when virtualization is enabled, and selected entries outside the view don't get deselected
			if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
			{
				if (sender is ListViewItem listViewitem)
				{
					if (listViewitem.DataContext is DivinityModData modData)
					{
						if (modData.IsActive)
						{
							foreach (var x in ViewModel.ActiveMods)
							{
								if (x != modData && x.IsSelected) x.IsSelected = false;
							}
						}
						else
						{
							foreach (var x in ViewModel.InactiveMods)
							{
								if (x != modData && x.IsSelected) x.IsSelected = false;
							}
						}
					}
				}
			}
		}
	}
}
