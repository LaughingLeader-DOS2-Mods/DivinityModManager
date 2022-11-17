using ReactiveUI.Fody.Helpers;
using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	public class ModFileDeletionData : ReactiveObject
	{
		[Reactive] public bool IsSelected { get; set; }
		[Reactive] public string FilePath { get; set; }
		[Reactive] public string DisplayName { get; set; }
	}
}
