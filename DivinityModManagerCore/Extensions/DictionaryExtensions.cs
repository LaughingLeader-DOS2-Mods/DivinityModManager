using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Extensions
{
	public static class DictionaryExtensions
	{
		public static object FindKeyValue(this Dictionary<string,object> dict, string key, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
		{
			foreach(KeyValuePair<string,object> kvp in dict)
			{
				if(kvp.Key.Equals(key, stringComparison))
				{
					return kvp.Value;
				}
				else if(kvp.Value.GetType() == typeof(Dictionary<string, object>))
				{
					var subDict = (Dictionary<string, object>)kvp.Value;
					var val = subDict.FindKeyValue(key, stringComparison);
					if (val != null) return val;
				}
			}
			return null;
		}

		private static object FindKeyValue_Recursive(object obj, string key, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
		{
			if (obj.GetType() == typeof(Dictionary<string, object>))
			{
				var subDict = (Dictionary<string, object>)obj;
				var val = subDict.FindKeyValue(key, stringComparison);
				if (val != null) return val;
			}
			else if (obj is IList list)
			{
				foreach (var childobj in list)
				{
					var val = FindKeyValue_Recursive(childobj, key, stringComparison);
					if (val != null) return val;
				}
			}
			else if (obj is IEnumerable enumerable)
			{
				foreach (var childobj in enumerable)
				{
					var val = FindKeyValue_Recursive(childobj, key, stringComparison);
					if (val != null) return val;
				}
			}
			return null;
		}

		public static bool TryFindKeyValue(this Dictionary<string, object> dict, string key, out object valObj, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
		{
			foreach (KeyValuePair<string, object> kvp in dict)
			{
				if (kvp.Key.Equals(key, stringComparison))
				{
					valObj = kvp.Value;
					return true;
				}
				else
				{
					var val = FindKeyValue_Recursive(kvp.Value, key, stringComparison);
					if(val != null)
					{
						valObj = val;
						return true;
					}
				}
			}
			valObj = null;
			return false;
		}
	}
}
