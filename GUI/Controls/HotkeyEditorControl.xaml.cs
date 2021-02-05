using System;
using System.Collections.Generic;
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
using DivinityModManager.Models.App;

namespace DivinityModManager.Controls
{
	/// <summary>
	/// Interaction logic for HotkeyEditorControl.xaml
	/// </summary>
	public partial class HotkeyEditorControl : UserControl
	{
		public static readonly DependencyProperty HotkeyProperty =
		DependencyProperty.Register("Hotkey", typeof(Hotkey),
			typeof(HotkeyEditorControl));

		public Hotkey Hotkey
		{
			get => (Hotkey)GetValue(HotkeyProperty);
			set => SetValue(HotkeyProperty, value);
		}

		public static readonly DependencyProperty FocusReturnTargetProperty =
		DependencyProperty.Register("FocusReturnTarget", typeof(IInputElement),
			typeof(HotkeyEditorControl));

		public IInputElement FocusReturnTarget
		{
			get => (IInputElement)GetValue(FocusReturnTargetProperty);
			set => SetValue(FocusReturnTargetProperty, value);
		}

		private void SetKeybind(Key key = Key.None, ModifierKeys modifierKeys = ModifierKeys.None)
		{
			Hotkey.Key = key;
			Hotkey.Modifiers = modifierKeys;
			Hotkey.UpdateDisplayBindingText();
			Keyboard.ClearFocus();
			//FocusManager.SetFocusedElement(this, FocusReturnTarget);
			//FocusReturnTarget?.Focus();
			Keyboard.Focus(FocusReturnTarget);
		}

		private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			// Don't let the event pass further
			// because we don't want standard textbox shortcuts working
			e.Handled = true;

			// Get modifiers and key data
			var modifiers = Keyboard.Modifiers;
			var key = e.Key;

			// When Alt is pressed, SystemKey is used instead
			if (key == Key.System)
			{
				key = e.SystemKey;
			}

			// Pressing delete, backspace or escape without modifiers clears the current value
			if (modifiers == ModifierKeys.None &&
				(key == Key.Delete || key == Key.Back || key == Key.Escape))
			{
				SetKeybind();
				return;
			}

			// If no actual key was pressed - return
			if (key == Key.LeftCtrl ||
				key == Key.RightCtrl ||
				key == Key.LeftAlt ||
				key == Key.RightAlt ||
				key == Key.LeftShift ||
				key == Key.RightShift ||
				key == Key.LWin ||
				key == Key.RWin ||
				key == Key.Clear ||
				key == Key.OemClear ||
				key == Key.Apps)
			{
				return;
			}

			// Update the value
			SetKeybind(key, modifiers);
		}

		public HotkeyEditorControl()
		{
			InitializeComponent();
		}
	}
}
