using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DivinityModManager.Util
{
	public static class FocusHelper
	{
		public static bool HasKeyboardFocus(FrameworkElement element)
		{
			if (element == null) return false;
			if(element.IsKeyboardFocused || element.IsKeyboardFocusWithin)
			{
				return true;
			}
			foreach(var child in element.FindVisualChildren<FrameworkElement>())
			{
				if(HasKeyboardFocus(child))
				{
					return true;
				}
			}
			return false;
		}
	}
}
