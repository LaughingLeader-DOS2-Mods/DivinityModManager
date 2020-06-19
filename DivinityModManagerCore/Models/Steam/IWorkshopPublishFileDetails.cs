using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models.Steam
{
	public interface IWorkshopPublishFileDetails
	{
		string publishedfileid { get; set; }
		long time_created { get; set; }
		long time_updated { get; set; }

		List<WorkshopTag> tags { get; set; }
	}
}
