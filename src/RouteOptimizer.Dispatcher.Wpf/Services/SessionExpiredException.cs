namespace RouteOptimizer.Dispatcher.Wpf.Services;

public class SessionExpiredException : Exception
{
    public SessionExpiredException() : base("Your session has expired. Please sign in again.")
    {
    }
}
