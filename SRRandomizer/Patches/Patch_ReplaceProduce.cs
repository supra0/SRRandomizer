using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;

namespace SRRandomizer.Patches
{
    [HarmonyPatch(typeof(GameModel))]
    [HarmonyPatch("RegisterResourceSpawner")]
    class Patch_ReplaceProduce
    {
        static void Prefix(ref SpawnResourceModel.Participant part)
        {
            if(SRRandomizer.produceRandomizer.randomMode == RandomMode.DISABLED)
            {
                return;
            }

            if(!(part is SpawnResource))
            {
                return;
            }

            SpawnResource sr = (SpawnResource)part;

            // Prevent Garden replacement
            // Wild produce has a CellDirector component in its parents, Gardens don't. Use this fact to differentiate between wild crops and gardens
            if (!SRRandomizer.produceRandomizer.randomizeGardens && sr.GetComponentInParent<CellDirector>() == null)
            {
                return;
            }

            for(int i = 0; i < sr.ObjectsToSpawn.Length; i++)
            {
                sr.ObjectsToSpawn[i] = SRRandomizer.produceRandomizer.GetRandomizedProduce(sr.ObjectsToSpawn[i]);
            }
        }
    }
}
