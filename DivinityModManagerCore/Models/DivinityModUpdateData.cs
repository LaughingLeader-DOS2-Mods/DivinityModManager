using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	public struct DivinityModUpdateData
	{
		public DivinityModData LocalMod { get; set; }
		public DivinityModData WorkshopMod { get; set; }
	}
}
