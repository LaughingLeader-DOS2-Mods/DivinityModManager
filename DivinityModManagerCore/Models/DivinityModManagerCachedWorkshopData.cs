using DivinityModManager.Models.Steam;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	[DataContract]
	public class DivinityModManagerCachedWorkshopData
	{
		[DataMember] public long LastUpdated { get; set; } = -1;
		[DataMember] public string LastVersion { get; set; } = "";

		[DataMember] public List<DivinityModWorkshopCachedData> Mods { get; set; } = new List<DivinityModWorkshopCachedData>();
		[DataMember] public List<string> NonWorkshopMods { get; set; } = new List<string>();

		public bool CacheUpdated { get; set; } = false;

		public void AddOrUpdate(string uuid, IWorkshopPublishFileDetails d, List<string> tags)
		{
			// Mods may have the same UUID, so use the WorkshopID instead.
			var cachedData = Mods.FirstOrDefault(x => x.WorkshopID == d.publishedfileid);
			if(cachedData != null)
			{
				cachedData.LastUpdated = d.time_updated;
				cachedData.Created = d.time_created;
				cachedData.Tags = tags;
			}
			else
			{
				Mods.Add(new DivinityModWorkshopCachedData()
				{
					Created = d.time_created,
					LastUpdated = d.time_updated,
					UUID = uuid,
					WorkshopID = d.publishedfileid,
					Tags = tags
				});
			}
			NonWorkshopMods.Remove(uuid);
			CacheUpdated = true;
		}

		public void AddNonWorkshopMod(string uuid)
		{
			if(!NonWorkshopMods.Any(x => x == uuid))
			{
				NonWorkshopMods.Add(uuid);
			}
			CacheUpdated = true;
		}

		public string Serialize()
		{
			StringBuilder sb = new StringBuilder();
			StringWriter sw = new StringWriter(sb);

			using (JsonWriter writer = new JsonTextWriter(sw))
			{
				try
				{
					writer.WriteStartObject();

					writer.WritePropertyName("LastUpdated");
					writer.WriteValue(LastUpdated);

					writer.WritePropertyName("LastVersion");
					writer.WriteValue(LastVersion);

					writer.WritePropertyName("Mods");
					writer.WriteStartArray();

					foreach (var data in Mods)
					{
						writer.WriteStartObject();

						writer.WritePropertyName("UUID");
						writer.WriteValue(data.UUID);

						writer.WritePropertyName("WorkshopID");
						writer.WriteValue(data.WorkshopID);

						//writer.WritePropertyName("Created");
						//writer.WriteValue(data.Created);

						writer.WritePropertyName("LastUpdated");
						writer.WriteValue(data.LastUpdated);

						writer.WritePropertyName("Tags");
						writer.WriteStartArray();

						if(data.Tags != null && data.Tags.Count > 0)
						{
							foreach (var tag in data.Tags)
							{
								writer.WriteValue(tag);
							}
						}

						writer.WriteEndArray();

						writer.WriteEndObject();
					}

					writer.WriteEndArray();

					writer.WritePropertyName("NonWorkshopMods");
					writer.WriteStartArray();
					foreach(var uuid in NonWorkshopMods)
					{
						writer.WriteValue(uuid);
					}
					writer.WriteEndArray();

					writer.WriteEndObject();
				}
				catch(Exception ex)
				{
					DivinityApp.Log("Error serializing CachedWorkshopData:");
					DivinityApp.Log(ex.ToString());
				}
			}

			return sb.ToString();
		}
	}
}
