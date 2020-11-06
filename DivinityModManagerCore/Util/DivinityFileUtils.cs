using Alphaleonis.Win32.Filesystem;
using DivinityModManager.Models;
using LSLib.LS;
using LSLib.LS.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DivinityModManager.Util
{
	/// <summary>
	/// Gets a unique file name if the file already exists.
	/// Source: https://stackoverflow.com/a/13050041
	/// </summary>
	public static class DivinityFileUtils
	{
		/// <summary>
		/// Gets the drive type of the given path.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns>DriveType of path</returns>
		public static System.IO.DriveType GetPathDriveType(string path)
		{
			//OK, so UNC paths aren't 'drives', but this is still handy
			if (path.StartsWith(@"\\")) return System.IO.DriveType.Network;
			var info = DriveInfo.GetDrives().Where(i => path.StartsWith(i.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
			if (info == null) return System.IO.DriveType.Unknown;
			return info.DriveType;
		}

		/// <summary>
		/// Check if a directory is the base of another
		/// </summary>
		/// <param name="root">Candidate root</param>
		/// <param name="child">Child folder</param>
		public static bool IsSubdirectoryOf(DirectoryInfo root, DirectoryInfo child)
		{
			var directoryPath = EndsWithSeparator(new Uri(child.FullName).AbsolutePath);
			var rootPath = EndsWithSeparator(new Uri(root.FullName).AbsolutePath);
			return directoryPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Check if a directory is the base of another
		/// </summary>
		/// <param name="root">Candidate root</param>
		/// <param name="child">Child folder</param>
		public static bool IsSubdirectoryOf(string root, string child)
		{
			return IsSubdirectoryOf(new DirectoryInfo(root), new DirectoryInfo(child));
		}

		private static string EndsWithSeparator(string absolutePath)
		{
			return absolutePath?.TrimEnd('/', '\\') + "/";
		}

		public static string GetUniqueFilename(string fullPath)
		{
			if (!Path.IsPathRooted(fullPath))
				fullPath = Path.GetFullPath(fullPath);
			if (File.Exists(fullPath))
			{
				String filename = Path.GetFileName(fullPath);
				String path = fullPath.Substring(0, fullPath.Length - filename.Length);
				String filenameWOExt = Path.GetFileNameWithoutExtension(fullPath);
				String ext = Path.GetExtension(fullPath);
				int n = 1;
				do
				{
					fullPath = Path.Combine(path, String.Format("{0} ({1}){2}", filenameWOExt, (n++), ext));
				}
				while (File.Exists(fullPath));
			}
			return fullPath;
		}


		public static List<string> IgnoredPackageFiles = new List<string>(){
			"ReConHistory.txt",
			"dialoglog.txt",
			"errors.txt",
			"log.txt",
			"personallog.txt",
			"story_orphanqueries_found.txt",
			"goals.div",
			"goals.raw",
			"story.div",
			"story_ac.dat",
			"story_definitions.div",
			"story.div.osi",
			".ailog",
			".log",
			".debugInfo",
			".dmp",
		};

		private static bool IgnoreFile(string targetFilePath, string ignoredFileName)
		{
			if (Path.GetFileName(targetFilePath).Equals(ignoredFileName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			else if (ignoredFileName.Substring(0) == "." && Path.GetExtension(targetFilePath).Equals(ignoredFileName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			return false;
		}
		#region Package Creation Async
		public static async Task<bool> CreatePackageAsync(string dataRootPath, List<string> inputPaths, string outputPath, List<string> ignoredFiles, CancellationToken? token = null)
		{
			try
			{
				if (token == null) token = CancellationToken.None;

				if (token.Value.IsCancellationRequested)
				{
					return false;
				}

				if (!dataRootPath.EndsWith(Alphaleonis.Win32.Filesystem.Path.DirectorySeparatorChar.ToString()))
				{
					dataRootPath += Alphaleonis.Win32.Filesystem.Path.DirectorySeparatorChar;
				}

				var package = new Package();

				foreach (var f in inputPaths)
				{
					if (token.Value.IsCancellationRequested) throw new TaskCanceledException("Cancelled package creation.");
					await AddFilesToPackageAsync(package, f, dataRootPath, outputPath, ignoredFiles, token.Value);
				}

				DivinityApp.Log($"Writing package '{outputPath}'.");
				using (var writer = new PackageWriter(package, outputPath))
				{
					await WritePackageAsync(writer, package, outputPath, token.Value);
				}
				return true;
			}
			catch (Exception ex)
			{
				if (!token.Value.IsCancellationRequested)
				{
					DivinityApp.Log($"Error creating package: {ex.ToString()}");
				}
				else
				{
					DivinityApp.Log($"Cancelled creating package: {ex.ToString()}");
				}
				return false;
			}
		}

		private static Task AddFilesToPackageAsync(Package package, string path, string dataRootPath, string outputPath, List<string> ignoredFiles, CancellationToken token)
		{
			Task task = null;

			task = Task.Run(() =>
			{
				if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
				{
					path += Path.DirectorySeparatorChar;
				}

				//var files = Directory.EnumerateFiles(path, "*.*", System.IO.SearchOption.AllDirectories).ToDictionary(k => k.Replace(dataRootPath, String.Empty), v => v);

				var files = Directory.EnumerateFiles(path, DirectoryEnumerationOptions.Recursive | DirectoryEnumerationOptions.LargeCache, new DirectoryEnumerationFilters()
				{
					InclusionFilter = (f) =>
					{
						return !ignoredFiles.Any(x => IgnoreFile(f.FullPath, x));
					}
				}).ToDictionary(k => k.Replace(dataRootPath, String.Empty), v => v);

				foreach (KeyValuePair<string, string> file in files)
				{
					if (token.IsCancellationRequested)
					{
						throw new TaskCanceledException(task);
					}
					FilesystemFileInfo fileInfo = FilesystemFileInfo.CreateFromEntry(file.Value, file.Key);
					package.Files.Add(fileInfo);
				}
			}, token);

			return task;
		}

		private static Task WritePackageAsync(PackageWriter writer, Package package, string outputPath, CancellationToken token)
		{
			var task = Task.Run(async () =>
			{
				// execute actual operation in child task
				var childTask = Task.Factory.StartNew(() =>
				{
					try
					{
						//writer.WriteProgress += WriteProgressUpdate;
						writer.Version = PackageVersion.V13;
						writer.Compression = CompressionMethod.LZ4;
						writer.CompressionLevel = CompressionLevel.MaxCompression;
						writer.Write();
					}
					catch (Exception)
					{
						// ignored because an exception on a cancellation request 
						// cannot be avoided if the stream gets disposed afterwards 
					}
				}, TaskCreationOptions.AttachedToParent);

				var awaiter = childTask.GetAwaiter();
				while (!awaiter.IsCompleted)
				{
					await Task.Delay(0, token);
				}
			}, token);

			return task;
		}
		#endregion
		#region Package Creation
		public static bool CreatePackage(string dataRootPath, List<string> inputPaths, string outputPath, List<string> ignoredFiles)
		{
			try
			{
				if (!dataRootPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
				{
					dataRootPath += Path.DirectorySeparatorChar;
				}

				var package = new Package();

				foreach (var f in inputPaths)
				{
					AddFilesToPackage(package, f, dataRootPath, outputPath, ignoredFiles);
				}

				DivinityApp.Log($"Writing package '{outputPath}'.");
				using (var writer = new PackageWriter(package, outputPath))
				{
					WritePackage(writer, package, outputPath);
				}
				return true;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error creating package: {ex}");
				return false;
			}
		}

		private static void AddFilesToPackage(Package package, string path, string dataRootPath, string outputPath, List<string> ignoredFiles)
		{
			if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				path += Path.DirectorySeparatorChar;
			}

			var files = Directory.EnumerateFiles(path, DirectoryEnumerationOptions.Recursive | DirectoryEnumerationOptions.LargeCache, new DirectoryEnumerationFilters()
			{
				InclusionFilter = (f) =>
				{
					return !ignoredFiles.Any(x => IgnoreFile(f.FullPath, x));
				}
			}).ToDictionary(k => k.Replace(dataRootPath, String.Empty), v => v);

			foreach (KeyValuePair<string, string> file in files)
			{
				DivinityApp.Log("Creating FilesystemFileInfo ");
				FilesystemFileInfo fileInfo = FilesystemFileInfo.CreateFromEntry(file.Value, file.Key);
				package.Files.Add(fileInfo);
			}
		}

		private static void WritePackage(PackageWriter writer, Package package, string outputPath)
		{
			try
			{
				//writer.WriteProgress += WriteProgressUpdate;
				writer.Version = PackageVersion.V13;
				writer.Compression = CompressionMethod.LZ4;
				writer.CompressionLevel = CompressionLevel.MaxCompression;
				writer.Write();
			}
			catch (Exception)
			{
				// ignored because an exception on a cancellation request 
				// cannot be avoided if the stream gets disposed afterwards 
			}
		}
		#endregion

		public static bool ExtractPackages(IEnumerable<string> pakPaths, string outputDirectory)
		{
			int success = 0;
			int count = pakPaths.Count();
			foreach(var path in pakPaths)
			{
				try
				{
					//Put each pak into its own folder
					string destination = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(path));

					//Unless the foldername == the pak name and we're only extracting one pak
					if(count == 1 && Path.GetDirectoryName(outputDirectory).Equals(Path.GetFileNameWithoutExtension(path)))
					{
						destination = outputDirectory;
					}
					var packager = new Packager();
					packager.UncompressPackage(path, destination, null);
					success++;
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error extracting package: {ex.ToString()}");
				}
			}
			return success >= count;
		}

		public static bool ExtractPackage(string pakPath, string outputDirectory)
		{
			try
			{
				var packager = new Packager();
				packager.UncompressPackage(pakPath, outputDirectory, null);
				return true;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error extracting package: {ex.ToString()}");
				return false;
			}
		}

		public static async Task<bool> ExtractPackageAsync(string pakPath, string outputDirectory, CancellationToken token)
		{
			var task = await Task.Run(async () =>
			{
				// execute actual operation in child task
				var childTask = Task.Factory.StartNew(() =>
				{
					try
					{
						var packager = new Packager();
						packager.UncompressPackage(pakPath, outputDirectory, null);
						return true;
					}
					catch (Exception) { return false; }
				}, TaskCreationOptions.AttachedToParent);

				var awaiter = childTask.GetAwaiter();
				while (!awaiter.IsCompleted)
				{
					await Task.Delay(0, token);
				}
				return childTask.Result;
			}, token);

			return task;
		}

		public static async Task<bool> WriteFileAsync(string path, string content)
		{
			try
			{
				using (System.IO.StreamWriter outputFile = new System.IO.StreamWriter(path))
				{
					await outputFile.WriteAsync(content);
				}
				return true;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error writing file: {ex.ToString()}");
				return false;
			}
		}
	}
}
