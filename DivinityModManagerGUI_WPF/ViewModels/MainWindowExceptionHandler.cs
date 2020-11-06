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
            DivinityApp.Log($"Error: ({value.GetType().ToString()}: {value.Message}\n{value.StackTrace}");
            //if (Debugger.IsAttached) Debugger.Break();
            //RxApp.MainThreadScheduler.Schedule(() => { throw value; });
        }

        public void OnError(Exception error)
        {
            DivinityApp.Log($"Error: ({error.GetType().ToString()}: {error.Message}\n{error.StackTrace}");
            if (Debugger.IsAttached) Debugger.Break();

            RxApp.MainThreadScheduler.Schedule(() => { 
                if(_viewModel.MainProgressIsActive)
                {
                    _viewModel.MainProgressIsActive = false;
                }
                _viewModel.View.AlertBar.SetDangerAlert(error.Message);
                //throw error;
            });
        }

        public void OnCompleted()
        {
            if (Debugger.IsAttached) Debugger.Break();
            //RxApp.MainThreadScheduler.Schedule(() => { throw new NotImplementedException(); });
        }
    }
}
