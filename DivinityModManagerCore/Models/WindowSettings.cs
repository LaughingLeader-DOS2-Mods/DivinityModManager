using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	public class WindowSettings
	{
		public bool Maximized { get; set; }
		public double X { get; set; }
		public double Y { get; set; }
		public int Screen { get; set; }

		public WindowSettings()
		{
			Maximized = false;
			X = 0;
			Y = 0;
			Screen = -1;
		}
	}
}
