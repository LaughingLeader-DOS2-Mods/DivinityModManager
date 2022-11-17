using AutoUpdaterDotNET;

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
					d(this.OneWayBind(ViewModel, vm => vm.Files, v => v.FilesListView.ItemsSource));
				}
			});
		}
	}
}
