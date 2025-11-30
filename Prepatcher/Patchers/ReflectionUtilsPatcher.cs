using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace SilksongPrepatcher.Patchers;

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
public class ReflectionUtilsPatcher : BasePrepatcher
{
    public override void PatchAssembly(AssemblyDefinition assembly)
    {
        TypeDefinition typeDef = assembly.MainModule.Types.FirstOrDefault(t =>
            t.Name == "ReflectionUtils"
        );

        if (typeDef == null)
        {
            Log.LogInfo($"Could not find type ReflectionUtils in {assembly.Name.Name}");
            return;
        }

        // Patch static constructor to repair certain types
        MethodDefinition cctor = typeDef.Methods.FirstOrDefault(m =>
            m.IsStatic && m.IsConstructor && m.IsSpecialName
        );
        if (cctor == null)
        {
            Log.LogInfo($"Could not find static constructor");
            return;
        }
        InjectTypeLookup(assembly.MainModule, typeDef, cctor);

        // Return null for certain non-existent types at the start of the method; putting null in the dictionary
        // causes it to be ignored so we have to check manually
        // This appears for example in Bone_10 when fleas are there
        MethodDefinition getGlobalType = typeDef.Methods.FirstOrDefault(m =>
            m.Name == "GetGlobalType"
        );
        if (getGlobalType == null)
        {
            Log.LogInfo($"Could not find GetGlobalType");
            return;
        }
        PatchGetGlobalType(assembly.MainModule, typeDef, getGlobalType);
    }

    private void PatchGetGlobalType(
        ModuleDefinition mainModule,
        TypeDefinition typeDef,
        MethodDefinition getGlobalType
    )
    {
        ILProcessor processor = getGlobalType.Body.GetILProcessor();
        Instruction originalFirstInstruction = getGlobalType.Body.Instructions.First();

        Instruction ldarg = processor.Create(OpCodes.Ldarg_0);
        Instruction ldstr = processor.Create(OpCodes.Ldstr, "HutongGames.PlayMaker.Actions.");

        MethodReference opEqualityRef = mainModule.ImportReference(
            typeof(string).GetMethod("op_Equality", [typeof(string), typeof(string)])
        );
        Instruction callOpEquality = processor.Create(OpCodes.Call, opEqualityRef);

        Instruction brfalse = processor.Create(OpCodes.Brfalse, originalFirstInstruction);
        Instruction ldnull = processor.Create(OpCodes.Ldnull);
        Instruction ret = processor.Create(OpCodes.Ret);

        processor.InsertBefore(originalFirstInstruction, ldarg); // Load argument
        processor.InsertBefore(originalFirstInstruction, ldstr); // Load corrupted string
        processor.InsertBefore(originalFirstInstruction, callOpEquality); // check if they are equal
        processor.InsertBefore(originalFirstInstruction, brfalse); // Skip to original first instruction if they are not equal
        processor.InsertBefore(originalFirstInstruction, ldnull); // Load null
        processor.InsertBefore(originalFirstInstruction, ret); // return null

        getGlobalType.Body.OptimizeMacros();
    }

    private void InjectTypeLookup(
        ModuleDefinition module,
        TypeDefinition typeDef,
        MethodDefinition cctor
    )
    {
        const string typeName = "TMProOldOld.TextAlignmentOptions";
        const string fixedTypeName = "TMProOld.TextAlignmentOptions, Assembly-CSharp";

        FieldDefinition typeLookupField = typeDef.Fields.FirstOrDefault(f =>
            f.Name == "typeLookup" && f.IsStatic
        );

        if (typeLookupField == null)
        {
            return;
        }

        // Get necessary references
        MethodReference getTypeMethod = module.ImportReference(
            typeof(Type).GetMethod(nameof(Type.GetType), [typeof(string)])
        );

        Type dictRuntimeType = typeof(Dictionary<,>).MakeGenericType(typeof(string), typeof(Type));
        MethodInfo setItemMethodInfo = dictRuntimeType.GetMethod("set_Item");
        MethodReference setItemMethodRef = module.ImportReference(setItemMethodInfo);

        // Manipulate IL
        ILProcessor il = cctor.Body.GetILProcessor();

        Instruction ret = cctor.Body.Instructions.Last();
        if (ret.OpCode != OpCodes.Ret)
        {
            ret = cctor.Body.Instructions.Reverse().FirstOrDefault(i => i.OpCode == OpCodes.Ret);
            if (ret == null)
                return;
        }

        il.InsertBefore(ret, il.Create(OpCodes.Ldsfld, typeLookupField));
        il.InsertBefore(ret, il.Create(OpCodes.Ldstr, typeName));
        il.InsertBefore(ret, il.Create(OpCodes.Ldstr, fixedTypeName));
        il.InsertBefore(ret, il.Create(OpCodes.Call, getTypeMethod));
        il.InsertBefore(ret, il.Create(OpCodes.Callvirt, setItemMethodRef));

        cctor.Body.OptimizeMacros();

        Log.LogInfo($"Patch successfully applied");
    }
}
