using Serilog;
using System;

namespace Focus.Apps.EasyNpc
{
    static class ObservableExtensions
    {
        public static IDisposable SubscribeSafe<T>(this IObservable<T> source, ILogger logger, Action<T> onNext)
        {
            return source.Subscribe(
                value =>
                {
                    try
                    {
                        onNext(value);
                    }
                    catch (Exception ex)
                    {
                        LogException(logger, ex);
                    }
                },
                exception => LogException(logger, exception));
        }

        private static void LogException(ILogger logger, Exception exception)
        {
            logger.Error(exception, "Error while watching Observable");
        }
    }
}
