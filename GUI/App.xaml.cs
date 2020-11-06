using DivinityModManager.Util;
using DivinityModManager.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace DivinityModManager
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			WebHelper.SetupClient();
#if DEBUG
			RxApp.SuppressViewCommandBindingMessage = false;
#else
			RxApp.DefaultExceptionHandler = new RxExceptionHandler();
			RxApp.SuppressViewCommandBindingMessage = true;
#endif
		}
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			//For making date display use the current system's culture
			FrameworkElement.LanguageProperty.OverrideMetadata(
				typeof(FrameworkElement),
				new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
		}
	}
}
