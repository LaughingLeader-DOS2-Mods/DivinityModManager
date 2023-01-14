using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DivinityModManager.Models
{
	public class DivinityModUpdateData : ReactiveObject, ISelectable
	{
		private DivinityModData localMod;
		public DivinityModData LocalMod
		{
			get => localMod;
			set
			{
				this.RaiseAndSetIfChanged(ref localMod, value);
			}
		}

		private DivinityModData workshopMod;
		public DivinityModData WorkshopMod
		{
			get => workshopMod;
			set
			{
				this.RaiseAndSetIfChanged(ref workshopMod, value);
			}
		}

		private bool isSelected = false;

		public bool IsSelected
		{
			get => isSelected;
			set { this.RaiseAndSetIfChanged(ref isSelected, value); }
		}

		public bool IsEditorMod { get; set; }
		public bool CanDrag { get; set; } = true;
		public Visibility Visibility { get; set; } = Visibility.Visible;

		public DivinityModUpdateData()
		{
			
		}
	}
}
