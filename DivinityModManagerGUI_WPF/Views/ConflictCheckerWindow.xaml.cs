using DivinityModManager.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DivinityModManager.Views
{
	/// <summary>
	/// Interaction logic for ConflictCheckerWindow.xaml
	/// </summary>
	public partial class ConflictCheckerWindow : ReactiveWindow<ConflictCheckerWindowViewModel>
	{

		public ConflictCheckerWindow()
		{
			InitializeComponent();

			ViewModel = new ConflictCheckerWindowViewModel();
			DataContext = ViewModel;

			this.OneWayBind(ViewModel, vm => vm.ConflictGroups, view => view.ConflictsTabControlPanel.ItemsSource).DisposeWith(ViewModel.Disposables);

			this.WhenActivated(d =>
			{
				d.Add(ViewModel.Disposables);

				this.OneWayBind(ViewModel, vm => vm.Profiles, view => view.ProfilesComboBox.ItemsSource).DisposeWith(ViewModel.Disposables);
				this.OneWayBind(ViewModel, vm => vm.SelectedProfileIndex, view => view.ProfilesComboBox.SelectedIndex).DisposeWith(ViewModel.Disposables);

				this.OneWayBind(ViewModel, vm => vm.LoadOrders, view => view.OrdersComboBox.ItemsSource).DisposeWith(ViewModel.Disposables);
				this.OneWayBind(ViewModel, vm => vm.SelectedLoadOrderIndex, view => view.OrdersComboBox.SelectedIndex).DisposeWith(ViewModel.Disposables);
			});
		}

		public void Init(MainWindowViewModel mainWindowViewModel)
		{
			ViewModel.OnActivated(this, mainWindowViewModel);
		}

		private void ComboBox_KeyDown_LoseFocus(object sender, KeyEventArgs e)
		{
			if ((e.Key == Key.Enter || e.Key == Key.Return))
			{
				UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;
				elementWithFocus.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
				e.Handled = true;
			}
		}
	}
}
