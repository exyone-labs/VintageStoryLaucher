namespace VSL.Domain;

public static class WorldRuleCatalog
{
    // A curated V1 subset with typed controls. Unknown keys are still preserved in advanced JSON mode.
    public static IReadOnlyList<WorldRuleDefinition> DefaultRules { get; } =
    [
        new() { Key = "gameMode", LabelZh = "游戏模式", Type = WorldRuleType.Choice, Choices = ["survival", "creative"] },
        new() { Key = "playerlives", LabelZh = "玩家生命次数", Type = WorldRuleType.Text },
        new() { Key = "creatureHostility", LabelZh = "生物敌对性", Type = WorldRuleType.Choice, Choices = ["aggressive", "passive", "off"] },
        new() { Key = "temporalStorms", LabelZh = "时空风暴频率", Type = WorldRuleType.Choice, Choices = ["off", "veryrare", "rare", "sometimes", "often", "veryoften"] },
        new() { Key = "allowMap", LabelZh = "允许地图", Type = WorldRuleType.Boolean },
        new() { Key = "allowCoordinateHud", LabelZh = "允许坐标HUD", Type = WorldRuleType.Boolean },
        new() { Key = "allowLandClaiming", LabelZh = "允许领地声明", Type = WorldRuleType.Boolean },
        new() { Key = "surfaceCopperDeposits", LabelZh = "地表铜矿生成率", Type = WorldRuleType.Text },
        new() { Key = "surfaceTinDeposits", LabelZh = "地表锡矿生成率", Type = WorldRuleType.Text },
        new() { Key = "globalDepositSpawnRate", LabelZh = "矿物总体生成率", Type = WorldRuleType.Text },
        new() { Key = "daysPerMonth", LabelZh = "每月天数", Type = WorldRuleType.Number },
        new() { Key = "worldWidth", LabelZh = "世界宽度", Type = WorldRuleType.Number },
        new() { Key = "worldLength", LabelZh = "世界长度", Type = WorldRuleType.Number },
        new() { Key = "worldEdge", LabelZh = "世界边界类型", Type = WorldRuleType.Choice, Choices = ["blocked", "traversable"] },
        new() { Key = "snowAccum", LabelZh = "积雪堆积", Type = WorldRuleType.Boolean }
    ];
}
