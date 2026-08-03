/// <summary>
/// Authored deeper-exit trigger. It preserves pending rewards and asks DungeonRoomLoader to replace
/// the current additive room with the next saved DungeonRunPlan room.
/// </summary>
public sealed class DeeperExit : DungeonRoomExit
{
    public override string DisplayName => "Deeper Exit";

    protected override bool TryUseExit(DungeonRoomLoader loader)
    {
        return loader.TryEnterDeeperRoom();
    }
}
