using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using ReactiveUI;
using ReactiveHistory;
using Reactive.Bindings.Extensions;
using System.Reflection;
using System.Windows.Input;
using System.Collections.Generic;
using DivinityModManager.Models;

namespace DivinityModManager.ViewModels
{
	public class BaseViewModel : ReactiveObject, IDisposable
	{
		public CompositeDisposable Disposables { get; private set; }

		public void Dispose()
		{
			this.Disposables?.Dispose();
		}

		public BaseViewModel()
		{
			Disposables = new CompositeDisposable();
		}
	}
}
