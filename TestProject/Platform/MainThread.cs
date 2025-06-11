namespace Microsoft.Maui.ApplicationModel
{
    /// <summary>
    /// Simplified MainThread class for testing that immediately executes actions
    /// </summary>
    public static class MainThread
    {
        /// <summary>
        /// Immediately executes the action for testing without actual UI thread
        /// </summary>
        /// <param name="action">Action to execute</param>
        public static void BeginInvokeOnMainThread(Action action)
        {
            action?.Invoke();
        }
    }
}