using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        const string PAGE_HEAD = "Menu Page ";
        const string BUTTON_BLOCK = "Block ";
        const string BUTTON_ACTION = "Action ";


        // MENU //
        public class Menu
        {

        }


        // MENU PAGE //
        public class MenuPage
        {

        }


        // MENU BUTTON //
        public class MenuButton
        {
            public int Number;

            public List<IMyTerminalBlock> Blocks;
            public IMyProgrammableBlock ProgramBlock;
            public IMyTerminalBlock ToggleBlock;

            public bool IsProgramButton;
            public bool IsToggleButton;
            public bool Activated;

            public string TopLabel;
            public string CenterLabel;
            public string Action;

            public MenuButton(IMyTerminalBlock menuBlock, int pageNumber, int buttonNumber)
            {
                Blocks = new List<IMyTerminalBlock>();
                Number = buttonNumber;  
            }
        }


        // INITIALIZE BUTTON //
        void InitializeButton(IMyTerminalBlock menuBlock, int pageNumber, int buttonNumber)
        {
            MenuButton button = new MenuButton(menuBlock, pageNumber, buttonNumber);

            string blockString = GetKey(menuBlock, PAGE_HEAD + pageNumber, BUTTON_BLOCK + button.Number, "");
            string [] buttonData = blockString.Split(';');

            if (buttonData.Length > 1)
                button.TopLabel = buttonData[1];

            if(blockString.ToUpper().StartsWith("G:"))
            {
                string groupName = buttonData[0].Substring(2).Trim();

                IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(groupName);

                if (group == null)
                {
                    _statusMessage += "NO GROUP WITH NAME \"" + groupName + "\" FOUND!\n";
                }

                //Assign group blocks to button's block list.
                group.GetBlocks(button.Blocks);
            }
            else if(blockString.ToUpper().StartsWith("P:"))
            {
                string programName = buttonData[0].Substring(2).Trim();
                button.ProgramBlock = GetProgramBlock(programName);
            }
            else
            {
                string blockName = buttonData[0].Trim();
                GridTerminalSystem.SearchBlocksOfName(blockName, button.Blocks);
            }

            // Set Button Action
            string actionString = GetKey(menuBlock, PAGE_HEAD + pageNumber, BUTTON_ACTION + button.Number, "");
            string[] actionData = actionString.Split(';');

            if (actionData.Length > 1)
            {
                string entry = actionData[1].Trim();

                if(entry.StartsWith("T:") && !entry.EndsWith("T:"))
                {
                    IMyTerminalBlock toggleBlock = GridTerminalSystem.GetBlockWithName(entry.Substring(2));

                    if(SameGridID(toggleBlock))
                        button.ToggleBlock = toggleBlock;
                }
            }
        

        }


        IMyProgrammableBlock GetProgramBlock(string blockName)
        {
            List<IMyProgrammableBlock> blocks = new List<IMyProgrammableBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(blocks);

            if (blocks.Count < 1)
                return null;

            foreach(IMyProgrammableBlock block in blocks)
            {
                if (SameGridID(block) && block.CustomName == blockName)
                    return block;
            }

            return null;
        }
    }
}
