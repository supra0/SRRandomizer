using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace SRRandomizer.Patches
{
    
    [HarmonyPatch(typeof(CellDirector))]
    [HarmonyPatch("Register")]
    [HarmonyPatch(new Type[] { typeof(DirectedSlimeSpawner) })]
    static class Patch_ReplaceSlimes
    {
        static void Prefix(ref DirectedSlimeSpawner spawner)
        {
            if(SRRandomizer.slimeRandomMode != RandomMode.DISABLED)
            {
                foreach (DirectedSlimeSpawner.SpawnConstraint constraint in spawner.constraints)
                {
                    foreach (SlimeSet.Member slimeSetMember in constraint.slimeset.members)
                    {
                        //SRRandomizer.Log(slimeSetMember.prefab.ToString());
                        slimeSetMember.prefab = SRRandomizer.GetRandomizedSlime(slimeSetMember.prefab);
                    }
                }
            }
        }
    }

    /*
         * attempt at transpiler implementation to enable slime prefab replacement while the game is running.
         * doesn't work for some reason.
         * may be added as an option later since it may impact performance. gives more flexibility though
         * 
        [HarmonyPatch(typeof(DirectedActorSpawner))]
        [HarmonyPatch("Spawn")]
        static class Patch_ReplaceSlimes
        {
            static MethodInfo m_getRandomizedSlime = SymbolExtensions.GetMethodInfo(() => SRRandomizer.GetRandomizedSlime(null));

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instruction in instructions)
                {
                    //find the call to MaybeReplacePrefab()
                    if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Equals("GameObject MaybeReplacePrefab()"))
                    {
                        yield return instruction; //keep the call in place

                        //inject a call to our method
                        yield return new CodeInstruction(OpCodes.Call, m_getRandomizedSlime);
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }
        }
        */
}
