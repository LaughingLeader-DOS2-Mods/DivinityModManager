using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Util
{
	public static class DateUtils
	{
		public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
		{
			// Unix timestamp is seconds past epoch
			System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
			return dtDateTime;
		}
	}
}
