using System.Collections.Generic;

namespace ItemRarity.Config;

public sealed class ToolInput
{
    public required string ItemCode { get; init; }
    public required int Quantity { get; init; }
}

public sealed class ToolkitTierConfig
{
    public required string TierName { get; init; }
    public required List<ToolInput> ToolInputs { get; init; }
    public required List<string> PossibleRarities { get; init; }
    public required Dictionary<string, float> RarityWeights { get; init; }
    public required string OutputCode { get; init; }
}
