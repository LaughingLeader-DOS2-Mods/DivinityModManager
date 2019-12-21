using ReactiveUI;
using System.Reactive.Concurrency;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.ViewModels
{
    public class MainWindowExceptionHandler : IObserver<Exception>
    {
        private MainWindowViewModel _viewModel;

        public MainWindowExceptionHandler(MainWindowViewModel vm)
        {
            _viewModel = vm;
        }

        public void OnNext(Exception value)
        {
            if (Debugger.IsAttached) Debugger.Break();
            Trace.WriteLine($"({value.GetType().ToString()}: {value.Message}");
            RxApp.MainThreadScheduler.Schedule(() => { throw value; });
        }

        public void OnError(Exception error)
        {
            if (Debugger.IsAttached) Debugger.Break();

            Trace.WriteLine($"Error ({error.GetType().ToString()}: {error.Message}");

            RxApp.MainThreadScheduler.Schedule(() => { 
                if(_viewModel.MainProgressIsActive)
                {
                    _viewModel.MainProgressIsActive = false;
                }
                throw error; 
            });
        }

        public void OnCompleted()
        {
            if (Debugger.IsAttached) Debugger.Break();
            RxApp.MainThreadScheduler.Schedule(() => { throw new NotImplementedException(); });
        }
    }
}
