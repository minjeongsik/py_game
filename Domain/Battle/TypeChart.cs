namespace PyGame.Domain.Battle;

public static class TypeChart
{
    public static float GetModifier(string attackingTypeId, string defendingTypeId)
    {
        return (attackingTypeId, defendingTypeId) switch
        {
            ("leaf", "wave") => 1.35f,
            ("wave", "flame") => 1.35f,
            ("flame", "leaf") => 1.35f,
            ("wave", "leaf") => 0.75f,
            ("flame", "wave") => 0.75f,
            ("leaf", "flame") => 0.75f,
            _ => 1.0f
        };
    }

    public static string Describe(float modifier)
    {
        if (modifier >= 1.2f)
        {
            return "IT HIT HARD!";
        }

        if (modifier <= 0.8f)
        {
            return "IT WAS NOT VERY EFFECTIVE.";
        }

        return "IT LANDED CLEANLY.";
    }
}
