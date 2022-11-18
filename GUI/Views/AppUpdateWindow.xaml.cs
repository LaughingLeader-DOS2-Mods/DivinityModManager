using AdonisUI.Controls;

using AutoUpdaterDotNET;

using DivinityModManager.Controls;
using DivinityModManager.Converters;
using DivinityModManager.Util;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;

namespace DivinityModManager.Views
{
	public class AppUpdateWindowBase : HideWindowBase<AppUpdateWindowViewModel> { }

	public class AppUpdateWindowViewModel : ReactiveObject
	{
		public UpdateInfoEventArgs UpdateArgs { get; set; }

		[Reactive] public bool Visible { get; set; }
		[Reactive] public bool CanConfirm { get; set; }
		[Reactive] public bool CanSkip { get; set; }
		[Reactive] public string SkipButtonText { get; set; }
		[Reactive] public string UpdateDescription { get; set; }
		[Reactive] public string UpdateChangelogView { get; set; }

		public ICommand ConfirmCommand { get; private set; }
		public ICommand SkipCommand { get; private set; }
		public ReactiveCommand<UpdateInfoEventArgs, Unit> CheckForUpdatesCommand { get; private set; }

		public void CheckArgs(UpdateInfoEventArgs args)
		{
			UpdateArgs = args;
			//Title = $"{AutoUpdater.AppTitle} {args.CurrentVersion}";

			string markdownText;

			if (!args.ChangelogURL.EndsWith(".md"))
			{
				markdownText = WebHelper.DownloadUrlAsString(DivinityApp.URL_CHANGELOG_RAW);
			}
			else
			{
				markdownText = WebHelper.DownloadUrlAsString(args.ChangelogURL);
			}
			if (!String.IsNullOrEmpty(markdownText))
			{
				markdownText = Regex.Replace(markdownText, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
				UpdateChangelogView = markdownText;
			}

			if (args.IsUpdateAvailable)
			{
				UpdateDescription = $"{AutoUpdater.AppTitle} {args.CurrentVersion} is now available.{Environment.NewLine}You have version {args.InstalledVersion} installed.";

				CanConfirm = true;
				SkipButtonText = "Skip";
				CanSkip = args.Mandatory?.Value != true;
			}
			else
			{
				UpdateDescription = $"{AutoUpdater.AppTitle} is up-to-date.";
				CanConfirm = false;
				CanSkip = true;
				SkipButtonText = "Close";
			}
		}

		public AppUpdateWindowViewModel()
		{
			var canConfirm = this.WhenAnyValue(x => x.CanConfirm);
			ConfirmCommand = ReactiveCommand.Create(() =>
			{
				try
				{
					if (AutoUpdater.DownloadUpdate(UpdateArgs))
					{
						System.Windows.Application.Current.Shutdown();
					}
				}
				catch (Exception exception)
				{
					MessageBox.Show(new MessageBoxModel
					{
						Caption = exception.GetType().ToString(),
						Text = exception.Message,
						Icon = MessageBoxImage.Error,
						Buttons = new IMessageBoxButtonModel[1] { MessageBoxButtons.Ok() },
					});
					Visible = false;
				}
			}, canConfirm, RxApp.MainThreadScheduler);

			var canSkip = this.WhenAnyValue(x => x.CanSkip);
			SkipCommand = ReactiveCommand.Create(() => Visible = false, canSkip);

			CheckForUpdatesCommand = ReactiveCommand.Create<UpdateInfoEventArgs, Unit>(e =>
			{
				DivinityApp.Log("CheckForUpdatesCommand");
				if (e.Error == null)
				{
					CheckArgs(e);

					if (MainWindow.Self.UserInvokedUpdate || e.IsUpdateAvailable)
					{
						Visible = true;
						MainWindow.Self.UserInvokedUpdate = false;
					}
				}
				else
				{
					if (e.Error is System.Net.WebException)
					{
						MessageBox.Show(new MessageBoxModel
						{
							Caption = "Update Check Failed",
							Text = "There is a problem reaching update server. Please check your internet connection and try again later.",
							Icon = MessageBoxImage.Error,
							Buttons = new IMessageBoxButtonModel[1] { MessageBoxButtons.Ok() },
						});
					}
					else
					{
						MessageBox.Show(new MessageBoxModel
						{
							Caption = e.Error.GetType().ToString(),
							Text = e.Error.Message,
							Icon = MessageBoxImage.Error,
							Buttons = new IMessageBoxButtonModel[1] { MessageBoxButtons.Ok() },
						});
					}
				}
				return Unit.Default;
			});
		}
	}

	public partial class AppUpdateWindow : AppUpdateWindowBase
	{
		private readonly Lazy<Markdown> _fallbackMarkdown = new Lazy<Markdown>(() => new Markdown());
		private Markdown _defaultMarkdown;

		private FlowDocument StringToMarkdown(string text)
		{
			var markdown = _defaultMarkdown ?? _fallbackMarkdown.Value;
			var doc = markdown.Transform(text);
			return doc;
		}

		public AppUpdateWindow()
		{
			InitializeComponent();

			ViewModel = new AppUpdateWindowViewModel();

			this.ViewModel.WhenAnyValue(x => x.Visible).ObserveOn(RxApp.MainThreadScheduler).Subscribe(b =>
			{
				if (b)
				{
					if (!this.IsVisible)
					{
						Owner = MainWindow.Self;
						Show();
					}
				}
				else
				{
					if (this.IsVisible)
					{
						Hide();
					}
				}
			});

			Observable.FromEvent<AutoUpdater.CheckForUpdateEventHandler, UpdateInfoEventArgs>(
				e => AutoUpdater.CheckForUpdateEvent += e,
				e => AutoUpdater.CheckForUpdateEvent -= e)
			.InvokeCommand(ViewModel.CheckForUpdatesCommand);

			this.WhenActivated(d =>
			{
				var obj = TryFindResource("DefaultMarkdown");
				if (obj != null && obj is Markdown markdown)
				{
					_defaultMarkdown = markdown;
				}

				d(this.BindCommand(ViewModel, vm => vm.ConfirmCommand, v => v.ConfirmButton));
				d(this.BindCommand(ViewModel, vm => vm.SkipCommand, v => v.SkipButton));
				d(this.OneWayBind(ViewModel, vm => vm.SkipButtonText, v => v.SkipButton.Content));
				d(this.OneWayBind(ViewModel, vm => vm.UpdateDescription, v => v.UpdateDescription.Text));
				d(this.OneWayBind(ViewModel, vm => vm.UpdateChangelogView, v => v.UpdateChangelogView.Document, StringToMarkdown));
			});
		}
	}
}
