using Alphaleonis.Win32.Filesystem;
using LSLib.LS;
using LSLib.LS.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DivinityModManager.Extensions;

namespace DivinityModManager.Util
{
	public static class DivinitySaveTools
	{
		public static bool RenameSave(string pathToSave, string newName)
		{
			try
			{
				string baseOldName = Path.GetFileNameWithoutExtension(pathToSave);
				string baseNewName = Path.GetFileNameWithoutExtension(newName);
				string output = Path.ChangeExtension(Path.Combine(Path.GetDirectoryName(pathToSave), newName), ".lsv");
				using (var reader = new PackageReader(pathToSave))
				{
					Package package = reader.Read();
					AbstractFileInfo saveScreenshotImage = package.Files.FirstOrDefault(p => p.Name.EndsWith(".png"));
					if (saveScreenshotImage != null)
					{
						saveScreenshotImage.Name = saveScreenshotImage.Name.Replace(Path.GetFileNameWithoutExtension(saveScreenshotImage.Name), baseNewName);

						Trace.WriteLine($"Renamed internal screenshot '{saveScreenshotImage.Name}' in '{output}'.");
					}

					// Edit the saved date in the meta.lsf to avoid "corruption" messages
					AbstractFileInfo abstractFileInfo = package.Files.FirstOrDefault(p => p.Name == "meta.lsf");
					if (abstractFileInfo != null)
					{
						Resource resource;
						try
						{
							System.IO.Stream rsrcStream = abstractFileInfo.MakeStream();
							using (var rsrcReader = new LSFReader(rsrcStream))
							{
								resource = rsrcReader.Read();

								if (resource != null)
								{
									var saveTimeNode = resource.FindNode("SaveTime");

									if (saveTimeNode != null)
									{
										NodeAttribute yearAtt = null;
										NodeAttribute monthAtt = null;
										NodeAttribute dayAtt = null;
										NodeAttribute hoursAtt = null;
										NodeAttribute minutesAtt = null;
										NodeAttribute secondsAtt = null;
										NodeAttribute millisecondsAtt = null;

										saveTimeNode.Attributes.TryGetValue("Year", out yearAtt);
										saveTimeNode.Attributes.TryGetValue("Month", out monthAtt);
										saveTimeNode.Attributes.TryGetValue("Day", out dayAtt);
										saveTimeNode.Attributes.TryGetValue("Hours", out hoursAtt);
										saveTimeNode.Attributes.TryGetValue("Minutes", out minutesAtt);
										saveTimeNode.Attributes.TryGetValue("Seconds", out secondsAtt);
										saveTimeNode.Attributes.TryGetValue("Milliseconds", out millisecondsAtt);

										var time = DateTime.Now;

										yearAtt.Value = time.Year;
										monthAtt.Value = time.Month;
										dayAtt.Value = time.Day;
										hoursAtt.Value = time.Hour;
										minutesAtt.Value = time.Minute;
										secondsAtt.Value = time.Second;
										millisecondsAtt.Value = time.Millisecond;

										Trace.WriteLine($"Updated SaveTime in save's meta.lsf.");

										var rscrWriter = new LSFWriter(rsrcStream, FileVersion.CurrentVersion);
										rscrWriter.Write(resource);
									}
									else
									{
										Trace.WriteLine($"Couldn't find SaveTime node '{String.Join(";", resource.Regions.Values.First().Children.Keys)}'.");
									}
								}
							}
						}
						finally
						{
							abstractFileInfo.ReleaseStream();
						}
					}

					using (var writer = new PackageWriter(package, output))
					{
						writer.Version = Package.CurrentVersion;
						writer.Compression = LSLib.LS.Enums.CompressionMethod.LZ4;
						writer.CompressionLevel = CompressionLevel.MaxCompression;
						writer.Write();
					}

					// Copy dates so the inner meta.lsf doesn't complain
					File.SetLastWriteTime(output, File.GetLastWriteTime(pathToSave));
					File.SetLastAccessTime(output, File.GetLastAccessTime(pathToSave));

					return true;
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
