using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SilksongPrepatcher.Utils;

namespace SilksongPrepatcher.Patchers;

/// <summary>
/// Replace all calls to methods satisfying the predicate with calls to the new method.
///
/// The new method should be visible to the patched assembly, and should be static.
/// </summary>
public class MethodReplacer(Func<MethodReference, bool> predicate, MethodInfo newMethodInfo)
    : BasePrepatcher
{
    public override void PatchAssembly(AssemblyDefinition asm)
    {
        MethodReference newMethodRef = asm.MainModule.ImportReference(newMethodInfo);

        foreach (TypeDefinition type in CecilUtils.GetTypeDefinitions(asm.MainModule))
        {
            foreach (MethodDefinition method in type.Methods.Where(m => m.HasBody))
            {
                using ILContext il = new(method);
                ILCursor cursor = new(il);

                while (
                    cursor.TryGotoNext(i =>
                        (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt)
                        && i.Operand is MethodReference methodReference
                        && predicate(methodReference)
                    )
                )
                {
                    Log.LogInfo($"Patching {type.FullName} : {method.FullName}");
                    cursor.Next.OpCode = OpCodes.Call;
                    cursor.Next.Operand = newMethodRef;
                }
            }
        }
    }
}
