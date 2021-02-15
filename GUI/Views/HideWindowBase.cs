using AdonisUI.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace DivinityModManager.Views
{
	public class HideWindowBase : AdonisWindow
	{
		public HideWindowBase()
		{
			Closing += HideWindow_Closing;
			KeyDown += (o, e) =>
			{
				if (!e.Handled && e.Key == System.Windows.Input.Key.Escape)
				{
					if (Keyboard.FocusedElement == null || Keyboard.FocusedElement.GetType() != typeof(TextBox))
					{
						Hide();
					}
				}
			};
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
		}

		public virtual void HideWindow_Closing(object sender, CancelEventArgs e)
		{
			e.Cancel = true;
			Hide();
		}
	}
}
