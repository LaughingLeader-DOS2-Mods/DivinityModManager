using Alphaleonis.Win32.Filesystem;
using LSLib.LS;
using LSLib.LS.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Util
{
	public static class DivinitySaveTools
	{
		public static bool RenameSave(string pathToSave, string newName)
		{
			try
			{
				string baseName = Path.GetFileNameWithoutExtension(pathToSave);
				string output = Path.ChangeExtension(Path.Combine(Path.GetDirectoryName(pathToSave), newName), ".lsv");
				using (var reader = new PackageReader(pathToSave))
				{
					Package package = reader.Read();
					AbstractFileInfo saveScreenshotImage = package.Files.FirstOrDefault(p => p.Name.EndsWith(".png"));
					if (saveScreenshotImage != null)
					{
						saveScreenshotImage.Name = saveScreenshotImage.Name.Replace(baseName, newName);

						using (var writer = new PackageWriter(package, output))
						{
							writer.Version = Package.CurrentVersion;
							writer.Compression = LSLib.LS.Enums.CompressionMethod.None;
							writer.CompressionLevel = CompressionLevel.FastCompression;
							writer.Write();
						}

						return true;
					}
				}
			}
			catch(Exception ex)
			{
				Trace.WriteLine($"Failed to rename save: {ex.ToString()}");
			}

			return false;
		}
	}
}
