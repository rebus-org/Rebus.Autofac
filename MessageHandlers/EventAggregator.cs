using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MessageHandlers
{
    public class EventAggregator : IEnumerable<string>
    {
        readonly ConcurrentQueue<string> _events = new ConcurrentQueue<string>();

        public void Register(string e) => _events.Enqueue(e);

        public IEnumerator<string> GetEnumerator() => _events.ToList().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
