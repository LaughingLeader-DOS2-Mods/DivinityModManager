using DivinityModManager.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DivinityModManager.Models;
using DynamicData.Binding;
using GongSolutions.Wpf.DragDrop;
using System.Windows.Automation.Peers;
using DivinityModManager.Util;
using System.Timers;
using DivinityModManager.Controls;

namespace DivinityModManager.Views
{
	public interface ModViewLayout
	{
		void UpdateViewSelection(IEnumerable<ISelectable> dataList, ListView listView = null);
		void FixActiveModsScrollbar();
	}
	/// <summary>
	/// Interaction logic for HorizonalModLayout.xaml
	/// </summary>
	public partial class HorizontalModLayout : UserControl, IViewFor<MainWindowViewModel>, ModViewLayout
	{
		public MainWindowViewModel ViewModel
		{
			get { return (MainWindowViewModel)GetValue(ViewModelProperty); }
			set { SetValue(ViewModelProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register("ViewModel", typeof(MainWindowViewModel), typeof(HorizontalModLayout), new PropertyMetadata(null));

		object IViewFor.ViewModel { get; set; }

		private MainWindowViewModel _lastVM;

		private object _focusedList = null;

		private bool ListHasFocus(ListView listView)
		{
			if (_focusedList == listView || listView.IsFocused || listView.IsKeyboardFocused)
			{
				return true;
			}
			if(listView.SelectedItem is ListViewItem item && (item.IsFocused || item.IsKeyboardFocused))
			{
				return true;
			}
			return false;
		}

		private bool FocusSelectedItem(ListView lv)
		{
			var listBoxItem = (ListBoxItem)lv.ItemContainerGenerator.ContainerFromItem(lv.SelectedItem);
			if(listBoxItem == null)
			{
				listBoxItem = (ListBoxItem)lv.ItemContainerGenerator.ContainerFromItem(lv.Items.GetItemAt(0));
			}
			if(listBoxItem != null)
			{
				listBoxItem.Focus();
				Keyboard.Focus(listBoxItem);
				return true;
			}
			return false;
		}

		private void FocusList(ListView listView)
		{
			if(!FocusSelectedItem(listView))
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
				//if(listView.ItemsSource is IEnumerable<ISelectable> mods)
				//{
				//	foreach(var mod in mods)
				//	{
				//		mod.IsSelected = true;
				//	}
				//	UpdateViewSelection(mods, listView);
				//}
			}));

			listView.InputBindings.Add(new KeyBinding(ReactiveCommand.Create(() =>
			{
				listView.SelectedItems.Clear();
				//if (listView.ItemsSource is IEnumerable<ISelectable> mods)
				//{
				//	foreach (var mod in mods)
				//	{
				//		mod.IsSelected = false;
				//	}
				//	UpdateViewSelection(mods, listView);
				//}

			}), new KeyGesture(Key.D, ModifierKeys.Control)));

			//listView.PreviewMouseLeftButtonDown += (s, e) =>
			//{
			//	if(!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
			//	{
					
			//	}
			//};

			listView.ItemContainerStyle = this.FindResource("ListViewItemMouseEvents") as Style;
			listView.GotFocus += (object sender, RoutedEventArgs e) =>
			{
				_focusedList = sender;
			};
			listView.LostFocus += (object sender, RoutedEventArgs e) =>
			{
				if(_focusedList == sender)
				{
					_focusedList = null;
				}
			};
		}

		public void FixActiveModsScrollbar()
		{
			ScrollViewer myViewer = ActiveModsListView.FindVisualChildren<ScrollViewer>().FirstOrDefault();
			if (myViewer != null)
			{
				myViewer.ScrollToHorizontalOffset(0d);
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
					foreach (var mod in dataList)
					{
						var listItem = (ListViewItem)listView.ItemContainerGenerator.ContainerFromItem(mod);
						if (listItem != null)
						{
							if(mod.Visibility == Visibility.Visible)
							{
								listItem.IsSelected = mod.IsSelected;
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

		private void UpdateIsSelected(SelectionChangedEventArgs e, ObservableCollectionExtended<DivinityModData> list)
		{
			if(e != null && list != null)
			{
				if (e.RemovedItems != null && e.RemovedItems.Count > 0)
				{
					foreach (var removedItem in e.RemovedItems.Cast<DivinityModData>())
					{
						if (list != null && list.Contains(removedItem)) removedItem.IsSelected = false;
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

		private void ActiveModListView_ItemContainerStatusChanged(object s, EventArgs e)
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

		private void InactiveModListView_ItemContainerStatusChanged(object s, EventArgs e)
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

		private bool canMoveSelectedMods = true;

		private void MoveSelectedMods()
		{
			List<DivinityModData> selectedMods = new List<DivinityModData>();
			int nextIndex = -1;
			DivinityModData targetMod = null;
			//DivinityApp.Log($"ListHasFocus(ActiveModsListView) = {ListHasFocus(ActiveModsListView)} | ListHasFocus(InactiveModsListView) = {ListHasFocus(InactiveModsListView)}");
			if (ListHasFocus(ActiveModsListView))
			{
				//var selectedMods = ViewModel.ActiveMods.Where(x => x.IsSelected).ToList();
				//for (int i = 0; i < selectedMods.Count; i++)
				//{
				//	ViewModel.ActiveMods.Remove(selectedMods[i]);
				//	ViewModel.InactiveMods.Add(selectedMods[i]);
				//}
				
				foreach(var m in ViewModel.ActiveMods.Where(x => x.IsSelected))
				{
					nextIndex = Math.Min(m.Index + 1, ViewModel.ActiveMods.Count - 1);
					selectedMods.Add(m);
				}
				if(nextIndex > -1)
				{
					targetMod = ViewModel.ActiveMods.ElementAtOrDefault(nextIndex);
					DivinityApp.Log($"nextIndex({nextIndex}) targetMod({targetMod})");
				}

				var dropInfo = new ManualDropInfo(selectedMods, InactiveModsListView.SelectedIndex, InactiveModsListView, ViewModel.InactiveMods, ViewModel.ActiveMods);
				InactiveModsListView.UnselectAll();
				ViewModel.DropHandler.Drop(dropInfo);
				string countSuffix = selectedMods.Count > 1 ? "mods" : "mod";
				string text = $"Moved {selectedMods.Count} {countSuffix} to the inactive mods list.";
				ScreenReaderHelper.Speak(text);
				ViewModel.ShowAlert(text, AlertType.Info, 10);
				canMoveSelectedMods = false;

				if(ViewModel.Settings.ShiftListFocusOnSwap)
				{
					InactiveModsListView.Focus();
					//FocusList(InactiveModsListView);
				}
				else
				{
					//FocusList(ActiveModsListView);
					ActiveModsListView.Focus();
				}

				if (targetMod != null && targetMod.IsActive)
				{
					ActiveModsListView.UnselectAll();
					ActiveModsListView.SelectedItem = targetMod;
					targetMod.IsSelected = true;
					//FocusSelectedItem(ActiveModsListView);
				}
			}
			else if (ListHasFocus(InactiveModsListView))
			{
				//var selectedMods = ViewModel.InactiveMods.Where(x => x.IsSelected).ToList();
				//for (int i = 0; i < selectedMods.Count; i++)
				//{
				//	ViewModel.InactiveMods.Remove(selectedMods[i]);
				//	ViewModel.ActiveMods.Add(selectedMods[i]);
				//}
				foreach (var m in ViewModel.InactiveMods.Where(x => x.IsSelected))
				{
					nextIndex = Math.Min(ViewModel.InactiveMods.IndexOf(m)+1, ViewModel.InactiveMods.Count-1);
					selectedMods.Add(m);
				}
				if (nextIndex > -1)
				{
					targetMod = ViewModel.InactiveMods.FirstOrDefault(x => ViewModel.InactiveMods.IndexOf(x) == nextIndex && !x.IsClassicMod);
				}

				var dropInfo = new ManualDropInfo(selectedMods, ActiveModsListView.SelectedIndex, ActiveModsListView, ViewModel.ActiveMods, ViewModel.InactiveMods);
				ActiveModsListView.UnselectAll();
				ViewModel.DropHandler.Drop(dropInfo);
				
				string countSuffix = selectedMods.Count > 1 ? "mods" : "mod";
				string text = $"Moved {selectedMods.Count} {countSuffix} to the active mods list.";
				ScreenReaderHelper.Speak(text);
				ViewModel.ShowAlert(text, AlertType.Info, 10);
				canMoveSelectedMods = false;

				if (ViewModel.Settings.ShiftListFocusOnSwap)
				{
					ActiveModsListView.Focus();
					//FocusList(ActiveModsListView);
				}
				else
				{
					//FocusList(InactiveModsListView);
					InactiveModsListView.Focus();
				}

				if (targetMod != null && !targetMod.IsActive)
				{
					InactiveModsListView.UnselectAll();
					InactiveModsListView.SelectedItem = targetMod;
					targetMod.IsSelected = true;
					//FocusSelectedItem(InactiveModsListView);
				}
			}
		}

		public HorizontalModLayout()
		{
			InitializeComponent();

			SetupListView(ActiveModsListView);
			SetupListView(InactiveModsListView);

			ActiveModsListView.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
			{
				if (ActiveModsListView.Items.Count == 0) return;
				UpdateIsSelected(e, ViewModel.ActiveMods);
			};

			InactiveModsListView.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
			{
				if (InactiveModsListView.Items.Count == 0) return;
				UpdateIsSelected(e, ViewModel.InactiveMods);
			};

			ActiveModsListView.ItemContainerGenerator.StatusChanged += ActiveModListView_ItemContainerStatusChanged;
			InactiveModsListView.ItemContainerGenerator.StatusChanged += InactiveModListView_ItemContainerStatusChanged;

			this.KeyDown += HorizontalModLayout_KeyDown;
			this.KeyUp += HorizontalModLayout_KeyUp;
			this.LostFocus += (o, e) =>
			{
				canMoveSelectedMods = true;
			};

			bool setInitialFocus = true;
			this.Loaded += (o, e) =>
			{
				if (setInitialFocus)
				{
					this.ActiveModsListView.Focus();
					if (ViewModel.ActiveSelected <= 0)
					{
						ActiveModsListView.SelectedIndex = 0;
					}
					Keyboard.Focus((ListViewItem)ActiveModsListView.SelectedItem);
					setInitialFocus = false;
				}
			};

			this.WhenActivated((d) =>
			{
				_lastVM = ViewModel;

				if(ViewModel != null)
				{
					ViewModel.OnOrderChanged += AutoSizeNameColumn_ActiveMods;
					ViewModel.OnOrderChanged += AutoSizeNameColumn_InactiveMods;
					ViewModel.Layout = this;

					//ViewModel.Keys.Confirm.AddAction(() =>
					//{
					//	if(ListHasFocus(ActiveModsListView) || ListHasFocus(InactiveModsListView))
					//	{
					//		//MoveSelectedMods();
					//	}
					//});

					ViewModel.Keys.MoveFocusLeft.AddAction(() =>
					{
						DivinityApp.IsKeyboardNavigating = true;
						this.ActiveModsListView.Focus();
						if (ViewModel.ActiveSelected <= 0)
						{
							ActiveModsListView.SelectedIndex = 0;
						}
						//InactiveModsListView.UnselectAll();
						FocusList(ActiveModsListView);
					});

					ViewModel.Keys.MoveFocusRight.AddAction(() =>
					{
						DivinityApp.IsKeyboardNavigating = true;
						InactiveModsListView.Focus();
						if (ViewModel.InactiveSelected <= 0)
						{
							InactiveModsListView.SelectedIndex = 0;
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
						else if(ListHasFocus(ActiveModsListView))
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

					ViewModel.WhenAnyValue(x => x.ActiveSelected).Subscribe((c) =>
					{
						if (c > 1 && DivinityApp.IsScreenReaderActive())
						{
							var peer = UIElementAutomationPeer.FromElement(this.ActiveSelectedText);
							if(peer == null)
							{
								peer = UIElementAutomationPeer.CreatePeerForElement(this.ActiveSelectedText);
							}
							peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
						}
					}).DisposeWith(d);

					ViewModel.WhenAnyValue(x => x.InactiveSelected).Subscribe((c) =>
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
					}).DisposeWith(d);
				}
				//BindingHelper.CreateCommandBinding(ViewModel.View.EditFocusActiveListMenuItem, "MoveLeftCommand", ViewModel);
				

				// when the ViewModel gets deactivated
				Disposable.Create(() =>
				{
					if(_lastVM != null)
					{
						_lastVM.OnOrderChanged -= AutoSizeNameColumn_ActiveMods;
						_lastVM.OnOrderChanged -= AutoSizeNameColumn_InactiveMods;

						_lastVM = null;
					}
				}).DisposeWith(d);
			});
		}

		private void HorizontalModLayout_KeyDown(object sender, KeyEventArgs e)
		{
			var key = e.Key != Key.System ? e.Key : e.SystemKey;
			switch (key)
			{
				case Key.Up:
				case Key.Right:
				case Key.Down:
				case Key.Left:
					DivinityApp.IsKeyboardNavigating = true;
					break;
			}

			var keyIsDown = e.Key == ViewModel.Keys.Confirm.Key && (ViewModel.Keys.Confirm.Modifiers == ModifierKeys.None || Keyboard.Modifiers.HasFlag(ViewModel.Keys.Confirm.Modifiers));
			if(!keyIsDown && (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
			{
				if(e.SystemKey == Key.Right && ActiveModsListView.IsKeyboardFocusWithin)
				{
					keyIsDown = true;
				}
				else if (e.SystemKey == Key.Left && InactiveModsListView.IsKeyboardFocusWithin)
				{
					keyIsDown = true;
				}
			}
			if (canMoveSelectedMods && keyIsDown)
			{
				DivinityApp.IsKeyboardNavigating = true;
				MoveSelectedMods();
			}
		}

		private void HorizontalModLayout_KeyUp(object sender, KeyEventArgs e)
		{
			if(e.Key == ViewModel.Keys.Confirm.Key)
			{
				canMoveSelectedMods = true;
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
			catch(Exception ex)
			{
				DivinityApp.Log("Error sorting mods:");
				DivinityApp.Log(ex.ToString());
			}
		}

		private int _FontSizeMeasurePadding = 36;

		public void AutoSizeNameColumn_ActiveMods(object sender, EventArgs e)
		{
			if (ViewModel.ActiveMods.Count > 0 && ActiveModsListView.View is GridView gridView && gridView.Columns.Count >= 2)
			{
				RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), () =>
				{
					if(ViewModel.ActiveMods.Count > 0)
					{
						var longestName = ViewModel.ActiveMods.OrderByDescending(m => m.Name.Length).FirstOrDefault()?.Name;
						if (!String.IsNullOrEmpty(longestName))
						{
							//DivinityApp.LogMessage($"Autosizing active mods grid for name {longestName}");
							var targetWidth = MeasureText(longestName,
								ActiveModsListView.FontFamily,
								ActiveModsListView.FontStyle,
								ActiveModsListView.FontWeight,
								ActiveModsListView.FontStretch,
								ActiveModsListView.FontSize).Width + _FontSizeMeasurePadding;
							if (gridView.Columns[1].Width > targetWidth)
							{
								gridView.Columns[1].Width = targetWidth;
							}
						}
					}
				});
			}
		}

		public void AutoSizeNameColumn_InactiveMods(object sender, EventArgs e)
		{
			if (ViewModel.InactiveMods.Count > 0 && InactiveModsListView.View is GridView gridView && gridView.Columns.Count >= 2)
			{
				var longestName = ViewModel.InactiveMods.OrderByDescending(m => m.Name.Length).FirstOrDefault()?.Name;
				if (!String.IsNullOrEmpty(longestName))
				{
					//DivinityApp.LogMessage($"Autosizing inactive mods grid for name {longestName}");
					gridView.Columns[0].Width = MeasureText(longestName,
						ActiveModsListView.FontFamily,
						ActiveModsListView.FontStyle,
						ActiveModsListView.FontWeight,
						ActiveModsListView.FontStretch,
						ActiveModsListView.FontSize).Width + _FontSizeMeasurePadding;
				}
			}
		}

		// Source: https://stackoverflow.com/a/22420728
		private static Size MeasureTextSize(string text, FontFamily fontFamily, FontStyle fontStyle,
			FontWeight fontWeight, FontStretch fontStretch, double fontSize)
		{
			var typeFace = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
			FormattedText ft = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeFace, fontSize, Brushes.Black);
			return new Size(ft.Width, ft.Height);
		}

		private static Size MeasureText(string text,
			FontFamily fontFamily,
			FontStyle fontStyle,
			FontWeight fontWeight,
			FontStretch fontStretch, double fontSize)
		{
			Typeface typeface = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
			GlyphTypeface glyphTypeface;

			if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
			{
				return MeasureTextSize(text, fontFamily, fontStyle, fontWeight, fontStretch, fontSize);
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
				catch(Exception ex)
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
