namespace PyGame.GameFlow.States.Battle;

internal static class BattleText
{
    public static readonly string[] Options = ["기술", "가방", "교체", "필살기", "포획", "도주"];

    public static string TypeLabel(string typeId) => typeId switch
    {
        "leaf" => "풀",
        "flame" => "불꽃",
        "wave" => "물",
        _ => "무속성"
    };

    public static string EffectText(float modifier) => modifier switch
    {
        >= 1.2f => "효과가 굉장하다!",
        <= 0.8f => "효과가 별로인 듯하다.",
        _ => "깔끔하게 적중했다."
    };

    public static string SuperMoveName(string typeId) => typeId switch
    {
        "leaf" => "숲의 맹습",
        "flame" => "홍련 폭발",
        "wave" => "폭류 파동",
        _ => "전력 돌진"
    };
}
