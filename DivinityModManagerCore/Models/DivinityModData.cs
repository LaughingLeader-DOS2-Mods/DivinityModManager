using Alphaleonis.Win32.Filesystem;
using DivinityModManager.Util;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

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

		private string filePath;

		public string FilePath
		{
			get => filePath;
			set 
			{ 
				this.RaiseAndSetIfChanged(ref filePath, value); 
				if(!String.IsNullOrWhiteSpace(filePath))
				{
					FileName = Path.GetFileName(FilePath);
				}
			}
		}

		private string filename;

		public string FileName
		{
			get => filename;
			set { this.RaiseAndSetIfChanged(ref filename, value); }
		}

		public string UUID { get; set; }

		private string name;

		public string Name
		{
			get => name;
			set 
			{ 
				this.RaiseAndSetIfChanged(ref name, value); 
				if(!DisplayFileForName)
				{
					DisplayName = Name;
				}
			}
		}

		public string Description { get; set; }
		public string Author { get; set; }
		public DivinityModVersion Version { get; set; }
		public string Folder { get; set; }
		public string MD5 { get; set; }
		public string Type { get; set; }
		public List<string> Modes { get; set; } = new List<string>();
		public string Targets { get; set; }
		public DateTime LastModified { get; set; }

		public DivinityModOsiExtenderConfig OsiExtenderData { get; set; }
		public List<DivinityModDependencyData> Dependencies { get; set; } = new List<DivinityModDependencyData>();

		private string displayName;

		public string DisplayName
		{
			get => displayName;
			set { this.RaiseAndSetIfChanged(ref displayName, value); }
		}

		private bool displayFileForName = false;

		public bool DisplayFileForName
		{
			get => displayFileForName;
			set 
			{ 
				this.RaiseAndSetIfChanged(ref displayFileForName, value);
				if(displayFileForName)
				{
					if(!IsEditorMod)
					{
						DisplayName = Path.GetFileName(FilePath);
					}
					else
					{
						DisplayName = Folder + " [Editor Project]";
					}
				}
				else
				{
					DisplayName = Name;
				}
			}
		}

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

		private Visibility visibility = Visibility.Visible;

		public Visibility Visibility
		{
			get => visibility;
			set { this.RaiseAndSetIfChanged(ref visibility, value); }
		}

		public ICommand OpenInFileExplorerCommand { get; private set; }
		public ICommand ToggleNameDisplayCommand { get; private set; }

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

		public DivinityModData()
		{
			this.OpenInFileExplorerCommand = DivinityApp.GlobalCommands.OpenInFileExplorerCommand;
			this.ToggleNameDisplayCommand = DivinityApp.GlobalCommands.ToggleNameDisplayCommand;
		}
	}
}
