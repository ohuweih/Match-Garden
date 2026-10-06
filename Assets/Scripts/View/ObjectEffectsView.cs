using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectEffectsView : MonoBehaviour
{
    public enum Kind { Stone, Ice, Package, Machine, Hit }
    public sealed class Cue
    {
        public SpriteRenderer Source;
        public Kind Type;
        public bool Remove;
        public Cue(SpriteRenderer source, Kind type, bool remove = true) { Source = source; Type = type; Remove = remove; }
    }
    private sealed class Fragment
    {
        public SpriteRenderer Renderer;
        public Vector3 Start, Velocity, Scale;
        public float Spin;
        public bool Lid;
        public Kind Type;
    }
    private Transform root;
    private readonly List<Sprite> temporarySprites = new List<Sprite>();
    private readonly List<Cue> sources = new List<Cue>();
    private readonly List<Vector3> sourcePositions = new List<Vector3>();
    private readonly List<Renderer> hiddenLabels = new List<Renderer>();

    public IEnumerator Play(List<Cue> cues)
    {
        Clear();
        if (cues.Count == 0) yield break;
        root = new GameObject("Garden object effects").transform;
        root.SetParent(transform, false);
        var fragments = new List<Fragment>();
        foreach (var cue in cues)
        {
            var source = cue.Source;
            if (source == null || source.sprite == null) continue;
            sources.Add(cue); sourcePositions.Add(source.transform.localPosition);
            if (cue.Type == Kind.Hit) continue;
            if (cue.Remove)
            {
                source.enabled = false;
                if (source.transform.parent != null)
                    foreach (var text in source.transform.parent.GetComponentsInChildren<TextMesh>())
                    {
                        var label = text.GetComponent<Renderer>();
                        if (label != null && label.enabled) { label.enabled = false; hiddenLabels.Add(label); }
                    }
            }
            int columns = cue.Type == Kind.Package ? 1 : 3;
            int rows = cue.Type == Kind.Package ? 2 : 3;
            for (int y = 0; y < rows; y++) for (int x = 0; x < columns; x++)
            {
                // Slice the existing sprite on the GPU; no duplicated image files or pixel readback.
                Rect full = source.sprite.rect;
                float y0 = cue.Type == Kind.Package ? (y == 0 ? 0f : .63f) : y / (float)rows;
                float y1 = cue.Type == Kind.Package ? (y == 0 ? .63f : 1f) : (y + 1f) / rows;
                var rect = new Rect(full.x + full.width * x / columns, full.y + full.height * y0, full.width / columns, full.height * (y1 - y0));
                var sprite = Sprite.Create(source.sprite.texture, rect, new Vector2(.5f, .5f), source.sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                temporarySprites.Add(sprite);
                var sr = new GameObject(cue.Type + " fragment").AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root, false); sr.sprite = sprite; sr.color = source.color;
                sr.sortingLayerID = source.sortingLayerID; sr.sortingOrder = source.sortingOrder + 10;
                Vector3 offset = new Vector3((rect.center.x - full.x - source.sprite.pivot.x) / source.sprite.pixelsPerUnit,
                    (rect.center.y - full.y - source.sprite.pivot.y) / source.sprite.pixelsPerUnit, 0);
                Vector3 center = source.transform.TransformPoint(offset);
                sr.transform.position = center; sr.transform.rotation = source.transform.rotation;
                sr.transform.localScale = source.transform.lossyScale;
                var velocity = new Vector3((x - 1) * .8f, .8f + y * .3f, 0);
                if (cue.Type == Kind.Ice) velocity = offset.normalized * 1.8f;
                if (cue.Type == Kind.Package) velocity = y == 1 ? new Vector3(.25f, 1.7f, 0) : Vector3.zero;
                fragments.Add(new Fragment { Renderer = sr, Start = center, Velocity = velocity, Scale = sr.transform.localScale,
                    Spin = cue.Type == Kind.Package ? (y == 1 ? -24f : 0f) : ((x + y) % 2 == 0 ? 1 : -1) * (35 + x * 20),
                    Lid = cue.Type == Kind.Package && y == 1, Type = cue.Type });
            }
            var fleck = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1, 0, SpriteMeshType.FullRect);
            temporarySprites.Add(fleck);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 2.39996f;
                var sr = new GameObject("Dust and glints").AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root, false); sr.sprite = fleck;
                sr.sortingLayerID = source.sortingLayerID; sr.sortingOrder = source.sortingOrder + 11;
                sr.color = cue.Type == Kind.Stone ? new Color(.85f, .77f, .57f) : cue.Type == Kind.Ice ? new Color(.7f, .95f, 1f) : i % 2 == 0 ? new Color(1f, .82f, .22f) : new Color(.45f, .8f, .20f);
                fragments.Add(new Fragment { Renderer = sr, Start = source.transform.position,
                    Velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) + 1f) * 1.3f,
                    Scale = new Vector3(.035f, .07f, 1), Spin = i % 2 == 0 ? 140 : -140, Type = cue.Type });
            }
        }
        const float duration = .52f;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = elapsed / duration;
            foreach (var fragment in fragments)
            {
                float move = Mathf.Max(0f, elapsed - .07f);
                bool package = fragment.Type == Kind.Package;
                var drop = package ? Vector3.zero : Vector3.down * move * move * (fragment.Type == Kind.Stone ? 6f : 3f);
                fragment.Renderer.transform.position = fragment.Start + fragment.Velocity * move + drop;
                fragment.Renderer.transform.rotation = Quaternion.Euler(0, 0, fragment.Spin * move);
                float shrink = package ? 1f : Mathf.Lerp(1f, .35f, t * t);
                if (fragment.Lid) fragment.Renderer.transform.localScale = Vector3.Scale(fragment.Scale, new Vector3(1f, 1f - .5f * t, 1));
                else fragment.Renderer.transform.localScale = fragment.Scale * shrink;
                var color = fragment.Renderer.color;
                color.a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - .5f) * 2f));
                fragment.Renderer.color = color;
            }
            for (int i = 0; i < sources.Count; i++)
                if (sources[i].Source != null && sources[i].Type == Kind.Hit)
                    sources[i].Source.transform.localPosition = sourcePositions[i] + Vector3.right * Mathf.Sin(t * 30f) * .045f * (1f - t);
            yield return null;
        }
        Clear();
    }

    public void Clear()
    {
        for (int i = 0; i < sources.Count; i++) if (sources[i].Source != null)
        {
            sources[i].Source.enabled = true;
            sources[i].Source.transform.localPosition = sourcePositions[i];
        }
        sources.Clear(); sourcePositions.Clear();
        foreach (var label in hiddenLabels) if (label != null) label.enabled = true;
        hiddenLabels.Clear();
        if (root != null) { root.gameObject.SetActive(false); Destroy(root.gameObject); root = null; }
        foreach (var sprite in temporarySprites) Destroy(sprite);
        temporarySprites.Clear();
    }
    private void OnDisable() { Clear(); }
    private void OnDestroy() { Clear(); }
}
