using UnityEngine;

// Deform the wing regions of the existing robin art without seams or replacing its identity.
public sealed class BirdFlightView : MonoBehaviour
{
    private SpriteRenderer source;
    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private Material material;
    private Vector3[] rest, vertices;
    private Vector3 lastPosition;
    private float movingUntil;
    private bool bird;
    public void SetBird(SpriteRenderer renderer, bool isBird)
    {
        source = renderer; bird = isBird;
        lastPosition = transform.position;
        if (!bird && meshRenderer != null) meshRenderer.enabled = false;
    }
    private void Build()
    {
        const int n = 20;
        mesh = new Mesh { name = "Robin wing deformation" };
        rest = new Vector3[(n + 1) * (n + 1)]; vertices = new Vector3[rest.Length];
        var uv = new Vector2[rest.Length]; var colors = new Color[rest.Length]; var triangles = new int[n * n * 6];
        var sprite = source.sprite; var bounds = sprite.bounds; var rect = sprite.rect;
        for (int y = 0; y <= n; y++) for (int x = 0; x <= n; x++)
        {
            int i = y * (n + 1) + x; float u = x / (float)n, v = y / (float)n;
            rest[i] = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, u), Mathf.Lerp(bounds.min.y, bounds.max.y, v), 0);
            uv[i] = new Vector2((rect.x + rect.width * u) / sprite.texture.width, (rect.y + rect.height * v) / sprite.texture.height);
            colors[i] = Color.white;
            if (x == n || y == n) continue;
            int k = (y * n + x) * 6;
            triangles[k] = i; triangles[k + 1] = i + n + 1; triangles[k + 2] = i + 1;
            triangles[k + 3] = i + 1; triangles[k + 4] = i + n + 1; triangles[k + 5] = i + n + 2;
        }
        mesh.vertices = rest; mesh.uv = uv; mesh.colors = colors; mesh.triangles = triangles; mesh.MarkDynamic();
        var go = new GameObject("Flapping robin"); go.transform.SetParent(source.transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        meshRenderer = go.AddComponent<MeshRenderer>();
        material = new Material(Shader.Find("Sprites/Default")); material.mainTexture = sprite.texture;
        meshRenderer.sharedMaterial = material;
        meshRenderer.sortingLayerID = source.sortingLayerID; meshRenderer.sortingOrder = source.sortingOrder;
    }
    private void LateUpdate()
    {
        if (!bird || source == null || source.sprite == null) return;
        if ((transform.position - lastPosition).sqrMagnitude > .000001f) movingUntil = Time.time + .10f;
        lastPosition = transform.position;
        bool moving = Time.time < movingUntil;
        if (!moving)
        {
            if (meshRenderer != null) meshRenderer.enabled = false;
            source.enabled = true; return;
        }
        if (mesh == null) Build();
        source.enabled = false; meshRenderer.enabled = true;
        var bounds = source.sprite.bounds;
        float flap = Mathf.Sin(Time.time * 32f);
        for (int i = 0; i < rest.Length; i++)
        {
            Vector3 v = rest[i];
            float x = (v.x - bounds.min.x) / bounds.size.x;
            float y = (v.y - bounds.min.y) / bounds.size.y;
            float side = Mathf.Clamp01((Mathf.Abs(x - .52f) - .20f) / .28f);
            float wing = side * Mathf.Clamp01((y - .22f) / .12f) * Mathf.Clamp01((.77f - y) / .15f);
            v.y += flap * wing * bounds.size.y * .17f;
            v.x -= Mathf.Sign(x - .52f) * (.5f + .5f * flap) * wing * bounds.size.x * .07f;
            vertices[i] = v;
        }
        mesh.vertices = vertices; mesh.RecalculateBounds();
    }
    private void OnDisable()
    {
        if (meshRenderer != null) meshRenderer.enabled = false;
        if (bird && source != null) source.enabled = true;
    }
    private void OnDestroy() { if (mesh != null) Destroy(mesh); if (material != null) Destroy(material); }
}
