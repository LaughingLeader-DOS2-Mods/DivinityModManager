using DivinityModManager.Models;

using GongSolutions.Wpf.DragDrop;
using GongSolutions.Wpf.DragDrop.Utilities;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;

namespace DivinityModManager.ViewModels
{
	public class ModListDropHandler : DefaultDropHandler
	{
		override public void Drop(IDropInfo dropInfo)
		{
			if (dropInfo == null || dropInfo.DragInfo == null)
			{
				return;
			}

			var insertIndex = dropInfo.UnfilteredInsertIndex;

			var itemsControl = dropInfo.VisualTarget as ItemsControl;
			if (itemsControl != null)
			{
				var editableItems = itemsControl.Items as IEditableCollectionView;
				if (editableItems != null)
				{
					var newItemPlaceholderPosition = editableItems.NewItemPlaceholderPosition;
					if (newItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning && insertIndex == 0)
					{
						++insertIndex;
					}
					else if (newItemPlaceholderPosition == NewItemPlaceholderPosition.AtEnd && insertIndex == itemsControl.Items.Count)
					{
						--insertIndex;
					}
				}
			}

			var destinationList = dropInfo.TargetCollection.TryGetList();
			var data = ExtractData(dropInfo.Data).OfType<DivinityModData>().ToList();

			var sourceList = dropInfo.DragInfo.SourceCollection.TryGetList();
			if (sourceList != null)
			{
				foreach (var o in data)
				{
					var index = sourceList.IndexOf(o);
					if (index != -1)
					{
						sourceList.RemoveAt(index);
						// so, is the source list the destination list too ?
						if (destinationList != null && Equals(sourceList, destinationList) && index < insertIndex)
						{
							--insertIndex;
						}
					}
				}
			}

			if (destinationList != null)
			{
				var objects2Insert = new List<object>();
				foreach (var o in data)
				{
					var obj2Insert = o;
					objects2Insert.Add(obj2Insert);
					destinationList.Insert(insertIndex++, obj2Insert);
				}

				var selectDroppedItems = itemsControl is TabControl || (itemsControl != null && GongSolutions.Wpf.DragDrop.DragDrop.GetSelectDroppedItems(itemsControl));
				if (selectDroppedItems)
				{
					SelectDroppedItems(dropInfo, objects2Insert);
				}
			}

			bool isActive = dropInfo.TargetCollection == _viewModel.ActiveMods;

			foreach (var mod in _viewModel.Mods)
			{
				if (data.Contains(mod))
				{
					mod.IsActive = isActive;
					mod.IsSelected = true;
				}
				else
				{
					mod.IsSelected = false;
				}
			}

			if (isActive)
			{
				_viewModel.Layout.UpdateViewSelection(_viewModel.ActiveMods);
				_viewModel.OnFilterTextChanged(_viewModel.ActiveModFilterText, _viewModel.ActiveMods);
				//_viewModel.Layout.FixActiveModsScrollbar();
			}
			else
			{
				_viewModel.Layout.UpdateViewSelection(_viewModel.InactiveMods);
				_viewModel.OnFilterTextChanged(_viewModel.InactiveModFilterText, _viewModel.InactiveMods);
			}

			if (_viewModel.SelectedModOrder != null)
			{
				_viewModel.SelectedModOrder.Order.Clear();
				foreach (var x in _viewModel.ActiveMods)
				{
					_viewModel.SelectedModOrder.Add(x);
				}
			}

			_viewModel.OnOrderChanged?.Invoke(_viewModel, new EventArgs());
		}

		private MainWindowViewModel _viewModel;

		public ModListDropHandler(MainWindowViewModel vm) : base()
		{
			_viewModel = vm;
		}
	}
}
