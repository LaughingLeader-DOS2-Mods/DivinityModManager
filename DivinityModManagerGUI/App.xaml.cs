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
		}
   }
}