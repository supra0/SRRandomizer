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
        #region Slime Options
        public RandomMode randomMode = RandomMode.DISABLED;

        // Fire slime
        public bool allowFireSlimes = false;

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

        #region Largo Options
        public bool randomizeLargos;
        public bool largoPreserveBase;
        public bool largoRandomFeral;
        #endregion

        #region Randomization Variables
        private static Dictionary<string, Identifiable.Id> slimePrefabToId; //mapping of slime prefabs to Ids
        private List<Identifiable.Id> allowedSlimes; //list of slimes that are allowed to be replaced by the randomizer
        internal Dictionary<Identifiable.Id, Identifiable.Id> slimeMap; //mapping of each slime type to another (used for replacement by randomizer)
        #endregion

        private static readonly Identifiable.Id[] LARGOABLE_SLIMES =
        {
            Identifiable.Id.PINK_SLIME,
            Identifiable.Id.ROCK_SLIME,
            Identifiable.Id.PHOSPHOR_SLIME,
            Identifiable.Id.TABBY_SLIME,
            Identifiable.Id.RAD_SLIME,
            Identifiable.Id.BOOM_SLIME,
            Identifiable.Id.HONEY_SLIME,
            Identifiable.Id.CRYSTAL_SLIME,
            Identifiable.Id.HUNTER_SLIME,
            Identifiable.Id.QUANTUM_SLIME,
            Identifiable.Id.DERVISH_SLIME,
            Identifiable.Id.TANGLE_SLIME,
            Identifiable.Id.MOSAIC_SLIME,
            Identifiable.Id.SABER_SLIME
        };

        internal void RandomizeSlimeSpawns(System.Random rand)
        {
            // Initialize slimePrefabToId
            // TODO: make this static, i.e. don't do it for every new instance
            LookupDirector lookupDirector = SRSingleton<GameContext>.Instance.LookupDirector;

            slimePrefabToId = new Dictionary<String, Identifiable.Id>();
            foreach (Identifiable.Id id in Identifiable.SLIME_CLASS)
            {
                slimePrefabToId.Add(lookupDirector.GetPrefab(id).ToString(), id);
            }

            //Build list of allowed slimes
            allowedSlimes = Identifiable.SLIME_CLASS.ToList(); //get list of all base slimes

            //Remove slime types not allowed by options
            if (!allowFireSlimes) allowedSlimes.Remove(Identifiable.Id.FIRE_SLIME);
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
            if (slimePrefabToId != null && !slimePrefabToId.ContainsKey(prefab.ToString())) //this slime is a largo
            {
                if(randomizeLargos)
                {
                    return GetRandomizedLargo(prefab);
                }
                else
                {
                    return prefab;
                }
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

        private GameObject GetRandomizedLargo(GameObject prefab)
        {
            SRRandomizer.Log(prefab.ToString());
            
            // Get the base slimes from the largo
            SlimeEat eat = prefab.GetComponent<SlimeEat>();
            if(eat == null)
            {
                SRRandomizer.Log("WARNING: SlimeEat not on largo passing through GetRandomizedLargo\n" + prefab.ToString());
                return prefab;
            }

            SlimeDefinition definition = eat.slimeDefinition;
            if(definition.BaseSlimes.Length != 2 || !definition.IsLargo) // Make sure largo is definitely a largo
            {
                SRRandomizer.Log("WARNING: Largo does not appear to be a largo?\n" + prefab.ToString());
                return prefab;
            }

            Identifiable.Id baseSlime1 = definition.BaseSlimes[0].IdentifiableId;
            Identifiable.Id baseSlime2 = definition.BaseSlimes[1].IdentifiableId;

            bool base1_allowed = allowedSlimes.Contains(baseSlime1);
            bool base2_allowed = allowedSlimes.Contains(baseSlime2);

            // If both base slimes are not allowed to be randomized, return prefab
            if (!base1_allowed && !base2_allowed)
            {
                return prefab;
            }

            // Keep track of which one(s) we're going to randomize
            bool randomizeSlime1 = false;
            bool randomizeSlime2 = false;

            Identifiable.Id newBase1 = baseSlime1; // need to pick these now for desirable behavior if both bases are allowed to be randomized
            Identifiable.Id newBase2 = baseSlime2;

            if (base1_allowed) // need to check if they're allowed before picking new slime, otherwise we run into a KeyNotFoundException with MAPPED and MAPPED_NODUPES
            {
                newBase1 = PickLargoBase(baseSlime1);
            }
            if(base2_allowed)
            {
                newBase2 = PickLargoBase(baseSlime2);
            }

            bool base1Largoable = LARGOABLE_SLIMES.Contains(newBase1);
            bool base2Largoable = LARGOABLE_SLIMES.Contains(newBase2);

            // Both bases of randomized slime cannot be largos... return
            if (!base1Largoable && !base2Largoable)
            {
                SRRandomizer.Log("NOTICE: Both largo bases could not be randomized");
                return prefab;
            }

            // If only one base slime is allowed to be randomized, randomize the one that is allowed
            if ((base1_allowed && !base2_allowed) || (!base1_allowed && base2_allowed))
            {
                if(base1_allowed && base1Largoable)
                {
                    randomizeSlime1 = true;
                }
                else if(base2Largoable)
                {
                    randomizeSlime2 = true;
                }
            }

            // Both base slimes allowed to be randomized
            else if(base1_allowed && base2_allowed)
            {
                // Don't preserve one of the bases
                if(!largoPreserveBase)
                {
                    randomizeSlime1 = true && base1Largoable;
                    randomizeSlime2 = true && base2Largoable;
                }
                else // preserve one of the bases
                {
                    // Preserve the randomization of the one that is largoable. If both are largoable, pick between them randomly
                    if(!base1Largoable)
                    {
                        randomizeSlime2 = true;
                    }
                    else if(!base2Largoable)
                    {
                        randomizeSlime1 = true;
                    }
                    else
                    {
                        randomizeSlime1 = SRRandomizer.RuntimeRandNext(2) == 0;
                        randomizeSlime2 = !randomizeSlime1;
                    }
                }
            }

            // Revert to base slimes for bases that shouldn't be randomized
            if (!randomizeSlime1)
            {
                newBase1 = baseSlime1;
            }
            if(!randomizeSlime2)
            {
                newBase2 = baseSlime2;
            }

            if(!randomizeSlime1 && !randomizeSlime2)
            {
                return prefab;
            }

            /*
             * Calling feral.IsFeral() throws a NullReferenceException, I guess because the model in SlimeFeral is null?
             * Very helpfully, the stack trace says the source is my function rather than IsFeral
             * Seems like the spawner is what determines feral-ness, so I should patch it there instead
             * 
            // Store whether the original largo was feral
            SlimeFeral feral = prefab.GetComponent<SlimeFeral>();

            bool wasFeral = false;

            if (feral == null)
            {
                SRRandomizer.Log("WARNING: No SlimeFeral component on largo\n" + prefab.ToString());
                return prefab;
            }


            bool wasFeral = feral.IsFeral();
            */

            // It's possible both bases end up being the same
            // In this case, just return the original
            if(newBase1 == newBase2)
            {
                return prefab;
            }

            // Build the new largo
            List<Identifiable.Id> bases = new List<Identifiable.Id>
            {
                newBase1,
                newBase2
            };
            Identifiable.Id newId = Identifiable.Combine(bases);

            GameObject newLargo = SRSingleton<GameContext>.Instance.LookupDirector.GetPrefab(newId);

            //if(wasFeral) newLargo.GetComponent<SlimeFeral>().SetFeral();

            return newLargo;
        }

        private Identifiable.Id PickLargoBase(Identifiable.Id original)
        {
            SRRandomizer.Log("PickLargoBase: " + original.ToString());

            switch (randomMode)
            {
                case RandomMode.DISABLED: //randomization deactivated
                    return original;
                case RandomMode.CHAOTIC: //no mapping
                    return LARGOABLE_SLIMES.ElementAt(SRRandomizer.RuntimeRandNext(LARGOABLE_SLIMES.Length));
                case RandomMode.MAPPED_NODUPES: //mapping
                    return slimeMap[original];
                case RandomMode.MAPPED_DUPES: //also mapping
                    return slimeMap[original];
                default: //do nothing
                    SRRandomizer.Log("Warning: Invalid randomization mode: " + randomMode);
                    return original;
            }
        }
    }
}
