using DivinityModManager.Models;
using DivinityModManager.Models.Steam;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using DivinityModManager.Enums.Steam;
using System.Web;

namespace DivinityModManager.Util
{
	public static class DivinityWorkshopDataLoader
	{
		private static readonly string STEAM_API_GET_WORKSHOP_DATA_URL = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/?";
		private static readonly string STEAM_API_GET_WORKSHOP_MODS_URL = "https://api.steampowered.com/IPublishedFileService/QueryFiles/v1/?";

		private static string CreatePublishFileIds(List<DivinityModData> mods)
		{
			string output = "";
			for(int i = 0; i < mods.Count; i++)
			{
				var mod = mods[i];
				output += output + $"publishedfileids[{i}]: {mod.WorkshopData.ID}";
				if (i < mods.Count - 1) output += ", ";
			}
			return output;
		}

		public static async Task<Unit> LoadAllWorkshopDataAsync(List<DivinityModData> workshopMods)
		{
			if(workshopMods.Count == 0)
			{
				return Unit.Default;
			}
			//var workshopMods = mods.Where(x => !String.IsNullOrEmpty(x.WorkshopData.ID)).ToList();
			var values = new Dictionary<string, string>
			{
			{ "itemcount", workshopMods.Count.ToString()}
			};
			int i = 0;
			foreach (var mod in workshopMods)
			{
				values.Add($"publishedfileids[{i}]", mod.WorkshopData.ID);
				i = i + 1;
			}

			Trace.WriteLine($"Attempting to get workshop data from mods.");

			string responseData = "";
			try
			{
				var content = new FormUrlEncodedContent(values);
				var response = await WebHelper.Client.PostAsync(STEAM_API_GET_WORKSHOP_DATA_URL, content);
				responseData = await response.Content.ReadAsStringAsync();
			}
			catch(Exception ex)
			{
				Trace.WriteLine($"Error requesting Steam API to get workshop mod data:\n{ex.ToString()}");
			}

			if (!String.IsNullOrEmpty(responseData))
			{
				PublishedFileDetailsResponse pResponse = DivinityJsonUtils.SafeDeserialize<PublishedFileDetailsResponse>(responseData);
				if(pResponse != null && pResponse.response != null && pResponse.response.publishedfiledetails != null && pResponse.response.publishedfiledetails.Count > 0)
				{
					int totalLoaded = 0;
					var details = pResponse.response.publishedfiledetails;
					foreach (var d in details)
					{
						try
						{
							var mod = workshopMods.FirstOrDefault(x => x.WorkshopData.ID == d.publishedfileid);
							if (mod != null)
							{
								if (d.tags != null && d.tags.Count > 0)
								{
									mod.WorkshopData.Tags = d.tags.Select(x => x.tag).ToList();
									//Trace.WriteLine($"Tags: {String.Join(";", mod.WorkshopData.Tags)}");
								}
								mod.WorkshopData.PreviewUrl = d.preview_url;
								mod.WorkshopData.Title = d.title;
								mod.WorkshopData.Description = d.description;
								mod.WorkshopData.CreatedDate = DateUtils.UnixTimeStampToDateTime(d.time_created);
								mod.WorkshopData.UpdatedDate = DateUtils.UnixTimeStampToDateTime(d.time_updated);
								mod.WorkshopData.Subscriptions = d.subscriptions;
								mod.WorkshopData.LifetimeSubscriptions = d.lifetime_subscriptions;
								mod.WorkshopData.Favorites = d.favorited;
								mod.WorkshopData.LifetimeFavorites = d.lifetime_favorited;
								mod.WorkshopData.Views = d.views;
								//Trace.WriteLine($"Loaded workshop details for mod {mod.Name}:");
								totalLoaded++;
							}
						}
						catch(Exception ex)
						{
							Trace.WriteLine($"Error parsing mod data for {d.title}({d.publishedfileid})\n{ex.ToString()}");
						}
					}

					Trace.WriteLine($"Successfully loaded workshop data for {totalLoaded} mods.");
				}
				else
				{
					Trace.WriteLine("Failed to load workshop data for mods.");
					Trace.WriteLine($"{responseData}");
				}
			}
			else
			{
				Trace.WriteLine("Failed to load workshop data for mods - no response data.");
			}
			return Unit.Default;
		}

		public static async Task<Unit> FindWorkshopDataAsync(List<DivinityModData> mods)
		{
			if (mods.Count == 0)
			{
				Trace.WriteLine($"Skipping FindWorkshopDataAsync");
				return Unit.Default;
			}
			Trace.WriteLine($"Attempting to get workshop data for mods missing workshop folders.");
			int totalLoaded = 0;
			foreach (var mod in mods)
			{
				string name = Uri.EscapeUriString(mod.DisplayName);
				string url = $"https://api.steampowered.com/IPublishedFileService/QueryFiles/v1/?key={ApiKeys.STEAM_WEB_API}&appid=435150&search_text={name}&return_tags=true&return_details=true&return_metadata=true&requiredtags[0]=Definitive+Edition";
				string responseData = "";
				try
				{
					var response = await WebHelper.Client.GetAsync(url);
					responseData = await response.Content.ReadAsStringAsync();
				}
				catch (Exception ex)
				{
					Trace.WriteLine($"Error requesting Steam API to get workshop mod data:\n{ex.ToString()}");
				}

				//Trace.WriteLine(responseData);
				if (!String.IsNullOrEmpty(responseData))
				{
					QueryFilesResponse pResponse = null;
					//QueryFilesResponse pResponse = DivinityJsonUtils.SafeDeserialize<QueryFilesResponse>(responseData);
					try
					{
						pResponse = JsonConvert.DeserializeObject<QueryFilesResponse>(responseData);
					}
					catch(Exception ex)
					{
						Trace.WriteLine(ex.ToString());
					}
					
					if (pResponse != null && pResponse.response != null && pResponse.response.publishedfiledetails != null && pResponse.response.publishedfiledetails.Count > 0)
					{
						var details = pResponse.response.publishedfiledetails;
						
						foreach (var d in details)
						{
							try
							{
								d.DeserializeMetadata();
								if (d.GetGuid() == mod.UUID)
								{
									mod.WorkshopData.ID = d.publishedfileid;
									mod.WorkshopData.PreviewUrl = d.preview_url;
									mod.WorkshopData.Title = d.title;
									mod.WorkshopData.Description = d.description;
									mod.WorkshopData.CreatedDate = DateUtils.UnixTimeStampToDateTime(d.time_created);
									mod.WorkshopData.UpdatedDate = DateUtils.UnixTimeStampToDateTime(d.time_updated);
									Trace.WriteLine($"Found workshop ID {mod.WorkshopData.ID} for mod {mod.DisplayName}.");
									break;
								}
							}
							catch (Exception ex)
							{
								Trace.WriteLine($"Error parsing mod data for {d.title}({d.publishedfileid})\n{ex.ToString()}");
							}
						}
					}
					else
					{
						Trace.WriteLine($"Failed to find workshop data for mod {mod.DisplayName}");
					}
				}
				else
				{
					Trace.WriteLine("Failed to load workshop data for mods - no response data.");
				}
			}

			Trace.WriteLine($"Successfully loaded workshop data for {totalLoaded} mods.");

			return Unit.Default;
		}
	}
}
