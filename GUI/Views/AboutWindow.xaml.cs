using ReactiveUI;
using ReactiveUI.Fody.Helpers;

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
using System.Windows.Shapes;

namespace DivinityModManager.Views
{
	public class AboutWindowBase : HideWindowBase<AboutWindowViewModel> { }

	public class AboutWindowViewModel : ReactiveObject
	{
		[Reactive] public string Title { get; set; }

		public AboutWindowViewModel()
		{
			Title = "About";
		}
	}

	/// <summary>
	/// Interaction logic for AboutWindow.xaml
	/// </summary>
	public partial class AboutWindow : AboutWindowBase
	{
		public AboutWindow()
		{
			InitializeComponent();

			ViewModel = new AboutWindowViewModel();

			this.WhenActivated(d =>
			{
				d(this.OneWayBind(ViewModel, vm => vm.Title, v => v.TitleText.Text));
				d(this.OneWayBind(ViewModel, vm => vm.Title, v => v.Title));
			});
		}
	}
}
