using UnityEngine;
using UModFramework.API;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;

namespace SRRandomizer
{
    [UMFHarmony(3)]
    [UMFScript]
    class SRRandomizer : MonoBehaviour
    {

        #region General Variables

        private static LookupDirector lookupDirector;

        #endregion
        #region  Randomization Variables

        private static int randomSeed;
        private static System.Random runTimeRand; //System.Random object used during runtime after random tables are setup. used for randomization mode 1 ("Chaotic")

        #endregion
        #region GUI Variables

        private static readonly int windowSizeX = 600;
        private static readonly int windowSizeY = 450;
        private static Rect windowRect = new Rect(Screen.width - windowSizeX, (Screen.height / 2) - (windowSizeY / 2), windowSizeX, windowSizeY); //right edge of screen, middle vertically
        private static Vector2 scrollViewPosition = Vector2.zero;

        private static readonly string windowTitle = "Randomizer Options";
        private static readonly GUIContent disabledContent = new GUIContent { image = null, text = "Disabled", tooltip = "Disable randomization for this tab." };
        private static readonly GUIContent chaoticContent = new GUIContent { image = null, text = "Chaotic", tooltip = "Every object is completely randomized." };
        private static readonly GUIContent mappedContent = new GUIContent { image = null, text = "1-to-1 Replacement", tooltip = "Every object type is replaced with another, with every type being represented exactly once." };
        private static readonly GUIContent mappedDupesContent = new GUIContent { image = null, text = "?-to-? Replacement", tooltip = "Every object type is replaced with another, but duplicates are allowed. Not all types may be represented." };
        private static readonly GUIContent[] selectionContents = {disabledContent, chaoticContent, mappedContent, mappedDupesContent};
        private static readonly string[] toolbarTabTexts = { "Slimes", "Gordos", "Diets", "Produce" };

        private static int currentToolbarTab;
        private static string seedInput;
        private int slimeRandomModeInput;
        private int produceRandomModeInput;

        #endregion
        #region Slime Options Variables

        public static RandomMode slime_randomMode = RandomMode.DISABLED;
        private bool slime_allowLuckySlimes;
        private bool slime_allowGoldSlimes;
        private bool slime_allowSaberSlimes;
        private bool slime_allowQuicksilverSlimes;
        private bool slime_allowGlitchSlimes;
        private bool slime_allowTarr;
        private bool slime_allowGlitchTarr;

        private static Dictionary<String, Identifiable.Id> slimePrefabToId; //mapping of slime prefabs to Ids
        private static List<Identifiable.Id> allowedSlimes; //list of slimes that are allowed to be replaced by the randomizer
        private static Dictionary<Identifiable.Id, Identifiable.Id> slimeMap; //mapping of each slime type to another (used for replacement by randomizer)

        #endregion
        #region Gordo Options Variables

        public static bool gordo_randomizeFoodRequirement;
        public static int gordo_minFoodRequirement = 1;
        public static int gordo_maxFoodRequirement = 100;

        #endregion
        #region Diet Options Variables

        public static RandomMode diet_RandomMode = RandomMode.DISABLED;
        public static readonly SlimeEat.FoodGroup[] VALID_FOOD_GROUPS = {SlimeEat.FoodGroup.FRUIT, SlimeEat.FoodGroup.MEAT, SlimeEat.FoodGroup.VEGGIES};
        public bool diet_enabled;
        public bool diet_changePinkSlimeDiet;
        public bool diet_changeGoldSlimeDiet = false;
        public bool diet_changeLuckySlimeDiet = false;
        public bool diet_allowElderFavorite = false;
        public bool diet_allowKookadobaFavorite = false;
        public bool diet_allowGildedGingerFavorite = false;
        public bool diet_allowTofuFavorite = false;

        #endregion
        #region Produce Options Variables

        public static RandomMode produce_randomMode = RandomMode.DISABLED;
        private bool produce_allowGildedGinger;
        private bool produce_allowKookadoba;
        private static bool produce_allowFruitToVeggie;
        private static bool produce_allowVeggieToFruit;

        private static Dictionary<String, Identifiable.Id> producePrefabToId;
        private static List<Identifiable.Id> allowedProduce;
        private static List<Identifiable.Id> allowedFruits;
        private static List<Identifiable.Id> allowedVeggies;
        private static Dictionary<Identifiable.Id, Identifiable.Id> produceMap;

        #endregion

        #region UMF & Startup

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
            //UMFGUI.RegisterCommand("srr_gtest", "srr_gtest", new string[] { "gtest" }, 0, "Temp command, place a pink gordo at 0,0,0", CommandPlaceGordo);
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

        //temp command, places a pink gordo at coords
        /*
        private void CommandPlaceGordo()
        {
            if(GORDO_PREFABS != null)
            {
                GameObject playertmp = SRSingleton<SceneContext>.Instance.Player;
                Log(playertmp.transform.position.ToString());
                GameObject gordotmp = Instantiate<GameObject>(lookupDirector.GetGordo(Identifiable.Id.PINK_GORDO), playertmp.transform.position, Quaternion.identity);
                Component[] clist = gordotmp.GetComponentsInChildren<Component>(false);

                foreach (Component c in clist)
                {
                    Log(c.ToString() + " | " + c.transform.position.ToString());
                }

                GameObject gordotmp2 = Instantiate<GameObject>(lookupDirector.GetGordo(Identifiable.Id.DERVISH_GORDO), playertmp.transform.position, Quaternion.identity);
                Component[] clist2 = gordotmp2.GetComponentsInChildren<Component>(false);

                foreach (Component c in clist2)
                {
                    Log(c.ToString() + " | " + c.transform.position.ToString());
                }

                Destroy(gordotmp);
                Destroy(gordotmp2);
                //now that's what i call efficiency
                //in my defence this is a command meant exclusively for debugging info
            }
            else
            {
                Log("Click apply randomization first dummy");
            }
        }*/

        #endregion

        #region GUI

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

            //GUI.DragWindow(); doesn't work for some reason, just makes all button non interactable

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
                    slimeRandomModeInput = GUILayout.SelectionGrid(slimeRandomModeInput, selectionContents, 1);
                    GUILayout.EndVertical();

                    scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition);

                    slime_allowLuckySlimes = GUILayout.Toggle(slime_allowLuckySlimes, "Allow Lucky Slimes in the pool");
                    slime_allowGoldSlimes = GUILayout.Toggle(slime_allowGoldSlimes, "Allow Gold Slimes in the pool");
                    slime_allowSaberSlimes = GUILayout.Toggle(slime_allowSaberSlimes, "Allow Saber Slimes in the pool");
                    slime_allowQuicksilverSlimes = GUILayout.Toggle(slime_allowQuicksilverSlimes, "Allow Quicksilver Slimes in the pool");
                    slime_allowGlitchSlimes = GUILayout.Toggle(slime_allowGlitchSlimes, "Allow Glitch Slimes in the pool");
                    slime_allowTarr = GUILayout.Toggle(slime_allowTarr, "Allow Tarr in the pool");
                    slime_allowGlitchTarr = GUILayout.Toggle(slime_allowGlitchTarr, "Allow Glitch Tarr in the pool");

                    GUILayout.EndScrollView();
                    GUILayout.EndHorizontal();
                    break;
                case 1: //gordos
                    GUILayout.BeginHorizontal(GUILayout.Height(windowSizeY * 0.8f));
                    scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition);

                    GUILayout.Label("Randomization of Gordo types in progress");
                    gordo_randomizeFoodRequirement = GUILayout.Toggle(gordo_randomizeFoodRequirement, "Randomize the amount of food required to pop each Gordo");
                    if(gordo_randomizeFoodRequirement)
                    {
                        GUILayout.Label("Minimum: " + gordo_minFoodRequirement);
                        gordo_minFoodRequirement = (int)GUILayout.HorizontalSlider(gordo_minFoodRequirement, 1, gordo_maxFoodRequirement);
                        GUILayout.Label("Maximum: " + gordo_maxFoodRequirement);
                        gordo_maxFoodRequirement = (int)GUILayout.HorizontalSlider(gordo_maxFoodRequirement, gordo_minFoodRequirement, 100);
                    }

                    GUILayout.EndScrollView();
                    GUILayout.EndHorizontal();
                    break;
                case 2: //diets
                    GUILayout.BeginHorizontal(GUILayout.Height(windowSizeY * 0.8f));
                    scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition);

                    diet_enabled = GUILayout.Toggle(diet_enabled, "Enable diet randomization (only 1-to-1 Mapped Mode supported)");
                    GUILayout.Label("The following options are always disabled for now:");
                    GUILayout.Toggle(false, "Allow the Gold Slime's diet to be changed and applied to other slimes");
                    GUILayout.Toggle(false, "Allow the Lucky Slime's diet to be changed and applied to other slimes");

                    GUILayout.EndScrollView();
                    GUILayout.EndHorizontal();
                    break;
                case 3: //produce
                    GUILayout.BeginHorizontal(GUILayout.Height(windowSizeY * 0.8f));

                    GUILayout.BeginVertical(GUILayout.Width(100));
                    GUILayout.Label("Randomization Mode");
                    produceRandomModeInput = GUILayout.SelectionGrid(produceRandomModeInput, selectionContents, 1);
                    GUILayout.Label(GUI.tooltip);
                    GUILayout.EndVertical();

                    scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition);
                    produce_allowGildedGinger = GUILayout.Toggle(produce_allowGildedGinger, "Allow Gilded Gingers in the pool");
                    produce_allowKookadoba = GUILayout.Toggle(produce_allowKookadoba, "Allow Kookadobas in the pool");
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

        #endregion

        #region Pause

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

        #endregion

        #region Info Commands

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

        #endregion
            
        #region Randomization Setup

        //Takes a seed and sets up everything for randomization
        private void ApplyRandomization(int seed)
        {
            randomSeed = seed;
            Log("Randomizing using seed " + randomSeed);

            CommandLoadPrefabs();

            slime_randomMode = (RandomMode)slimeRandomModeInput;
            produce_randomMode = (RandomMode)produceRandomModeInput;

            /* For consistent randomization for a given seed, we want to create a new Random object for each element that's being randomized separately */
            /* These methods simply *set up* randomization tables. The replacement is done in separate methods that are called from Harlomony patches */
            RandomizeSlimeSpawns(new System.Random(seed));
            RandomizeSlimeDiets(new System.Random(seed));
            RandomizeProduce(new System.Random(seed));
            //RandomizeGordos(new System.Random(seed));

            //Set up for randomization that needs to be done at runtime
            runTimeRand = new System.Random(seed);
        }

        private void RandomizeSlimeSpawns(System.Random rand)
        {
            //Build list of allowed slimes
            allowedSlimes = Identifiable.SLIME_CLASS.ToList(); //get list of all base slimes

            //Remove slime types not allowed by options
            if (!slime_allowLuckySlimes) allowedSlimes.Remove(Identifiable.Id.LUCKY_SLIME);
            if (!slime_allowGoldSlimes) allowedSlimes.Remove(Identifiable.Id.GOLD_SLIME);
            if (!slime_allowSaberSlimes) allowedSlimes.Remove(Identifiable.Id.SABER_SLIME);
            if (!slime_allowQuicksilverSlimes) allowedSlimes.Remove(Identifiable.Id.QUICKSILVER_SLIME);
            if (!slime_allowGlitchSlimes) allowedSlimes.Remove(Identifiable.Id.GLITCH_SLIME);
            if (!slime_allowTarr) allowedSlimes.Remove(Identifiable.Id.TARR_SLIME);
            if (!slime_allowGlitchTarr) allowedSlimes.Remove(Identifiable.Id.GLITCH_TARR_SLIME);
            
            if(slime_randomMode == RandomMode.DISABLED || slime_randomMode == RandomMode.CHAOTIC)
            {
                //do nothing; this is handled in GetRandomizedSlime()
                return;
            }

            slimeMap = new Dictionary<Identifiable.Id, Identifiable.Id>();
          
            //Build Dictionary mapping if in a mapping randomization mode
            if (slime_randomMode == RandomMode.MAPPED_NODUPES)
            {
                CreateRandomMapping(allowedSlimes, slimeMap, rand, false);
            }
            else if(slime_randomMode == RandomMode.MAPPED_DUPES)
            {
                CreateRandomMapping(allowedSlimes, slimeMap, rand, true);
            }
        }

        private void RandomizeSlimeDiets(System.Random rand)
        {
            // Build list of food items that cannot be set as favorite //
            List<Identifiable.Id> restrictedFavorites = new List<Identifiable.Id>();
            if (!diet_allowElderFavorite)
            {
                restrictedFavorites.Add(Identifiable.Id.ELDER_HEN);
                restrictedFavorites.Add(Identifiable.Id.ELDER_ROOSTER);
            }
            if (!diet_allowGildedGingerFavorite) restrictedFavorites.Add(Identifiable.Id.GINGER_VEGGIE);
            if (!diet_allowKookadobaFavorite) restrictedFavorites.Add(Identifiable.Id.KOOKADOBA_FRUIT);
            if (!diet_allowTofuFavorite) restrictedFavorites.Add(Identifiable.Id.SPICY_TOFU);
            

            // Build list of allowed slime & largo types //
            SlimeDefinitions slimeDefinitions = SRSingleton<GameContext>.Instance.SlimeDefinitions;
            List<Identifiable.Id> allowedSlimes = new List<Identifiable.Id>(Identifiable.EATERS_CLASS);
            
            //remove slimes from the list that are special cases or have no diet
            allowedSlimes.Remove(Identifiable.Id.FIRE_SLIME);
            allowedSlimes.Remove(Identifiable.Id.GLITCH_SLIME);
            allowedSlimes.Remove(Identifiable.Id.GLITCH_TARR_SLIME);
            allowedSlimes.Remove(Identifiable.Id.PUDDLE_SLIME);
            allowedSlimes.Remove(Identifiable.Id.QUICKSILVER_SLIME);
            allowedSlimes.Remove(Identifiable.Id.TARR_SLIME);

            //remove slimes as specified by options
            if(!diet_changeGoldSlimeDiet) allowedSlimes.Remove(Identifiable.Id.GOLD_SLIME);
            if(!diet_changeLuckySlimeDiet) allowedSlimes.Remove(Identifiable.Id.LUCKY_SLIME);


            // Handle base slime types //
            IEnumerable<Identifiable.Id> baseSlimes = from slime in allowedSlimes
                                                      where slime.ToString().EndsWith("_SLIME")
                                                      select slime;

            foreach(Identifiable.Id slime in baseSlimes)
            {
                SlimeDefinition currSlime = slimeDefinitions.GetSlimeByIdentifiableId(slime);

                Log("Slime: " + currSlime);

                //set new food group
                SlimeEat.FoodGroup newFoodGroup = PickRandom(VALID_FOOD_GROUPS, rand);
                currSlime.Diet.MajorFoodGroups = new SlimeEat.FoodGroup[] { newFoodGroup };

                Log("Food group: " + newFoodGroup.ToString());

                //set new favorite food
                Identifiable.Id newFavoriteFood = Identifiable.Id.NONE;
                while(newFavoriteFood == Identifiable.Id.NONE && restrictedFavorites.Contains(newFavoriteFood))
                {
                    IEnumerable<Identifiable.Id> idClass = null;
                    if (newFoodGroup == SlimeEat.FoodGroup.FRUIT) idClass = Identifiable.FRUIT_CLASS;
                    else if (newFoodGroup == SlimeEat.FoodGroup.VEGGIES) idClass = Identifiable.VEGGIE_CLASS;
                    else if (newFoodGroup == SlimeEat.FoodGroup.MEAT) idClass = Identifiable.MEAT_CLASS;
                    else
                    {
                        Log("Invalid food group in RandomizeSlimeDiets. Applying fallback...");
                        newFavoriteFood = Identifiable.Id.POGO_FRUIT;
                        break;
                    }

                    newFavoriteFood = PickRandom(idClass, rand);
                }

                Log("Favorite food: " + newFavoriteFood.ToString());

                currSlime.Diet.Favorites = new Identifiable.Id[] { newFavoriteFood };
            }


            // Handle largo types //
            IEnumerable<Identifiable.Id> largos = from largo in allowedSlimes
                                                  where largo.ToString().EndsWith("_LARGO") && (diet_changePinkSlimeDiet ? true : !largo.ToString().Contains("PINK")) //skip pink slime + pink largo variants if options dictate
                                                  select largo;


            //refresh the EatMap of every slime definition (apply the changes, essentially)
            slimeDefinitions.RefreshDefinitions();
        }

        private void RandomizeProduce(System.Random rand)
        {
            allowedFruits = Identifiable.FRUIT_CLASS.ToList();
            allowedVeggies = Identifiable.VEGGIE_CLASS.ToList();

            if (!produce_allowKookadoba) allowedFruits.Remove(Identifiable.Id.KOOKADOBA_FRUIT);
            if (!produce_allowGildedGinger) allowedVeggies.Remove(Identifiable.Id.GINGER_VEGGIE);

            allowedProduce = new List<Identifiable.Id>();
            allowedProduce.AddRange(allowedFruits);
            allowedProduce.AddRange(allowedVeggies);

            if (produce_randomMode == RandomMode.DISABLED || produce_randomMode == RandomMode.CHAOTIC)
            {
                return;
            }

            produceMap = new Dictionary<Identifiable.Id, Identifiable.Id>();

            if(produce_randomMode == RandomMode.MAPPED_NODUPES)
            {
                CreateRandomMapping(allowedProduce, produceMap, rand, false);
            }
            else if(produce_randomMode == RandomMode.MAPPED_DUPES)
            {
                CreateRandomMapping(allowedProduce, produceMap, rand, true);
            }
        }

        private void RandomizeGordos(System.Random rand)
        {
            /*
            IEnumerable<GameObject> gordos = lookupDirector.GordoEntries;
            foreach(GameObject obj in gordos)
            {
                Log(obj.ToString());
            }
            */
        }

        // Creates a random mapping of the elements in keyList and stores it in mapDict
        private void CreateRandomMapping<T>(List<T> keyList, Dictionary<T,T> mapDict, System.Random rand, bool allowDupes = false)
        {
            if (!allowDupes)
            {
                List<T> valueList = new List<T>(keyList);

                //Fisher-Yates shuffle on valueList
                for(int i = valueList.Count-1; i > 0; i--)
                {
                    int j = rand.Next(0,i);
                    var tmp = valueList[i];
                    valueList[i] = valueList[j];
                    valueList[j] = tmp;
                }

                //In order, map elements from keyList (original object) to valueList (replacement object)
                for(int i = 0; i < keyList.Count; i++)
                {
                    mapDict.Add(keyList[i], valueList[i]);
                }
            }
            else
            {
                foreach (T obj in keyList)
                {
                    mapDict.Add(obj, keyList[rand.Next(keyList.Count())]);
                }
            }
        }

        //Randomly pick and return an item from the given collection
        private T PickRandom<T>(IEnumerable<T> items, System.Random rand)
        {
            int index = rand.Next(0, items.Count());
            return items.ElementAt(index);
        }

        #endregion
        
        #region Randomization Replacement

        public static GameObject GetRandomizedSlime(GameObject prefab) //currently no largo support, in future this will be added here
        {
            if(slimePrefabToId != null && !slimePrefabToId.ContainsKey(prefab.ToString())) //this slime is a largo. for now just return it
            {
                return prefab;
            }

            Identifiable.Id id = slimePrefabToId[prefab.ToString()];

            if (!allowedSlimes.Contains(id)) //this means the slime was excluded via an option, return back the original (don't do anything)
            {
                return prefab;
            }

            switch(slime_randomMode)
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
                    Log("Warning: Invalid randomization mode: " + slime_randomMode);
                    return prefab;
            }
        }

        public static GameObject GetRandomizedProduce(GameObject prefab)
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

            switch (produce_randomMode)
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
                    Log("Warning: Invalid randomization mode: " + slime_randomMode);
                    return prefab;
            }
        }

        public static GameObject GetRandomizedGordo()
        {
            return null;
        }

        public static int GetRandomizedGordoFoodTarget()
        {
            return runTimeRand.Next(gordo_minFoodRequirement, gordo_maxFoodRequirement+1);
        }

        #endregion
    }

    public enum RandomMode
    {
        DISABLED,
        CHAOTIC,
        MAPPED_NODUPES,
        MAPPED_DUPES
    }
}