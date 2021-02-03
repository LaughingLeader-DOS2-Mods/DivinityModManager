using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace DivinityModManager.Util
{
	public static class BindingHelper
	{
		public static void CreateCommandBinding(Button button, string vmProperty, object source)
		{
			Binding binding = new Binding(vmProperty);
			binding.Source = source;
			binding.Mode = BindingMode.OneWay;
			button.SetBinding(Button.CommandProperty, binding);
		}
		public static void CreateCommandBinding(MenuItem button, string vmProperty, object source)
		{
			Binding binding = new Binding(vmProperty);
			binding.Source = source;
			binding.Mode = BindingMode.OneWay;
			button.SetBinding(MenuItem.CommandProperty, binding);
		}
	}
}
