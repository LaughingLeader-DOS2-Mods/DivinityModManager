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
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Concurrency;
using DynamicData;
using DynamicData.Binding;
using System.Diagnostics;
using System.Globalization;
using AutoUpdaterDotNET;
using System.Windows.Threading;
using AdonisUI.Controls;
using Alphaleonis.Win32.Filesystem;
using DivinityModManager.WinForms;
using AdonisUI;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DivinityModManager.Util.ScreenReader;
using System.Reflection;
using DivinityModManager.Models.App;

namespace DivinityModManager.Views
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : AdonisWindow, IViewFor<MainWindowViewModel>, INotifyPropertyChanged
	{
		private static MainWindow self;
		public static MainWindow Self => self;

		public SettingsWindow SettingsWindow { get; set; }
		public AboutWindow AboutWindow { get; set; }
		public VersionGeneratorWindow VersionGeneratorWindow { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;

		private MainWindowViewModel viewModel;
		public MainWindowViewModel ViewModel
		{
			get => viewModel;
			set
			{
				viewModel = value;
				// ViewModel is POCO type warning suppression
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ViewModel"));
			}
		}

		object IViewFor.ViewModel
		{
			get => ViewModel;
			set => ViewModel = (MainWindowViewModel)value;
		}

		private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();
		public Dictionary<string, MenuItem> MenuItems => menuItems;

		public MainWindow()
		{
			InitializeComponent();

			self = this;

			DivinityApp.DateTimeColumnFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
			DivinityApp.DateTimeTooltipFormat = CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern;

			RxExceptionHandler.view = this;

			App.Current.Exit += OnAppClosing;

			//Wrapper = new WindowWrapper(this);

			SettingsWindow = new SettingsWindow();
			SettingsWindow.OnWorkshopPathChanged += delegate
			{
				RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(50), _ => ViewModel.LoadWorkshopMods());
			};
			SettingsWindow.Closed += delegate
			{
				if(ViewModel?.Settings != null)
				{
					ViewModel.Settings.SettingsWindowIsOpen = false;
				}
			};
			SettingsWindow.Hide();

			ViewModel = new MainWindowViewModel();

			if (File.Exists(Alphaleonis.Win32.Filesystem.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "debug")))
			{
				ViewModel.DebugMode = true;
				ViewModel.ToggleLogging(true);
				DivinityApp.Log("Enable logging due to the debug file next to the exe.");
			}

			this.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;

			AlertBar.Show += AlertBar_Show;

			AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
			AutoUpdater.HttpUserAgent = "DivinityModManagerUser";

			var res = this.TryFindResource("ModUpdaterPanel");
			if(res != null && res is ModUpdatesLayout modUpdaterPanel)
			{
				Binding binding = new Binding("ModUpdatesViewData");
				binding.Source = ViewModel;
				modUpdaterPanel.SetBinding(ModUpdatesLayout.DataContextProperty, binding);
			}

			DataContext = ViewModel;

			this.WhenActivated(d =>
			{
				ViewModel.OnViewActivated(this);
				RegisterBindings();
			});

			AddHandler(UIElement.GotFocusEvent, new RoutedEventHandler(OnGotFocus));
		}

		void OnGotFocus(object sender, RoutedEventArgs e)
		{
			//Trace.WriteLine($"[OnGotFocus] {sender} {e.Source}");
		}

		private void OpenPreferences(bool switchToKeybindings = false)
		{
			if (!SettingsWindow.IsVisible)
			{
				if (switchToKeybindings == true)
				{
					ViewModel.Settings.SelectedTabIndex = SettingsWindow.PreferencesTabControl.Items.IndexOf(SettingsWindow.KeybindingsTabItem);
				}
				SettingsWindow.Init(this.ViewModel);
				SettingsWindow.Show();
				SettingsWindow.Owner = this;
				ViewModel.Settings.SettingsWindowIsOpen = true;
			}
			else
			{
				SettingsWindow.Hide();
				ViewModel.Settings.SettingsWindowIsOpen = false;
			}
		}

		private void ToggleAboutWindow()
		{
			if (AboutWindow == null)
			{
				AboutWindow = new AboutWindow();
			}

			if (!AboutWindow.IsVisible)
			{
				AboutWindow.DataContext = ViewModel;
				AboutWindow.Show();
				AboutWindow.Owner = this;
			}
			else
			{
				AboutWindow.Hide();
			}
		}

		private void RegisterBindings()
		{
			this.WhenAnyValue(x => x.ViewModel.Title).BindTo(this, view => view.Title);

			ViewModel.Keys.OpenPreferences.AddAction(() => OpenPreferences(false));
			ViewModel.Keys.OpenKeybindings.AddAction(() => OpenPreferences(true));
			ViewModel.Keys.OpenAboutWindow.AddAction(ToggleAboutWindow);

			this.WhenAnyValue(x => x.ViewModel.MainProgressIsActive).Subscribe((b) =>
			{
				if (b)
				{
					this.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
				}
				else
				{
					this.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
				}
			});

			ViewModel.Keys.ToggleVersionGeneratorWindow.AddAction(() =>
			{
				if (VersionGeneratorWindow == null)
				{
					VersionGeneratorWindow = new VersionGeneratorWindow();
				}

				if (!VersionGeneratorWindow.IsVisible)
				{
					VersionGeneratorWindow.Show();
					VersionGeneratorWindow.Owner = this;
				}
				else
				{
					VersionGeneratorWindow.Hide();
				}
			});

			//this.OneWayBind(ViewModel, vm => vm.MainProgressValue, view => view.TaskbarItemInfo.ProgressValue).DisposeWith(ViewModel.Disposables);

			this.WhenAnyValue(x => x.ViewModel.MainProgressValue).BindTo(this, view => view.TaskbarItemInfo.ProgressValue);

			foreach (var key in ViewModel.Keys.All)
			{
				var keyBinding = new KeyBinding(key.Command, key.Key, key.Modifiers);
				BindingOperations.SetBinding(keyBinding, InputBinding.CommandProperty, new Binding { Path = new PropertyPath("Command"), Source=key });
				BindingOperations.SetBinding(keyBinding, KeyBinding.KeyProperty, new Binding { Path = new PropertyPath("Key"), Source=key });
				BindingOperations.SetBinding(keyBinding, KeyBinding.ModifiersProperty, new Binding { Path = new PropertyPath("Modifiers"), Source=key });
				this.InputBindings.Add(keyBinding);
			}

			foreach (var entry in TopMenuBar.Items.Cast<MenuItem>())
			{
				menuItems.Add((string)entry.Header, entry);
			}

			//Generating menu items
			var menuKeyProperties = typeof(AppKeys)
			.GetRuntimeProperties()
			.Where(prop => Attribute.IsDefined(prop, typeof(MenuSettingsAttribute)))
			.Select(prop => typeof(AppKeys).GetProperty(prop.Name));
			foreach(var prop in menuKeyProperties)
			{
				Hotkey key = (Hotkey)prop.GetValue(ViewModel.Keys);
				MenuSettingsAttribute menuSettings = prop.GetCustomAttribute<MenuSettingsAttribute>();
				if (String.IsNullOrEmpty(key.DisplayName))
					key.DisplayName = menuSettings.DisplayName;

				MenuItem parentMenuItem;
				if (!menuItems.TryGetValue(menuSettings.Parent, out parentMenuItem))
				{
					parentMenuItem = new MenuItem();
					parentMenuItem.Header = menuSettings.Parent;
					TopMenuBar.Items.Add(parentMenuItem);
					menuItems.Add(menuSettings.Parent, parentMenuItem);
				}

				MenuItem newEntry = new MenuItem();
				newEntry.Header = menuSettings.DisplayName;
				newEntry.InputGestureText = key.ToString();
				newEntry.Command = key.Command;
				BindingOperations.SetBinding(newEntry, MenuItem.CommandProperty, new Binding { Path = new PropertyPath("Command"), Source = key });
				parentMenuItem.Items.Add(newEntry);
				if(!String.IsNullOrWhiteSpace(menuSettings.Tooltip))
				{
					newEntry.ToolTip = menuSettings.Tooltip;
				}
				if(!String.IsNullOrWhiteSpace(menuSettings.Style))
				{
					Style style = (Style)TryFindResource(menuSettings.Style);
					if(style != null)
					{
						newEntry.Style = style;
					}
				}

				if(menuSettings.AddSeparator)
				{
					parentMenuItem.Items.Add(new Separator());
				}

				menuItems.Add(prop.Name, newEntry);
			}
		}

		protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
		{
			return new CachedAutomationPeer(this);
		}

		public void UpdateColorTheme(bool darkMode)
		{
			ResourceLocator.SetColorScheme(this.Resources, !darkMode ? DivinityApp.LightTheme : DivinityApp.DarkTheme);
			ResourceLocator.SetColorScheme(SettingsWindow.Resources, !darkMode ? DivinityApp.LightTheme : DivinityApp.DarkTheme);
			if(AboutWindow != null)
			{
				ResourceLocator.SetColorScheme(AboutWindow.Resources, !darkMode ? DivinityApp.LightTheme : DivinityApp.DarkTheme);
			}
			if(VersionGeneratorWindow != null)
			{
				ResourceLocator.SetColorScheme(VersionGeneratorWindow.Resources, !darkMode ? DivinityApp.LightTheme : DivinityApp.DarkTheme);
			}
		}

		private void OnAppClosing(object sender, ExitEventArgs e)
		{
			ViewModel.SaveSettings();
		}

		private void AutoUpdater_ApplicationExitEvent()
		{
			ViewModel.Settings.LastUpdateCheck = DateTimeOffset.Now.ToUnixTimeSeconds();
			ViewModel.SaveSettings();
			App.Current.Shutdown();
		}

		private void AlertBar_Show(object sender, RoutedEventArgs e)
		{
			var spStandard = (StackPanel)AlertBar.FindName("spStandard");
			var spOutline = (StackPanel)AlertBar.FindName("spOutline");

			Grid grdParent;
			switch (AlertBar.Theme)
			{
				case DivinityModManager.Controls.AlertBar.ThemeType.Standard:
					grdParent = spStandard.FindVisualChildren<Grid>().FirstOrDefault();
					break;
				case DivinityModManager.Controls.AlertBar.ThemeType.Outline:
				default:
					grdParent = spOutline.FindVisualChildren<Grid>().FirstOrDefault();
					break;
			}

			TextBlock lblMessage = grdParent.FindVisualChildren<TextBlock>().FirstOrDefault();
			if(lblMessage != null)
			{
				DivinityApp.Log(lblMessage.Text);
			}
		}

		private void ExportTestButton_Click(object sender, RoutedEventArgs e)
		{
			//DivinityModDataLoader.ExportModOrder(Data.ActiveModOrder, Data.Mods);
		}

		private void ComboBox_KeyDown_LoseFocus(object sender, KeyEventArgs e)
		{
			bool loseFocus = false;
			if((e.Key == Key.Enter || e.Key == Key.Return))
			{
				UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;
				elementWithFocus.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
				ViewModel.StopRenaming(false);
				loseFocus = true;
				e.Handled = true;
			}
			else if(e.Key == Key.Escape)
			{
				ViewModel.StopRenaming(true);
				loseFocus = true;
			}

			if(loseFocus && sender is ComboBox comboBox)
			{
				var tb = comboBox.FindVisualChildren<TextBox>().FirstOrDefault();
				if (tb != null)
				{
					tb.Select(0, 0);
				}
			}
		}

		private void OrdersComboBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if(sender is ComboBox comboBox && comboBox.IsEditable)
			{
				RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), _ =>
				{
					if(ViewModel.IsRenamingOrder)
					{
						var tb = comboBox.FindVisualChildren<TextBox>().FirstOrDefault();
						if (tb != null && !tb.IsFocused)
						{
							ViewModel.StopRenaming(false);
						}
					}
				});
			}
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{

		}

		private void OrderComboBox_OnUserClick(object sender, MouseButtonEventArgs e)
		{
			RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(200), () =>
			{
				if (ViewModel.Settings != null && ViewModel.Settings.LastOrder != ViewModel.SelectedModOrder.Name)
				{
					ViewModel.Settings.LastOrder = ViewModel.SelectedModOrder.Name;
					ViewModel.SaveSettings();
				}
			});
		}

		private void OrdersComboBox_Loaded(object sender, RoutedEventArgs e)
		{
			if(sender is ComboBox ordersComboBox)
			{
				var tb = ordersComboBox.FindVisualChildren<TextBox>().FirstOrDefault();
				if(tb != null)
				{
					tb.ContextMenu = ordersComboBox.ContextMenu;
					tb.ContextMenu.DataContext = ViewModel;
				}
			}
		}

		private Dictionary<string, string> _shortcutButtonBindings = new Dictionary<string, string>()
		{
			["OpenWorkshopFolderButton"] = "Keys.OpenWorkshopFolder.Command",
			["OpenModsFolderButton"] = "Keys.OpenModsFolder.Command",
			["OpenExtenderLogsFolderButton"] = "Keys.OpenLogsFolder.Command",
			["OpenGameButton"] = "Keys.LaunchGame.Command",
			["LoadGameMasterModOrderButton"] = "Keys.ImportOrderFromSelectedGMCampaign.Command",
		};

		private void ModOrderPanel_Loaded(object sender, RoutedEventArgs e)
		{
			//var orderPanel = (Grid)this.FindResource("ModOrderPanel");
			if(sender is Grid orderPanel)
			{
				var buttons = orderPanel.FindVisualChildren<Button>();
				foreach(var button in buttons)
				{
					if(_shortcutButtonBindings.TryGetValue(button.Name, out string path))
					{
						if(button.Command == null)
						{
							BindingHelper.CreateCommandBinding(button, path, ViewModel);
						}
					}
				}
			};
		}

		private void GameMasterCampaignComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ViewModel.UserChangedSelectedGMCampaign = true;
		}
	}
}
