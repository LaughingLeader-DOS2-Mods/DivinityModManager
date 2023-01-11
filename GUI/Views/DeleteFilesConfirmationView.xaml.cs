using AutoUpdaterDotNET;

using DivinityModManager.Converters;
using DivinityModManager.ViewModels;

using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DivinityModManager.Views
{
	public class DeleteFilesConfirmationViewBase : ReactiveUserControl<DeleteFilesViewData> { }

	/// <summary>
	/// Interaction logic for DeleteFilesConfirmationView.xaml
	/// </summary>
	public partial class DeleteFilesConfirmationView : DeleteFilesConfirmationViewBase
	{
		public DeleteFilesConfirmationView()
		{
			InitializeComponent();

			this.ViewModel = new DeleteFilesViewData();
			this.DataContext = ViewModel;

			this.WhenActivated(d =>
			{
				if (this.ViewModel != null)
				{
					d(this.OneWayBind(ViewModel, vm => vm.IsActive, view => view.Visibility, BoolToVisibilityConverter.FromBool));
					d(this.OneWayBind(ViewModel, vm => vm.IsRunning, v => v.ProgressIndicator.IsBusy));
					d(this.OneWayBind(ViewModel, vm => vm.Files, v => v.FilesListView.ItemsSource));

					d(this.OneWayBind(ViewModel, vm => vm.ProgressTitle, v => v.TaskProgressTitleText.Text));
					d(this.OneWayBind(ViewModel, vm => vm.ProgressWorkText, v => v.TaskProgressWorkText.Text));
					d(this.OneWayBind(ViewModel, vm => vm.ProgressValue, v => v.TaskProgressBar.Value));
					d(this.OneWayBind(ViewModel, vm => vm.IsProgressActive, view => view.TaskProgressBar.Visibility, BoolToVisibilityConverter.FromBool));
					d(this.Bind(ViewModel, vm => vm.PermanentlyDelete, view => view.DeletionOptionCheckbox.IsChecked));
					d(this.Bind(ViewModel, vm => vm.RemoveFromLoadOrder, view => view.RemoveFromLoadOrderCheckbox.IsChecked));

					//d(this.BindCommand(ViewModel, vm => vm.SelectAllCommand, v => v.ConfirmButton));
					d(this.BindCommand(ViewModel, vm => vm.RunCommand, v => v.ConfirmButton));
					d(this.BindCommand(ViewModel, vm => vm.CancelCommand, v => v.CancelButton));
					d(this.BindCommand(ViewModel, vm => vm.StopRunningCommand, v => v.CancelProgressButton));
				}
			});
		}
	}
}
