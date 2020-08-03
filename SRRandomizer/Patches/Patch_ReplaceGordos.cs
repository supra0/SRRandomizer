using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
using UnityEngine;

namespace SRRandomizer.Patches
{

    [HarmonyPatch(typeof(GameModel))]
    [HarmonyPatch("RegisterGordo")]
    class Patch_ReplaceGordos
    {
        static void Prefix(ref GameObject gordoObj)
        {
            if(!SRRandomizer.gordo_randomizeFoodRequirement)
            {
                return;
            }

            GordoEat gordoEat = gordoObj.GetComponent<GordoEat>();
            gordoEat.targetCount = SRRandomizer.GetRandomizedGordoFoodTarget();

            //SlimeDefinitions slimeDefinitions = SRSingleton<GameContext>.Instance.SlimeDefinitions;
            //gordoEat.slimeDefinition = slimeDefinitions.GetSlimeByIdentifiableId(Identifiable.Id.TABBY_SLIME);
        }
    }
}
