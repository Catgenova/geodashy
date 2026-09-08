using System.Collections.Generic;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>Physics + presentation description of a mount (the equivalent of a Geometry Dash game mode).</summary>
    public class MountDefinition
    {
        public string id;
        public string name;
        /// <summary>One-line explanation of what the button does on this mount.</summary>
        public string control;
        public string colorHex;
        public string accentHex;

        /// <summary>Whether the mount flies (has a ceiling and no side-kill on ceilings/floors).</summary>
        public bool flying;
        /// <summary>Whether holding the button is meaningful (dragon, wisp, cart) or only presses count.</summary>
        public bool holdControl;
        public float gravity = 84f;
        public float jumpVelocity = 19.4f;
        public float maxFallSpeed = 30f;
        public float maxRiseSpeed = 30f;
        /// <summary>Extra upward acceleration while held (flying / thrust mounts).</summary>
        public float holdAccel = 0f;
        /// <summary>Wisp: vertical speed as a factor of horizontal speed.</summary>
        public float waveSlope = 1f;
        public float width = 1f;
        public float height = 1f;
        /// <summary>Rotation behaviour: 0 none, 1 spin while airborne (horse), 2 tilt with velocity (dragon), 3 roll (boar).</summary>
        public int rotationMode = 1;
        public float cameraCeilingPadding = 0f;

        public Color Color => ObjectCatalog.Hex(colorHex);
        public Color Accent => ObjectCatalog.Hex(accentHex);
    }

    public static class MountCatalog
    {
        /// <summary>Blocks per second per speed tier (Slow, Normal, Fast, Faster, Fastest).</summary>
        public static readonly float[] SpeedUnitsPerSecond = { 8.4f, 10.4f, 12.9f, 15.6f, 19.2f };
        public static readonly string[] SpeedLabels = { "0.5x", "1x", "2x", "3x", "4x" };

        public static readonly List<MountDefinition> All = new List<MountDefinition>
        {
            new MountDefinition
            {
                id = "horse", name = "Horse", control = "Click to jump.",
                colorHex = "b8763a", accentHex = "5a3a1a",
                gravity = 84f, jumpVelocity = 19.4f, rotationMode = 1
            },
            new MountDefinition
            {
                id = "dragon", name = "Dragon", control = "Hold to pitch up, release to pitch down.",
                colorHex = "b8302c", accentHex = "ffb03a",
                flying = true, holdControl = true, gravity = 42f, holdAccel = 84f, maxFallSpeed = 9.5f, maxRiseSpeed = 9.5f,
                jumpVelocity = 0f, rotationMode = 2, width = 1.2f, height = 0.8f
            },
            new MountDefinition
            {
                id = "griffin", name = "Griffin", control = "Click to flap upward.",
                colorHex = "e0c060", accentHex = "8a6a20",
                flying = true, gravity = 58f, jumpVelocity = 11.5f, maxFallSpeed = 14f, maxRiseSpeed = 14f, rotationMode = 0
            },
            new MountDefinition
            {
                id = "boar", name = "War Boar", control = "Click to flip gravity while on a surface.",
                colorHex = "6a5a4a", accentHex = "e0e0e0",
                gravity = 74f, jumpVelocity = 0f, rotationMode = 3, width = 0.9f, height = 0.9f
            },
            new MountDefinition
            {
                id = "wisp", name = "Wisp", control = "Hold to fly up diagonally, release to dive.",
                colorHex = "8ae0ff", accentHex = "ffffff",
                flying = true, holdControl = true, gravity = 0f, waveSlope = 1f, rotationMode = 2, width = 0.6f, height = 0.6f
            },
            new MountDefinition
            {
                id = "cart", name = "Siege Cart", control = "Hold to jump higher.",
                colorHex = "8a5a2a", accentHex = "4a4c56",
                gravity = 84f, jumpVelocity = 13.5f, holdAccel = 62f, holdControl = true, rotationMode = 0
            },
            new MountDefinition
            {
                id = "shadowcat", name = "Shadow Cat", control = "Click to leap instantly to the opposite surface.",
                colorHex = "3a2a5a", accentHex = "b070ff",
                gravity = 84f, jumpVelocity = 0f, rotationMode = 0
            }
        };

        public static MountDefinition Get(string id)
        {
            foreach (var m in All) if (m.id == id) return m;
            return All[0];
        }

        public static string[] Ids
        {
            get
            {
                var ids = new string[All.Count];
                for (int i = 0; i < All.Count; i++) ids[i] = All[i].id;
                return ids;
            }
        }

        public static float Speed(int tier)
        {
            tier = Mathf.Clamp(tier, 0, SpeedUnitsPerSecond.Length - 1);
            return SpeedUnitsPerSecond[tier];
        }
    }
}
