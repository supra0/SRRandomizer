using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRRandomizer
{
    class SlimeDietRandomizer
    {

        #region Option Variables
        public static RandomMode diet_RandomMode = RandomMode.DISABLED;
        
        // Slime options
        public bool changePinkSlimeDiet;
        public bool changeGoldSlimeDiet;
        public bool changeLuckySlimeDiet;

        // Special food options
        public bool allowElderFavorite;
        public bool allowKookadobaFavorite;
        public bool allowGildedGingerFavorite;
        public bool allowTofuFavorite;
        #endregion

        #region Randomization Variables
        public static readonly SlimeEat.FoodGroup[] VALID_FOOD_GROUPS = { SlimeEat.FoodGroup.FRUIT, SlimeEat.FoodGroup.MEAT, SlimeEat.FoodGroup.VEGGIES };
        #endregion

        internal void RandomizeSlimeDiets(System.Random rand)
        {
            // Build list of food items that cannot be set as favorite //
            List<Identifiable.Id> restrictedFavorites = new List<Identifiable.Id>();
            if (!allowElderFavorite)
            {
                restrictedFavorites.Add(Identifiable.Id.ELDER_HEN);
                restrictedFavorites.Add(Identifiable.Id.ELDER_ROOSTER);
            }
            if (!allowGildedGingerFavorite) restrictedFavorites.Add(Identifiable.Id.GINGER_VEGGIE);
            if (!allowKookadobaFavorite) restrictedFavorites.Add(Identifiable.Id.KOOKADOBA_FRUIT);
            if (!allowTofuFavorite) restrictedFavorites.Add(Identifiable.Id.SPICY_TOFU);

            // Build list of allowed slime & largo types //
            SlimeDefinitions slimeDefinitions = SRSingleton<GameContext>.Instance.SlimeDefinitions;
            List<Identifiable.Id> allowedSlimes = new List<Identifiable.Id>(Identifiable.EATERS_CLASS);

            // remove slimes from the list that are special cases or have no diet
            allowedSlimes.Remove(Identifiable.Id.FIRE_SLIME);
            allowedSlimes.Remove(Identifiable.Id.GLITCH_SLIME);
            allowedSlimes.Remove(Identifiable.Id.GLITCH_TARR_SLIME);
            allowedSlimes.Remove(Identifiable.Id.PUDDLE_SLIME);
            allowedSlimes.Remove(Identifiable.Id.QUICKSILVER_SLIME);
            allowedSlimes.Remove(Identifiable.Id.TARR_SLIME);

            // remove slimes as specified by options
            if (!changeGoldSlimeDiet) allowedSlimes.Remove(Identifiable.Id.GOLD_SLIME);
            if (!changeLuckySlimeDiet) allowedSlimes.Remove(Identifiable.Id.LUCKY_SLIME);
            if (!changePinkSlimeDiet) allowedSlimes.Remove(Identifiable.Id.PINK_SLIME);

            // Handle base slime types //
            IEnumerable<Identifiable.Id> baseSlimes = from slime in allowedSlimes
                                                      where slime.ToString().EndsWith("_SLIME")
                                                      select slime;

            foreach (Identifiable.Id slime in baseSlimes)
            {
                SlimeDefinition currSlime = slimeDefinitions.GetSlimeByIdentifiableId(slime);

                //SRRandomizer.Log("Slime: " + currSlime);

                // set new food group
                SlimeEat.FoodGroup newFoodGroup = SRRandomizer.PickRandom(VALID_FOOD_GROUPS, rand);
                currSlime.Diet.MajorFoodGroups = new SlimeEat.FoodGroup[] { newFoodGroup };

                //SRRandomizer.Log("Food group: " + newFoodGroup.ToString());

                // set new favorite food
                Identifiable.Id newFavoriteFood = Identifiable.Id.NONE;
                while (newFavoriteFood == Identifiable.Id.NONE || restrictedFavorites.Contains(newFavoriteFood))
                {
                    IEnumerable<Identifiable.Id> idClass = null;
                    if (newFoodGroup == SlimeEat.FoodGroup.FRUIT) idClass = Identifiable.FRUIT_CLASS;
                    else if (newFoodGroup == SlimeEat.FoodGroup.VEGGIES) idClass = Identifiable.VEGGIE_CLASS;
                    else if (newFoodGroup == SlimeEat.FoodGroup.MEAT) idClass = Identifiable.MEAT_CLASS;
                    else
                    {
                        SRRandomizer.Log("Invalid food group in RandomizeSlimeDiets. Applying Pogofruit fallback...");
                        newFavoriteFood = Identifiable.Id.POGO_FRUIT;
                        break;
                    }

                    newFavoriteFood = SRRandomizer.PickRandom(idClass, rand);
                }

                //SRRandomizer.Log("Favorite food: " + newFavoriteFood.ToString());

                currSlime.Diet.Favorites = new Identifiable.Id[] { newFavoriteFood };
            }


            // Handle largo types //
            IEnumerable<Identifiable.Id> largos = from largo in allowedSlimes
                                                  where largo.ToString().EndsWith("_LARGO")
                                                  select largo;

            foreach (Identifiable.Id largo in largos)
            {
                SlimeDefinition currLargo = slimeDefinitions.GetSlimeByIdentifiableId(largo);
                if (currLargo.IsLargo && currLargo.BaseSlimes.Length == 2) //coherence check
                {
                    currLargo.Diet = SlimeDiet.Combine(currLargo.BaseSlimes[0].Diet, currLargo.BaseSlimes[1].Diet);
                }
                else
                {
                    SRRandomizer.Log("Non-largo coming through largo diet randomization? Id: " + largo.ToString());
                }
            }


            // refresh the EatMap of every slime definition (apply the changes, essentially)
            slimeDefinitions.RefreshDefinitions();
        }

        internal static void ResetDietsToDefault()
        {
            SRRandomizer.Log("Currently not implemented");
        }
    }
}
