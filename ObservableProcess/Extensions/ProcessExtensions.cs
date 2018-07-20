using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;

namespace ObservableProcess.ProcessExtensions
{
    public static class ProcessExtensions
    {
        public static IObservable<EventPattern<object>> DisposedObservable(this Process proc) => 
            Observable.FromEventPattern(h => proc.Disposed += h, h => proc.Disposed -= h);

        public static IObservable<EventPattern<object>> ExitedObservable(this Process proc) => 
            Observable.FromEventPattern(h => proc.Exited += h, h => proc.Exited -= h);

        public static IObservable<EventPattern<DataReceivedEventArgs>> OutputDataReceivedObservable(this Process proc) => 
            Observable.FromEventPattern<DataReceivedEventHandler, DataReceivedEventArgs>(h => proc.OutputDataReceived += h, h => proc.OutputDataReceived -= h);

        public static IObservable<EventPattern<DataReceivedEventArgs>> ErrorDataReceivedObservable(this Process proc) => 
            Observable.FromEventPattern<DataReceivedEventHandler, DataReceivedEventArgs>(h => proc.ErrorDataReceived += h, h => proc.ErrorDataReceived -= h);
    }
}
