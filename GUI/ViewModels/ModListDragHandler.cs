using DivinityModManager.Models;

using GongSolutions.Wpf.DragDrop;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace DivinityModManager.ViewModels
{
	public class ManualDragInfo : IDragInfo
	{
		public DataFormat DataFormat { get; set; }
		public object Data { get; set; }
		public Point DragStartPosition { get; }
		public Point PositionInDraggedItem { get; }
		public DragDropEffects Effects { get; set; }
		public MouseButton MouseButton { get; set; }
		public System.Collections.IEnumerable SourceCollection { get; set; }
		public int SourceIndex { get; set; }
		public object SourceItem { get; set; }
		public System.Collections.IEnumerable SourceItems { get; set; }
		public CollectionViewGroup SourceGroup { get; set; }
		public UIElement VisualSource { get; set; }
		public UIElement VisualSourceItem { get; set; }
		public FlowDirection VisualSourceFlowDirection { get; set; }
		public object DataObject { get; set; }
		public Func<DependencyObject, object, DragDropEffects, DragDropEffects> DragDropHandler { get; set; }
		public DragDropKeyStates DragDropCopyKeyState { get; set; }
	}

	public class ModListDragHandler : DefaultDragHandler
	{
		private MainWindowViewModel _viewModel;

		public ModListDragHandler(MainWindowViewModel vm) : base()
		{
			_viewModel = vm;
		}

		public override void StartDrag(IDragInfo dragInfo)
		{
			//base.StartDrag(dragInfo);
			if (dragInfo != null)
			{
				dragInfo.Data = null;
				if (dragInfo.SourceCollection == _viewModel.ActiveMods)
				{
					var selected = _viewModel.ActiveMods.Where(x => x.IsSelected && x.Visibility == Visibility.Visible);
					dragInfo.Data = selected;
					//DivinityApp.LogMessage($"Drag source is ActiveMods | {selected.Count()}");
				}
				else if (dragInfo.SourceCollection == _viewModel.InactiveMods)
				{
					var selected = _viewModel.InactiveMods.Where(x => x.IsSelected && x.Visibility == Visibility.Visible && x.CanDrag);
					dragInfo.Data = selected;
					//DivinityApp.LogMessage($"Drag source is InactiveMods | {selected.Count()} | Classic: {selected.Where(x => x.IsClassicMod && x.CanDrag).Count()}");
				}
				dragInfo.Effects = dragInfo.Data != null ? DragDropEffects.Copy | DragDropEffects.Move : DragDropEffects.None;
			}
		}

		public override bool CanStartDrag(IDragInfo dragInfo)
		{
			if (dragInfo.Data is ISelectable d && !d.CanDrag)
			{
				return false;
			}
			else if (dragInfo.Data is IEnumerable<DivinityModData> modData)
			{
				if (modData.All(x => !x.CanDrag))
				{
					return false;
				}
			}
			return true;
		}
	}
}
