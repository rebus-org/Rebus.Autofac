﻿using Rebus.Config;

namespace Rebus.Autofac
{
    /// <summary>
    /// Class to start up the handlers for a registered queue. When <see cref="Start"/> is called,
    /// workers are added, and message processing will start.
    /// </summary>
    public class BusStarter<THandlerBase> : IBusStarter<THandlerBase>
        where THandlerBase : class
    {
        private readonly IBusStarter _busStarter;

        /// <summary>
        /// Constructor for the bus stater wrapper class
        /// </summary>
        /// <param name="busStarter">Bus stater for this bus instance</param>
        public BusStarter(
            IBusStarter busStarter)
        {
            _busStarter = busStarter;
        }

        /// <summary>
        /// Starts message processing handlers for this queue
        /// </summary>
        public void Start()
        {
            _busStarter.Start();
        }
    }
}