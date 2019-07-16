using DivinityModManager.Models;
using DivinityModManager.Views;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DivinityModManager.Converters
{
	public class ModIsAvailableConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value is IDivinityModData data)
			{
				return MainWindow.Self?.ViewModel.ModIsAvailable(data);
			}

			return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
