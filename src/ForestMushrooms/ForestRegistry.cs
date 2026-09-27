using System.Collections.Generic;
using ForestMushrooms.Core;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace ForestMushrooms;

public sealed class ForestRegistry : IUpdatableSingleton
{
    private readonly Dictionary<Vector3Int, ForestMushroom> _trees = new Dictionary<Vector3Int, ForestMushroom>();
    private bool _needsRefresh;
    public void Add(ForestMushroom tree)
    {
        _trees[tree.Coordinates] = tree;
        _needsRefresh = true;
    }
    public void Remove(ForestMushroom tree)
    {
        if (_trees.TryGetValue(tree.Coordinates, out var current) && current == tree)
        {
            _trees.Remove(tree.Coordinates);
            _needsRefresh = true;
        }
    }
    public void UpdateSingleton()
    {
        if (!_needsRefresh) return;
        _needsRefresh = false;
        // Singleton updates start after all entity loading/post-initialization.
        // They run while paused, unlike simulation ticks. Recheck with the complete
        // registry so save-file ordering cannot leave ripe crops without visuals.
        foreach (var tree in _trees.Values) tree.Synchronize();
    }
    public bool TryGet(Vector3Int cell, out ForestMushroom tree) => _trees.TryGetValue(cell, out tree);
    public int Neighbors(Vector3Int cell)
    {
        // Map XY is horizontal; Z is elevation. Trees on another terrace do not count.
        int count = 0;
        for (int i = 0; i < TreeEligibility.RequiredNeighbors; i++)
        {
            var offset = TreeEligibility.Offset(i);
            if (_trees.TryGetValue(cell + new Vector3Int(offset.X, offset.Y, 0), out var tree) && tree.Alive) count++;
        }
        return count;
    }
}
