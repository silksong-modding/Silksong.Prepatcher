using System.Collections.Generic;

namespace PrepatcherCodeGenerator;

internal static class Deconstructor
{
    public static void Deconstruct<T, U>(this KeyValuePair<T, U> kvp, out T key, out U value)
    {
        key = kvp.Key; 
        value = kvp.Value;
    }
}
