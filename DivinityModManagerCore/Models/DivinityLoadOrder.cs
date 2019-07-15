using DynamicData;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace DivinityModManager.Models
{
	public struct DivinityLoadOrderEntry
	{
		public string UUID;
		public string Name;
	}

	public class DivinityLoadOrder : ReactiveObject
	{
		public List<DivinityLoadOrderEntry> Order { get; set; } = new List<DivinityLoadOrderEntry>();

		private string name;

		public string Name
		{
			get => name;
			set { this.RaiseAndSetIfChanged(ref name, value); }
		}
	}
}
