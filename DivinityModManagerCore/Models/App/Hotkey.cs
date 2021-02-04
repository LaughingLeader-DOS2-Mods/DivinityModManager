using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DivinityModManager.Models.App
{
	public class Hotkey : ReactiveObject
	{
		private Key key;
		public Key Key
		{
			get => key;
			set
			{
				this.RaiseAndSetIfChanged(ref key, value);
			}
		}

		private ModifierKeys modifiers;
		public ModifierKeys Modifiers
		{
			get => modifiers;
			set
			{
				this.RaiseAndSetIfChanged(ref modifiers, value);
			}
		}

		private ICommand command;

		public ICommand Command
		{
			get => command;
			set { this.RaiseAndSetIfChanged(ref command, value); }
		}

		private List<Action> actions = new List<Action>();
		private List<IObservable<bool>> canExecuteList = new List<IObservable<bool>>();

		public void AddAction(Action action, IObservable<bool> canExecute = null)
		{
			actions.Add(action);
			if(canExecute != null)
			{
				canExecuteList.Add(canExecute);
			}
		}

		public void Invoke()
		{
			actions.ForEach(a => a.Invoke());
		}

		private void Init(Key key, ModifierKeys modifiers)
		{
			Key = key;
			Modifiers = modifiers;

			var canExecute = this.WhenAnyValue(x => x.canExecuteList, (list) => list.Count == 0 || list.All(b => b.Latest().All(b2 => b2 == true)));
			Command = ReactiveCommand.Create(Invoke, canExecute);
		}


		public Hotkey(Key key)
		{
			Init(key, ModifierKeys.None);
		}

		public Hotkey(Key key, ModifierKeys modifiers)
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

			str.Append(Key);

			return str.ToString();
		}
	}
}
