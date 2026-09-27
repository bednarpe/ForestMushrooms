using System.Collections.Generic;
using UnityEngine;

namespace ForestMushrooms;

// Unity destroys previews and entire scenes without invoking IDeletableEntity.
public sealed class ProceduralResources : MonoBehaviour
{
    private readonly List<Object> _owned = new List<Object>();
    public void Own(Object resource) => _owned.Add(resource);
    private void OnDestroy()
    {
        foreach (var resource in _owned) if (resource) Object.Destroy(resource);
        _owned.Clear();
    }
}
