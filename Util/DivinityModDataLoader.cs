using DivinityModManager.Models;
using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
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
						//XmlDocument xDoc = new XmlDocument();
						//xDoc.LoadXml(str);
						XElement xDoc = XElement.Parse(str);
						var moduleInfoNode = xDoc.Descendants("node").FirstOrDefault(n => n.Attribute("id")?.Value == "ModuleInfo");
						//var moduleInfoNode = xDoc.SelectSingleNode("//node[@id='ModuleInfo']");
						if(moduleInfoNode != null)
						{
							DivinityModData modData = new DivinityModData()
							{
								UUID = GetAttribute(moduleInfoNode, "UUID", ""),
								Name = GetAttribute(moduleInfoNode, "Name", ""),
								Author = GetAttribute(moduleInfoNode, "Author", ""),
								Version = GetAttribute(moduleInfoNode, "Version", ""),
								Folder = GetAttribute(moduleInfoNode, "Folder", "")
							};
							Console.WriteLine($"Added mod {modData.Name} from {metaFile}");
							//var dependenciesNodes = xDoc.SelectNodes("//node[@id='ModuleShortDesc']");
							var dependenciesNodes = xDoc.Descendants("node").Where(n => n.Attribute("id")?.Value == "ModuleShortDesc");

							if (dependenciesNodes != null)
							{
								foreach(var node in dependenciesNodes)
								{
									DivinityModDependency dependencyMod = new DivinityModDependency()
									{
										UUID = GetAttribute(node, "UUID", ""),
										Name = GetAttribute(node, "Name", ""),
										Version = GetAttribute(node, "Version", "")
									};
									Console.WriteLine($"Added dependency to {modData.Name} - {dependencyMod.ToString()}");
									if(dependencyMod.UUID != "")
									{
										modData.Dependencies.Add(dependencyMod);
									}
								}
							}

							projects.Add(modData);
						}
					}
				}
			}
			return projects;
		}
	}
}
