using UnityEngine;
using UModFramework.API;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;

namespace SRRandomizer
{
    [UMFHarmony(5)]
    [UMFScript]
    class SRRandomizer : MonoBehaviour
    {
        #region  Randomization Variables

        private static int randomSeed;
        private static System.Random runtimeRand; //System.Random object used during runtime after random tables are setup. used for randomization mode 1 ("Chaotic")

        public static SlimeRandomizer slimeRandomizer;
        public static ProduceRandomizer produceRandomizer;
        public static SlimeDietRandomizer slimeDietRandomizer;
        public static GordoRandomizer gordoRandomizer;
        public static StatueRandomizer statueRandomizer;

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
        private static readonly GUIContent[] selectionContents = { disabledContent, chaoticContent, mappedContent };
        private static readonly string[] toolbarTabTexts = { "Slimes", "Gordos", "Diets", "Food", "Statues" };

        private static readonly GUIStyle LABEL_STYLE_BOLD = new GUIStyle();

        private static int currentToolbarTab;
        private static string seedInput;
        private int slimeRandomModeInput;
        private int produceRandomModeInput;
        private bool dietEnabled;
        public static bool gordo_randomizeFoodRequirement;

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

            LABEL_STYLE_BOLD.fontSize = 16;
            LABEL_STYLE_BOLD.fontStyle = FontStyle.Bold;
            LABEL_STYLE_BOLD.normal.textColor = Color.white;

            slimeRandomizer = new SlimeRandomizer();
            slimeDietRandomizer = new SlimeDietRandomizer();
            produceRandomizer = new ProduceRandomizer();
            gordoRandomizer = new GordoRandomizer();
            statueRandomizer = new StatueRandomizer();
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

                    GUILayout.Label("Slime types allowed to be randomized:", LABEL_STYLE_BOLD);
                    slimeRandomizer.allowFireSlimes = GUILayout.Toggle(slimeRandomizer.allowFireSlimes, "Fire Slimes");
                    slimeRandomizer.allowLuckySlimes = GUILayout.Toggle(slimeRandomizer.allowLuckySlimes, "Lucky Slimes");
                    slimeRandomizer.allowGoldSlimes = GUILayout.Toggle(slimeRandomizer.allowGoldSlimes, "Gold Slimes");
                    slimeRandomizer.allowSaberSlimes = GUILayout.Toggle(slimeRandomizer.allowSaberSlimes, "Saber Slimes (may make other slime(s) unobtainable in 1-to-1 or ?-to-?)");
                    slimeRandomizer.allowQuicksilverSlimes = GUILayout.Toggle(slimeRandomizer.allowQuicksilverSlimes, "Quicksilver Slimes (may break Mochi minigame)");
                    slimeRandomizer.allowGlitchSlimes = GUILayout.Toggle(slimeRandomizer.allowGlitchSlimes, "Glitch Slimes (may break Viktor minigame)");
                    slimeRandomizer.allowTarr = GUILayout.Toggle(slimeRandomizer.allowTarr, "Tarr");
                    slimeRandomizer.allowGlitchTarr = GUILayout.Toggle(slimeRandomizer.allowGlitchTarr, "Glitch Tarr");

                    GUILayout.Label("\nLargo Settings", LABEL_STYLE_BOLD);
                    slimeRandomizer.randomizeLargos = GUILayout.Toggle(slimeRandomizer.randomizeLargos, "Randomize largo spawns");
                    slimeRandomizer.largoPreserveBase = GUILayout.Toggle(slimeRandomizer.largoPreserveBase, "Always preserve 1 base slime type");
                    //slimeRandomizer.largoRandomFeral = GUILayout.Toggle(slimeRandomizer.largoRandomFeral, "Randomly decide if a largo is feral");

                    GUILayout.EndScrollView();
                    GUILayout.EndHorizontal();
                    break;

                case 1: //gordos
                    GUILayout.BeginHorizontal(GUILayout.Height(windowSizeY * 0.8f));
                    scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition);

                    gordo_randomizeFoodRequirement = GUILayout.Toggle(gordo_randomizeFoodRequirement, "Randomize the amount of food required to pop each Gordo");
                    if(gordo_randomizeFoodRequirement)
                    {
                        GUILayout.Label("Minimum: " + gordoRandomizer.minFoodRequirement);
                        gordoRandomizer.minFoodRequirement = (int)GUILayout.HorizontalSlider(gordoRandomizer.minFoodRequirement, 1, gordoRandomizer.maxFoodRequirement);
                        GUILayout.Label("Maximum: " + gordoRandomizer.maxFoodRequirement);
                        gordoRandomizer.maxFoodRequirement = (int)GUILayout.HorizontalSlider(gordoRandomizer.maxFoodRequirement, gordoRandomizer.minFoodRequirement, 100);
                    }

                    GUILayout.EndScrollView();
                    GUILayout.EndHorizontal();
                    break;

                case 2: //diets
                    GUILayout.BeginHorizontal(GUILayout.Height(windowSizeY * 0.8f));
                    scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition);

                    dietEnabled = GUILayout.Toggle(dietEnabled, "Enable diet randomization (affects Slimes, Largos, and Gordos)");
                    if(dietEnabled)
                    {
                        slimeDietRandomizer.changePinkSlimeDiet = GUILayout.Toggle(slimeDietRandomizer.changePinkSlimeDiet, "Allow the Pink Slime diet to be changed");
                        //diet_changeGoldSlimeDiet = GUILayout.Toggle(diet_changeGoldSlimeDiet, "Allow the Gold Slime diet to be changed");
                        //diet_changeLuckySlimeDiet = GUILayout.Toggle(diet_changeLuckySlimeDiet, "Allow the Lucky Slime diet to be changed");
                        slimeDietRandomizer.allowElderFavorite = GUILayout.Toggle(slimeDietRandomizer.allowElderFavorite, "Allow Elder Hens/Roostros to be favorite foods");
                        slimeDietRandomizer.allowKookadobaFavorite = GUILayout.Toggle(slimeDietRandomizer.allowKookadobaFavorite, "Allow Kookadobas to be favorite foods");
                        slimeDietRandomizer.allowGildedGingerFavorite = GUILayout.Toggle(slimeDietRandomizer.allowGildedGingerFavorite, "Allow Gilded Gingers to be favorite foods");
                        slimeDietRandomizer.allowTofuFavorite = GUILayout.Toggle(slimeDietRandomizer.allowTofuFavorite, "Allow Spicy Tofu to be favorite foods");
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("NOTE: The following Slimes are unaffected by diet randomization:");
                        GUILayout.Label("Fire, Glitch, Glitch Tarr, Gold, Lucky, Puddle, Quicksilver, Tarr");
                    }

                    GUILayout.EndScrollView();
                    GUILayout.EndHorizontal();
                    break;

                case 3: //food
                    GUILayout.BeginHorizontal(GUILayout.Height(windowSizeY * 0.8f));

                    GUILayout.BeginVertical(GUILayout.Width(100));
                    GUILayout.Label("Randomization Mode");
                    produceRandomModeInput = GUILayout.SelectionGrid(produceRandomModeInput, selectionContents, 1);
                    GUILayout.EndVertical();

                    scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition);

                    GUILayout.Label("Food types allowed to be randomized:", LABEL_STYLE_BOLD);
                    produceRandomizer.allowGildedGinger = GUILayout.Toggle(produceRandomizer.allowGildedGinger, "Allow Gilded Gingers");
                    produceRandomizer.allowKookadoba = GUILayout.Toggle(produceRandomizer.allowKookadoba, "Allow Kookadobas");
                    //allowFruitToVeggie = GUILayout.Toggle(allowFruitToVeggie, "Allow Fruits to be replaced by Veggies");
                    //allowVeggieToFruit = GUILayout.Toggle(allowVeggieToFruit, "Allow Veggies to be replaced by Fruits");

                    GUILayout.Label("Other options:", LABEL_STYLE_BOLD);
                    produceRandomizer.randomizeGardens = GUILayout.Toggle(produceRandomizer.randomizeGardens, "Randomize Gardens");

                    GUILayout.EndScrollView();
                    GUILayout.EndHorizontal();
                    break;

                case 4: //plort statues
                    scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition);

                    GUILayout.Label("General Settings", LABEL_STYLE_BOLD);
                    statueRandomizer.ensureCompletable = GUILayout.Toggle(statueRandomizer.ensureCompletable, "Ensure statues will be fillable without glitches or cheats");
                    GUILayout.Label("Note: Has no effect if slime randomization is set to Chaotic");

                    GUILayout.Label("Ruins Gate", LABEL_STYLE_BOLD);
                    statueRandomizer.randomizeRuins = GUILayout.Toggle(statueRandomizer.randomizeRuins, "Randomize Ruins gate statues") || statueRandomizer.ensureCompletable;

                    GUILayout.Label("Desert Portal", LABEL_STYLE_BOLD);
                    statueRandomizer.randomizeDesert = GUILayout.Toggle(statueRandomizer.randomizeDesert, "Randomize Desert portal statues") || statueRandomizer.ensureCompletable;
                    //statueRandomizer.desertSinglePlort = GUILayout.Toggle(statueRandomizer.desertSinglePlort, "Make each Desert statue require the same plort");
                    //statueRandomizer.desertPortalDifferent = GUILayout.Toggle(statueRandomizer.desertPortalDifferent, "If possible, make all desert statue plorts different from ruins gate plorts") && statueRandomizer.randomizeDesert;

                    GUILayout.Label("Allowed Plorts", LABEL_STYLE_BOLD);
                    statueRandomizer.allowGoldPlort = GUILayout.Toggle(statueRandomizer.allowGoldPlort, "Gold Plort");
                    statueRandomizer.allowSaberPlort = GUILayout.Toggle(statueRandomizer.allowSaberPlort, "Saber Plort");
                    GUILayout.EndScrollView();
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

            GUI.DragWindow();
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
            if(slimeRandomizer.slimeMap == null)
            {
                Log("Please create a slime map first.");
                return;
            }
            foreach(KeyValuePair<Identifiable.Id, Identifiable.Id> item in slimeRandomizer.slimeMap)
            {
                Log("Original: " + item.Key.ToString() + ", Replacement: " + item.Value.ToString());
            }
        }

        private void CommandPrintProduceMap()
        {
            if (produceRandomizer.produceMap == null)
            {
                Log("Please create a produce map first.");
                return;
            }
            foreach (KeyValuePair<Identifiable.Id, Identifiable.Id> item in produceRandomizer.produceMap)
            {
                Log("Original: " + item.Key.ToString() + ", Replacement: " + item.Value.ToString());
            }
        }

        #endregion

        #region Update
        public void Update()
        {
            if(!Levels.isSpecial() && !Levels.isMainMenu())
            {
                // If player is targeting a plort statue, show the needed plort on screen where the name of the currently targeted object is normally shown
                GameObject target = SRSingleton<SceneContext>.Instance.PlayerState.Targeting;
                if(target != null)
                {
                    PuzzleSlot slot = target.GetComponent<PuzzleSlot>();
                    if (slot != null)
                    {
                        TargetingUI targetUI = SRSingleton<TargetingUI>.Instance;
                        typeof(TargetingUI).GetField("currentTarget", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(targetUI, target); // overwrite the currently targeted object

                        // Set appropriate text for the needed object
                        targetUI.nameText.text = Identifiable.GetName(slot.catchId);
                        targetUI.infoText.text = String.Empty;
                        targetUI.nameText.enabled = true; // make sure the text actually shows (if it wasn't already enabled before looking at the statue, it won't become enabled until after looking away from it)
                    }
                }
            }
        }
        #endregion

        #region Randomization

        // Takes a seed and sets up everything for randomization
        // Certain things are taken care of by each respective *Randomization class without any action from this method
        private void ApplyRandomization(int seed)
        {
            randomSeed = seed;
            Log("Randomizing using seed " + randomSeed);

            slimeRandomizer.randomMode = (RandomMode)slimeRandomModeInput;
            produceRandomizer.randomMode = (RandomMode)produceRandomModeInput;

            /* For consistent randomization for a given seed, we want to create a new Random object for each element that's being randomized separately */
            /* These methods simply *set up* randomization tables. The replacement is done in separate methods that are called from Harmony patches */
            slimeRandomizer.RandomizeSlimeSpawns(new System.Random(seed));
            if (dietEnabled)
                slimeDietRandomizer.RandomizeSlimeDiets(new System.Random(seed));

            produceRandomizer.RandomizeProduce(new System.Random(seed));

            statueRandomizer.RandomizeStatues(new System.Random(seed));

            //Set up for randomization that needs to be done at runtime
            runtimeRand = new System.Random(seed);
        }

        // Creates a random mapping of the elements in keyList and stores it in mapDict
        internal static void CreateRandomMapping<T>(List<T> keyList, Dictionary<T,T> mapDict, System.Random rand, bool allowDupes = false)
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

        internal static int RuntimeRandNext(int max)
        {
            return runtimeRand.Next(max);
        }

        internal static int RuntimeRandNext(int min, int max)
        {
            return runtimeRand.Next(min, max);
        }

        //Randomly pick and return an item from the given collection
        internal static T PickRandom<T>(IEnumerable<T> items, System.Random rand)
        {
            int index = rand.Next(items.Count());
            return items.ElementAt(index);
        }

        #endregion
    }

    public enum RandomMode
    {
        DISABLED,           // Randomization disabled
        CHAOTIC,            // Every instance of this identifiable is randomized
        MAPPED_NODUPES,     // 1-to-1 mapped; each identifiable is mapped to 1 other and there cannot be duplicate mappings
        MAPPED_DUPES,       // ?-to-? mapped; each identifiable is mapped to 1 other and there can be duplicates
        SWAPS               // TODO: Like 1-to-1 mapped, but identifiables are randomly swapped instead of shuffled
    }
}