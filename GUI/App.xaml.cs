﻿using DivinityModManager.Util;
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
using System.Windows.Input;
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
			// POCO type warning suppression
			Splat.Locator.CurrentMutable.Register(() => new DivinityModManager.Util.CustomPropertyResolver(), typeof(ICreatesObservableForProperty));
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

			EventManager.RegisterClassHandler(typeof(Window), Window.PreviewMouseDownEvent, new MouseButtonEventHandler(OnPreviewMouseDown));
		}

		private static void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			DivinityApp.IsKeyboardNavigating = false;
		}
	}
}