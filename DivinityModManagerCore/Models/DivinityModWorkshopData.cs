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
		private string id = "";

		public string ID
		{
			get => id;
			set { this.RaiseAndSetIfChanged(ref id, value); }
		}

		private string description;

		public string Description
		{
			get => description;
			set { this.RaiseAndSetIfChanged(ref description, value); }
		}

		public string PreviewUrl { get; set; }
		public string Title { get; set; }

		public List<string> Tags { get; set; }

		public DateTime CreatedDate { get; set; }
		public DateTime UpdatedDate { get; set; }

		public int Subscriptions { get; set; }
		public int Favorites { get; set; }
		public int LifetimeSubscriptions { get; set; }
		public int LifetimeFavorites { get; set; }
		public int Views { get; set; }
	}
}
