using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PyGame.Domain.Creatures;
using PyGame.GameFlow.StateManager;

namespace PyGame.GameFlow.States.Party;

public sealed class PartyState : IGameState
{
    private int _selected;
    private string _panelMessage = "크리처를 선택하세요. L로 선두를 바꿉니다.";

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

        if (context.Input.WasPressed(Keys.Up) || context.Input.WasPressed(Keys.W))
        {
            _selected = (_selected + party.Count - 1) % party.Count;
        }

        if (context.Input.WasPressed(Keys.Down) || context.Input.WasPressed(Keys.S))
        {
            _selected = (_selected + 1) % party.Count;
        }

        if (context.Input.WasPressed(Keys.L))
        {
            if (party.SetLead(_selected))
            {
                _selected = 0;
                var lead = party.ActiveCreature;
                _panelMessage = lead.IsFainted
                    ? $"{lead.Nickname}이(가) 선두지만 전투에서는 다른 준비된 크리처가 나섭니다."
                    : $"{lead.Nickname}이(가) 이제 선두 크리처입니다.";
                context.Session.StatusMessage = $"선두를 {lead.Nickname}(으)로 변경했습니다.";
            }

            return;
        }

        if (context.Input.WasPressed(Keys.Tab) || context.Input.WasPressed(Keys.R))
        {
            context.Session.ReturnState = GameStateId.Party;
            context.StateManager.ChangeState(GameStateId.Storage);
            return;
        }

        if (context.Input.WasPressed(Keys.Enter) || context.Input.WasPressed(Keys.Space))
        {
            var creature = party.Members[_selected];
            _panelMessage = creature.IsFainted
                ? $"{creature.Nickname}은(는) 회복해야 선두로 설 수 있습니다."
                : $"{creature.Nickname}은(는) 필드와 전투 준비가 됐습니다.";
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

        context.SpriteBatch.Begin();
        context.PrimitiveRenderer.Fill(new Rectangle(0, 0, context.Viewport.Width, context.Viewport.Height), new Color(196, 214, 176));
        context.PrimitiveRenderer.Fill(new Rectangle(24, 22, 912, 70), new Color(16, 24, 34, 232));
        context.PrimitiveRenderer.Outline(new Rectangle(24, 22, 912, 70), 3, new Color(208, 184, 108));
        context.TextRenderer.DrawText(new Vector2(54, 40), "파티", 4, new Color(248, 238, 188));
        context.TextRenderer.DrawText(new Vector2(640, 46), $"선두 {party.ActiveCreature.Nickname}", 2, new Color(220, 228, 232));

        context.PrimitiveRenderer.Fill(new Rectangle(24, 110, 520, 390), new Color(18, 26, 36, 236));
        context.PrimitiveRenderer.Outline(new Rectangle(24, 110, 520, 390), 3, new Color(208, 184, 108));
        context.PrimitiveRenderer.Fill(new Rectangle(566, 110, 370, 390), new Color(18, 26, 36, 236));
        context.PrimitiveRenderer.Outline(new Rectangle(566, 110, 370, 390), 3, new Color(208, 184, 108));

        var y = 134;
        for (var i = 0; i < party.Members.Count; i++)
        {
            var creature = party.Members[i];
            var selected = i == _selected;
            var active = i == party.ActiveIndex;
            var rowRect = new Rectangle(44, y - 8, 480, 52);
            var fill = selected ? new Color(64, 86, 112) : new Color(28, 38, 52);
            var border = active ? new Color(250, 226, 132) : new Color(84, 96, 110);
            context.PrimitiveRenderer.Fill(rowRect, fill);
            context.PrimitiveRenderer.Outline(rowRect, 2, border);
            DrawMiniPortrait(context, new Rectangle(58, y, 34, 30), creature, selected);

            var marker = active ? "선두" : creature.IsFainted ? "휴식" : "준비";
            var markerColor = active ? new Color(248, 226, 132) : creature.IsFainted ? new Color(236, 146, 132) : new Color(214, 224, 228);
            context.TextRenderer.DrawText(new Vector2(108, y + 2), $"{i + 1} {creature.Nickname}", 2, Color.White);
            context.TextRenderer.DrawText(new Vector2(108, y + 24), $"Lv {creature.Level} HP {creature.CurrentHealth}/{creature.MaxHealth}", 2, new Color(214, 224, 228));
            context.TextRenderer.DrawText(new Vector2(404, y + 12), marker, 2, markerColor);
            y += 62;
        }

        var selectedCreature = party.Members[_selected];
        context.TextRenderer.DrawText(new Vector2(598, 136), "정보", 3, new Color(248, 238, 188));
        DrawLargePortrait(context, new Rectangle(674, 188, 150, 114), selectedCreature);
        context.TextRenderer.DrawText(new Vector2(604, 328), selectedCreature.Nickname, 3, Color.White);
        context.TextRenderer.DrawText(new Vector2(604, 366), $"종족 {context.Definitions.Species[selectedCreature.SpeciesId].Name}", 2, new Color(214, 224, 228));
        context.TextRenderer.DrawText(new Vector2(604, 396), $"레벨 {selectedCreature.Level}", 2, new Color(214, 224, 228));
        context.TextRenderer.DrawText(new Vector2(604, 426), $"체력 {selectedCreature.CurrentHealth}/{selectedCreature.MaxHealth}", 2, new Color(214, 224, 228));
        context.TextRenderer.DrawText(new Vector2(604, 456), _selected == party.ActiveIndex ? "현재 선두 크리처" : "L을 눌러 선두로 지정", 2, new Color(236, 236, 224));

        context.PrimitiveRenderer.Fill(new Rectangle(24, 514, 912, 58), new Color(16, 24, 34, 232));
        context.PrimitiveRenderer.Outline(new Rectangle(24, 514, 912, 58), 2, new Color(208, 184, 108));
        context.TextRenderer.DrawText(new Vector2(44, 528), _panelMessage, 2, new Color(236, 236, 224));
        context.TextRenderer.DrawText(new Vector2(44, 550), "위아래 선택  L 선두변경  R 보관함  ESC 돌아가기", 2, new Color(216, 226, 232));
        context.SpriteBatch.End();
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
