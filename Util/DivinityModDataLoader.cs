using DivinityModManager.Models;
using System;
using System.Collections.Generic;
#if NETFRAMEWORK
using Alphaleonis.Win32.Filesystem;
#else
using System.IO;
#endif
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Linq;

namespace DivinityModManager.Util
{
	public static class DivinityModDataLoader
	{
		public static List<DivinityModData> IgnoredMods = new List<DivinityModData>()
		{
			new DivinityModData{ Name = "Shared", UUID = "2bd9bdbe-22ae-4aa2-9c93-205880fc6564", Folder = "Shared" },
			new DivinityModData{ Name = "Shared_DOS", UUID = "eedf7638-36ff-4f26-a50a-076b87d53ba0", Folder = "Shared_DOS" },
			new DivinityModData{ Name = "Divinity: Original Sin 2", UUID = "1301db3d-1f54-4e98-9be5-5094030916e4", Folder="DivinityOrigins_1301db3d-1f54-4e98-9be5-5094030916e4"},
			new DivinityModData{ Name = "Arena", UUID = "a99afe76-e1b0-43a1-98c2-0fd1448c223b", Folder = "DOS2_Arena"},
			new DivinityModData{ Name = "Game Master", UUID = "00550ab2-ac92-410c-8d94-742f7629de0e", Folder = "GameMaster"},
			new DivinityModData{ Name = "Character_Creation_Pack", UUID = "b40e443e-badd-4727-82b3-f88a170c4db7", Folder="Character_Creation_Pack"}
		};

		public static bool IgnoreMod(string modUUID)
		{
			return IgnoredMods.Any(m => m.UUID == modUUID);
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
						Description = GetAttribute(moduleInfoNode, "Description", "")
					};
					//var dependenciesNodes = xDoc.SelectNodes("//node[@id='ModuleShortDesc']");
					var dependenciesNodes = xDoc.Descendants("node").Where(n => n.Attribute("id")?.Value == "ModuleShortDesc");

					if (dependenciesNodes != null)
					{
						foreach (var node in dependenciesNodes)
						{
							DivinityModDependency dependencyMod = new DivinityModDependency()
							{
								UUID = GetAttribute(node, "UUID", ""),
								Name = GetAttribute(node, "Name", ""),
								Version = DivinityModVersion.FromInt(SafeConvertString(GetAttribute(node, "Version", "")))
							};
							Console.WriteLine($"Added dependency to {modData.Name} - {dependencyMod.ToString()}");
							if (dependencyMod.UUID != "")
							{
								modData.Dependencies.Add(dependencyMod);
							}
						}
					}
					modData.UpdateDependencyText();
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

			if(Directory.Exists(modsFolderPath))
			{
				var projectDirectories = Directory.EnumerateDirectories(modsFolderPath);
				var filteredFolders = projectDirectories.Where(f => !IgnoredMods.Any(m => Path.GetFileName(f).Equals(m.Folder, StringComparison.OrdinalIgnoreCase)));
				Console.WriteLine("Project Folders: " + filteredFolders.Count());
				foreach (var folder in filteredFolders)
				{
					//Console.WriteLine($"Folder: {Path.GetFileName(folder)} Blacklisted: {IgnoredMods.Any(m => Path.GetFileName(folder).Equals(m.Folder, StringComparison.OrdinalIgnoreCase))}");
					var metaFile = Path.Combine(folder, "meta.lsx");
					if(File.Exists(metaFile))
					{
						var str = File.ReadAllText(metaFile);
						var modData = ParseMetaFile(str);
						if (modData != null) projects.Add(modData);
					}
				}
			}
			return projects;
		}

		public static List<DivinityModData> LoadModPackageData(string modsFolderPath)
		{
			List<DivinityModData> mods = new List<DivinityModData>();

#if NETFRAMEWORK
			if (Directory.Exists(modsFolderPath))
			{
				var modPaks = Directory.EnumerateFiles(modsFolderPath, DirectoryEnumerationOptions.Files, new DirectoryEnumerationFilters() {
					InclusionFilter = (f) =>
					{
						return Path.GetExtension(f.Extension).Equals(".pak", StringComparison.OrdinalIgnoreCase);
					}
				}, PathFormat.FullPath);
				Console.WriteLine("Mod Packages: " + modPaks.Count());
				foreach (var pakPath in modPaks)
				{
					using (var pr = new LSLib.LS.PackageReader(pakPath))
					{
						var pak = pr.Read();
						var metaFile = pak.Files.FirstOrDefault(pf => pf.Name.Contains("meta.lsx"));
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
										mods.Add(modData);
									}
								}
							}
						}
					}
				}
			}
#endif
			return mods;
		}
	}
}
