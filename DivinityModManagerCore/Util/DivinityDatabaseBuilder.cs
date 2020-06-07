using DivinityModManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DivinityModManager.Util
{
	public class DivinityDatabaseBuilder
	{
		public void StartBuild(List<DivinityModData> mods, Dispatcher dispatcher)
		{
			var modsToParse = mods.ToList();

			dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(async () =>
			{
				await BuildDatabaseAsync(modsToParse);
			}));
		}

		private async Task<bool> BuildDatabaseAsync(List<DivinityModData> mods)
		{
			return true;
		}
	}
}
