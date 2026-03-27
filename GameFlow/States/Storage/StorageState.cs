using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PyGame.Domain.Creatures;
using PyGame.GameFlow.StateManager;

namespace PyGame.GameFlow.States.Storage;

public sealed class StorageState : IGameState
{
    private int _selectedParty;
    private int _selectedStorage;
    private bool _partyPaneSelected = true;
    private string _panelMessage = "왼쪽은 파티, 오른쪽은 보관함입니다.";

    public GameStateId Id => GameStateId.Storage;

    public void Update(GameTime gameTime, GameContext context)
    {
        _ = gameTime;

        var party = context.Session.Party;
        var storage = context.Session.Storage;

        if (context.Input.WasPressed(Keys.Left) || context.Input.WasPressed(Keys.A))
        {
            _partyPaneSelected = true;
        }

        if (context.Input.WasPressed(Keys.Right) || context.Input.WasPressed(Keys.D) || context.Input.WasPressed(Keys.Tab))
        {
            _partyPaneSelected = false;
        }

        if (_partyPaneSelected && party.Count > 0)
        {
            _selectedParty = Math.Clamp(_selectedParty, 0, party.Count - 1);

            if (context.Input.WasPressed(Keys.Up) || context.Input.WasPressed(Keys.W))
            {
                _selectedParty = (_selectedParty + party.Count - 1) % party.Count;
            }

            if (context.Input.WasPressed(Keys.Down) || context.Input.WasPressed(Keys.S))
            {
                _selectedParty = (_selectedParty + 1) % party.Count;
            }

            if (context.Input.WasPressed(Keys.Enter) || context.Input.WasPressed(Keys.Space))
            {
                DepositSelected(context);
                return;
            }
        }
        else if (!_partyPaneSelected && storage.Count > 0)
        {
            _selectedStorage = Math.Clamp(_selectedStorage, 0, storage.Count - 1);

            if (context.Input.WasPressed(Keys.Up) || context.Input.WasPressed(Keys.W))
            {
                _selectedStorage = (_selectedStorage + storage.Count - 1) % storage.Count;
            }

            if (context.Input.WasPressed(Keys.Down) || context.Input.WasPressed(Keys.S))
            {
                _selectedStorage = (_selectedStorage + 1) % storage.Count;
            }

            if (context.Input.WasPressed(Keys.Enter) || context.Input.WasPressed(Keys.Space))
            {
                WithdrawSelected(context);
                return;
            }
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
        var storage = context.Session.Storage;

        context.SpriteBatch.Begin();
        context.PrimitiveRenderer.Fill(new Rectangle(0, 0, context.Viewport.Width, context.Viewport.Height), new Color(174, 198, 214));
        context.PrimitiveRenderer.Fill(new Rectangle(24, 22, 912, 70), new Color(16, 24, 34, 232));
        context.PrimitiveRenderer.Outline(new Rectangle(24, 22, 912, 70), 3, new Color(196, 220, 232));
        context.TextRenderer.DrawText(new Vector2(54, 40), "PC 보관함", 4, new Color(238, 246, 252));
        context.TextRenderer.DrawText(new Vector2(650, 46), $"파티 {party.Count}  박스 {storage.Count}", 2, new Color(220, 228, 232));

        DrawPartyPane(context, party);
        DrawStoragePane(context, storage);
        DrawDetailPane(context, party, storage);

        context.PrimitiveRenderer.Fill(new Rectangle(24, 514, 912, 58), new Color(16, 24, 34, 232));
        context.PrimitiveRenderer.Outline(new Rectangle(24, 514, 912, 58), 2, new Color(196, 220, 232));
        context.TextRenderer.DrawText(new Vector2(44, 528), _panelMessage, 2, new Color(236, 236, 224));
        context.TextRenderer.DrawText(new Vector2(44, 550), "좌우 화면선택  위아래 선택  엔터 이동  ESC 돌아가기", 2, new Color(216, 226, 232));
        context.SpriteBatch.End();
    }

    private void DepositSelected(GameContext context)
    {
        var party = context.Session.Party;
        if (party.Count <= 1)
        {
            _panelMessage = "파티에는 최소 한 마리가 남아 있어야 합니다.";
            return;
        }

        var creature = party.RemoveAt(_selectedParty);
        context.Session.Storage.Add(creature);
        party.EnsureUsableLead();
        _selectedParty = Math.Clamp(_selectedParty, 0, Math.Max(0, party.Count - 1));
        _selectedStorage = context.Session.Storage.Count - 1;
        context.Session.StatusMessage = $"{creature.Nickname}을(를) 보관함으로 보냈습니다.";
        _panelMessage = $"{creature.Nickname}이(가) 1번 박스로 이동했습니다.";
    }

    private void WithdrawSelected(GameContext context)
    {
        var party = context.Session.Party;
        if (party.IsFull)
        {
            _panelMessage = "파티가 가득 찼습니다. 먼저 맡겨 주세요.";
            return;
        }

        var creature = context.Session.Storage.RemoveAt(_selectedStorage);
        party.Add(creature);
        _selectedStorage = Math.Clamp(_selectedStorage, 0, Math.Max(0, context.Session.Storage.Count - 1));
        context.Session.StatusMessage = $"{creature.Nickname}을(를) 꺼냈습니다.";
        _panelMessage = $"{creature.Nickname}이(가) 파티에 합류했습니다.";
    }

    private void DrawPartyPane(GameContext context, Domain.Party.Party party)
    {
        var rect = new Rectangle(24, 110, 360, 390);
        context.PrimitiveRenderer.Fill(rect, new Color(18, 26, 36, 236));
        context.PrimitiveRenderer.Outline(rect, 3, _partyPaneSelected ? new Color(250, 226, 132) : new Color(196, 220, 232));
        context.TextRenderer.DrawText(new Vector2(48, 132), "파티", 3, new Color(238, 246, 252));

        if (party.Count == 0)
        {
            context.TextRenderer.DrawText(new Vector2(72, 240), "파티 멤버가 없습니다", 2, new Color(220, 228, 232));
            return;
        }

        var y = 174;
        for (var i = 0; i < party.Members.Count; i++)
        {
            var creature = party.Members[i];
            var selected = _partyPaneSelected && i == _selectedParty;
            var rowRect = new Rectangle(44, y - 8, 320, 52);
            context.PrimitiveRenderer.Fill(rowRect, selected ? new Color(72, 98, 116) : new Color(28, 38, 52));
            context.PrimitiveRenderer.Outline(rowRect, 2, i == party.ActiveIndex ? new Color(250, 226, 132) : new Color(84, 96, 110));
            DrawMiniPortrait(context, new Rectangle(58, y, 34, 30), creature, selected);
            context.TextRenderer.DrawText(new Vector2(108, y + 2), creature.Nickname, 2, Color.White);
            context.TextRenderer.DrawText(new Vector2(108, y + 24), i == party.ActiveIndex ? "선두" : creature.IsFainted ? "휴식" : "준비", 2, creature.IsFainted ? new Color(236, 146, 132) : new Color(214, 224, 228));
            y += 62;
        }
    }

    private void DrawStoragePane(GameContext context, Domain.Party.CreatureStorage storage)
    {
        var rect = new Rectangle(400, 110, 536, 390);
        context.PrimitiveRenderer.Fill(rect, new Color(18, 26, 36, 236));
        context.PrimitiveRenderer.Outline(rect, 3, !_partyPaneSelected ? new Color(206, 236, 244) : new Color(196, 220, 232));
        context.TextRenderer.DrawText(new Vector2(424, 132), "1번 박스", 3, new Color(238, 246, 252));

        if (storage.Count == 0)
        {
            context.TextRenderer.DrawText(new Vector2(520, 240), "보관함이 비어 있습니다", 2, new Color(220, 228, 232));
            return;
        }

        var y = 174;
        for (var i = 0; i < storage.Count; i++)
        {
            var creature = storage.Creatures[i];
            var selected = !_partyPaneSelected && i == _selectedStorage;
            var rowRect = new Rectangle(420, y - 8, 240, 52);
            context.PrimitiveRenderer.Fill(rowRect, selected ? new Color(72, 98, 116) : new Color(28, 38, 52));
            context.PrimitiveRenderer.Outline(rowRect, 2, selected ? new Color(206, 236, 244) : new Color(84, 96, 110));
            DrawMiniPortrait(context, new Rectangle(434, y, 34, 30), creature, selected);
            context.TextRenderer.DrawText(new Vector2(484, y + 2), creature.Nickname, 2, Color.White);
            context.TextRenderer.DrawText(new Vector2(484, y + 24), $"Lv {creature.Level}", 2, new Color(214, 224, 228));
            y += 62;
        }
    }

    private void DrawDetailPane(GameContext context, Domain.Party.Party party, Domain.Party.CreatureStorage storage)
    {
        context.PrimitiveRenderer.Fill(new Rectangle(678, 174, 232, 282), new Color(24, 34, 44, 244));
        context.PrimitiveRenderer.Outline(new Rectangle(678, 174, 232, 282), 3, new Color(196, 220, 232));
        context.TextRenderer.DrawText(new Vector2(700, 192), "상세", 3, new Color(238, 246, 252));

        Creature? current = null;
        string actionHint = "크리처를 선택하세요.";
        if (_partyPaneSelected && party.Count > 0)
        {
            current = party.Members[_selectedParty];
            actionHint = party.Count <= 1 ? "마지막 파티 멤버는 맡길 수 없습니다" : "엔터로 보관함에 맡기기";
        }
        else if (!_partyPaneSelected && storage.Count > 0)
        {
            current = storage.Creatures[_selectedStorage];
            actionHint = party.IsFull ? "파티가 가득 찼습니다" : "엔터로 파티로 꺼내기";
        }

        if (current is null)
        {
            context.TextRenderer.DrawText(new Vector2(706, 264), "선택한 항목이 없습니다", 2, new Color(220, 228, 232));
            return;
        }

        DrawLargePortrait(context, new Rectangle(730, 228, 126, 96), current);
        context.TextRenderer.DrawText(new Vector2(706, 344), current.Nickname, 2, Color.White);
        context.TextRenderer.DrawText(new Vector2(706, 372), $"종족 {context.Definitions.Species[current.SpeciesId].Name}", 2, new Color(214, 224, 228));
        context.TextRenderer.DrawText(new Vector2(706, 400), $"레벨 {current.Level}", 2, new Color(214, 224, 228));
        context.TextRenderer.DrawText(new Vector2(706, 428), $"HP {current.CurrentHealth}/{current.MaxHealth}", 2, new Color(214, 224, 228));
        context.TextRenderer.DrawText(new Vector2(706, 456), actionHint, 2, new Color(236, 236, 224));
    }

    private static void DrawMiniPortrait(GameContext context, Rectangle rect, Creature creature, bool selected)
    {
        var palette = GetPalette(creature.SpeciesId);
        context.PrimitiveRenderer.Fill(rect, selected ? new Color(210, 226, 234) : new Color(184, 198, 204));
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 8, rect.Y + 2, 18, 12), palette.Primary);
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 4, rect.Y + 12, 24, 14), palette.Secondary);
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 10, rect.Y + 8, 10, 5), palette.Accent);
    }

    private static void DrawLargePortrait(GameContext context, Rectangle rect, Creature creature)
    {
        var palette = GetPalette(creature.SpeciesId);
        context.PrimitiveRenderer.Fill(rect, new Color(214, 226, 232));
        context.PrimitiveRenderer.Outline(rect, 3, new Color(106, 126, 136));
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 30, rect.Y + 14, 40, 30), palette.Primary);
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 22, rect.Y + 42, 62, 36), palette.Secondary);
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 38, rect.Y + 28, 14, 8), palette.Accent);
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 24, rect.Y + 50, 12, 12), palette.Accent);
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 68, rect.Y + 50, 12, 12), palette.Accent);
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
