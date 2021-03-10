using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DivinityModManager.Models.App
{
	public interface IHotkey
	{
		Key Key { get; set; }
		ModifierKeys Modifiers { get; set; }
		ICommand Command { get; set; }
		bool Enabled { get; set; }
		string DisplayName { get; set; }
	}

	[DataContract]
	public class Hotkey : ReactiveObject, IHotkey
	{
		public string ID { get; set; }

		private string _displayName = "";
		public string DisplayName
		{
			get => _displayName;
			set
			{
				this.RaiseAndSetIfChanged(ref _displayName, value);
				this.RaisePropertyChanged("ToolTip");
			}
		}

		public string ToolTip
		{
			get
			{
				if (!IsDefault)
				{
					return DisplayName + " (Modified)";
				}
				else
				{
					return DisplayName;
				}
			}
		}
		[Reactive] public string DisplayBindingText { get; private set; }

		[DataMember]
		[JsonConverter(typeof(StringEnumConverter))]
		[Reactive] public Key Key { get; set; }

		[DataMember]
		[JsonConverter(typeof(StringEnumConverter))]
		[Reactive] public ModifierKeys Modifiers { get; set; }

		[Reactive] public ICommand Command { get; set; }
		[Reactive] public ICommand ResetCommand { get; private set; }
		[Reactive] public ICommand ClearCommand { get; private set; }

		[Reactive] public bool Enabled { get; set; } = true;
		[Reactive] public bool CanEdit { get; set; } = true;
		[Reactive] public bool IsDefault { get; set; } = true;
		[Reactive] public bool IsSelected { get; set; } = false;
		[Reactive] public string ModifiedText { get; set; } = "";

		private Key _defaultKey;
		private ModifierKeys _defaultModifiers;

		public Key DefaultKey => _defaultKey;
		public ModifierKeys DefaultModifiers => _defaultModifiers;

		[Reactive] public IObservable<bool> CanExecute { get; private set; }

		private List<Action> actions = new List<Action>();
		private List<IObservable<bool>> canExecuteList = new List<IObservable<bool>>();

		public void AddAction(Action action, IObservable<bool> actionCanExecute = null, bool clearAllFirst = false)
		{
			if (clearAllFirst)
			{
				ClearActions();
			}
			if (!actions.Contains(action))
			{
				actions.Add(action);
			}

			if (actionCanExecute != null && !canExecuteList.Contains(actionCanExecute))
			{
				canExecuteList.Add(actionCanExecute);
				CanExecute = Observable.Merge(canExecuteList);
				Command = ReactiveCommand.Create(Invoke, CanExecute);
			}
		}

		public void ClearActions()
		{
			actions.Clear();
			canExecuteList.Clear();

			var canExecuteInitial = this.WhenAnyValue(x => x.Enabled, (b) => b == true);
			canExecuteList.Add(canExecuteInitial);
			CanExecute = Observable.Merge(canExecuteList);
			Command = ReactiveCommand.Create(Invoke, CanExecute);
		}

		public void Invoke()
		{
			actions.ForEach(a => a.Invoke());
		}

		public void ResetToDefault()
		{
			Key = _defaultKey;
			Modifiers = _defaultModifiers;
			UpdateDisplayBindingText();
		}

		public void Clear()
		{
			Key = Key.None;
			Modifiers = ModifierKeys.None;
			UpdateDisplayBindingText();
		}

		public void UpdateDisplayBindingText()
		{
			DisplayBindingText = ToString();
		}

		private void Init(Key key, ModifierKeys modifiers)
		{
			Key = key;
			Modifiers = modifiers;
			_defaultKey = key;
			_defaultModifiers = modifiers;

			var canExecuteInitial = this.WhenAnyValue(x => x.Enabled, (b) => b == true);
			canExecuteList.Add(canExecuteInitial);
			CanExecute = Observable.Merge(canExecuteList);
			//var canExecute = this.WhenAnyValue(x => x.canExecuteList, (list) => list.Count == 0 || list.All(b => b.Latest().All(b2 => b2 == true)));
			Command = ReactiveCommand.Create(Invoke, CanExecute);

			DisplayBindingText = ToString();

			var canReset = this.WhenAnyValue(x => x.Key, x => x.Modifiers, (k,m) => k != _defaultKey || m != _defaultModifiers).StartWith(false);
			ResetCommand = ReactiveCommand.Create(ResetToDefault, canReset);
			var canClear = this.WhenAnyValue(x => x.Key, x => x.Modifiers, (k, m) => k != Key.None).StartWith(false);
			ClearCommand = ReactiveCommand.Create(Clear, canClear);

			this.WhenAnyValue(x => x.Key, x => x.Modifiers, (k, m) => k == _defaultKey && m == _defaultModifiers).BindTo(this, x => x.IsDefault);
			var isDefaultChanged = this.WhenAnyValue(x => x.IsDefault);
			isDefaultChanged.Subscribe((b) =>
			{
				this.RaisePropertyChanged("ToolTip");
			});

			isDefaultChanged.Select(b => !b ? "*" : "").BindTo(this, x => x.ModifiedText);
		}

		public Hotkey(Key key)
		{
			Init(key, ModifierKeys.None);
		}

		public Hotkey(Key key, ModifierKeys modifiers)
		{
			Init(key, modifiers);
		}
		
		public Hotkey()
		{
			Init(Key.None, ModifierKeys.None);
		}

		public override string ToString()
		{
			var str = new StringBuilder();

			if (Modifiers.HasFlag(ModifierKeys.Control))
				str.Append("Ctrl + ");
			if (Modifiers.HasFlag(ModifierKeys.Shift))
				str.Append("Shift + ");
			if (Modifiers.HasFlag(ModifierKeys.Alt))
				str.Append("Alt + ");
			if (Modifiers.HasFlag(ModifierKeys.Windows))
				str.Append("Win + ");

			str.Append(Key.GetKeyName());

			return str.ToString();
		}
	}
}
