using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Psns.Common.Threading
{
    public class LimitedConcurrencyTaskScheduler : TaskScheduler
    {
        readonly int _maxDegreeOfParallelism;
        readonly LinkedList<Task> _tasks;

        // Indicates whether the current thread is processing work items.
        [ThreadStatic]
        static bool _currentThreadIsProcessingItems;

        int _delegatesQueuedOrRunning;

        public LimitedConcurrencyTaskScheduler(int maxDegreeOfParallelism) 
        {
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
            _tasks = new LinkedList<Task>();
        }

        // Gets an enumerable of the tasks currently scheduled on this scheduler. 
        protected sealed override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(_tasks, ref lockTaken);
                if(lockTaken) return _tasks;
                else throw new NotSupportedException();
            }
            finally
            {
                if(lockTaken) Monitor.Exit(_tasks);
            }
        }

        protected override void QueueTask(Task task)
        {
            lock(_tasks)
            {
                _tasks.AddLast(task);

                if(_delegatesQueuedOrRunning < _maxDegreeOfParallelism)
                {
                    ++_delegatesQueuedOrRunning;
                    NotifyThreadPoolOfPendingWork();
                }
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // If this thread isn't already processing a task, we don't support inlining
            if(!_currentThreadIsProcessingItems) return false;

            // If the task was previously queued, remove it from the queue
            if(taskWasPreviouslyQueued)
                // Try to run the task. 
                if(TryDequeue(task))
                    return base.TryExecuteTask(task);
                else
                    return false;
            else
                return base.TryExecuteTask(task);
        }

        protected override bool TryDequeue(Task task)
        {
            lock(_tasks) return _tasks.Remove(task);
        }

        public override int MaximumConcurrencyLevel => _maxDegreeOfParallelism;

        // Inform the ThreadPool that there's work to be executed for this scheduler. 
        private void NotifyThreadPoolOfPendingWork()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                // Note that the current thread is now processing work items.
                // This is necessary to enable inlining of tasks into this thread.
                _currentThreadIsProcessingItems = true;

                try
                {
                    // Process all available items in the queue.
                    while(true)
                    {
                        Task item;
                        lock(_tasks)
                        {
                            // When there are no more items to be processed,
                            // note that we're done processing, and get out.
                            if(_tasks.Count == 0)
                            {
                                --_delegatesQueuedOrRunning;
                                break;
                            }

                            // Get the next item from the queue
                            item = _tasks.First.Value;
                            _tasks.RemoveFirst();
                        }

                        // Execute the task we pulled out of the queue
                        base.TryExecuteTask(item);
                    }
                }
                // We're done processing items on the current thread
                finally { _currentThreadIsProcessingItems = false; }
            }, null);
        }
    }
}