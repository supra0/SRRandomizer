using System;
using UnityEngine;
using HarmonyLib;

namespace SRRandomizer.Patches
{
    //In the future, move this to DirectedActorSpawner.Spawn with transpiler for more flexibility
    [HarmonyPatch(typeof(CellDirector))]
    [HarmonyPatch("Register")]
    [HarmonyPatch(new Type[] { typeof(DirectedSlimeSpawner) })]
    static class Patch_ReplaceSlimes
    {
        static void Prefix(ref DirectedSlimeSpawner spawner)
        {
            foreach(DirectedSlimeSpawner.SpawnConstraint constraint in spawner.constraints)
            {
                foreach(SlimeSet.Member slimeSetMember in constraint.slimeset.members)
                {
                    //SRRandomizer.Log(slimeSetMember.prefab.ToString());
                    slimeSetMember.prefab = SRRandomizer.GetRandomizedSlime(slimeSetMember.prefab);
                }
            }
        }
    }

}
