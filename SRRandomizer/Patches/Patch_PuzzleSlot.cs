using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace SRRandomizer.Patches
{
    /*
     * Modifies the catchId of PuzzleSlots (plort statues) based on randomiztion settings
     * catchId = the Identifiable.Id the statue needs in order to be filled
     */

    [HarmonyPatch(typeof(PuzzleSlot))]
    [HarmonyPatch("Awake")]
    class Patch_PuzzleSlot
    {
        static void Postfix(PuzzleSlot __instance)
        {
            /*
            foreach(Component c in __instance.GetComponentsInParent<Component>())
            {
                SRRandomizer.Log(c.ToString());
            }
            SRRandomizer.Log(""); // spacer
            */

            //SRRandomizer.Log(__instance.gameObject.name + "; fillOnAwake=" + __instance.fillOnAwake.ToString());

            // Replace the catchId and add text
            __instance.catchId = SRRandomizer.statueRandomizer.GetRandomizedSlotCatch(__instance);
        }
    }
}
