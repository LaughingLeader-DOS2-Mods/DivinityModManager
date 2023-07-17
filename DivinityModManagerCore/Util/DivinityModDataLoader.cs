﻿using Alphaleonis.Win32.Filesystem;

using DivinityModManager.Extensions;
using DivinityModManager.Models;

using DynamicData;

using LSLib.LS;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace DivinityModManager.Util
{
	public static class DivinityModDataLoader
	{
		private static readonly StringComparison SCOMP = StringComparison.OrdinalIgnoreCase;
		private static readonly string[] LarianFileTypes = new string[4] { ".lsb", ".lsf", ".lsx", ".lsj" };

		public static int HEADER_MAJOR = 3;
		public static int HEADER_MINOR = 6;
		public static int HEADER_REVISION = 1;
		public static int HEADER_BUILD = 5;

		public static bool IgnoreMod(string modUUID)
		{
			return DivinityApp.IgnoredMods.Any(m => m.UUID == modUUID);
		}

		public static bool IgnoreModDependency(string modUUID)
		{
			return DivinityApp.IgnoredDependencyMods.Any(m => m.UUID == modUUID) || IgnoreMod(modUUID);
		}

		public static bool IgnoreModByFolder(string folder)
		{
			return DivinityApp.IgnoredMods.Any(m => m.Folder.Equals(Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar)), SCOMP));
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
		private static string GetAttributeWithId(XElement node, string id, string fallbackValue = "")
		{
			var att = node.Descendants("attribute").FirstOrDefault(a => a.Attribute("id")?.Value == id)?.Attribute("value")?.Value;
			if (att != null)
			{
				return att;
			}
			return fallbackValue;
		}

		private static string GetAttributeWithId(XElement node, string[] ids, string fallbackValue = "")
		{
			foreach (var id in ids)
			{
				var att = node.Descendants("attribute").FirstOrDefault(a => a.Attribute("id")?.Value == id)?.Attribute("value")?.Value;
				if (att != null)
				{
					return att;
				}
			}
			return fallbackValue;
		}

		private static bool TryGetAttribute(XElement node, string id, out string value, string fallbackValue = "")
		{
			var att = node.Attributes().FirstOrDefault(a => a.Name == id);
			if (att != null)
			{
				value = att.Value;
				return true;
			}
			value = fallbackValue;
			return false;
		}

		private static int SafeConvertString(string str)
		{
			if (!String.IsNullOrWhiteSpace(str) && int.TryParse(str, out int val))
			{
				return val;
			}
			return -1;
		}

		public static string EscapeXml(string s)
		{
			string toxml = s;
			if (!String.IsNullOrEmpty(toxml))
			{
				// replace literal values with entities
				toxml = toxml.Replace("&", "&amp;");
				toxml = toxml.Replace("'", "&apos;");
				toxml = toxml.Replace("\"", "&quot;");
				toxml = toxml.Replace(">", "&gt;");
				toxml = toxml.Replace("<", "&lt;");
			}
			return toxml;
		}

		public static string EscapeXmlAttributes(string xmlstring)
		{
			if (!String.IsNullOrEmpty(xmlstring))
			{
				xmlstring = Regex.Replace(xmlstring, "value=\"(.*?)\"", new MatchEvaluator((m) =>
				{
					return $"value=\"{EscapeXml(m.Groups[1].Value)}\"";
				}));
			}
			return xmlstring;
		}

		public static string UnescapeXml(string str)
		{
			if (!String.IsNullOrEmpty(str))
			{
				str = str.Replace("&amp;", "&");
				str = str.Replace("&apos;", "'");
				str = str.Replace("&quot;", "\"");
				str = str.Replace("&gt;", ">");
				str = str.Replace("&lt;", "<");
				str = str.Replace("<br>", Environment.NewLine);
			}
			return str;
		}

		private static DivinityModData ParseMetaFile(string metaContents, bool isBaseGameMod = false)
		{
			try
			{
				XElement xDoc = XElement.Parse(EscapeXmlAttributes(metaContents));
				var versionNode = xDoc.Descendants("version").FirstOrDefault();

				var headerMajor = HEADER_MAJOR;
				var headerMinor = HEADER_MINOR;
				var headerRevision = HEADER_REVISION;
				var headerBuild = HEADER_BUILD;

				if (versionNode != null)
				{
					//DivinityApp.LogMessage($"Version node: {versionNode.ToString()}");
					//DE Mods <version major="3" minor="6" revision="2" build="0" />
					//Classic Mods <version major="3" minor="1" revision="3" build="5" />
					if (TryGetAttribute(versionNode, "major", out var headerMajorStr))
					{
						int.TryParse(headerMajorStr, out headerMajor);
					}
					if (TryGetAttribute(versionNode, "minor", out var headerMinorStr))
					{
						int.TryParse(headerMinorStr, out headerMinor);
					}
					if (TryGetAttribute(versionNode, "revision", out var headerRevisionStr))
					{
						int.TryParse(headerRevisionStr, out headerRevision);
					}
					if (TryGetAttribute(versionNode, "build", out var headerBuildStr))
					{
						int.TryParse(headerBuildStr, out headerBuild);
					}

					//DivinityApp.LogMessage($"Version: {headerMajor}.{headerMinor}.{headerRevision}.{headerBuild}");
				}

				var moduleInfoNode = xDoc.Descendants("node").FirstOrDefault(n => n.Attribute("id")?.Value == "ModuleInfo");
				if (moduleInfoNode != null)
				{
					var uuid = GetAttributeWithId(moduleInfoNode, "UUID", "");
					var name = UnescapeXml(GetAttributeWithId(moduleInfoNode, "Name", ""));
					var description = UnescapeXml(GetAttributeWithId(moduleInfoNode, "Description", ""));
					var author = UnescapeXml(GetAttributeWithId(moduleInfoNode, "Author", ""));
					/*
					if (DivinityApp.MODS_GiftBag.Any(x => x.UUID == uuid))
					{
						name = UnescapeXml(GetAttributeWithId(moduleInfoNode, "DisplayName", name));
						description = UnescapeXml(GetAttributeWithId(moduleInfoNode, "DescriptionName", description));
						author = "Larian Studios";
					}
					*/
					DivinityModData modData = new DivinityModData(isBaseGameMod)
					{
						UUID = uuid,
						Name = name,
						Author = author,
						Version = DivinityModVersion.FromInt(SafeConvertString(GetAttributeWithId(moduleInfoNode, "Version", ""))),
						Folder = GetAttributeWithId(moduleInfoNode, "Folder", ""),
						Description = description,
						MD5 = GetAttributeWithId(moduleInfoNode, "MD5", ""),
						ModType = GetAttributeWithId(moduleInfoNode, "Type", ""),
						HeaderVersion = new DivinityModVersion(headerMajor, headerMinor, headerRevision, headerBuild)
					};
					var tagsText = GetAttributeWithId(moduleInfoNode, "Tags", "");
					if (!String.IsNullOrWhiteSpace(tagsText))
					{
						var tags = tagsText.Split(';');
						modData.AddTags(tags);
					}
					//var dependenciesNodes = xDoc.SelectNodes("//node[@id='ModuleShortDesc']");
					var dependenciesNodes = xDoc.Descendants("node").Where(n => n.Attribute("id")?.Value == "ModuleShortDesc");

					if (dependenciesNodes != null)
					{
						foreach (var node in dependenciesNodes)
						{
							DivinityModDependencyData dependencyMod = new DivinityModDependencyData()
							{
								UUID = GetAttributeWithId(node, "UUID", ""),
								Name = UnescapeXml(GetAttributeWithId(node, "Name", "")),
								Version = DivinityModVersion.FromInt(SafeConvertString(GetAttributeWithId(node, "Version", ""))),
								Folder = GetAttributeWithId(node, "Folder", ""),
								MD5 = GetAttributeWithId(node, "MD5", "")
							};
							//DivinityApp.LogMessage($"Added dependency to {modData.Name} - {dependencyMod.ToString()}");
							if (dependencyMod.UUID != "")
							{
								modData.Dependencies.Add(dependencyMod);
							}
						}
					}

					var publishVersionNode = moduleInfoNode.Descendants("node").Where(n => n.Attribute("id")?.Value == "PublishVersion").FirstOrDefault();
					if (publishVersionNode != null)
					{
						var publishVersion = DivinityModVersion.FromInt(SafeConvertString(GetAttributeWithId(publishVersionNode, "Version", "")));
						modData.PublishVersion = publishVersion;
						//DivinityApp.LogMessage($"{modData.Folder} PublishVersion is {publishVersion.Version}");
					}

					var targets = moduleInfoNode.Descendants("node").Where(n => n.Attribute("id")?.Value == "Target");
					if (targets != null)
					{
						foreach (var node in targets)
						{
							var target = GetAttributeWithId(node, "Object", "");
							if (!String.IsNullOrWhiteSpace(target))
							{
								modData.Modes.Add(target);
							}
						}
						modData.Targets = String.Join(", ", modData.Modes);
					}
					return modData;
				}
				else
				{
					DivinityApp.Log($"**[ERROR] ModuleInfo node not found for meta.lsx: {metaContents}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error parsing meta.lsx: {ex}");
			}
			return null;
		}

		//BOM
		private static readonly string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());

		private static System.IO.FileStream _GetAsyncStream(string filePath)
		{
			return new System.IO.FileStream(filePath,
					System.IO.FileMode.Open,
					System.IO.FileAccess.Read,
					System.IO.FileShare.Read,
					2048,
					System.IO.FileOptions.Asynchronous);
		}

		private static async Task<DivinityModData> LoadEditorProjectFolderAsync(string folder, CancellationToken token)
		{
			var metaFile = Path.Combine(folder, "meta.lsx");
			if (File.Exists(metaFile))
			{
				using (var fileStream = _GetAsyncStream(metaFile))
				{
					var result = new byte[fileStream.Length];
					await fileStream.ReadAsync(result, 0, (int)fileStream.Length, token);

					string str = Encoding.UTF8.GetString(result, 0, result.Length);

					if (!String.IsNullOrEmpty(str))
					{
						//XML parsing doesn't like the BOM for some reason
						if (str.StartsWith(_byteOrderMarkUtf8, StringComparison.Ordinal))
						{
							str = str.Remove(0, _byteOrderMarkUtf8.Length);
						}

						DivinityModData modData = ParseMetaFile(str);
						if (modData != null)
						{
							modData.IsEditorMod = true;
							modData.IsUserMod = true;
							modData.FilePath = folder;
							try
							{
								modData.LastModified = File.GetChangeTime(metaFile);
								modData.LastUpdated = modData.LastModified.Value;
							}
							catch (PlatformNotSupportedException ex)
							{
								DivinityApp.Log($"Error getting last modified date for '{metaFile}': {ex}");
							}

							var configFile = Path.Combine(folder, DivinityApp.EXTENDER_MOD_CONFIG);
							if (File.Exists(configFile))
							{
								var config = await LoadScriptExtenderConfigAsync(configFile);
								if (config != null)
								{
									modData.ScriptExtenderData = config;
									if (modData.ScriptExtenderData.RequiredExtensionVersion > -1) modData.HasScriptExtenderSettings = true;
								}
								else
								{
									DivinityApp.Log($"Failed to parse {DivinityApp.EXTENDER_MOD_CONFIG} for '{folder}'.");
								}
							}

							return modData;
						}
					}
				}
			}
			return null;
		}

		public static async Task<List<DivinityModData>> LoadEditorProjectsAsync(string modsFolderPath, CancellationToken token)
		{
			var projects = new ConcurrentBag<DivinityModData>();

			try
			{
				if (Directory.Exists(modsFolderPath))
				{
					var projectDirectories = Directory.EnumerateDirectories(modsFolderPath);
					var filteredFolders = projectDirectories.Where(f => !IgnoreModByFolder(f));
					Console.WriteLine($"Project Folders: {filteredFolders.Count()} / {projectDirectories.Count()}");

					async Task AwaitPartition(IEnumerator<string> partition)
					{
						using (partition)
						{
							while (partition.MoveNext())
							{
								if (token.IsCancellationRequested) return;
								await Task.Yield(); // prevents a sync/hot thread hangup
								var modData = await LoadEditorProjectFolderAsync(partition.Current, token);
								if (modData != null)
								{
									projects.Add(modData);
								}
							}
						}
					}

					var currentTime = DateTime.Now;
					var partitionAmount = Environment.ProcessorCount;
					await Task.WhenAll(Partitioner.Create(filteredFolders).GetPartitions(partitionAmount).AsParallel().Select(p => AwaitPartition(p)));
					DivinityApp.Log($"Took {DateTime.Now - currentTime:s\\.ff} seconds(s) to load editor mods.");
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error loading mod projects: {ex}");
			}
			return projects.ToList();
		}

		private static HashSet<string> _AllPaksNames = new HashSet<string>();

		private static Regex multiPartPakPattern = new Regex("_[0-9]+.pak", RegexOptions.IgnoreCase | RegexOptions.Singleline);
		private static Regex multiPartPakPatternNoExtension = new Regex("(_[0-9]+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

		private static bool PakIsNotPartial(string path)
		{
			var baseName = Path.GetFileNameWithoutExtension(path);
			var match = multiPartPakPatternNoExtension.Match(baseName);
			if (match.Success)
			{
				var nameWithoutPartial = baseName.Replace(match.Groups[0].Value, "");
				if (_AllPaksNames.Contains(nameWithoutPartial))
				{
					DivinityApp.Log($"Pak ({baseName}) is a partial pak for ({nameWithoutPartial}). Skipping.");
					return false;
				}
			}
			return true;
		}

		private static readonly Regex modMetaPattern = new Regex("^Mods/([^/]+)/meta.lsx", RegexOptions.IgnoreCase);
		private static bool IsModMetaFile(AbstractFileInfo f)
		{
			if (Path.GetFileName(f.Name).Equals("meta.lsx", SCOMP))
			{
				return modMetaPattern.IsMatch(f.Name);
			}
			return false;
		}

		private static Regex _ModFolderPattern = new Regex("^(Mods|Public)/(.+?)/.+$");
		private static string[] _IgnoredRecursiveFolders = new string[]
		{
			"DefEd\\Data",
			"Divinity Original Sin 2\\bin",
			"Localization",
		};

		private static async Task<DivinityModData> LoadModDataFromPakAsync(string pakPath, Dictionary<string, DivinityModData> builtinMods)
		{
			using (var pr = new LSLib.LS.PackageReader(pakPath))
			{
				DivinityModData modData = null;

				string pakName = Path.GetFileNameWithoutExtension(pakPath);

				var pak = pr.Read();

				var metaFiles = new List<AbstractFileInfo>();
				var hasBuiltinDirectory = false;
				var builtinModOverrides = new Dictionary<string, DivinityModData>();

				AbstractFileInfo configInfo = null;

				if (pak != null && pak.Files != null)
				{
					for (int i = 0; i < pak.Files.Count; i++)
					{
						var f = pak.Files[i];
						if(f.Name.Contains(DivinityApp.EXTENDER_MOD_CONFIG))
						{
							configInfo = f;
						}
						if (IsModMetaFile(f))
						{
							metaFiles.Add(f);
						}
						else if (!hasBuiltinDirectory)
						{
							var modFolderMatch = _ModFolderPattern.Match(f.Name);
							if (modFolderMatch.Success)
							{
								var modFolder = Path.GetFileName(modFolderMatch.Groups[2].Value.TrimEnd(Path.DirectorySeparatorChar));
								if (!builtinModOverrides.ContainsKey(modFolder) && builtinMods.TryGetValue(modFolder, out var builtinMod))
								{
									hasBuiltinDirectory = true;
									builtinModOverrides[builtinMod.Folder] = builtinMod;
									DivinityApp.Log($"Found a mod overriding a builtin directory. Pak({pakName}) Folder({modFolder}) File({f.Name}");
								}
							}
						}
					}
				}

				AbstractFileInfo metaFile = null;
				for (int i = 0; i < metaFiles.Count; i++)
				{
					var f = metaFiles[i];
					if (metaFile == null)
					{
						metaFile = f;
					}	
					else
					{
						var parentDir = Directory.GetParent(f.Name);
						// A pak may have multiple meta.lsx files for overriding NumPlayers or something. Match against the pak name in that case.
						if (pakName.Contains(parentDir.Name))
						{
							metaFile = f;
							break;
						}
					}
				}

				if (metaFile != null)
				{
					//DivinityApp.LogMessage($"Parsing meta.lsx for mod pak '{pakPath}'.");
					using (var stream = metaFile.MakeStream())
					{
						using (var sr = new System.IO.StreamReader(stream))
						{
							string text = await sr.ReadToEndAsync();
							modData = ParseMetaFile(text);
						}
					}

					if (modData != null)
					{
						modData.HasBuiltinOverride = hasBuiltinDirectory;
						if (hasBuiltinDirectory)
						{
							modData.BuiltinOverrideModsText = String.Join(Environment.NewLine, builtinModOverrides.Values.OrderBy(x => x.Name).Select(x => $"{x.Folder} ({x.Name})"));
							modData.IsForcedLoaded = true;
						}
						modData.FilePath = pakPath;
						try
						{
							modData.LastModified = File.GetChangeTime(pakPath);
							modData.LastUpdated = modData.LastModified.Value;
						}
						catch (PlatformNotSupportedException ex)
						{
							DivinityApp.Log($"Error getting pak last modified date for '{pakPath}': {ex}");
						}

						modData.IsUserMod = true;

						if (configInfo != null)
						{
							var config = await LoadScriptExtenderConfigAsync(configInfo);
							if (config != null)
							{
								modData.ScriptExtenderData = config;
								if (modData.ScriptExtenderData.RequiredExtensionVersion > -1) modData.HasScriptExtenderSettings = true;
							}
							else
							{
								DivinityApp.Log($"Failed to parse {configInfo.Name} for '{pakPath}'.");
							}
						}

						//DivinityApp.Log($"Loaded mod '{modData.Name}'.");
						return modData;
					}
					else
					{
						DivinityApp.Log($"Error: Failed to parse meta.lsx for mod pak '{pakPath}'.");
					}
				}
				else
				{
					DivinityApp.Log($"Error: No meta.lsx for mod pak '{pakPath}'.");
				}
			}

			return null;
		}

		private static async Task<DivinityModData> LoadModDataFromPakAsync(string pakPath, Dictionary<string, DivinityModData> builtinMods, CancellationToken cts)
		{
			try
			{
				while (!cts.IsCancellationRequested)
				{
					return await LoadModDataFromPakAsync(pakPath, builtinMods);
				}
			}
			catch(Exception ex)
			{
				DivinityApp.Log($"Error loading mod pak '{pakPath}':\n{ex}");
			}
			return null;
		}

		public static async Task<List<DivinityModData>> LoadModPackageDataAsync(string modsFolderPath, CancellationToken cts)
		{
			var builtinMods = DivinityApp.IgnoredMods.ToDictionary(x => x.Folder, x => x);

			var modPaks = new List<string>();
			try
			{
				var dirOptions = DirectoryEnumerationOptions.Files | DirectoryEnumerationOptions.Recursive;
				var allPaks = Directory.EnumerateFiles(modsFolderPath, dirOptions,
				new DirectoryEnumerationFilters()
				{
					InclusionFilter = (f) => Path.GetExtension(f.Extension).Equals(".pak", SCOMP),
					RecursionFilter = (f) => !_IgnoredRecursiveFolders.Any(x => f.FullPath.Contains(x))
				});
				_AllPaksNames.UnionWith(allPaks.Select(p => Path.GetFileNameWithoutExtension(p)));
				modPaks.AddRange(allPaks.Where(PakIsNotPartial));
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error enumerating pak folder '{modsFolderPath}': {ex}");
			}

			DivinityApp.Log($"Mod Packages: {modPaks.Count()}");

			var loadedMods = new ConcurrentBag<DivinityModData>();

			async Task AwaitPartition(IEnumerator<string> partition)
			{
				using (partition)
				{
					while (partition.MoveNext())
					{
						if (cts.IsCancellationRequested) return;
						await Task.Yield(); // prevents a sync/hot thread hangup
						var modData = await LoadModDataFromPakAsync(partition.Current, builtinMods, cts);
						if(modData != null)
						{
							loadedMods.Add(modData);
						}
					}
				}
			}

			var partitionAmount = Environment.ProcessorCount;
			var currentTime = DateTime.Now;
			DivinityApp.Log($"Split mod loading into {partitionAmount} partitions.");
			await Task.WhenAll(Partitioner.Create(modPaks).GetPartitions(partitionAmount).AsParallel().Select(p => AwaitPartition(p)));

			DivinityApp.Log($"Took {DateTime.Now - currentTime:s\\.ff} second(s) to load mod paks.");
			return loadedMods.ToList();
		}

		private static string GetNodeAttribute(Node node, string key, string defaultValue)
		{
			if (node.Attributes.TryGetValue(key, out var att))
			{
				if (att.Type == NodeAttribute.DataType.DT_TranslatedString)
				{
					TranslatedString ts = (TranslatedString)att.Value;
					return ts.Value;
				}
				return att.Value.ToString();
			}
			return defaultValue;
		}

		private static T GetNodeAttribute<T>(Node node, string key, T defaultValue)
		{
			if (node.Attributes.TryGetValue(key, out var att))
			{
				return (T)Convert.ChangeType(att.Value, typeof(T));
			}
			return defaultValue;
		}

		private static DivinityGameMasterCampaign ParseGameMasterCampaignMetaFile(Resource meta)
		{
			try
			{
				var headerMajor = HEADER_MAJOR;
				var headerMinor = HEADER_MINOR;
				var headerRevision = HEADER_REVISION;
				var headerBuild = HEADER_BUILD;

				int.TryParse(meta.Metadata.MajorVersion.ToString(), out headerMajor);
				int.TryParse(meta.Metadata.MinorVersion.ToString(), out headerMinor);
				int.TryParse(meta.Metadata.Revision.ToString(), out headerRevision);
				int.TryParse(meta.Metadata.BuildNumber.ToString(), out headerBuild);

				if (meta.TryFindNode("ModuleInfo", out var moduleInfoNode))
				{
					var uuid = moduleInfoNode.Attributes.First(x => x.Key == "UUID").Value.Value.ToString();
					var name = moduleInfoNode.Attributes.First(x => x.Key == "Name").Value.Value.ToString();
					var description = moduleInfoNode.Attributes.First(x => x.Key == "Description").Value.Value.ToString();
					var author = moduleInfoNode.Attributes.First(x => x.Key == "Author").Value.Value.ToString();
					/*
					if (DivinityApp.MODS_GiftBag.Any(x => x.UUID == uuid))
					{
						name = UnescapeXml(GetAttributeWithId(moduleInfoNode, "DisplayName", name));
						description = UnescapeXml(GetAttributeWithId(moduleInfoNode, "DescriptionName", description));
						author = "Larian Studios";
					}
					*/

					DivinityGameMasterCampaign data = new DivinityGameMasterCampaign()
					{
						UUID = GetNodeAttribute(moduleInfoNode, "UUID", ""),
						Name = GetNodeAttribute(moduleInfoNode, "Name", ""),
						Author = GetNodeAttribute(moduleInfoNode, "Author", ""),
						Version = DivinityModVersion.FromInt(SafeConvertString(GetNodeAttribute(moduleInfoNode, "Version", ""))),
						Folder = GetNodeAttribute(moduleInfoNode, "Folder", ""),
						Description = GetNodeAttribute(moduleInfoNode, "Description", ""),
						MD5 = GetNodeAttribute(moduleInfoNode, "MD5", ""),
						HeaderVersion = new DivinityModVersion(headerMajor, headerMinor, headerRevision, headerBuild)
					};
					var tagsText = GetNodeAttribute(moduleInfoNode, "Tags", "");
					if (!String.IsNullOrWhiteSpace(tagsText))
					{
						var tags = tagsText.Split(';');
						data.AddTags(tags);
					}

					if (meta.TryFindNode("Dependencies", out var dependenciesNode))
					{
						foreach (var container in dependenciesNode.Children)
						{
							foreach (var node in container.Value)
							{
								DivinityModDependencyData dependencyMod = new DivinityModDependencyData()
								{
									UUID = GetNodeAttribute(node, "UUID", ""),
									Name = GetNodeAttribute(node, "Name", ""),
									Version = DivinityModVersion.FromInt(SafeConvertString(GetNodeAttribute(node, "Version", ""))),
									Folder = GetNodeAttribute(node, "Folder", ""),
									MD5 = GetNodeAttribute(node, "MD5", "")
								};
								//DivinityApp.LogMessage($"Added dependency to {modData.Name} - {dependencyMod.ToString()}");
								if (dependencyMod.UUID != "")
								{
									data.Dependencies.Add(dependencyMod);
								}
							}
						}
					}

					if (moduleInfoNode.Children.TryGetValue("PublishVersion", out var publishVersionList))
					{
						var publishVersion = DivinityModVersion.FromInt(SafeConvertString(GetNodeAttribute(publishVersionList.First(), "Version", "")));
						data.PublishVersion = publishVersion;
					}

					//if (moduleInfoNode.Children.TryGetValue("TargetModes", out var targetModesList))
					//{
					//	foreach (var node in targetModesList)
					//	{
					//		var target = GetNodeAttribute(node, "Object", "");
					//		if (!String.IsNullOrEmpty(target))
					//		{
					//			data.Modes.Add(target);
					//		}
					//	}
					//	data.Targets = string.Join(", ", data.Modes);
					//}

					data.MetaResource = meta;

					return data;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error parsing meta.lsx: {ex}");
			}
			return null;
		}

		private static bool CanProcessGMMetaFile(FileSystemEntryInfo f)
		{
			return f.FileName.Equals("meta.lsf", SCOMP);
		}

		private static DirectoryEnumerationFilters GMMetaFilters = new DirectoryEnumerationFilters()
		{
			InclusionFilter = CanProcessGMMetaFile
		};

		public static List<DivinityGameMasterCampaign> LoadGameMasterData(string folderPath, CancellationToken? token = null)
		{
			List<DivinityGameMasterCampaign> campaignEntries = new List<DivinityGameMasterCampaign>();

			if (Directory.Exists(folderPath))
			{
				List<string> campaignMetaFiles = new List<string>();
				try
				{
					var files = Directory.EnumerateFiles(folderPath, DirectoryEnumerationOptions.Files | DirectoryEnumerationOptions.Recursive, GMMetaFilters);
					if (files != null)
					{
						campaignMetaFiles.AddRange(files);
					}
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error enumerating GM campaigns folder '{folderPath}':\n{ex}");
				}

				DivinityApp.Log("Campaign meta files: " + campaignMetaFiles.Count());
				foreach (var metaPath in campaignMetaFiles)
				{
					try
					{
						if (token.HasValue && token.Value.IsCancellationRequested)
						{
							return campaignEntries;
						}
						DivinityGameMasterCampaign campaignData = null;
						using (var fileStream = new System.IO.FileStream(metaPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read, 4096, true))
						{
							//var result = new byte[fileStream.Length];
							//await fileStream.ReadAsync(result, 0, (int)fileStream.Length);
							using (var reader = new LSFReader(fileStream))
							{
								campaignData = ParseGameMasterCampaignMetaFile(reader.Read());
							}
						}

						if (campaignData != null)
						{
							campaignData.FilePath = metaPath;
							try
							{
								campaignData.LastModified = File.GetChangeTime(metaPath);
							}
							catch (PlatformNotSupportedException ex)
							{
								DivinityApp.Log($"Error getting pak last modified date for '{metaPath}':\n{ex}");
							}
							campaignEntries.Add(campaignData);
						}
					}
					catch (Exception ex)
					{
						DivinityApp.Log($"Error loading meta file '{metaPath}':\n{ex}");
					}
				}
			}
			return campaignEntries;
		}

		private static Node FindResourceNode(Node node, string attribute, string matchVal)
		{
			if (node.Attributes.TryGetValue(attribute, out var att))
			{
				var attVal = (string)att.Value;
				if (attVal.Equals(matchVal, SCOMP))
				{
					return node;
				}
			}
			foreach (var nList in node.Children.Values)
			{
				foreach (var n in nList)
				{
					var match = FindResourceNode(n, attribute, matchVal);
					if (match != null) return match;
				}
			}
			return null;
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

					//Console.WriteLine($"Folder: {Path.GetFileName(folder)} Blacklisted: {IgnoredMods.Any(m => Path.GetFileName(folder).Equals(m.Folder, SCOMP))}");
					var profileFile = GetProfileFile(folder);
					if (profileFile != null)
					{
						try
						{
							var profileRes = ResourceUtils.LoadResource(profileFile.FullName, LSLib.LS.Enums.ResourceFormat.LSB);
							if (profileRes != null && profileRes.Regions.TryGetValue("PlayerProfile", out var region))
							{
								if (region.Attributes.TryGetValue("PlayerProfileDisplayName", out var profileDisplayNameAtt))
								{
									string storedName = (string)profileDisplayNameAtt.Value;
									if (!String.IsNullOrEmpty(storedName))
									{
										storedDisplayedName = storedName;
									}
								}
								if (region.Attributes.TryGetValue("PlayerProfileID", out var profileIdAtt))
								{
									profileUUID = (string)profileIdAtt.Value;
								}
							}
						}
						catch (Exception ex)
						{
							DivinityApp.Log($"Error parsing profile data: \n{ex}");
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
					if (File.Exists(modSettingsFile))
					{
						Resource modSettingsRes = null;
						try
						{
							modSettingsRes = ResourceUtils.LoadResource(modSettingsFile, LSLib.LS.Enums.ResourceFormat.LSX);
						}
						catch (Exception ex)
						{
							DivinityApp.Log($"Error reading '{modSettingsFile}': '{ex}'");
						}

						if (modSettingsRes != null && modSettingsRes.Regions.TryGetValue("ModuleSettings", out var region))
						{
							if (region.Children.TryGetValue("ModOrder", out var modOrderRootNode))
							{
								var modOrderChildrenRoot = modOrderRootNode.FirstOrDefault();
								if (modOrderChildrenRoot != null)
								{
									var modOrder = modOrderChildrenRoot.Children.Values.FirstOrDefault();
									if (modOrder != null)
									{
										foreach (var c in modOrder)
										{
											if (c.Attributes.TryGetValue("UUID", out var attribute))
											{
												var uuid = (string)attribute.Value;
												if (!String.IsNullOrEmpty(uuid))
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
											var activeModData = new DivinityProfileActiveModData();
											activeModData.LoadFromAttributes(c.Attributes);
											if (!DivinityModDataLoader.IgnoreMod(activeModData.UUID))
											{
												profileData.ActiveMods.Add(activeModData);
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

		public static async Task<List<DivinityProfileData>> LoadProfileDataAsync(string profilePath)
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

					//Console.WriteLine($"Folder: {Path.GetFileName(folder)} Blacklisted: {IgnoredMods.Any(m => Path.GetFileName(folder).Equals(m.Folder, SCOMP))}");
					var profileFile = Path.Combine(folder, "profile.lsb");
					if (File.Exists(profileFile))
					{
						var profileRes = await LoadResourceAsync(profileFile, LSLib.LS.Enums.ResourceFormat.LSB);
						if (profileRes != null && profileRes.Regions.TryGetValue("PlayerProfile", out var region))
						{
							if (region.Attributes.TryGetValue("PlayerProfileDisplayName", out var profileDisplayNameAtt))
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
					if (File.Exists(modSettingsFile))
					{
						Resource modSettingsRes = null;
						try
						{
							modSettingsRes = await LoadResourceAsync(modSettingsFile, LSLib.LS.Enums.ResourceFormat.LSX);
						}
						catch (Exception ex)
						{
							DivinityApp.Log($"Error reading '{modSettingsFile}': '{ex}'");
						}

						if (modSettingsRes != null && modSettingsRes.Regions.TryGetValue("ModuleSettings", out var region))
						{
							if (region.Children.TryGetValue("ModOrder", out var modOrderRootNode))
							{
								var modOrderChildrenRoot = modOrderRootNode.FirstOrDefault();
								if (modOrderChildrenRoot != null)
								{
									var modOrder = modOrderChildrenRoot.Children.Values.FirstOrDefault();
									if (modOrder != null)
									{
										foreach (var c in modOrder)
										{
											if (c.Attributes.TryGetValue("UUID", out var attribute))
											{
												var uuid = (string)attribute.Value;
												if (!String.IsNullOrEmpty(uuid))
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
											var activeModData = new DivinityProfileActiveModData();
											activeModData.LoadFromAttributes(c.Attributes);
											if (!DivinityModDataLoader.IgnoreMod(activeModData.UUID))
											{
												profileData.ActiveMods.Add(activeModData);
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

		public static async Task<Resource> LoadResourceAsync(string path, LSLib.LS.Enums.ResourceFormat resourceFormat)
		{
			return await Task.Run(() =>
			{
				try
				{
					var resource = LSLib.LS.ResourceUtils.LoadResource(path, resourceFormat);
					return resource;
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error loading '{path}': {ex}");
					return null;
				}
			});
		}

		public static async Task<Resource> LoadResourceAsync(System.IO.Stream stream, LSLib.LS.Enums.ResourceFormat resourceFormat)
		{
			return await Task.Run(() =>
			{
				try
				{
					var resource = LSLib.LS.ResourceUtils.LoadResource(stream, resourceFormat);
					return resource;
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error loading resource: {ex}");
					return null;
				}
			});
		}

		private static FileInfo GetProfileFile(string path)
		{
			var files = Directory.EnumerateFiles(path, DirectoryEnumerationOptions.Files, new DirectoryEnumerationFilters()
			{
				InclusionFilter = (f) =>
				{
					if (f.FileName.IndexOf("profile", SCOMP) > -1 && LarianFileTypes.Any(e => e.Equals(f.Extension, SCOMP)))
					{
						return true;
					}
					return false;
				}
			}).Select(x => new FileInfo(x)).OrderBy(x => x.LastWriteTime).ToList();
			return files.FirstOrDefault();
		}

		private static FileInfo GetPlayerProfilesFile(string path)
		{
			var files = Directory.EnumerateFiles(path, DirectoryEnumerationOptions.Files, new DirectoryEnumerationFilters()
			{
				InclusionFilter = (f) =>
				{
					if (f.FileName.IndexOf("playerprofiles", SCOMP) > -1 && LarianFileTypes.Any(e => e.Equals(f.Extension, SCOMP)))
					{
						return true;
					}
					return false;
				}
			}).Select(x => new FileInfo(x)).OrderBy(x => x.LastWriteTime).ToList();
			return files.FirstOrDefault();
		}

		public static string GetSelectedProfileUUID(string profilePath)
		{

			FileInfo playerprofilesFile = GetPlayerProfilesFile(profilePath);
			string activeProfileUUID = "";
			if (playerprofilesFile != null)
			{
				try
				{
					DivinityApp.Log($"Loading playerprofiles at '{playerprofilesFile.FullName}'");
					var res = ResourceUtils.LoadResource(playerprofilesFile.FullName);
					if (res != null && res.Regions.TryGetValue("UserProfiles", out var region))
					{
						if (region.Attributes.TryGetValue("ActiveProfile", out var att))
						{
							DivinityApp.Log($"ActiveProfile | '{att.Type} {att.Value}'");
							activeProfileUUID = (string)att.Value;
						}
					}
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error loading {playerprofilesFile}: {ex}");
				}
			}
			return activeProfileUUID;
		}

		public static bool ExportedSelectedProfile(string profilePath, string profileUUID)
		{
			var conversionParams = ResourceConversionParameters.FromGameVersion(DivinityApp.GAME);
			var playerprofilesFile = Path.Combine(profilePath, "playerprofiles.lsb");
			if (File.Exists(playerprofilesFile))
			{
				try
				{
					var res = ResourceUtils.LoadResource(playerprofilesFile, LSLib.LS.Enums.ResourceFormat.LSB);
					if (res != null && res.Regions.TryGetValue("UserProfiles", out var region))
					{
						if (region.Attributes.TryGetValue("ActiveProfile", out var att))
						{
							att.Value = profileUUID;
							ResourceUtils.SaveResource(res, playerprofilesFile, LSLib.LS.Enums.ResourceFormat.LSB, conversionParams);
							return true;
						}
					}
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error saving {playerprofilesFile}: {ex}");
				}
			}
			else
			{
				DivinityApp.Log($"[*WARNING*] '{playerprofilesFile}' does not exist. Skipping selected profile saving.");
			}
			return false;
		}

		public static async Task<string> GetSelectedProfileUUIDAsync(string profilePath)
		{
			var playerprofilesFile = Path.Combine(profilePath, "playerprofiles.lsb");
			string activeProfileUUID = "";
			if (File.Exists(playerprofilesFile))
			{
				DivinityApp.Log($"Loading playerprofiles.lsb at '{playerprofilesFile}'");
				var res = await LoadResourceAsync(playerprofilesFile, LSLib.LS.Enums.ResourceFormat.LSB);
				if (res != null && res.Regions.TryGetValue("UserProfiles", out var region))
				{
					//DivinityApp.LogMessage($"ActiveProfile | Getting root node '{String.Join(";", region.Attributes.Keys)}'");

					if (region.Attributes.TryGetValue("ActiveProfile", out var att))
					{
						DivinityApp.Log($"ActiveProfile | '{att.Value}'");
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

			order.FilePath = outputFilePath;

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

			order.FilePath = outputFilePath;

			return true;
		}

		public static List<DivinityLoadOrder> FindLoadOrderFilesInDirectory(string directory)
		{
			List<DivinityLoadOrder> loadOrders = new List<DivinityLoadOrder>();

			if (Directory.Exists(directory))
			{
				var files = Directory.EnumerateFiles(directory, DirectoryEnumerationOptions.Files | DirectoryEnumerationOptions.Recursive, new DirectoryEnumerationFilters()
				{
					InclusionFilter = (f) =>
					{
						return f.Extension.Equals(".json", SCOMP);
					}
				});

				foreach (var loadOrderFile in files)
				{
					try
					{
						var fileText = File.ReadAllText(loadOrderFile);
						DivinityLoadOrder order = DivinityJsonUtils.SafeDeserialize<DivinityLoadOrder>(fileText);
						if (order != null)
						{
							order.FilePath = loadOrderFile;
							order.LastModifiedDate = File.GetLastWriteTime(loadOrderFile);
							loadOrders.Add(order);
						}
					}
					catch (Exception ex)
					{
						DivinityApp.Log($"Failed to read '{loadOrderFile}': {ex}");
					}
				}
			}

			return loadOrders;
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
						return f.Extension.Equals(".json", SCOMP) && !f.FileName.Equals("settings.json", SCOMP);
					}
				});

				foreach (var loadOrderFile in files)
				{
					try
					{
						using (var reader = File.OpenText(loadOrderFile))
						{
							var fileText = await reader.ReadToEndAsync();

							DivinityLoadOrder order = DivinityJsonUtils.SafeDeserialize<DivinityLoadOrder>(fileText);
							order.Name = Path.GetFileNameWithoutExtension(loadOrderFile);
							if (order != null)
							{
								order.FilePath = loadOrderFile;
								order.LastModifiedDate = File.GetLastWriteTime(loadOrderFile);

								loadOrders.Add(order);
							}
						}
					}
					catch (Exception ex)
					{
						DivinityApp.Log($"Failed to read '{loadOrderFile}': {ex}");
					}
				}
			}

			return loadOrders;
		}
		public static async Task<DivinityLoadOrder> LoadOrderFromFileAsync(string loadOrderFile)
		{
			if (File.Exists(loadOrderFile))
			{
				try
				{
					using (var reader = File.OpenText(loadOrderFile))
					{
						var fileText = await reader.ReadToEndAsync();
						DivinityLoadOrder order = DivinityJsonUtils.SafeDeserialize<DivinityLoadOrder>(fileText);
						if (order != null)
						{
							order.FilePath = loadOrderFile;
						}
						return order;
					}
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error loading '{loadOrderFile}': {ex}");
				}
			}
			return null;
		}

		public static DivinityLoadOrder LoadOrderFromFile(string loadOrderFile, IEnumerable<DivinityModData> allMods)
		{
			var ext = Path.GetExtension(loadOrderFile).ToLower();
			DivinityLoadOrder order = null;
			switch (ext)
			{
				case ".json":
					if (DivinityJsonUtils.TrySafeDeserializeFromPath<DivinityLoadOrder>(loadOrderFile, out var savedOrder))
					{
						return savedOrder;
					}
					else
					{
						if (DivinityJsonUtils.TrySafeDeserializeFromPath<List<DivinitySerializedModData>>(loadOrderFile, out var exportedOrder))
						{
							order = new DivinityLoadOrder();
							order.IsDecipheredOrder = true;
							order.AddRange(exportedOrder);
							DivinityApp.Log(String.Join("\n", order.Order.Select(x => x.UUID)));
							var modGUIDs = allMods.Select(x => x.UUID).ToHashSet();
							foreach (var entry in order.Order)
							{
								if(!modGUIDs.Contains(entry.UUID))
								{
									entry.Missing = true;
								}
							}
							order.Name = Path.GetFileNameWithoutExtension(loadOrderFile);
							return order;
						}
					}
					break;
				case ".txt":
					var textPattern = new Regex(@"\((\S+\.pak)\)", RegexOptions.IgnoreCase);
					var textLines = File.ReadAllLines(loadOrderFile);
					order = new DivinityLoadOrder();
					foreach (var line in textLines)
					{
						var match = textPattern.Match(line);
						if(match.Success)
						{
							var pakName = Path.GetFileName(match.Groups[1].Value.Trim());
							var mod = allMods.FirstOrDefault(x => x.PakEquals(pakName, SCOMP));
							if(mod != null)
							{
								order.Add(mod);
							}
							else
							{
								order.Order.Add(new DivinityLoadOrderEntry
								{
									Missing = true,
									Name = pakName,
									UUID = "",
								});
							}
						}
					}
					break;
				case ".tsv":
					var tsvLines = File.ReadAllLines(loadOrderFile);
					var header = tsvLines[0].Split('\t');
					var fileIndex = header.IndexOf("FileName");
					var nameIndex = header.IndexOf("Name");
					var urlIndex = header.IndexOf("URL");
					if (fileIndex > -1)
					{
						order = new DivinityLoadOrder();
						for(var i = 1; i < tsvLines.Length; i++)
						{
							var lineData = tsvLines[i].Split('\t');
							if (lineData.Length > fileIndex)
							{
								var fileName = Path.GetFileName(lineData[fileIndex].Trim());
								var mod = allMods.FirstOrDefault(x => x.PakEquals(fileName, SCOMP));
								if (mod != null)
								{
									order.Add(mod);
								}
								else
								{
									var name = fileName;
									if (nameIndex > -1)
									{
										name = lineData[nameIndex];
									}
									if (urlIndex > -1 && lineData.Length > urlIndex)
									{
										name = $"{name} {lineData[urlIndex]}";
									}
									order.Order.Add(new DivinityLoadOrderEntry
									{
										Missing = true,
										Name = name,
										UUID = "",
									});
								}
							}
						}
					}
					break;
			}
			if(order != null)
			{
				order.IsDecipheredOrder = true;
				order.Name = Path.GetFileNameWithoutExtension(loadOrderFile);
			}
			return order;
		}

		public static async Task<bool> ExportModSettingsToFileAsync(string folder, IEnumerable<DivinityModData> order)
		{
			if (Directory.Exists(folder))
			{
				string outputFilePath = Path.Combine(folder, "modsettings.lsx");
				string contents = GenerateModSettingsFile(order);
				try
				{
					//Lazy indentation!
					var xml = new XmlDocument();
					xml.LoadXml(contents);
					using (var sw = new System.IO.StringWriter())
					{
						using (var xw = new XmlTextWriter(sw))
						{
							xw.Formatting = System.Xml.Formatting.Indented;
							xw.Indentation = 2;
							xml.WriteTo(xw);
						}

						var buffer = Encoding.UTF8.GetBytes(sw.ToString());
						using (var fs = new System.IO.FileStream(outputFilePath, System.IO.FileMode.Create,
							System.IO.FileAccess.Write, System.IO.FileShare.None, buffer.Length, true))
						{
							await fs.WriteAsync(buffer, 0, buffer.Length);
						}
					}

					return true;
				}
				catch (AccessViolationException ex)
				{
					DivinityApp.Log($"Failed to write file '{outputFilePath}': {ex}");
				}
			}
			return false;
		}

		public static List<DivinityModData> GetDependencyMods(DivinityModData mod, IEnumerable<DivinityModData> allMods, IEnumerable<DivinityLoadOrderEntry> order)
		{
			List<DivinityModData> mods = new List<DivinityModData>();
			//var dependencies = mod.Dependencies.Items.Where(x => (!order.Any(y => y.UUID == x.UUID) && !IgnoreMod(x.UUID)));
			foreach (var d in mod.Dependencies.Items.Where(x => !IgnoreModDependency(x.UUID)))
			{
				var dependencyModData = allMods.FirstOrDefault(x => x.UUID == d.UUID);
				if (dependencyModData != null)
				{
					var dependencyMods = GetDependencyMods(dependencyModData, allMods, order);
					if (dependencyMods.Count > 0)
					{
						mods.AddRange(dependencyMods);
					}
					if (!order.Any(x => x.UUID == dependencyModData.UUID) && !mods.Any(x => x.UUID == dependencyModData.UUID))
					{
						mods.Add(dependencyModData);
					}
				}
			}
			return mods;
		}

		public static List<DivinityModData> BuildOutputList(IEnumerable<DivinityLoadOrderEntry> order, IEnumerable<DivinityModData> allMods, bool addDependencies = true, DivinityModData selectedAdventure = null)
		{
			List<DivinityModData> orderList = new List<DivinityModData>();
			if (selectedAdventure != null)
			{
				if (addDependencies && selectedAdventure.HasDependencies)
				{
					orderList.AddRange(GetDependencyMods(selectedAdventure, allMods, order));
				}
				orderList.Add(selectedAdventure);
			}

			foreach (var m in order.Where(x => !x.Missing))
			{
				var mData = allMods.FirstOrDefault(x => x.UUID == m.UUID);
				if (mData != null)
				{
					if (addDependencies && mData.HasDependencies)
					{
						orderList.AddRange(GetDependencyMods(mData, allMods, order));
					}

					if (!orderList.Any(x => x.UUID == mData.UUID))
					{
						orderList.Add(mData);
					}
				}
				else
				{
					DivinityApp.Log($"[*ERROR*] Missing mod for mod in order: '{m.Name}'.");
				}
			}
			return orderList;
		}

		public static string GenerateModSettingsFile(IEnumerable<DivinityModData> orderList)
		{
			/* Active mods are contained within the "ModOrder" node.*/
			string modulesText = "";
			/* The "Mods" node is used for the in-game menu it seems. The selected adventure mod is always at the top. */
			string modShortDescText = "";

			foreach (var mod in orderList)
			{
				if (!String.IsNullOrWhiteSpace(mod.UUID))
				{
					modulesText += String.Format(DivinityApp.XML_MOD_ORDER_MODULE, mod.UUID) + Environment.NewLine;
					string safeName = System.Security.SecurityElement.Escape(mod.Name);
					modShortDescText += String.Format(DivinityApp.XML_MODULE_SHORT_DESC, mod.Folder, mod.MD5, safeName, mod.UUID, mod.Version.VersionInt) + Environment.NewLine;
				}
			}

			return String.Format(DivinityApp.XML_MOD_SETTINGS_TEMPLATE, modulesText, modShortDescText);
		}

		public static string CreateHandle()
		{
			return Guid.NewGuid().ToString().Replace('-', 'g').Insert(0, "h");
		}

		private static Node FindNode(Node node, string name)
		{
			if (node.Name.Equals(name, SCOMP))
			{
				return node;
			}
			else
			{
				return FindNode(node.Children, name);
			}
		}

		private static Node FindNode(Dictionary<string, List<Node>> children, string name)
		{
			foreach (var kvp in children)
			{
				if (kvp.Key.Equals(name, SCOMP))
				{
					return kvp.Value.FirstOrDefault();
				}

				foreach (var node in kvp.Value)
				{
					var match = FindNode(node, name);
					if (match != null)
					{
						return match;
					}
				}
			}
			return null;
		}

		private static Node FindNode(Region region, string name)
		{
			foreach (var kvp in region.Children)
			{
				if (kvp.Key.Equals(name, SCOMP))
				{
					return kvp.Value.First();
				}
			}

			var match = FindNode(region.Children, name);
			if (match != null)
			{
				return match;
			}

			return null;
		}

		private static Node FindNode(Resource resource, string name)
		{
			foreach (var region in resource.Regions.Values)
			{
				var match = FindNode(region, name);
				if (match != null)
				{
					return match;
				}
			}

			return null;
		}

		public static DivinityLoadOrder GetLoadOrderFromSave(string file, string ordersFolder = "")
		{
			try
			{
				using (var reader = new PackageReader(file))
				{
					Package package = reader.Read();
					AbstractFileInfo abstractFileInfo = package.Files.FirstOrDefault(p => p.Name == "meta.lsf");
					if (abstractFileInfo == null)
					{
						return null;
					}

					Resource resource;
					System.IO.Stream rsrcStream = abstractFileInfo.MakeStream();
					try
					{
						using (var rsrcReader = new LSFReader(rsrcStream))
						{
							resource = rsrcReader.Read();
						}
					}
					finally
					{
						abstractFileInfo.ReleaseStream();
					}

					if (resource != null)
					{
						var modListChildrenRoot = FindNode(resource, "Mods");

						if (modListChildrenRoot != null)
						{
							var modList = modListChildrenRoot.Children.Values.FirstOrDefault();
							if (modList != null && modList.Count > 0)
							{
								var fileName = Path.GetFileNameWithoutExtension(file);
								string orderName = fileName;
								var re = new Regex(@".*PlayerProfiles\\(.*?)\\Savegames.*");
								var match = re.Match(Path.GetFullPath(file));
								if (match.Success)
								{
									orderName = $"{match.Groups[1].Value}_{fileName}";
								}
								DivinityLoadOrder loadOrder = new DivinityLoadOrder()
								{
									Name = orderName,
									FilePath = Path.Combine(ordersFolder, MakeSafeFilename(Path.Combine(orderName + ".json"), '_'))
								};

								foreach (var c in modList)
								{
									string name = "";
									string uuid = null;
									if (c.Attributes.TryGetValue("UUID", out var idAtt))
									{
										uuid = (string)idAtt.Value;
									}

									if (c.Attributes.TryGetValue("Name", out var nameAtt))
									{
										name = (string)nameAtt.Value;
									}

									if (uuid != null && !IgnoreMod(uuid))
									{
										DivinityApp.Log($"Found mod in save: '{name}_{uuid}'.");
										loadOrder.Order.Add(new DivinityLoadOrderEntry()
										{
											UUID = uuid,
											Name = name
										});
									}
									else
									{
										DivinityApp.Log($"Ignoring mod in save: '{name}_{uuid}'.");
									}
								}

								if (loadOrder.Order.Count > 0)
								{
									return loadOrder;
								}
							}
						}
						else
						{
							DivinityApp.Log($"Couldn't find Mods node '{String.Join(";", resource.Regions.Values.First().Children.Keys)}'.");
						}
					}
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error parsing save '{file}':\n{ex}");
			}

			return null;
		}

		private static async Task<DivinityModScriptExtenderConfig> LoadScriptExtenderConfigAsync(string configFile)
		{
			try
			{
				using (var reader = File.OpenText(configFile))
				{
					var text = await reader.ReadToEndAsync();
					if (!String.IsNullOrWhiteSpace(text))
					{
						var config = DivinityJsonUtils.SafeDeserialize<DivinityModScriptExtenderConfig>(text);
						if (config != null)
						{
							return config;
						}
						else
						{
							var jsonObj = JObject.Parse(text);
							if (jsonObj != null)
							{
								config = new DivinityModScriptExtenderConfig();
								config.RequiredExtensionVersion = jsonObj.GetValue<int>("RequiredExtensionVersion", -1);
								config.FeatureFlags = jsonObj.GetValue<List<string>>("FeatureFlags", null);
								return config;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error reading '{configFile}': {ex}");
			}
			return null;
		}

		private static async Task<DivinityModScriptExtenderConfig> LoadScriptExtenderConfigAsync(AbstractFileInfo configFile)
		{
			try
			{
				using (var stream = configFile.MakeStream())
				{
					using (var sr = new System.IO.StreamReader(stream))
					{
						string text = await sr.ReadToEndAsync();
						if (!String.IsNullOrWhiteSpace(text))
						{
							var config = DivinityJsonUtils.SafeDeserialize<DivinityModScriptExtenderConfig>(text);
							if (config != null)
							{
								return config;
							}
							else
							{
								var jsonObj = JObject.Parse(text);
								if (jsonObj != null)
								{
									config = new DivinityModScriptExtenderConfig();
									config.RequiredExtensionVersion = jsonObj.GetValue<int>("RequiredExtensionVersion", -1);
									config.FeatureFlags = jsonObj.GetValue<List<string>>("FeatureFlags", null);
									return config;
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error reading '{configFile?.Name}': {ex}");
			}
			return null;
		}

		public static List<DivinityModData> LoadBuiltinMods(string gameDataPath)
		{
			List<DivinityModData> baseMods = new List<DivinityModData>();

			try
			{
				var modResources = new ModResources();
				var modHelper = new ModPathVisitor(modResources)
				{
					Game = DivinityApp.GAME_COMPILER,
					CollectGlobals = false,
					CollectLevels = false,
					CollectStoryGoals = false,
					CollectStats = false
				};

				modHelper.DiscoverBuiltinPackages(gameDataPath);

				if (modResources.Mods != null && modResources.Mods.Values != null)
				{
					foreach (var modInfo in modResources.Mods.Values)
					{
						var metaFile = modInfo.Meta;
						if (metaFile != null)
						{
							using (var stream = metaFile.MakeStream())
							{
								using (var sr = new System.IO.StreamReader(stream))
								{
									string text = sr.ReadToEnd();
									var modData = ParseMetaFile(text, true);
									if (modData != null)
									{
										modData.IsLarianMod = DivinityApp.IgnoredMods.Any(x => x.UUID == modData.UUID) || modData.Author.Contains("Larian");
										modData.IsHidden = modData.IsLarianMod;

										var last = baseMods.FirstOrDefault(x => x.UUID == modData.UUID);

										if (last == null)
										{
											baseMods.Add(modData);
										}
										else
										{

											if (modData.Version.VersionInt > last.Version.VersionInt)
											{
												baseMods.Remove(last);
												baseMods.Add(modData);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log("Error parsing base game mods:\n" + ex.ToString());
			}

			return baseMods;
		}

		private static async Task<DivinityModData> LoadModFromModInfo(ModInfo modInfo, CancellationToken token)
		{
			var metaFile = modInfo.Meta;
			if (metaFile != null)
			{
				using (var stream = metaFile.MakeStream())
				{
					using (var sr = new System.IO.StreamReader(stream))
					{
						if (token.IsCancellationRequested) return null;
						string text = await sr.ReadToEndAsync();
						var modData = ParseMetaFile(text, true);
						if (modData != null)
						{
							DivinityApp.Log($"Added base mod: Name({modData.Name}) UUID({modData.UUID}) Author({modData.Author}) Version({modData.Version.VersionInt})");
							modData.IsLarianMod = DivinityApp.IgnoredMods.Any(x => x.UUID == modData.UUID) || modData.Author.Contains("Larian");
							modData.IsHidden = modData.IsLarianMod;
							return modData;
						}
					}
				}
			}
			return null;
		}

		public static async Task<List<DivinityModData>> LoadBuiltinModsAsync(string gameDataPath, CancellationToken token)
		{
			List<DivinityModData> baseMods = new List<DivinityModData>();

			try
			{
				var modResources = new ModResources();
				var modHelper = new ModPathVisitor(modResources)
				{
					Game = DivinityApp.GAME_COMPILER,
					CollectGlobals = false,
					CollectLevels = false,
					CollectStoryGoals = false,
					CollectStats = false
				};

				modHelper.DiscoverBuiltinPackages(gameDataPath);

				if (modResources.Mods != null && modResources.Mods.Values != null)
				{
					var currentTime = DateTime.Now;
					foreach(var modInfo in modResources.Mods.Values)
					{
						var modData = await LoadModFromModInfo(modInfo, token);
						if (modData != null)
						{
							baseMods.Add(modData);
						}
					}
					DivinityApp.Log($"Took {DateTime.Now - currentTime:s\\.ff} seconds(s) to load builtin mods.");
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log("Error parsing base game mods:\n" + ex.ToString());
			}

			return baseMods;
		}
	}
}
