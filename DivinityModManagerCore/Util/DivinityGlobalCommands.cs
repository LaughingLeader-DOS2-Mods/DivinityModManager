using Alphaleonis.Win32.Filesystem;

using DivinityModManager.Models;
using DivinityModManager.Models.App;
using DivinityModManager.ViewModels;

using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DivinityModManager.Util
{
	public class DivinityGlobalCommands
	{
		private IDivinityAppViewModel _viewModel;

		public void SetViewModel(IDivinityAppViewModel vm)
		{
			_viewModel = vm;
		}

		public ReactiveCommand<string, Unit> OpenFileCommand { get; private set; }
		public ReactiveCommand<string, Unit> OpenInFileExplorerCommand { get; private set; }
		public ICommand ClearMissingModsCommand { get; private set; }
		public ReactiveCommand<DivinityModData, Unit> ToggleNameDisplayCommand { get; private set; }
		public ReactiveCommand<string, Unit> CopyToClipboardCommand { get; private set; }
		public ReactiveCommand<DivinityModData, Unit> DeleteModCommand { get; private set; }

		public void OpenFile(string path)
		{
			if (File.Exists(path))
			{
				try
				{
					Process.Start(Path.GetFullPath(path));
				}
				catch (System.ComponentModel.Win32Exception ex) // No File Association
				{
					Process.Start("explorer.exe", $"\"{Path.GetFullPath(path)}\"");
				}
			}
			else if (Directory.Exists(path))
			{
				Process.Start("explorer.exe", $"\"{Path.GetFullPath(path)}\"");
			}
			else
			{
				_viewModel.ShowAlert($"Error opening '{path}': File does not exist!", AlertType.Danger, 10);
			}
		}

		public void OpenInFileExplorer(string path)
		{
			if (File.Exists(path))
			{
				Process.Start("explorer.exe", $"/select, \"{Path.GetFullPath(path)}\"");
			}
			else if (Directory.Exists(path))
			{
				Process.Start("explorer.exe", $"\"{Path.GetFullPath(path)}\"");
			}
			else
			{
				_viewModel.ShowAlert($"Error opening '{path}': File does not exist!", AlertType.Danger, 10);
			}
		}

		public void CopyToClipboard(string text)
		{
			try
			{
				Clipboard.SetText(text);
				_viewModel.ShowAlert("Copied text to clipboard.", 0, 10);
			}
			catch (Exception ex)
			{
				_viewModel.ShowAlert($"Error copying text to clipboard: {ex}", AlertType.Danger, 10);
			}
		}

		public void ClearMissingMods()
		{
			if (_viewModel != null)
			{
				_viewModel.ClearMissingMods();
			}
		}

		public DivinityGlobalCommands()
		{
			OpenFileCommand = ReactiveCommand.Create<string>(OpenFile);
			OpenInFileExplorerCommand = ReactiveCommand.Create<string>(OpenInFileExplorer);
			ClearMissingModsCommand = ReactiveCommand.Create(ClearMissingMods);
			ToggleNameDisplayCommand = ReactiveCommand.Create<DivinityModData>((mod) =>
			{
				mod.DisplayFileForName = !mod.DisplayFileForName;
				if (_viewModel != null)
				{
					var b = mod.DisplayFileForName;
					foreach (var m in _viewModel.Mods)
					{
						if (m.IsSelected)
						{
							m.DisplayFileForName = b;
						}
					}
				}
			});
			CopyToClipboardCommand = ReactiveCommand.Create<string>(CopyToClipboard);
			DeleteModCommand = ReactiveCommand.Create<DivinityModData>((mod) =>
			{
				if (mod.CanDelete && _viewModel != null)
				{
					_viewModel.ConfirmDeleteMod(mod);
				}
			});
		}
	}
}
