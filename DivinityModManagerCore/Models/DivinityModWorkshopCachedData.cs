using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DivinityModManager.Models
{
	public class DivinityModWorkshopCachedData
	{
		public string UUID { get; set; }
		public string WorkshopID { get; set; }
		public long Created { get; set; }
		public long LastUpdated { get; set; }
		public List<string> Tags { get; set; } = new List<string>();
    }
}
