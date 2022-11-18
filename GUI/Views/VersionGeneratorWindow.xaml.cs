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
using System.Reactive.Linq;
using DivinityModManager.Controls;
using Xceed.Wpf.Toolkit;

namespace DivinityModManager.Views
{
	public class VersionGeneratorViewModel : ReactiveObject
	{
		[Reactive] public DivinityModVersion Version { get; set; }
		[Reactive] public string Text { get; set; }

		public ICommand CopyCommand { get; private set; }
		public ICommand ResetCommand { get; private set; }
		public ReactiveCommand<KeyboardFocusChangedEventArgs, Unit> UpdateVersionFromTextCommand { get; private set; }

		public VersionGeneratorViewModel(AlertBar alert)
		{
			Version = new DivinityModVersion(268435456);

			CopyCommand = ReactiveCommand.Create(() =>
			{
				Clipboard.SetText(Version.VersionInt.ToString());
				alert.SetSuccessAlert($"Copied {Version.VersionInt} to the clipboard.");
			});

			ResetCommand = ReactiveCommand.Create(() =>
			{
				Version.ParseInt(268435456);
				Text = "268435456";
				alert.SetWarningAlert($"Reset version number.");
			});

			UpdateVersionFromTextCommand = ReactiveCommand.Create<KeyboardFocusChangedEventArgs, Unit>(e =>
			{
				if (Int32.TryParse(Text, out int version))
				{
					Version.ParseInt(version);
				}
				else
				{
					Version.ParseInt(268435456);
				}
				return Unit.Default;
			});

			this.WhenAnyValue(x => x.Version.VersionInt).Throttle(TimeSpan.FromMilliseconds(50)).ObserveOn(RxApp.MainThreadScheduler).Subscribe(v =>
			{
				Text = v.ToString();
			});
		}
	}

	public class VersionGeneratorWindowBase : HideWindowBase<VersionGeneratorViewModel> { }

	/// <summary>
	/// Interaction logic for VersionGenerator.xaml
	/// </summary>
	public partial class VersionGeneratorWindow : VersionGeneratorWindowBase
	{
		private static readonly Regex _numberOnlyRegex = new Regex("[^0-9]+");

		public VersionGeneratorWindow()
		{
			InitializeComponent();

			ViewModel = new VersionGeneratorViewModel(AlertBar);

			this.WhenActivated(d =>
			{
				d(this.Bind(ViewModel, vm => vm.Text, v => v.VersionNumberTextBox.Text));
				d(this.Bind(ViewModel, vm => vm.Version.Major, v => v.MajorIntegerUpDown.Value));
				d(this.Bind(ViewModel, vm => vm.Version.Minor, v => v.MinorIntegerUpDown.Value));
				d(this.Bind(ViewModel, vm => vm.Version.Revision, v => v.RevisionIntegerUpDown.Value));
				d(this.Bind(ViewModel, vm => vm.Version.Build, v => v.BuildIntegerUpDown.Value));
				d(this.BindCommand(ViewModel, vm => vm.CopyCommand, v => v.CopyButton));
				d(this.BindCommand(ViewModel, vm => vm.ResetCommand, v => v.ResetButton));

				var tbEvents = this.VersionNumberTextBox.Events();
				d(tbEvents.LostKeyboardFocus.ObserveOn(RxApp.MainThreadScheduler).InvokeCommand(ViewModel.UpdateVersionFromTextCommand));
				d(tbEvents.PreviewTextInput.ObserveOn(RxApp.MainThreadScheduler).Subscribe((e) =>
				{
					e.Handled = _numberOnlyRegex.IsMatch(e.Text);
				}));
			});
		}
	}
}
