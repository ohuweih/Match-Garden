using System.Collections;
using UnityEngine;

// Stationary board object. No collider: it cannot be selected or swapped.
public sealed class GeneratorView : MonoBehaviour
{
    public int DisplayedHits { get; private set; }
    private Texture2D texture;
    private Sprite sprite;
    private TextMesh health;
    private SpriteRenderer core;

    public void Setup(float spacing, int sortingLayer, int sortingOrder, GeneratorType type, int hits, Sprite machineArt = null)
    {
        if (machineArt != null)
        {
            var art = new GameObject("Machine art"); art.transform.SetParent(transform, false);
            var renderer = art.AddComponent<SpriteRenderer>(); renderer.sprite = machineArt; renderer.color = Color.white;
            renderer.sortingLayerID = sortingLayer; renderer.sortingOrder = sortingOrder; core = renderer;
            health = Label("", -.29f, spacing, sortingLayer, sortingOrder + 3);
            health.transform.localPosition = new Vector3(.19f * spacing, -.29f * spacing, -.1f);
            health.characterSize = spacing * .027f; SetHits(hits); return;
        }

        texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white); texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        Plate("Housing", 0, 0, .90f, .90f, new Color(.35f, .48f, .58f), spacing, sortingLayer, sortingOrder);
        Plate("Panel", 0, 0, .78f, .78f, new Color(.08f, .17f, .24f), spacing, sortingLayer, sortingOrder + 1);
        Plate("Left outlet", -.47f, 0, .12f, .23f, Color.cyan, spacing, sortingLayer, sortingOrder + 1);
        Plate("Right outlet", .47f, 0, .12f, .23f, Color.cyan, spacing, sortingLayer, sortingOrder + 1);
        for (int i = 0; i < 3; i++)
        {
            var spoke = Plate("Cooling fan", 0, 0, .40f, .035f, new Color(.65f, .95f, 1f), spacing, sortingLayer, sortingOrder + 2);
            spoke.transform.localRotation = Quaternion.Euler(0, 0, i * 60f);
        }
        core = Plate("Core", 0, 0, .13f, .13f, Color.cyan, spacing, sortingLayer, sortingOrder + 3);
        Label("FROST", .29f, spacing, sortingLayer, sortingOrder + 3);
        health = Label("", -.29f, spacing, sortingLayer, sortingOrder + 3);
        // Packages can reveal a machine over frost; leave room for its lower-left layer badge.
        health.transform.localPosition = new Vector3(.19f * spacing, -.29f * spacing, -.1f);
        health.characterSize = spacing * .027f;
        SetHits(hits);
    }

    private SpriteRenderer Plate(string name, float x, float y, float width, float height, Color color,
        float spacing, int layer, int order)
    {
        var go = new GameObject(name); go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(x * spacing, y * spacing, 0);
        go.transform.localScale = new Vector3(width * spacing, height * spacing, 1);
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color;
        renderer.sortingLayerID = layer; renderer.sortingOrder = order;
        return renderer;
    }

    private TextMesh Label(string text, float y, float spacing, int layer, int order)
    {
        var go = new GameObject("Machine label"); go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0, y * spacing, -.1f);
        var label = go.AddComponent<TextMesh>();
        label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 48; label.characterSize = spacing * .035f; label.color = Color.white; label.text = text;
        var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = label.font.material;
        renderer.sortingLayerID = layer; renderer.sortingOrder = order;
        return label;
    }

    public void SetHits(int hits)
    {
        DisplayedHits = hits;
        gameObject.SetActive(hits > 0);
        health.text = hits + (hits == 1 ? " HIT" : " HITS");
        if (texture != null) core.color = hits == 1 ? new Color(1f, .65f, .25f) : Color.cyan;
    }

    public IEnumerator Pulse()
    {
        float elapsed = 0;
        while (elapsed < .25f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.one * (1f + .12f * Mathf.Sin(Mathf.Clamp01(elapsed / .25f) * Mathf.PI));
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    private void OnDestroy()
    {
        if (sprite != null) Destroy(sprite);
        if (texture != null) Destroy(texture);
    }
}
