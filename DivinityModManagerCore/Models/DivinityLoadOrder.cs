using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DivinityModManager.Models
{
	public struct DivinityLoadOrderEntry
	{
		public string UUID;
		public string Name;

		public DivinityLoadOrderEntry Clone()
		{
			return new DivinityLoadOrderEntry() { Name = this.Name, UUID = this.UUID };
		}
	}

	public class DivinityLoadOrder : ReactiveObject, IActivatable
	{
		public ObservableCollectionExtended<DivinityLoadOrderEntry> Order { get; set; } = new ObservableCollectionExtended<DivinityLoadOrderEntry>();

		private string name;

		public string Name
		{
			get => name;
			set { this.RaiseAndSetIfChanged(ref name, value); }
		}

		public void SetOrder(IEnumerable<DivinityLoadOrderEntry> nextOrder)
		{
			Order.Clear();
			Order.AddRange(nextOrder);
		}

		public IDisposable ActiveModBinding { get; set; }
	}
}
