using System;
using System.Reflection;
using System.Linq;

namespace Silksong.Prepatcher
{
    public static class AssemblyExtensions
    {
        public static Type[] GetTypesSafely(this Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(x => x is not null).ToArray();
            }
        }
    }
}
