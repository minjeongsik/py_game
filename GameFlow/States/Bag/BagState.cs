using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PyGame.Domain.Inventory;
using PyGame.GameFlow.StateManager;

namespace PyGame.GameFlow.States.Bag;

public sealed class BagState : IGameState
{
    private int _selected;
    private string _message = "사용할 아이템을 고르세요.";

    public GameStateId Id => GameStateId.Bag;

    public void Update(GameTime gameTime, GameContext context)
    {
        _ = gameTime;

        var slots = context.Session.Inventory.Slots;
        if (slots.Count == 0)
        {
            if (context.Input.WasPressed(Keys.Escape))
            {
                context.StateManager.ChangeState(context.Session.ReturnState);
            }

            return;
        }

        _selected = Math.Clamp(_selected, 0, slots.Count - 1);

        if (context.Input.WasPressed(Keys.Up) || context.Input.WasPressed(Keys.W))
        {
            _selected = (_selected + slots.Count - 1) % slots.Count;
        }

        if (context.Input.WasPressed(Keys.Down) || context.Input.WasPressed(Keys.S))
        {
            _selected = (_selected + 1) % slots.Count;
        }

        if (context.Input.WasPressed(Keys.Enter) || context.Input.WasPressed(Keys.Space))
        {
            UseSelectedItem(context, slots[_selected]);
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

        var slots = context.Session.Inventory.Slots;
        context.SpriteBatch.Begin();
        context.PrimitiveRenderer.Fill(new Rectangle(0, 0, context.Viewport.Width, context.Viewport.Height), new Color(202, 216, 184));
        context.PrimitiveRenderer.Fill(new Rectangle(24, 22, 912, 70), new Color(16, 24, 34, 232));
        context.PrimitiveRenderer.Outline(new Rectangle(24, 22, 912, 70), 3, new Color(208, 184, 108));
        context.TextRenderer.DrawText(new Vector2(54, 40), "가방", 4, new Color(248, 238, 188));
        context.TextRenderer.DrawText(new Vector2(706, 46), $"소지금 {context.Session.Money}", 2, new Color(220, 228, 232));

        context.PrimitiveRenderer.Fill(new Rectangle(24, 110, 520, 390), new Color(18, 26, 36, 236));
        context.PrimitiveRenderer.Outline(new Rectangle(24, 110, 520, 390), 3, new Color(208, 184, 108));
        context.PrimitiveRenderer.Fill(new Rectangle(566, 110, 370, 390), new Color(18, 26, 36, 236));
        context.PrimitiveRenderer.Outline(new Rectangle(566, 110, 370, 390), 3, new Color(208, 184, 108));

        if (slots.Count == 0)
        {
            context.TextRenderer.DrawText(new Vector2(182, 240), "가방이 비어 있습니다", 2, new Color(220, 228, 232));
        }
        else
        {
            var y = 140;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var item = context.Definitions.Items[slot.ItemId];
                var selected = i == _selected;
                var rowRect = new Rectangle(44, y - 8, 480, 52);
                context.PrimitiveRenderer.Fill(rowRect, selected ? new Color(64, 86, 112) : new Color(28, 38, 52));
                context.PrimitiveRenderer.Outline(rowRect, 2, selected ? new Color(250, 226, 132) : new Color(84, 96, 110));
                context.TextRenderer.DrawText(new Vector2(68, y + 2), item.Name, 2, Color.White);
                context.TextRenderer.DrawText(new Vector2(68, y + 24), $"X{slot.Quantity}", 2, new Color(214, 224, 228));
                y += 62;
            }

            var currentItem = context.Definitions.Items[slots[_selected].ItemId];
            context.TextRenderer.DrawText(new Vector2(594, 136), "아이템 정보", 3, new Color(248, 238, 188));
            context.TextRenderer.DrawText(new Vector2(604, 192), currentItem.Name, 3, Color.White);
            context.TextRenderer.DrawText(new Vector2(604, 228), $"종류 {GetCategoryLabel(currentItem.Category)}", 2, new Color(214, 224, 228));
            context.TextRenderer.DrawText(new Vector2(604, 258), currentItem.HealAmount > 0 ? $"체력 {currentItem.HealAmount} 회복" : "포획용 도구", 2, new Color(214, 224, 228));
            context.TextRenderer.DrawText(new Vector2(604, 288), $"가격 {currentItem.Price}", 2, new Color(214, 224, 228));
            context.TextRenderer.DrawText(new Vector2(604, 436), "엔터로 현재 선두에게 사용", 2, new Color(236, 236, 224));
        }

        context.PrimitiveRenderer.Fill(new Rectangle(24, 514, 912, 58), new Color(16, 24, 34, 232));
        context.PrimitiveRenderer.Outline(new Rectangle(24, 514, 912, 58), 2, new Color(208, 184, 108));
        context.TextRenderer.DrawText(new Vector2(44, 528), _message, 2, new Color(236, 236, 224));
        context.TextRenderer.DrawText(new Vector2(44, 550), "위아래 선택  엔터 사용  ESC 돌아가기", 2, new Color(216, 226, 232));
        context.SpriteBatch.End();
    }

    private void UseSelectedItem(GameContext context, InventorySlot slot)
    {
        var item = context.Definitions.Items[slot.ItemId];
        if (item.HealAmount <= 0)
        {
            _message = "이 아이템은 전투에서만 사용할 수 있습니다.";
            return;
        }

        var target = context.Session.Party.ActiveCreature;
        if (target.CurrentHealth >= target.MaxHealth)
        {
            _message = $"{target.Nickname}은(는) 이미 체력이 가득합니다.";
            return;
        }

        if (!context.Session.Inventory.UseOne(item.Id))
        {
            _message = "해당 아이템이 없습니다.";
            return;
        }

        target.CurrentHealth = Math.Min(target.MaxHealth, target.CurrentHealth + item.HealAmount);
        _message = $"{target.Nickname}에게 {item.Name}을(를) 사용했습니다.";
        context.Session.StatusMessage = _message;
        context.Audio.PlayConfirm();
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
