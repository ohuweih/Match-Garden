using UnityEngine;

// Stationary terrain with no collider: colored pieces and input remain usable.
public sealed class FrostTileView : MonoBehaviour
{
    public int DisplayedLayers { get; private set; }
    private Texture2D texture;
    private Sprite sprite;
    private SpriteRenderer surface;
    private GameObject secondLayer;
    private TextMesh label;

    public void Setup(float spacing, int sortingLayer, int sortingOrder, int layers, Sprite frostArt = null)
    {
        if (frostArt != null)
        {
            surface = new GameObject("Frost art").AddComponent<SpriteRenderer>();
            surface.transform.SetParent(transform, false);
            surface.sprite = frostArt; surface.color = Color.white;
            surface.sortingLayerID = sortingLayer; surface.sortingOrder = sortingOrder;
            secondLayer = new GameObject("Second frost layer"); secondLayer.transform.SetParent(transform, false);
            var second = secondLayer.AddComponent<SpriteRenderer>(); second.sprite = frostArt; second.color = new Color(1f,1f,1f,.45f);
            second.sortingLayerID = sortingLayer; second.sortingOrder = sortingOrder + 1;
            secondLayer.transform.localScale = Vector3.one * .92f;
            var layertext = new GameObject("Frost layers remaining"); layertext.transform.SetParent(transform, false);
            layertext.transform.localPosition = new Vector3(-spacing * .28f, -spacing * .32f, -.1f);
            label = layertext.AddComponent<TextMesh>(); label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = 48; label.characterSize = spacing * .04f; label.color = Color.white;
            var tr = layertext.GetComponent<MeshRenderer>(); tr.sharedMaterial = label.font.material; tr.sortingLayerID = sortingLayer; tr.sortingOrder = sortingOrder + 3;
            SetLayers(layers); return;
        }

        texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        surface = Plate(transform, "Frost surface", 0f, 0f, spacing * 0.86f, spacing * 0.86f,
            Color.white, sortingLayer, sortingOrder);
        Frame(transform, spacing * 0.43f, spacing * 0.035f, new Color(0.60f, 0.95f, 1f), sortingLayer, sortingOrder + 1);
        secondLayer = new GameObject("Second frost layer");
        secondLayer.transform.SetParent(transform, false);
        Frame(secondLayer.transform, spacing * 0.35f, spacing * 0.03f, Color.white, sortingLayer, sortingOrder + 1);
        // A corner badge leaves the central special marker and bird label readable.
        Plate(transform, "Layer badge", -spacing * 0.25f, -spacing * 0.32f, spacing * 0.36f, spacing * 0.23f,
            new Color(0.06f, 0.24f, 0.34f), sortingLayer, sortingOrder + 2);
        var text = new GameObject("Frost layers remaining");
        text.transform.SetParent(transform, false);
        text.transform.localPosition = new Vector3(-spacing * 0.25f, -spacing * 0.32f, -0.1f);
        label = text.AddComponent<TextMesh>();
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 48;
        label.characterSize = spacing * 0.04f;
        label.color = Color.white;
        var renderer = text.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = label.font.material;
        renderer.sortingLayerID = sortingLayer;
        renderer.sortingOrder = sortingOrder + 3;
        SetLayers(layers);
    }

    private SpriteRenderer Plate(Transform parent, string name, float x, float y, float width, float height,
        Color color, int layer, int order)
    {
        var plate = new GameObject(name);
        plate.transform.SetParent(parent, false);
        plate.transform.localPosition = new Vector3(x, y, 0f);
        plate.transform.localScale = new Vector3(width, height, 1f);
        var renderer = plate.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerID = layer;
        renderer.sortingOrder = order;
        return renderer;
    }

    private void Frame(Transform parent, float edge, float thickness, Color color, int layer, int order)
    {
        Plate(parent, "Top", 0, edge, edge * 2, thickness, color, layer, order);
        Plate(parent, "Bottom", 0, -edge, edge * 2, thickness, color, layer, order);
        Plate(parent, "Left", -edge, 0, thickness, edge * 2, color, layer, order);
        Plate(parent, "Right", edge, 0, thickness, edge * 2, color, layer, order);
    }

    public void SetLayers(int layers)
    {
        DisplayedLayers = layers;
        gameObject.SetActive(layers > 0);
        if (layers == 0) return;
        if (texture != null) surface.color = new Color(0.5f, 0.9f, 1f, layers == 2 ? 0.27f : 0.12f);
        else surface.color = Color.white;
        secondLayer.SetActive(layers == 2);
        label.text = "F" + layers;
    }

    private void OnDestroy()
    {
        if (sprite != null) Destroy(sprite);
        if (texture != null) Destroy(texture);
    }
}
