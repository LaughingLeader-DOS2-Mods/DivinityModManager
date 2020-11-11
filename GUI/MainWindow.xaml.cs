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

		private void CreateButtonBinding(Button button, string vmProperty, object source = null)
		{
			if (source == null) source = ViewModel;
			Binding binding = new Binding(vmProperty);
			binding.Source = source;
			binding.Mode = BindingMode.OneWay;
			button.SetBinding(Button.CommandProperty, binding);
		}

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
		}

		private void RegisterBindings()
		{
			this.WhenAnyValue(x => x.ViewModel.Title).BindTo(this, view => view.Title);

			var c = ReactiveCommand.Create(() =>
			{
				if (!SettingsWindow.IsVisible)
				{
					SettingsWindow.Init(this.ViewModel.Settings);
					SettingsWindow.Show();
					SettingsWindow.Owner = this;
					ViewModel.Settings.SettingsWindowIsOpen = true;
				}
				else
				{
					SettingsWindow.Hide();
					ViewModel.Settings.SettingsWindowIsOpen = false;
				}
			});
			c.ThrownExceptions.Subscribe((ex) =>
			{
				DivinityApp.Log("Error opening settings window: " + ex.ToString());
			});
			ViewModel.OpenPreferencesCommand = c;

			ViewModel.OpenAboutWindowCommand = ReactiveCommand.Create(() =>
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
			});

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

			ViewModel.ToggleVersionGeneratorWindowCommand = ReactiveCommand.Create(() =>
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

			this.WhenAnyValue(x => x.ViewModel.AddOrderConfigCommand).BindTo(this, view => view.FileAddNewOrderMenuItem.Command);
			this.WhenAnyValue(x => x.ViewModel.SaveOrderCommand).BindTo(this, view => view.FileSaveOrderMenuItem.Command);
			this.WhenAnyValue(x => x.ViewModel.SaveOrderAsCommand).BindTo(this, view => view.FileSaveOrderAsMenuItem.Command);

			this.WhenAnyValue(x => x.ViewModel.ImportOrderFromSaveCommand).BindTo(this, view => view.FileImportOrderFromSave.Command);
			this.WhenAnyValue(x => x.ViewModel.ImportOrderFromSaveAsNewCommand).BindTo(this, view => view.FileImportOrderFromSaveAsNew.Command);
			this.WhenAnyValue(x => x.ViewModel.ImportOrderFromFileCommand).BindTo(this, view => view.FileImportOrderFromFile.Command);
			this.WhenAnyValue(x => x.ViewModel.ImportOrderZipFileCommand).BindTo(this, view => view.FileImportOrderZip.Command);
			this.FileImportOrderZip.Visibility = Visibility.Collapsed; // Disabled for now

			this.WhenAnyValue(x => x.ViewModel.ExportOrderCommand).BindTo(this, view => view.FileExportOrderToGameMenuItem.Command);
			this.WhenAnyValue(x => x.ViewModel.ExportLoadOrderAsArchiveCommand).BindTo(this, view => view.FileExportOrderToArchiveMenuItem.Command);
			this.WhenAnyValue(x => x.ViewModel.ExportLoadOrderAsArchiveToFileCommand).BindTo(this, view => view.FileExportOrderToArchiveAsMenuItem.Command);
			this.WhenAnyValue(x => x.ViewModel.ExportLoadOrderAsTextFileCommand).BindTo(this, view => view.FileExportOrderToTextListMenuItem.Command);

			this.WhenAnyValue(x => x.ViewModel.RefreshCommand).BindTo(this, view => view.FileRefreshMenuItem.Command);

			this.WhenAnyValue(x => x.ViewModel.ToggleDisplayNameCommand).BindTo(this, view => view.EditToggleFileNameDisplayMenuItem.Command);

			this.WhenAnyValue(x => x.ViewModel.OpenPreferencesCommand).BindTo(this, view => view.SettingsPreferencesMenuItem.Command);
			this.WhenAnyValue(x => x.ViewModel.ToggleDarkModeCommand).BindTo(this, view => view.SettingsDarkModeMenuItem.Command);

			this.WhenAnyValue(x => x.ViewModel.DownloadAndInstallOsiExtenderCommand).BindTo(this, view => view.ToolsInstallOsiExtenderMenuItem.Command);
			this.WhenAnyValue(x => x.ViewModel.ExtractSelectedModsCommand).BindTo(this, view => view.ToolsExtractSelectedModsMenuItem.Command);
			this.WhenAnyValue(x => x.ViewModel.ToggleVersionGeneratorWindowCommand).BindTo(this, view => view.ToolsToggleVersionGeneratorWindowMenuItem.Command);
			this.WhenAnyValue(x => x.ViewModel.RenameSaveCommand).BindTo(this, view => view.ToolsRenameSaveMenuItem.Command);

			this.WhenAnyValue(x => x.ViewModel.CheckForAppUpdatesCommand).BindTo(this, view => view.HelpCheckForUpdateMenuItem.Command);
			this.WhenAnyValue(x => x.ViewModel.OpenDonationPageCommand).BindTo(this, view => view.HelpDonationMenuItem.Command);
			this.WhenAnyValue(x => x.ViewModel.OpenRepoPageCommand).BindTo(this, view => view.HelpOpenRepoPageMenuItem.Command);
			this.WhenAnyValue(x => x.ViewModel.OpenAboutWindowCommand).BindTo(this, view => view.HelpOpenAboutWindowMenuItem.Command);
		}

		public void UpdateColorTheme(bool darkMode)
		{
			ResourceLocator.SetColorScheme(this.Resources, !darkMode ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme);
			ResourceLocator.SetColorScheme(SettingsWindow.Resources, !darkMode ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme);
			if(AboutWindow != null)
			{
				ResourceLocator.SetColorScheme(AboutWindow.Resources, !darkMode ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme);
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

		private Dictionary<string, string> _buttonBindings = new Dictionary<string, string>()
		{
			["OpenWorkshopFolderButton"] = "OpenWorkshopFolderCommand",
			["OpenModsFolderButton"] = "OpenModsFolderCommand",
			["OpenExtenderLogsFolderButton"] = "OpenExtenderLogDirectoryCommand",
			["OpenGameButton"] = "OpenGameCommand"
		};

		private void ModOrderPanel_Loaded(object sender, RoutedEventArgs e)
		{
			//var orderPanel = (Grid)this.FindResource("ModOrderPanel");
			if(sender is Grid orderPanel)
			{
				var buttons = orderPanel.FindVisualChildren<Button>();
				foreach(var button in buttons)
				{
					if(_buttonBindings.TryGetValue(button.Name, out string command))
					{
						CreateButtonBinding(button, command);
					}
				}
			};
		}
	}
}
