using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PyGame.Domain.Creatures;
using PyGame.GameFlow.StateManager;
using PyGame.UI.Menus;

namespace PyGame.GameFlow.States.Party;

public sealed class PartyState : IGameState
{
    private static readonly Color BorderColor = new(208, 184, 108);
    private int _selected;
    private string _panelMessage = "몬스터를 고르세요. Enter로 선두를 바꿀 수 있습니다.";

    public GameStateId Id => GameStateId.Party;

    public void Update(GameTime gameTime, GameContext context)
    {
        _ = gameTime;
        var party = context.Session.Party;
        if (party.Count == 0)
        {
            if (context.Input.WasPressed(Keys.Escape))
            {
                context.StateManager.ChangeState(context.Session.ReturnState);
            }

            return;
        }

        _selected = Math.Clamp(_selected, 0, party.Count - 1);
        if (context.Input.WasRepeated(Keys.Up) || context.Input.WasRepeated(Keys.W)) _selected = (_selected + party.Count - 1) % party.Count;
        if (context.Input.WasRepeated(Keys.Down) || context.Input.WasRepeated(Keys.S)) _selected = (_selected + 1) % party.Count;

        if (context.Input.WasPressed(Keys.Enter) || context.Input.WasPressed(Keys.Space) || context.Input.WasPressed(Keys.L))
        {
            if (party.SetLead(_selected))
            {
                _selected = 0;
                var lead = party.ActiveCreature;
                _panelMessage = lead.IsFainted
                    ? $"{lead.Nickname}은(는) 선두지만 전투에서는 다른 동료가 먼저 나갑니다."
                    : $"{lead.Nickname}을(를) 선두 몬스터로 바꿨습니다.";
                context.Session.StatusMessage = $"{lead.Nickname}이(가) 새로운 선두입니다.";
            }

            return;
        }

        if (context.Input.WasPressed(Keys.Tab) || context.Input.WasPressed(Keys.R))
        {
            context.Session.ReturnState = GameStateId.Party;
            context.StateManager.ChangeState(GameStateId.Storage);
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
        var party = context.Session.Party;
        var layout = new StateLayoutRenderer(context.TextRenderer, context.UiSkin);

        context.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        context.PrimitiveRenderer.Fill(new Rectangle(0, 0, context.Viewport.Width, context.Viewport.Height), new Color(196, 214, 176));
        layout.DrawHeader("파티", $"선두 {party.ActiveCreature.Nickname}", BorderColor);
        layout.DrawBodyPanels(new Rectangle(24, 110, 520, 390), new Rectangle(566, 110, 370, 390), BorderColor);
        DrawPartyList(context, layout, party);
        DrawSelectedInfo(context, party.Members[_selected], party.ActiveIndex);
        layout.DrawFooter(_panelMessage, "위아래 선택  Enter 선두 변경  L 대체 입력  R 보관함  ESC 뒤로", BorderColor);
        context.SpriteBatch.End();
    }

    private void DrawPartyList(GameContext context, StateLayoutRenderer layout, Domain.Party.Party party)
    {
        var y = 134;
        for (var i = 0; i < party.Members.Count; i++)
        {
            var creature = party.Members[i];
            var active = i == party.ActiveIndex;
            layout.DrawSelectableRow(
                new Rectangle(44, y - 8, 480, 52),
                i == _selected,
                new Color(64, 86, 112),
                new Color(28, 38, 52),
                active ? new Color(250, 226, 132) : new Color(84, 96, 110),
                new Color(84, 96, 110));
            DrawMiniPortrait(context, new Rectangle(58, y, 34, 30), creature, i == _selected);
            context.TextRenderer.DrawText(new Vector2(108, y + 2), $"{i + 1} {creature.Nickname}", 2, Color.White);
            context.TextRenderer.DrawText(new Vector2(108, y + 24), $"Lv {creature.Level} HP {creature.CurrentHealth}/{creature.MaxHealth}", 2, new Color(214, 224, 228));
            context.TextRenderer.DrawText(
                new Vector2(404, y + 12),
                active ? "선두" : creature.IsFainted ? "기절" : "준비",
                2,
                active ? new Color(248, 226, 132) : creature.IsFainted ? new Color(236, 146, 132) : new Color(214, 224, 228));
            y += 62;
        }
    }

    private void DrawSelectedInfo(GameContext context, Creature selectedCreature, int activeIndex)
    {
        context.TextRenderer.DrawText(new Vector2(598, 136), "정보", 3, new Color(248, 238, 188));
        DrawLargePortrait(context, new Rectangle(674, 188, 150, 114), selectedCreature);
        context.TextRenderer.DrawText(new Vector2(604, 328), selectedCreature.Nickname, 3, Color.White);
        context.TextRenderer.DrawText(new Vector2(604, 366), $"종족 {context.Definitions.Species[selectedCreature.SpeciesId].Name}", 2, new Color(214, 224, 228));
        context.TextRenderer.DrawText(new Vector2(604, 396), $"레벨 {selectedCreature.Level}", 2, new Color(214, 224, 228));
        context.TextRenderer.DrawText(new Vector2(604, 426), $"체력 {selectedCreature.CurrentHealth}/{selectedCreature.MaxHealth}", 2, new Color(214, 224, 228));
        context.TextRenderer.DrawText(new Vector2(604, 456), "EXP", 2, new Color(214, 224, 228));
        context.UiSkin.DrawExperienceBar(context.SpriteBatch, new Rectangle(660, 460, 192, 12), GetExpRatio(selectedCreature));
        context.TextRenderer.DrawText(new Vector2(604, 480), $"다음 레벨까지 {GetExpRemaining(selectedCreature)}", 1, new Color(214, 224, 228));
        context.TextRenderer.DrawText(new Vector2(604, 504), _selected == activeIndex ? "현재 선두 몬스터" : "Enter로 선두 지정", 2, new Color(236, 236, 224));
    }

    private static float GetExpRatio(Creature creature)
    {
        var next = Creature.GetExperienceForNextLevel(creature.Level);
        return next <= 0 ? 0f : (float)creature.Experience / next;
    }

    private static int GetExpRemaining(Creature creature)
    {
        return Math.Max(0, Creature.GetExperienceForNextLevel(creature.Level) - creature.Experience);
    }

    private static void DrawMiniPortrait(GameContext context, Rectangle rect, Creature creature, bool selected)
    {
        var palette = GetPalette(creature.SpeciesId);
        context.PrimitiveRenderer.Fill(rect, selected ? new Color(214, 222, 188) : new Color(186, 196, 168));
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 8, rect.Y + 2, 18, 12), palette.Primary);
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 4, rect.Y + 12, 24, 14), palette.Secondary);
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 10, rect.Y + 8, 10, 5), palette.Accent);
    }

    private static void DrawLargePortrait(GameContext context, Rectangle rect, Creature creature)
    {
        var palette = GetPalette(creature.SpeciesId);
        context.PrimitiveRenderer.Fill(rect, new Color(216, 226, 188));
        context.PrimitiveRenderer.Outline(rect, 3, new Color(114, 126, 96));
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 34, rect.Y + 16, 46, 36), palette.Primary);
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 24, rect.Y + 48, 72, 42), palette.Secondary);
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 42, rect.Y + 34, 18, 10), palette.Accent);
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 26, rect.Y + 56, 14, 14), palette.Accent);
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 74, rect.Y + 56, 14, 14), palette.Accent);
    }

    private static PortraitPalette GetPalette(string speciesId)
    {
        return speciesId switch
        {
            "sproutle" => new PortraitPalette(new Color(86, 154, 82), new Color(142, 204, 110), new Color(232, 238, 138)),
            _ => new PortraitPalette(new Color(182, 106, 70), new Color(228, 166, 96), new Color(244, 222, 154))
        };
    }

    private readonly record struct PortraitPalette(Color Primary, Color Secondary, Color Accent);
}
