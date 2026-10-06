using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// One round-wide clock keeps arrivals, tile breaks and chained specials in sync.
// No gameplay decisions or random target selection belong in this component.
public sealed class SpecialEffectsView : MonoBehaviour
{
    private Transform effectRoot;
    private Sprite softDot, leaf, triangle;
    private Material lineMaterial;
    private readonly List<Texture2D> textures = new List<Texture2D>();
    private int layer, order;
    private float unit;
    private BoardView board;
    private static readonly Color Gold = new Color(1f, .75f, .20f);
    private static readonly Color Cream = new Color(1f, .97f, .72f);
    private static readonly Color Green = new Color(.27f, .72f, .20f);
    private static readonly Color Purple = new Color(.74f, .37f, 1f);

    public static float ImpactDelay(SpecialEffectRecord effect, Vector2Int target)
    {
        float distance = Vector2Int.Distance(effect.Origin, target);
        switch (effect.Type)
        {
            case SpecialType.LineHorizontal:
            case SpecialType.LineVertical:
                float longest = 1f;
                foreach (var point in effect.Targets)
                    longest = Mathf.Max(longest, Vector2Int.Distance(effect.Origin, point));
                return .12f + .40f * distance / longest;
            case SpecialType.Bomb:
                return .14f + .30f * distance / (effect.Radius * Mathf.Sqrt(2f));
            case SpecialType.Target:
                return .72f + .07f * Mathf.Max(0, effect.Targets.IndexOf(target));
            case SpecialType.ColorClear:
                return .65f + Mathf.Min(.18f, distance * .018f);
            default: return .1f;
        }
    }

    public IEnumerator Play(BoardView boardView, ResolutionRound round, float spacing, float popDuration)
    {
        Clear();
        board = boardView; unit = spacing;
        EnsureAssets();
        foreach (var cell in round.ClearedCells)
        {
            var piece = board.GetPieceView(cell.X, cell.Y);
            var renderer = piece != null ? piece.GetComponentInChildren<SpriteRenderer>() : null;
            if (renderer == null) continue;
            layer = renderer.sortingLayerID; order = renderer.sortingOrder + 30; break;
        }
        effectRoot = new GameObject("Special Effects").transform;
        effectRoot.SetParent(transform, false);
        var starts = new Dictionary<SpecialEffectRecord, float>();
        var breaks = new Dictionary<Vector2Int, float>();
        var visuals = new List<EffectVisual>();
        var impactPoints = new HashSet<Vector2Int>();
        float finish = 0f;
        foreach (var effect in round.SpecialEffects)
        {
            float start = 0f;
            if (effect.Parent != null && starts.TryGetValue(effect.Parent, out float parentStart))
                start = parentStart + ImpactDelay(effect.Parent, effect.Origin);
            starts.Add(effect, start);
            SetEarliest(breaks, effect.Origin, start + .12f);
            float lastHit = .2f;
            foreach (var target in effect.Targets)
            {
                float hit = ImpactDelay(effect, target);
                SetEarliest(breaks, target, start + hit);
                lastHit = Mathf.Max(lastHit, hit);
            }
            finish = Mathf.Max(finish, start + lastHit + .34f);
        }
        foreach (var effect in round.SpecialEffects)
        {
            float lastHit = .2f;
            foreach (var target in effect.Targets) lastHit = Mathf.Max(lastHit, ImpactDelay(effect, target));
            visuals.Add(new EffectVisual(this, effect, starts[effect], lastHit + .34f, impactPoints, breaks));
        }
        var pieces = new List<BreakVisual>();
        foreach (var cell in round.ClearedCells)
        {
            var piece = board.GetPieceView(cell.X, cell.Y);
            if (piece == null) continue;
            breaks.TryGetValue(new Vector2Int(cell.X, cell.Y), out float hit);
            pieces.Add(new BreakVisual { Piece = piece, Scale = piece.transform.localScale, Hit = hit });
            finish = Mathf.Max(finish, hit + popDuration);
        }
        float elapsed = 0f;
        while (elapsed <= finish)
        {
            foreach (var visual in visuals) visual.Draw(elapsed - visual.Start);
            foreach (var piece in pieces)
            {
                if (piece.Piece == null || elapsed < piece.Hit) continue;
                float p = Mathf.Clamp01((elapsed - piece.Hit) / Mathf.Max(.08f, popDuration));
                float scale = p < .25f ? Mathf.Lerp(1f, 1.2f, p * 4f) : Mathf.Lerp(1.2f, 0f, (p - .25f) / .75f);
                piece.Piece.transform.localScale = piece.Scale * scale;
            }
            yield return null;
            elapsed += Time.deltaTime;
        }
        Clear();
    }

    private static void SetEarliest(Dictionary<Vector2Int, float> map, Vector2Int point, float time)
    { if (!map.TryGetValue(point, out float old) || time < old) map[point] = time; }
    private sealed class BreakVisual { public PieceView Piece; public Vector3 Scale; public float Hit; }

    public void Clear()
    {
        if (effectRoot == null) return;
        effectRoot.gameObject.SetActive(false);
        Destroy(effectRoot.gameObject); effectRoot = null;
    }
    private void OnDisable() { Clear(); }
    private void OnDestroy()
    {
        Clear();
        if (softDot != null) Destroy(softDot);
        if (leaf != null) Destroy(leaf);
        if (triangle != null) Destroy(triangle);
        if (lineMaterial != null) Destroy(lineMaterial);
        foreach (var texture in textures) Destroy(texture);
    }

    private void EnsureAssets()
    {
        if (softDot != null) return;
        softDot = MakeSprite(0); leaf = MakeSprite(1); triangle = MakeSprite(2);
        lineMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Garden FX Lines", hideFlags = HideFlags.DontSave };
    }
    private Sprite MakeSprite(int shape)
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Garden FX Shape", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float u = (x + .5f) / size * 2f - 1f, v = (y + .5f) / size * 2f - 1f;
            float edge = shape == 0 ? 1f - Mathf.Sqrt(u * u + v * v) :
                shape == 1 ? 1f - Mathf.Abs(u) - v * v : Mathf.Min(v + 1f, (1f - v) * .5f - Mathf.Abs(u));
            pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(edge * (shape == 0 ? 9f : 25f)));
        }
        texture.SetPixels(pixels); texture.Apply(false, true); textures.Add(texture);
        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size, 0, SpriteMeshType.FullRect);
        sprite.hideFlags = HideFlags.DontSave; return sprite;
    }
    private SpriteRenderer Dot(Transform parent, Sprite sprite, Color color, int depth = 0)
    {
        var sr = new GameObject("FX sprite").AddComponent<SpriteRenderer>();
        sr.transform.SetParent(parent, false); sr.sprite = sprite; sr.color = color;
        sr.sortingLayerID = layer; sr.sortingOrder = order + depth; return sr;
    }
    private LineRenderer Line(Transform parent, Color color, float width, int count, int depth = 0)
    {
        var line = new GameObject("FX trail").AddComponent<LineRenderer>();
        line.transform.SetParent(parent, false); line.sharedMaterial = lineMaterial;
        line.useWorldSpace = true; line.positionCount = count; line.startColor = line.endColor = color;
        line.widthMultiplier = width * unit; line.numCapVertices = 3; line.numCornerVertices = 2;
        line.sortingLayerID = layer; line.sortingOrder = order + depth;
        return line;
    }
    private void Ring(LineRenderer ring, Vector3 center, float radius, Color color)
    {
        ring.startColor = ring.endColor = color;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / (ring.positionCount - 1);
            ring.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * unit);
        }
    }
    private static Color Alpha(Color c, float a) { c.a *= Mathf.Clamp01(a); return c; }
    private Vector3 World(Vector2Int point) { return board.GetWorldPosition(point.x, point.y); }

    private sealed class EffectVisual
    {
        public readonly float Start, Duration;
        private readonly SpecialEffectsView owner;
        private readonly SpecialEffectRecord record;
        private readonly Transform root;
        private readonly Vector3 origin;
        private readonly List<Flight> flights = new List<Flight>();
        private readonly List<Impact> impacts = new List<Impact>();
        private SpriteRenderer fire, core;
        private LineRenderer shock;
        private SpriteRenderer[] sparks, smoke, fireballs;

        public EffectVisual(SpecialEffectsView owner, SpecialEffectRecord record, float start, float duration, HashSet<Vector2Int> impactPoints, Dictionary<Vector2Int, float> breaks)
        {
            this.owner = owner; this.record = record; Start = start; Duration = duration;
            origin = owner.World(record.Origin);
            root = new GameObject(record.Type.ToString()).transform; root.SetParent(owner.effectRoot, false);
            if (record.Type == SpecialType.Bomb)
            {
                fire = owner.Dot(root, owner.softDot, Gold); core = owner.Dot(root, owner.softDot, Cream, 1);
                shock = owner.Line(root, Gold, .07f, 49, 2);
                sparks = new SpriteRenderer[18]; smoke = new SpriteRenderer[7];
                fireballs = new SpriteRenderer[8];
                for (int i = 0; i < fireballs.Length; i++) fireballs[i] = owner.Dot(root, owner.softDot, Gold, 1);
                for (int i = 0; i < sparks.Length; i++) sparks[i] = owner.Dot(root, owner.leaf, i % 2 == 0 ? Gold : new Color(1f, .36f, .08f), 3);
                for (int i = 0; i < smoke.Length; i++) smoke[i] = owner.Dot(root, owner.softDot, new Color(.51f, .34f, .18f, .3f), -1);
            }
            else if (record.Type == SpecialType.LineHorizontal || record.Type == SpecialType.LineVertical)
            {
                bool horizontal = record.Type == SpecialType.LineHorizontal;
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector2Int end = record.Origin;
                    foreach (var target in record.Targets)
                        if ((horizontal ? target.x - end.x : target.y - end.y) * sign > 0) end = target;
                    if (end == record.Origin) continue;
                    flights.Add(new Flight(owner, root, origin, owner.World(end), record, end, sign, true));
                }
            }
            else
            {
                for (int i = 0; i < record.Targets.Count; i++)
                    flights.Add(new Flight(owner, root, origin, owner.World(record.Targets[i]), record, record.Targets[i], i, false));
            }
            // Small impacts stay local, leaving the garden and unaffected tiles readable.
            foreach (var target in record.Targets)
                if (owner.board.GetPieceView(target.x, target.y) != null &&
                    Mathf.Approximately(start + ImpactDelay(record, target), breaks[target]) && impactPoints.Add(target))
                    impacts.Add(new Impact(owner, root, owner.World(target), ImpactDelay(record, target), record.Type == SpecialType.ColorClear));
            root.gameObject.SetActive(false);
        }
        public void Draw(float time)
        {
            bool visible = time >= 0f && time <= Duration;
            if (root.gameObject.activeSelf != visible) root.gameObject.SetActive(visible);
            if (!visible) return;
            foreach (var flight in flights) flight.Draw(time);
            foreach (var impact in impacts) impact.Draw(time);
            if (record.Type != SpecialType.Bomb) return;
            float t = Mathf.Clamp01((time - .10f) / .56f);
            float radius = record.Radius * Mathf.Sqrt(2f);
            fire.transform.position = core.transform.position = origin;
            fire.transform.localScale = Vector3.one * owner.unit * Mathf.Lerp(.3f, radius * 2f, Mathf.Sqrt(t));
            core.transform.localScale = Vector3.one * owner.unit * Mathf.Lerp(.2f, 1.9f, t);
            fire.color = Alpha(new Color(1f, .33f, .035f, .65f), (1f - t) * (1f - t));
            core.color = Alpha(Cream, 1f - Mathf.Clamp01(t * 1.8f));
            float wave = Mathf.Clamp01((time - .14f) / .30f);
            owner.Ring(shock, origin, Mathf.Lerp(.08f, radius, wave), Alpha(Gold, 1f - Mathf.Clamp01((time - .34f) / .30f)));
            for (int i = 0; i < fireballs.Length; i++)
            {
                float a = i * Mathf.PI * 2f / fireballs.Length;
                float expansion = Mathf.Sin(Mathf.Min(1f, t * 1.7f) * Mathf.PI * .5f);
                fireballs[i].transform.position = origin + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * owner.unit * expansion * (i % 2 == 0 ? 1.1f : .7f);
                fireballs[i].transform.localScale = Vector3.one * owner.unit * (.35f + expansion * 1.15f);
                fireballs[i].color = Alpha(i % 2 == 0 ? new Color(1f, .42f, .04f) : new Color(1f, .80f, .18f), Mathf.Pow(1f - t, 1.2f) * .85f);
            }
            for (int i = 0; i < sparks.Length; i++)
            {
                float a = i * 2.39996f;
                var direction = new Vector3(Mathf.Cos(a), Mathf.Sin(a));
                sparks[i].transform.position = origin + direction * owner.unit * radius * t * (i % 3 == 0 ? 1f : .75f) + Vector3.down * t * t * .35f * owner.unit;
                sparks[i].transform.rotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg - 90f);
                sparks[i].transform.localScale = new Vector3(.13f, .48f, 1f) * owner.unit * (1f - t);
                sparks[i].color = Alpha(i % 2 == 0 ? Gold : new Color(1f, .36f, .08f), 1f - t);
            }
            for (int i = 0; i < smoke.Length; i++)
            {
                float a = i * Mathf.PI * 2f / smoke.Length;
                smoke[i].transform.position = origin + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * owner.unit * radius * t * .65f;
                smoke[i].transform.localScale = Vector3.one * owner.unit * (.25f + t * 1.15f);
                smoke[i].color = new Color(.51f, .34f, .18f, Mathf.Sin(t * Mathf.PI) * .22f);
            }
        }
    }

    private sealed class Flight
    {
        private readonly SpecialEffectsView owner;
        private readonly Transform root, head;
        private readonly Vector3 start, end, bend;
        private readonly float arrival, launch;
        private readonly bool rocket, vine;
        private readonly LineRenderer trail, highlight, wrap;
        private readonly SpriteRenderer wingA, wingB;
        private readonly SpriteRenderer[] leaves;
        private readonly Color color;

        public Flight(SpecialEffectsView owner, Transform parent, Vector3 start, Vector3 end, SpecialEffectRecord record, Vector2Int target, int index, bool rocket)
        {
            this.owner = owner; this.start = start; this.end = end; this.rocket = rocket;
            vine = record.Type == SpecialType.ColorClear;
            arrival = ImpactDelay(record, target); launch = rocket ? .12f : .10f;
            color = vine ? Green : rocket ? Gold : Purple;
            root = new GameObject(vine ? "Growing vine" : rocket ? "Rocket" : "Seeker").transform;
            root.SetParent(parent, false);
            Vector3 delta = end - start;
            bend = new Vector3(-delta.y, delta.x).normalized * owner.unit * (index % 2 == 0 ? 1f : -1f) * (vine ? .6f : 1.2f);
            trail = owner.Line(root, color, vine ? .10f : .13f, 25);
            highlight = owner.Line(root, vine ? new Color(.70f, .94f, .30f) : Cream, .035f, 25, 1);
            head = new GameObject("Tip").transform; head.SetParent(root, false);
            if (vine)
            {
                var bud = owner.Dot(head, owner.leaf, new Color(.67f, .92f, .25f), 3);
                bud.transform.localScale = new Vector3(.16f, .29f, 1f) * owner.unit;
                leaves = new SpriteRenderer[6];
                for (int i = 0; i < leaves.Length; i++) leaves[i] = owner.Dot(root, owner.leaf, i % 2 == 0 ? Green : new Color(.62f, .85f, .20f), 2);
                wrap = owner.Line(root, Green, .065f, 33, 3);
            }
            else if (rocket)
            {
                var outline = owner.Dot(head, owner.softDot, new Color(.57f, .19f, .09f), 2);
                outline.transform.localScale = new Vector3(.31f, .62f, 1f) * owner.unit;
                var body = owner.Dot(head, owner.softDot, Cream, 3);
                body.transform.localScale = new Vector3(.25f, .56f, 1f) * owner.unit;
                var nose = owner.Dot(head, owner.triangle, new Color(.95f, .25f, .20f), 4);
                nose.transform.localPosition = Vector3.up * .28f * owner.unit;
                nose.transform.localScale = new Vector3(.32f, .34f, 1f) * owner.unit;
                var fins = owner.Dot(head, owner.triangle, new Color(.95f, .35f, .20f), 2);
                fins.transform.localPosition = Vector3.down * .16f * owner.unit;
                fins.transform.localScale = new Vector3(.47f, .30f, 1f) * owner.unit;
                var flame = owner.Dot(head, owner.leaf, Gold, 1);
                flame.transform.localPosition = Vector3.down * .46f * owner.unit;
                flame.transform.localScale = new Vector3(.20f, .55f, 1f) * owner.unit;
            }
            else
            {
                wingA = owner.Dot(head, owner.leaf, Purple, 3); wingB = owner.Dot(head, owner.leaf, new Color(1f, .55f, .82f), 3);
                var body = owner.Dot(head, owner.softDot, Cream, 4);
                body.transform.localScale = new Vector3(.12f, .35f, 1f) * owner.unit;
                var halo = owner.Dot(head, owner.softDot, new Color(.86f, .57f, 1f, .18f), 2);
                halo.transform.localScale = Vector3.one * .65f * owner.unit;
            }
        }
        private Vector3 Point(float t)
        {
            if (rocket) return Vector3.Lerp(start, end, t);
            return Vector3.Lerp(start, end, t) + bend * Mathf.Sin(t * Mathf.PI) +
                (vine ? Vector3.up * Mathf.Sin(t * Mathf.PI * 4f) * .11f * owner.unit : Vector3.up * Mathf.Sin(t * Mathf.PI) * .30f * owner.unit);
        }
        public void Draw(float time)
        {
            float t = Mathf.Clamp01((time - launch) / (arrival - launch));
            bool visible = time >= launch && time < arrival + (vine ? .22f : .06f);
            root.gameObject.SetActive(visible); if (!visible) return;
            float fade = time <= arrival ? 1f : 1f - (time - arrival) / (vine ? .22f : .06f);
            var point = Point(t);
            head.position = point;
            Vector3 direction = Point(Mathf.Min(1f, t + .01f)) - Point(Mathf.Max(0f, t - .01f));
            head.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            head.localScale = Vector3.one * fade;
            for (int i = 0; i < 25; i++)
            {
                float segment = i / 24f;
                float path = vine ? t * segment : Mathf.Lerp(Mathf.Max(0f, t - .22f), t, segment);
                Vector3 pos = Point(path);
                trail.SetPosition(i, pos); highlight.SetPosition(i, pos);
            }
            trail.startColor = Alpha(color, (vine ? 1f : 0f) * fade); trail.endColor = Alpha(color, fade);
            highlight.startColor = Alpha(vine ? new Color(.70f, .94f, .30f) : Cream, .5f * fade);
            highlight.endColor = Alpha(Cream, fade);
            if (wingA != null)
            {
                float flap = .45f + .55f * Mathf.Abs(Mathf.Sin(time * 32f));
                wingA.transform.localPosition = Vector3.left * .16f * owner.unit * flap;
                wingB.transform.localPosition = Vector3.right * .16f * owner.unit * flap;
                wingA.transform.localScale = wingB.transform.localScale = new Vector3(.38f * flap, .32f, 1f) * owner.unit;
                wingA.transform.localRotation = Quaternion.Euler(0, 0, -35f);
                wingB.transform.localRotation = Quaternion.Euler(0, 0, 35f);
            }
            if (!vine) return;
            for (int i = 0; i < leaves.Length; i++)
            {
                float along = (i + 1f) / (leaves.Length + 1f);
                float grown = Mathf.Clamp01((t - along) * 12f) * fade;
                leaves[i].transform.position = Point(along);
                leaves[i].transform.localScale = new Vector3(.17f, .37f, 1f) * owner.unit * grown;
                leaves[i].transform.rotation = Quaternion.Euler(0, 0, i % 2 == 0 ? -45f : 65f);
            }
            float curl = Mathf.Clamp01((t - .82f) / .18f);
            wrap.startColor = wrap.endColor = Alpha(Green, fade * curl);
            for (int i = 0; i < wrap.positionCount; i++)
            {
                float angle = i / (wrap.positionCount - 1f) * Mathf.PI * 2.5f * curl;
                wrap.SetPosition(i, end + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * owner.unit * Mathf.Lerp(.48f, .23f, Mathf.Clamp01((time - arrival) / .16f)));
            }
        }
    }

    private sealed class Impact
    {
        private readonly SpecialEffectsView owner;
        private readonly Transform root;
        private readonly SpriteRenderer[] motes = new SpriteRenderer[5];
        private readonly Vector3 center;
        private readonly float hit;
        private readonly bool vine;
        public Impact(SpecialEffectsView owner, Transform parent, Vector3 center, float hit, bool vine)
        {
            this.owner = owner; this.center = center; this.hit = hit; this.vine = vine;
            root = new GameObject("Tile impact").transform; root.SetParent(parent, false);
            for (int i = 0; i < motes.Length; i++) motes[i] = owner.Dot(root, vine ? owner.leaf : owner.softDot, vine ? Green : Gold, 5);
            root.gameObject.SetActive(false);
        }
        public void Draw(float time)
        {
            float t = (time - hit) / .28f;
            bool visible = t >= 0f && t < 1f; root.gameObject.SetActive(visible); if (!visible) return;
            for (int i = 0; i < motes.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / motes.Length + .4f;
                motes[i].transform.position = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * owner.unit * t * .65f;
                motes[i].transform.localScale = new Vector3(vine ? .13f : .1f, vine ? .25f : .1f, 1f) * owner.unit * (1f - t);
                motes[i].transform.rotation = Quaternion.Euler(0, 0, i * 70f + t * 120f);
                motes[i].color = Alpha(vine ? Green : Cream, 1f - t);
            }
        }
    }
}
