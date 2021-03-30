using Alphaleonis.Win32.Filesystem;

using DivinityModManager.Models.App;
using DivinityModManager.Util;

using DynamicData;

using Newtonsoft.Json;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DivinityModManager.ViewModels
{
	public class AppKeys : ReactiveObject
	{
		[MenuSettings("File", "Add New Order", true)]
		[Reactive] public Hotkey NewOrder { get; set; } = new Hotkey(Key.N, ModifierKeys.Control);

		[MenuSettings("File", "Save Order")]
		[Reactive] public Hotkey Save { get; set; } = new Hotkey(Key.S, ModifierKeys.Control);

		[MenuSettings("File", "Save Order As...", true)]
		[Reactive] public Hotkey SaveAs { get; set; } = new Hotkey(Key.S, ModifierKeys.Control | ModifierKeys.Alt);

		[MenuSettings("File", "Import Order from Save...")]
		[Reactive] public Hotkey ImportOrderFromSave { get; set; } = new Hotkey(Key.I, ModifierKeys.Control);

		[MenuSettings("File", "Import Order from Save As New Order...")]
		[Reactive] public Hotkey ImportOrderFromSaveAsNew { get; set; } = new Hotkey(Key.I, ModifierKeys.Control | ModifierKeys.Shift);

		[MenuSettings("File", "Import Order from File...")]
		[Reactive] public Hotkey ImportOrderFromFile { get; set; } = new Hotkey(Key.O, ModifierKeys.Control | ModifierKeys.Shift);

		[MenuSettings("File", "Import Order from Zip...", true)]
		[Reactive] public Hotkey ImportOrderFromZipFile { get; set; } = new Hotkey(Key.None);

		[MenuSettings("File", "Load Order From Selected GM Campaign", true)]
		[Reactive] public Hotkey ImportOrderFromSelectedGMCampaign { get; set; } = new Hotkey(Key.None);

		[MenuSettings("File", "Export Order to Game")]
		[Reactive] public Hotkey ExportOrderToGame { get; set; } = new Hotkey(Key.E, ModifierKeys.Control);

		[MenuSettings("File", "Export Order to Text File...")]
		[Reactive] public Hotkey ExportOrderToList { get; set; } = new Hotkey(Key.L, ModifierKeys.Control);

		[MenuSettings("File", "Export Order to Archive (.zip)")]
		[Reactive] public Hotkey ExportOrderToZip { get; set; } = new Hotkey(Key.R, ModifierKeys.Control);

		[MenuSettings("File", "Export Order to Archive As...", true)]
		[Reactive] public Hotkey ExportOrderToArchiveAs { get; set; } = new Hotkey(Key.R, ModifierKeys.Control | ModifierKeys.Shift);

		[MenuSettings("File", "Reload All")]
		[Reactive] public Hotkey Refresh { get; set; } = new Hotkey(Key.F5);

		[MenuSettings("Edit", "Moved Selected Mods to Opposite List", true)]
		[Reactive] public Hotkey Confirm { get; set; } = new Hotkey(Key.Enter);

		[MenuSettings("Edit", "Focus Active Mods List")]
		[Reactive] public Hotkey MoveFocusLeft { get; set; } = new Hotkey(Key.Left);

		[MenuSettings("Edit", "Focus Inactive Mods List")]
		[Reactive] public Hotkey MoveFocusRight { get; set; } = new Hotkey(Key.Right);

		[MenuSettings("Edit", "Go to Other List")]
		[Reactive] public Hotkey SwapListFocus { get; set; } = new Hotkey(Key.Tab);

		[MenuSettings("Edit", "Move to Top of Active List")]
		[Reactive] public Hotkey MoveToTop { get; set; } = new Hotkey(Key.PageUp, ModifierKeys.Control);

		[MenuSettings("Edit", "Move to Bottom of Active List", true)]
		[Reactive] public Hotkey MoveToBottom { get; set; } = new Hotkey(Key.PageDown, ModifierKeys.Control);

		[MenuSettings("Edit", "Toggle Focus Filter for Current List", AddSeparator = true)]
		[Reactive] public Hotkey ToggleFilterFocus { get; set; } = new Hotkey(Key.F, ModifierKeys.Control);

		[MenuSettings("Edit", "Show File Names for Mods")]
		[Reactive] public Hotkey ToggleFileNameDisplay { get; set; } = new Hotkey(Key.None);

		[MenuSettings("Settings", "Open Preferences")]
		[Reactive] public Hotkey OpenPreferences { get; set; } = new Hotkey(Key.P, ModifierKeys.Control);

		[MenuSettings("Settings", "Open Keyboard Shortcuts")]
		[Reactive] public Hotkey OpenKeybindings { get; set; } = new Hotkey(Key.K, ModifierKeys.Control);

		[MenuSettings("Settings", "Toggle Light/Dark Mode")]
		[Reactive] public Hotkey ToggleViewTheme { get; set; } = new Hotkey(Key.L, ModifierKeys.Control);

		[MenuSettings("View", "Toggle Updates View")]
		[Reactive] public Hotkey ToggleUpdatesView { get; set; } = new Hotkey(Key.U, ModifierKeys.Control);

		[MenuSettings("Go", "Open Mods Folder")]
		[Reactive] public Hotkey OpenModsFolder { get; set; } = new Hotkey(Key.D1, ModifierKeys.Control);

		[MenuSettings("Go", "Open Game Folder")]
		[Reactive] public Hotkey OpenGameFolder { get; set; } = new Hotkey(Key.D2, ModifierKeys.Control);

		[MenuSettings("Go", "Open Workshop Folder")]
		[Reactive] public Hotkey OpenWorkshopFolder { get; set; } = new Hotkey(Key.D3, ModifierKeys.Control);

		[MenuSettings("Go", "Open Extender Logs Folder")]
		[Reactive] public Hotkey OpenLogsFolder { get; set; } = new Hotkey(Key.D4, ModifierKeys.Control);

		[MenuSettings("Go", "Launch Game")]
		[Reactive] public Hotkey LaunchGame { get; set; } = new Hotkey(Key.G, ModifierKeys.Control | ModifierKeys.Shift);

		[MenuSettings("Tools", "Extract Selected Mods To...")]
		[Reactive] public Hotkey ExtractSelectedMods { get; set; } = new Hotkey(Key.M, ModifierKeys.Control);

		[MenuSettings("Tools", "Toggle Version Generator Window", Tooltip = "A tool for mod authors to generate version numbers for a mod's meta.lsx.")]
		[Reactive] public Hotkey ToggleVersionGeneratorWindow { get; set; } = new Hotkey(Key.G, ModifierKeys.Control);

		[MenuSettings("Tools", "Download & Install the Script Extender...", Style = "MenuItemHightlightBlink")]
		[Reactive] public Hotkey DownloadScriptExtender { get; set; } = new Hotkey(Key.T, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt);

		[MenuSettings("Tools", "Speak Active Order")]
		[Reactive] public Hotkey SpeakActiveModOrder { get; set; } = new Hotkey(Key.Home, ModifierKeys.Control);

		[MenuSettings("Help", "Check for Updates")]
		[Reactive] public Hotkey CheckForUpdates { get; set; } = new Hotkey(Key.F7);

		[MenuSettings("Help", "Donate a Coffee...")]
		[Reactive] public Hotkey OpenDonationLink { get; set; } = new Hotkey(Key.F10);

		[MenuSettings("Help", "About")]
		[Reactive] public Hotkey OpenAboutWindow { get; set; } = new Hotkey(Key.F1);

		[MenuSettings("Help", "Open Repository Page...")]
		[Reactive] public Hotkey OpenRepositoryPage { get; set; } = new Hotkey(Key.F11);

		private readonly SourceCache<Hotkey, string> keyMap = new SourceCache<Hotkey, string>((hk) => hk.ID);

		protected readonly ReadOnlyObservableCollection<Hotkey> allKeys;
		public ReadOnlyObservableCollection<Hotkey> All => allKeys;

		public void SaveDefaultKeybindings()
		{
			string filePath = @"Data\keybindings-default.json";
			try
			{
				Directory.CreateDirectory("Data");
				var keyMapDict = new Dictionary<string, Hotkey>();
				foreach (var key in All)
				{
					keyMapDict.Add(key.ID, key);
				}
				string contents = JsonConvert.SerializeObject(keyMapDict, Newtonsoft.Json.Formatting.Indented);
				File.WriteAllText(filePath, contents);
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error saving default keybindings at '{filePath}': {ex}");
			}
		}

		public bool SaveKeybindings(MainWindowViewModel vm, string filePath = @"Data\keybindings.json")
		{
			try
			{
				Directory.CreateDirectory("Data");
				var keyMapDict = new Dictionary<string, Hotkey>();
				foreach(var key in All)
				{
					if(!key.IsDefault)
					{
						keyMapDict.Add(key.ID, key);
					}
				}
				if(keyMapDict.Count == 0)
				{
					return true;
				}
				string contents = JsonConvert.SerializeObject(keyMapDict, Newtonsoft.Json.Formatting.Indented);
				File.WriteAllText(filePath, contents);
				return true;
			}
			catch (Exception ex)
			{
				vm.ShowAlert($"Error saving keybindings at '{filePath}': {ex}", AlertType.Danger);
			}
			return false;
		}

		public bool LoadKeybindings(MainWindowViewModel vm, string filePath = @"Data\keybindings.json")
		{
			try
			{
				if (File.Exists(filePath))
				{
					using (var reader = File.OpenText(filePath))
					{
						var fileText = reader.ReadToEnd();
						var allKeybindings = DivinityJsonUtils.SafeDeserialize<Dictionary<string, Hotkey>>(fileText);
						if(allKeybindings != null)
						{
							foreach(var kvp in allKeybindings)
							{
								var existingHotkey = All.FirstOrDefault(x => x.ID.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
								if(existingHotkey != null)
								{
									existingHotkey.Key = kvp.Value.Key;
									existingHotkey.Modifiers = kvp.Value.Modifiers;
									existingHotkey.UpdateDisplayBindingText();
								}
							}
						}
						else
						{
							DivinityApp.Log("Error deserializing keybindings.json - result is null.");
							DivinityApp.Log(fileText);
						}
					}
				}
			}
			catch (Exception ex)
			{
				vm.ShowAlert($"Error loading keybindings at '{filePath}': {ex}", AlertType.Danger);
			}
			return false;
		}

		public void SetToDefault()
		{
			foreach(var entry in keyMap.Items)
			{
				entry.ResetToDefault();
			}
		}

		public AppKeys()
		{
			Type t = typeof(AppKeys);
			// Building a list of keys / key names from properties, because lazy
			var keyProps = t.GetRuntimeProperties().Where(prop => Attribute.IsDefined(prop, typeof(ReactiveAttribute)) && prop.GetGetMethod() != null).ToList();
			foreach(var prop in keyProps)
			{
				var hotkey = (Hotkey)t.GetProperty(prop.Name).GetValue(this);
				hotkey.ID = prop.Name;
				keyMap.AddOrUpdate(hotkey);
			}
			keyMap.Connect().Bind(out allKeys).Subscribe();
			this.RaisePropertyChanged("All");
		}
	}
}
