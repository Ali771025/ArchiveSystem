using System;

namespace ArchiveSystem.Domain.Enums
{
    [Flags]
    public enum TagPermissionMask : int
    {
        None = 0,
        View = 1 << 0,
        Assign = 1 << 1
    }
}
