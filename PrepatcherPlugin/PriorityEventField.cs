using System;
using System.Collections.Generic;

namespace PrepatcherPlugin;

internal class PriorityEventField<T>(Func<T, int> getPriority)
{
    private readonly Func<T, int> _getPriority = getPriority;

    private readonly SortedDictionary<int, List<T>> _subscribers = [];

    public IEnumerable<T> GetInvocationList()
    {
        foreach (List<T> subscriberList in _subscribers.Values)
        {
            foreach (T sub in subscriberList)
            {
                yield return sub;
            }
        }
    }

    public void Add(T obj)
    {
        int priority = _getPriority(obj);
        if (_subscribers.TryGetValue(priority, out List<T> subscriberList))
        {
            subscriberList.Add(obj);
        }
        else
        {
            _subscribers[priority] = [obj];
        }
    }

    public void Remove(T obj)
    {
        int priority = _getPriority(obj);
        if (_subscribers.TryGetValue(priority, out List<T> subscriberList))
        {
            subscriberList.Remove(obj);
        }
    }
}
