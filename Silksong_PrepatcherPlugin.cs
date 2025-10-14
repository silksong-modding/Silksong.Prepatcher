using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        }
    }
}