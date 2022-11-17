using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;

namespace DivinityModManager.Converters
{
	public class BoolToVisibilityConverter : IValueConverter
	{
		public static Visibility FromBool(bool b) => b ? Visibility.Visible : Visibility.Collapsed;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool reverse = false;
			if (parameter != null)
			{
				if (parameter is int reverseInt)
				{
					reverse = reverseInt > 0;
				}
				else if (parameter is bool r)
				{
					reverse = r;
				}
				DivinityApp.Log($"BoolToVisibilityConverter param: {parameter} | {parameter.GetType()}");
			}

			if (value is bool b)
			{
				if (!reverse)
				{
					return b ? Visibility.Visible : Visibility.Collapsed;
				}
				else
				{
					return !b ? Visibility.Visible : Visibility.Collapsed;
				}
			}
			return Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Visibility visbility)
			{
				if (visbility == Visibility.Visible)
				{
					return true;
				}
			}
			return false;
		}
	}

	public class BoolToVisibilityConverterReversed : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool b)
			{
				return !b ? Visibility.Visible : Visibility.Collapsed;
			}
			return Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Visibility visbility)
			{
				if (visbility == Visibility.Visible)
				{
					return false;
				}
			}
			return true;
		}
	}
}
