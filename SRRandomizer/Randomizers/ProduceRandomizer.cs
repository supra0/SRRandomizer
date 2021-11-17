using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRRandomizer
{
    class ProduceRandomizer
    {
        #region Option Variables
        public RandomMode randomMode = RandomMode.DISABLED;

        // Rare or unorthodox foods
        public bool allowGildedGinger;
        public bool allowKookadoba;

        // Fruit <-> Veggie
        // Currently unused, to be used in a future update
        public bool allowFruitToVeggie;
        public bool allowVeggieToFruit;

        // Randomize Gardens
        // If enabled, a garden will produce a random food based on what was shot into it (or in the case of chaotic mode, completely randomly)
        // Only checked in Patch_ReplaceProduce
        public bool randomizeGardens;
        #endregion

        #region Randomization Variables
        private Dictionary<String, Identifiable.Id> producePrefabToId;

        private List<Identifiable.Id> allowedProduce;
        private List<Identifiable.Id> allowedFruits;
        private List<Identifiable.Id> allowedVeggies;

        internal Dictionary<Identifiable.Id, Identifiable.Id> produceMap;
        #endregion

        #region Randomization Functions
        internal void RandomizeProduce(System.Random rand)
        {
            // Initialize producePrefabToId
            // TODO: make this static, i.e. don't do it for every new instance
            LookupDirector lookupDirector = SRSingleton<GameContext>.Instance.LookupDirector;

            var produce = Identifiable.FRUIT_CLASS.ToList();
            produce.AddRange(Identifiable.VEGGIE_CLASS);
            producePrefabToId = new Dictionary<string, Identifiable.Id>();
            foreach (Identifiable.Id id in produce)
            {
                producePrefabToId.Add(lookupDirector.GetPrefab(id).ToString(), id);
            }

            allowedFruits = Identifiable.FRUIT_CLASS.ToList();
            allowedVeggies = Identifiable.VEGGIE_CLASS.ToList();

            if (!allowKookadoba) allowedFruits.Remove(Identifiable.Id.KOOKADOBA_FRUIT);
            if (!allowGildedGinger) allowedVeggies.Remove(Identifiable.Id.GINGER_VEGGIE);

            allowedProduce = new List<Identifiable.Id>();
            allowedProduce.AddRange(allowedFruits);
            allowedProduce.AddRange(allowedVeggies);


            if (randomMode == RandomMode.DISABLED || randomMode == RandomMode.CHAOTIC)
            {
                //do nothing; this is handled in GetRandomizedProduce()
                return;
            }

            produceMap = new Dictionary<Identifiable.Id, Identifiable.Id>();

            if (randomMode == RandomMode.MAPPED_NODUPES)
            {
                SRRandomizer.CreateRandomMapping(allowedProduce, produceMap, rand, false);
            }
            else if (randomMode == RandomMode.MAPPED_DUPES)
            {
                SRRandomizer.CreateRandomMapping(allowedProduce, produceMap, rand, true);
            }
        }

        // Used by Patch_ReplaceProduce
        public GameObject GetRandomizedProduce(GameObject prefab)
        {
            if (producePrefabToId != null && !producePrefabToId.ContainsKey(prefab.ToString())) //this... isn't produce? just return it to avoid causing issues
            {
                //Log("Non-produce prefab received in GetRandomizedProduce: " + prefab.ToString());
                return prefab;
            }

            Identifiable.Id id = producePrefabToId[prefab.ToString()];
            if (!allowedProduce.Contains(id)) //this means the produce was excluded via an option, return back the original (don't do anything)
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
                    return lookupDirector.GetPrefab(allowedProduce.ElementAt(SRRandomizer.RuntimeRandNext(allowedProduce.Count)));
                case RandomMode.MAPPED_NODUPES: //mapping
                    return lookupDirector.GetPrefab(produceMap[producePrefabToId[prefab.ToString()]]);
                case RandomMode.MAPPED_DUPES: //also mapping
                    return lookupDirector.GetPrefab(produceMap[producePrefabToId[prefab.ToString()]]);
                default: //do nothing
                    SRRandomizer.Log("Warning: Invalid randomization mode: " + randomMode);
                    return prefab;
            }
        }
        #endregion
    }
}
