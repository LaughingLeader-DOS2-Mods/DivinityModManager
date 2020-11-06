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
	public partial class SettingsWindow : HideWindowBase, IViewFor<DivinityModManagerSettings>
	{
		public SettingsWindow()
		{
			InitializeComponent();
		}

		public DivinityModManagerSettings ViewModel { get; set; }
		object IViewFor.ViewModel { get; set; }

		public void Init(DivinityModManagerSettings vm)
		{
			ViewModel = vm;
			DataContext = ViewModel;

			this.WhenAnyValue(x => x.ViewModel.SaveSettingsCommand).BindTo(this, view => view.SaveSettingsButton.Command);
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

		private void TabItem_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if(sender is TabItem tabItem)
			{
				ViewModel.ExtenderTabIsVisible = tabItem.IsSelected;
			}
		}

		private void PreferencesTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ViewModel.ExtenderTabIsVisible = PreferencesTabControl.SelectedItem == this.OsirisExtenderTab;
		}
	}
}
