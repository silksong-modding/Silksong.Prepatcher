using System;
using System.Collections.Generic;

namespace PrepatcherPlugin;

/// <summary>
/// Object representing the backing field of a priority event. This object should
/// not be exposed directly, but instead through the add and remove methods on
/// a public event which delegate to the <see cref="Add(T)"/> and <see cref="Remove(T)"/>
/// methods of this object.
/// </summary>
/// <typeparam name="T">The type of the event.</typeparam>
/// <param name="getPriority">Function to get the priority of an element.
/// It is expected that this function is pure (returns an identical output
/// for an identical input).</param>
internal class PriorityEventField<T>(Func<T, float> getPriority)
{
    private readonly Func<T, float> _getPriority = getPriority;

    private readonly SortedDictionary<float, List<T>> _subscribers = [];

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
        float priority = _getPriority(obj);
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
        float priority = _getPriority(obj);
        if (_subscribers.TryGetValue(priority, out List<T> subscriberList))
        {
            subscriberList.Remove(obj);
        }
    }
}
