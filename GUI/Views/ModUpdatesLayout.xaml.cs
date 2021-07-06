using AdonisUI;
using DivinityModManager.ViewModels;
using Microsoft.Windows.Themes;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DivinityModManager.Views
{
	public class ModUpdatesLayoutBase : ReactiveUserControl<ModUpdatesViewData> { }
	/// <summary>
	/// Interaction logic for ModUpdatesLayout.xaml
	/// </summary>
	public partial class ModUpdatesLayout : ModUpdatesLayoutBase
	{
		public static ModUpdatesLayout Instance { get; private set; }
		public ModUpdatesLayout()
		{
			InitializeComponent();

			Instance = this;

			Loaded += ModUpdatesLayout_Loaded;
			DataContextChanged += ModUpdatesLayout_DataContextChanged;
		}

		private void ModUpdatesLayout_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue != null && e.NewValue is ModUpdatesViewData vm)
			{
				ViewModel = vm;

				ViewModel.OnLoaded = new Action(() =>
				{
					var newModsGridRowDef = UpdateGrid.RowDefinitions.FirstOrDefault(x => x.Name == "NewModsGridRow");

					if (newModsGridRowDef != null)
					{
						if (!ViewModel.NewAvailable)
						{
							newModsGridRowDef.Height = new GridLength(75, GridUnitType.Pixel);
						}
						else
						{
							newModsGridRowDef.Height = new GridLength(1, GridUnitType.Star);
						}
					}

					var updatesGridRowDef = UpdateGrid.RowDefinitions.FirstOrDefault(x => x.Name == "UpdatesGridRow");

					if (updatesGridRowDef != null)
					{
						if (!ViewModel.UpdatesAvailable)
						{
							updatesGridRowDef.Height = new GridLength(75, GridUnitType.Pixel);
						}
						else
						{
							updatesGridRowDef.Height = new GridLength(2, GridUnitType.Star);
						}
					}
				});
			}
		}

		private List<string> ignoreColors = new List<string>{"#FFEDEDED", "#00FFFFFF", "#FFFFFFFF", "#FFF4F4F4", "#FFE8E8E8", "#FF000000" };

		public void UpdateBackgroundColors()
		{
			//Fix for IsEnabled False ListView having a system color border background we can't change.
			foreach (var border in this.FindVisualChildren<ClassicBorderDecorator>())
			{
				border.SetResourceReference(BackgroundProperty, Brushes.Layer4BackgroundBrush);
			}

			//foreach (var c in this.FindVisualChildren<Control>())
			//{
			//	if(c.Background != null)
			//	{
			//		DivinityApp.Log($"{c.GetType()} ({c.Name}) | Background: {c.Background.ToString().Replace("#FF", "#")}");
			//	}
			//	//c.SetResourceReference(BackgroundProperty, Brushes.Layer4BackgroundBrush);
			//}
		}
		private void ModUpdatesLayout_Loaded(object sender, RoutedEventArgs e)
		{
			UpdateBackgroundColors();
		}

		GridViewColumnHeader _lastHeaderClicked = null;
		ListSortDirection _lastDirection = ListSortDirection.Ascending;

		private void Sort(string sortBy, ListSortDirection direction, object sender, bool modUpdatesGrid = false)
		{
			if (sortBy == "Version" || sortBy == "Current") sortBy = "Version.Version";
			if (sortBy == "New") sortBy = "WorkshopMod.Version.Version";
			if (sortBy == "#") sortBy = "Index";
			
			if (modUpdatesGrid && sortBy != "IsSelected" && sortBy != "WorkshopMod.Version.Version") 
			{
				sortBy = "LocalMod." + sortBy;
			}

			if (sortBy != "")
			{
				try
				{
					ListView lv = sender as ListView;
					ICollectionView dataView =
						CollectionViewSource.GetDefaultView(lv.ItemsSource);

					dataView.SortDescriptions.Clear();
					SortDescription sd = new SortDescription(sortBy, direction);
					dataView.SortDescriptions.Add(sd);
					dataView.Refresh();
				}
				catch (Exception ex)
				{
					DivinityApp.Log("Error sorting grid: " + ex.ToString());
				}
			}
		}

		private void SortGrid(object sender, RoutedEventArgs e, bool modUpdatesGrid = false)
		{
			GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
			ListSortDirection direction;

			if (headerClicked != null)
			{
				if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
				{
					if (headerClicked != _lastHeaderClicked)
					{
						direction = ListSortDirection.Ascending;
					}
					else
					{
						if (_lastDirection == ListSortDirection.Ascending)
						{
							direction = ListSortDirection.Descending;
						}
						else
						{
							direction = ListSortDirection.Ascending;
						}
					}

					string header = "";

					if (headerClicked.Column.Header is TextBlock textBlock)
					{
						header = textBlock.Text;
					}
					else if (headerClicked.Column.Header is string gridHeader)
					{
						header = gridHeader;
					}
					else if (headerClicked.Column.Header is CheckBox selectionHeader)
					{
						header = "IsSelected";
					}
					else if (headerClicked.Column.Header is Control c && c.ToolTip is string toolTip)
					{
						header = toolTip;
					}

					Sort(header, direction, sender, modUpdatesGrid);

					_lastHeaderClicked = headerClicked;
					_lastDirection = direction;
				}
			}
		}

		private void SortNewModsGridView(object sender, RoutedEventArgs e)
		{
			SortGrid(sender, e);
		}

		private void SortModUpdatesGridView(object sender, RoutedEventArgs e)
		{
			SortGrid(sender, e, true);
		}
	}
}
