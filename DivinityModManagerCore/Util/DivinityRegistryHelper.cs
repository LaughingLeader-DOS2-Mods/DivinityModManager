using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using Microsoft.Win32;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;

namespace DivinityModManager.Util
{
	public static class DivinityRegistryHelper
	{
		const string REG_Steam_32 = @"SOFTWARE\Valve\Steam";
		const string REG_Steam_64 = @"SOFTWARE\Wow6432Node\Valve\Steam";
		const string PATH_Steam_WorkshopFolder = @"steamapps/workshop";
		const string PATH_Steam_LibraryFile = @"steamapps/libraryfolders.vdf";
		const string PATH_Steam_DivinityOriginalSin2 = @"steamapps/common/Divinity Original Sin 2";
		const string PATH_Steam_DivinityOriginalSin2_WorkshopFolder = @"content/435150";

		private static string lastSteamInstallPath = "";
		private static string LastSteamInstallPath
		{
			get
			{
				if(lastSteamInstallPath == "" || !Directory.Exists(lastSteamInstallPath))
				{
					lastSteamInstallPath = GetSteamInstallPath();
				}
				return lastSteamInstallPath;
			}
		}

		private static string lastDivinityOriginalSin2Path = "";
		private static string LastDivinityOriginalSin2Path => lastDivinityOriginalSin2Path;

		private static object GetKey(RegistryKey reg, string subKey, string keyValue)
		{
			try
			{
				RegistryKey key = reg.OpenSubKey(subKey);
				if (key != null)
				{
					return key.GetValue(keyValue);
				}
			}
			catch (Exception e)
			{
				Trace.WriteLine($"Error reading registry subKey ({subKey}): {e.ToString()}");
			}
			return null;
		}

		public static string GetTruePath(string path)
		{
			if (JunctionPoint.Exists(path))
			{
				string realPath = JunctionPoint.GetTarget(path);
				if(!String.IsNullOrEmpty(realPath))
				{
					return realPath;
				}
			}
			return path;
		}

		public static string GetSteamInstallPath()
		{
			RegistryKey reg = Registry.LocalMachine;
			object installPath = GetKey(reg, REG_Steam_64, "InstallPath");
			if (installPath == null)
			{
				installPath = GetKey(reg, REG_Steam_32, "InstallPath");
			}
			if (installPath != null)
			{
				return (string)installPath;
			}
			return "";
		}
  
		public static string GetSteamWorkshopPath()
		{
			if(LastSteamInstallPath != "")
			{
				string workshopFolder = GetTruePath(Path.Combine(LastSteamInstallPath, PATH_Steam_WorkshopFolder));
				Trace.WriteLine($"Looking for workshop folder at '{workshopFolder}'.");
				if(Directory.Exists(workshopFolder))
				{
					return workshopFolder;
				}
			}
			return "";
		}
		
		public static string GetDOS2WorkshopPath()
		{
			if (LastSteamInstallPath != "")
			{
				string workshopFolder = GetTruePath(Path.Combine(GetSteamWorkshopPath(), PATH_Steam_DivinityOriginalSin2_WorkshopFolder));
				Trace.WriteLine($"Looking for Divinity Original Sin 2 workshop folder at '{workshopFolder}'.");
				if (Directory.Exists(workshopFolder))
				{
					return workshopFolder;
				}
			}
			return "";
		}

		public static string GetDOS2Path()
		{
			if (LastSteamInstallPath != "")
			{
				if(!String.IsNullOrEmpty(lastDivinityOriginalSin2Path) && Directory.Exists(lastDivinityOriginalSin2Path))
				{
					return lastDivinityOriginalSin2Path;
				}
				string folder = Path.Combine(LastSteamInstallPath, PATH_Steam_DivinityOriginalSin2);
				if (Directory.Exists(folder))
				{
					Trace.WriteLine($"Found Divinity Original Sin 2 at '{folder}'.");
					lastDivinityOriginalSin2Path = folder;
					return folder;
				}
				else
				{
					Trace.WriteLine($"Divinity Original Sin 2 not found. Looking for Steam libraries.");
					string libraryFile = Path.Combine(LastSteamInstallPath, PATH_Steam_LibraryFile);
					if(File.Exists(libraryFile))
					{
						List<string> libraryFolders = new List<string>();
						try
						{
							var libraryData = VdfConvert.Deserialize(File.ReadAllText(libraryFile));
							foreach (VProperty token in libraryData.Value.Children())
							{
								if (token.Key != "TimeNextStatsReport" && token.Key != "ContentStatsID")
								{
									if (token.Value is VValue innerValue)
									{
										var p = innerValue.Value<string>();
										if (Directory.Exists(p))
										{
											Trace.WriteLine($"Found steam library folder at '{p}'.");
											libraryFolders.Add(p);
										}
									}
								}
							}
						}
						catch(Exception ex)
						{
							Trace.WriteLine($"Error parsing steam library file at '{libraryFile}': {ex.ToString()}");
						}
						
						foreach(var folderPath in libraryFolders)
						{
							string checkFolder = GetTruePath(Path.Combine(folderPath, PATH_Steam_DivinityOriginalSin2));
							if (Directory.Exists(checkFolder))
							{
								Trace.WriteLine($"Found Divinity Original Sin 2 at '{checkFolder}'.");
								lastDivinityOriginalSin2Path = checkFolder;
								return lastDivinityOriginalSin2Path;
							}
						}
					}
				}
			}
			return "";
		}
	}
}
