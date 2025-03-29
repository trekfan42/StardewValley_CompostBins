using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Machines;
using StardewValley.Network;

namespace CompostBins
{
    internal sealed class ModEntry : Mod
    {
        public static IMonitor? mon = null;

        public override void Entry(IModHelper helper)
        {
            mon = Monitor;
        }

        public static Item GetResourceOutput(StardewValley.Object machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady)
        {
            overrideMinutesUntilReady = null;

            if (machine is StardewValley.Object)
            {
                int machineType = GetMachineType(machine);

                // ✅ Safeguard against unknown compost bin types
                if (machineType == 0)
                {
                    Game1.showRedMessage("Unrecognized compost bin.");
                    return null;
                }

                Building building = machine.Location.ParentBuilding;
                if (building == null || building.indoors.Value is not AnimalHouse)
                {
                    Game1.showRedMessage("Must be placed in a Barn or Coop!");
                    return null;
                }

                int buildingLevel = GetBuildingLevel(building);
                int animalCount = GetAnimalCount(building);

                if (animalCount < 1)
                {
                    if (Game1.timeOfDay != 600) //disables daily updates, message only shows when put down
                    {
                        Game1.showGlobalMessage("No animals live here yet...");
                    }
                    return null;
                }

                int resourceId = GetMaxUnlockedRecipe(machineType, buildingLevel);
                return new StardewValley.Object(resourceId.ToString(), animalCount);
            }

            return null;
        }

        private static int GetMachineType(StardewValley.Object machine)
        {
            switch (machine.DisplayName)
            {
                case "Fertilizer Compost Bin":
                    return 1;
                case "Retaining Soil Compost Bin":
                    return 2;
                case "Speed-Gro Compost Bin":
                    return 3;
                default:
                    return 0;
            }
        }

        private static int GetBuildingLevel(Building building)
        {
            switch (building.buildingType.Value)
            {
                case "Barn":
                case "Coop":
                    return 1;
                case "Big Barn":
                case "Big Coop":
                    return 2;
                case "Deluxe Barn":
                case "Deluxe Coop":
                // add other mod support here:
                case "jenf1.megacoopbarn_MegaCoop":
                case "jenf1.megacoopbarn_MegaBarn":
                case "FlashShifter.StardewValleyExpandedCP_PremiumCoop":
                case "FlashShifter.StardewValleyExpandedCP_PremiumBarn":
                    return 3;
                default:
                    // Log used to determine Building name for adding more mod supported buildings
                    ModEntry.mon?.Log($"Modded Building ID: {building.buildingType.Value}", LogLevel.Info);
                    return 0;
            }
        }

        private static int GetAnimalCount(Building building)
        {
            if (building.indoors.Value is AnimalHouse animalHouse)
            {
                return animalHouse.animalsThatLiveHere.Count();
            }
            else
            {
                return 0;
            }
        }

        private static int GetMaxUnlockedRecipe(int machineType, int buildingLevel)
        {
            FarmerCollection farmers = Game1.getOnlineFarmers();

            List<string> unlockedMachineRecipes = new();

            Dictionary<int, Dictionary<string, Recipe>> modData = new Dictionary<int, Dictionary<string, Recipe>>
            {
                {
                    1, new Dictionary<string, Recipe>
                    {
                        { "Basic Fertilizer", new Recipe { UpgradeLevels = new List<int> { 1, 2, 3 }, RecipeID = 368, Unlocked = false } },
                        { "Quality Fertilizer", new Recipe { UpgradeLevels = new List<int> { 2, 3 }, RecipeID = 369, Unlocked = false } },
                        { "Deluxe Fertilizer", new Recipe { UpgradeLevels = new List<int> { 3 }, RecipeID = 919, Unlocked = false } }
                    }
                },
                {
                    2, new Dictionary<string, Recipe>
                    {
                        { "Basic Retaining Soil", new Recipe { UpgradeLevels = new List<int> { 1, 2, 3 }, RecipeID = 370, Unlocked = false } },
                        { "Quality Retaining Soil", new Recipe { UpgradeLevels = new List<int> { 2, 3 }, RecipeID = 371, Unlocked = false } },
                        { "Deluxe Retaining Soil", new Recipe { UpgradeLevels = new List<int> { 3 }, RecipeID = 920, Unlocked = false } }
                    }
                },
                {
                    3, new Dictionary<string, Recipe>
                    {
                        { "Speed-Gro", new Recipe { UpgradeLevels = new List<int> { 1, 2, 3 }, RecipeID = 465, Unlocked = false } },
                        { "Deluxe Speed-Gro", new Recipe { UpgradeLevels = new List<int> { 2, 3 }, RecipeID = 466, Unlocked = false } },
                        { "Hyper Speed-Gro", new Recipe { UpgradeLevels = new List<int> { 3 }, RecipeID = 918, Unlocked = false } }
                    }
                }
            };

            foreach (Farmer f in farmers)
            {
                foreach (var recipe in modData[machineType].Keys)
                {
                    if (modData[machineType][recipe].UpgradeLevels.Contains(buildingLevel))
                    {
                        if (f.knowsRecipe(recipe) && !unlockedMachineRecipes.Contains(recipe))
                        {
                            modData[machineType][recipe].Unlocked = true;
                            unlockedMachineRecipes.Add(recipe);
                        }
                    }
                }
            }

            if (unlockedMachineRecipes.Any())
            {
                string last = unlockedMachineRecipes.Last();
                ModEntry.mon?.Log($"Max output quality of this machine is: {last}", LogLevel.Info);
                return modData[machineType][last].RecipeID;
            }

            return 0;
        }

        private static int GetResourceIdForBuildingLevel(int machineType, int buildingLevel)
        {
            switch (machineType)
            {
                case 1:
                    return buildingLevel switch
                    {
                        1 => 368, // Basic Fertilizer
                        2 => 369, // Quality Fertilizer
                        3 => 919, // Deluxe Fertilizer
                        _ => 368
                    };
                case 2:
                    return buildingLevel switch
                    {
                        1 => 370,
                        2 => 371,
                        3 => 920,
                        _ => 370
                    };
                case 3:
                    return buildingLevel switch
                    {
                        1 => 465,
                        2 => 466,
                        3 => 918,
                        _ => 465
                    };
                default:
                    return 0;
            }
        }
    }
}
