using Alphaleonis.Win32.Filesystem;
using DivinityModManager.Util;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Aggregation;

using LSLib.LS;

using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Reflection;

namespace DivinityModManager.Models
{
	[DataContract]
	[ScreenReaderHelper(Name = "DisplayName", HelpText = "HelpText")]
	public class DivinityModData : DivinityBaseModData, ISelectable
	{
		[Reactive][DataMember] public int Index { get; set; }

		[DataMember(Name = "FileName")]
		public string OutputPakName
		{
			get
			{
				if (!Folder.Contains(UUID))
				{
					return Path.ChangeExtension($"{Folder}_{UUID}", "pak");
				}
				else
				{
					return Path.ChangeExtension($"{FileName}", "pak");
				}
			}
		}

		[Reactive][DataMember(Name = "Type")] public string ModType { get; set; }

		[DataMember] public List<string> Modes { get; set; } = new List<string>();

		[DataMember] public string Targets { get; set; }
		[Reactive] public DateTime? LastUpdated { get; set; }

		private DivinityExtenderModStatus extenderModStatus = DivinityExtenderModStatus.NONE;

		public static int CurrentExtenderVersion { get; set; } = -1;

		public DivinityExtenderModStatus ExtenderModStatus
		{
			get => extenderModStatus;
			set
			{
				this.RaiseAndSetIfChanged(ref extenderModStatus, value);
				UpdateScriptExtenderToolTip(CurrentExtenderVersion);
			}
		}

		public string ScriptExtenderSupportToolTipText { get; private set; }

		public void UpdateScriptExtenderToolTip(int currentVersion = -1)
		{
			switch (ExtenderModStatus)
			{
				case DivinityExtenderModStatus.REQUIRED:
				case DivinityExtenderModStatus.REQUIRED_MISSING:
				case DivinityExtenderModStatus.REQUIRED_DISABLED:
				case DivinityExtenderModStatus.REQUIRED_OLD:
					ScriptExtenderSupportToolTipText = "";
					if (ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_MISSING)
					{
						ScriptExtenderSupportToolTipText = "[MISSING] ";
					}
					else if (ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_DISABLED)
					{
						ScriptExtenderSupportToolTipText = "[EXTENSIONS DISABLED] ";
					}
					else if (ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_OLD)
					{
						ScriptExtenderSupportToolTipText = "[OLD] ";
					}
					if (ScriptExtenderData.RequiredExtensionVersion > -1)
					{
						ScriptExtenderSupportToolTipText += $"Requires Script Extender v{ScriptExtenderData.RequiredExtensionVersion} or higher";
					}
					else
					{
						ScriptExtenderSupportToolTipText += "Requires the Script Extender";
					}
					if (ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_DISABLED)
					{
						ScriptExtenderSupportToolTipText += " (Enable Extensions in the Script Extender config)";
					}
					if (ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_OLD)
					{
						ScriptExtenderSupportToolTipText += " (Update by running the game)";
					}
					break;
				case DivinityExtenderModStatus.SUPPORTS:
					if (ScriptExtenderData.RequiredExtensionVersion > -1)
					{
						ScriptExtenderSupportToolTipText = $"Supports Script Extender v{ScriptExtenderData.RequiredExtensionVersion} or higher";
					}
					else
					{
						ScriptExtenderSupportToolTipText = "Supports the Script Extender";
					}
					break;
				case DivinityExtenderModStatus.NONE:
				default:
					ScriptExtenderSupportToolTipText = "";
					break;
			}
			if (ScriptExtenderSupportToolTipText != "")
			{
				ScriptExtenderSupportToolTipText += Environment.NewLine;
			}
			if (currentVersion > -1)
			{
				ScriptExtenderSupportToolTipText += $"(Currently installed version is v{currentVersion})";
			}
			else
			{
				ScriptExtenderSupportToolTipText += "(No installed extender version found)";
			}
			this.RaisePropertyChanged("ScriptExtenderSupportToolTipText");
		}

		[DataMember] public DivinityModScriptExtenderConfig ScriptExtenderData { get; set; }
		[DataMember] public SourceList<DivinityModDependencyData> Dependencies { get; set; } = new SourceList<DivinityModDependencyData>();

		protected ReadOnlyObservableCollection<DivinityModDependencyData> displayedDependencies;
		public ReadOnlyObservableCollection<DivinityModDependencyData> DisplayedDependencies => displayedDependencies;

		public override string GetDisplayName()
		{
			if (DisplayFileForName)
			{
				if (!IsEditorMod)
				{
					return FileName;
				}
				else
				{
					return Folder + " [Editor Project]";
				}
			}
			else
			{
				return !IsClassicMod ? Name : Name + " [Classic]";
			}
		}

		private readonly ObservableAsPropertyHelper<bool> hasToolTip;

		public bool HasToolTip => hasToolTip.Value;

		private readonly ObservableAsPropertyHelper<int> dependencyCount;
		public int TotalDependencies => dependencyCount.Value;

		private readonly ObservableAsPropertyHelper<bool> hasDependencies;

		public bool HasDependencies => hasDependencies.Value;

		private readonly ObservableAsPropertyHelper<Visibility> dependencyVisibility;
		public Visibility DependencyVisibility => dependencyVisibility.Value;

		[Reactive] public bool HasScriptExtenderSettings { get; set; }

		[Reactive] public bool IsEditorMod { get; set; }
		[Reactive] public bool IsForcedLoaded { get; set; }

		[Reactive] public bool IsActive { get; set; }

		private bool isSelected = false;

		public bool IsSelected
		{
			get => isSelected;
			set
			{
				if (value && Visibility != Visibility.Visible)
				{
					value = false;
				}
				this.RaiseAndSetIfChanged(ref isSelected, value);
			}
		}

		private readonly ObservableAsPropertyHelper<bool> canDelete;
		public bool CanDelete => canDelete.Value;

		private readonly ObservableAsPropertyHelper<bool> canAddToLoadOrder;
		public bool CanAddToLoadOrder => canAddToLoadOrder.Value;


		[Reactive] public bool CanDrag { get; set; }

		[Reactive] public bool DeveloperMode { get; set; }

		[Reactive] public bool HasColorOverride { get; set; }
		[Reactive] public string SelectedColor { get; set; }
		[Reactive] public string ListColor { get; set; }

		private DivinityModWorkshopData workshopData = new DivinityModWorkshopData();

		public DivinityModWorkshopData WorkshopData
		{
			get => workshopData;
			set { this.RaiseAndSetIfChanged(ref workshopData, value); }
		}

		//public DivinityModWorkshopData WorkshopData { get; private set; } = new DivinityModWorkshopData();
		public ICommand OpenWorkshopPageCommand { get; private set; }
		public ICommand OpenWorkshopPageInSteamCommand { get; private set; }

		public string GetURL(bool asSteamBrowserProtocol = false)
		{
			if (WorkshopData != null && WorkshopData.ID != "")
			{
				if (!asSteamBrowserProtocol)
				{
					return $"https://steamcommunity.com/sharedfiles/filedetails/?id={WorkshopData.ID}";
				}
				else
				{
					return $"steam://url/CommunityFilePage/{WorkshopData.ID}";
				}
			}
			return "";
		}

		public void OpenSteamWorkshopPage()
		{
			var url = GetURL();
			if (!String.IsNullOrEmpty(url))
			{
				System.Diagnostics.Process.Start(url);

			}
		}

		public void OpenSteamWorkshopPageInSteam()
		{
			var url = GetURL(true);
			if (!String.IsNullOrEmpty(url))
			{
				System.Diagnostics.Process.Start(url);

			}
		}

		public override string ToString()
		{
			return $"Name({Name}) Version({Version?.Version}) Author({Author}) UUID({UUID})";
		}

		public DivinityLoadOrderEntry ToOrderEntry()
		{
			return new DivinityLoadOrderEntry
			{
				UUID = this.UUID,
				Name = this.Name
			};
		}

		public DivinityProfileActiveModData ToProfileModData()
		{
			return new DivinityProfileActiveModData()
			{
				Folder = Folder,
				MD5 = MD5,
				Name = Name,
				UUID = UUID,
				Version = Version.VersionInt
			};
		}

		public DivinityModData(bool isBaseGameMod = false) : base()
		{
			Index = -1;
			CanDrag = true;

			var connection = this.Dependencies.Connect().ObserveOn(RxApp.MainThreadScheduler);
			connection.Bind(out displayedDependencies).DisposeMany().Subscribe();
			dependencyCount = connection.Count().StartWith(0).ToProperty(this, nameof(TotalDependencies));
			hasDependencies = this.WhenAnyValue(x => x.TotalDependencies, c => c > 0).StartWith(false).ToProperty(this, nameof(HasDependencies));
			dependencyVisibility = this.WhenAnyValue(x => x.HasDependencies, b => b ? Visibility.Visible : Visibility.Collapsed).StartWith(Visibility.Collapsed).ToProperty(this, nameof(DependencyVisibility));
			this.WhenAnyValue(x => x.IsActive, x => x.IsClassicMod, x => x.IsForcedLoaded).Subscribe((b) =>
			{
				if (b.Item3)
				{
					CanDrag = false;
				}
				else
				{
					if (b.Item1)
					{
						//Allow removing a classic mod from the active list.
						CanDrag = true;
					}
					else
					{
						CanDrag = !b.Item2;
					}
				}
			});

			this.WhenAnyValue(x => x.IsClassicMod, x => x.IsForcedLoaded, x => x.IsEditorMod).Subscribe((b) =>
			{
				if (b.Item1)
				{
					this.SelectedColor = "#32BF0808";
					this.ListColor = "#32FA0202";
					HasColorOverride = true;
				}
				else if (b.Item2)
				{
					this.SelectedColor = "#32F38F00";
					this.ListColor = "#32C17200";
					HasColorOverride = true;
				}
				else if (b.Item3)
				{
					this.SelectedColor = "#6400ED48";
					this.ListColor = "#0C00FF4D";
					HasColorOverride = true;
				}
				else
				{
					HasColorOverride = false;
				}
			});

			this.WhenAnyValue(x => x.HeaderVersion).Select(x => x != null && x.Minor == 1).Subscribe((b) =>
			{
				if (b)
				{
					IsClassicMod = true;
				}
			});

			if (!isBaseGameMod)
			{
				var canOpenWorkshopLink = this.WhenAnyValue(x => x.WorkshopData.ID, (id) => !String.IsNullOrEmpty(id));
				OpenWorkshopPageCommand = ReactiveCommand.Create(OpenSteamWorkshopPage, canOpenWorkshopLink);
				OpenWorkshopPageInSteamCommand = ReactiveCommand.Create(OpenSteamWorkshopPageInSteam, canOpenWorkshopLink);
			}
			else
			{
				this.IsHidden = true;
				this.IsLarianMod = true;
			}

			// If a screen reader is active, don't bother making tooltips for the mod item entry
			hasToolTip = this.WhenAnyValue(x => x.Description, x => x.HasDependencies, x => x.UUID).
				Select(x => !DivinityApp.IsScreenReaderActive() && (
				!String.IsNullOrEmpty(x.Item1) || x.Item2 || !String.IsNullOrEmpty(x.Item3))).StartWith(true).ToProperty(this, nameof(HasToolTip));

			canDelete = this.WhenAnyValue(x => x.IsEditorMod, x => x.IsLarianMod, x => x.FilePath,
				(a, b, c) => !a && !b && File.Exists(c)).StartWith(false).ToProperty(this, nameof(CanDelete));
			canAddToLoadOrder = this.WhenAnyValue(x => x.ModType, x => x.IsLarianMod, x => x.IsForcedLoaded,
				(a, b, c) => a != "Adventure" && !b && !c).StartWith(true).ToProperty(this, nameof(CanAddToLoadOrder));
		}
	}
}
