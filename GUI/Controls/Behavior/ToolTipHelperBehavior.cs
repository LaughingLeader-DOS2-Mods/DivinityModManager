using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace DivinityModManager.Controls.Behavior
{
	public class ToolTipHelperBehavior
	{
		public static bool GetDisableMouseEvents(DependencyObject element)
		{
			return (bool)element.GetValue(DisableMouseEventsProperty);
		}

		public static void SetDisableMouseEvents(DependencyObject element, bool value)
		{
			element.SetValue(DisableMouseEventsProperty, value);
		}

		public static readonly DependencyProperty DisableMouseEventsProperty =
			DependencyProperty.RegisterAttached(
			"DisableMouseEvents",
			typeof(bool),
			typeof(ScreenReaderHelperBehavior),
			new UIPropertyMetadata(false, OnDisableMouseEvents));

		static void OnDisableMouseEvents(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
		{
			if (depObj is UIElement element)
			{
				if ((bool)e.NewValue == true)
				{
					element.PreviewMouseDown += OnPreviewMouseDown;
				}
				else
				{
					element.PreviewMouseDown -= OnPreviewMouseDown;
				}
			}
		}

		private static void OnPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			e.Handled = true;
		}
	}
}
