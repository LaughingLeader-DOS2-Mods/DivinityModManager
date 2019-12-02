using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager
{
	public static class DivinityApp
	{
		public const string DIR_DATA = "Data\\";
		public const string URL_REPO = @"https://github.com/LaughingLeader-DOS2-Mods/DivinityModManager";
		public const string URL_CHANGELOG = @"https://github.com/LaughingLeader-DOS2-Mods/DivinityModManager/blob/master/CHANGELOG.md";
		public const string URL_CHANGELOG_RAW = @"https://raw.githubusercontent.com/LaughingLeader-DOS2-Mods/DivinityModManager/master/CHANGELOG.md";
#if DEBUG
		public const string URL_UPDATE = @"G:\DivinityModManager\Update.xml";
#else
		public const string URL_UPDATE = @"https://raw.githubusercontent.com/LaughingLeader-DOS2-Mods/DivinityModManager/master/Update.xml";
#endif
		public const string URL_AUTHOR = @"https://github.com/LaughingLeader";
		public const string URL_ISSUES = @"https://github.com/LaughingLeader-DOS2-Mods/DivinityModManager/issues";
		public const string URL_LICENSE = @"https://github.com/LaughingLeader-DOS2-Mods/DivinityModManager/blob/master/LICENSE";
		public const string URL_DONATION = @"https://ko-fi.com/laughingleader";

	}
}
