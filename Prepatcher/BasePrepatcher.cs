using BepInEx.Logging;
using Mono.Cecil;

namespace SilksongPrepatcher;

public abstract class BasePrepatcher
{
    protected ManualLogSource Log { get; private set; }

    public string Name { get; private set; }

    public BasePrepatcher()
        : this(null) { }

    public BasePrepatcher(string? name)
    {
        if (name is null)
        {
            name = GetType().Name;
        }

        Name = name;
        Log = Logger.CreateLogSource(name);
    }

    public abstract void PatchAssembly(AssemblyDefinition asm);
}
