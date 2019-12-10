using DivinityModManager.Models;
using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using LSLib.LS;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace DivinityModManager.Util
{
	public static class DivinityModDataLoader
	{
		public static List<DivinityModData> LarianMods { get; private set; } = new List<DivinityModData>()
		{
			new DivinityModData{ Name = "Shared", UUID = "2bd9bdbe-22ae-4aa2-9c93-205880fc6564", Folder = "Shared" },
			new DivinityModData{ Name = "Shared_DOS", UUID = "eedf7638-36ff-4f26-a50a-076b87d53ba0", Folder = "Shared_DOS" },
			new DivinityModData{ Name = "Divinity: Original Sin 2", UUID = "1301db3d-1f54-4e98-9be5-5094030916e4", Folder="DivinityOrigins_1301db3d-1f54-4e98-9be5-5094030916e4", Version = new DivinityModVersion(372251161)},
			new DivinityModData{ Name = "Arena", UUID = "a99afe76-e1b0-43a1-98c2-0fd1448c223b", Folder = "DOS2_Arena"},
			new DivinityModData{ Name = "Game Master", UUID = "00550ab2-ac92-410c-8d94-742f7629de0e", Folder = "GameMaster"},
			new DivinityModData{ Name = "Character_Creation_Pack", UUID = "b40e443e-badd-4727-82b3-f88a170c4db7", Folder="Character_Creation_Pack"},
			new DivinityModData{ Name = "Nine Lives", UUID = "015de505-6e7f-460c-844c-395de6c2ce34", Folder="AS_BlackCatPlus"},
			new DivinityModData{ Name = "Herb Gardens", UUID = "38608c30-1658-4f6a-8adf-e826a5295808", Folder="AS_GrowYourHerbs"},
			new DivinityModData{ Name = "Endless Runner", UUID = "ec27251d-acc0-4ab8-920e-dbc851e79bb4", Folder="AS_ToggleSpeedAddon"},
			new DivinityModData{ Name = "8 Action Points", UUID = "9b45f7e5-d4e2-4fc2-8ef7-3b8e90a5256c", Folder="CMP_8AP_Kamil"},
			new DivinityModData{ Name = "Hagglers", UUID = "f33ded5d-23ab-4f0c-b71e-1aff68eee2cd", Folder="CMP_BarterTweaks"},
			new DivinityModData{ Name = "Crafter's Kit", UUID = "68a99fef-d125-4ed0-893f-bb6751e52c5e", Folder="CMP_CraftingOverhaul"},
			new DivinityModData{ Name = "Combat Randomiser", UUID = "f30953bb-10d3-4ba4-958c-0f38d4906195", Folder="CMP_EnemyRandomizer_Kamil"},
			new DivinityModData{ Name = "Animal Empathy", UUID = "423fae51-61e3-469a-9c1f-8ad3fd349f02", Folder="CMP_Free_PetPalTag_Kamil"},
			new DivinityModData{ Name = "Fort Joy Magic Mirror", UUID = "2d42113c-681a-47b6-96a1-d90b3b1b07d3", Folder="CMP_FTJRespec_Kamil"},
			new DivinityModData{ Name = "Sourcerous Sundries", UUID = "a945eefa-530c-4bca-a29c-a51450f8e181", Folder="CMP_LevelUpEquipment"},
			new DivinityModData{ Name = "Improved Organisation", UUID = "f243c84f-9322-43ac-96b7-7504f990a8f0", Folder="CMP_OrganizedContainers_Marek"},
			new DivinityModData{ Name = "Pet Power", UUID = "d2507d43-efce-48b8-ba5e-5dd136c715a7", Folder="CMP_SummoningImproved_Kamil"}
		};

		public static List<DivinityModData> IgnoredMods { get; set; } = new List<DivinityModData>(LarianMods);

		public static bool IgnoreMod(string modUUID)
		{
			return IgnoredMods.Any(m => m.UUID == modUUID);
		}

		public static bool IgnoreModByFolder(string folder)
		{
			return IgnoredMods.Any(m => m.Folder.Equals(folder, StringComparison.OrdinalIgnoreCase));
		}

		public static string MakeSafeFilename(string filename, char replaceChar)
		{
			foreach (char c in System.IO.Path.GetInvalidFileNameChars())
			{
				filename = filename.Replace(c, replaceChar);
			}
			return filename;
		}

		/// <summary>
		/// Gets an attribute node with the supplied id, return the value.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="attribute"></param>
		/// <param name="fallbackValue"></param>
		/// <returns></returns>
		private static string GetAttribute(XElement node, string id, string fallbackValue = "")
		{
			var att = node.Descendants("attribute").FirstOrDefault(a => a.Attribute("id")?.Value == id)?.Attribute("value")?.Value;
			if (att != null)
			{
				return att;
			}
			return fallbackValue;
		}

		private static int SafeConvertString(string str)
		{
			if(!String.IsNullOrWhiteSpace(str) && int.TryParse(str, out int val))
			{
				return val;
			}
			return -1;
		}

		private static DivinityModData ParseMetaFile(string metaContents)
		{
			try
			{
				XElement xDoc = XElement.Parse(metaContents);
				var moduleInfoNode = xDoc.Descendants("node").FirstOrDefault(n => n.Attribute("id")?.Value == "ModuleInfo");
				if (moduleInfoNode != null)
				{
					DivinityModData modData = new DivinityModData()
					{
						UUID = GetAttribute(moduleInfoNode, "UUID", ""),
						Name = GetAttribute(moduleInfoNode, "Name", ""),
						Author = GetAttribute(moduleInfoNode, "Author", ""),
						Version = DivinityModVersion.FromInt(SafeConvertString(GetAttribute(moduleInfoNode, "Version", ""))),
						Folder = GetAttribute(moduleInfoNode, "Folder", ""),
						Description = GetAttribute(moduleInfoNode, "Description", ""),
						MD5 = GetAttribute(moduleInfoNode, "MD5", ""),
						Type = GetAttribute(moduleInfoNode, "Type", "")
					};
					//var dependenciesNodes = xDoc.SelectNodes("//node[@id='ModuleShortDesc']");
					var dependenciesNodes = xDoc.Descendants("node").Where(n => n.Attribute("id")?.Value == "ModuleShortDesc");

					if (dependenciesNodes != null)
					{
						foreach (var node in dependenciesNodes)
						{
							DivinityModDependencyData dependencyMod = new DivinityModDependencyData()
							{
								UUID = GetAttribute(node, "UUID", ""),
								Name = GetAttribute(node, "Name", ""),
								Version = DivinityModVersion.FromInt(SafeConvertString(GetAttribute(node, "Version", ""))),
								Folder = GetAttribute(node, "Folder", ""),
								MD5 = GetAttribute(node, "MD5", "")
							};
							//Trace.WriteLine($"Added dependency to {modData.Name} - {dependencyMod.ToString()}");
							if (dependencyMod.UUID != "")
							{
								modData.Dependencies.Add(dependencyMod);
							}
						}
					}
					modData.UpdateDependencyText();

					var targets = moduleInfoNode.Descendants("node").Where(n => n.Attribute("id")?.Value == "Target");
					if(targets != null)
					{
						List<string> allTargetValues = new List<string>();
						foreach (var node in targets)
						{
							var target = GetAttribute(node, "Object", "");
							if(!String.IsNullOrEmpty(target))
							{
								allTargetValues.Add(target);
							}
						}
						modData.Targets = string.Join(", ", allTargetValues);
					}

					return modData;
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine($"Error parsing meta.lsx: {ex.ToString()}");
			}
			return null;
		}

		public static List<DivinityModData> LoadEditorProjects(string modsFolderPath)
		{
			List<DivinityModData> projects = new List<DivinityModData>();

			try
			{
				if (Directory.Exists(modsFolderPath))
				{
					var projectDirectories = Directory.EnumerateDirectories(modsFolderPath);
					var filteredFolders = projectDirectories.Where(f => !IgnoreModByFolder(f));
					Console.WriteLine("Project Folders: " + filteredFolders.Count());
					foreach (var folder in filteredFolders)
					{
						//Console.WriteLine($"Folder: {Path.GetFileName(folder)} Blacklisted: {IgnoredMods.Any(m => Path.GetFileName(folder).Equals(m.Folder, StringComparison.OrdinalIgnoreCase))}");
						var metaFile = Path.Combine(folder, "meta.lsx");
						if (File.Exists(metaFile))
						{
							var str = File.ReadAllText(metaFile);
							var modData = ParseMetaFile(str);
							modData.IsEditorMod = true;
							modData.FilePath = folder;
							try
							{
								modData.LastModified = File.GetChangeTime(metaFile);
							}
							catch (PlatformNotSupportedException ex)
							{
								Trace.WriteLine($"Error getting last modified date for '{metaFile}': {ex.ToString()}");
							}
							if (modData != null) projects.Add(modData);
						}
					}
				}
			}
			catch(Exception ex)
			{
				Trace.WriteLine($"Error loading mod projects: {ex.ToString()}");
			}
			return projects;
		}

		private static Regex multiPartPakPattern = new Regex("_[0-9]+.pak", RegexOptions.IgnoreCase | RegexOptions.Singleline);

		private static bool CanProcessPak(FileSystemEntryInfo f)
		{
			return !multiPartPakPattern.IsMatch(f.FileName) && Path.GetExtension(f.Extension).Equals(".pak", StringComparison.OrdinalIgnoreCase);
		}

		public static List<DivinityModData> LoadModPackageData(string modsFolderPath)
		{
			List<DivinityModData> mods = new List<DivinityModData>();

			if (Directory.Exists(modsFolderPath))
			{
				List<string> modPaks = new List<string>();
				try
				{
					var files = Directory.EnumerateFiles(modsFolderPath, DirectoryEnumerationOptions.Files | DirectoryEnumerationOptions.Recursive,
						new DirectoryEnumerationFilters()
						{
							InclusionFilter = CanProcessPak
						});
					if (files != null)
					{
						modPaks.AddRange(files);
					}
				}
				catch(Exception ex)
				{
					Trace.WriteLine($"Error enumerating pak folder '{modsFolderPath}': {ex.ToString()}");
				}

				Trace.WriteLine("Mod Packages: " + modPaks.Count());
				foreach (var pakPath in modPaks)
				{
					try
					{
						using (var pr = new LSLib.LS.PackageReader(pakPath))
						{
							var pak = pr.Read();
							var metaFile = pak?.Files?.FirstOrDefault(pf => pf.Name.Contains("meta.lsx"));
							if (metaFile != null)
							{
								using (var stream = metaFile.MakeStream())
								{
									using (var sr = new System.IO.StreamReader(stream))
									{
										string text = sr.ReadToEnd();
										var modData = ParseMetaFile(text);
										if (modData != null)
										{
											modData.FilePath = pakPath;
											try
											{
												modData.LastModified = File.GetChangeTime(pakPath);
											}
											catch (PlatformNotSupportedException ex)
											{
												Trace.WriteLine($"Error getting pak last modified date for '{pakPath}': {ex.ToString()}");
											}
											mods.Add(modData);
										}
									}
								}
							}
						}
					}
					catch (Exception ex)
					{
						Trace.WriteLine($"Error loading pak '{pakPath}': {ex.ToString()}");
					}
				}
			}

			try
			{
				
			}
			catch(Exception ex)
			{
				Trace.WriteLine($"Error loading mod paks: {ex.ToString()}");
			}

			return mods;
		}

		private static Node FindResourceNode(Node node, string attribute, string matchVal)
		{
			if (node.Attributes.TryGetValue(attribute, out var att))
			{
				var attVal = (string)att.Value;
				if (attVal.Equals(matchVal, StringComparison.OrdinalIgnoreCase))
				{
					return node;
				}
			}
			foreach(var nList in node.Children.Values)
			{
				foreach(var n in nList)
				{
					var match = FindResourceNode(n, attribute, matchVal);
					if (match != null) return match;
				}
			}
			return null;
		}

		private static bool NodeIdMatch(Node n, string id)
		{
			if(n.Attributes.TryGetValue("id", out var att))
			{
				string idAttVal = (string)att.Value;
				return id.Equals(idAttVal, StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		public static List<DivinityProfileData> LoadProfileData(string profilePath)
		{
			List<DivinityProfileData> profiles = new List<DivinityProfileData>();
			if (Directory.Exists(profilePath))
			{
				var profileDirectories = Directory.EnumerateDirectories(profilePath);
				foreach (var folder in profileDirectories)
				{
					string displayName = Path.GetFileName(folder);
					string storedDisplayedName = displayName;
					string profileUUID = "";

					//Console.WriteLine($"Folder: {Path.GetFileName(folder)} Blacklisted: {IgnoredMods.Any(m => Path.GetFileName(folder).Equals(m.Folder, StringComparison.OrdinalIgnoreCase))}");
					var profileFile = Path.Combine(folder, "profile.lsb");
					if (File.Exists(profileFile))
					{
						var profileRes = ResourceUtils.LoadResource(profileFile, LSLib.LS.Enums.ResourceFormat.LSB);
						if(profileRes != null && profileRes.Regions.TryGetValue("PlayerProfile", out var region))
						{
							if(region.Attributes.TryGetValue("PlayerProfileDisplayName", out var profileDisplayNameAtt))
							{
								storedDisplayedName = (string)profileDisplayNameAtt.Value;
							}
							if (region.Attributes.TryGetValue("PlayerProfileID", out var profileIdAtt))
							{
								profileUUID = (string)profileIdAtt.Value;
							}
						}
					}

					var profileData = new DivinityProfileData()
					{
						Name = displayName,
						ProfileName = storedDisplayedName,
						UUID = profileUUID,
						Folder = Path.GetFullPath(folder)
					};

					var modSettingsFile = Path.Combine(folder, "modsettings.lsx");
					if(File.Exists(modSettingsFile))
					{
						Resource modSettingsRes = null;
						try
						{
							modSettingsRes = ResourceUtils.LoadResource(modSettingsFile, LSLib.LS.Enums.ResourceFormat.LSX);
						}
						catch(Exception ex)
						{
							Trace.WriteLine($"Error reading '{modSettingsFile}': '{ex.ToString()}'");
						}
						
						if (modSettingsRes != null && modSettingsRes.Regions.TryGetValue("ModuleSettings", out var region))
						{
							if(region.Children.TryGetValue("ModOrder", out var modOrderRootNode))
							{
								var modOrderChildrenRoot = modOrderRootNode.FirstOrDefault();
								if(modOrderChildrenRoot != null)
								{
									var modOrder = modOrderChildrenRoot.Children.Values.FirstOrDefault();
									if(modOrder != null)
									{
										foreach (var c in modOrder)
										{
											//Trace.WriteLine($"ModuleNode: {c.Name} Attributes: {String.Join(";", c.Attributes.Keys)}");
											if (c.Attributes.TryGetValue("UUID", out var attribute))
											{
												var uuid = (string)attribute.Value;
												if(!string.IsNullOrEmpty(uuid))
												{
													profileData.ModOrder.Add(uuid);
												}
											}
										}
									}
								}
							}

							if (region.Children.TryGetValue("Mods", out var modListRootNode))
							{
								var modListChildrenRoot = modListRootNode.FirstOrDefault();
								if (modListChildrenRoot != null)
								{
									var modList = modListChildrenRoot.Children.Values.FirstOrDefault();
									if (modList != null)
									{
										foreach (var c in modList)
										{
											//Trace.WriteLine($"ModuleNode: {c.Name} Attributes: {String.Join(";", c.Attributes.Keys)}");
											if (c.Attributes.TryGetValue("UUID", out var attribute))
											{
												var uuid = (string)attribute.Value;
												if (!string.IsNullOrEmpty(uuid))
												{
													profileData.ActiveMods.Add(uuid);
												}
											}
										}
									}
								}
							}
						}
					}

					profiles.Add(profileData);
				}
			}
			return profiles;
		}

		public static string GetSelectedProfileUUID(string profilePath)
		{
			var playerprofilesFile = Path.Combine(profilePath, "playerprofiles.lsb");
			string activeProfileUUID = "";
			if (File.Exists(playerprofilesFile))
			{
				Trace.WriteLine($"Loading playerprofiles.lsb at '{playerprofilesFile}'");
				var res = ResourceUtils.LoadResource(playerprofilesFile, LSLib.LS.Enums.ResourceFormat.LSB);
				if (res != null && res.Regions.TryGetValue("UserProfiles", out var region))
				{
					Trace.WriteLine($"ActiveProfile | Getting root node '{String.Join(";", region.Attributes.Keys)}'");

					if (region.Attributes.TryGetValue("ActiveProfile", out var att))
					{
						Trace.WriteLine($"ActiveProfile | '{att.Type} {att.Value}'");
						activeProfileUUID = (string)att.Value;
					}
				}
			}
			return activeProfileUUID;
		}

		public static bool ExportLoadOrderToFile(string outputFilePath, DivinityLoadOrder order)
		{
			var parentDir = Path.GetDirectoryName(outputFilePath);
			if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

			string contents = JsonConvert.SerializeObject(order, Newtonsoft.Json.Formatting.Indented);

			var buffer = Encoding.UTF8.GetBytes(contents);
			using (var fs = new System.IO.FileStream(outputFilePath, System.IO.FileMode.Create,
				System.IO.FileAccess.Write, System.IO.FileShare.None, buffer.Length, false))
			{
				fs.Write(buffer, 0, buffer.Length);
			}

			return true;
		}

		public static async Task<bool> ExportLoadOrderToFileAsync(string outputFilePath, DivinityLoadOrder order)
		{
			var parentDir = Path.GetDirectoryName(outputFilePath);
			if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

			string contents = JsonConvert.SerializeObject(order, Newtonsoft.Json.Formatting.Indented);

			var buffer = Encoding.UTF8.GetBytes(contents);
			using (var fs = new System.IO.FileStream(outputFilePath, System.IO.FileMode.Create,
				System.IO.FileAccess.Write, System.IO.FileShare.None, buffer.Length, true))
			{
				await fs.WriteAsync(buffer, 0, buffer.Length);
			}

			return true;
		}

		public static async Task<List<DivinityLoadOrder>> FindLoadOrderFilesInDirectoryAsync(string directory)
		{
			List<DivinityLoadOrder> loadOrders = new List<DivinityLoadOrder>();

			if (Directory.Exists(directory))
			{
				var files = Directory.EnumerateFiles(directory, DirectoryEnumerationOptions.Files | DirectoryEnumerationOptions.Recursive, new DirectoryEnumerationFilters()
				{
					InclusionFilter = (f) =>
					{
						return f.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
					}
				});

				foreach(var loadOrderFile in files)
				{
					try
					{
						using (var reader = File.OpenText(loadOrderFile))
						{
							var fileText = await reader.ReadToEndAsync();
							DivinityLoadOrder order = JsonConvert.DeserializeObject<DivinityLoadOrder>(fileText);
							if (order != null)
							{
								order.LastModifiedDate = File.GetLastWriteTime(loadOrderFile);

								/*
								if (loadOrderFile.IndexOf("_Current.json", StringComparison.OrdinalIgnoreCase) > -1)
								{
									var fileName = Path.GetFileNameWithoutExtension(loadOrderFile);
									var profile = fileName.Split('_').First();
									order.Name = $"Current ({profile})";
								}
								*/

								loadOrders.Add(order);
							}
						}
					}
					catch(Exception ex)
					{
						Trace.WriteLine($"Failed to read '{loadOrderFile}': {ex.ToString()}");
					}
				}
			}

			return loadOrders;
		}
		public static async Task<DivinityLoadOrder> LoadOrderFromFileAsync(string loadOrderFile)
		{
			if(File.Exists(loadOrderFile))
			{
				using (var reader = File.OpenText(loadOrderFile))
				{
					var fileText = await reader.ReadToEndAsync();
					DivinityLoadOrder order = JsonConvert.DeserializeObject<DivinityLoadOrder>(fileText);
					return order;
				}
			}
			return null;
		}

		public static async Task<bool> ExportModSettingsToFileAsync(string folder, DivinityLoadOrder order, IEnumerable<DivinityModData> allMods)
		{
			if(Directory.Exists(folder))
			{
				string outputFilePath = Path.Combine(folder, "modsettings.lsx");
				string contents = GenerateModSettingsFile(order.Order, allMods);
				try
				{
					var buffer = Encoding.UTF8.GetBytes(contents);
					using (var fs = new System.IO.FileStream(outputFilePath, System.IO.FileMode.Create, 
						System.IO.FileAccess.Write, System.IO.FileShare.None, buffer.Length, true))
					{
						await fs.WriteAsync(buffer, 0, buffer.Length);
					}

					return true;
				}
				catch(AccessViolationException ex)
				{
					Trace.WriteLine($"Failed to write file '{outputFilePath}': {ex.ToString()}");
				}
			}
			return false;
		}

		public static string GenerateModSettingsFile(IEnumerable<DivinityLoadOrderEntry> order, IEnumerable<DivinityModData> allMods)
		{
			/* The ModOrder node contains the load order. DOS2 by default stores all UUIDs, even if the mod no longer exists. */
			string modulesText = "";
			foreach(var uuid in order.Select(m => m.UUID))
			{
				modulesText += String.Format(Properties.Resources.ModSettingsModOrderModuleNode, uuid) + Environment.NewLine;
			}

			/* Active mods are contained within the Mods node. Origins is always included at the top, despite it not being in ModOrder. */
			string modShortDescText = "";
			var dos2Origins = IgnoredMods.First(x => x.Name == "Divinity: Original Sin 2");
			modShortDescText += String.Format(Properties.Resources.ModSettingsModuleShortDescNode, dos2Origins.Folder, dos2Origins.MD5, dos2Origins.Name, dos2Origins.UUID, dos2Origins.Version.VersionInt) + Environment.NewLine;
			foreach (var mod in allMods.Where(m => order.Any(o => o.UUID == m.UUID)))
			{
				string safeName = System.Security.SecurityElement.Escape(mod.Name);
				modShortDescText += String.Format(Properties.Resources.ModSettingsModuleShortDescNode, mod.Folder, mod.MD5, safeName, mod.UUID, mod.Version.VersionInt) + Environment.NewLine;
			}
			string output = String.Format(Properties.Resources.ModSettingsTemplate, modulesText, modShortDescText);
			//Trace.WriteLine(output);
			return output;
		}

		public static string CreateHandle()
		{
			return Guid.NewGuid().ToString().Replace('-', 'g').Insert(0, "h");
		}
	}
}
