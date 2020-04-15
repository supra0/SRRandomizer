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
        private GameModel gameModel;
        private List<SpawnerTriggerModel> spawnerTriggers;
        private LookupDirector lookupDirector;
        private static System.Random rand = new System.Random(); //temp implementation; will want save files to have different seeds maybe

        private static int slimeRandomizeMode;
        private static int foodRandomizeMode;

        private static Dictionary<Identifiable.Id, Identifiable.Id> slimeMap; //1:1 mapping of slime types


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

            //Register Commands
            UMFGUI.RegisterCommand("srr_randomslime", "srr_randomslime", new string[] { "randomslime" }, 0, "Test command, prints a random slime type.", CommandRandomSlime);
            UMFGUI.RegisterCommand("srr_listslimes", "srr_listslimes", new string[] { "listslimes" }, 0, "Test command, prints all slime types.", CommandListSlimes);
            UMFGUI.RegisterCommand("srr_newslimemap <allowDupes>", "srr_newslimemap", new string[] { "newslimemap" }, 1, "Creates a new random mapping of slime types.", CommandNewSlimeMap);
            UMFGUI.RegisterCommand("srr_printslimemap", "srr_printslimemap", new string[] { "printslimemap" }, 0, "Prints the current random slime map.", CommandPrintSlimeMap);
            UMFGUI.RegisterCommand("srr_setmode <mode>", "srr_setmode", new string[] { "setmode" }, 1, "Sets the randomization mode for slimes (0=off, 1=nomap, 2=mapped).", CommandSetMode);
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

        private void CommandRandomSlime()
        {
            Log(GetRandomSlime().ToString());
        }


        private void CommandListSlimes()
        {
            foreach (Identifiable.Id id in Identifiable.SLIME_CLASS)
            {
                Log(id.ToString());
            }
        }

        private void CommandNewSlimeMap()
        {
            if(UMFGUI.Args.Length == 1)
            {
                int num = int.Parse(UMFGUI.Args[0]);
                switch(num)
                {
                    case 0:
                        CreateRandomMapping(Identifiable.SLIME_CLASS, false);
                        break;
                    case 1:
                        CreateRandomMapping(Identifiable.SLIME_CLASS, true);
                        break;
                    default:
                        Log("Specify whether to allow duplicate mappings (no=0; yes=1)");
                        break;
                }
            }
            else
            {
                Log("Insufficient parameters");
            }
        }

        private void CommandPrintSlimeMap()
        {
            foreach(KeyValuePair<Identifiable.Id, Identifiable.Id> item in slimeMap)
            {
                Log("Original: " + item.Key.ToString() + ", Replacement: " + item.Value.ToString());
            }
        }

        private void CommandSetMode()
        {
            if(UMFGUI.Args.Length == 1)
            {
                slimeRandomizeMode = int.Parse(UMFGUI.Args[0]);
            }
            else
            {
                Log("Insufficient parameters");
            }
        }

        // Creates a random mapping of the elements in the given HashSet
        private void CreateRandomMapping(HashSet<Identifiable.Id> ids, bool allowDupes = false)
        {
            List<Identifiable.Id> idList = ids.ToList();
            slimeMap = new Dictionary<Identifiable.Id, Identifiable.Id>();

            if (!allowDupes)
            {
                //Fisher-Yates shuffle
                for(int i = idList.Count-1; i > 0; i--)
                {
                    int j = rand.Next(0,i);
                    slimeMap.Add(ids.ElementAt(i), idList[j]);
                }
            }
            else
            {
                foreach(Identifiable.Id id in idList)
                {
                    slimeMap.Add(id, idList[rand.Next(idList.Count())]);
                }
            }
        }

        public static Identifiable.Id Randomize(Identifiable.Id id)
        {
            switch(slimeRandomizeMode)
            {
                case 0: //randomization deactivated
                    return id;
                    break;
                case 1: //no mapping
                    return GetRandomSlime();
                case 2: //mapping
                    return slimeMap[id];
                default: //do nothing
                    Log("Warning: Invalid randomization mode: " + slimeRandomizeMode);
                    return id;
            }
        }

        public static Identifiable.Id GetRandomSlime()
        {
            HashSet<Identifiable.Id> ids = Identifiable.SLIME_CLASS; //get a list of all slime types (doesn't include largos, gordos)
            return ids.ElementAt(rand.Next(ids.Count));
        }

    }
}