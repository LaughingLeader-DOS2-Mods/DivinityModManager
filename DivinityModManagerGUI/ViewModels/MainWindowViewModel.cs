using Avalonia.Collections;
using DivinityModManager.Models;
using DivinityModManager.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ReactiveUI;
using DynamicData;

namespace DivinityModManager.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
		public string Title => "Divinity Mod Manager 1.0.0.0";

		public string Greeting => "Hello World!";

		public AvaloniaList<DivinityModData> Mods { get; set; } = new AvaloniaList<DivinityModData>();

		public AvaloniaList<DivinityProfileData> Profiles { get; set; } = new AvaloniaList<DivinityProfileData>();

		private void traceNewMods(List<DivinityModData> mods)
		{
			foreach (var mod in mods)
			{
				Trace.WriteLine($"Found mod. Name({mod.Name}) Author({mod.Author}) Version({mod.Version}) UUID({mod.UUID}) Description({mod.Description})");
				foreach (var dependency in mod.Dependencies)
				{
					Console.WriteLine($"  {dependency.ToString()}");
				}
			}
		}

		public MainWindowViewModel() : base()
		{
			var projects = DivinityModDataLoader.LoadEditorProjects(@"G:\Divinity Original Sin 2\DefEd\Data\Mods");
			Mods.AddRange(projects);
			//traceNewMods(projects);

			var modPakData = DivinityModDataLoader.LoadModPackageData(@"D:\Users\LaughingLeader\Documents\Larian Studios\Divinity Original Sin 2 Definitive Edition\Mods");
			var pakEditorConflicts = modPakData.Where(m => !projects.Any(p => p.UUID == m.UUID));
			//Editor mods have priority over paks
			Mods.AddRange(modPakData.Where(m => !pakEditorConflicts.Contains(m)));
			traceNewMods(modPakData);


		}
    }
}
