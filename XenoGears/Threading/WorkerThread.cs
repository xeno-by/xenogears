using System;
using System.Diagnostics;
using System.Threading;
using XenoGears.Assertions;
using XenoGears.Exceptions;
using XenoGears.Reflection.Attributes;
using XenoGears.Traits.Disposable;

namespace XenoGears.Threading
{
    [DebuggerNonUserCode]
    public class WorkerThread : Disposable
    {
        private WorkerThreadAttribute _attr;
        private String CustomName { get { return _attr.Name; } }
        private bool IsAffined { get { return _attr.IsAffined; } }
        private bool IsBackground { get { return _attr.IsBackground; } }

        protected virtual Func<T> Wrap<T>(Func<T> task) { return task; }
        protected virtual Action Wrap(Action task) { return task; }
        protected virtual void Initialize() { /* do nothing by default */ }
        protected virtual Exception Wrap(Exception exn) { return exn; }

        private readonly Thread _thread;
        private int _nativeThreadId;
        private readonly Mutex _taskReservation = new Mutex(false);
        private readonly AutoResetEvent _taskArrived = new AutoResetEvent(false);
        private readonly AutoResetEvent _taskCompleted = new AutoResetEvent(false);
        private Func<Object> _task = null;
        private Object _ret = null;
        private Exception _exn = null;

        public WorkerThread()
        {
            _attr = this.GetType().Attr<WorkerThreadAttribute>();
            _thread = new Thread(() =>
            {
                _nativeThreadId = NativeThread.Id;

                while (true)
                {
                    _taskArrived.WaitOne();
                    if (IsBeingDisposed) break;

                    (_task != null).AssertTrue();
                    (_ret == null).AssertTrue();
                    (_exn == null).AssertTrue();

                    try
                    {
                        if (IsAffined)
                        {
                            using (NativeThread.Affinitize(_nativeThreadId))
                            {
                                EnsureInitialized();
                                _ret = _task();
                                _exn = null;
                            }
                        }
                        else
                        {
                            EnsureInitialized();
                            _ret = _task();
                            _exn = null;
                        }
                    }
                    catch (Exception exn)
                    {
                        _ret = null;
                        exn.EraseStackTrace();
                        _exn = Wrap(exn);
                    }

                    _task = null;
                    _taskCompleted.Set();
                }
            }){IsBackground = IsBackground};

            if (CustomName != null) _thread.Name = CustomName;
            _thread.Start();
        }

        protected override void DisposeManagedResources()
        {
            Invoke(() => {});
        }

        private Object _initializationLock = new Object();
        private bool _hasBeenInitialized = false;
        private bool _isBeingInitialized = false;
        private Exception _initializationException = null;
        private void EnsureInitialized()
        {
            if (!_hasBeenInitialized && !_isBeingInitialized)
            {
                lock (_initializationLock)
                {
                    if (!_hasBeenInitialized && !_isBeingInitialized)
                    {
                        try
                        {
                            _isBeingInitialized = true;
                            Initialize();
                        }
                        catch(Exception exn)
                        {
                            exn.PreserveStackTrace();
                            _initializationException = exn;
                            throw _initializationException;
                        }
                        finally
                        {
                            _isBeingInitialized = false;
                            _hasBeenInitialized = true;
                        }
                    }
                    else
                    {
                        if (_initializationException != null)
                        {
                            throw _initializationException;
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }
            else
            {
                if (_initializationException != null)
                {
                    throw _initializationException;
                }
                else
                {
                    return;
                }
            }
        }

        public void Ensure()
        {
            Invoke(() => {});
        }

        public void Invoke(Action task)
        {
            IsDisposed.AssertFalse();
            task.AssertNotNull();
            task = Wrap(task);

            if (Thread.CurrentThread == _thread)
            {
                task();
            }
            else
            {
                try
                {
                    _taskReservation.WaitOne();
                    (_task == null).AssertTrue();
                    IsDisposed.AssertFalse();

                    _task = () => { task(); return null; };
                    _taskArrived.Set();
                    _taskCompleted.WaitOne();

                    var ret = _ret;
                    var exn = _exn;
                    _ret = null;
                    _exn = null;

                    if (exn != null) throw exn;
                    (ret == null).AssertTrue();
                }
                finally
                {
                    _taskReservation.ReleaseMutex();
                }
            }
        }

        public T Invoke<T>(Func<T> task)
        {
            IsDisposed.AssertFalse();
            task.AssertNotNull();
            task = Wrap(task);

            if (Thread.CurrentThread == _thread)
            {
                return task();
            }
            else
            {
                try
                {
                    _taskReservation.WaitOne();
                    (_task == null).AssertTrue();
                    IsDisposed.AssertFalse();

                    _task = () => task();
                    _taskArrived.Set();
                    _taskCompleted.WaitOne();

                    var ret = _ret;
                    var exn = _exn;
                    _ret = null;
                    _exn = null;

                    if (exn != null) throw exn;
                    return (T)ret;
                }
                finally
                {
                    _taskReservation.ReleaseMutex();
                }
            }
        }
    }
}