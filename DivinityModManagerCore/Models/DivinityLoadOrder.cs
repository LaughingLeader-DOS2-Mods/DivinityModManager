using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Windows.Input;
using Alphaleonis.Win32.Filesystem;

namespace DivinityModManager.Models
{
	[DataContract]
	public class DivinityLoadOrderEntry
	{
		[DataMember]
		public string UUID { get; set; }

		[DataMember]
		public string Name { get; set; }
		public bool Missing { get; set; } = false;

		public DivinityLoadOrderEntry Clone()
		{
			return new DivinityLoadOrderEntry() { Name = this.Name, UUID = this.UUID, Missing = this.Missing };
		}
	}

	[DataContract]
	public class DivinityLoadOrder : ReactiveObject
	{
		private string name;
		public string Name
		{
			get => name;
			set 
			{
				string lastName = name;
				this.RaiseAndSetIfChanged(ref name, value);
				if(!String.IsNullOrEmpty(lastName) && lastName != name)
				{
					DivinityApp.Events.OnOrderNameChanged(lastName, name);
				}
			}
		}


		private string filePath = "";
		public string FilePath
		{
			get => filePath;
			set { this.RaiseAndSetIfChanged(ref filePath, value); }
		}

		private DateTime lastModifiedDate;

		public DateTime LastModifiedDate
		{
			get => lastModifiedDate;
			set {
				this.RaiseAndSetIfChanged(ref lastModifiedDate, value);
				LastModified = lastModifiedDate.ToString("g");
			}
		}

		private string lastModified;

		public string LastModified
		{
			get => lastModified;
			set { this.RaiseAndSetIfChanged(ref lastModified, value); }
		}

		[DataMember]
		public List<DivinityLoadOrderEntry> Order { get; set; } = new List<DivinityLoadOrderEntry>();

		public void Add(DivinityModData mod)
		{
			try
			{
				if (Order != null && mod != null)
				{
					if (Order.Count > 0)
					{
						bool alreadyInOrder = false;
						foreach (var x in Order)
						{
							if (x != null && x.UUID == mod.UUID)
							{
								alreadyInOrder = true;
								break;
							}
						}
						if (!alreadyInOrder)
						{
							Order.Add(mod.ToOrderEntry());
						}
					}
					else
					{
						Order.Add(mod.ToOrderEntry());
					}
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error adding mod to order:\n{ex.ToString()}");
			}
		}

		public void AddRange(IEnumerable<DivinityModData> mods)
		{
			foreach (var mod in mods)
			{
				Add(mod);
			}
		}

		public void Remove(DivinityModData mod)
		{
			try
			{
				if (Order != null && Order.Count > 0 && mod != null)
				{
					DivinityLoadOrderEntry entry = null;
					foreach(var x in Order)
					{
						if(x != null && x.UUID == mod.UUID)
						{
							entry = x;
							break;
						}
					}
					if (entry != null) Order.Remove(entry);
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error removing mod from order:\n{ex.ToString()}");
			}
		}

		public void RemoveRange(IEnumerable<DivinityModData> mods)
		{
			if (Order.Count > 0 && mods != null)
			{
				foreach (var mod in mods)
				{
					Remove(mod);
				}
			}
		}

		public void Sort(Comparison<DivinityLoadOrderEntry> comparison)
		{
			try
			{
				if (Order.Count > 1)
				{
					Order.Sort(comparison);
				}
			}
			catch(Exception ex)
			{
				DivinityApp.Log($"Error sorting order:\n{ex.ToString()}");
			}
		}

		public void SetOrder(IEnumerable<DivinityLoadOrderEntry> nextOrder)
		{
			Order.Clear();
			Order.AddRange(nextOrder);
		}

		public void SetOrder(DivinityLoadOrder nextOrder)
		{
			Order.Clear();
			Order.AddRange(nextOrder.Order);
		}

		public bool OrderEquals(IEnumerable<string> orderList)
		{
			if(Order.Count > 0)
			{
				return Order.Select(x => x.UUID).SequenceEqual(orderList);
			}
			return false;
		}

		public DivinityLoadOrder Clone()
		{
			return new DivinityLoadOrder()
			{
				Name = this.name,
				Order = this.Order.ToList(),
				LastModifiedDate = this.LastModifiedDate
			};
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

						DivinityApp.LogMessage($"Load order {Name} changed.");
						DivinityApp.LogMessage("=========================");
						DivinityApp.LogMessage($"{String.Join(Environment.NewLine + "	", Order.Select(e => e.Name))}");
						DivinityApp.LogMessage("=========================");
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

		public DivinityLoadOrder()
		{
			
		}
	}
}
