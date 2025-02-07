﻿using System.Threading;
using System.Threading.Tasks;

// Original idea by Stephen Toub: http://blogs.msdn.com/b/pfxteam/archive/2012/02/11/10266920.aspx

namespace SIL.Machine.Threading
{
    /// <summary>
    /// An async-compatible manual-reset event.
    /// </summary>
    public sealed class AsyncManualResetEvent
    {
        /// <summary>
        /// The object used for synchronization.
        /// </summary>
        private readonly object _sync;

        /// <summary>
        /// The current state of the event.
        /// </summary>
        private TaskCompletionSource _tcs;

        /// <summary>
        /// Creates an async-compatible manual-reset event.
        /// </summary>
        /// <param name="set">Whether the manual-reset event is initially set or unset.</param>
        public AsyncManualResetEvent(bool set)
        {
            _sync = new object();
            _tcs = new TaskCompletionSource();
            if (set)
            {
                //Enlightenment.Trace.AsyncManualResetEvent_Set(this, _tcs.Task);
                _tcs.SetResult();
            }
            else
            {
                //Enlightenment.Trace.AsyncManualResetEvent_Reset(this, _tcs.Task);
            }
        }

        /// <summary>
        /// Creates an async-compatible manual-reset event that is initially unset.
        /// </summary>
        public AsyncManualResetEvent() : this(false) { }

        /// <summary>
        /// Whether this event is currently set. This member is seldom used; code using this member has a high possibility of race conditions.
        /// </summary>
        public bool IsSet
        {
            get
            {
                lock (_sync)
                    return _tcs.Task.IsCompleted;
            }
        }

        /// <summary>
        /// Asynchronously waits for this event to be set.
        /// </summary>
        public Task WaitAsync()
        {
            lock (_sync)
            {
                var ret = _tcs.Task;
                //Enlightenment.Trace.AsyncManualResetEvent_Wait(this, ret);
                return ret;
            }
        }

        /// <summary>
        /// Synchronously waits for this event to be set. This method may block the calling thread.
        /// </summary>
        public void Wait()
        {
            WaitAsync().Wait();
        }

        /// <summary>
        /// Synchronously waits for this event to be set. This method may block the calling thread.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to cancel the wait. If this token is already canceled, this method will first check whether the event is set.</param>
        public void Wait(CancellationToken cancellationToken)
        {
            var ret = WaitAsync();
            if (ret.IsCompleted)
                return;
            ret.Wait(cancellationToken);
        }

        /// <summary>
        /// Sets the event, atomically completing every task returned by <see cref="WaitAsync"/>. If the event is already set, this method does nothing.
        /// </summary>
        public void Set()
        {
            lock (_sync)
            {
                //Enlightenment.Trace.AsyncManualResetEvent_Set(this, _tcs.Task);
                _tcs.TrySetResultWithBackgroundContinuations();
            }
        }

        /// <summary>
        /// Resets the event. If the event is already reset, this method does nothing.
        /// </summary>
        public void Reset()
        {
            lock (_sync)
            {
                if (_tcs.Task.IsCompleted)
                    _tcs = new TaskCompletionSource();
                //Enlightenment.Trace.AsyncManualResetEvent_Reset(this, _tcs.Task);
            }
        }
    }
}
