using UnityEngine;
using UModFramework.API;
using System;
using System.Linq;
using System.Collections.Generic;
using MonomiPark.SlimeRancher.DataModel;

namespace SRRandomizer
{
    [UMFHarmony(1)] //Set this to the number of harmony patches in your mod.
    [UMFScript]
    class SRRandomizer : MonoBehaviour
    {
        private static LookupDirector lookupDirector;

        /* Randomization */
        private static int randomSeed;
        private static System.Random runTimeRand; //System.Random object used during runtime after random tables are setup. used for randomization mode 1 ("Chaotic")

        /* GUI */
        public static int windowSizeX = 600;
        public static int windowSizeY = 450;
        public static Rect windowRect = new Rect((Screen.width/2) - (windowSizeX/2), (Screen.height/2) - (windowSizeY/2), windowSizeX, windowSizeY);

        private int toolbarTab;
        private static string[] toolbarTabTexts = { "Slimes", "Gordos", "Crops", "Market" };
        private static string[] modeSelectionTexts = { "Disabled", "Chaotic", "Mapped", "Mapped (Dupes)" };

        private static string seedInput = "Random Seed";
       
        /* Slime Options */
        private static int slimeRandomizationMode; //randomnization mode for slimes
        private bool allowLuckySlimes;
        private bool allowGoldSlimes;
        private bool allowExpansionSlimes;
        private bool allowTarr;
        private bool allowGlitchTarr;
        private bool randomizeFavoriteFood;
        private bool randomizeDietCategory;

        private static List<Identifiable.Id> allowedSlimes;
        private static Dictionary<String, Identifiable.Id> slimePrefabToId; //mapping of prefabs to Ids
        private static Dictionary<Identifiable.Id, Identifiable.Id> slimeMap; //1:1 mapping of slime types

        /* Gordo Options */

        /* Crop Options */


        internal static void Log(string text, bool clean = false)
        {
            using (UMFLog log = new UMFLog()) log.Log(text, clean);
        }

        [UMFConfig]
        public static void LoadConfig()
        {
            SRRandomizerConfig.Load();
        }

        void Awake()
        {
            Log("SRRandomizer v" + UMFMod.GetModVersion().ToString(), true);
            UMFGUI.RegisterPauseHandler(Pause);
            SRRandomizerConfig.Load();

            randomSeed = new System.Random().Next(); //get an initial seed

            //Register Commands
            UMFGUI.RegisterCommand("srr_listslimes", "srr_listslimes", new string[] { "listslimes" }, 0, "Test command, prints all slime types.", CommandListSlimes);
            UMFGUI.RegisterCommand("srr_printslimemap", "srr_printslimemap", new string[] { "printslimemap" }, 0, "Prints the current random slime map.", CommandPrintSlimeMap);
            UMFGUI.RegisterCommand("srr_loadprefabs", "srr_loadprefabs", new string[] { "loadprefabs" }, 0, "Temp command, load prefab table", CommandLoadPrefabs);
        }

        void OnGUI()
        {
            if(Levels.isMainMenu() && !UMFGUI.IsConsoleOpen && !UMFGUI.IsMenuOpen)
            {
                windowRect = GUILayout.Window(32123, windowRect, MenuWindow, "Randomizer Options");
            }
            
        }

        void MenuWindow(int windowId)
        {
            //Toolbar tabs
            toolbarTab = GUILayout.Toolbar(toolbarTab, toolbarTabTexts);

            GUILayout.BeginVertical();
            slimeRandomizationMode = GUILayout.SelectionGrid(slimeRandomizationMode, modeSelectionTexts, 4);
            allowLuckySlimes = GUILayout.Toggle(allowLuckySlimes, "Allow Lucky Slimes");
            allowGoldSlimes = GUILayout.Toggle(allowGoldSlimes, "Allow Gold Slimes");
            allowExpansionSlimes = GUILayout.Toggle(allowExpansionSlimes, "Allow Saber, Quicksilver, and Glitch Slimes");
            allowTarr = GUILayout.Toggle(allowTarr, "Allow Tarr");
            allowGlitchTarr = GUILayout.Toggle(allowGlitchTarr, "Allow Glitch Tarr");
            randomizeFavoriteFood = GUILayout.Toggle(randomizeFavoriteFood, "Randomize Slime favorite foods");
            randomizeDietCategory = GUILayout.Toggle(randomizeDietCategory, "Randomize the category of food Slimes favor");
            GUILayout.EndVertical();

            //utility bar at bottom
            GUILayout.BeginHorizontal();

            seedInput = GUILayout.TextField(seedInput, 11);
            int num;
            if(int.TryParse(seedInput, out num))
            {
                randomSeed = num;
            }

            if (GUILayout.Button("Apply"))
            {
                ApplyRandomization(randomSeed);
            }

            GUILayout.EndHorizontal();
        }

        private void CommandLoadPrefabs()
        {
            var slimes = Identifiable.SLIME_CLASS;
            slimePrefabToId = new Dictionary<String, Identifiable.Id>();
            lookupDirector = SRSingleton<GameContext>.Instance.LookupDirector;

            foreach (Identifiable.Id id in slimes)
            {
                slimePrefabToId.Add(lookupDirector.GetPrefab(id).ToString(), id);
            }
        }

        public static void Pause(bool pause)
        {
            TimeDirector timeDirector = null;
            try
            {
                timeDirector = SRSingleton<SceneContext>.Instance.TimeDirector;
            }
            catch { }
            if (!timeDirector) return;
            if (pause)
            {
                if (!timeDirector.HasPauser()) timeDirector.Pause();
            }
            else timeDirector.Unpause();
        }

        void Update()
        {
            //gameModel = SRSingleton<SceneContext>.Instance.GameModel;
            //spawnerTriggers = gameModel.AllSpawnerTriggers().ToList();
        }

        private void CommandListSlimes()
        {
            foreach (Identifiable.Id id in Identifiable.SLIME_CLASS)
            {
                Log(id.ToString());
            }
        }

        private void CommandPrintSlimeMap()
        {
            if(slimeMap == null)
            {
                Log("Please run srr_newslimemap first.");
                return;
            }
            foreach(KeyValuePair<Identifiable.Id, Identifiable.Id> item in slimeMap)
            {
                Log("Original: " + item.Key.ToString() + ", Replacement: " + item.Value.ToString());
            }
        }

        //Takes a seed and sets up everything for randomization
        private void ApplyRandomization(int seed)
        {
            randomSeed = seed;
            Log("Randomizing using seed " + randomSeed);

            /* For consistent randomization for every given seed, we want to create a new Random object for each element that's being randomized separately */
            /* These methods simply *set up* randomization tables. The replacement is done in separate methods that are called from Harmony patches, and they utilize the setup that these methods provide */
            RandomizeSlimeSpawns(randomSeed);
            RandomizeSlimeDiets(randomSeed);

            //Set up for randomization that needs to be done at runtime
            runTimeRand = new System.Random(seed);
        }

        private void RandomizeSlimeSpawns(int seed)
        {
            if (slimeRandomizationMode == 0)
            {
                return; //do nothing
            }

            System.Random rand = new System.Random(seed);

            //Build list of allowed slimes
            allowedSlimes = Identifiable.SLIME_CLASS.ToList();
            if (!allowLuckySlimes)
            {
                allowedSlimes.Remove(Identifiable.Id.LUCKY_SLIME);
            }
            if (!allowGoldSlimes)
            {
                allowedSlimes.Remove(Identifiable.Id.GOLD_SLIME);
            }
            if (!allowExpansionSlimes)
            {
                allowedSlimes.Remove(Identifiable.Id.SABER_SLIME);
                allowedSlimes.Remove(Identifiable.Id.QUICKSILVER_SLIME);
                allowedSlimes.Remove(Identifiable.Id.GLITCH_SLIME);
            }
            if (!allowTarr)
            {
                allowedSlimes.Remove(Identifiable.Id.TARR_SLIME);
            }
            if (!allowGlitchTarr)
            {
                allowedSlimes.Remove(Identifiable.Id.GLITCH_TARR_SLIME);
            }

            if(slimeRandomizationMode == 1)
            {
                //do nothing; this is handled in GetRandomizedSlime()
                return;
            }

            CommandLoadPrefabs(); //temp
            slimeMap = new Dictionary<Identifiable.Id, Identifiable.Id>();
          
            //Build Dictionary mapping if in a mapping randomization mode
            if (slimeRandomizationMode == 2)
            {
                CreateRandomMapping(allowedSlimes, slimeMap, rand, false);
            }
            else if(slimeRandomizationMode == 3)
            {
                CreateRandomMapping(allowedSlimes, slimeMap, rand, true);
            }
        }

        private void RandomizeSlimeDiets(int seed)
        {
            // todo
        }

        // Creates a random mapping of the elements in idList and puts it into mapDict
        private void CreateRandomMapping(List<Identifiable.Id> idList, Dictionary<Identifiable.Id, Identifiable.Id> mapDict, System.Random rand, bool allowDupes = false)
        {
            if (!allowDupes)
            {
                List<Identifiable.Id> idList2 = new List<Identifiable.Id>(idList);

                //Fisher-Yates shuffle on idList2
                for(int i = idList2.Count-1; i > 0; i--)
                {
                    int j = rand.Next(0,i);
                    var tmp = idList2[i];
                    idList2[i] = idList2[j];
                    idList2[j] = tmp;
                }

                //In order, map elements from list 1 (original slime type) to list 2 (replacement slime type)
                for(int i = 0; i < idList.Count; i++)
                {
                    mapDict.Add(idList[i], idList2[i]);
                }
            }
            else
            {
                foreach(Identifiable.Id id in idList)
                {
                    mapDict.Add(id, idList[rand.Next(idList.Count())]);
                }
            }
        }

        public static GameObject GetRandomizedSlime(GameObject prefab) //should note that this was hacked together if that wasn't obvious
        {
            Identifiable.Id slimeId = slimePrefabToId[prefab.ToString()];
            if(!slimeMap.ContainsKey(slimeId)) //this means the slime was excluded via an option, return back the original (don't do anything)
            {
                return prefab;
            }

            switch(slimeRandomizationMode)
            {
                case 0: //randomization deactivated
                    return prefab;
                case 1: //no mapping
                    return lookupDirector.GetPrefab(allowedSlimes.ElementAt(runTimeRand.Next(allowedSlimes.Count)));
                case 2: //mapping
                    return lookupDirector.GetPrefab(slimeMap[slimePrefabToId[prefab.ToString()]]);
                case 3: //also mapping
                    return lookupDirector.GetPrefab(slimeMap[slimePrefabToId[prefab.ToString()]]);
                default: //do nothing
                    Log("Warning: Invalid randomization mode: " + slimeRandomizationMode);
                    return prefab;
            }
        }
    }

    public enum RandomMode
    {
        DISABLED,
        CHAOTIC,
        MAPPED_DUPES,
        MAPPED_NODUPES
    }
}