using BepInEx;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SilksongPrepatcher.Patchers.PlayerDataPatcher
{
    /// <summary>
    /// Class holding cached type/method definitions used during patching
    /// </summary>
    internal class PatchingContext
    {
        public PatchingContext(AssemblyDefinition asm)
        {
            MainModule = asm.MainModule;
            PDType = MainModule.Types.First(t => t.Name == "PlayerData");

            DefaultGetMethods = PDType.Methods
                .Where(m => m.Name.StartsWith("Get") && DefaultAccessMethods.Contains(m.Name.Substring(3)))
                .ToDictionary(m => m.ReturnType);

            DefaultSetMethods = PDType.Methods
                .Where(m => m.Name.StartsWith("Set") && DefaultAccessMethods.Contains(m.Name.Substring(3)) && m.Parameters.Count == 2)
                .ToDictionary(m => m.Parameters[1].ParameterType);


            AssemblyDefinition sharedUtilsAsm = AssemblyDefinition.ReadAssembly(Path.Combine(Paths.ManagedPath, "TeamCherry.SharedUtils.dll"));
            TypeDefinition variableExt = sharedUtilsAsm.MainModule.GetType("TeamCherry.SharedUtils.VariableExtensions");
            GenericGetMethod = variableExt.GetMethods().First(
                x => x.Name == "GetVariable" && x.ContainsGenericParameter && x.Parameters.Count == 2);
            GenericSetMethod = variableExt.GetMethods().First(
                x => x.Name == "SetVariable" && x.ContainsGenericParameter && x.Parameters.Count == 3);
        }

        public ModuleDefinition MainModule { get; }
        public TypeDefinition PDType { get; } 

        public MethodDefinition GenericGetMethod { get; }
        public MethodDefinition GenericSetMethod { get; }

        // List of allowed types
        public static readonly HashSet<string> DefaultAccessMethods =
        [
            "Bool",
            "String",
            "Float",
            "Int",
            "Vector3",
        ];

        // Cache Get/Set methods
        public Dictionary<TypeReference, MethodDefinition> DefaultGetMethods { get; }
        public Dictionary<TypeReference, MethodDefinition> DefaultSetMethods { get; }

        public enum AccessMethodType
        {
            Default,
            Generic,
        }

        public void GetGetMethod(TypeReference fieldType, out MethodReference accessMethod, out AccessMethodType accessType)
        {
            if (DefaultGetMethods.TryGetValue(fieldType, out MethodDefinition method))
            {
                accessMethod = MainModule.ImportReference(method);
                accessType = AccessMethodType.Default;
                return;
            }

            // TODO - cache this?
            MethodReference genericMethodRefImported = MainModule.ImportReference(GenericGetMethod);
            GenericInstanceMethod genericInstanceMethod = new(genericMethodRefImported);
            genericInstanceMethod.GenericArguments.Add(fieldType);

            accessMethod = genericInstanceMethod;
            accessType = AccessMethodType.Generic;
            return;
        }

        public void GetSetMethod(TypeReference fieldType, out MethodReference accessMethod, out AccessMethodType accessType)
        {
            if (DefaultSetMethods.TryGetValue(fieldType, out MethodDefinition method))
            {
                accessMethod = MainModule.ImportReference(method);
                accessType = AccessMethodType.Default;
                return;
            }

            // TODO - cache this?
            MethodReference genericMethodRefImported = MainModule.ImportReference(GenericSetMethod);
            GenericInstanceMethod genericInstanceMethod = new(genericMethodRefImported);
            genericInstanceMethod.GenericArguments.Add(fieldType);

            accessMethod = genericInstanceMethod;
            accessType = AccessMethodType.Generic;
            return;
        }
    }
}
