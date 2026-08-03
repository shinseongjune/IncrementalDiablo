/// <summary>
/// Authored return portal trigger. It banks pending rewards before DungeonRoomLoader returns the hero
/// to the persistent hub and unloads the active additive room.
/// </summary>
public sealed class ReturnPortal : DungeonRoomExit
{
    public override string DisplayName => "Return Portal";

    protected override bool TryUseExit(DungeonRoomLoader loader)
    {
        return loader.TryReturnToHub();
    }
}
