namespace Focus.Graphics
{
    public interface IScheduler
    {
        void Run(Action action);
        T Run<T>(Func<T> action);
    }
}
