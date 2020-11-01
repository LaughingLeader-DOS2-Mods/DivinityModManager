using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models.App
{
	public class AppSettings
	{
		public DefaultPathwayData DefaultPathways { get; set; } = new DefaultPathwayData();

		public Dictionary<string, bool> Features { get; set; } = new Dictionary<string, bool>();

		public bool FeatureEnabled(string id)
		{
			if (Features.TryGetValue(id.ToLower(), out bool v))
			{
				return v == true;
			}
			return false;
		}
	}
}
