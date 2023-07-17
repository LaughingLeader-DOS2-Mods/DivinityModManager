using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	[JsonObject(MemberSerialization.OptIn)]
	public class DivinityModScriptExtenderConfig
	{
		[JsonProperty("RequiredExtensionVersion")]
		public int RequiredExtensionVersion { get; set; } = -1;

		[JsonProperty("FeatureFlags")]
		public List<string> FeatureFlags { get; set; } = new List<string>();

		public bool HasAnySettings
		{
			get
			{
				return RequiredExtensionVersion > -1 || FeatureFlags?.Count > 0;
			}
		}
	}
}
