using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	[JsonObject(MemberSerialization.OptIn)]
	public class DivinityModOsiExtenderConfig
	{
		[JsonProperty("RequiredExtensionVersion")]
		public int RequiredExtensionVersion { get; set; } = -1;

		[JsonProperty("FeatureFlags")]
		public List<string> FeatureFlags { get; set; }
	}
}
