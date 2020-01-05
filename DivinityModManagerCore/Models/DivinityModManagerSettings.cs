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
using DivinityModManager.Util;
using System.Reactive.Disposables;

namespace DivinityModManager.Models
{
	[DataContract]
	public class DivinityModManagerSettings : ReactiveObject, IDisposable
	{
		private string gameDataPath = "";

		[DataMember]
		public string GameDataPath
		{
			get => gameDataPath;
			set 
			{
				if (value != gameDataPath) CanSaveSettings = true;
				this.RaiseAndSetIfChanged(ref gameDataPath, value);
			}
		}

		private string gameExecutable = "";

		[DataMember]
		public string DOS2DEGameExecutable
		{
			get => gameExecutable;
			set
			{
				if (value != gameExecutable) CanSaveSettings = true;
				this.RaiseAndSetIfChanged(ref gameExecutable, value);
			}
		}

		private bool gameStoryLogEnabled = false;

		[DataMember]
		public bool GameStoryLogEnabled
		{
			get => gameStoryLogEnabled;
			set
			{
				if (value != gameStoryLogEnabled) CanSaveSettings = true;
				this.RaiseAndSetIfChanged(ref gameStoryLogEnabled, value);
			}
		}

		private string dos2workshopPath = "";

		[DataMember]
		public string DOS2WorkshopPath
		{
			get => dos2workshopPath;
			set 
			{
				if (value != dos2workshopPath) CanSaveSettings = true;
				this.RaiseAndSetIfChanged(ref dos2workshopPath, value);
			}
		}

		private string loadOrderPath = "";

		[DataMember]
		public string LoadOrderPath
		{
			get => loadOrderPath;
			set 
			{
				if (value != loadOrderPath) CanSaveSettings = true;
				this.RaiseAndSetIfChanged(ref loadOrderPath, value); 
			}
		}

		private bool logEnabled = false;

		[DataMember]
		public bool LogEnabled
		{
			get => logEnabled;
			set
			{
				if (value != logEnabled) CanSaveSettings = true;
				this.RaiseAndSetIfChanged(ref logEnabled, value);
			}
		}

		private bool autoAddDependenciesWhenExporting = true;

		[DataMember]
		public bool AutoAddDependenciesWhenExporting
		{
			get => autoAddDependenciesWhenExporting;
			set
			{
				if (value != autoAddDependenciesWhenExporting) CanSaveSettings = true;
				this.RaiseAndSetIfChanged(ref autoAddDependenciesWhenExporting, value);
			}
		}

		private bool checkForUpdates = true;

		[DataMember]
		public bool CheckForUpdates
		{
			get => checkForUpdates;
			set { this.RaiseAndSetIfChanged(ref checkForUpdates, value); }
		}

		private long lastUpdateCheck = -1;

		[DataMember]
		public long LastUpdateCheck
		{
			get => lastUpdateCheck;
			set { this.RaiseAndSetIfChanged(ref lastUpdateCheck, value); }
		}

		private string lastOrder = "";

		[DataMember]
		public string LastOrder
		{
			get => lastOrder;
			set { this.RaiseAndSetIfChanged(ref lastOrder, value); }
		}

		private string lastLoadedOrderFilePath = "";

		[DataMember]
		public string LastLoadedOrderFilePath
		{
			get => lastLoadedOrderFilePath;
			set { this.RaiseAndSetIfChanged(ref lastLoadedOrderFilePath, value); }
		}

		private bool darkThemeEnabled = false;

		[DataMember]
		public bool DarkThemeEnabled
		{
			get => darkThemeEnabled;
			set { this.RaiseAndSetIfChanged(ref darkThemeEnabled, value); }
		}

		private OsiExtenderSettings extenderSettings;

		[DataMember]
		public OsiExtenderSettings ExtenderSettings
		{
			get => extenderSettings;
			set { this.RaiseAndSetIfChanged(ref extenderSettings, value); }
		}

		public ICommand SaveSettingsCommand { get; set; }
		public ICommand OpenSettingsFolderCommand { get; set; }

		public CompositeDisposable Disposables { get; internal set; }

		private bool canSaveSettings = false;

		public bool CanSaveSettings
		{
			get => canSaveSettings;
			set { this.RaiseAndSetIfChanged(ref canSaveSettings, value); }
		}

		public DivinityModManagerSettings()
		{
			Disposables = new CompositeDisposable();
			ExtenderSettings = new OsiExtenderSettings();
		}

		public void Dispose()
		{
			Disposables?.Dispose();
			Disposables = null;
		}
	}
}
