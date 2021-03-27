using Alphaleonis.Win32.Filesystem;

using DivinityModManager.Util;

using DynamicData;
using DynamicData.Binding;

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
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;

namespace DivinityModManager.Models
{
	[ScreenReaderHelper(Name = "DisplayName", HelpText = "HelpText")]
	public class DivinityGameMasterCampaign : DivinityBaseModData
	{
		public Resource MetaResource { get; set; }

		public List<DivinityModDependencyData> Dependencies = new List<DivinityModDependencyData>();

		public DivinityGameMasterCampaign() : base()
		{
			
		}
	}
}
