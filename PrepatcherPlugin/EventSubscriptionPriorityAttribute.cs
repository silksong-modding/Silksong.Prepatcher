using System;
using System.Reflection;

namespace PrepatcherPlugin;

/// <summary>
/// Attribute that can be applied to a method to specify a priority, with smaller/more negative
/// priorities being executed before larger/more positive priorities.
/// 
/// The default priority (if this attribute is not applied) is zero.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EventSubscriptionPriorityAttribute : Attribute
{
    /// <summary>
    /// The priority of the subscriber.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Create an instance of this attribute with the specified priority.
    /// </summary>
    public EventSubscriptionPriorityAttribute(int priority) => Priority = priority;

    internal static int GetPriority<T>(T? func) where T : Delegate
    {
        return func?.Method?.GetCustomAttribute<EventSubscriptionPriorityAttribute>()?.Priority ?? 0;
    }
}
