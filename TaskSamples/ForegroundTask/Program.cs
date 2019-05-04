using System;
using System.Threading;
using System.Threading.Tasks;

namespace ForegroundTask
{
    class Program
    {
        static void Main(string[] args)
        {
            Task fgTask = CreateForegroundTask<int>(() =>
            {
                Console.WriteLine("Running...");
                Thread.Sleep(2000);
                return 42;
            }).ContinueWith(
                t => Console.WriteLine($"Result is {t.Result}"), 
                TaskContinuationOptions.ExecuteSynchronously);
            Console.WriteLine("Main Completed.");
        }

        public static Task<T> CreateForegroundTask<T>(Func<T> taskBody)
        {
            return CreateForegroundTask(taskBody, CancellationToken.None);
        }

        public static Task<T> CreateForegroundTask<T>(Func<T> taskBody, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<T>();
            var fgThread = new Thread(() => ExecuteForegroundTaskBody(taskBody, ct, tcs));
            fgThread.Start();
            // Return a task that is bound to the lifetime of the foreground thread body
            return tcs.Task;
        }

        private static void ExecuteForegroundTaskBody<T>(
            Func<T> taskBody, 
            CancellationToken ct, 
            TaskCompletionSource<T> tcs)
        {
            try
            {
                T result = taskBody();
                tcs.SetResult(result);
            }
            catch(OperationCanceledException cancelledException)
            {
                // Cancellation is associated with the cancellation token for this Task
                if (ct == cancelledException.CancellationToken)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    tcs.SetException(cancelledException);
                }
            }
            catch(Exception exc)
            {
                // Set the task status to Faulted, and re-throw as part of an AggregateException
                tcs.SetException(exc);
            }
        }
    }
}
