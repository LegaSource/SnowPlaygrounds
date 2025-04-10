using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace SnowPlaygrounds.Behaviours;

public class FrozenCustomPass : CustomPass
{
    public Dictionary<Renderer, Material> rendererMaterials = [];

    public void AddTargetRenderers(Renderer[] renderers, Material material)
    {
        foreach (Renderer renderer in renderers)
        {
            if (rendererMaterials.ContainsKey(renderer)) continue;
            rendererMaterials[renderer] = material;
        }
    }

    public void RemoveTargetRenderers(List<Renderer> renderers)
    {
        foreach (Renderer renderer in renderers) _ = rendererMaterials.Remove(renderer);
    }

    public override void Execute(CustomPassContext ctx)
    {
        foreach (KeyValuePair<Renderer, Material> rendererMaterial in rendererMaterials)
        {
            Renderer renderer = rendererMaterial.Key;
            Material material = rendererMaterial.Value;

            if (renderer == null || renderer.sharedMaterials == null) continue;

            for (int i = 0; i < renderer.sharedMaterials.Length; i++) ctx.cmd.DrawRenderer(renderer, material);
        }
    }
}
