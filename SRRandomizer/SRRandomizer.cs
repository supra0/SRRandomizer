using UnityEngine;
using UModFramework.API;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace SRRandomizer
{
    [UMFHarmony(3)] //Set this to the number of harmony patches in your mod.
    [UMFScript]
    class SRRandomizer : MonoBehaviour
    {
        private static LookupDirector lookupDirector;

        /* Randomization */
        private static int randomSeed;
        private static System.Random runTimeRand; //System.Random object used during runtime after random tables are setup. used for randomization mode 1 ("Chaotic")

        /* GUI */
        private static readonly int windowSizeX = 600;
        private static readonly int windowSizeY = 450;
        private static Rect windowRect = new Rect(Screen.width - windowSizeX, (Screen.height / 2) - (windowSizeY / 2), windowSizeX, windowSizeY); //right edge of screen, middle vertically
        private static Vector2 scrollViewPosition = Vector2.zero;

        private static readonly string windowTitle = "Randomizer Options";
        private static readonly string[] modeSelectionTexts = { "Disabled", "Chaotic", "Mapped", "Mapped (Dupes)" };
        private static readonly string[] toolbarTabTexts = { "Slimes", "Gordos", "Produce" };

        private static int currentToolbarTab;
        private static string seedInput;
        private int slimeRandomModeInput;
        private int produceRandomModeInput;
        
        /* Slime Options */
        public static RandomMode slimeRandomMode = RandomMode.DISABLED;
        private bool allowLuckySlimes;
        private bool allowGoldSlimes;
        private bool allowExpansionSlimes;
        private bool allowTarr;
        private bool allowGlitchTarr;
        private bool randomizeFavoriteFood;
        private bool randomizeDietCategory;
        private bool allowMultipleDietCategories;

        private static Dictionary<String, Identifiable.Id> slimePrefabToId; //mapping of slime prefabs to Ids
        private static List<Identifiable.Id> allowedSlimes; //list of slimes that are allowed to be replaced by the randomizer
        private static Dictionary<Identifiable.Id, Identifiable.Id> slimeMap; //mapping of each slime type to another (used for replacement by randomizer)

        private static SlimeDefinitions vanillaSlimeDefinitions;
        private static SlimeDefinitions randomizedSlimeDefinitions;

        /* Gordo Options */
        private bool randomizeFoodReq;
        private int randomFoodMax = 50;

        private static Dictionary<String, Identifiable.Id> gordoPrefabToId;
        private static Dictionary<Identifiable.Id, Identifiable.Id> gordoMap;

        /* Produce Options */
        public static RandomMode produceRandomMode = RandomMode.DISABLED;
        private bool allowGildedGinger;
        private bool allowKookadoba;
        private static bool allowFruitToVeggie;
        private static bool allowVeggieToFruit;

        private static Dictionary<String, Identifiable.Id> producePrefabToId;
        private static List<Identifiable.Id> allowedProduce;
        private static List<Identifiable.Id> allowedFruits;
        private static List<Identifiable.Id> allowedVeggies;
        private static Dictionary<Identifiable.Id, Identifiable.Id> produceMap;


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
            seedInput = randomSeed.ToString();

            //Register Commands
            UMFGUI.RegisterCommand("srr_listslimes", "srr_listslimes", new string[] { "listslimes" }, 0, "Test command, prints all slime types.", CommandListSlimes);
            UMFGUI.RegisterCommand("srr_printslimemap", "srr_printslimemap", new string[] { "printslimemap" }, 0, "Prints the current random slime map.", CommandPrintSlimeMap);
            UMFGUI.RegisterCommand("srr_printproducemap", "srr_printproducemap", new string[] { "printproducemap" }, 0, "Prints the current random produce map.", CommandPrintProduceMap);
            UMFGUI.RegisterCommand("srr_loadprefabs", "srr_loadprefabs", new string[] { "loadprefabs" }, 0, "Temp command, load prefab table", CommandLoadPrefabs);
        }

        void OnGUI()
        {
            if(Levels.isMainMenu() && !UMFGUI.IsConsoleOpen && !UMFGUI.IsMenuOpen)
            {
                GUI.backgroundColor = new Color(0, 0, 0, 0.85f);
                windowRect = GUILayout.Window(32123, windowRect, MenuWindow, windowTitle);
            }
        }

        void MenuWindow(int windowId)
        {
            GUI.skin.button.padding = new RectOffset(8, 8, 8, 8);
            //GUI.skin.button.margin = new RectOffset(0, 0, 0, 0);

            GUI.skin.button.fontSize = 16;
            GUI.skin.textField.fontSize = 16;
            GUI.skin.label.fontSize = 16;
            GUI.skin.toggle.fontSize = 16;
            
            //GUI.DragWindow();

            //Toolbar tabs
            int tempTabValue = GUILayout.Toolbar(currentToolbarTab, toolbarTabTexts);
            if(currentToolbarTab != tempTabValue) //if the tab has changed, reset the scrollbar position
            {
                scrollViewPosition = Vector2.zero;
            }
            currentToolbarTab = tempTabValue;

            GUILayout.Space(20);

            switch(currentToolbarTab)
            {
                case 0: //slime tab
                    GUILayout.BeginHorizontal(GUILayout.Height(windowSizeY * 0.8f));

                    GUILayout.BeginVertical(GUILayout.Width(100));
                    GUILayout.Label("Randomization Mode");
                    slimeRandomModeInput = GUILayout.SelectionGrid(slimeRandomModeInput, modeSelectionTexts, 1);
                    GUILayout.EndVertical();

                    scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition);
                    allowLuckySlimes = GUILayout.Toggle(allowLuckySlimes, "Allow Lucky Slimes in the pool");
                    allowGoldSlimes = GUILayout.Toggle(allowGoldSlimes, "Allow Gold Slimes in the pool");
                    allowExpansionSlimes = GUILayout.Toggle(allowExpansionSlimes, "Allow Saber, Quicksilver, and Glitch Slimes in the pool");
                    allowTarr = GUILayout.Toggle(allowTarr, "Allow Tarr in the pool");
                    allowGlitchTarr = GUILayout.Toggle(allowGlitchTarr, "Allow Glitch Tarr in the pool");
                    //randomizeFavoriteFood = GUILayout.Toggle(randomizeFavoriteFood, "Randomize Slime favorite foods");
                    //randomizeDietCategory = GUILayout.Toggle(randomizeDietCategory, "Randomize the category of food Slimes favor");
                    //allowMultipleDietCategories = GUILayout.Toggle(allowMultipleDietCategories, "Allow Slimes to favor more than one food category");
                    GUILayout.EndScrollView();
                    GUILayout.EndHorizontal();
                    break;
                case 1: //gordos
                    GUILayout.BeginHorizontal(GUILayout.Height(windowSizeY * 0.8f));

                    GUILayout.BeginVertical(GUILayout.Width(100));
                    GUILayout.Label("Only Mapped mode is currently supported for Gordos");
                    GUI.enabled = false;
                    GUILayout.SelectionGrid(2, modeSelectionTexts, 1);
                    GUI.enabled = true;
                    GUILayout.EndVertical();

                    scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition);
                    randomizeFoodReq = GUILayout.Toggle(randomizeFoodReq, "Randomize the amount of food required to pop each Gordo");
                    if (!randomizeFoodReq) GUI.enabled = false;
                    randomFoodMax = (int) GUILayout.HorizontalSlider(randomFoodMax, 1.0f, 100f);
                    GUILayout.Label(randomFoodMax.ToString());
                    GUI.enabled = true;
                    GUILayout.EndScrollView();
                    GUILayout.EndHorizontal();
                    break;
                case 2: //produce
                    GUILayout.BeginHorizontal(GUILayout.Height(windowSizeY * 0.8f));

                    GUILayout.BeginVertical(GUILayout.Width(100));
                    GUILayout.Label("Randomization Mode");
                    produceRandomModeInput = GUILayout.SelectionGrid(produceRandomModeInput, modeSelectionTexts, 1);
                    GUILayout.EndVertical();

                    scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition);
                    allowGildedGinger = GUILayout.Toggle(allowGildedGinger, "Allow Gilded Gingers in the pool");
                    allowKookadoba = GUILayout.Toggle(allowKookadoba, "Allow Kookadobas in the pool");
                    //allowFruitToVeggie = GUILayout.Toggle(allowFruitToVeggie, "Allow Fruits to be replaced by Veggies");
                    //allowVeggieToFruit = GUILayout.Toggle(allowVeggieToFruit, "Allow Veggies to be replaced by Fruits");
                    GUILayout.EndScrollView();
                    GUILayout.EndHorizontal();
                    break;
                default:
                    GUILayout.Label("Something went wrong, you should never see this.");
                    break;
            }
            
            //utility bar at bottom
            GUILayout.BeginHorizontal();

            GUILayout.Label("Random Seed:");

            seedInput = GUILayout.TextField(seedInput, 11, GUILayout.Width(120));
            int num;
            if(int.TryParse(seedInput, out num))
            {
                randomSeed = num;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Apply Settings"))
            {
                ApplyRandomization(randomSeed);
            }

            GUILayout.EndHorizontal();
        }

        private void CommandLoadPrefabs() //load the prefab maps we need for randomization
        {
            lookupDirector = SRSingleton<GameContext>.Instance.LookupDirector;

            var slimes = Identifiable.SLIME_CLASS;
            slimePrefabToId = new Dictionary<String, Identifiable.Id>();
            foreach (Identifiable.Id id in slimes)
            {
                slimePrefabToId.Add(lookupDirector.GetPrefab(id).ToString(), id);
            }

            var produce = Identifiable.FRUIT_CLASS.ToList();
            produce.AddRange(Identifiable.VEGGIE_CLASS);
            producePrefabToId = new Dictionary<string, Identifiable.Id>();
            foreach (Identifiable.Id id in produce)
            {
                producePrefabToId.Add(lookupDirector.GetPrefab(id).ToString(), id);
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
            //not needed right now
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
                Log("Please create a slime map first.");
                return;
            }
            foreach(KeyValuePair<Identifiable.Id, Identifiable.Id> item in slimeMap)
            {
                Log("Original: " + item.Key.ToString() + ", Replacement: " + item.Value.ToString());
            }
        }

        private void CommandPrintProduceMap()
        {
            if (produceMap == null)
            {
                Log("Please create a produce map first.");
                return;
            }
            foreach (KeyValuePair<Identifiable.Id, Identifiable.Id> item in produceMap)
            {
                Log("Original: " + item.Key.ToString() + ", Replacement: " + item.Value.ToString());
            }
        }

        //Takes a seed and sets up everything for randomization
        private void ApplyRandomization(int seed)
        {
            randomSeed = seed;
            Log("Randomizing using seed " + randomSeed);

            CommandLoadPrefabs();

            slimeRandomMode = (RandomMode)slimeRandomModeInput;
            produceRandomMode = (RandomMode)produceRandomModeInput;

            /* For consistent randomization for a given seed, we want to create a new Random object for each element that's being randomized separately */
            /* These methods simply *set up* randomization tables. The replacement is done in separate methods that are called from Harmony patches */
            RandomizeSlimeSpawns(new System.Random(seed));
            //RandomizeSlimeDiets(new System.Random(seed));
            RandomizeProduce(new System.Random(seed));

            //Set up for randomization that needs to be done at runtime
            runTimeRand = new System.Random(seed);
        }

        private void RandomizeSlimeSpawns(System.Random rand)
        {
            //Build list of allowed slimes
            allowedSlimes = Identifiable.SLIME_CLASS.ToList();
            if (!allowLuckySlimes) allowedSlimes.Remove(Identifiable.Id.LUCKY_SLIME);
            if (!allowGoldSlimes) allowedSlimes.Remove(Identifiable.Id.GOLD_SLIME);
            if (!allowExpansionSlimes)
            {
                allowedSlimes.Remove(Identifiable.Id.SABER_SLIME);
                allowedSlimes.Remove(Identifiable.Id.QUICKSILVER_SLIME);
                allowedSlimes.Remove(Identifiable.Id.GLITCH_SLIME);
            }
            if (!allowTarr) allowedSlimes.Remove(Identifiable.Id.TARR_SLIME);
            if (!allowGlitchTarr) allowedSlimes.Remove(Identifiable.Id.GLITCH_TARR_SLIME);
            
            if(slimeRandomMode == RandomMode.DISABLED || slimeRandomMode == RandomMode.CHAOTIC)
            {
                //do nothing; this is handled in GetRandomizedSlime()
                return;
            }

            slimeMap = new Dictionary<Identifiable.Id, Identifiable.Id>();
          
            //Build Dictionary mapping if in a mapping randomization mode
            if (slimeRandomMode == RandomMode.MAPPED_NODUPES)
            {
                CreateRandomMapping(allowedSlimes, slimeMap, rand, false);
            }
            else if(slimeRandomMode == RandomMode.MAPPED_DUPES)
            {
                CreateRandomMapping(allowedSlimes, slimeMap, rand, true);
            }
        }

        private void RandomizeSlimeDiets(System.Random rand)
        {
            if (randomizeDietCategory) //randomize food category before favorite food (keep favorite food within each slime's diet)
            {
                int numCategories = 1;
                if(allowMultipleDietCategories)
                {
                    numCategories = rand.Next();
                }

                vanillaSlimeDefinitions = SRSingleton<GameContext>.Instance.SlimeDefinitions;
                //randomizedSlimeDefinitions = vanillaSlimeDefinitions.Slimes.Clone;

                /*
                tabbyDefinition.Diet.MajorFoodGroups[0] = SlimeEat.FoodGroup.FRUIT;
                slimeDefinitions.RefreshDefinitions();
                lookupDirector.GetPrefab(Identifiable.Id.TABBY_SLIME).GetComponent<SlimeEat>().InitFood();
                slimeDefinitions.RefreshIndexes(); //this doesn't null the "indexes" first and just starts trying to add new entries... may cause issues. solution -> build custom SlimeDefinitions, hotswap it in GameContext. necessary for largos
                */

                IEnumerable<Identifiable.Id> slimesAndLargos = Identifiable.SLIME_CLASS.ToList();
                slimesAndLargos.Concat(Identifiable.LARGO_CLASS.ToList());

                foreach(Identifiable.Id id in Identifiable.SLIME_CLASS)
                {
                    var slimeDef = vanillaSlimeDefinitions.GetSlimeByIdentifiableId(id);
                    bool producesPlorts = slimeDef.Diet.Produces.Count((Identifiable.Id id2) => Identifiable.IsPlort(id2)) > 0; //stolen from private method SlimeDiet.ProducesPlorts
                    bool isLargo = Identifiable.IsLargo(id);
                    if (!isLargo && producesPlorts)
                    {
                        slimeDef.Diet.MajorFoodGroups = new SlimeEat.FoodGroup[] { SlimeEat.FoodGroup.MEAT };
                          
                    }
                }

                vanillaSlimeDefinitions.RefreshDefinitions();

                foreach (Identifiable.Id id in slimesAndLargos)
                {
                    var slimeDef = vanillaSlimeDefinitions.GetSlimeByIdentifiableId(id);
                    if (slimeDef.CanLargofy && !slimeDef.IsLargo)
                    {
                        lookupDirector.GetPrefab(id).GetComponent<SlimeEat>().InitFood();
                    }
                }

                vanillaSlimeDefinitions.RefreshIndexes();
            }
        }

        private void RandomizeProduce(System.Random rand)
        {
            allowedFruits = Identifiable.FRUIT_CLASS.ToList();
            allowedVeggies = Identifiable.VEGGIE_CLASS.ToList();

            if (!allowKookadoba) allowedFruits.Remove(Identifiable.Id.KOOKADOBA_FRUIT);
            if (!allowGildedGinger) allowedVeggies.Remove(Identifiable.Id.GINGER_VEGGIE);

            allowedProduce = new List<Identifiable.Id>();
            allowedProduce.AddRange(allowedFruits);
            allowedProduce.AddRange(allowedVeggies);

            if (produceRandomMode == RandomMode.DISABLED || produceRandomMode == RandomMode.CHAOTIC)
            {
                return;
            }

            produceMap = new Dictionary<Identifiable.Id, Identifiable.Id>();

            if(produceRandomMode == RandomMode.MAPPED_NODUPES)
            {
                CreateRandomMapping(allowedProduce, produceMap, rand, false);
            }
            else if(produceRandomMode == RandomMode.MAPPED_DUPES)
            {
                CreateRandomMapping(allowedProduce, produceMap, rand, true);
            }
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

        public static GameObject GetRandomizedSlime(GameObject prefab) //currently no largo support, in future this will be added here
        {
            if(!slimePrefabToId.ContainsKey(prefab.ToString())) //this slime is a largo. for now just return it
            {
                return prefab;
            }

            Identifiable.Id id = slimePrefabToId[prefab.ToString()];

            if (!allowedSlimes.Contains(id)) //this means the slime was excluded via an option, return back the original (don't do anything)
            {
                return prefab;
            }

            switch(slimeRandomMode)
            {
                case RandomMode.DISABLED: //randomization deactivated
                    return prefab;
                case RandomMode.CHAOTIC: //no mapping
                    return lookupDirector.GetPrefab(allowedSlimes.ElementAt(runTimeRand.Next(allowedSlimes.Count)));
                case RandomMode.MAPPED_NODUPES: //mapping
                    return lookupDirector.GetPrefab(slimeMap[slimePrefabToId[prefab.ToString()]]);
                case RandomMode.MAPPED_DUPES: //also mapping
                    return lookupDirector.GetPrefab(slimeMap[slimePrefabToId[prefab.ToString()]]);
                default: //do nothing
                    Log("Warning: Invalid randomization mode: " + slimeRandomMode);
                    return prefab;
            }
        }

        public static GameObject GetRandomizedProduce(GameObject prefab)
        {
            if (!producePrefabToId.ContainsKey(prefab.ToString())) //this... isn't produce? just return it to avoid causing issues
            {
                Log("Non-produce prefab received in GetRandomizedProduce: " + prefab.ToString());
                return prefab;
            }

            Identifiable.Id id = producePrefabToId[prefab.ToString()];
            if (!allowedProduce.Contains(id)) //this means the produce was excluded via an option, return back the original (don't do anything)
            {
                return prefab;
            }

            switch (produceRandomMode)
            {
                case RandomMode.DISABLED: //randomization deactivated
                    return prefab;
                case RandomMode.CHAOTIC: //no mapping
                    return lookupDirector.GetPrefab(allowedProduce.ElementAt(runTimeRand.Next(allowedProduce.Count)));
                case RandomMode.MAPPED_NODUPES: //mapping
                    return lookupDirector.GetPrefab(produceMap[producePrefabToId[prefab.ToString()]]);
                case RandomMode.MAPPED_DUPES: //also mapping
                    return lookupDirector.GetPrefab(produceMap[producePrefabToId[prefab.ToString()]]);
                default: //do nothing
                    Log("Warning: Invalid randomization mode: " + slimeRandomMode);
                    return prefab;
            }
        }

        public static GameObject GetRandomizedGordo()
        {
            return null;
        }
    }

    public enum RandomMode
    {
        DISABLED,
        CHAOTIC,
        MAPPED_NODUPES,
        MAPPED_DUPES
    }
}