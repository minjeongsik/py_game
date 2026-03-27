using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PyGame.GameFlow.StateManager;

namespace PyGame.GameFlow.States.Shop;

public sealed class ShopState : IGameState
{
    private static readonly string[] FallbackShopItemIds = ["potion", "capture-sphere"];
    private int _selected;
    private string _message = "길에 필요한 물건을 사세요.";

    public GameStateId Id => GameStateId.Shop;

    public void Update(GameTime gameTime, GameContext context)
    {
        _ = gameTime;

        var shopItemIds = GetShopItemIds(context);
        if (shopItemIds.Count == 0)
        {
            if (context.Input.WasPressed(Keys.Escape))
            {
                context.StateManager.ChangeState(context.Session.ReturnState);
            }

            return;
        }

        _selected = Math.Clamp(_selected, 0, shopItemIds.Count - 1);

        if (context.Input.WasPressed(Keys.Up) || context.Input.WasPressed(Keys.W))
        {
            _selected = (_selected + shopItemIds.Count - 1) % shopItemIds.Count;
        }

        if (context.Input.WasPressed(Keys.Down) || context.Input.WasPressed(Keys.S))
        {
            _selected = (_selected + 1) % shopItemIds.Count;
        }

        if (context.Input.WasPressed(Keys.Enter) || context.Input.WasPressed(Keys.Space))
        {
            var item = context.Definitions.Items[shopItemIds[_selected]];
            if (context.Session.Money < item.Price)
            {
                _message = "돈이 부족합니다.";
                return;
            }

            context.Session.Money -= item.Price;
            context.Session.Inventory.Add(item.Id, 1);
            context.Session.StatusMessage = $"{item.Name}을(를) 구입했습니다";
            _message = $"{item.Name}을(를) {item.Price}원에 샀습니다.";
            context.Audio.PlayConfirm();
            return;
        }

        if (context.Input.WasPressed(Keys.Escape))
        {
            context.StateManager.ChangeState(context.Session.ReturnState);
        }
    }

    public void Draw(GameTime gameTime, GameContext context)
    {
        _ = gameTime;
        var shopItemIds = GetShopItemIds(context);

        context.SpriteBatch.Begin();
        context.PrimitiveRenderer.Fill(new Rectangle(0, 0, context.Viewport.Width, context.Viewport.Height), new Color(216, 204, 178));
        context.PrimitiveRenderer.Fill(new Rectangle(24, 22, 912, 70), new Color(16, 24, 34, 232));
        context.PrimitiveRenderer.Outline(new Rectangle(24, 22, 912, 70), 3, new Color(214, 188, 108));
        context.TextRenderer.DrawText(new Vector2(54, 40), "상점", 4, new Color(248, 238, 188));
        context.TextRenderer.DrawText(new Vector2(706, 46), $"소지금 {context.Session.Money}", 2, new Color(220, 228, 232));

        context.PrimitiveRenderer.Fill(new Rectangle(24, 110, 520, 390), new Color(18, 26, 36, 236));
        context.PrimitiveRenderer.Outline(new Rectangle(24, 110, 520, 390), 3, new Color(214, 188, 108));
        context.PrimitiveRenderer.Fill(new Rectangle(566, 110, 370, 390), new Color(18, 26, 36, 236));
        context.PrimitiveRenderer.Outline(new Rectangle(566, 110, 370, 390), 3, new Color(214, 188, 108));

        var y = 140;
        for (var i = 0; i < shopItemIds.Count; i++)
        {
            var item = context.Definitions.Items[shopItemIds[i]];
            var selected = i == _selected;
            var rowRect = new Rectangle(44, y - 8, 480, 52);
            context.PrimitiveRenderer.Fill(rowRect, selected ? new Color(88, 84, 62) : new Color(44, 38, 28));
            context.PrimitiveRenderer.Outline(rowRect, 2, selected ? new Color(250, 226, 132) : new Color(118, 108, 82));
            context.TextRenderer.DrawText(new Vector2(68, y + 2), item.Name, 2, Color.White);
            context.TextRenderer.DrawText(new Vector2(68, y + 24), $"가격 {item.Price}", 2, new Color(214, 224, 228));
            y += 62;
        }

        if (shopItemIds.Count > 0)
        {
            var current = context.Definitions.Items[shopItemIds[_selected]];
            context.TextRenderer.DrawText(new Vector2(594, 136), "상점 정보", 3, new Color(248, 238, 188));
            context.TextRenderer.DrawText(new Vector2(604, 192), current.Name, 3, Color.White);
            context.TextRenderer.DrawText(new Vector2(604, 228), $"종류 {GetCategoryLabel(current.Category)}", 2, new Color(214, 224, 228));
            context.TextRenderer.DrawText(new Vector2(604, 258), current.HealAmount > 0 ? $"체력 {current.HealAmount} 회복" : "포획용 도구", 2, new Color(214, 224, 228));
            context.TextRenderer.DrawText(new Vector2(604, 288), $"보유 {context.Session.Inventory.GetQuantity(current.Id)}", 2, new Color(214, 224, 228));
            context.TextRenderer.DrawText(new Vector2(604, 436), "엔터로 1개 구입", 2, new Color(236, 236, 224));
        }

        context.PrimitiveRenderer.Fill(new Rectangle(24, 514, 912, 58), new Color(16, 24, 34, 232));
        context.PrimitiveRenderer.Outline(new Rectangle(24, 514, 912, 58), 2, new Color(214, 188, 108));
        context.TextRenderer.DrawText(new Vector2(44, 528), _message, 2, new Color(236, 236, 224));
        context.TextRenderer.DrawText(new Vector2(44, 550), "위아래 선택  엔터 구입  ESC 돌아가기", 2, new Color(216, 226, 232));
        context.SpriteBatch.End();
    }

    private static IReadOnlyList<string> GetShopItemIds(GameContext context)
    {
        return context.Session.CurrentShopItemIds.Count > 0 ? context.Session.CurrentShopItemIds : FallbackShopItemIds;
    }

    private static string GetCategoryLabel(string category)
    {
        return category switch
        {
            "healing" => "회복",
            "capture" => "포획",
            _ => "도구"
        };
    }
}
