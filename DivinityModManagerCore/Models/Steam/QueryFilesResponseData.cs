using DivinityModManager.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models.Steam
{
	public class QueryFilesResponse
	{
		public QueryFilesResponseData response { get; set; }
	}

	public class QueryFilesResponseData
	{
		public int total { get; set; }

		public List<QueryFilesPublishedFileDetails> publishedfiledetails { get; set; }
	}

	public class QueryFilesPublishedFileDetails : IWorkshopPublishFileDetails
	{
		public int result { get; set; }
		public string publishedfileid { get; set; }
		public string creator { get; set; }
		public string filename { get; set; }
		public string file_size { get; set; }
		public string file_url { get; set; }
		public string preview_url { get; set; }
		public string url { get; set; }
		public string title { get; set; }
		public string description { get; set; }
		public long time_created { get; set; }
		public long time_updated { get; set; }
		public int visibility { get; set; }
		public int flags { get; set; }
		public List<WorkshopTag> tags { get; set; }
		public string metadata { get; set; }
		[JsonIgnore] public QueryFilesPublishedFileDivinityMetadataMain MetaData { get; set; }
		public int language { get; set; }
		public string revision_change_number { get; set; }
		public int revision { get; set; }

		public string GetGuid()
		{
			if (this.MetaData != null)
			{
				try
				{
					return this.MetaData.root.regions.MetaData.Guid.Value;
				}
				catch
				{
				}
			}
			return null;
		}

		public void DeserializeMetadata()
		{
			if (!String.IsNullOrEmpty(metadata))
			{
				MetaData = DivinityJsonUtils.SafeDeserialize<QueryFilesPublishedFileDivinityMetadataMain>(metadata);
			}
		}
	}

	public class QueryFilesPublishedFileDivinityMetadataMain
	{
		public QueryFilesPublishedFileDivinityMetadataRoot root { get; set; }
	}

	public class QueryFilesPublishedFileDivinityMetadataRoot
	{
		public QueryFilesPublishedFileDivinityMetadataHeader header { get; set; }
		public QueryFilesPublishedFileDivinityMetadataRegions regions { get; set; }
	}

	public class QueryFilesPublishedFileDivinityMetadataHeader
	{
		public int time { get; set; }
		public string version { get; set; }
	}

	public class QueryFilesPublishedFileDivinityMetadataRegions
	{
		public QueryFilesPublishedFileDivinityMetadataEntry MetaData { get; set; }
	}

	public class QueryFilesPublishedFileDivinityMetadataEntry
	{
		public QueryFilesPublishedFileDivinityMetadataEntryAttribute<string> Guid { get; set; }
		public QueryFilesPublishedFileDivinityMetadataEntryAttribute<int> Type { get; set; }
		public QueryFilesPublishedFileDivinityMetadataEntryAttribute<int> Version { get; set; }
	}
	public class QueryFilesPublishedFileDivinityMetadataEntryAttribute<T>
	{
		public int type { get; set; }
		public object value { get; set; }

		[JsonIgnore]
		public T Value
		{
			get
			{
				return (T)value;
			}
		}
	}
}
