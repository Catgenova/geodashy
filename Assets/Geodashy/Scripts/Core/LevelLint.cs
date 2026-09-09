using System.Collections.Generic;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>A problem the linter found, with a place to jump to.</summary>
    public class LintIssue
    {
        public string severity;   // "error" | "warning"
        public string message;
        public float x, y;
        public int uid;
    }

    /// <summary>
    /// Static checks for a level: gaps wider than the current mount can clear, hazards buried inside solids,
    /// triggers that target empty groups, start positions past the finish, objects under the ground.
    /// </summary>
    public static class LevelLint
    {
        /// <summary>Rough horizontal reach of one jump (blocks) for a ground mount at a speed tier.</summary>
        public static float JumpReach(MountDefinition m, int speedTier)
        {
            if (m.flying) return float.MaxValue;
            if (m.jumpVelocity <= 0f) return 3f;   // boar: flips rather than jumps
            float airTime = 2f * m.jumpVelocity / Mathf.Max(1f, m.gravity);
            return airTime * MountCatalog.Speed(speedTier) * 0.9f;
        }

        public static List<LintIssue> Check(LevelData level)
        {
            var issues = new List<LintIssue>();
            var objs = level.objects;
            float finishX = level.GetFinishX();
            var groups = new HashSet<int>();
            foreach (var o in objs) foreach (var g in o.groups) groups.Add(g);

            var solids = new List<(LevelObject o, ObjectDefinition d, Rect r)>();
            foreach (var o in objs)
            {
                var d = ObjectCatalog.Get(o.type);
                if (d == null)
                {
                    issues.Add(new LintIssue { severity = "error", message = "Unknown object type '" + o.type + "'", x = o.x, y = o.y, uid = o.uid });
                    continue;
                }
                var r = GeoMath.ObjectBounds(o, d);
                if (d.IsSolidLike) solids.Add((o, d, r));
                if (d.kind == ObjectKind.Trigger)
                {
                    foreach (var key in new[] { "target", "target1", "target2", "follow", "center" })
                    {
                        int g = o.GetInt(key, 0);
                        if (g > 0 && !groups.Contains(g)) issues.Add(new LintIssue { severity = "warning", message = d.name + " trigger targets group " + g + " but nothing is in that group", x = o.x, y = o.y, uid = o.uid });
                    }
                }
                if (d.kind == ObjectKind.StartPos && o.x > finishX - 0.5f) issues.Add(new LintIssue { severity = "error", message = "Start position sits at or after the finish", x = o.x, y = o.y, uid = o.uid });
                if (d.kind != ObjectKind.Trigger && d.kind != ObjectKind.Decoration && r.yMax < level.settings.groundY - 0.01f) issues.Add(new LintIssue { severity = "warning", message = d.name + " is below the ground and can never be reached", x = o.x, y = o.y, uid = o.uid });
                if (d.kind != ObjectKind.Trigger && d.kind != ObjectKind.Decoration && d.kind != ObjectKind.Finish && o.x > finishX + 1f) issues.Add(new LintIssue { severity = "warning", message = d.name + " is beyond the finish", x = o.x, y = o.y, uid = o.uid });
            }
            // hazards buried inside solids
            foreach (var o in objs)
            {
                var d = ObjectCatalog.Get(o.type);
                if (d == null || d.kind != ObjectKind.Hazard) continue;
                var r = GeoMath.ObjectBounds(o, d);
                foreach (var s in solids)
                {
                    if (s.r.xMin <= r.xMin + 0.05f && s.r.xMax >= r.xMax - 0.05f && s.r.yMin <= r.yMin + 0.05f && s.r.yMax >= r.yMax - 0.05f)
                    {
                        issues.Add(new LintIssue { severity = "warning", message = d.name + " is completely inside " + s.d.name + " (invisible hazard)", x = o.x, y = o.y, uid = o.uid });
                        break;
                    }
                }
            }
            // gaps: scan the ground row for stretches of hazards wider than the mount can jump
            var mount = MountCatalog.Get(level.settings.startMount);
            int speed = level.settings.startSpeed;
            float reach = JumpReach(mount, speed);
            var hazardsOnGround = new List<Rect>();
            var portals = new List<(float x, string mount, int speed)>();
            foreach (var o in objs)
            {
                var d = ObjectCatalog.Get(o.type);
                if (d == null) continue;
                if (d.kind == ObjectKind.Hazard)
                {
                    var r = GeoMath.ObjectBounds(o, d);
                    if (r.yMin <= level.settings.groundY + 0.6f) hazardsOnGround.Add(r);
                }
                if (d.kind == ObjectKind.Portal && d.portalType == PortalType.Mount) portals.Add((o.x, d.portalMount, -1));
                if (d.kind == ObjectKind.Portal && d.portalType == PortalType.Speed) portals.Add((o.x, "", (int)d.portalSpeed));
            }
            hazardsOnGround.Sort((a, b) => a.xMin.CompareTo(b.xMin));
            portals.Sort((a, b) => a.x.CompareTo(b.x));
            int pi = 0;
            int i = 0;
            while (i < hazardsOnGround.Count)
            {
                float start = hazardsOnGround[i].xMin, end = hazardsOnGround[i].xMax;
                int j = i + 1;
                while (j < hazardsOnGround.Count && hazardsOnGround[j].xMin <= end + 0.3f)
                {
                    end = Mathf.Max(end, hazardsOnGround[j].xMax);
                    j++;
                }
                while (pi < portals.Count && portals[pi].x < start)
                {
                    if (portals[pi].mount != "") mount = MountCatalog.Get(portals[pi].mount);
                    else speed = portals[pi].speed;
                    reach = JumpReach(mount, speed);
                    pi++;
                }
                float width = end - start;
                // a ledge above the stretch makes it passable; only flag open runs of hazards
                bool ledge = false;
                foreach (var s in solids) if (s.r.xMax > start && s.r.xMin < end && s.r.yMin > level.settings.groundY + 0.2f) { ledge = true; break; }
                if (!ledge && width > reach && !mount.flying)
                {
                    bool helper = false;
                    foreach (var o in objs)
                    {
                        var d = ObjectCatalog.Get(o.type);
                        if (d != null && (d.kind == ObjectKind.Orb || d.kind == ObjectKind.Pad) && o.x > start - 1f && o.x < end + 1f) { helper = true; break; }
                    }
                    if (!helper) issues.Add(new LintIssue { severity = "error", message = "Hazard stretch of " + width.ToString("0.#") + " blocks at x " + start.ToString("0.#") + " is wider than the " + mount.name + " can jump (~" + reach.ToString("0.#") + ") and has no rune, shroom or ledge", x = (start + end) / 2f, y = level.settings.groundY + 1f, uid = 0 });
                }
                i = j;
            }
            if (finishX <= 4f) issues.Add(new LintIssue { severity = "warning", message = "The level is almost empty: no finish gate and nothing to ride past", x = 2f, y = 2f, uid = 0 });
            return issues;
        }
    }
}
