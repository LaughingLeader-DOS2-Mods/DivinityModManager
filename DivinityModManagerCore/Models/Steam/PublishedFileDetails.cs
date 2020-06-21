using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models.Steam
{
	public struct WorkshopTag
	{
		public string tag { get; set; }
	}

	public class PublishedFileDetailsResponse
	{
		public PublishedFileDetailsResponseData response { get; set; }
	}
	
	public class PublishedFileDetailsResponseData
	{
		public int result { get; set; }
		public int resultcount { get; set; }

		public List<PublishedFileDetails> publishedfiledetails { get; set;}
	}


	public class PublishedFileDetails : IWorkshopPublishFileDetails
	{
		public string publishedfileid { get; set; }
		public int result { get; set; }
		public string creator { get; set; }
		public int creator_app_id { get; set; }
		public int consumer_app_id { get; set; }
		public string filename { get; set; }
		public string file_size { get; set; }
		public string file_url { get; set; }
		public string hcontent_file { get; set; }
		public string preview_url { get; set; }
		public string hcontent_preview { get; set; }
		public string title { get; set; }
		public string description { get; set; }
		public long time_created { get; set; }
		public long time_updated { get; set; }
		public int visibility { get; set; }
		public bool banned { get; set; }
		public string ban_reason { get; set; }
		public int subscriptions { get; set; }
		public int favorited { get; set; }
		public int lifetime_subscriptions { get; set; }
		public int lifetime_favorited { get; set; }
		public int views { get; set; }
		public List<WorkshopTag> tags { get; set; }
	}
}
