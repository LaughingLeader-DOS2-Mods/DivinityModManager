using Alphaleonis.Win32.Filesystem;
using DivinityModManager.Util;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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

		private DivinityModVersion headerVersion;
		public DivinityModVersion HeaderVersion
		{
			get => headerVersion;
			set
			{
				headerVersion = value;
				if(headerVersion != null)
				{
					IsClassicMod = headerVersion.Minor == 1;
					if (IsClassicMod)
					{
						System.Diagnostics.Trace.WriteLine($"Found a Classic mod: {Name}");
					}
				}
			}
		}

		public bool IsHidden { get; set; } = false;
		public bool IsLarianMod { get; set; } = false;

		private bool isClassicMod = false;

		public bool IsClassicMod
		{
			get => isClassicMod;
			set 
			{ 
				this.RaiseAndSetIfChanged(ref isClassicMod, value); 
				if(IsClassicMod)
				{
					CanDrag = !IsActive;
				}
			}
		}
		
		private bool isMissingOsirisExtender = false;

		public bool IsMissingOsirisExtender
		{
			get => isMissingOsirisExtender;
			set 
			{ 
				this.RaiseAndSetIfChanged(ref isMissingOsirisExtender, value);
				if(value)
				{
					if (OsiExtenderData != null && OsiExtenderData.RequiredExtensionVersion > 0)
					{
						MissingOsirisExtenderText = $"Missing Osiris Extender v{OsiExtenderData.RequiredExtensionVersion} or higher.";
					}
					else
					{
						MissingOsirisExtenderText = "Missing the Osiris Extender";
					}
					this.RaisePropertyChanged("MissingOsirisExtenderText");
				}
			}
		}

		public string MissingOsirisExtenderText { get; private set; }

		public DivinityModOsiExtenderConfig OsiExtenderData { get; set; }
		public List<DivinityModDependencyData> Dependencies { get; set; } = new List<DivinityModDependencyData>();

		private string displayName;

		public string DisplayName
		{
			get => displayName;
			set { this.RaiseAndSetIfChanged(ref displayName, value); }
		}

		public void UpdateDisplayName()
		{
			if (DisplayFileForName)
			{
				if (!IsEditorMod)
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
				DisplayName = !IsClassicMod ? Name : Name + " [Classic]";
			}
		}

		private bool displayFileForName = false;

		public bool DisplayFileForName
		{
			get => displayFileForName;
			set 
			{ 
				this.RaiseAndSetIfChanged(ref displayFileForName, value);
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

		private bool hasOsirisExtenderSettings = false;

		public bool HasOsirisExtenderSettings
		{
			get => hasOsirisExtenderSettings;
			set { this.RaiseAndSetIfChanged(ref hasOsirisExtenderSettings, value); }
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
			set 
			{ 
				this.RaiseAndSetIfChanged(ref isActive, value);
				if(IsClassicMod)
				{
					CanDrag = isActive;
				}
			}
		}

		private bool isSelected = false;

		public bool IsSelected
		{
			get => isSelected;
			set { this.RaiseAndSetIfChanged(ref isSelected, value); }
		}

		private bool canDrag = true;

		public bool CanDrag
		{
			get => canDrag;
			private set { this.RaiseAndSetIfChanged(ref canDrag, value); }
		}

		private Visibility visibility = Visibility.Visible;

		public Visibility Visibility
		{
			get => visibility;
			set { this.RaiseAndSetIfChanged(ref visibility, value); }
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
