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
using System.Runtime.Serialization.Formatters.Binary;

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

						DivinityApp.Log($"Renamed internal screenshot '{saveScreenshotImage.Name}' in '{output}'.");
					}

					// Edit the saved date in the meta.lsf to avoid "corruption" messages
					/*
					AbstractFileInfo metaFile = package.Files.FirstOrDefault(p => p.Name == "meta.lsf");
					if (metaFile != null)
					{
						Resource resource;
						System.IO.MemoryStream ms = null;
						System.IO.Stream rsrcStream = null;
						try
						{
							rsrcStream = metaFile.MakeStream();
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

										DivinityApp.LogMessage($"Year: {yearAtt.Type}");
										DivinityApp.LogMessage($"Month: {monthAtt.Type}");
										DivinityApp.LogMessage($"Day: {dayAtt.Type}");
										DivinityApp.LogMessage($"Hours: {hoursAtt.Type}");
										DivinityApp.LogMessage($"Minutes: {minutesAtt.Type}");
										DivinityApp.LogMessage($"Seconds: {secondsAtt.Type}");
										DivinityApp.LogMessage($"Milliseconds: {millisecondsAtt.Type}");

										yearAtt.Value = (Byte)time.Year;
										monthAtt.Value = (Byte)time.Month;
										dayAtt.Value = (Byte)time.Day;
										hoursAtt.Value = (Byte)time.Hour;
										minutesAtt.Value = (Byte)time.Minute;
										secondsAtt.Value = (Byte)time.Second;
										millisecondsAtt.Value = (UInt16)time.Millisecond;

										DivinityApp.LogMessage($"Updated SaveTime in save's meta.lsf.");
									}
									else
									{
										DivinityApp.LogMessage($"Couldn't find SaveTime node '{String.Join(";", resource.Regions.Values.First().Children.Keys)}'.");
									}

									ms = new System.IO.MemoryStream(new byte[4096], true);
									var rscrWriter = new LSFWriter(ms, FileVersion.CurrentVersion);
									rscrWriter.Write(resource);
									ms.Position = 0;
									var data = ms.ToArray();

									if (!ms.CanRead) DivinityApp.LogMessage("MemoryStream is not readable!");
									if(!ms.CanWrite) DivinityApp.LogMessage("MemoryStream is not writable!");
									if(!rsrcStream.CanRead) DivinityApp.LogMessage("rsrcStream is not readable!");
									if(!rsrcStream.CanWrite) DivinityApp.LogMessage("rsrcStream is not writable!");

									rsrcStream.Write(data, 0, data.Length);
									ms.Close();
								}
							}
						}
						finally
						{
							if (metaFile != null) metaFile.ReleaseStream();
							if (ms != null) ms.Dispose();
							if (rsrcStream != null) rsrcStream.Dispose();
						}
					}
					*/
					using (var writer = new PackageWriter(package, output))
					{
						writer.Version = Package.CurrentVersion;
						writer.Compression = LSLib.LS.Enums.CompressionMethod.Zlib;
						writer.CompressionLevel = CompressionLevel.DefaultCompression;
						writer.Write();
					}

					File.SetLastWriteTime(output, File.GetLastWriteTime(pathToSave));
					File.SetLastAccessTime(output, File.GetLastAccessTime(pathToSave));

					return true;
				}
				
			}
			catch(Exception ex)
			{
				DivinityApp.Log($"Failed to rename save: {ex.ToString()}");
			}

			return false;
		}
	}
}
