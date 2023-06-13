using Alphaleonis.Win32.Filesystem;

using DivinityModManager.Models;
using DivinityModManager.Util;

using DynamicData;
using DynamicData.Binding;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DivinityModManager.ViewModels
{
	public class FileDeletionCompleteEventArgs : EventArgs
	{
		public int TotalFilesDeleted => DeletedFiles?.Count ?? 0;
		public List<ModFileDeletionData> DeletedFiles { get; set; }
		public bool RemoveFromLoadOrder { get; set; }

		public FileDeletionCompleteEventArgs()
		{
			DeletedFiles = new List<ModFileDeletionData>();
		}
	}

	public class DeleteFilesViewData : ReactiveObject
	{
		public ObservableCollectionExtended<ModFileDeletionData> Files { get; set; } = new ObservableCollectionExtended<ModFileDeletionData>();

		private readonly ObservableAsPropertyHelper<bool> _anySelected;
		public bool AnySelected => _anySelected.Value;

		private readonly ObservableAsPropertyHelper<bool> _allSelected;
		public bool AllSelected => _allSelected.Value;

		private readonly ObservableAsPropertyHelper<bool> _isRunning;
		public bool IsRunning => _isRunning.Value;

		private readonly ObservableAsPropertyHelper<string> _selectAllTooltip;
		public string SelectAllTooltip => _selectAllTooltip.Value;

		public ReactiveCommand<Unit, Unit> SelectAllCommand { get; private set; }
		public ReactiveCommand<Unit, bool> RunCommand { get; private set; }
		public ReactiveCommand<Unit, Unit> StopRunningCommand { get; private set; }
		public ReactiveCommand<Unit, Unit> CancelCommand { get; private set; }

		[Reactive] public bool PermanentlyDelete { get; set; }
		[Reactive] public bool RemoveFromLoadOrder { get; set; }

		[Reactive] public bool IsActive{ get; set; }
		[Reactive] public bool IsProgressActive { get; set; }
		[Reactive] public string ProgressTitle { get; set; }
		[Reactive] public string ProgressWorkText { get; set; }
		[Reactive] public double ProgressValue { get; set; }

		public event EventHandler<FileDeletionCompleteEventArgs> FileDeletionComplete;

		private async Task<Unit> UpdateProgress(string title = "", string workText = "", double value = -1)
		{
			await Observable.Start(() =>
			{
				if(!String.IsNullOrEmpty(title))
				{
					ProgressTitle = title;
				}
				if(!String.IsNullOrEmpty(workText))
				{
					ProgressWorkText = workText;
				}
				if(value > -1)
				{
					ProgressValue = value;
				}
			}, RxApp.MainThreadScheduler);
			return Unit.Default;
		}

		public async Task<bool> Run(CancellationToken cts)
		{
			var targetFiles = Files.Where(x => x.IsSelected).ToList();

			await UpdateProgress($"Confirming deletion...", "", 0d);

			var result = await DivinityInteractions.ConfirmModDeletion.Handle(new DeleteFilesViewConfirmationData { Total = targetFiles.Count, PermanentlyDelete = PermanentlyDelete, Token = cts });
			if (result)
			{
				var eventArgs = new FileDeletionCompleteEventArgs();
				eventArgs.RemoveFromLoadOrder = RemoveFromLoadOrder;

				await Observable.Start(() => IsProgressActive = true, RxApp.MainThreadScheduler);
				await UpdateProgress($"Deleting {targetFiles.Count} mod file(s)...", "", 0d);
				double progressInc = 1d / targetFiles.Count;
				foreach (var f in targetFiles)
				{
					try
					{
						if (cts.IsCancellationRequested)
						{
							DivinityApp.Log("Deletion stopped.");
							break;
						}
						if (File.Exists(f.FilePath))
						{
							await UpdateProgress("", $"Deleting {f.FilePath}...");
#if DEBUG
							eventArgs.DeletedFiles.Add(f);
#else
							if (RecycleBinHelper.DeleteFile(f.FilePath, false, PermanentlyDelete))
							{
								eventArgs.DeletedFiles.Add(f);
								DivinityApp.Log($"Deleted mod file '{f.FilePath}'");
							}
#endif
						}
					}
					catch (Exception ex)
					{
						DivinityApp.Log($"Error deleting file '${f.FilePath}':\n{ex}");
					}
					await UpdateProgress("", "", ProgressValue + progressInc);
				}
				await UpdateProgress("", "", 1d);
				await Task.Delay(500);
				RxApp.MainThreadScheduler.Schedule(() =>
				{
					Close();
					FileDeletionComplete?.Invoke(this, eventArgs);
				});
			}
			return true;
		}

		public void Close()
		{
			IsProgressActive = false;
			IsActive = false;
			Files.Clear();
		}

		public void ToggleSelectAll()
		{
			var b = !AllSelected;
			foreach (var f in Files)
			{
				f.IsSelected = b;
			}
		}

		public DeleteFilesViewData()
		{
			RemoveFromLoadOrder = true;
			IsActive = false;
			PermanentlyDelete = false;
			var filesChanged = this.Files.ToObservableChangeSet().AutoRefresh(x => x.IsSelected).ToCollection().Throttle(TimeSpan.FromMilliseconds(50)).ObserveOn(RxApp.MainThreadScheduler);
			var anySelected = filesChanged.Select(x => x.Any(y => y.IsSelected));
			_anySelected = anySelected.ToProperty(this, nameof(AnySelected));

			RunCommand = ReactiveCommand.CreateFromObservable(() => Observable.StartAsync(cts => Run(cts)).TakeUntil(this.StopRunningCommand), anySelected);
			StopRunningCommand = ReactiveCommand.Create(() => { }, this.RunCommand.IsExecuting);

			CancelCommand = ReactiveCommand.Create(Close, RunCommand.IsExecuting.Select(b => !b));

			_isRunning = this.RunCommand.IsExecuting.ToProperty(this, nameof(IsRunning), true, RxApp.MainThreadScheduler);

			_allSelected = filesChanged.Select(x => x.All(y => y.IsSelected)).ToProperty(this, nameof(AllSelected), true, RxApp.MainThreadScheduler);

			_selectAllTooltip = this.WhenAnyValue(x => x.AllSelected).Select(b => $"{(b ? "Deselect" : "Select")} All").ToProperty(this, nameof(SelectAllTooltip), true, RxApp.MainThreadScheduler);

			SelectAllCommand = ReactiveCommand.Create(ToggleSelectAll, this.RunCommand.IsExecuting.Select(b => !b), RxApp.MainThreadScheduler);
		}
	}
}
