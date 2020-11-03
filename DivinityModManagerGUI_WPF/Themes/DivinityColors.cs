using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DivinityModManager.Resources
{
	public static class DivinityColors
	{
		public static ComponentResourceKey TagBackgroundColor => new ComponentResourceKey(typeof(DivinityColors), "TagBackgroundColor");
		public static ComponentResourceKey CustomTagBackgroundColor => new ComponentResourceKey(typeof(DivinityColors), "CustomTagBackgroundColor");
		public static ComponentResourceKey ModeTagBackgroundColor => new ComponentResourceKey(typeof(DivinityColors), "ModeTagBackgroundColor");

		public static ComponentResourceKey TagBackgroundBrush => new ComponentResourceKey(typeof(DivinityColors), "TagBackgroundBrush");
		public static ComponentResourceKey CustomTagBackgroundBrush => new ComponentResourceKey(typeof(DivinityColors), "CustomTagBackgroundBrush");
		public static ComponentResourceKey ModeTagBackgroundBrush => new ComponentResourceKey(typeof(DivinityColors), "ModeTagBackgroundBrush");
	}
}
