using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	public class DivinityModUpdateData : ReactiveObject, ISelectable
	{
		public DivinityModData LocalMod { get; set; }
		public DivinityModData WorkshopMod { get; set; }

		private bool isSelected = false;

		public bool IsSelected
		{
			get => isSelected;
			set { this.RaiseAndSetIfChanged(ref isSelected, value); }
		}

		public bool IsEditorMod { get; set; } = false;
	}
}
