using UnityEngine;

// Child of a PieceView, so selection, swapping and all gravity animations work normally.
public sealed class PackageView : MonoBehaviour
{
    private Texture2D texture;
    private Sprite sprite;

    public void Setup(int sortingLayer, int sortingOrder, Sprite packageArt = null)
    {
        if (packageArt != null)
        {
            var art = new GameObject("Package art"); art.transform.SetParent(transform, false);
            var packagerenderer = art.AddComponent<SpriteRenderer>();
            packagerenderer.sprite = packageArt; packagerenderer.color = Color.white;
            packagerenderer.sortingLayerID = sortingLayer; packagerenderer.sortingOrder = sortingOrder;
            return;
        }

        texture = new Texture2D(1, 1); texture.SetPixel(0, 0, Color.white); texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
        Plate("Ribbon vertical", .15f, .86f, new Color(1f, .88f, .6f), sortingLayer, sortingOrder);
        Plate("Ribbon horizontal", .86f, .15f, new Color(1f, .88f, .6f), sortingLayer, sortingOrder);
        Plate("Mystery tag", .42f, .5f, new Color(.25f, .16f, .09f), sortingLayer, sortingOrder + 1);
        var text = new GameObject("Package question mark"); text.transform.SetParent(transform, false);
        text.transform.localPosition = new Vector3(0, 0, -.1f);
        var label = text.AddComponent<TextMesh>();
        label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 48; label.characterSize = .065f; label.color = Color.white; label.text = "?";
        var renderer = text.GetComponent<MeshRenderer>(); renderer.sharedMaterial = label.font.material;
        renderer.sortingLayerID = sortingLayer; renderer.sortingOrder = sortingOrder + 2;
    }

    private void Plate(string name, float width, float height, Color color, int layer, int order)
    {
        var go = new GameObject(name); go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(width, height, 1);
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color;
        renderer.sortingLayerID = layer; renderer.sortingOrder = order;
    }

    private void OnDestroy()
    {
        if (sprite != null) Destroy(sprite);
        if (texture != null) Destroy(texture);
    }
}
