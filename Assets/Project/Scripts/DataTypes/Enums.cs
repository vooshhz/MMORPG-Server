public enum InventoryLocation
{
    player,
    chest,
    count
}

public enum CharacterState
{
    Idle,
    Running,
    Attacking,
    Casting
}

public enum PlayerFacing
{
    Up,
    Down,
    Left,
    Right
}

public enum ItemType
{
    Seed,
    Commodity,
    Watering_tool,
    Hoeing_tool,
    Chopping_tool,
    Breaking_tool,
    Reaping_tool,
    Collecting_tool,
    Reapable_scenary,
    Furniture,
    none,
    count
}

public enum GridBoolProperty
{
    diggable,
    canDropItem,
    canPlaceFurniture,
    isPath,
    isNPCObstacle
}

public enum SceneName
{
    ServerScene,
    LoginScene,
    CharacterSelectionScene,
    CharacterCreationScene,
}
public enum EventType
{

}