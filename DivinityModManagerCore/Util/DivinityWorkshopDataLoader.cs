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
			for (int i = 0; i < mods.Count; i++)
			{
				var mod = mods[i];
				output += output + $"publishedfileids[{i}]: {mod.WorkshopData.ID}";
				if (i < mods.Count - 1) output += ", ";
			}
			return output;
		}

		private static List<string> ignoredTags = new List<string>{"Add-on", "Adventure", "GM", "Arena", "Story", "Definitive Edition"};
		private static List<string> GetWorkshopTags(IWorkshopPublishFileDetails data)
		{
			var tags = data.tags.Where(t => !ignoredTags.Contains(t.tag)).Select(x => x.tag).ToList();
			if (tags != null)
			{
				return tags;
			}
			return new List<string>();
		}

		public static async Task<int> LoadAllWorkshopDataAsync(List<DivinityModData> workshopMods, DivinityModManagerCachedWorkshopData cachedData)
		{
			if(workshopMods == null || workshopMods.Count == 0)
			{
				return 0;
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
				i++;
			}

			Trace.WriteLine($"Updating workshop data for mods.");

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

			int totalLoaded = 0;

			if (!String.IsNullOrEmpty(responseData))
			{
				PublishedFileDetailsResponse pResponse = DivinityJsonUtils.SafeDeserialize<PublishedFileDetailsResponse>(responseData);
				if(pResponse != null && pResponse.response != null && pResponse.response.publishedfiledetails != null && pResponse.response.publishedfiledetails.Count > 0)
				{
					var details = pResponse.response.publishedfiledetails;
					foreach (var d in details)
					{
						try
						{
							var mod = workshopMods.FirstOrDefault(x => x.WorkshopData.ID == d.publishedfileid);
							if (mod != null)
							{
								mod.WorkshopData.CreatedDate = DateUtils.UnixTimeStampToDateTime(d.time_created);
								mod.WorkshopData.UpdatedDate = DateUtils.UnixTimeStampToDateTime(d.time_updated);
								if (d.tags != null && d.tags.Count > 0)
								{
									mod.WorkshopData.Tags = GetWorkshopTags(d);
									mod.AddTags(mod.WorkshopData.Tags);
								}
								cachedData.AddOrUpdate(mod.UUID, d, mod.WorkshopData.Tags);
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
			return totalLoaded;
		}

		public static async Task<bool> GetAllWorkshopDataAsync(DivinityModManagerCachedWorkshopData cachedData, string appid)
		{
			Trace.WriteLine($"Attempting to get workshop data for mods missing workshop folders.");
			int totalFound = 0;

			int total = 1482;
			int page = 0;
			int maxPage = (total / 99)+1;

			while (page < maxPage)
			{
				string url = $"https://api.steampowered.com/IPublishedFileService/QueryFiles/v1/?key={ApiKeys.STEAM_WEB_API}&appid={appid}&return_short_description=true&numperpage=99&return_tags=true&return_metadata=true&requiredtags[0]=Definitive+Edition&excludedtags[0]=GM+Campaign&page={page}";
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

				if (!String.IsNullOrEmpty(responseData))
				{
					QueryFilesResponse pResponse = null;
					try
					{
						pResponse = JsonConvert.DeserializeObject<QueryFilesResponse>(responseData);
					}
					catch (Exception ex)
					{
						Trace.WriteLine(ex.ToString());
					}

					if (pResponse != null && pResponse.response != null && pResponse.response.publishedfiledetails != null && pResponse.response.publishedfiledetails.Count > 0)
					{
						if (pResponse.response.total > total)
						{
							total = pResponse.response.total;
							maxPage = (total / 99)+1;
						}
						var details = pResponse.response.publishedfiledetails;

						foreach (var d in details)
						{
							try
							{
								d.DeserializeMetadata();
								string uuid = d.GetGuid();
								if (!String.IsNullOrEmpty(uuid))
								{
									cachedData.AddOrUpdate(uuid, d, GetWorkshopTags(d));
									totalFound++;
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
						Trace.WriteLine($"Failed to get workshop data.");
					}
				}
				else
				{
					Trace.WriteLine("Failed to load workshop data for mods - no response data.");
				}

				page++;
			}

			if(totalFound > 0)
			{
				Trace.WriteLine($"Cached workshop data for {totalFound} mods.");
				return true;
			}
			else
			{
				return false;
			}
		}

		public static async Task<int> FindWorkshopDataAsync(List<DivinityModData> mods, DivinityModManagerCachedWorkshopData cachedData, string appid)
		{
			if (mods == null || mods.Count == 0)
			{
				Trace.WriteLine($"Skipping FindWorkshopDataAsync");
				return 0;
			}
			Trace.WriteLine($"Attempting to get workshop data for mods missing workshop folders.");
			int totalFound = 0;
			foreach (var mod in mods)
			{
				string name = Uri.EscapeUriString(mod.DisplayName);
				string url = $"https://api.steampowered.com/IPublishedFileService/QueryFiles/v1/?key={ApiKeys.STEAM_WEB_API}&appid={appid}&search_text={name}&return_short_description=true&return_tags=true&numperpage=99&return_metadata=true&requiredtags[0]=Definitive+Edition";
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
									mod.WorkshopData.CreatedDate = DateUtils.UnixTimeStampToDateTime(d.time_created);
									mod.WorkshopData.UpdatedDate = DateUtils.UnixTimeStampToDateTime(d.time_updated);
									if (d.tags != null && d.tags.Count > 0)
									{
										mod.WorkshopData.Tags = GetWorkshopTags(d);
										mod.AddTags(mod.WorkshopData.Tags);
									}
									cachedData.AddOrUpdate(mod.UUID, d, mod.WorkshopData.Tags);
									Trace.WriteLine($"Found workshop ID {mod.WorkshopData.ID} for mod {mod.DisplayName}.");
									totalFound++;
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
						if(!cachedData.NonWorkshopMods.Contains(mod.UUID))
						{
							cachedData.NonWorkshopMods.Add(mod.UUID);
							cachedData.CacheUpdated = true;
						}
					}
				}
				else
				{
					Trace.WriteLine("Failed to load workshop data for mods - no response data.");
				}
			}

			if (totalFound > 0)
			{
				Trace.WriteLine($"Successfully loaded workshop data for {totalFound} mods.");
			}
			else
			{
				Trace.WriteLine($"Failed to find workshop data for {mods.Count} mods (they're probably not on the workshop).");
			}

			return totalFound;
		}
	}
}
