using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	public class DivinityModWorkshopData : ReactiveObject
	{
		private long id = -1;

		public long ID
		{
			get => id;
			set { this.RaiseAndSetIfChanged(ref id, value); }
		}

		public List<string> Tags { get; set; }
	}
}
