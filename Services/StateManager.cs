using System.Collections.Concurrent;

namespace STIN_BurzaModule.Services
{
    public class StateManager
    {
        private readonly ConcurrentQueue<(string ClientId, List<Item> Items)> _queue;

        public StateManager()
        {
            _queue = new ConcurrentQueue<(string, List<Item>)>();
        }

        public void Enqueue(string clientId, List<Item> items)
        {
            _queue.Enqueue((clientId, items));
        }

        public bool TryDequeue(out (string ClientId, List<Item> Items) result)
        {
            return _queue.TryDequeue(out result);
        }
    }
}