using DivinityModManager.Models.App;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DivinityModManager.ViewModels
{
	public class AppKeys : ReactiveObject
	{
		[MenuSettings("File", "Save Order")]
		[Reactive] public Hotkey Save { get; set; } = new Hotkey(Key.S, ModifierKeys.Control);

		[MenuSettings("File", "Save Order As...")]
		[Reactive] public Hotkey SaveAs { get; set; } = new Hotkey(Key.S, ModifierKeys.Control | ModifierKeys.Alt);

		[MenuSettings("File", "Import Order from Save...")]
		[Reactive] public Hotkey ImportOrderFromSave { get; set; } = new Hotkey(Key.O, ModifierKeys.Control);

		[MenuSettings("File", "Import Order from File...")]
		[Reactive] public Hotkey ImportOrderFromFile { get; set; } = new Hotkey(Key.O, ModifierKeys.Control | ModifierKeys.Shift);
		[Reactive] public Hotkey ExportOrderToGame { get; set; } = new Hotkey(Key.E, ModifierKeys.Control);
		[Reactive] public Hotkey ExportOrderToList { get; set; } = new Hotkey(Key.L, ModifierKeys.Control);
		[Reactive] public Hotkey NewOrder { get; set; } = new Hotkey(Key.N, ModifierKeys.Control);
		[Reactive] public Hotkey OpenPreferences { get; set; } = new Hotkey(Key.P, ModifierKeys.Control);
		[Reactive] public Hotkey Refresh { get; set; } = new Hotkey(Key.F5);
		[Reactive] public Hotkey OpenAboutWindow { get; set; } = new Hotkey(Key.F1);
		[Reactive] public Hotkey CheckForUpdates { get; set; } = new Hotkey(Key.F7);
		[Reactive] public Hotkey OpenDonationLink { get; set; } = new Hotkey(Key.F10);
		[Reactive] public Hotkey ToggleUpdatesView { get; set; } = new Hotkey(Key.U, ModifierKeys.Control);
		[Reactive] public Hotkey ToggleFilterFocus { get; set; } = new Hotkey(Key.F, ModifierKeys.Control);
		[Reactive] public Hotkey ToggleViewTheme { get; set; } = new Hotkey(Key.L, ModifierKeys.Control);
		[Reactive] public Hotkey ToggleVersionGeneratorWindow { get; set; } = new Hotkey(Key.G, ModifierKeys.Control);
		[Reactive] public Hotkey Confirm { get; set; } = new Hotkey(Key.Enter);
		[Reactive] public Hotkey MoveFocusLeft { get; set; } = new Hotkey(Key.Left);
		[Reactive] public Hotkey MoveFocusRight { get; set; } = new Hotkey(Key.Right);
		[Reactive] public Hotkey MoveToTop { get; set; } = new Hotkey(Key.PageUp, ModifierKeys.Control);
		[Reactive] public Hotkey MoveToBottom { get; set; } = new Hotkey(Key.PageDown, ModifierKeys.Control);
		[Reactive] public Hotkey SpeakActiveModOrder { get; set; } = new Hotkey(Key.Home, ModifierKeys.Control);

		private List<Hotkey> allKeys = new List<Hotkey>();
		public List<Hotkey> All => allKeys;

		public AppKeys()
		{
			Type t = typeof(AppKeys);
			allKeys.AddRange(t.GetRuntimeProperties()
			.Where(prop => Attribute.IsDefined(prop, typeof(ReactiveAttribute)))
			.Select(prop => t.GetProperty(prop.Name).GetValue(this)).Cast<Hotkey>());
		}
	}
}
