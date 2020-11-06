using DivinityModManager.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DivinityModManager.Util
{
	class RxExceptionHandler : IObserver<Exception>
	{
		public static MainWindow view { get; set; }
		public void OnNext(Exception value)
		{
			//if (Debugger.IsAttached) Debugger.Break();

			var message = $"(OnNext) Exception encountered:\nType: {value.GetType().ToString()}\tMessage: {value.Message}\nSource: {value.Source}\nStackTrace: {value.StackTrace}";
			DivinityApp.Log(message);
			MessageBox.Show(message, "Error Encountered", MessageBoxButton.OK, MessageBoxImage.Error);
			//MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(view, message, "Error Encountered", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, view.MainWindowMessageBox_OK.Style);
			//RxApp.MainThreadScheduler.Schedule(() => { throw value; });
		}

		public void OnError(Exception value)
		{
			var message = $"(OnError) Exception encountered:\nType: {value.GetType().ToString()}\tMessage: {value.Message}\nSource: {value.Source}\nStackTrace: {value.StackTrace}";
			DivinityApp.Log(message);
			//MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(view, message, "Error Encountered", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, view.MainWindowMessageBox_OK.Style);
		}

		public void OnCompleted()
		{
			//if (Debugger.IsAttached) Debugger.Break();
			//RxApp.MainThreadScheduler.Schedule(() => { throw new NotImplementedException(); });
		}
	}
}
