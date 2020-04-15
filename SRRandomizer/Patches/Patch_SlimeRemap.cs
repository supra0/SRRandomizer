using System;
using UnityEngine;
using HarmonyLib;

namespace SRRandomizer.Patches
{
    [HarmonyPatch(typeof(CellDirector))]
    [HarmonyPatch("Register")]
    [HarmonyPatch(new Type[] { typeof(DirectedSlimeSpawner) })]
    static class Patch_SlimeRemap
    {
        static void Prefix(ref DirectedSlimeSpawner spawner)
        {
            foreach(DirectedSlimeSpawner.SpawnConstraint constraint in spawner.constraints)
            {
                foreach(SlimeSet.Member slimeSetMember in constraint.slimeset.members)//hooray for nested foreach loops
                {
                    SRRandomizer.Log(slimeSetMember.prefab.ToString());
                    slimeSetMember.prefab = SRSingleton<GameContext>.Instance.LookupDirector.GetPrefab(SRRandomizer.Randomize(Identifiable.Id.PINK_SLIME)); //temp
                }
            }
        }
    }

}
