namespace Geodashy.Core
{
    /// <summary>What an object does when the player touches it.</summary>
    public enum ObjectKind
    {
        Solid,        // blocks, platforms - landable, side collision kills
        Slope,        // ramps
        Hazard,       // spikes, blades, fire - touch = death
        Orb,          // runes the player activates by clicking while touching
        Pad,          // spring shrooms - automatic launch on touch
        Portal,       // mount / gravity / speed / size / mirror / dual / teleport
        Collectible,  // coins, gems, keys
        Decoration,   // no collision
        Trigger,      // invisible in play, fires when passed (or by spawn / touch)
        Finish,       // finish line
        StartPos,     // playtest start position
        Text          // decorative text
    }

    /// <summary>Which placeholder art to draw when no real sprite is present.</summary>
    public enum PlaceholderShape
    {
        Block,
        BlockOutline,
        Bricks,
        Planks,
        HalfBlock,
        Slope45,
        Slope22,
        Spike,
        SpikeSmall,
        SpikeWide,
        Lance,
        Thorns,
        Fire,
        Saw,
        Mace,
        Orb,
        Pad,
        Portal,
        Coin,
        Gem,
        Key,
        Flag,
        Gate,
        Torch,
        Banner,
        Chain,
        Skull,
        Barrel,
        Crate,
        Tree,
        Bush,
        Cloud,
        Window,
        Arch,
        Pillar,
        Crystal,
        Rune,
        Statue,
        Vine,
        Mushroom,
        Stalactite,
        Bog,
        Lightning,
        Trigger,
        StartMarker,
        Text,
        // castle decoration families (drawn by DecorArt; the definition's material and variant pick the look)
        Facade,
        Column,
        ArchDeco,
        Battlement,
        Relief,
        Monument,
        Ivy,
        Foliage,
        TreeDeco,
        Roof,
        TowerCap,
        WindowDeco,
        Door,
        Furniture,
        Tapestry,
        Armoury,
        Lantern,
        Yard,
        Fence,
        Siege
    }

    /// <summary>Surface material used by the procedural decoration art.</summary>
    public enum DecoMaterial
    {
        None,
        Marble,
        Slate,
        Granite,
        Sandstone,
        Limestone,
        Basalt,
        Cobble,
        MossyStone,
        Oak,
        DarkWood,
        Iron,
        Gold,
        Bronze,
        Copper,
        Plaster,
        Terracotta,
        Thatch,
        Lead,
        Ivy,
        Brick
    }

    public enum ColliderShape
    {
        None,
        Box,
        Triangle,     // spike: apex at top (local +y)
        Circle,
        SlopeUp,      // rising left->right (low at left, high at right)
        Ring          // orb/portal touch area
    }

    /// <summary>Portal behaviour sub-type.</summary>
    public enum PortalType
    {
        None,
        Mount,
        GravityNormal,
        GravityFlip,
        Speed,
        SizeNormal,
        SizeMini,
        MirrorOn,
        MirrorOff,
        DualOn,
        DualOff,
        Teleport
    }

    /// <summary>Orb behaviour sub-type.</summary>
    public enum OrbType
    {
        None,
        Jump,        // wind rune (yellow)
        SmallJump,   // feather rune (pink)
        BigJump,     // fire rune (red)
        GravityFlip, // gravity rune (blue)
        GravityJump, // storm rune (green) - flip and jump
        Slam,        // void rune (black) - slam toward gravity
        Dash,        // dash rune
        DashFlip,    // dash rune that also flips gravity
        Teleport     // shadow rune - snap to the opposite surface
    }

    public enum PadType
    {
        None,
        Jump,
        SmallJump,
        BigJump,
        GravityFlip,
        Teleport
    }

    public enum TriggerType
    {
        None,
        Move,
        Rotate,
        Color,
        Alpha,
        Toggle,
        Spawn,
        Pulse,
        Shake,
        Follow,
        Stop,
        CameraZoom,
        CameraOffset,
        CameraStatic,
        BackgroundSwitch,
        GroundSwitch,
        HidePlayer,
        ShowPlayer,
        Touch,
        Random,
        Song,
        Reverse,
        Count
    }

    public enum Easing
    {
        Linear,
        EaseInOut,
        EaseIn,
        EaseOut,
        ElasticInOut,
        ElasticIn,
        ElasticOut,
        BounceInOut,
        BounceIn,
        BounceOut,
        ExponentialInOut,
        ExponentialIn,
        ExponentialOut,
        SineInOut,
        SineIn,
        SineOut,
        BackInOut,
        BackIn,
        BackOut
    }

    /// <summary>Speed tiers. Values are index into MountCatalog.SpeedUnitsPerSecond.</summary>
    public enum SpeedTier
    {
        Slow = 0,
        Normal = 1,
        Fast = 2,
        Faster = 3,
        Fastest = 4
    }

    /// <summary>How forgiving a run is. Only Champion runs count for the personal best.</summary>
    public enum Difficulty
    {
        Training,     // squire mode: place your own waystones, auto waystones, scrubbing
        Checkpoints,  // respawn at the waystones the level author placed
        Champion      // no checkpoints at all
    }

    public static class DifficultyInfo
    {
        public static string Name(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Training: return "Training";
                case Difficulty.Checkpoints: return "Checkpoints";
                default: return "Champion";
            }
        }

        public static string Describe(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Training: return "Raise your own waystones (Z), remove them (X), scrub between them (← →). Nothing is recorded.";
                case Difficulty.Checkpoints: return "Fall back to the last waystone the level author placed. Clears are recorded separately.";
                default: return "One life from the gate to the finish. Waystones are ignored. Sets your personal best.";
            }
        }
    }

    public enum EditorMode
    {
        Build,
        Edit,
        Delete
    }

    public enum PropType
    {
        Float,
        Int,
        Bool,
        Color,
        Enum,
        Group,
        Text,
        Easing,
        Mount,
        Speed,
        Theme
    }
}
