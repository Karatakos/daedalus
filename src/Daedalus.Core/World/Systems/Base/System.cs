namespace Daedalus.Core.Systems;

using Arch;
using Arch.Core;

public abstract class System<T>(World world) {
    public abstract void Update(in T state);
}