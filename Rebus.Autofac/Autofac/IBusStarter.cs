namespace Rebus.Autofac
{
    /// <summary>
    /// Interface to start up the handlers for a registered queue. When <see cref="Start"/> is called,
    /// workers are added, and message processing will start.
    /// </summary>
    public interface IBusStarter<THandlerBase>
        where THandlerBase : class
    {
        /// <summary>
        /// Starts message processing handlers for this queue
        /// </summary>
        void Start();
    }
}