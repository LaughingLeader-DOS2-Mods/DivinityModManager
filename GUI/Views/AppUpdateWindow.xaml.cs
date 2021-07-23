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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace DivinityModManager.Views
{
	public partial class AppUpdateWindow : HideWindowBase
	{
		public UpdateInfoEventArgs UpdateArgs { get; set; }

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

			var obj = TryFindResource("DefaultMarkdown");
			if (obj != null && obj is Markdown markdown)
			{
				_defaultMarkdown = markdown;
			}

			ConfirmButton.Click += (o, e) =>
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
					Hide();
				}
			};

			SkipButton.Click += (o, e) =>
			{
				Hide();
			};
		}
		public void Init(UpdateInfoEventArgs args)
		{
			UpdateArgs = args;
			//Title = $"{AutoUpdater.AppTitle} {args.CurrentVersion}";

			string markdownText = "";

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
				UpdateChangelogView.Document = StringToMarkdown(markdownText);
			}

			if (args.IsUpdateAvailable)
			{
				UpdateDescription.Text = $"{AutoUpdater.AppTitle} {args.CurrentVersion} is now available.{Environment.NewLine}You have version {args.InstalledVersion} installed.";

				ConfirmButton.IsEnabled = true;
				SkipButton.Content = "Skip";
				SkipButton.IsEnabled = args.Mandatory?.Value != true;
			}
			else
			{
				UpdateDescription.Text = $"{AutoUpdater.AppTitle} is up-to-date.";
				ConfirmButton.IsEnabled = false;
				SkipButton.Content = "Close";
			}

			/*
			if (args.IsUpdateAvailable)
			{
				MessageBoxResult dialogResult;
				if (args.Mandatory.Value)
				{
					dialogResult = MessageBox.Show(new MessageBoxModel
					{
						Caption = "Update Available",
						Text = $@"There is new version {args.CurrentVersion} available. You are using version {args.InstalledVersion}. This is required update. Press Ok to begin updating the application.",
						Icon = MessageBoxImage.Information,
						Buttons = new IMessageBoxButtonModel[1] { MessageBoxButtons.Ok() },
					});
				}
				else
				{
					dialogResult = MessageBox.Show(new MessageBoxModel
					{
						Caption = "Update Available",
						Text = $@"There is new version {args.CurrentVersion} available. You are using version {args.InstalledVersion}. Do you want to update the application now?",
						Icon = MessageBoxImage.Information,
						Buttons = MessageBoxButtons.YesNo(),
					});
				}

				if (dialogResult.Equals(MessageBoxResult.Yes) || dialogResult.Equals(MessageBoxResult.OK))
				{
					try
					{
						if (AutoUpdater.DownloadUpdate(args))
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
					}
				}
			}
			else
			{
				MessageBox.Show(new MessageBoxModel
				{
					Caption = "No Update Available",
					Text = @"There is no update available please try again later.",
					Icon = MessageBoxImage.Information,
					Buttons = new IMessageBoxButtonModel[1] { MessageBoxButtons.Ok() },
				});
			}
			*/
		}

		public void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
		{
			if (args.Error == null)
			{
				Init(args);

				if(MainWindow.Self.UserInvokedUpdate || args.IsUpdateAvailable)
				{
					Owner = MainWindow.Self;
					Show();
					MainWindow.Self.UserInvokedUpdate = false;
				}
			}
			else
			{
				if (args.Error is System.Net.WebException)
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
						Caption = args.Error.GetType().ToString(),
						Text = args.Error.Message,
						Icon = MessageBoxImage.Error,
						Buttons = new IMessageBoxButtonModel[1] { MessageBoxButtons.Ok() },
					});
				}
			}
		}
	}
}
