using Avalonia;
using Avalonia.Markup.Xaml;
using DivinityModManager.Util;
using System;

namespace DivinityModManager
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

			var projects = DivinityModDataLoader.LoadEditorProjects(@"G:\Divinity Original Sin 2\DefEd\Data\Mods");
			foreach (var mod in projects)
			{
				Console.WriteLine($"Found mod. Name({mod.Name}) Author({mod.Author}) Version({mod.Version}) UUID({mod.UUID}) Description({mod.Description})");
				foreach (var dependency in mod.Dependencies)
				{
					Console.WriteLine($"  {dependency.ToString()}");
				}
			}
		}
   }
}