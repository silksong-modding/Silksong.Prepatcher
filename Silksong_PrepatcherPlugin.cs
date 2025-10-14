using Mono.Cecil;
using Silksong.Prepatcher.Patchers;
using System.Collections.Generic;

namespace Silksong.Prepatcher
{

    public static class AssemblyPatcher
    {
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "TeamCherry.NestedFadeGroup.dll", "PlayMaker.dll" };

        public static void Patch(AssemblyDefinition assembly)
        {
            if (assembly.Name.Name == "TeamCherry.NestedFadeGroup" || assembly.Name.Name == "PlayMaker")
            {
                GetTypesPatcher.PatchAssembly(assembly);
            }
            if (assembly.Name.Name == "PlayMaker")
            {
                ReflectionUtilsPatcher.PatchAssembly(assembly);
            }
        }
    }
}