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
    [HarmonyPatch("RegisterGordo")]
    class Patch_ReplaceGordos
    {
        static GameObject gordoReplacementPrefab = null;

        static void Prefix(ref String gordoId, ref GameObject gordoObj)
        {
            SRRandomizer.Log(gordoId + ", " + gordoObj.ToString());
            if(gordoReplacementPrefab == null) gordoReplacementPrefab = gordoObj;
            gordoObj = gordoReplacementPrefab;
        }
    }
}
