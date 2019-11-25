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

namespace DivinityModManager.Views
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : ReactiveWindow<DivinityModManagerSettings>
	{
		public SettingsWindow()
		{
			InitializeComponent();
		}

		public void Init(DivinityModManagerSettings vm)
		{
			ViewModel = vm;
			DataContext = ViewModel;

			this.OneWayBind(ViewModel, x => x.SaveSettingsCommand, view => view.SaveSettingsButton.Command);
		}
	}
}
