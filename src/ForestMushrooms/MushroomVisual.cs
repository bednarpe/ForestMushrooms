using UnityEngine;
using UnityEngine.SceneManagement;
using Timberborn.Coordinates;

namespace ForestMushrooms;

// Small original procedural fruiting bodies: no copied game or Leaf Coats assets.
public sealed class MushroomVisual
{
    private readonly GameObject _root;
    private readonly Material _cap;
    private readonly Material _stem;
    public MushroomVisual(Transform tree, Vector3Int tile, float baseZ)
    {
        _root = new GameObject("ForestMushrooms.Visual");
        // Tree roots are centered, randomized and can have every child renderer
        // disabled by NaturalResourceModel. Anchor this independent visual to the tile.
        SceneManager.MoveGameObjectToScene(_root, tree.gameObject.scene);
        _root.transform.position = CoordinateSystem.GridToWorld(new Vector3(tile.x, tile.y, tile.z + baseZ));
        var resources = _root.AddComponent<ProceduralResources>();
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null) return;
        _cap = new Material(shader) { color = new Color(0.52f, 0.23f, 0.10f) };
        _stem = new Material(shader) { color = new Color(0.86f, 0.75f, 0.52f) };
        resources.Own(_cap); resources.Own(_stem);
        // Small deterministic mottling supplies surface detail without external assets.
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        texture.name = "ForestMushrooms.Mottling";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        var random = new System.Random(731);
        var pixels = new Color[32 * 32];
        for (int p = 0; p < pixels.Length; p++)
        {
            float shade = .78f + (float)random.NextDouble() * .22f;
            pixels[p] = new Color(shade, shade, shade, 1);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        resources.Own(texture);
        foreach (var material in new[] { _cap, _stem })
        {
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .15f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .15f);
        }
        uint hash = unchecked((uint)tile.x * 73856093u ^ (uint)tile.y * 19349663u ^ (uint)tile.z * 83492791u);
        hash ^= hash >> 13;
        for (int i = 0; i < 2; i++)
        {
            float size = i == 0 ? 1f : .65f;
            float height = .145f * size;
            float x = .24f + (i == 1 ? .15f : 0f);
            float z = .24f + (i == 1 ? .035f : 0f);
            if ((hash & 4) != 0) { float swap = x; x = z; z = swap; }
            if ((hash & 1) != 0) x = 1 - x;
            if ((hash & 2) != 0) z = 1 - z;
            var position = new Vector3(x, height / 2, z);
            // Unity cylinders are two units tall: this puts each flat stem base at ground level.
            AddPart(position, new Vector3(0.045f * size, height / 2, 0.045f * size), _stem, PrimitiveType.Cylinder);
            AddPart(position + Vector3.up * (height / 2), new Vector3(0.24f, 0.075f, 0.21f) * size, _cap);
        }
    }
    private void AddPart(Vector3 position, Vector3 scale, Material material, PrimitiveType shape = PrimitiveType.Sphere)
    {
        var part = GameObject.CreatePrimitive(shape);
        Object.Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(_root.transform, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
    }
    public void SetVisible(bool visible) { if (_root && _root.activeSelf != visible) _root.SetActive(visible); }
    public void Dispose()
    {
        if (_root) Object.Destroy(_root);
    }
}
