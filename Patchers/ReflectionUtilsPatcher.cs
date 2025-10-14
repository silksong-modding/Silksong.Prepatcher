using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Silksong.Prepatcher.Patchers
{
    /// <summary>
    /// Patch ReflectionUtils to not slow the game when entering certain scenes
    /// 
    /// In particular, some FSMs, such as Mapper NPC - Dialogue in Bone_04, have references to the
    /// nonexistent TMProOldOld.TextAlignmentOptions class, and attempting to load this type
    /// via reflection is slowing down the game.
    /// 
    /// We insert the correct type into the typeLookup on the ReflectionUtils class
    /// so that this slowdown doesn't happen.
    /// </summary>
    internal static class ReflectionUtilsPatcher
    {
        private static readonly ManualLogSource Log = Logger.CreateLogSource($"Silksong.Prepatcher.{nameof(ReflectionUtilsPatcher)}");

        public static void PatchAssembly(AssemblyDefinition assembly)
        {
            Log.LogInfo($"Patching ReflectionUtils typeLookup dict in {assembly.Name.Name}");

            TypeDefinition typeDef = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "ReflectionUtils");

            if (typeDef == null)
            {
                Log.LogInfo($"Could not find type ReflectionUtils in {assembly.Name.Name}");
                return;
            }

            MethodDefinition cctor = typeDef.Methods.FirstOrDefault(m => m.IsStatic && m.IsConstructor && m.IsSpecialName);

            if (cctor == null)
            {
                Log.LogInfo($"Could not find static constructor");
                return;
            }

            InjectTypeLookup(assembly.MainModule, typeDef, cctor);
        }

        private static void InjectTypeLookup(ModuleDefinition module, TypeDefinition typeDef, MethodDefinition cctor)
        {
            const string typeName = "TMProOldOld.TextAlignmentOptions";
            const string fixedTypeName = "TMProOld.TextAlignmentOptions, Assembly-CSharp";

            FieldDefinition typeLookupField = typeDef.Fields.FirstOrDefault(f => f.Name == "typeLookup" && f.IsStatic);

            if (typeLookupField == null)
            {
                return;
            }

            // Get necessary references
            MethodReference getTypeMethod = module.ImportReference(typeof(Type).GetMethod(nameof(Type.GetType), [typeof(string)]));

            Type dictRuntimeType = typeof(Dictionary<,>).MakeGenericType(typeof(string), typeof(Type));
            MethodInfo setItemMethodInfo = dictRuntimeType.GetMethod("set_Item");
            MethodReference setItemMethodRef = module.ImportReference(setItemMethodInfo);

            // Manipulate IL
            ILProcessor il = cctor.Body.GetILProcessor();

            Instruction ret = cctor.Body.Instructions.Last();
            if (ret.OpCode != OpCodes.Ret)
            {
                ret = cctor.Body.Instructions.Reverse().FirstOrDefault(i => i.OpCode == OpCodes.Ret);
                if (ret == null) return;
            }

            il.InsertBefore(ret, il.Create(OpCodes.Ldsfld, typeLookupField));
            il.InsertBefore(ret, il.Create(OpCodes.Ldstr, typeName));
            il.InsertBefore(ret, il.Create(OpCodes.Ldstr, fixedTypeName));
            il.InsertBefore(ret, il.Create(OpCodes.Call, getTypeMethod));
            il.InsertBefore(ret, il.Create(OpCodes.Callvirt, setItemMethodRef));

            cctor.Body.OptimizeMacros();
        }
    }
}
