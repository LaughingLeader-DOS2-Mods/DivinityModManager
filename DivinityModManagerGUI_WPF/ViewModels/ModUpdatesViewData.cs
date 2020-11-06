using Alphaleonis.Win32.Filesystem;
using DivinityModManager.Models;
using DivinityModManager.Util;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Ookii.Dialogs.Wpf;
using DivinityModManager.Views;

namespace DivinityModManager.ViewModels
{
	public struct CopyModUpdatesTask
	{
		public List<string> NewFilesToMove;
		public List<string> UpdatesToMove;
		public string DocumentsFolder;
		public string ModPakFolder;
		public int TotalMoved;
	}

	public class ModUpdatesViewData : ReactiveObject
	{
		private bool unlocked = true;

		public bool Unlocked
		{
			get => unlocked;
			set { this.RaiseAndSetIfChanged(ref unlocked, value); }
		}

		private bool newAvailable;

		public bool NewAvailable
		{
			get => newAvailable;
			set { this.RaiseAndSetIfChanged(ref newAvailable, value); }
		}

		private bool updatesAvailable = false;

		public bool UpdatesAvailable
		{
			get => updatesAvailable;
			set { this.RaiseAndSetIfChanged(ref updatesAvailable, value); }
		}

		public ObservableCollectionExtended<DivinityModData> NewMods { get; set; } = new ObservableCollectionExtended<DivinityModData>();
		public ObservableCollectionExtended<DivinityModUpdateData> Updates { get; set; } = new ObservableCollectionExtended<DivinityModUpdateData>();

		private int totalUpdates;

		public int TotalUpdates
		{
			get => totalUpdates;
			set { this.RaiseAndSetIfChanged(ref totalUpdates, value); }
		}

		private bool anySelected = false;

		public bool AnySelected
		{
			get => anySelected;
			set { this.RaiseAndSetIfChanged(ref anySelected, value); }
		}

		private bool justUpdated = false;

		public bool JustUpdated
		{
			get => justUpdated;
			set { this.RaiseAndSetIfChanged(ref justUpdated, value); }
		}

		private bool allNewModsSelected = false;

		public bool AllNewModsSelected
		{
			get => allNewModsSelected;
			set { this.RaiseAndSetIfChanged(ref allNewModsSelected, value); }
		}

		private bool allModUpdatesSelected = false;

		public bool AllModUpdatesSelected
		{
			get => allModUpdatesSelected;
			set { this.RaiseAndSetIfChanged(ref allModUpdatesSelected, value); }
		}

		public ICommand CopySelectedModsCommand { get; set; }
		public ICommand SelectAllNewModsCommand { get; set; }
		public ICommand SelectAllUpdatesCommand { get; set; }

		public Action OnLoaded { get; set; }

		public Action<bool> CloseView { get; set; }

		private MainWindowViewModel _mainWindowViewModel;

		public void Clear()
		{
			Updates.Clear();
			NewMods.Clear();

			TotalUpdates = 0;
			NewAvailable = UpdatesAvailable = false;
			Unlocked = true;
		}

		public void SelectAll(bool select = true)
		{
			foreach (var x in NewMods)
			{
				x.IsSelected = select;
			}
			foreach (var x in Updates)
			{
				x.IsSelected = select;
			}
		}

		private void CopySelectedMods_Run()
		{
			string documentsFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			string modPakFolder = Path.Combine(documentsFolder, _mainWindowViewModel.AppSettings.DefaultPathways.DocumentsGameFolder, "Mods");

			if (Directory.Exists(modPakFolder))
			{
				Unlocked = false;
				using (ProgressDialog dialog = new ProgressDialog()
				{
					WindowTitle = "Updating Mods",
					Text = "Copying workshop mods...",
					CancellationText = "Update Canceled",
					MinimizeBox = false,
					ProgressBarStyle = ProgressBarStyle.ProgressBar
				})
				{
					dialog.DoWork += CopyFilesProgress_DoWork;
					dialog.RunWorkerCompleted += CopyFilesProgress_RunWorkerCompleted;

					var args = new CopyModUpdatesTask()
					{
						DocumentsFolder = documentsFolder,
						ModPakFolder = modPakFolder,
						NewFilesToMove = new List<string>(NewMods.Where(x => x.IsSelected).Select(x => x.FilePath)),
						UpdatesToMove = new List<string>(Updates.Where(x => x.IsSelected).Select(x => x.WorkshopMod.FilePath)),
						TotalMoved = 0
					};

					dialog.ShowDialog(MainWindow.Self, args);
				}
			}
			else
			{
				CloseView?.Invoke(false);
			}
		}

		public void CopySelectedMods()
		{
			if (Updates.Where(x => x.IsSelected).Count() > 0)
			{
				using (TaskDialog dialog = new TaskDialog()
				{
					Buttons =
					{
						new TaskDialogButton(ButtonType.Yes),
						new TaskDialogButton(ButtonType.No)
					},
					WindowTitle = "Update Mods?",
					Content = "Override local mods with the latest workshop versions?",
					/*
					Content = string.Format("Override local mods with the latest workshop versions?{0}{1}{0}{2}",
						Environment.NewLine,
						"Existing paks will be moved to the backup folder:",
						"(Larian Studios/Divinity Original Sin 2 Definitive Edition/Mods_Backup/)"),
					*/
					MainIcon = TaskDialogIcon.Warning
				})
				{
					var result = dialog.ShowDialog(MainWindow.Self);
					if (result.ButtonType == ButtonType.Yes)
					{
						CopySelectedMods_Run();
					}
					else
					{
						//CloseView?.Invoke(false);
					}
				}
			}
			else
			{
				CopySelectedMods_Run();
			}
		}

		private void CopyFilesProgress_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
		{
			Unlocked = true;
			DivinityApp.Log("Workshop mod copying complete.");
			try
			{
				if (e.Result is CopyModUpdatesTask args)
				{
					JustUpdated = args.TotalMoved > 0;
				}
			}
			catch(Exception ex)
			{
				string message = $"Error copying workshop mods: {ex.ToString()}";
				DivinityApp.Log(message);
				MainWindow.Self.AlertBar.SetDangerAlert(message);
			}
			CloseView?.Invoke(true);
		}

		private void CopyFilesProgress_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
		{
			ProgressDialog dialog = (ProgressDialog)sender;
			if(e.Argument is CopyModUpdatesTask args)
			{
				var totalWork = args.NewFilesToMove.Count + args.UpdatesToMove.Count;
				if (args.NewFilesToMove.Count > 0)
				{
					DivinityApp.Log($"Copying '{args.NewFilesToMove.Count}' new workshop mod(s) to the local mods folder.");

					foreach (string file in args.NewFilesToMove)
					{
						if (e.Cancel) return;
						var fileName = Path.GetFileName(file);
						dialog.ReportProgress(args.TotalMoved / totalWork, $"Copying '{fileName}'...", null);
						try
						{
							File.Copy(file, Path.Combine(args.ModPakFolder, fileName), true);
						}
						catch(Alphaleonis.Win32.Filesystem.FileReadOnlyException ex)
						{
							string message = $"Error copying '{fileName}' - File is read only!{Environment.NewLine}{ex.ToString()}";
							DivinityApp.Log(message);
							MainWindow.Self.AlertBar.SetDangerAlert(message);
							dialog.ReportProgress(args.TotalMoved / totalWork, message, null);
						}
						catch (Exception ex)
						{
							string message = $"Error copying '{fileName}':{Environment.NewLine}{ex.ToString()}";
							DivinityApp.Log(message);
							MainWindow.Self.AlertBar.SetDangerAlert(message);
							dialog.ReportProgress(args.TotalMoved / totalWork, message, null);
						}
						args.TotalMoved++;
					}
				}

				if (args.UpdatesToMove.Count > 0)
				{
					string backupFolder = Path.Combine(args.DocumentsFolder, _mainWindowViewModel.AppSettings.DefaultPathways.DocumentsGameFolder, "Mods_Old_ModManager");
					Directory.CreateDirectory(backupFolder);
					DivinityApp.Log($"Copying '{args.UpdatesToMove.Count}' workshop mod update(s) to the local mods folder.");
					foreach (string file in args.UpdatesToMove)
					{
						if (e.Cancel) return;
						string baseName = Path.GetFileName(file);
						try
						{
							DivinityApp.Log($"Moving workshop mod into mods folder: '{file}'.");
							File.Copy(file, Path.Combine(args.ModPakFolder, Path.GetFileName(file)), true);
						}
						catch(Exception ex)
						{
							DivinityApp.Log($"Error copying workshop mod:\n{ex.ToString()}");
						}
						dialog.ReportProgress(args.TotalMoved / totalWork, $"Copying '{baseName}'...", null);
						args.TotalMoved++;
					}
				}
			}
			
		}

		public ModUpdatesViewData(MainWindowViewModel mainWindowViewModel)
		{
			_mainWindowViewModel = mainWindowViewModel;

			NewMods.CollectionChanged += delegate
			{
				NewAvailable = NewMods.Count > 0;
			};

			Updates.CollectionChanged += delegate
			{
				UpdatesAvailable = Updates.Count > 0;
			};

			var anySelectedObservable = this.WhenAnyValue(x => x.AnySelected);

			CopySelectedModsCommand = ReactiveCommand.Create(CopySelectedMods, anySelectedObservable);
			SelectAllNewModsCommand = ReactiveCommand.Create<bool>((b) =>
			{
				foreach (var x in NewMods)
				{
					x.IsSelected = b;
				}
			});
			SelectAllUpdatesCommand = ReactiveCommand.Create<bool>((b) =>
			{
				foreach(var x in Updates)
				{
					x.IsSelected = b;
				}
			});

			//this.WhenAnyValue(x => x.NewMods.Count).Subscribe((count) =>
			//{
			//	NewAvailable = count > 0;
			//});

			//this.WhenAnyValue(x => x.Updates.Count).Subscribe((count) =>
			//{
			//	UpdatesAvailable = count > 0;
			//});

			this.WhenAnyValue(x => x.NewMods.Count, x => x.Updates.Count, (a, b) => a + b).BindTo(this, x => x.TotalUpdates);
			NewMods.ToObservableChangeSet().AutoRefresh(x => x.IsSelected).ToCollection().Subscribe((c) =>
			{
				bool nextAnySelected = c.Any(x => x.IsSelected);
				if(!nextAnySelected)
				{
					AnySelected = Updates.Any(x => x.IsSelected);
				}
				else
				{
					AnySelected = true;
				}
				AllNewModsSelected = NewMods.Count > 0 && c.All(x => x.IsSelected);
			});
			Updates.ToObservableChangeSet().AutoRefresh(x => x.IsSelected).ToCollection().Subscribe((c) =>
			{
				bool nextAnySelected = c.Any(x => x.IsSelected);
				if (!nextAnySelected)
				{
					AnySelected = NewMods.Any(x => x.IsSelected);
				}
				else
				{
					AnySelected = true;
				}
				AllModUpdatesSelected = Updates.Count > 0 && c.All(x => x.IsSelected);
			});
		}
	}
}
