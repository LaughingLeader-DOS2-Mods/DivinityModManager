using DivinityModManager.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Util
{
	public static class GithubHelper
	{
		private static readonly string GIT_URL_REPO_LATEST = "https://api.github.com/repos/{0}/releases/latest";

		public static async Task<string> GetLatestReleaseDataAsync(string repo)
		{
			var response = await WebHelper.Client.GetAsync(String.Format(GIT_URL_REPO_LATEST, repo), System.Net.Http.HttpCompletionOption.ResponseContentRead);
			return await response.Content.ReadAsStringAsync();
		}

		private static string GetBrowserDownloadUrl(string dataString)
		{
			var jsonData = DivinityJsonUtils.SafeDeserialize<Dictionary<string, object>>(dataString);
			if (jsonData != null)
			{
				if (jsonData.TryGetValue("assets", out var assetsArray))
				{
					JArray assets = (JArray)assetsArray;
					foreach (var obj in assets.Children<JObject>())
					{
						if (obj.TryGetValue("browser_download_url", StringComparison.OrdinalIgnoreCase, out var browserUrl))
						{
							return browserUrl.ToString();
						}
					}
				}
#if DEBUG
				var lines = jsonData.Select(kvp => kvp.Key + ": " + kvp.Value.ToString());
				DivinityApp.Log($"Can't find 'browser_download_url' in:\n{String.Join(Environment.NewLine, lines)}");
#endif
			}
			return "";
		}

		public static async Task<string> GetLatestReleaseLinkAsync(string repo)
		{
			var response = await WebHelper.Client.GetAsync(String.Format(GIT_URL_REPO_LATEST, repo), System.Net.Http.HttpCompletionOption.ResponseContentRead);
			return GetBrowserDownloadUrl(await response.Content.ReadAsStringAsync());
		}
	}
}
