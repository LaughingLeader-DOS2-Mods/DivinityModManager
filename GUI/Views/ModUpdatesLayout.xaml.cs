using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DivinityModManager.ViewModels;
using ReactiveUI;

namespace DivinityModManager.Views
{
	public class ModUpdatesLayoutBase : ReactiveUserControl<ModUpdatesViewData> { }
	/// <summary>
	/// Interaction logic for ModUpdatesLayout.xaml
	/// </summary>
	public partial class ModUpdatesLayout : ModUpdatesLayoutBase
	{
		public ModUpdatesLayout()
		{
			InitializeComponent();

			Loaded += ModUpdatesLayout_Loaded;
			DataContextChanged += ModUpdatesLayout_DataContextChanged;
		}

		private void ModUpdatesLayout_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if(DataContext is ModUpdatesViewData vm)
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

		private void ModUpdatesLayout_Loaded(object sender, RoutedEventArgs e)
		{
			
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
