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

namespace DivinityModManager.Views
{
	public class MainWindowDebugData : MainWindowViewModel
	{
		public MainWindowDebugData () : base()
		{
			Random rnd = new Random();
			for(var i = 0; i < 60; i++)
			{
				var d = new DivinityModData()
				{
					Name = "Test" + i,
					Author = "LaughingLeader",
					Version = new DivinityModVersion(370871668),
					Description = "Test",
					UUID = DivinityModDataLoader.CreateHandle()
				};
				d.IsEditorMod = i%2 == 0;
				d.IsActive = rnd.Next(4) <= 2;
				this.AddMods(d);
			}

			for (var i = 0; i < 4; i++)
			{
				var p = new DivinityProfileData()
				{
					Name = "Profile" + i
				};
				p.ModOrder.AddRange(Mods.Select(m => m.UUID));
				Profiles.Add(p);
			}
			SelectedProfileIndex = 0;

			for (var i = 0; i < 4; i++)
			{
				var lo = new DivinityLoadOrder()
				{
					Name = "SavedOrder" + i
				};
				var orderList = this.mods.Items.Where(m => m.IsActive).Select(m => m.ToOrderEntry()).OrderBy(e => rnd.Next(10));
				lo.SetOrder(orderList);
				ModOrderList.Add(lo);
			}
			SelectedModOrderIndex = 2;
		}
	}
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
	{
		private static MainWindow self;
		public static MainWindow Self => self;
		public MainWindow()
		{
			InitializeComponent();

			self = this;

			ViewModel = new MainWindowViewModel();

			this.WhenActivated(disposableRegistration =>
			{
				DataContext = ViewModel;
				//this.OneWayBind(ViewModel, vm => vm, view => view.DataContext).DisposeWith(disposableRegistration);

				this.OneWayBind(ViewModel,
					viewModel => viewModel.Title,
					view => view.Title).DisposeWith(disposableRegistration);

				//this.OneWayBind(ViewModel, vm => vm, view => view.LayoutContent.Content).DisposeWith(disposableRegistration);

				this.OneWayBind(ViewModel, vm => vm.SaveOrderCommand, view => view.SaveButton.Command).DisposeWith(disposableRegistration);
				this.OneWayBind(ViewModel, vm => vm.SaveOrderAsCommand, view => view.SaveAsButton.Command).DisposeWith(disposableRegistration);
				this.OneWayBind(ViewModel, vm => vm.ExportOrderCommand, view => view.ExportToModSettingsButton.Command).DisposeWith(disposableRegistration);

				this.OneWayBind(ViewModel, vm => vm.AddOrderConfigCommand, view => view.AddNewOrderButton.Command).DisposeWith(disposableRegistration);

				this.OneWayBind(ViewModel, vm => vm.Profiles, view => view.ProfilesComboBox.ItemsSource).DisposeWith(disposableRegistration);
				this.Bind(ViewModel, vm => vm.SelectedProfileIndex, view => view.ProfilesComboBox.SelectedIndex).DisposeWith(disposableRegistration);

				this.OneWayBind(ViewModel, vm => vm.ModOrderList, view => view.OrdersComboBox.ItemsSource).DisposeWith(disposableRegistration);
				this.Bind(ViewModel, vm => vm.SelectedModOrderIndex, view => view.OrdersComboBox.SelectedIndex).DisposeWith(disposableRegistration);

				ViewModel.Refresh();
			});
		}

		private void ExportTestButton_Click(object sender, RoutedEventArgs e)
		{
			//DivinityModDataLoader.ExportModOrder(Data.ActiveModOrder, Data.Mods);
		}

		private void ComboBox_KeyDown_LoseFocus(object sender, KeyEventArgs e)
		{
			if((e.Key == Key.Enter || e.Key == Key.Return))
			{
				UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;
				elementWithFocus.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
				e.Handled = true;
			}
		}
	}
}
