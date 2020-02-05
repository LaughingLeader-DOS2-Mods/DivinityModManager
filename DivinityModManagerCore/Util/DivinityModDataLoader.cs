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
using System.Threading;
using Newtonsoft.Json.Linq;

namespace DivinityModManager.Util
{
	public static class DivinityModDataLoader
	{
		public static bool IgnoreMod(string modUUID)
		{
			return DivinityApp.IgnoredMods.Any(m => m.UUID == modUUID);
		}

		public static bool IgnoreModByFolder(string folder)
		{
			return DivinityApp.IgnoredEditorMods.Any(m => m.Folder.Equals(Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar)), StringComparison.OrdinalIgnoreCase));
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
			if(!String.IsNullOrWhiteSpace(str) && int.TryParse(str, out int val))
			{
				return val;
			}
			return -1;
		}

		public static string EscapeXml(string s)
		{
			string toxml = s;
			if (!string.IsNullOrEmpty(toxml))
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
			if (!string.IsNullOrEmpty(xmlstring))
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
			if (!string.IsNullOrEmpty(str))
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

		private static DivinityModData ParseMetaFile(string metaContents)
		{
			try
			{
				XElement xDoc = XElement.Parse(EscapeXmlAttributes(metaContents));
				var versionNode = xDoc.Descendants("version").FirstOrDefault();

				int headerMajor = 3;
				int headerMinor = 6;
				int headerRevision = 1;
				int headerBuild = 5;

				if (versionNode != null)
				{
					//Trace.WriteLine($"Version node: {versionNode.ToString()}");
					//DE Mods <version major="3" minor="6" revision="2" build="0" />
					//Classic Mods <version major="3" minor="1" revision="3" build="5" />
					if(TryGetAttribute(versionNode, "major", out var headerMajorStr))
					{
						int.TryParse(headerMajorStr, out headerMajor);
					}
					if(TryGetAttribute(versionNode, "minor", out var headerMinorStr))
					{
						int.TryParse(headerMinorStr, out headerMinor);
					}
					if(TryGetAttribute(versionNode, "revision", out var headerRevisionStr))
					{
						int.TryParse(headerRevisionStr, out headerRevision);
					}
					if(TryGetAttribute(versionNode, "build", out var headerBuildStr))
					{
						int.TryParse(headerBuildStr, out headerBuild);
					}

					//Trace.WriteLine($"Version: {headerMajor}.{headerMinor}.{headerRevision}.{headerBuild}");
				}

				var moduleInfoNode = xDoc.Descendants("node").FirstOrDefault(n => n.Attribute("id")?.Value == "ModuleInfo");
				if (moduleInfoNode != null)
				{
					var uuid = GetAttributeWithId(moduleInfoNode, "UUID", "");
					var name = UnescapeXml(GetAttributeWithId(moduleInfoNode, "Name", ""));
					var description = UnescapeXml(GetAttributeWithId(moduleInfoNode, "Description", ""));
					var author = UnescapeXml(GetAttributeWithId(moduleInfoNode, "Author", ""));
					if (DivinityApp.MODS_GiftBag.Any(x => x.UUID == uuid))
					{
						name = UnescapeXml(GetAttributeWithId(moduleInfoNode, "DisplayName", name));
						description = UnescapeXml(GetAttributeWithId(moduleInfoNode, "DescriptionName", description));
						author = "Larian Studios";
					}
					DivinityModData modData = new DivinityModData()
					{
						UUID = uuid,
						Name = name,
						Author = author,
						Version = DivinityModVersion.FromInt(SafeConvertString(GetAttributeWithId(moduleInfoNode, "Version", ""))),
						Folder = GetAttributeWithId(moduleInfoNode, "Folder", ""),
						Description = description,
						MD5 = GetAttributeWithId(moduleInfoNode, "MD5", ""),
						Type = GetAttributeWithId(moduleInfoNode, "Type", ""),
						HeaderVersion = new DivinityModVersion(headerMajor, headerMinor, headerRevision, headerBuild)
					};
					modData.UpdateDisplayName();
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
						foreach (var node in targets)
						{
							var target = GetAttributeWithId(node, "Object", "");
							if(!String.IsNullOrEmpty(target))
							{
								modData.Modes.Add(target);
							}
						}
						modData.Targets = string.Join(", ", modData.Modes);
					}

					return modData;
				}
				else
				{
					Trace.WriteLine($"**[ERROR] ModuleInfo node not found for meta.lsx: {metaContents}");
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine($"Error parsing meta.lsx: {ex.ToString()}");
			}
			return null;
		}

		//BOM
		private static string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());

		public static List<DivinityModData> LoadEditorProjects(string modsFolderPath)
		{
			List<DivinityModData> projects = new List<DivinityModData>();

			try
			{
				if (Directory.Exists(modsFolderPath))
				{
					var projectDirectories = Directory.EnumerateDirectories(modsFolderPath);
					var filteredFolders = projectDirectories.Where(f => !IgnoreModByFolder(f));
					Console.WriteLine($"Project Folders: {filteredFolders.Count()} / {projectDirectories.Count()}");
					foreach (var folder in filteredFolders)
					{
						//Console.WriteLine($"Folder: {Path.GetFileName(folder)} Blacklisted: {IgnoredMods.Any(m => Path.GetFileName(folder).Equals(m.Folder, StringComparison.OrdinalIgnoreCase))}");
						var metaFile = Path.Combine(folder, "meta.lsx");
						if (File.Exists(metaFile))
						{
							
							var str = File.ReadAllText(metaFile);
							if(!String.IsNullOrEmpty(str))
							{
								//BOM stripping
								if (str.StartsWith(_byteOrderMarkUtf8, StringComparison.Ordinal))
								{
									str = str.Remove(0, _byteOrderMarkUtf8.Length);
								}
								DivinityModData modData = ParseMetaFile(str);
								if (modData != null)
								{
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

									projects.Add(modData);

									var osiConfigFile = Path.Combine(folder, "OsiToolsConfig.json");
									if (File.Exists(osiConfigFile))
									{
										var osiToolsConfig = LoadOsiConfig(osiConfigFile);
										if (osiToolsConfig != null)
										{
											modData.OsiExtenderData = osiToolsConfig;
											if (modData.OsiExtenderData.RequiredExtensionVersion > -1) modData.HasOsirisExtenderSettings = true;
#if DEBUG
											Trace.WriteLine($"Loaded OsiToolsConfig.json for '{folder}':");
											Trace.WriteLine($"\tRequiredVersion: {modData.OsiExtenderData.RequiredExtensionVersion}");
											if (modData.OsiExtenderData.FeatureFlags != null) Trace.WriteLine($"\tFeatureFlags: {String.Join(",", modData.OsiExtenderData.FeatureFlags)}");
#endif
										}
										else
										{
											Trace.WriteLine($"Failed to parse OsiToolsConfig.json for '{folder}'.");
										}
									}
								}
							}
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

		public static async Task<List<DivinityModData>> LoadEditorProjectsAsync(string modsFolderPath)
		{
			List<DivinityModData> projects = new List<DivinityModData>();

			try
			{
				if (Directory.Exists(modsFolderPath))
				{
					var projectDirectories = Directory.EnumerateDirectories(modsFolderPath);
					var filteredFolders = projectDirectories.Where(f => !IgnoreModByFolder(f));
					Console.WriteLine($"Project Folders: {filteredFolders.Count()} / {projectDirectories.Count()}");
					foreach (var folder in filteredFolders)
					{
						//Trace.WriteLine($"Reading meta file from folder: {folder}");
						//Console.WriteLine($"Folder: {Path.GetFileName(folder)} Blacklisted: {IgnoredMods.Any(m => Path.GetFileName(folder).Equals(m.Folder, StringComparison.OrdinalIgnoreCase))}");
						var metaFile = Path.Combine(folder, "meta.lsx");
						if (File.Exists(metaFile))
						{
							using (var fileStream = new System.IO.FileStream(metaFile, 
								System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
							{
								var result = new byte[fileStream.Length];
								await fileStream.ReadAsync(result, 0, (int)fileStream.Length);

								string str = Encoding.UTF8.GetString(result, 0, result.Length);

								if(!String.IsNullOrEmpty(str))
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
										modData.FilePath = folder;
										try
										{
											modData.LastModified = File.GetChangeTime(metaFile);
										}
										catch (PlatformNotSupportedException ex)
										{
											Trace.WriteLine($"Error getting last modified date for '{metaFile}': {ex.ToString()}");
										}
										projects.Add(modData);

										var osiConfigFile = Path.Combine(folder, "OsiToolsConfig.json");
										if (File.Exists(osiConfigFile))
										{
											var osiToolsConfig = await LoadOsiConfigAsync(osiConfigFile);
											if (osiToolsConfig != null)
											{
												modData.OsiExtenderData = osiToolsConfig;
												if (modData.OsiExtenderData.RequiredExtensionVersion > -1) modData.HasOsirisExtenderSettings = true;
#if DEBUG
												Trace.WriteLine($"Loaded OsiToolsConfig.json for '{folder}':");
												Trace.WriteLine($"\tRequiredVersion: {modData.OsiExtenderData.RequiredExtensionVersion}");
												if (modData.OsiExtenderData.FeatureFlags != null) Trace.WriteLine($"\tFeatureFlags: {String.Join(",", modData.OsiExtenderData.FeatureFlags)}");
#endif
											}
											else
											{
												Trace.WriteLine($"Failed to parse OsiToolsConfig.json for '{folder}'.");
											}
										}
									}
								}
							}
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

		private static Regex modMetaPattern = new Regex("^Mods/([^/]+)/meta.lsx", RegexOptions.IgnoreCase);
		private static bool IsModMetaFile(string pakName, AbstractFileInfo f)
		{
			if(Path.GetFileName(f.Name).Equals("meta.lsx", StringComparison.OrdinalIgnoreCase))
			{
				return modMetaPattern.IsMatch(f.Name);
			}
			return false;
		}

		public static List<DivinityModData> LoadModPackageData(string modsFolderPath, bool ignoreClassic = false)
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
							DivinityModData modData = null;

							string pakName = Path.GetFileNameWithoutExtension(pakPath);

							var pak = pr.Read();
							var metaFiles = pak?.Files?.Where(pf => IsModMetaFile(pakName, pf));
							AbstractFileInfo metaFile = null;
							foreach(var f in metaFiles)
							{
								var parentDir = Directory.GetParent(f.Name);
								// A pak may have multiple meta.lsx files for overriding NumPlayers or something. Match the parent diretory against the pak name in that case.
								if (parentDir.Name == pakName)
								{
									metaFile = f;
									break;
								}
							}
							if (metaFile == null) metaFile = metaFiles.FirstOrDefault();

							if (metaFile != null)
							{
								Trace.WriteLine($"Parsing meta.lsx for '{pakPath}'.");
								using (var stream = metaFile.MakeStream())
								{
									using (var sr = new System.IO.StreamReader(stream))
									{
										string text = sr.ReadToEnd();
										modData = ParseMetaFile(text);
									}
								}
							}
							else
							{
								Trace.WriteLine($"Error: No meta.lsx for mod pak '{pakPath}'.");
							}

							if(modData != null && (!ignoreClassic || !modData.IsClassicMod))
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

								var osiToolsConfig = pak?.Files?.FirstOrDefault(pf => pf.Name.Contains("OsiToolsConfig.json"));
								if (osiToolsConfig != null)
								{
									try
									{
										using (var stream = osiToolsConfig.MakeStream())
										{
											using (var sr = new System.IO.StreamReader(stream))
											{
												string text = sr.ReadToEnd();
												if (!String.IsNullOrWhiteSpace(text))
												{
													var osiConfig = DivinityJsonUtils.SafeDeserialize<DivinityModOsiExtenderConfig>(text);
													if (osiConfig != null)
													{
														modData.OsiExtenderData = osiConfig;
														if (modData.OsiExtenderData.RequiredExtensionVersion > -1) modData.HasOsirisExtenderSettings = true;
#if DEBUG
														Trace.WriteLine($"Loaded OsiToolsConfig.json for '{pakPath}':");
														Trace.WriteLine($"\tRequiredVersion: {modData.OsiExtenderData.RequiredExtensionVersion}");
														if (modData.OsiExtenderData.FeatureFlags != null) Trace.WriteLine($"\tFeatureFlags: {String.Join(",", modData.OsiExtenderData.FeatureFlags)}");
#endif
													}
													else
													{
														var jsonObj = JObject.Parse(text);
														if (jsonObj != null)
														{
															modData.OsiExtenderData = new DivinityModOsiExtenderConfig();
															modData.OsiExtenderData.RequiredExtensionVersion = jsonObj.GetValue<int>("RequiredExtensionVersion", -1);
															modData.OsiExtenderData.FeatureFlags = jsonObj.GetValue<List<string>>("FeatureFlags", null);

															if(modData.OsiExtenderData.RequiredExtensionVersion > -1) modData.HasOsirisExtenderSettings = true;
#if DEBUG
															Trace.WriteLine($"Loaded OsiToolsConfig.json for '{pakPath}':");
															Trace.WriteLine($"\tRequiredVersion: {modData.OsiExtenderData.RequiredExtensionVersion}");
															if (modData.OsiExtenderData.FeatureFlags != null)
															{
																Trace.WriteLine($"\tFeatureFlags: {String.Join(",", modData.OsiExtenderData.FeatureFlags)}");
															}
															else
															{
																Trace.WriteLine("\tFeatureFlags: null");
															}
#endif
														}
														else
														{
															Trace.WriteLine($"Failed to parse OsiToolsConfig.json for '{pakPath}':\n\t{text}");
														}
													}
												}
											}
										}
									}
									catch(Exception ex)
									{
										Trace.WriteLine($"Error reading 'OsiToolsConfig.json' for '{pakPath}': {ex.ToString()}");
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

		public static async Task<List<DivinityModData>> LoadModPackageDataAsync(string modsFolderPath, bool ignoreClassic = false)
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
				catch (Exception ex)
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
							DivinityModData modData = null;

							string pakName = Path.GetFileNameWithoutExtension(pakPath);

							var pak = pr.Read();
							var metaFiles = pak?.Files?.Where(pf => IsModMetaFile(pakName, pf));
							AbstractFileInfo metaFile = null;
							foreach (var f in metaFiles)
							{
								var parentDir = Directory.GetParent(f.Name);
								// A pak may have multiple meta.lsx files for overriding NumPlayers or something. Match against the pak name in that case.
								if (parentDir.Name == pakName)
								{
									metaFile = f;
									break;
								}
							}
							if (metaFile == null) metaFile = metaFiles.FirstOrDefault();
							if (metaFile != null)
							{
								Trace.WriteLine($"Parsing meta.lsx for mod pak '{pakPath}'.");
								using (var stream = metaFile.MakeStream())
								{
									using (var sr = new System.IO.StreamReader(stream))
									{
										string text = await sr.ReadToEndAsync();
										modData = ParseMetaFile(text);
									}
								}

								if (modData != null && (!ignoreClassic || !modData.IsClassicMod))
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

									var osiConfigInfo = pak.Files?.FirstOrDefault(pf => pf.Name.Contains("OsiToolsConfig.json"));
									if(osiConfigInfo != null)
									{
										var osiToolsConfig = await LoadOsiConfigAsync(osiConfigInfo);
										if (osiToolsConfig != null)
										{
											modData.OsiExtenderData = osiToolsConfig;
											if (modData.OsiExtenderData.RequiredExtensionVersion > -1) modData.HasOsirisExtenderSettings = true;
#if DEBUG
											Trace.WriteLine($"Loaded OsiToolsConfig.json for '{pakPath}':");
											Trace.WriteLine($"\tRequiredVersion: {modData.OsiExtenderData.RequiredExtensionVersion}");
											if (modData.OsiExtenderData.FeatureFlags != null) Trace.WriteLine($"\tFeatureFlags: {String.Join(",", modData.OsiExtenderData.FeatureFlags)}");
#endif
										}
										else
										{
											Trace.WriteLine($"Failed to parse OsiToolsConfig.json for '{pakPath}'.");
										}
									}
								}
							}
							else
							{
								Trace.WriteLine($"Error: No meta.lsx for mod pak '{pakPath}'.");
							}
						}
					}
					catch (Exception ex)
					{
						Trace.WriteLine($"Error loading pak '{pakPath}': {ex.ToString()}");
					}
				}
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

					//Console.WriteLine($"Folder: {Path.GetFileName(folder)} Blacklisted: {IgnoredMods.Any(m => Path.GetFileName(folder).Equals(m.Folder, StringComparison.OrdinalIgnoreCase))}");
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
							Trace.WriteLine($"Error reading '{modSettingsFile}': '{ex.ToString()}'");
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
											//Trace.WriteLine($"ModuleNode: {c.Name} Attributes: {String.Join(";", c.Attributes.Keys)}");
											if (c.Attributes.TryGetValue("UUID", out var attribute))
											{
												var uuid = (string)attribute.Value;
												if (!string.IsNullOrEmpty(uuid))
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

		public static async Task<Resource> LoadResourceAsync(string path, LSLib.LS.Enums.ResourceFormat resourceFormat, CancellationToken? token = null)
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
					Trace.WriteLine($"Error loading '{path}': {ex.ToString()}");
					return null;
				}
			});
		}

		public static async Task<Resource> LoadResourceAsync(System.IO.Stream stream, LSLib.LS.Enums.ResourceFormat resourceFormat, CancellationToken? token = null)
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
					Trace.WriteLine($"Error loading resource: {ex.ToString()}");
					return null;
				}
			});
		}

		public static string GetSelectedProfileUUID(string profilePath)
		{
			var playerprofilesFile = Path.Combine(profilePath, "playerprofiles.lsb");
			string activeProfileUUID = "";
			if (File.Exists(playerprofilesFile))
			{
				try
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
				catch (Exception ex)
				{
					Trace.WriteLine($"Error loading {playerprofilesFile}: {ex.ToString()}");
				}
			}
			return activeProfileUUID;
		}

		public static bool ExportedSelectedProfile(string profilePath, string profileUUID)
		{
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
							ResourceUtils.SaveResource(res, playerprofilesFile, LSLib.LS.Enums.ResourceFormat.LSB);
							return true;
						}
					}
				}
				catch(Exception ex)
				{
					Trace.WriteLine($"Error saving {playerprofilesFile}: {ex.ToString()}");
				}
			}
			else
			{
				Trace.WriteLine($"[*WARNING*] '{playerprofilesFile}' does not exist. Skipping selected profile saving.");
			}
			return false;
		}

		public static async Task<string> GetSelectedProfileUUIDAsync(string profilePath)
		{
			var playerprofilesFile = Path.Combine(profilePath, "playerprofiles.lsb");
			string activeProfileUUID = "";
			if (File.Exists(playerprofilesFile))
			{
				Trace.WriteLine($"Loading playerprofiles.lsb at '{playerprofilesFile}'");
				var res = await LoadResourceAsync(playerprofilesFile, LSLib.LS.Enums.ResourceFormat.LSB);
				if (res != null && res.Regions.TryGetValue("UserProfiles", out var region))
				{
					//Trace.WriteLine($"ActiveProfile | Getting root node '{String.Join(";", region.Attributes.Keys)}'");

					if (region.Attributes.TryGetValue("ActiveProfile", out var att))
					{
						Trace.WriteLine($"ActiveProfile | '{att.Value}'");
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
						return f.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
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
						Trace.WriteLine($"Failed to read '{loadOrderFile}': {ex.ToString()}");
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
						return f.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase) && !f.FileName.Equals("settings.json", StringComparison.OrdinalIgnoreCase);
					}
				});

				foreach(var loadOrderFile in files)
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
								order.LastModifiedDate = File.GetLastWriteTime(loadOrderFile);

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
				try
				{
					using (var reader = File.OpenText(loadOrderFile))
					{
						var fileText = await reader.ReadToEndAsync();
						DivinityLoadOrder order = DivinityJsonUtils.SafeDeserialize<DivinityLoadOrder>(fileText);
						if(order != null)
						{
							order.FilePath = loadOrderFile;
						}
						return order;
					}
				}
				catch(Exception ex)
				{
					Trace.WriteLine($"Error loading '{loadOrderFile}': {ex.ToString()}");
				}
			}
			return null;
		}

		public static DivinityLoadOrder LoadOrderFromFile(string loadOrderFile)
		{
			if (File.Exists(loadOrderFile))
			{
				try
				{
					DivinityLoadOrder order = DivinityJsonUtils.SafeDeserialize<DivinityLoadOrder>(File.ReadAllText(loadOrderFile));
					return order;
				}
				catch(Exception ex)
				{
					Trace.WriteLine($"Error reading '{loadOrderFile}': {ex.ToString()}");
				}
			}
			return null;
		}

		public static async Task<bool> ExportModSettingsToFileAsync(string folder, DivinityLoadOrder order, IEnumerable<DivinityModData> allMods, bool addDependencies, DivinityModData selectedAdventure)
		{
			if(Directory.Exists(folder))
			{
				string outputFilePath = Path.Combine(folder, "modsettings.lsx");
				string contents = GenerateModSettingsFile(order.Order, allMods, addDependencies, selectedAdventure);
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
				catch(AccessViolationException ex)
				{
					Trace.WriteLine($"Failed to write file '{outputFilePath}': {ex.ToString()}");
				}
			}
			return false;
		}

		public static string GenerateModSettingsFile(IEnumerable<DivinityLoadOrderEntry> order, IEnumerable<DivinityModData> allMods, bool addDependencies, DivinityModData selectedAdventure)
		{
			List<string> orderList = new List<string>();
			foreach (var m in order.Where(x => !x.Missing))
			{
				var mData = allMods.FirstOrDefault(x => x.UUID == m.UUID);
				if(mData != null)
				{
					if (addDependencies && mData.HasDependencies)
					{
						var dependencies = mData.Dependencies.Where(x => (!order.Any(y => y.UUID == x.UUID) && !IgnoreMod(x.UUID)));
						foreach (var d in dependencies)
						{
							if (!orderList.Any(x => x == d.UUID))
							{
								orderList.Add(d.UUID);
								Trace.WriteLine($"Added missing dependency '{d.Name}' above mod '{mData.Name}'");
							}
						}
					}

					orderList.Add(mData.UUID);
				}
				else
				{
					Trace.WriteLine($"[*ERROR*] Missing mod pak for mod in order: '{m.Name}'.");
				}
			}

			/* The ModOrder node contains the load order. DOS2 by default stores all UUIDs, even if the mod no longer exists. */
			string modulesText = "";
			foreach(var uuid in orderList)
			{
				modulesText += String.Format(DivinityApp.XML_MOD_ORDER_MODULE, uuid) + Environment.NewLine;
			}

			/* Active mods are contained within the Mods node. Origins is always included at the top, despite it not being in ModOrder. */
			string modShortDescText = "";

			if(selectedAdventure == DivinityApp.MOD_Origins)
			{
				foreach (var mod in DivinityApp.MODS_Base)
				{
					string safeName = System.Security.SecurityElement.Escape(mod.Name);
					modShortDescText += String.Format(DivinityApp.XML_MODULE_SHORT_DESC, mod.Folder, mod.MD5, safeName, mod.UUID, mod.Version.VersionInt) + Environment.NewLine;
				}
			}
			else
			{
				modShortDescText += String.Format(DivinityApp.XML_MODULE_SHORT_DESC, 
					selectedAdventure.Folder, selectedAdventure.MD5, System.Security.SecurityElement.Escape(selectedAdventure.Name), selectedAdventure.UUID, selectedAdventure.Version.VersionInt) + Environment.NewLine;
				foreach (var mod in DivinityApp.MODS_Base.Where(x => x != DivinityApp.MOD_Origins))
				{
					string safeName = System.Security.SecurityElement.Escape(mod.Name);
					modShortDescText += String.Format(DivinityApp.XML_MODULE_SHORT_DESC, mod.Folder, mod.MD5, safeName, mod.UUID, mod.Version.VersionInt) + Environment.NewLine;
				}
			}

			
			foreach (var mod in allMods.Where(m => orderList.Any(o => o == m.UUID)))
			{
				string safeName = System.Security.SecurityElement.Escape(mod.Name);
				modShortDescText += String.Format(DivinityApp.XML_MODULE_SHORT_DESC, mod.Folder, mod.MD5, safeName, mod.UUID, mod.Version.VersionInt) + Environment.NewLine;
			}
			//string output = String.Format(Properties.Resources.ModSettingsTemplate, modulesText, modShortDescText);
			string output = String.Format(DivinityApp.XML_MOD_SETTINGS_TEMPLATE, modulesText, modShortDescText);

			//Trace.WriteLine(output);
			return output;
		}

		public static string CreateHandle()
		{
			return Guid.NewGuid().ToString().Replace('-', 'g').Insert(0, "h");
		}

		private static Node FindNode(Node node, string name)
		{
			if (node.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
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
				if(kvp.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
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
			foreach(var kvp in region.Children)
			{
				if (kvp.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
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
			foreach(var region in resource.Regions.Values)
			{
				var match = FindNode(region, name);
				if (match != null)
				{
					return match;
				}
			}
			
			return null;
		}

		public static DivinityLoadOrder GetLoadOrderFromSave(string file)
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
									Name = orderName
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
										Trace.WriteLine($"Found mod in save: '{name}_{uuid}'.");
										loadOrder.Order.Add(new DivinityLoadOrderEntry()
										{
											UUID = uuid,
											Name = name
										});
									}
									else
									{
										Trace.WriteLine($"Ignoring mod in save: '{name}_{uuid}'.");
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
							Trace.WriteLine($"Couldn't find Mods node '{String.Join(";", resource.Regions.Values.First().Children.Keys)}'.");
						}
					}
				}
			}
			catch(Exception ex)
			{
				Trace.WriteLine($"Error parsing save '{file}':\n{ex.ToString()}");
			}

			return null;
		}


		private static DivinityModOsiExtenderConfig LoadOsiConfig(string osiToolsConfig)
		{
			try
			{
				var text = File.ReadAllText(osiToolsConfig);
				if (!String.IsNullOrWhiteSpace(text))
				{
					var osiConfig = DivinityJsonUtils.SafeDeserialize<DivinityModOsiExtenderConfig>(text);
					if (osiConfig != null)
					{
						return osiConfig;
					}
					else
					{
						var jsonObj = JObject.Parse(text);
						if (jsonObj != null)
						{
							osiConfig = new DivinityModOsiExtenderConfig();
							osiConfig.RequiredExtensionVersion = jsonObj.GetValue<int>("RequiredExtensionVersion", -1);
							osiConfig.FeatureFlags = jsonObj.GetValue<List<string>>("FeatureFlags", null);
							return osiConfig;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Error reading 'OsiToolsConfig.json': {ex.ToString()}");
			}
			return null;
		}

		private static async Task<DivinityModOsiExtenderConfig> LoadOsiConfigAsync(string osiToolsConfig)
		{
			try
			{
				using (var reader = File.OpenText(osiToolsConfig))
				{
					var text = await reader.ReadToEndAsync();
					if (!String.IsNullOrWhiteSpace(text))
					{
						var osiConfig = DivinityJsonUtils.SafeDeserialize<DivinityModOsiExtenderConfig>(text);
						if (osiConfig != null)
						{
							return osiConfig;
						}
						else
						{
							var jsonObj = JObject.Parse(text);
							if (jsonObj != null)
							{
								osiConfig = new DivinityModOsiExtenderConfig();
								osiConfig.RequiredExtensionVersion = jsonObj.GetValue<int>("RequiredExtensionVersion", -1);
								osiConfig.FeatureFlags = jsonObj.GetValue<List<string>>("FeatureFlags", null);
								return osiConfig;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Error reading 'OsiToolsConfig.json': {ex.ToString()}");
			}
			return null;
		}

		private static async Task<DivinityModOsiExtenderConfig> LoadOsiConfigAsync(AbstractFileInfo osiToolsConfig)
		{
			try
			{
				using (var stream = osiToolsConfig.MakeStream())
				{
					using (var sr = new System.IO.StreamReader(stream))
					{
						string text = await sr.ReadToEndAsync();
						if (!String.IsNullOrWhiteSpace(text))
						{
							var osiConfig = DivinityJsonUtils.SafeDeserialize<DivinityModOsiExtenderConfig>(text);
							if (osiConfig != null)
							{
								return osiConfig;
							}
							else
							{
								var jsonObj = JObject.Parse(text);
								if (jsonObj != null)
								{
									osiConfig = new DivinityModOsiExtenderConfig();
									osiConfig.RequiredExtensionVersion = jsonObj.GetValue<int>("RequiredExtensionVersion", -1);
									osiConfig.FeatureFlags = jsonObj.GetValue<List<string>>("FeatureFlags", null);
									return osiConfig;
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Error reading 'OsiToolsConfig.json': {ex.ToString()}");
			}
			return null;
		}
	}
}
