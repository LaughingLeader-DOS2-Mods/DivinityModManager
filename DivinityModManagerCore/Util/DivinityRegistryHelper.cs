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
		const string REG_GOG_32 = @"SOFTWARE\GOG.com\Games";
		const string REG_GOG_64 = @"SOFTWARE\Wow6432Node\GOG.com\Games";

		const string PATH_Steam_WorkshopFolder = @"steamapps/workshop";
		const string PATH_Steam_LibraryFile = @"steamapps/libraryfolders.vdf";

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

		private static string lastGamePath = "";
		private static bool isGOG = false;
		public static bool IsGOG => isGOG;

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
				DivinityApp.Log($"Error reading registry subKey ({subKey}): {e.ToString()}");
			}
			return null;
		}

		public static string GetTruePath(string path)
		{
			try
			{
				var driveType = DivinityFileUtils.GetPathDriveType(path);
				if (driveType == System.IO.DriveType.Fixed)
				{
					if (JunctionPoint.Exists(path))
					{
						string realPath = JunctionPoint.GetTarget(path);
						if (!String.IsNullOrEmpty(realPath))
						{
							return realPath;
						}
					}
				}
				else
				{
					DivinityApp.Log($"Skipping junction check for path '{path}'. Drive type is '{driveType}'.");
				}
			}
			catch (Exception ex) 
			{
				DivinityApp.Log($"Error checking junction point '{path}': {ex.ToString()}");
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
				string workshopFolder = Path.Combine(LastSteamInstallPath, PATH_Steam_WorkshopFolder);
				DivinityApp.Log($"Looking for workshop folder at '{workshopFolder}'.");
				if(Directory.Exists(workshopFolder))
				{
					return workshopFolder;
				}
			}
			return "";
		}
		
		public static string GetWorkshopPath(string appid)
		{
			if (LastSteamInstallPath != "")
			{
				string steamWorkshopPath = GetSteamWorkshopPath();
				if(!String.IsNullOrEmpty(steamWorkshopPath))
				{
					string workshopFolder = Path.Combine(steamWorkshopPath, "content", appid);
					DivinityApp.Log($"Looking for game workshop folder at '{workshopFolder}'.");
					if (Directory.Exists(workshopFolder))
					{
						return workshopFolder;
					}
				}
			}
			return "";
		}

		public static string GetGOGInstallPath(string gogRegKey32, string gogRegKey64)
		{
			RegistryKey reg = Registry.LocalMachine;
			object installPath = GetKey(reg, gogRegKey32, "path");
			if (installPath == null)
			{
				installPath = GetKey(reg, gogRegKey64, "path");
			}
			if (installPath != null)
			{
				return (string)installPath;
			}
			return "";
		}

		public static string GetGameInstallPath(string steamGameInstallPath, string gogRegKey32, string gogRegKey64)
		{
			try
			{
				if (LastSteamInstallPath != "")
				{
					if (!String.IsNullOrEmpty(lastGamePath) && Directory.Exists(lastGamePath))
					{
						return lastGamePath;
					}
					string folder = Path.Combine(LastSteamInstallPath, "steamapps", "common", steamGameInstallPath);
					if (Directory.Exists(folder))
					{
						DivinityApp.Log($"Found game at '{folder}'.");
						lastGamePath = folder;
						isGOG = false;
						return lastGamePath;
					}
					else
					{
						DivinityApp.Log($"Game not found. Looking for Steam libraries.");
						string libraryFile = Path.Combine(LastSteamInstallPath, PATH_Steam_LibraryFile);
						if (File.Exists(libraryFile))
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
												DivinityApp.Log($"Found steam library folder at '{p}'.");
												libraryFolders.Add(p);
											}
										}
									}
								}
							}
							catch (Exception ex)
							{
								DivinityApp.Log($"Error parsing steam library file at '{libraryFile}': {ex.ToString()}");
							}

							foreach (var folderPath in libraryFolders)
							{
								string checkFolder = Path.Combine(folderPath, "steamapps", "common", steamGameInstallPath);
								DivinityApp.Log(checkFolder);
								if (!String.IsNullOrEmpty(checkFolder) && Directory.Exists(checkFolder))
								{
									DivinityApp.Log($"Found game at '{checkFolder}'.");
									lastGamePath = checkFolder;
									isGOG = false;
									return lastGamePath;
								}
							}
						}
					}
				}

				string gogGamePath = GetGOGInstallPath(gogRegKey32, gogRegKey64);
				if (!String.IsNullOrEmpty(gogGamePath) && Directory.Exists(gogGamePath))
				{
					isGOG = true;
					lastGamePath = gogGamePath;
					DivinityApp.Log($"Found game (GoG) install at '{lastGamePath}'.");
					return lastGamePath;
				}
			}
			catch(Exception ex)
			{
				DivinityApp.Log($"[*ERROR*] Error finding game path: {ex.ToString()}");
			}

			return "";
		}
	}
}
