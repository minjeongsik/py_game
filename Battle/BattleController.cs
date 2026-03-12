using System;
using System.Collections.Generic;

namespace Game.Battle
{
    public class BattleController
    {
        private BattleMenuMode _menuMode;

        public BattleMenuMode MenuMode => _menuMode;

        public void SetMenuMode(BattleMenuMode mode)
        {
            // Add any necessary logic when changing the menu mode here
            _menuMode = mode;
        }

        public void SwitchToActionMenu()
        {
            SetMenuMode(BattleMenuMode.Action);
        }

        public void SwitchToItemMenu()
        {
            SetMenuMode(BattleMenuMode.Item);
        }

        public void SwitchToSkillMenu()
        {
            SetMenuMode(BattleMenuMode.Skill);
        }

        public void SwitchToDefenseMenu()
        {
            SetMenuMode(BattleMenuMode.Defense);
        }
    }
}