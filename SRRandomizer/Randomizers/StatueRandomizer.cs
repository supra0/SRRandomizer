using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRRandomizer
{
    class StatueRandomizer
    {

        #region Option Variables
        // Locations
        public bool randomizeRuins;
        public bool randomizeDesert;

        // Allowed plorts
        public bool allowGoldPlort;
        public bool allowSaberPlort;
        //public bool allowQuicksilverPlort;

        // Other
        public bool desertSinglePlort = true; // if true, desert teleporter plorts will all be the same. if false, they will all be random
        public bool desertPortalDifferent = false; // if true, desert teleporter will not require any of the same plorts as the ancient gate
        public bool ensureCompletable; // ensures that the statues will be fillable with the available slimes
        #endregion
        #region Statue Metadata
        public static readonly List<string> ancientGateSlots = new List<string>
        {
            "PlortSwitch_rad",
            "PlortSwitch_honey",
            "PlortSwitch_pink",
            "PlortSwitch_boom",
            "PlortSwitch_phos",
            "PlortSwitch_rock",
            "PlortSwitch_tabby"
        };

        public static readonly List<string> desertPortalSlots = new List<string>
        {
            "PlortSwitch_quantum",
            "PlortSwitch_quantum (1)",
            "PlortSwitch_quantum (2)",
            "PlortSwitch_quantum (3)",
            "PlortSwitch_quantum (4)"
        };
        #endregion
        #region Vanilla Statue Data
        // List of slimes normally available before the ruins gate
        public static readonly List<Identifiable.Id> ruinsAvailableSlimes = new List<Identifiable.Id>
        {
            Identifiable.Id.PINK_SLIME,
            Identifiable.Id.ROCK_SLIME,
            Identifiable.Id.PHOSPHOR_SLIME,
            Identifiable.Id.TABBY_SLIME,
            Identifiable.Id.RAD_SLIME,
            Identifiable.Id.BOOM_SLIME,
            Identifiable.Id.HONEY_SLIME,
            Identifiable.Id.PUDDLE_SLIME,
            Identifiable.Id.CRYSTAL_SLIME,
            Identifiable.Id.HUNTER_SLIME,
            Identifiable.Id.SABER_SLIME,
            //Identifiable.Id.QUICKSILVER_SLIME,
            Identifiable.Id.GOLD_SLIME
        };

        // List of slimes normally available before the desert portal
        public static readonly List<Identifiable.Id> desertAvailableSlimes = new List<Identifiable.Id>
        {
            Identifiable.Id.PINK_SLIME,
            Identifiable.Id.ROCK_SLIME,
            Identifiable.Id.PHOSPHOR_SLIME,
            Identifiable.Id.TABBY_SLIME,
            Identifiable.Id.RAD_SLIME,
            Identifiable.Id.BOOM_SLIME,
            Identifiable.Id.HONEY_SLIME,
            Identifiable.Id.PUDDLE_SLIME,
            Identifiable.Id.CRYSTAL_SLIME,
            Identifiable.Id.HUNTER_SLIME,
            Identifiable.Id.QUANTUM_SLIME,
            Identifiable.Id.SABER_SLIME,
            //Identifiable.Id.QUICKSILVER_SLIME,
            Identifiable.Id.GOLD_SLIME
        };

        // List of plorts normally available before the ruins gate
        public static readonly List<Identifiable.Id> ruinsAvailablePlorts = new List<Identifiable.Id>
        {
            Identifiable.Id.PINK_PLORT,
            Identifiable.Id.ROCK_PLORT,
            Identifiable.Id.PHOSPHOR_PLORT,
            Identifiable.Id.TABBY_PLORT,
            Identifiable.Id.RAD_PLORT,
            Identifiable.Id.BOOM_PLORT,
            Identifiable.Id.HONEY_PLORT,
            Identifiable.Id.PUDDLE_PLORT,
            Identifiable.Id.CRYSTAL_PLORT,
            Identifiable.Id.HUNTER_PLORT,
            Identifiable.Id.SABER_PLORT,
            //Identifiable.Id.QUICKSILVER_PLORT,
            Identifiable.Id.GOLD_PLORT
        };

        // List of plorts normally available before the desert portal
        public static readonly List<Identifiable.Id> desertAvailablePlorts = new List<Identifiable.Id>
        {
            Identifiable.Id.PINK_PLORT,
            Identifiable.Id.ROCK_PLORT,
            Identifiable.Id.PHOSPHOR_PLORT,
            Identifiable.Id.TABBY_PLORT,
            Identifiable.Id.RAD_PLORT,
            Identifiable.Id.BOOM_PLORT,
            Identifiable.Id.HONEY_PLORT,
            Identifiable.Id.PUDDLE_PLORT,
            Identifiable.Id.CRYSTAL_PLORT,
            Identifiable.Id.HUNTER_PLORT,
            Identifiable.Id.QUANTUM_PLORT,
            Identifiable.Id.SABER_PLORT,
            //Identifiable.Id.QUICKSILVER_PLORT,
            Identifiable.Id.GOLD_PLORT
        };

        public static readonly List<Identifiable.Id> vanillaRuinsPlorts = new List<Identifiable.Id>
        {
            Identifiable.Id.ROCK_PLORT,
            Identifiable.Id.TABBY_PLORT,
            Identifiable.Id.HONEY_PLORT,
            Identifiable.Id.BOOM_PLORT,
            Identifiable.Id.RAD_PLORT,
            Identifiable.Id.PHOSPHOR_PLORT
        };
        #endregion
        #region Randomization Variables
        private List<Identifiable.Id> newRuinsPlorts;
        private Dictionary<Identifiable.Id, Identifiable.Id> ruinsPlortMap;

        private Identifiable.Id newDesertPlort;
        #endregion

        #region Randomization Functions
        internal void RandomizeStatues(System.Random rand)
        {
            // Abort if no randomization is enabled
            if(!randomizeRuins && !randomizeDesert)
            {
                return;
            }

            if(randomizeRuins)
            {
                RandomizeRuinsStatues(rand);
            }
            if(randomizeDesert)
            {
                RandomizeDesertStatues(rand);
            }

            SRRandomizer.Log("Randomize statues");
        }

        private void RandomizeRuinsStatues(System.Random rand)
        {
            List<Identifiable.Id> availablePlorts;

            if(ensureCompletable && SRRandomizer.slimeRandomizer.randomMode != RandomMode.CHAOTIC)
            {
                if(SRRandomizer.slimeRandomizer.slimeMap == null)
                {
                    availablePlorts = new List<Identifiable.Id>(ruinsAvailablePlorts);
                }
                else
                {
                    availablePlorts = GetPlortsFromSlimeMap(ruinsAvailableSlimes);
                }
            }
            else
            {
                availablePlorts = Identifiable.PLORT_CLASS.ToList();
            }

            // Build list of restricted plorts we're not allowed to use
            List<Identifiable.Id> restrictedPlorts = new List<Identifiable.Id>
            {
                Identifiable.Id.QUICKSILVER_PLORT
            };
            if (!allowGoldPlort) restrictedPlorts.Add(Identifiable.Id.GOLD_PLORT);
            if (!allowSaberPlort) restrictedPlorts.Add(Identifiable.Id.SABER_PLORT);

            newRuinsPlorts = BuildPlortList(availablePlorts, restrictedPlorts, 6, rand);
            ruinsPlortMap = BuildPlortMap(vanillaRuinsPlorts, newRuinsPlorts);
        }

        private void RandomizeDesertStatues(System.Random rand)
        {
            /*
             * Largely identical to ruins randomization at the moment (different at the end)
             * Will change significantly in future update(s)
             */
            List<Identifiable.Id> availablePlorts;

            if(ensureCompletable && SRRandomizer.slimeRandomizer.randomMode != RandomMode.CHAOTIC)
            {
                if(SRRandomizer.slimeRandomizer.slimeMap == null)
                {
                    availablePlorts = new List<Identifiable.Id>(desertAvailablePlorts);
                }
                else
                {
                    availablePlorts = GetPlortsFromSlimeMap(desertAvailableSlimes);
                }
            }
            else
            {
                availablePlorts = Identifiable.PLORT_CLASS.ToList();
            }

            // Build list of restricted plorts we're not allowed to use
            List<Identifiable.Id> restrictedPlorts = new List<Identifiable.Id>
            {
                Identifiable.Id.QUICKSILVER_PLORT
            };
            if (!allowGoldPlort) restrictedPlorts.Add(Identifiable.Id.GOLD_PLORT);
            if (!allowSaberPlort) restrictedPlorts.Add(Identifiable.Id.SABER_PLORT);

            foreach(Identifiable.Id id in restrictedPlorts)
            {
                availablePlorts.Remove(id);
            }

            newDesertPlort = SRRandomizer.PickRandom(availablePlorts, rand);
        }

        public Identifiable.Id GetRandomizedSlotCatch(PuzzleSlot slot)
        {
            if(randomizeRuins && IsAncientGateSlot(slot) && ruinsPlortMap != null)
            {
                return ruinsPlortMap[slot.catchId];
            }
            else if(randomizeDesert && IsDesertPortalSlot(slot) && newDesertPlort != Identifiable.Id.NONE)
            {
                return newDesertPlort;
            }

            return slot.catchId;
        }
        #endregion

        #region Utility Functions
        public static bool IsAncientGateSlot(PuzzleSlot slot)
        {
            return ancientGateSlots.Contains(slot.gameObject.name) && !slot.fillOnAwake;
        }

        public static bool IsDesertPortalSlot(PuzzleSlot slot)
        {
            return desertPortalSlots.Contains(slot.gameObject.name) && !slot.fillOnAwake;
        }

        // Runs the given list of slimes through the slime map from SlimeRandomizer.slimeMap and returns a list of the plorts from the mapped slimes
        private static List<Identifiable.Id> GetPlortsFromSlimeMap(List<Identifiable.Id> availableSlimes)
        {
            List<Identifiable.Id> newPlorts = new List<Identifiable.Id>();
            SlimeDefinitions slimeDefinitions = SRSingleton<GameContext>.Instance.SlimeDefinitions;

            foreach(Identifiable.Id id in availableSlimes)
            {
                Identifiable.Id newSlime = Identifiable.Id.NONE;
                if(!SRRandomizer.slimeRandomizer.slimeMap.TryGetValue(id, out newSlime))
                {
                    newSlime = id; // keep the same slime if it's not mapped to anything else
                }

                Identifiable.Id[] produces = slimeDefinitions.GetSlimeByIdentifiableId(newSlime).Diet.Produces;
                if(produces != null && produces.Length > 0)
                {
                    Identifiable.Id newPlort = slimeDefinitions.GetSlimeByIdentifiableId(newSlime).Diet.Produces[0]; // get this slime's plort
                    //SRRandomizer.Log("New plort: " + newPlort.ToString());
                    if (Identifiable.IsPlort(newPlort)) // make sure this is actually a plort
                    {
                        newPlorts.Add(newPlort);
                    }
                }

            }

            return newPlorts;
        }

        // Randomly builds a list of plorts from the given list excluding the specified restricted slimes, with a max length of maxCount
        // If there aren't enough elements in "allowed" to fill newPlorts up to "count", it only fills it up to the length of "allowed"
        // Destroys contents of "allowed"
        private static List<Identifiable.Id> BuildPlortList(List<Identifiable.Id> allowed, List<Identifiable.Id> restricted, int maxCount, System.Random rand)
        {
            foreach(Identifiable.Id id in restricted)
            {
                allowed.Remove(id);
            }

            int numAvailable = Math.Min(allowed.Count, maxCount);
            List<Identifiable.Id> newPlorts = new List<Identifiable.Id>();
            for(int i = 0; i < numAvailable; i++)
            {
                Identifiable.Id id = RandomPluck(allowed, rand);
                if(id != Identifiable.Id.NONE)
                {
                    newPlorts.Add(id);
                }
                else
                {
                    SRRandomizer.Log("Uh ooooh...");
                }
            }

            return newPlorts;
        }

        // Creates a mapping of identifiables from keyList to valueList
        private static Dictionary<Identifiable.Id, Identifiable.Id> BuildPlortMap(List<Identifiable.Id> keyList, List<Identifiable.Id> valueList)
        {
            if (keyList.Count != valueList.Count)
            {
                SRRandomizer.Log("ERROR: keyList and valueList are not the same length.\nkeylist=" + keyList.Count + " | valueList=" + valueList.Count);
                return null;
            }

            Dictionary<Identifiable.Id, Identifiable.Id> map = new Dictionary<Identifiable.Id, Identifiable.Id>(keyList.Count);
            for(int i = 0; i < keyList.Count; i++)
            {
                map.Add(keyList[i], valueList[i]);
            }

            return map;
        }

        private static T Pluck<T>(List<T> list)
        {
            if(list.Count > 0)
            {
                T t = list.ElementAt(0);
                list.RemoveAt(0);
                return t;
            }

            return default;
        }

        // Removes a random item from the given lists and returns it
        // Should really be moved elsewhere. New static class, "RandomUtils"?
        private static T RandomPluck<T>(List<T> list, System.Random rand)
        {
            if(list.Count > 0)
            {
                int index = rand.Next(list.Count);
                T obj = list.ElementAt(index);
                list.RemoveAt(index);
                return obj;
            }

            return default;
        }
        #endregion
    }
}
