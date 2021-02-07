using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DivinityModManager.Themes
{
	public static class DivinityColors
	{
		public static ComponentResourceKey TagBackgroundColor => new ComponentResourceKey(typeof(DivinityColors), "TagBackgroundColor");
		public static ComponentResourceKey CustomTagBackgroundColor => new ComponentResourceKey(typeof(DivinityColors), "CustomTagBackgroundColor");
		public static ComponentResourceKey ModeTagBackgroundColor => new ComponentResourceKey(typeof(DivinityColors), "ModeTagBackgroundColor");
		public static ComponentResourceKey ListInactiveColor => new ComponentResourceKey(typeof(DivinityColors), "ListInactiveColor");

		public static ComponentResourceKey TagBackgroundBrush => new ComponentResourceKey(typeof(DivinityColors), "TagBackgroundBrush");
		public static ComponentResourceKey CustomTagBackgroundBrush => new ComponentResourceKey(typeof(DivinityColors), "CustomTagBackgroundBrush");
		public static ComponentResourceKey ModeTagBackgroundBrush => new ComponentResourceKey(typeof(DivinityColors), "ModeTagBackgroundBrush");
		public static ComponentResourceKey ListInactiveBrush => new ComponentResourceKey(typeof(DivinityColors), "ListInactiveBrush");

		public static ComponentResourceKey ListInactiveRectangle => new ComponentResourceKey(typeof(DivinityColors), "ListInactiveRectangle");
	}
}
