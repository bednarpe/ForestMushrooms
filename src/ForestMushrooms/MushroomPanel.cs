using System;
using ForestMushrooms.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.Debugging;
using Timberborn.EntityPanelSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForestMushrooms;

public sealed class MushroomPanel : IEntityPanelFragment
{
    private readonly DevModeManager _devMode;
    private ForestMushroom _tree;
    private VisualElement _root, _debug;
    private Label _status, _details;
    private Button _mature, _pick;
    public MushroomPanel(DevModeManager devMode) { _devMode = devMode; }
    public VisualElement InitializeFragment()
    {
        _root = new VisualElement();
        _root.style.paddingTop = 8;
        _root.style.paddingBottom = 8;
        _root.style.paddingLeft = 12;
        _root.style.paddingRight = 12;
        _root.style.backgroundColor = new Color(0.12f, 0.18f, 0.12f, 0.85f);
        _status = new Label();
        _status.style.color = new Color(0.95f, 0.95f, 0.90f, 1f);
        _status.style.whiteSpace = WhiteSpace.Normal;
        _root.Add(_status);
        _debug = new VisualElement();
        _details = new Label();
        _details.style.color = new Color(0.95f, 0.95f, 0.90f, 1f);
        _details.style.whiteSpace = WhiteSpace.Normal;
        _debug.Add(_details);
        _mature = new Button(() => { if (_devMode.Enabled && _tree != null) _tree.DebugMature(); }) { text = "Mature mushrooms now" };
        _mature.tooltip = "Skips age and moisture timers. Requires a living mature tree, moist soil and four side-adjacent trees. Succulents do not count.";
        _pick = new Button(() => { if (_devMode.Enabled && _tree != null) _tree.DebugPick(); }) { text = "Simulate picking (no goods)" };
        _debug.Add(_mature);
        _debug.Add(_pick);
        _root.Add(_debug);
        return _root;
    }
    public void ShowFragment(BaseComponent entity) { _tree = entity.GetComponent<ForestMushroom>(); UpdateFragment(); }
    public void ClearFragment() { _tree = null; if (_root != null) _root.style.display = DisplayStyle.None; }
    public void UpdateFragment()
    {
        _root.style.display = _tree != null ? DisplayStyle.Flex : DisplayStyle.None;
        if (_tree == null) return;
        string crop = _tree.Available ? "1 ready to gather"
            : _tree.Growing ? "growing" : "not growing";
        _status.text = $"Mushrooms: {crop}\nNeighboring trees: {_tree.Neighbors}/4";
        _debug.style.display = _devMode.Enabled ? DisplayStyle.Flex : DisplayStyle.None;
        if (!_devMode.Enabled) return;
        var s = _tree.State;
        var timing = _tree.Timing;
        _details.text = "Status: " + Blocker();
        double wait = Math.Max(0, Math.Max(timing.MinimumAge - s.AgeDays, timing.RequiredWetDays - s.WetDays)) + s.RemainingDays;
        _details.text += $"\nTree age: {s.AgeDays:F2} / {timing.MinimumAge:F2} days\nConsecutive moist days: {s.WetDays:F2} / {timing.RequiredWetDays:F2}\nRandom wait remaining: {s.RemainingDays:F2} days\nEarliest crop: {(_tree.Available ? "now" : !_tree.Alive ? "none" : $"in {wait:F2} days, if conditions hold")}\nDead-tree crop lifetime left: {Math.Max(0, timing.DeadTreeDays - s.DeadDays):F2} days\nHarvests: {s.HarvestCount}";
        _mature.SetEnabled(_tree.Alive && _tree.Grown && _tree.Wet && !_tree.Contaminated && _tree.Neighbors == TreeEligibility.RequiredNeighbors && _tree.GoodsAvailable);
        _pick.SetEnabled(_tree.Available);
    }
    private string Blocker()
    {
        if (!_tree.GoodsAvailable) return "this faction has no mushroom good";
        if (_tree.Contaminated) return "soil is contaminated; growth reset";
        if (!_tree.Alive) return "tree is dead or cut";
        if (!_tree.Grown) return "tree is still growing";
        if (!_tree.Wet) return "soil is dry";
        if (_tree.Neighbors != TreeEligibility.RequiredNeighbors) return "needs trees north, south, east and west";
        if (_tree.State.AgeDays < _tree.Timing.MinimumAge) return "tree is too young";
        if (_tree.State.WetDays < _tree.Timing.RequiredWetDays) return $"needs {_tree.Timing.RequiredWetDays:F2} consecutive moist days";
        return "growing";
    }
}
