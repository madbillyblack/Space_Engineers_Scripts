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
        const string MENU_HEAD = "Context Menu";
        const string PAGE_HEAD = "Menu Page ";
        const string BUTTON_BLOCK = "Block ";
        const string BUTTON_ACTION = "Action ";

        static Dictionary<int, Menu> _menus;

        // MENU //
        public class Menu
        {
            public int IDNumber;
            public Dictionary<int, MenuPage> Pages;

            public Menu(){}
        }


        // MENU PAGE //
        public class MenuPage
        {
            public int Number;
            public string Name;
            public Dictionary<int, MenuButton> Buttons;

            public MenuPage(int number)
            {
                Buttons = new Dictionary<int, MenuButton>();
                Number = number;
            }
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

            public MenuButton(int pageNumber, int buttonNumber)
            {
                Blocks = new List<IMyTerminalBlock>();
                Number = buttonNumber;  
            }
        }


        // ASSIGN MENUS //
        void AssignMenus()
        {
            _menus = new Dictionary<int, Menu>();

            //TODO
        }


        // INITIALIZE MENU //
        Menu InitializeMenu(IMyTerminalBlock menuBlock)
        {
            Menu menu = new Menu();
            int pageCount = ParseInt(GetKey(menuBlock, MENU_HEAD, "Page Count", "1"), 1);

            for(int i = 1; i <= pageCount; i++)
            {
                menu.Pages[i] = InitializeMenuPage(menuBlock, i);
            }

            return menu;
        }


        // INITIALIZE MENU PAGE //
        MenuPage InitializeMenuPage(IMyTerminalBlock menuBlock, int pageNumber)
        {
            MenuPage menuPage = new MenuPage(pageNumber);

            for(int i = 1; i < 8; i++)
            {
                menuPage.Buttons[i] = InitializeButton(menuBlock, pageNumber, i);
            }

            return menuPage;
        }


        // INITIALIZE BUTTON //
        MenuButton InitializeButton(IMyTerminalBlock menuBlock, int pageNumber, int buttonNumber)
        {
            MenuButton button = new MenuButton(pageNumber, buttonNumber);

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

            return button;
        }



        // GET PROGRAM BLOCK //
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
