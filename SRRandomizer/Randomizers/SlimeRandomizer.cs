using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRRandomizer
{
    class SlimeRandomizer
    {
        #region Option Variables
        public RandomMode randomMode = RandomMode.DISABLED;

        // Rare slimes
        public bool allowLuckySlimes = false;
        public bool allowGoldSlimes = false;

        // Expansion slimes
        public bool allowSaberSlimes = false;
        public bool allowQuicksilverSlimes = false;
        public bool allowGlitchSlimes = false;

        // Tarr
        public bool allowTarr = false;
        public bool allowGlitchTarr = false;
        #endregion

        #region Randomization Variables
        private static Dictionary<string, Identifiable.Id> slimePrefabToId; //mapping of slime prefabs to Ids
        private List<Identifiable.Id> allowedSlimes; //list of slimes that are allowed to be replaced by the randomizer
        internal Dictionary<Identifiable.Id, Identifiable.Id> slimeMap; //mapping of each slime type to another (used for replacement by randomizer)
        #endregion

        internal void RandomizeSlimeSpawns(System.Random rand)
        {
            // Initialize slimePrefabToId
            // TODO: make this static, i.e. don't do it for every new instance
            LookupDirector lookupDirector = SRSingleton<GameContext>.Instance.LookupDirector;

            var slimes = Identifiable.SLIME_CLASS;
            slimePrefabToId = new Dictionary<String, Identifiable.Id>();
            foreach (Identifiable.Id id in slimes)
            {
                slimePrefabToId.Add(lookupDirector.GetPrefab(id).ToString(), id);
            }

            //Build list of allowed slimes
            allowedSlimes = Identifiable.SLIME_CLASS.ToList(); //get list of all base slimes

            //Remove slime types not allowed by options
            if (!allowLuckySlimes) allowedSlimes.Remove(Identifiable.Id.LUCKY_SLIME);
            if (!allowGoldSlimes) allowedSlimes.Remove(Identifiable.Id.GOLD_SLIME);
            if (!allowSaberSlimes) allowedSlimes.Remove(Identifiable.Id.SABER_SLIME);
            if (!allowQuicksilverSlimes) allowedSlimes.Remove(Identifiable.Id.QUICKSILVER_SLIME);
            if (!allowGlitchSlimes) allowedSlimes.Remove(Identifiable.Id.GLITCH_SLIME);
            if (!allowTarr) allowedSlimes.Remove(Identifiable.Id.TARR_SLIME);
            if (!allowGlitchTarr) allowedSlimes.Remove(Identifiable.Id.GLITCH_TARR_SLIME);

            if (randomMode == RandomMode.DISABLED || randomMode == RandomMode.CHAOTIC)
            {
                //do nothing; this is handled in GetRandomizedSlime()
                return;
            }

            slimeMap = new Dictionary<Identifiable.Id, Identifiable.Id>();

            //Build Dictionary mapping if in a mapping randomization mode
            if (randomMode == RandomMode.MAPPED_NODUPES)
            {
                SRRandomizer.CreateRandomMapping(allowedSlimes, slimeMap, rand, false);
            }
            else if (randomMode == RandomMode.MAPPED_DUPES)
            {
                SRRandomizer.CreateRandomMapping(allowedSlimes, slimeMap, rand, true);
            }
        }

        // Used by Patch_ReplaceSlimes
        public GameObject GetRandomizedSlime(GameObject prefab) //currently no largo support, in future this will be added here
        {
            if (slimePrefabToId != null && !slimePrefabToId.ContainsKey(prefab.ToString())) //this slime is a largo. for now just return it
            {
                return prefab;
            }

            Identifiable.Id id = slimePrefabToId[prefab.ToString()];

            if (!allowedSlimes.Contains(id)) //this means the slime was excluded via an option, return back the original (don't do anything)
            {
                return prefab;
            }

            // TODO: make this static
            LookupDirector lookupDirector = SRSingleton<GameContext>.Instance.LookupDirector;

            switch (randomMode)
            {
                case RandomMode.DISABLED: //randomization deactivated
                    return prefab;
                case RandomMode.CHAOTIC: //no mapping
                    return lookupDirector.GetPrefab(allowedSlimes.ElementAt(SRRandomizer.RuntimeRandNext(allowedSlimes.Count)));
                case RandomMode.MAPPED_NODUPES: //mapping
                    return lookupDirector.GetPrefab(slimeMap[slimePrefabToId[prefab.ToString()]]);
                case RandomMode.MAPPED_DUPES: //also mapping
                    return lookupDirector.GetPrefab(slimeMap[slimePrefabToId[prefab.ToString()]]);
                default: //do nothing
                    SRRandomizer.Log("Warning: Invalid randomization mode: " + randomMode);
                    return prefab;
            }
        }
    }
}
