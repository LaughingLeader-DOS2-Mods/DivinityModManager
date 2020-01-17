using LSLib.LS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Extensions
{
	static public class ResourceExtensions
	{

		public static Node FindNode(this Node node, string name)
		{
			if (node.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
			{
				return node;
			}
			else
			{
				return FindNode(node.Children, name);
			}
		}

		public static Node FindNode(this Dictionary<string, List<Node>> children, string name)
		{
			foreach (var kvp in children)
			{
				if (kvp.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
				{
					return kvp.Value.FirstOrDefault();
				}

				foreach (var node in kvp.Value)
				{
					var match = FindNode(node, name);
					if (match != null)
					{
						return match;
					}
				}
			}
			return null;
		}

		public static Node FindNode(this Region region, string name)
		{
			foreach (var kvp in region.Children)
			{
				if (kvp.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
				{
					return kvp.Value.First();
				}
			}

			var match = FindNode(region.Children, name);
			if (match != null)
			{
				return match;
			}

			return null;
		}

		public static Node FindNode(this Resource resource, string name)
		{
			foreach (var region in resource.Regions.Values)
			{
				var match = FindNode(region, name);
				if (match != null)
				{
					return match;
				}
			}

			return null;
		}
	}
}
