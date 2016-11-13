
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace IoTBrowser
{
    public class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        public ViewModelBase(
            SynchronizationContext synchronizationContext,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string memberName = "")
        {
            SynchronizationContext = synchronizationContext;
            FilePath = filePath;
            LineNumber = lineNumber;
            MemberName = memberName;

            PeriodicUpdates = ImmutableDictionary<string, PeriodicUpdateInfo>.Empty;
            var currentType = GetType();
            while (currentType != typeof(ViewModelBase))
            {
                var info = currentType.GetTypeInfo();
                var periodicUpdateProperties = info.DeclaredProperties.Where(
                    p => p.GetCustomAttribute(typeof(PeriodicUpdateAttribute)) != null);
                foreach (var periodicUpdateProperty in periodicUpdateProperties)
                {
                    PeriodicUpdates = PeriodicUpdates.Add(periodicUpdateProperty.Name, new PeriodicUpdateInfo(true));
                }
                currentType = info.BaseType;
            }

            if (PeriodicUpdates.Count > 0)
            {
                TokenSource = new CancellationTokenSource();
                Task.Run(() => Update(TokenSource.Token));
            }
        }
        string Id { get; set; }
        private ManualResetEvent Updating { get; set; }
        private CancellationTokenSource TokenSource { get; set; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
        public string MemberName { get; set; }

        void Update(CancellationToken cancellationToken)
        {
            Updating = new ManualResetEvent(false);
            while (!cancellationToken.IsCancellationRequested)
            {
                using (var wait = new ManualResetEvent(false))
                {

                    foreach (var propertyName in PeriodicUpdates.Keys)
                    {
                        if (PeriodicUpdates[propertyName].IsActive) { OnPropertyChanged(propertyName); }
                    }
                    wait.WaitOne(250);
                }
            }
            Updating.Set();
        }

        protected SynchronizationContext SynchronizationContext { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged != null)
            {
                var dispatch = DispatchAsync(() => propertyChanged(this, new PropertyChangedEventArgs(propertyName)));
            }
        }


        protected async Task OnPropertyChangedAsync([CallerMemberName] string propertyName = "")
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged != null)
            {
                await DispatchAsync(() => propertyChanged(this, new PropertyChangedEventArgs(propertyName)));
            }
        }

        protected virtual async Task OnPropertyChangedAsync<T>(Expression<Func<T>> action)
        {
            var expr = (MemberExpression)action.Body;
            await OnPropertyChangedAsync(expr.Member.Name);
        }

        protected virtual void OnPropertyChanged<T>(Expression<Func<T>> action)
        {
            var expr = (MemberExpression)action.Body;
            OnPropertyChanged(expr.Member.Name);
        }


        protected async Task DispatchAsync(Action action)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();
            var callback = new SendOrPostCallback(delegate (object state)
            {
                try
                {
                    action.Invoke();
                    taskCompletionSource.SetResult(Type.Missing);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            });
            SynchronizationContext.Post(callback, null);
            await taskCompletionSource.Task;
        }

        protected async Task DispatchAsync(Func<Task> func)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();
            var callback = new SendOrPostCallback(async delegate (object state)
            {
                try
                {
                    await func.Invoke();
                    taskCompletionSource.SetResult(Type.Missing);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            });
            SynchronizationContext.Post(callback, null);
            await taskCompletionSource.Task;
        }

        protected ImmutableDictionary<string, PeriodicUpdateInfo> PeriodicUpdates { get; set; }

        protected void SetPropertyActive(string propertyName, bool isActive)
        {
            PeriodicUpdates[propertyName].IsActive = isActive;
        }

        protected void SafeDispose(IDisposable disposable)
        {
            if (disposable != null)
            {
                disposable.Dispose();
                disposable = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (TokenSource != null)
                {
                    TokenSource.Cancel();
                    if (Updating != null)
                    {
                        Updating.WaitOne();
                        Updating.Dispose();
                        Updating = null;
                    }

                    TokenSource.Dispose();
                    TokenSource = null;
                }
            }
        }
    }
}
