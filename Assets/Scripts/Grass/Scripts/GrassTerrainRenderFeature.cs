using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Grass_1
{
    public class GrassTerrainRenderFeature : ScriptableRendererFeature
    {
        private GrassTerrainRenderPass pass = null;

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            if (cameraData.renderType == CameraRenderType.Base)
            {
                renderer.EnqueuePass(pass);
            }
        }

        public override void Create()
        {
            pass = new GrassTerrainRenderPass();
        }

        public class GrassTerrainRenderPass : ScriptableRenderPass
        {
            public GrassTerrainRenderPass()
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get("GrassTerrain 1.0");
                try
                {
                    cmd.Clear();
                    foreach (var grassTerrian in GrassTerrain.Actives)
                    {
                        if (!Application.isPlaying && Application.isEditor && !grassTerrian.executeInEditMode)
                            continue;
                        if (!grassTerrian || !grassTerrian.GrassMaterial || !grassTerrian.GrassMesh)
                            continue;
                        cmd.DrawMeshInstancedProcedural(
                            grassTerrian.GrassMesh, 
                            0, 
                            grassTerrian.GrassMaterial, 
                            0, 
                            grassTerrian.GrassCount, 
                            grassTerrian.MaterialPropertyBlock);
                    }
                    context.ExecuteCommandBuffer(cmd);
                }
                finally
                {
                    cmd.Release();
                }
            }
        }
    }
}