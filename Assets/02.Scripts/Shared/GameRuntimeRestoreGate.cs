using System;

/// <summary>
/// Stops simulation while a validated world snapshot is being projected. A restore may span an
/// additive-room load, so producers must consult this gate before spawning, ticking combat, or
/// writing another checkpoint.
/// </summary>
public static class GameRuntimeRestoreGate
{
    private static int restoreDepth;

    public static bool IsRestoring => restoreDepth > 0;

    public static event Action Changed;

    public static void BeginRestore()
    {
        restoreDepth++;
        Changed?.Invoke();
    }

    public static void EndRestore()
    {
        if (restoreDepth <= 0)
        {
            return;
        }

        restoreDepth--;
        Changed?.Invoke();
    }
}
