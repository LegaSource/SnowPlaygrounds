using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using SnowPlaygrounds.Behaviours;

namespace SnowPlaygrounds.Managers
{
    public class CustomPassManager : MonoBehaviour
    {
        public static FrozenCustomPass frozenPass;
        public static CustomPassVolume customPassVolume;
        public static Dictionary<EnemyAI, List<Renderer>> frozenEnemies = new Dictionary<EnemyAI, List<Renderer>>();

        public static CustomPassVolume CustomPassVolume
        {
            get
            {
                if (customPassVolume == null)
                {
                    customPassVolume = GameNetworkManager.Instance.localPlayerController.gameplayCamera.gameObject.AddComponent<CustomPassVolume>();
                    if (customPassVolume != null)
                    {
                        customPassVolume.targetCamera = GameNetworkManager.Instance.localPlayerController.gameplayCamera;
                        customPassVolume.injectionPoint = (CustomPassInjectionPoint)1;
                        customPassVolume.isGlobal = true;

                        frozenPass = new FrozenCustomPass();
                        customPassVolume.customPasses.Add(frozenPass);
                    }
                }
                return customPassVolume;
            }
        }

        public static void SetupCustomPassForEnemy(EnemyAI enemy)
        {
            if (frozenEnemies.ContainsKey(enemy)) return;

            LayerMask frozenLayer = 524288;
            List<Renderer> enemyRenderers = enemy.GetComponentsInChildren<Renderer>().Where(r => (frozenLayer & (1 << r.gameObject.layer)) != 0).ToList();
            if (enemyRenderers.Any(r => r.name.Contains("LOD")))
                enemyRenderers.RemoveAll(r => !r.name.Contains("LOD"));

            if (enemyRenderers == null || enemyRenderers.Count == 0)
            {
                SnowPlaygrounds.mls.LogError($"No renderer could be found on {enemy.enemyType.enemyName}.");
                return;
            }

            if (CustomPassVolume == null)
            {
                SnowPlaygrounds.mls.LogError("CustomPassVolume is not assigned.");
                return;
            }

            frozenPass = CustomPassVolume.customPasses.Find(pass => pass is FrozenCustomPass) as FrozenCustomPass;
            if (frozenPass == null)
            {
                SnowPlaygrounds.mls.LogError("FrozenCustomPass could not be found in CustomPassVolume.");
                return;
            }

            frozenEnemies[enemy] = enemyRenderers;
            frozenPass.AddTargetRenderers(enemyRenderers.ToArray(), SnowPlaygrounds.frozenShader);
        }

        public static void RemoveAura(EnemyAI enemy)
        {
            if (frozenEnemies.ContainsKey(enemy))
            {
                frozenPass.RemoveTargetRenderers(frozenEnemies[enemy]);
                frozenEnemies.Remove(enemy);
            }
        }
    }
}
