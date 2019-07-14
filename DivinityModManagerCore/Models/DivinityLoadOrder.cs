using DynamicData;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace DivinityModManager.Models
{
	public class DivinityLoadOrder : ReactiveObject
	{
		public SourceCache<DivinityModData, string> Order { get; set; } = new SourceCache<DivinityModData, string>(t => t.UUID);

		private string name;

		public string Name
		{
			get => name;
			set { this.RaiseAndSetIfChanged(ref name, value); }
		}
	}
}
