using DivinityModManager.Models;

using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Concurrency;
using ReactiveUI.Fody.Helpers;

namespace DivinityModManager.Views
{
	public class VersionGeneratorData : ReactiveObject
	{
		[Reactive] public long VersionNumber { get; set; }

		[Reactive] public long Major { get; set; }

		[Reactive] public long Minor { get; set; }

		[Reactive] public long Revision { get; set; }

		[Reactive] public long Build { get; set; }
	}

	/// <summary>
	/// Interaction logic for VersionGenerator.xaml
	/// </summary>
	public partial class VersionGeneratorWindow : HideWindowBase
	{
		public DivinityModVersion VersionData { get; set; } = new DivinityModVersion(268435456);

		public VersionGeneratorWindow()
		{
			InitializeComponent();

			DataContext = VersionData;
		}

		private Regex _numberOnlyRegex = new Regex("[^0-9]+");
		private void VersionNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = _numberOnlyRegex.IsMatch(e.Text);
		}

		private void IntegerUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (VersionNumberTextBox != null)
			{
				RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(50), _ =>
				{
					VersionNumberTextBox.Text = VersionData.VersionInt.ToString();
				});
			}
		}

		private void CopyButton_Click(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(VersionData.VersionInt.ToString());
			AlertBar.SetSuccessAlert($"Copied {VersionData.VersionInt} to the clipboard.");
		}
		private void ResetButton_Click(object sender, RoutedEventArgs e)
		{
			VersionData.ParseInt(268435456);
			VersionNumberTextBox.Text = "268435456";
			AlertBar.SetWarningAlert($"Reset version number.");
		}

		private void IntegerUpDown_LostFocus(object sender, RoutedEventArgs e)
		{
			if (VersionNumberTextBox != null)
			{
				VersionNumberTextBox.Text = VersionData.VersionInt.ToString();
			}
		}

		private void VersionNumberTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (sender is TextBox tb)
			{
				if (Int32.TryParse(tb.Text, out int version))
				{
					VersionData.ParseInt(version);
				}
				else
				{
					VersionData.ParseInt(268435456);
					tb.Text = "268435456";
				}
			}
		}
	}
}
