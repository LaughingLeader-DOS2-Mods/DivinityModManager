using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DivinityModManager.Converters
{
	class DivinityGameLaunchWindowActionToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (DivinityGameLaunchWindowAction)Enum.Parse(typeof(DivinityGameLaunchWindowAction), value.ToString(), true);
        }
    }
}
