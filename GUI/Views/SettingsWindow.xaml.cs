using DivinityModManager.Models;
using DivinityModManager.Util;
using DivinityModManager.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
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
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
using System.Diagnostics;
using System.Globalization;
using DivinityModManager.Controls;
using Xceed.Wpf.Toolkit;
using DivinityModManager.Converters;

namespace DivinityModManager.Views
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : HideWindowBase, IViewFor<DivinityModManagerSettings>
	{
		public SettingsWindow()
		{
			InitializeComponent();
		}

		public DivinityModManagerSettings ViewModel { get; set; }
		object IViewFor.ViewModel { get; set; }

		private void CreateExtenderSettings()
		{
			var props = from p in typeof(OsiExtenderSettings).GetProperties()
						let attr = p.GetCustomAttributes(typeof(SettingsEntryAttribute), true)
						where attr.Length == 1
						select new { Property = p, Attribute = attr.First() as SettingsEntryAttribute };

			int count = ExtenderSettingsAutoGrid.RowCount + props.Count();
			int row = ExtenderSettingsAutoGrid.RowCount + 1;

			ExtenderSettingsAutoGrid.Children.Clear();

			ExtenderSettingsAutoGrid.RowCount = count;
			ExtenderSettingsAutoGrid.Rows = String.Join(",", Enumerable.Repeat("auto", count));
			DivinityApp.Log($"{count} = {ExtenderSettingsAutoGrid.Rows}");

			foreach (var prop in props)
			{
				row++;
				TextBlock tb = new TextBlock();
				tb.Text = prop.Attribute.DisplayName;
				tb.ToolTip = prop.Attribute.Tooltip;
				ExtenderSettingsAutoGrid.Children.Add(tb);
				Grid.SetRow(tb, row);

				BoolToVisibilityConverter boolToVisibilityConverter = (BoolToVisibilityConverter)FindResource("BoolToVisibilityConverter");

				if (prop.Attribute.IsDebug)
				{
					tb.SetBinding(TextBlock.VisibilityProperty, new Binding("DebugModeEnabled")
					{
						Source = ViewModel,
						Converter = boolToVisibilityConverter,
						FallbackValue = Visibility.Collapsed
					});
				}

				var propType = Type.GetTypeCode(prop.Property.PropertyType);

				if (prop.Attribute.DisplayName == "Osiris Debugger Flags")
				{
					propType = TypeCode.String;
				}

				switch (propType)
				{
					case TypeCode.Boolean:
						CheckBox cb = new CheckBox();
						cb.ToolTip = prop.Attribute.Tooltip;
						cb.VerticalAlignment = VerticalAlignment.Center;
						//cb.HorizontalAlignment = HorizontalAlignment.Right;
						cb.SetBinding(CheckBox.IsCheckedProperty, new Binding(prop.Property.Name)
						{
							Source = ViewModel.ExtenderSettings,
							Mode = BindingMode.TwoWay,
							UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
						});
						if (prop.Attribute.IsDebug)
						{
							cb.SetBinding(CheckBox.VisibilityProperty, new Binding("DebugModeEnabled")
							{
								Source = ViewModel,
								Converter = boolToVisibilityConverter,
								FallbackValue = Visibility.Collapsed
							});
						}
						ExtenderSettingsAutoGrid.Children.Add(cb);
						Grid.SetRow(cb, row);
						Grid.SetColumn(cb, 1);
						break;

					case TypeCode.String:
						UnfocusableTextBox utb = new UnfocusableTextBox();
						utb.ToolTip = prop.Attribute.Tooltip;
						utb.VerticalAlignment = VerticalAlignment.Center;
						//utb.HorizontalAlignment = HorizontalAlignment.Stretch;
						utb.TextAlignment = TextAlignment.Left;
						utb.SetBinding(UnfocusableTextBox.TextProperty, new Binding(prop.Property.Name)
						{
							Source = ViewModel.ExtenderSettings,
							Mode = BindingMode.TwoWay,
							UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
						});
						if (prop.Attribute.IsDebug)
						{
							utb.SetBinding(CheckBox.VisibilityProperty, new Binding("DebugModeEnabled")
							{
								Source = ViewModel,
								Converter = boolToVisibilityConverter,
								FallbackValue = Visibility.Collapsed
							});
						}
						ExtenderSettingsAutoGrid.Children.Add(utb);
						Grid.SetRow(utb, row);
						Grid.SetColumn(utb, 1);
						break;
					case TypeCode.Int32:
					case TypeCode.Int64:
						IntegerUpDown ud = new IntegerUpDown();
						ud.ToolTip = prop.Attribute.Tooltip;
						ud.VerticalAlignment = VerticalAlignment.Center;
						ud.HorizontalAlignment = HorizontalAlignment.Left;
						ud.Padding = new Thickness(4, 2, 4, 2);
						ud.AllowTextInput = true;
						ud.SetBinding(IntegerUpDown.ValueProperty, new Binding(prop.Property.Name)
						{
							Source = ViewModel.ExtenderSettings,
							Mode = BindingMode.TwoWay,
							UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
						});
						if (prop.Attribute.IsDebug)
						{
							ud.SetBinding(VisibilityProperty, new Binding("DebugModeEnabled")
							{
								Source = ViewModel,
								Converter = boolToVisibilityConverter,
								FallbackValue = Visibility.Collapsed
							});
						}
						ExtenderSettingsAutoGrid.Children.Add(ud);
						Grid.SetRow(ud, row);
						Grid.SetColumn(ud, 1);
						break;
				}
			}
		}

		public void Init(MainWindowViewModel vm)
		{
			ViewModel = vm.Settings;
			DataContext = ViewModel;

			BindingHelper.CreateCommandBinding(this.ExportExtenderSettingsButton, "ExportExtenderSettingsCommand", ViewModel);
			BindingHelper.CreateCommandBinding(this.SaveSettingsButton, "SaveSettingsCommand", ViewModel);

			KeybindingsListView.ItemsSource = vm.Keys.All;
			KeybindingsListView.SetBinding(ListView.ItemsSourceProperty, new Binding("All")
			{
				Source = vm.Keys,
				Mode = BindingMode.OneWay
			});
			KeybindingsListView.SetBinding(ListView.SelectedItemProperty, new Binding("SelectedHotkey")
			{
				Source = ViewModel,
				Mode = BindingMode.OneWayToSource
			});

			this.KeyDown += SettingsWindow_KeyDown;
			KeybindingsListView.Loaded += (o, e) =>
			{
				if (KeybindingsListView.SelectedIndex < 0)
				{
					KeybindingsListView.SelectedIndex = 0;
				}
				ListViewItem row = (ListViewItem)KeybindingsListView.ItemContainerGenerator.ContainerFromIndex(KeybindingsListView.SelectedIndex);
				if (row != null && !FocusHelper.HasKeyboardFocus(row))
				{
					Keyboard.Focus(row);
				}
			};
			KeybindingsListView.KeyUp += KeybindingsListView_KeyUp;

			CreateExtenderSettings();
			//this.WhenAnyValue(x => x.ViewModel.ExportExtenderSettingsCommand).BindTo(this, view => view.ExportExtenderSettingsButton.Command);
			//this.WhenAnyValue(x => x.ViewModel.SaveSettingsCommand).BindTo(this, view => view.SaveSettingsButton.Command);
		}

		private bool isSettingKeybinding = false;

		private void FocusSelectedHotkey()
		{
			ListViewItem row = (ListViewItem)KeybindingsListView.ItemContainerGenerator.ContainerFromIndex(KeybindingsListView.SelectedIndex);
			var hotkeyControls = row.FindVisualChildren<HotkeyEditorControl>();
			foreach (var c in hotkeyControls)
			{
				c.HotkeyTextBox.Focus();
				isSettingKeybinding = true;
			}
		}

		private void KeybindingsListView_KeyUp(object sender, KeyEventArgs e)
		{
			if (KeybindingsListView.SelectedIndex >= 0 && e.Key == Key.Enter)
			{
				FocusSelectedHotkey();
			}
		}

		private void SettingsWindow_KeyDown(object sender, KeyEventArgs e)
		{
			if(isSettingKeybinding)
			{
				return;
			}
			else if(e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
			{
				ViewModel.SaveSettingsCommand.Execute(null);
				if(ViewModel.ExtenderTabIsVisible)
				{
					ViewModel.ExportExtenderSettingsCommand.Execute(null);
				}
				e.Handled = true;
			}
			else if(e.Key == Key.Left && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
			{
				int current = PreferencesTabControl.SelectedIndex;
				int nextIndex = current - 1;
				if(nextIndex < 0)
				{
					nextIndex = PreferencesTabControl.Items.Count - 1;
				}
				PreferencesTabControl.SelectedIndex = nextIndex;
				Keyboard.Focus((FrameworkElement)PreferencesTabControl.SelectedContent);
				MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
			}
			else if(e.Key == Key.Right && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
			{
				int current = PreferencesTabControl.SelectedIndex;
				int nextIndex = current + 1;
				if(nextIndex >= PreferencesTabControl.Items.Count)
				{
					nextIndex = 0;
				}
				PreferencesTabControl.SelectedIndex = nextIndex;
				//Keyboard.Focus((FrameworkElement)PreferencesTabControl.SelectedContent);
				//MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
			}
		}

		private string lastWorkshopPath = "";

		public EventHandler OnWorkshopPathChanged { get; set; }

		private void WorkshopPathTextbox_GotFocus(object sender, RoutedEventArgs e)
		{
			lastWorkshopPath = ViewModel.WorkshopPath;
		}

		private void WorkshopPathTextbox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (sender is TextBox tb && tb.Text != lastWorkshopPath)
			{
				OnWorkshopPathChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		private void HotkeyEditorControl_GotFocus(object sender, RoutedEventArgs e)
		{
			isSettingKeybinding = true;
		}

		private void HotkeyEditorControl_LostFocus(object sender, RoutedEventArgs e)
		{
			isSettingKeybinding = false;
		}

		private void HotkeyListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			FocusSelectedHotkey();
		}
	}
}
