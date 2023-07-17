using DynamicData.Binding;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Reactive.Bindings.Extensions;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
		ICommand Command { get; }
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

		public ReactiveCommand<Unit, Unit> Command { get; set; }
		ICommand IHotkey.Command => this.Command;

		[Reactive] public ICommand ResetCommand { get; private set; }
		[Reactive] public ICommand ClearCommand { get; private set; }

		[Reactive] public bool Enabled { get; set; }
		[Reactive] public bool CanEdit { get; set; }
		[Reactive] public bool IsDefault { get; set; }
		[Reactive] public bool IsSelected { get; set; }
		[Reactive] public string ModifiedText { get; set; }

		private Key _defaultKey = Key.None;
		private ModifierKeys _defaultModifiers = ModifierKeys.None;

		public Key DefaultKey => _defaultKey;
		public ModifierKeys DefaultModifiers => _defaultModifiers;

		private readonly ObservableAsPropertyHelper<bool> _canExecuteCommand;
		public bool CanExecuteCommand => _canExecuteCommand.Value;

		private IObservable<bool> _canExecuteConditions;

		private readonly List<Action> _actions;

		public void AddAction(Action action, IObservable<bool> actionCanExecute = null)
		{
			_actions.Add(action);

			if (actionCanExecute != null)
			{
				AddCanExecuteCondition(actionCanExecute);
			}
		}

		public void AddCanExecuteCondition(IObservable<bool> canExecute)
		{
			_canExecuteConditions = _canExecuteConditions.CombineLatest(canExecute, (b1, b2) => b1 && b2);
			this.RaisePropertyChanged("_canExecuteConditions");
		}

		public void Invoke()
		{
			_actions.ForEach(a => a.Invoke());
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

		public Hotkey()
		{
			Enabled = true;
			CanEdit = true;
			IsDefault = true;
			ModifiedText = "";

			_actions = new List<Action>();

			DisplayBindingText = ToString();

			_canExecuteConditions = this.WhenAnyValue(x => x.Enabled);
			_canExecuteCommand = this.WhenAnyObservable(x => x._canExecuteConditions).ToProperty(this, nameof(CanExecuteCommand), false, RxApp.MainThreadScheduler);
			Command = ReactiveCommand.Create(Invoke, this.WhenAnyValue(x => x.CanExecuteCommand));

			this.WhenAnyValue(x => x.Key, x => x.Modifiers).Select(x => x.Item1 == _defaultKey && x.Item2 == _defaultModifiers).Skip(1).StartWith(true).BindTo(this, x => x.IsDefault);
			var isDefaultObservable = this.WhenAnyValue(x => x.IsDefault);
			isDefaultObservable.Subscribe(_ =>
			{
				this.RaisePropertyChanged("ToolTip");
			});
			isDefaultObservable.Select(b => b ? "*" : "").Skip(1).BindTo(this, x => x.ModifiedText);

			var canReset = isDefaultObservable.Select(b => !b).StartWith(false);
			var canClear = this.WhenAnyValue(x => x.Key, x => x.Modifiers, (k, m) => k != Key.None).StartWith(false);

			ResetCommand = ReactiveCommand.Create(ResetToDefault, canReset);
			ClearCommand = ReactiveCommand.Create(Clear, canClear);
		}

		private void Init(Key key, ModifierKeys modifiers)
		{
			Key = key;
			Modifiers = modifiers;
			_defaultKey = key;
			_defaultModifiers = modifiers;
		}

		public Hotkey(Key key) : this()
		{
			Init(key, ModifierKeys.None);
		}

		public Hotkey(Key key, ModifierKeys modifiers) : this()
		{
			Init(key, modifiers);
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
