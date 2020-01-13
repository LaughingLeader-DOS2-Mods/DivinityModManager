using Alphaleonis.Win32.Filesystem;
using DivinityModManager.Models;
using DivinityModManager.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Util
{
	public class DivinityGlobalCommands
	{
		public ReactiveCommand<string, Unit> OpenInFileExplorerCommand { get; private set; }
		public ReactiveCommand<DivinityModData, Unit> ToggleNameDisplayCommand { get; private set; }

		public void OpenInFileExplorer(string path)
		{
			if(File.Exists(path))
			{
				Process.Start("explorer.exe", $"/select, \"{Path.GetFullPath(path)}\"");
			}
			else if(Directory.Exists(path))
			{
				Process.Start("explorer.exe", $"\"{Path.GetFullPath(path)}\"");
			}
			else
			{
				_viewModel.ShowAlert($"Error opening '{path}': File does not exist!", -1, 10);
			}
		}

		private IDivinityAppViewModel _viewModel;

		public void SetViewModel(IDivinityAppViewModel vm)
		{
			_viewModel = vm;
		}

		public DivinityGlobalCommands()
		{
			OpenInFileExplorerCommand = ReactiveCommand.Create<string>(OpenInFileExplorer);
			ToggleNameDisplayCommand = ReactiveCommand.Create<DivinityModData>((mod) =>
			{
				mod.DisplayFileForName = !mod.DisplayFileForName;
				if (_viewModel != null)
				{
					if (_viewModel.ActiveSelected > 1 && _viewModel.ActiveMods.Contains(mod))
					{
						foreach(var m in _viewModel.ActiveMods.Where(x => x.IsSelected))
						{
							m.DisplayFileForName = mod.DisplayFileForName;
						}
					}
					else if (_viewModel.InactiveSelected > 1 && _viewModel.InactiveMods.Contains(mod))
					{
						foreach (var m in _viewModel.InactiveMods.Where(x => x.IsSelected))
						{
							m.DisplayFileForName = mod.DisplayFileForName;
						}
					}
				}
				
			});
		}
	}
}
