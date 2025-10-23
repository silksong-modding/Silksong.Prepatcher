using Mono.Cecil;

namespace SilksongPrepatcher;

public abstract class BasePrepatcher
{
    public abstract void PatchAssembly(AssemblyDefinition asm);

    public virtual string Name
    {
        get
        {
            return GetType().Name;
        }
    }
}
