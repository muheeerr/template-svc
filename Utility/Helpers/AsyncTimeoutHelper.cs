namespace Utility.Helpers
{
    public static class AsyncTimeoutHelper
    {
        public static async Task<T> RunWithTimeout<T>(Func<CancellationToken, Task<T>> action, TimeSpan timeout, T timeoutValue, CancellationToken cancellationToken = default)
        {
            using var timeoutCts = new CancellationTokenSource();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var delayTask = Task.Delay(timeout, timeoutCts.Token);
            var actionTask = action(linkedCts.Token);

            var completedTask = await Task.WhenAny(actionTask, delayTask);

            if (completedTask == delayTask)
            {
                // Timeout occurred
                return timeoutValue;
            }

            // Cancel the delay task to release resources
            timeoutCts.Cancel();

            // Await the action task to propagate exceptions
            return await actionTask;
        }

    }
}




