using System;
using System.Reflection;

namespace PrepatcherPlugin;

internal static class Extensions
{
    public static MethodInfo GetMethodOrThrow(this Type type, string name)
    {
        MethodInfo mi = type.GetMethod(name);
        if (mi == null) throw new MissingMemberException(type.FullName, name);
        return mi;
    }
}
