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
		public static string GetLatestReleaseData(string repo)
		{
			string url = $"https://api.github.com/repos/{repo}/releases/latest";
			return WebHelper.Get(url, new WebRequestHeaderValue
			{
				HttpRequestHeader = HttpRequestHeader.UserAgent,
				Value = "DivinityModManagerUser"
			});
		}

		public static async Task<string> GetLatestReleaseDataAsync(string repo)
		{
			string url = $"https://api.github.com/repos/{repo}/releases/latest";
			return await WebHelper.GetAsync(url, new WebRequestHeaderValue
			{
				HttpRequestHeader = HttpRequestHeader.UserAgent,
				Value = "DivinityModManagerUser"
			});
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
				Trace.WriteLine($"Can't find 'browser_download_url' in:\n{String.Join(Environment.NewLine, lines)}");
#endif
			}
			return "";
		}
		public static string GetLatestReleaseLink(string repo)
		{
			string url = $"https://api.github.com/repos/{repo}/releases/latest";
			var dataString = WebHelper.Get(url, new WebRequestHeaderValue
			{
				HttpRequestHeader = HttpRequestHeader.UserAgent,
				Value = "DivinityModManagerUser"
			});
			return GetBrowserDownloadUrl(dataString);
		}

		public static async Task<string> GetLatestReleaseLinkAsync(string repo)
		{
			string url = $"https://api.github.com/repos/{repo}/releases/latest";
			var dataString = await WebHelper.GetAsync(url, new WebRequestHeaderValue
			{
				HttpRequestHeader = HttpRequestHeader.UserAgent,
				Value = "DivinityModManagerUser"
			});
			return GetBrowserDownloadUrl(dataString);
		}
	}
}
