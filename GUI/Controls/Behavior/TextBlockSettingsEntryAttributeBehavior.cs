using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DivinityModManager.Controls.Behavior
{
	public class TextBlockSettingsEntryAttributeBehavior
	{
		public static string GetTarget(DependencyObject element)
		{
			return (string)element.GetValue(TargetProperty);
		}

		public static void SetTarget(DependencyObject element, string value)
		{
			element.SetValue(TargetProperty, value);
		}

		public static readonly DependencyProperty TargetProperty =
			DependencyProperty.RegisterAttached(
			"Target",
			typeof(string),
			typeof(ScreenReaderHelperBehavior),
			new UIPropertyMetadata("", OnTargetSet));

		static void OnTargetSet(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
		{
			if (depObj is TextBlock element && e.NewValue is string propName && !String.IsNullOrEmpty(propName))
			{
				if(element.DataContext != null)
				{
					PropertyInfo prop = element.DataContext.GetType().GetProperty(propName);
					SettingsEntryAttribute settingsEntry = prop.GetCustomAttribute<SettingsEntryAttribute>();
					element.Text = settingsEntry.DisplayName;
					element.ToolTip = settingsEntry.Tooltip;
				}
				else
				{
					element.DataContextChanged += OnDataContextChanged;
				}
			}
		}

		private static void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is TextBlock element && e.NewValue != null)
			{
				string propName = (string)element.GetValue(TargetProperty);
				PropertyInfo prop = element.DataContext.GetType().GetProperty(propName);
				SettingsEntryAttribute settingsEntry = prop.GetCustomAttribute<SettingsEntryAttribute>();
				element.Text = settingsEntry.DisplayName;
				element.ToolTip = settingsEntry.Tooltip;
			}
		}
	}
}
