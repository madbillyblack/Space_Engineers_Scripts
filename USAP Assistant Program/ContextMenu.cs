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
        const string PAGE_KEY = "Current Page";
        const string PAGE_COUNT = "Page Count";
        const string ID_KEY = "Menu ID";
        const string PAGE_HEAD = "Menu Page ";
        const string BUTTON_BLOCK = "Block ";
        const string BUTTON_ACTION = "Action ";
        const string MENU_COLOR = "Menu Color Settings";
        const string BG_KEY = "Background Color";
        const string TITLE_KEY = "Title Color";
        const string BUTTON_KEY = "Button Color";
        const string LABEL_KEY = "Label Color";
        const int PAGE_LIMIT = 9;

        static Dictionary<int, Menu> _menus;

        // MENU //
        public class Menu
        {
            public int IDNumber;
            public int PageCount;
            public int CurrentPage;
            public IMyTerminalBlock Block;
            public IMyTextSurface Surface;
            public RectangleF Viewport;
            public string Alignment;
            public Dictionary<int, MenuPage> Pages;

            public Color BackgroundColor;
            public Color TitleColor;
            public Color LabelColor;
            public Color ButtonColor;

            public Menu(IMyTerminalBlock block)
            {
                Pages = new Dictionary<int, MenuPage>();

                // Default value if no ID recorded in INI
                int idKey = _menus.Count + 1;

                // Read Parameters from Custom Data
                Block = block;
                IDNumber = ParseInt(GetKey(block, MENU_HEAD, ID_KEY, idKey.ToString()), idKey);

                SetPageCount();
                SetCurrentPage();

                Alignment = GetKey(block, MENU_HEAD, "Alignment", "BOTTOM");

                // Set Menu Surface
                int screenIndex = ParseInt(GetKey(block, MENU_HEAD, "Screen Index", "0"), 0);
                Surface = SurfaceFromBlock(block as IMyTextSurfaceProvider, screenIndex);
                PrepareTextSurfaceForSprites(Surface);

                if (Surface != null)
                    Viewport = new RectangleF((Surface.TextureSize - Surface.SurfaceSize) / 2f, Surface.SurfaceSize);

                // Set to try if current ID is already in Dictionary
                bool updateID = false;

                while (_menus.ContainsKey(IDNumber))
                {
                    idKey++;
                    IDNumber = idKey;
                    updateID = true;
                }

                if (updateID)
                    SetKey(block, MENU_HEAD, ID_KEY, IDNumber.ToString());

                // Set currently available menu parameters
                BackgroundColor = ParseColor(GetKey(block, MENU_COLOR, BG_KEY, "0,0,0"));
                TitleColor = ParseColor(GetKey(block, MENU_COLOR, TITLE_KEY, "160,160,0"));
                LabelColor = ParseColor(GetKey(block, MENU_COLOR, LABEL_KEY, "160,160,160"));
                ButtonColor = ParseColor(GetKey(block, MENU_COLOR, BUTTON_KEY, "0,160,160"));
            }

            // SET PAGE COUNT //
            void SetPageCount()
            {
                PageCount = ParseInt(GetKey(Block, MENU_HEAD, PAGE_COUNT, "1"), 1);

                if (PageCount > PAGE_LIMIT)
                {
                    PageCount = PAGE_LIMIT;
                    SetKey(Block, MENU_HEAD, PAGE_COUNT, PAGE_LIMIT.ToString());
                }
            }

            // SET CURRENT PAGE //
            void SetCurrentPage()
            {
                CurrentPage = ParseInt(GetKey(Block, MENU_HEAD, PAGE_KEY, "1"), 1);
                

                if(CurrentPage > PageCount || CurrentPage < 1)
                {
                    CurrentPage = 1;
                    SetKey(Block, MENU_HEAD, PAGE_KEY, "1");
                }
            }
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

            List<IMyTerminalBlock> menuBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(MENU_TAG, menuBlocks);

            if (menuBlocks.Count < 1)
                return;

            foreach(IMyTerminalBlock menuBlock in menuBlocks)
            {
                if(SameGridID(menuBlock) && (menuBlock as IMyTextSurfaceProvider).SurfaceCount > 0)
                {
                    Menu menu = InitializeMenu(menuBlock);
                    _menus[menu.IDNumber] = menu;

                    DrawMenu(menu);
                }
            }
        }


        // INITIALIZE MENU //
        Menu InitializeMenu(IMyTerminalBlock menuBlock)
        {
            Menu menu = new Menu(menuBlock);

            for(int i = 1; i <= menu.PageCount; i++)
            {
                menu.Pages[i] = InitializeMenuPage(menuBlock, i);
            }

            return menu;
        }


        // INITIALIZE MENU PAGE //
        MenuPage InitializeMenuPage(IMyTerminalBlock menuBlock, int pageNumber)
        {
            MenuPage menuPage = new MenuPage(pageNumber);
            menuPage.Name = GetKey(menuBlock, PAGE_HEAD + pageNumber, "Title", "");

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


        // NEXT MENU PAGE //
        void NextMenuPage(string menuKey)
        {
            Menu menu = GetMenu(menuKey);

            if(menu == null)
            {
                _statusMessage = "NO MENU FOUND!";
                return;
            }

            menu.CurrentPage++;

            if (menu.CurrentPage > menu.PageCount)
                menu.CurrentPage = 1;

            SetKey(menu.Block, MENU_HEAD, PAGE_KEY, menu.CurrentPage.ToString());

            DrawMenu(menu);
        }


        // PREVIOUS MENU PAGE //
        void PreviousMenuPage(string menuKey)
        {
            Menu menu = GetMenu(menuKey);

            if (menu == null)
            {
                _statusMessage = "NO MENU FOUND!";
                return;
            }

            menu.CurrentPage--;

            if (menu.CurrentPage < 1)
                menu.CurrentPage = menu.PageCount;

            SetKey(menu.Block, MENU_HEAD, PAGE_KEY, menu.CurrentPage.ToString());

            DrawMenu(menu);
        }


        // GET MENU //
        Menu GetMenu(string menuKey)
        {
            if (_menus.Count < 1)
                return null;

            int key;

            if (menuKey == "0" || menuKey == "")
                key = 0;
            else
                key = ParseInt(menuKey, 0);

            if (key == 0)
                return GetFirstMenu();
            else if (!_menus.ContainsKey(key))
                return null;
            else
                return _menus[key];
        }


        // GET FIRST MENU //
        Menu GetFirstMenu()
        {
            int index = int.MaxValue;

            foreach (int key in _menus.Keys)
            {
                if (key < index)
                    index = key;
            }

            return _menus[index];
        }
    }
}
