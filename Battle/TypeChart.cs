namespace PyGame.Battle;

public static class TypeChart
{
    public static float GetMultiplier(string attackType, string defendType)
    {
        if (attackType == "Spark" && defendType == "Mist") return 1.25f;
        if (attackType == "Mist" && defendType == "Stone") return 1.25f;
        if (attackType == "Stone" && defendType == "Spark") return 1.25f;

        if (attackType == "Mist" && defendType == "Spark") return 0.85f;
        if (attackType == "Stone" && defendType == "Mist") return 0.85f;
        if (attackType == "Spark" && defendType == "Stone") return 0.85f;

        return 1f;
    }
}
