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
using System.Windows.Interop;

namespace DivinityModManager.Views
{
	public class HideWindowBase : AdonisWindow
	{
		[DllImport("user32.dll")]
		static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
		[DllImport("user32.dll")]
		static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

		const uint MF_BYCOMMAND = 0x00000000;
		const uint MF_GRAYED = 0x00000001;
		const uint MF_ENABLED = 0x00000000;

		const uint SC_CLOSE = 0xF060;

		const int WM_SHOWWINDOW = 0x00000018;
		const int WM_CLOSE = 0x10;

		public HideWindowBase()
		{
			Closing += HideWindow_Closing;
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			//HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;

			//if (hwndSource != null)
			//{
			//	hwndSource.AddHook(new HwndSourceHook(this.hwndSourceHook));
			//}
		}

		IntPtr hwndSourceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == WM_SHOWWINDOW)
			{
				IntPtr hMenu = GetSystemMenu(hwnd, false);
				if (hMenu != IntPtr.Zero)
				{
					EnableMenuItem(hMenu, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
				}
			}
			//else if (msg == WM_CLOSE)
			//{
			//	handled = true;
			//}
			return IntPtr.Zero;
		}

		public virtual void HideWindow_Closing(object sender, CancelEventArgs e)
		{
			e.Cancel = true;
			Hide();
		}
	}
}
