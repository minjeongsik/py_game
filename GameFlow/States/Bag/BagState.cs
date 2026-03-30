using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PyGame.Domain.Inventory;
using PyGame.GameFlow.StateManager;
using PyGame.UI.Menus;

namespace PyGame.GameFlow.States.Bag;

public sealed class BagState : IGameState
{
    private static readonly Color BorderColor = new(208, 184, 108);
    private int _selected;
    private int _targetSelection;
    private bool _selectingTarget;
    private string _message = "사용할 아이템을 고르세요.";

    public GameStateId Id => GameStateId.Bag;

    public void Update(GameTime gameTime, GameContext context)
    {
        _ = gameTime;
        var slots = context.Session.Inventory.Slots;
        if (_selectingTarget)
        {
            UpdateTargetSelection(context);
            return;
        }

        if (slots.Count == 0)
        {
            if (context.Input.WasPressed(Keys.Escape))
            {
                context.StateManager.ChangeState(context.Session.ReturnState);
            }

            return;
        }

        _selected = Math.Clamp(_selected, 0, slots.Count - 1);
        if (context.Input.WasPressed(Keys.Up) || context.Input.WasPressed(Keys.W)) _selected = (_selected + slots.Count - 1) % slots.Count;
        if (context.Input.WasPressed(Keys.Down) || context.Input.WasPressed(Keys.S)) _selected = (_selected + 1) % slots.Count;

        if (context.Input.WasPressed(Keys.Enter) || context.Input.WasPressed(Keys.Space))
        {
            OpenUseFlow(context, slots[_selected]);
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
        var layout = new StateLayoutRenderer(context.TextRenderer, context.UiSkin);

        context.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        context.PrimitiveRenderer.Fill(new Rectangle(0, 0, context.Viewport.Width, context.Viewport.Height), new Color(202, 216, 184));
        layout.DrawHeader("가방", $"소지금 {context.Session.Money}", BorderColor);
        layout.DrawBodyPanels(new Rectangle(24, 110, 520, 390), new Rectangle(566, 110, 370, 390), BorderColor);
        DrawItemList(context, layout, slots);
        DrawDetailPanel(context, layout, slots);
        layout.DrawFooter(_message, _selectingTarget ? "위아래 이동  Enter 사용  ESC 뒤로" : "위아래 선택  Enter 사용  ESC 돌아가기", BorderColor);
        context.SpriteBatch.End();
    }

    private void OpenUseFlow(GameContext context, InventorySlot slot)
    {
        var item = context.Definitions.Items[slot.ItemId];
        if (item.HealAmount <= 0)
        {
            _message = "이 아이템은 필드에서 사용할 수 없습니다.";
            return;
        }

        _selectingTarget = true;
        _targetSelection = context.Session.Party.ActiveIndex;
        _message = $"{item.Name}을 사용할 대상을 고르세요.";
    }

    private void UpdateTargetSelection(GameContext context)
    {
        var party = context.Session.Party.Members;
        _targetSelection = Math.Clamp(_targetSelection, 0, Math.Max(0, party.Count - 1));
        if (context.Input.WasPressed(Keys.Up) || context.Input.WasPressed(Keys.W)) _targetSelection = (_targetSelection + party.Count - 1) % party.Count;
        if (context.Input.WasPressed(Keys.Down) || context.Input.WasPressed(Keys.S)) _targetSelection = (_targetSelection + 1) % party.Count;

        if (context.Input.WasPressed(Keys.Escape))
        {
            _selectingTarget = false;
            _message = "아이템 선택으로 돌아갑니다.";
            return;
        }

        if (!context.Input.WasPressed(Keys.Enter) && !context.Input.WasPressed(Keys.Space))
        {
            return;
        }

        UseSelectedItem(context, context.Session.Inventory.Slots[_selected], _targetSelection);
    }

    private void UseSelectedItem(GameContext context, InventorySlot slot, int targetIndex)
    {
        var item = context.Definitions.Items[slot.ItemId];
        var target = context.Session.Party.Members[targetIndex];
        if (target.CurrentHealth >= target.MaxHealth) { _message = $"{target.Nickname}의 체력은 이미 가득합니다."; return; }
        if (!context.Session.Inventory.UseOne(item.Id)) { _message = "해당 아이템이 없습니다."; return; }

        target.CurrentHealth = Math.Min(target.MaxHealth, target.CurrentHealth + item.HealAmount);
        _selectingTarget = false;
        _message = $"{target.Nickname}에게 {item.Name}을 사용했습니다.";
        context.Session.StatusMessage = _message;
        context.Audio.PlayConfirm();
    }

    private void DrawItemList(GameContext context, StateLayoutRenderer layout, IReadOnlyList<InventorySlot> slots)
    {
        if (slots.Count == 0)
        {
            context.TextRenderer.DrawText(new Vector2(182, 240), "가방이 비어 있습니다.", 2, new Color(220, 228, 232));
            return;
        }

        var y = 140;
        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var item = context.Definitions.Items[slot.ItemId];
            layout.DrawSelectableRow(new Rectangle(44, y - 8, 480, 52), i == _selected && !_selectingTarget, new Color(64, 86, 112), new Color(28, 38, 52), new Color(250, 226, 132), new Color(84, 96, 110));
            context.TextRenderer.DrawText(new Vector2(68, y + 2), item.Name, 2, Color.White);
            context.TextRenderer.DrawText(new Vector2(68, y + 24), $"x{slot.Quantity}", 2, new Color(214, 224, 228));
            y += 62;
        }
    }

    private void DrawDetailPanel(GameContext context, StateLayoutRenderer layout, IReadOnlyList<InventorySlot> slots)
    {
        if (slots.Count == 0)
        {
            return;
        }

        var currentItem = context.Definitions.Items[slots[_selected].ItemId];
        context.TextRenderer.DrawText(new Vector2(594, 136), _selectingTarget ? "대상 선택" : "아이템 정보", 3, new Color(248, 238, 188));
        if (_selectingTarget)
        {
            var y = 184;
            for (var i = 0; i < context.Session.Party.Members.Count; i++)
            {
                var creature = context.Session.Party.Members[i];
                layout.DrawSelectableRow(new Rectangle(586, y - 8, 330, 46), i == _targetSelection, new Color(64, 86, 112), new Color(28, 38, 52), new Color(250, 226, 132), new Color(84, 96, 110));
                context.TextRenderer.DrawText(new Vector2(604, y), creature.Nickname, 2, Color.White);
                context.TextRenderer.DrawText(new Vector2(604, y + 20), $"Lv {creature.Level}  HP {creature.CurrentHealth}/{creature.MaxHealth}", 1, new Color(214, 224, 228));
                y += 54;
            }

            return;
        }

        context.TextRenderer.DrawText(new Vector2(604, 192), currentItem.Name, 3, Color.White);
        context.TextRenderer.DrawText(new Vector2(604, 228), $"종류 {GetCategoryLabel(currentItem.Category)}", 2, new Color(214, 224, 228));
        context.TextRenderer.DrawText(new Vector2(604, 258), currentItem.HealAmount > 0 ? $"체력 {currentItem.HealAmount} 회복" : "필드에서는 사용할 수 없음", 2, new Color(214, 224, 228));
        context.TextRenderer.DrawText(new Vector2(604, 288), $"가격 {currentItem.Price}", 2, new Color(214, 224, 228));
        context.TextRenderer.DrawText(new Vector2(604, 436), "Enter로 사용하고, 회복 아이템은 대상을 고를 수 있습니다.", 2, new Color(236, 236, 224));
    }

    private static string GetCategoryLabel(string category) => category switch
    {
        "healing" => "회복",
        "capture" => "포획",
        _ => "도구"
    };
}
