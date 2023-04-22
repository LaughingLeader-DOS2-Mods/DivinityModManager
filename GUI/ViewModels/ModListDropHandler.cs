using DivinityModManager.Models;

using GongSolutions.Wpf.DragDrop;
using GongSolutions.Wpf.DragDrop.Utilities;

using ReactiveUI;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DivinityModManager.ViewModels
{
	public class ManualDropInfo : IDropInfo
	{
		public object Data { get; private set; }
		public IDragInfo DragInfo { get; }
		public Point DropPosition { get; }
		public Type DropTargetAdorner { get; set; }
		public DragDropEffects Effects { get; set; }
		public int InsertIndex { get; }
		public int UnfilteredInsertIndex { get; }
		public System.Collections.IEnumerable TargetCollection { get; set; }
		public object TargetItem { get; }
		public CollectionViewGroup TargetGroup { get; }
		public UIElement VisualTarget { get; }
		public UIElement VisualTargetItem { get; }
		public Orientation VisualTargetOrientation { get; }
		public FlowDirection VisualTargetFlowDirection { get; }
		public string DestinationText { get; set; }
		public string EffectText { get; set; }
		public RelativeInsertPosition InsertPosition { get; }
		public DragDropKeyStates KeyStates { get; }
		public bool NotHandled { get; set; }
		public bool IsSameDragDropContextAsSource { get; }
		public EventType EventType { get; }
		object IDropInfo.Data
		{
			get => Data;
			set => Data = value;
		}

		private readonly ScrollViewer _targetScrollViewer;
		private readonly ScrollingMode _targetScrollingMode;

		ScrollViewer IDropInfo.TargetScrollViewer => _targetScrollViewer;
		ScrollingMode IDropInfo.TargetScrollingMode => _targetScrollingMode;

		public ManualDropInfo(List<DivinityModData> data, int index, UIElement visualTarget, System.Collections.IEnumerable targetCollection, System.Collections.IEnumerable sourceCollection)
		{
			UnfilteredInsertIndex = index;
			VisualTarget = visualTarget;
			TargetCollection = targetCollection;
			Data = data;
			var scrollViewer = visualTarget.FindVisualChildren<ScrollViewer>().FirstOrDefault();
			if (scrollViewer != null)
			{
				_targetScrollViewer = scrollViewer;
				_targetScrollingMode = ScrollingMode.VerticalOnly;
			}
			DragInfo = new ManualDragInfo()
			{
				SourceCollection = sourceCollection,
				Data = data
			};
		}
	}


	public class ModListDropHandler : DefaultDropHandler
	{
		override public void Drop(IDropInfo dropInfo)
		{
			if (dropInfo == null || dropInfo.DragInfo == null || _viewModel.IsLoadingOrder || _viewModel.IsRefreshing)
			{
				return;
			}

			var insertIndex = dropInfo.UnfilteredInsertIndex;

			var itemsControl = dropInfo.VisualTarget as ItemsControl;
			if (itemsControl != null && itemsControl.Items is IEditableCollectionView editableItems)
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
				if (insertIndex < 0)
				{
					insertIndex = 0;
				}

				var objects2Insert = new List<object>();
				foreach (var o in data)
				{
					var obj2Insert = o;
					objects2Insert.Add(obj2Insert);
					try
					{
						if (insertIndex < destinationList.Count)
						{
							destinationList.Insert(insertIndex, obj2Insert);
							insertIndex++;
						}
						else
						{
							destinationList.Add(obj2Insert);
						}
					}
					catch (Exception ex)
					{
						DivinityApp.Log($"Error adding drop operation item to destinationList at {insertIndex}:\n{ex}");
						destinationList.Add(obj2Insert);
					}
				}

				var selectDroppedItems = itemsControl is TabControl || (itemsControl != null && GongSolutions.Wpf.DragDrop.DragDrop.GetSelectDroppedItems(itemsControl));
				if (selectDroppedItems)
				{
					SelectDroppedItems(dropInfo, objects2Insert);
				}
			}

			bool isActive = dropInfo.TargetCollection == _viewModel.ActiveMods;
			var selectedUUIDs = data.Select(x => x.UUID).ToHashSet();

			foreach (var mod in _viewModel.ActiveMods)
			{
				mod.Index = _viewModel.ActiveMods.IndexOf(mod);
			}

			foreach (var mod in _viewModel.Mods)
			{
				if (selectedUUIDs.Contains(mod.UUID))
				{
					mod.IsActive = isActive;
					mod.IsSelected = true;
				}
				else
				{
					mod.IsSelected = false;
				}
			}

			RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(20), () =>
			{
				_viewModel.Layout.SelectMods(data, isActive);
			});

			if (isActive)
			{
				_viewModel.OnFilterTextChanged(_viewModel.ActiveModFilterText, _viewModel.ActiveMods);
				//_viewModel.Layout.FixActiveModsScrollbar();
			}
			else
			{
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
		}

		private readonly MainWindowViewModel _viewModel;

		public ModListDropHandler(MainWindowViewModel vm) : base()
		{
			_viewModel = vm;
		}
	}
}
