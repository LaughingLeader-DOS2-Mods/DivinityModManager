using DynamicData;
using DynamicData.Binding;

using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;

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
		//private List<DivinityLoadOrderEntry> savedList;
		//public List<DivinityLoadOrderEntry> SavedOrder => savedList;

		public List<DivinityLoadOrderEntry> Order { get; set; } = new List<DivinityLoadOrderEntry>();

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

		public void CreateActiveOrderBind(IObservable<IChangeSet<DivinityModData>> changeSet)
		{
			/*
			ActiveModBinding = changeSet.AutoRefresh(m => m.Index).Transform(m => new DivinityLoadOrderEntry { Name = m.Name, UUID = m.UUID }).Buffer(TimeSpan.FromMilliseconds(250)).
					FlattenBufferResult().Bind(Order).
					Subscribe(c =>
					{
						//newOrder.Order = c.ToList();

						Trace.WriteLine($"Load order {Name} changed.");
						Trace.WriteLine("=========================");
						Trace.WriteLine($"{String.Join(Environment.NewLine + "	", Order.Select(e => e.Name))}");
						Trace.WriteLine("=========================");
					});
			*/
		}

		public void DisposeBinding()
		{
			if(ActiveModBinding != null)
			{
				//savedList = new List<DivinityLoadOrderEntry>(Order);
				ActiveModBinding.Dispose();
			}
		}
	}
}
