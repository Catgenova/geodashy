using System.Collections.Generic;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>Describes an editable property shown in the properties panel.</summary>
    public class PropDef
    {
        public string key;
        public string label;
        public PropType type;
        public float min = float.MinValue;
        public float max = float.MaxValue;
        public float step = 0.1f;
        public string defaultValue = "";
        public string[] enumValues;
        public string help = "";

        public PropDef(string key, string label, PropType type, string defaultValue = "")
        {
            this.key = key;
            this.label = label;
            this.type = type;
            this.defaultValue = defaultValue;
        }

        public PropDef Range(float min, float max, float step = 0.1f)
        {
            this.min = min;
            this.max = max;
            this.step = step;
            return this;
        }

        public PropDef Values(params string[] values)
        {
            enumValues = values;
            return this;
        }

        public PropDef Help(string text)
        {
            help = text;
            return this;
        }
    }

    /// <summary>Static description of an object type available in the palette.</summary>
    public class ObjectDefinition
    {
        public string id;
        public string name;
        public string category;
        public ObjectKind kind;
        public float width = 1f;
        public float height = 1f;
        public PlaceholderShape shape = PlaceholderShape.Block;
        public ColliderShape collider = ColliderShape.Box;
        /// <summary>Scale of the collision box relative to the sprite (hazards use a smaller hitbox).</summary>
        public float hitboxScale = 1f;
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.gray;
        public int defaultZLayer = 0;
        public int defaultZOrder = 0;
        public string spriteId;
        public string description = "";
        public string[] tags = new string[0];
        public List<PropDef> props = new List<PropDef>();

        public PortalType portalType = PortalType.None;
        public string portalMount = "";
        public SpeedTier portalSpeed = SpeedTier.Normal;
        public OrbType orbType = OrbType.None;
        public PadType padType = PadType.None;
        public TriggerType triggerType = TriggerType.None;
        /// <summary>Short code shown on trigger placeholders.</summary>
        public string code = "";
        /// <summary>True when the object rotates continuously in play (blades).</summary>
        public bool spins;

        public string SpriteId => string.IsNullOrEmpty(spriteId) ? id : spriteId;

        public bool IsSolidLike => kind == ObjectKind.Solid || kind == ObjectKind.Slope;
        public bool HiddenInPlay => kind == ObjectKind.Trigger || kind == ObjectKind.StartPos;

        public PropDef GetProp(string key)
        {
            for (int i = 0; i < props.Count; i++) if (props[i].key == key) return props[i];
            return null;
        }

        // Fluent builder helpers used by the catalog ----------------------------

        public ObjectDefinition Size(float w, float h)
        {
            width = w;
            height = h;
            return this;
        }

        public ObjectDefinition Shape(PlaceholderShape s)
        {
            shape = s;
            return this;
        }

        public ObjectDefinition Collider(ColliderShape c, float hitbox = 1f)
        {
            collider = c;
            hitboxScale = hitbox;
            return this;
        }

        public ObjectDefinition Colors(Color primary, Color secondary)
        {
            primaryColor = primary;
            secondaryColor = secondary;
            return this;
        }

        public ObjectDefinition Colors(string primaryHex, string secondaryHex)
        {
            primaryColor = ObjectCatalog.Hex(primaryHex);
            secondaryColor = ObjectCatalog.Hex(secondaryHex);
            return this;
        }

        public ObjectDefinition Z(int layer, int order = 0)
        {
            defaultZLayer = layer;
            defaultZOrder = order;
            return this;
        }

        public ObjectDefinition Tags(params string[] t)
        {
            tags = t;
            return this;
        }

        public ObjectDefinition Desc(string d)
        {
            description = d;
            return this;
        }

        public ObjectDefinition Prop(PropDef p)
        {
            props.Add(p);
            return this;
        }

        public ObjectDefinition Code(string c)
        {
            code = c;
            return this;
        }

        public ObjectDefinition Spins()
        {
            spins = true;
            props.Add(new PropDef("spin", "Spin (deg/s)", PropType.Float, "180").Range(-1440, 1440, 10));
            return this;
        }
    }
}
