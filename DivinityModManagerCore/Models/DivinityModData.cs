using DivinityModManager.Util;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DivinityModManager.Models
{
	public interface IDivinityModData
	{
		string UUID { get; set; }
		string Name { get; set; }
		string Folder { get; set; }
		string MD5 { get; set; }
		DivinityModVersion Version { get; set; }
	}

	public class DivinityModData : ReactiveObject, IDivinityModData, ISelectable
	{
		private int index = -1;

		public int Index
		{
			get => index;
			set { this.RaiseAndSetIfChanged(ref index, value); }
		}
		public string FilePath { get; set; }

		public string UUID { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string Author { get; set; }
		public DivinityModVersion Version { get; set; }
		public string Folder { get; set; }

		public string MD5 { get; set; }

		public List<DivinityModDependencyData> Dependencies { get; set; } = new List<DivinityModDependencyData>();

		private string dependenciesText;

		public string DependenciesText
		{
			get => dependenciesText;
			private set { this.RaiseAndSetIfChanged(ref dependenciesText, value); }
		}

		private bool hasDescription = false;

		public bool HasDescription
		{
			get => hasDescription;
			set { this.RaiseAndSetIfChanged(ref hasDescription, value); }
		}

		private bool hasToolTip = false;

		public bool HasToolTip
		{
			get => hasToolTip;
			set { this.RaiseAndSetIfChanged(ref hasToolTip, value); }
		}

		private bool hasDependencies = false;

		public bool HasDependencies
		{
			get => hasDependencies;
			set { this.RaiseAndSetIfChanged(ref hasDependencies, value); }
		}

		private bool isEditorMod = false;

		public bool IsEditorMod
		{
			get => isEditorMod;
			set { this.RaiseAndSetIfChanged(ref isEditorMod, value); }
		}

		private bool isActive = false;

		public bool IsActive
		{
			get => isActive;
			set { this.RaiseAndSetIfChanged(ref isActive, value); }
		}

		private bool isSelected = false;

		public bool IsSelected
		{
			get => isSelected;
			set { this.RaiseAndSetIfChanged(ref isSelected, value); }
		}

		public void UpdateDependencyText()
		{
			HasDescription = !String.IsNullOrWhiteSpace(Description);
			string t = "";
			var listDependencies = Dependencies.Where(d => !DivinityModDataLoader.IgnoreMod(d.UUID)).ToList();
			if (listDependencies.Count > 0)
			{
				HasDependencies = true;
				//t += "Dependencies" + Environment.NewLine;
				for (var i = 0; i < listDependencies.Count; i++)
				{
					var mod = listDependencies[i];
					t += $"{mod.Name} {mod.Version.Version}";
					if (i < listDependencies.Count - 1) t += Environment.NewLine;
				}
			}
			else
			{
				HasDependencies = false;
			}

			DependenciesText = t;

			HasToolTip = HasDescription | HasDependencies;
		}
		public override string ToString()
		{
			return $"Mod|Name({Name}) Version({Version?.Version}) Author({Author}) UUID({UUID})";
		}

		public DivinityLoadOrderEntry ToOrderEntry()
		{
			return new DivinityLoadOrderEntry
			{
				UUID = this.UUID,
				Name = this.Name
			};
		}
	}
}
