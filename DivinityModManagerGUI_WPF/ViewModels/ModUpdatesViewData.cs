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

namespace DivinityModManager.ViewModels
{
	public class ModUpdatesViewData : ReactiveObject
	{
		private bool newAvailable;

		public bool NewAvailable
		{
			get => newAvailable;
			set { this.RaiseAndSetIfChanged(ref newAvailable, value); }
		}

		private bool updatesAvailable;

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

		public ICommand CopySelectedModsCommand { get; set; }
		public ICommand SelectAllNewModsCommand { get; set; }
		public ICommand SelectAllUpdatesCommand { get; set; }

		public void Clear()
		{
			Updates.Clear();
			NewMods.Clear();

			TotalUpdates = 0;
			NewAvailable = UpdatesAvailable = false;
		}

		public void CopySelectedMods()
		{
			string documentsFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			string modPakFolder = Path.Combine(documentsFolder, @"Larian Studios\Divinity Original Sin 2 Definitive Edition\Mods");
			int totalMoved = 0;
			
			if (Directory.Exists(modPakFolder))
			{
				List<string> filesToMove = new List<string>(NewMods.Where(x => x.IsSelected).Select(x => x.FilePath));
				if (filesToMove.Count > 0)
				{
					Trace.WriteLine($"Copying '{filesToMove.Count}' new workshop mod(s) to the local mods folder.");

					foreach (string file in filesToMove)
					{
						File.Copy(file, Path.Combine(modPakFolder, Path.GetFileName(file)), true);
						totalMoved++;
					}

					filesToMove.Clear();
				}
				
				filesToMove.AddRange(Updates.Where(x => x.IsSelected).Select(x => x.WorkshopMod.FilePath));
				if(filesToMove.Count > 0)
				{
					string backupFolder = Path.Combine(documentsFolder, @"Larian Studios\Divinity Original Sin 2 Definitive Edition\Mods_Old_ModManager");
					Directory.CreateDirectory(backupFolder);
					Trace.WriteLine($"Copying '{filesToMove.Count}' workshop mod update(s) to the local mods folder.");
					foreach (string file in filesToMove)
					{
						string baseName = Path.GetFileName(file);
						string existingMod = Path.Combine(modPakFolder, baseName);
						if(File.Exists(existingMod))
						{
							Trace.WriteLine($"Moved old pak to backup folder: '{existingMod}'.");
							string nextPath = DivinityFileUtils.GetUniqueFilename(Path.Combine(backupFolder, Path.GetFileName(existingMod)));
							File.Move(existingMod, nextPath);
							totalMoved++;
						}
						Trace.WriteLine($"Moving workshop mod into mods folder: '{file}'.");
						File.Move(file, Path.Combine(modPakFolder, Path.GetFileName(file)));
					}
				}

				Trace.WriteLine("Update complete.");
				JustUpdated = totalMoved > 0;
			}
		}

		public void SelectAll(bool select)
		{
			foreach(var x in NewMods)
			{
				x.IsSelected = select;
			}
			foreach(var x in Updates)
			{
				x.IsSelected = select;
			}
		}

		public ModUpdatesViewData()
		{
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
			});
		}
	}
}
