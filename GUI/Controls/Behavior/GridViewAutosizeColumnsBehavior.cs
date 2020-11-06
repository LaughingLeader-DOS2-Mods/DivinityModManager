using DivinityModManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DivinityModManager.Controls.Behavior
{
	public class GridViewAutoSizeColumnsBehavior
	{
		public static bool GetGridViewAutoSizeColumns(ListView listView)
		{
			return (bool)listView.GetValue(GridViewAutoSizeColumnsProperty);
		}

		public static void SetGridViewAutoSizeColumns(ListView listView, bool value)
		{
			listView.SetValue(GridViewAutoSizeColumnsProperty, value);
		}

		public static readonly DependencyProperty GridViewAutoSizeColumnsProperty =
			DependencyProperty.RegisterAttached(
			"AutoSizeColumns",
			typeof(bool),
			typeof(GridViewAutoSizeColumnsBehavior),
			new UIPropertyMetadata(false, OnAutoSizeColumnsChanged));

		static void OnAutoSizeColumnsChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
		{
			if(depObj is ListView listView)
			{
				if(e.NewValue is bool enabled)
				{
					if(enabled)
					{
						listView.Loaded += OnDataChangedChanged;
						//listView.SizeChanged += OnGridViewSizeChanged;
					}
					else
					{
						listView.Loaded -= OnDataChangedChanged;
						//listView.SizeChanged -= OnGridViewSizeChanged;
					}
				}
			}
		}

		static void OnGridViewSizeChanged(object sender, RoutedEventArgs e)
		{
			if (sender is ListView listView)
			{
				if (listView.View is GridView gridView)
				{
					if (gridView.Columns.Count >= 2)
					{
						// take into account vertical scrollbar
						var actualWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth;
						DivinityApp.Log($"GridView actual width: {actualWidth}");

						for (Int32 i = 2; i < gridView.Columns.Count; i++)
						{
							DivinityApp.Log($"** GridView.Columns[{i}] actual width: {gridView.Columns[i].ActualWidth}");
							actualWidth = actualWidth - gridView.Columns[i].ActualWidth;
						}

						DivinityApp.Log($"GridView.Columns[1] next actual width: {actualWidth}");

						if (actualWidth > 0 && gridView.Columns.Count >= 2)
						{
							gridView.Columns[1].Width = actualWidth;
						}
					}
				}
			}
		}

		static void OnDataChangedChanged(object sender, EventArgs e)
		{
			if (sender is ListView listView)
			{
				if (listView.View is GridView gridView)
				{
					if (gridView.Columns.Count >= 2)
					{
						if (listView.ItemsSource is IEnumerable<DivinityModData> mods && mods.Count() > 0)
						{
							var longestName = mods.OrderByDescending(m => m.Name.Length).FirstOrDefault()?.Name;
							gridView.Columns[1].Width = MeasureText(longestName,
								listView.FontFamily,
								listView.FontStyle,
								listView.FontWeight,
								listView.FontStretch,
								listView.FontSize).Width;
						}
					}
				}
			}
		}

		private static Size MeasureTextSize(string text, FontFamily fontFamily, FontStyle fontStyle, 
			FontWeight fontWeight, FontStretch fontStretch, double fontSize)
		{
			var typeFace = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
			FormattedText ft = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeFace, fontSize, Brushes.Black);
			return new Size(ft.Width, ft.Height);
		}

		private static Size MeasureText(string text,
			FontFamily fontFamily,
			FontStyle fontStyle,
			FontWeight fontWeight,
			FontStretch fontStretch, double fontSize)
		{
			Typeface typeface = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
			GlyphTypeface glyphTypeface;

			if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
			{
				return MeasureTextSize(text, fontFamily, fontStyle, fontWeight, fontStretch, fontSize);
			}

			double totalWidth = 0;
			double height = 0;

			for (int n = 0; n < text.Length; n++)
			{
				ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[text[n]];

				double width = glyphTypeface.AdvanceWidths[glyphIndex] * fontSize;

				double glyphHeight = glyphTypeface.AdvanceHeights[glyphIndex] * fontSize;

				if (glyphHeight > height)
				{
					height = glyphHeight;
				}

				totalWidth += width;
			}

			return new Size(totalWidth, height);
		}
	}
}
