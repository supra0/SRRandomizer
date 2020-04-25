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
            if(!(part is SpawnResource))
            {
                return;
            }

            SpawnResource sr = (SpawnResource)part;

            for(int i = 0; i < sr.ObjectsToSpawn.Length; i++)
            {
                sr.ObjectsToSpawn[i] = SRRandomizer.GetRandomizedProduce(sr.ObjectsToSpawn[i]);
            }
        }
    }
}
