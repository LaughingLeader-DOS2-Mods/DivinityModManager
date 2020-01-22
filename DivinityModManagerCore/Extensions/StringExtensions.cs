using Alphaleonis.Win32.Filesystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Extensions
{
	public static class StringExtensions
	{
		public static bool IsExistingDirectory(this string path)
		{
			return !String.IsNullOrWhiteSpace(path) && Directory.Exists(path);
		}

		public static bool IsExistingFile(this string path)
		{
			return !String.IsNullOrWhiteSpace(path) && File.Exists(path);
		}
	}
}
