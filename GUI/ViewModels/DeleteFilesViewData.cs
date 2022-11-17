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
	public class DeleteFilesViewData : ReactiveObject
	{
		public ObservableCollectionExtended<ModFileDeletionData> Files { get; set; } = new ObservableCollectionExtended<ModFileDeletionData>();

		private readonly ObservableAsPropertyHelper<bool> _anySelected;
		public bool AnySelected => _anySelected.Value;

		private readonly ObservableAsPropertyHelper<bool> _allSelected;
		public bool AllSelected => _allSelected.Value;
		[Reactive] public bool PermanentlyDelete { get; set; }

		private readonly ObservableAsPropertyHelper<bool> _isRunning;
		public bool IsRunning => _isRunning.Value;

		private readonly ObservableAsPropertyHelper<string> _selectAllTooltip;
		public string SelectAllTooltip => _selectAllTooltip.Value;

		public ReactiveCommand<Unit, Unit> SelectAllCommand { get; private set; }
		public ReactiveCommand<Unit, bool> RunCommand { get; private set; }
		public ReactiveCommand<Unit, Unit> StopRunningCommand { get; private set; }
		public ReactiveCommand<Unit, Unit> CancelCommand { get; private set; }

		[Reactive] public bool IsActive{ get; set; }
		[Reactive] public string ProgressTitle { get; set; }
		[Reactive] public string ProgressWorkText { get; set; }
		[Reactive] public double ProgressValue { get; set; }

		public async Task<bool> Run(CancellationToken cts)
		{
			var targetFiles = Files.Where(x => x.IsSelected).ToList();

			ProgressTitle = $"Deleting {targetFiles} mod file(s)...";
			ProgressValue = 0;
			var progressInc = 1 / targetFiles.Count;

			var result = await DivinityInteractions.ConfirmModDeletion.Handle(new DeleteFilesViewConfirmationData { Total = targetFiles.Count, PermanentlyDelete = PermanentlyDelete, Token = cts });
			if (result)
			{
				foreach (var f in targetFiles)
				{
					try
					{
						if (cts.IsCancellationRequested)
						{
							DivinityApp.Log("Deletion stopped.");
							return false;
						}
						if (File.Exists(f.FilePath))
						{
							ProgressWorkText = $"Deleting {f.FilePath}...";
							//RecycleBinHelper.DeleteFile(f.FilePath, false, PermanentlyDelete);
							DivinityApp.Log($"Deleted mod file '${f.FilePath}'");
						}
					}
					catch (Exception ex)
					{
						DivinityApp.Log($"Error deleting file '${f.FilePath}':\n{ex}");
					}
					ProgressValue += progressInc;
				}
				ProgressValue = 1d;
				Close();
			}
			return true;
		}

		public void Close()
		{
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
