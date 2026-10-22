using System;

namespace ArchiveSystem.Domain.Enums
{
    [Flags]
    public enum ClassificationPermissionMask : int
    {
        None = 0,
        Read = 1 << 0,
        Write = 1 << 1,
        Manage = 1 << 2,
        Move = 1 << 3
    }
}
