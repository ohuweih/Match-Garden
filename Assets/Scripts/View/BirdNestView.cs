using System.Collections.Generic;
using UnityEngine;

// A stationary overlay: stays visible over pieces and never intercepts input.
public sealed class BirdNestView : MonoBehaviour
{
    private readonly List<int> birds = new List<int>();
    private Texture2D texture;
    private Sprite sprite;
    private TextMesh label;

    public void Setup(float spacing, int sortingLayer, int sortingOrder)
    {
        texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        float edge = spacing * 0.45f;
        float length = spacing * 0.94f;
        float thickness = spacing * 0.04f;
        Color gold = new Color(1f, 0.8f, 0.15f);
        AddPlate("Top", 0, edge, length, thickness, gold, sortingLayer, sortingOrder);
        AddPlate("Bottom", 0, -edge, length, thickness, gold, sortingLayer, sortingOrder);
        AddPlate("Left", -edge, 0, thickness, length, gold, sortingLayer, sortingOrder);
        AddPlate("Right", edge, 0, thickness, length, gold, sortingLayer, sortingOrder);
        AddPlate("Nest badge", 0, spacing * 0.34f, spacing * 0.72f, spacing * 0.18f,
            new Color(0.12f, 0.16f, 0.2f), sortingLayer, sortingOrder + 1);
        var text = new GameObject("Nest number");
        text.transform.SetParent(transform, false);
        text.transform.localPosition = new Vector3(0, spacing * 0.34f, -0.1f);
        label = text.AddComponent<TextMesh>();
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 48;
        label.characterSize = spacing * 0.08f;
        label.color = gold;
        var renderer = text.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = label.font.material;
        renderer.sortingLayerID = sortingLayer;
        renderer.sortingOrder = sortingOrder + 2;
    }

    private void AddPlate(string name, float x, float y, float width, float height,
        Color color, int sortingLayer, int sortingOrder)
    {
        var plate = new GameObject(name);
        plate.transform.SetParent(transform, false);
        plate.transform.localPosition = new Vector3(x, y, 0);
        plate.transform.localScale = new Vector3(width, height, 1);
        var renderer = plate.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerID = sortingLayer;
        renderer.sortingOrder = sortingOrder;
    }

    public void AddBird(int number)
    {
        if (!birds.Contains(number)) birds.Add(number);
        UpdateLabel();
    }

    // Shared destinations stay highlighted until every assigned bird is rescued.
    public bool RemoveBird(int number)
    {
        birds.Remove(number);
        UpdateLabel();
        return birds.Count == 0;
    }

    private void UpdateLabel()
    {
        label.text = "Nest " + string.Join(",", birds);
        label.transform.localScale = Vector3.one * Mathf.Min(1f, 10f / label.text.Length);
    }

    private void OnDestroy()
    {
        if (sprite != null) Destroy(sprite);
        if (texture != null) Destroy(texture);
    }
}
