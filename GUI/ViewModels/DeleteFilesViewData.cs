using DivinityModManager.Models;

using DynamicData;
using DynamicData.Binding;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.ViewModels
{
	public class ModFileDeletionData : ReactiveObject
	{
		[Reactive] public bool IsSelected { get; set; }
		[Reactive] public string FilePath { get; set; }
		[Reactive] public string DisplayName { get; set; }
	}

	public class DeleteFilesViewData : ReactiveObject
	{
		public ObservableCollectionExtended<ModFileDeletionData> Files { get; set; } = new ObservableCollectionExtended<ModFileDeletionData>();

		[Reactive] public bool AnySelected { get; set; }
		[Reactive] public bool PermanentlyDelete { get; set; }

		public ReactiveCommand<Unit, Unit> RunCommand { get; private set; }
		public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }

		public void Run()
		{

		}

		public void Cancel()
		{

		}

		public DeleteFilesViewData()
		{
			PermanentlyDelete = false;
			var anySelected = this.Files.ToObservableChangeSet().AutoRefresh(x => x.IsSelected).ToCollection().Select(x => x.All(y => y.IsSelected));
			anySelected.ToProperty(this, nameof(AnySelected));
			RunCommand = ReactiveCommand.Create(Run, anySelected);
		}
	}
}
