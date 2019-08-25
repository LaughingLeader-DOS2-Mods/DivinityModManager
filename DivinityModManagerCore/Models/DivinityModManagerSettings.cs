using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Newtonsoft.Json;

namespace DivinityModManager.Models
{
	[JsonObject(MemberSerialization.OptIn)]
	public class DivinityModManagerSettings : ReactiveObject
	{
		private string gameDataPath = "";

		[JsonProperty]
		public string GameDataPath
		{
			get => gameDataPath;
			set { this.RaiseAndSetIfChanged(ref gameDataPath, value); }
		}

		private string loadOrderPath = @"\Data\ModOrder";

		[JsonProperty]
		public string LoadOrderPath
		{
			get => loadOrderPath;
			set { this.RaiseAndSetIfChanged(ref loadOrderPath, value); }
		}
	}
}
