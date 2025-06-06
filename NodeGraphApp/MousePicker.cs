﻿using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace NodeGraphApp;

public sealed class MousePicker
{
    private readonly NodeGraph _nodeGraph;
    
    public VisualNode? HoveredNode { get; private set; }
    public Link? HoveredLink { get; private set; }

    public Mouse Mouse { get; }
    public Viewport Viewport { get; }
    public Vector2 MouseWorldPosition => Viewport.ScreenToWorldPoint(Mouse.Position);

    public MousePicker(Viewport viewport, Mouse mouse, NodeGraph nodeGraph)
    {
        Mouse = mouse;
        Viewport = viewport;
        _nodeGraph = nodeGraph;
    }
    
    public void Update()
    {
        var mousePosition = Mouse.Position;
        var worldPosition = Viewport.ScreenToWorldPoint(mousePosition);
        var scene = _nodeGraph;
        VisualNode? hoveredNode = null;
        foreach (var node in scene.Nodes.TraverseDepthFirstPostOrder())
        {
            var bounds = node.Bounds;
            if (bounds.Contains(worldPosition))
            {
                hoveredNode = node;
                break;
            }
        }

        HoveredNode = hoveredNode;

        Link? hoveredLink = null;
        var links = _nodeGraph.BackgroundLinks.GetAll();
        foreach (var link in links)
        {
            var bounds = link.Bounds;
            if (bounds.Contains(worldPosition))
            {
                var p0 = link.P0;
                var p1 = link.P1;
                var p2 = link.P2;
                var p3 = link.P3;
                if (BezierUtils.IsPointOverBezier(worldPosition, p0, p1, p2, p3))
                {
                    hoveredLink = link;
                    break;
                }
            }
        }

        HoveredLink = hoveredLink;
    }

    public bool TryPick<T>([NotNullWhen(true)] out T? result, bool includeChildren = true) where T : VisualNode
    {
        if (HoveredNode == null)
        {
            result = null;
            return false;
        }

        result = HoveredNode as T;
        if (result != null)
            return true;

        if (includeChildren && HoveredNode.ChildOf<T>(out result))
            return true;

        return false;
    }

    public bool TryPickLink([NotNullWhen(true)] out Link? pickedLink)
    {
        if (HoveredLink != null)
        {
            pickedLink = HoveredLink;
            return true;
        }

        pickedLink = null;
        return false;
    }
}