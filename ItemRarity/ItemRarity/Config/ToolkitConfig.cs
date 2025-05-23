using System.Collections.Generic;
using Vintagestory.API.Common; // For GridRecipe, if we can use it directly or as a reference

namespace ItemRarity.Config
{
    public class ToolkitConfig
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public RecipeDefinition Recipe { get; set; }
        public List<string> RarityPool { get; set; } = new();
        public Dictionary<string, float> RarityWeights { get; set; } = new();
    }

    public class RecipeDefinition
    {
        public List<string> Pattern { get; set; } = new();
        public Dictionary<string, IngredientDefinition> Ingredients { get; set; } = new();
        public ItemOutputDefinition Output { get; set; }
    }

    public class IngredientDefinition
    {
        public string Type { get; set; } // "item" or "block"
        public string Code { get; set; } // e.g., "game:flint"
        public int Quantity { get; set; } = 1; // Added quantity
    }

    public class ItemOutputDefinition
    {
        public string Type { get; set; } // "item"
        public string Code { get; set; } // e.g., "yourmod:toolkit-stone"
        public int Quantity { get; set; } = 1;
    }
}
