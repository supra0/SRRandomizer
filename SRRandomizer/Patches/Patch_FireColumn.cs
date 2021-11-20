using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace SRRandomizer.Patches
{
    [HarmonyPatch(typeof(FireColumn))]
    [HarmonyPatch("Awake")]
    static class Patch_FireColumn
    {
        // This patch replaces fire slimes in fire columns with the appropriate replacement slime type
        static void Postfix(FireColumn __instance)
        {
            if(SRRandomizer.slimeRandomizer.randomMode != RandomMode.DISABLED)
            {
                // 3rd element of fireballs list is always fire slime prefab
                __instance.fireballs[2].prefab = SRRandomizer.slimeRandomizer.GetRandomizedSlime(__instance.fireballs[2].prefab);
            }
        }
    }
}
