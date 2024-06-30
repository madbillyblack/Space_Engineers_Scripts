using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Eventing.Reader;
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
using static IngameScript.Program;

namespace IngameScript
{
    partial class Program
    {
        const string MENU_HEAD = "Virtual Hotbar";
        const string PAGE_KEY = "Current Page";
        const string PAGE_COUNT = "Page Count";
        const string ID_KEY = "Menu ID";
        const string PAGE_HEAD = "HOTBAR PAGE ";
        const string BUTTON_BLOCK = "Block";
        const string BLOCK_LABEL = "Block Label";
        const string BUTTON_ACTION = "Action";
        const string MAX_BUTTONS = "Max Button Count";
        const string ACTION_LABEL = "Action Label";
        const string TOGGLE_KEY = "Toggle Block";
        const string MENU_COLOR = "Menu Color Settings";
        const string BG_KEY = "Background Color";
        const string TITLE_KEY = "Title Color";
        const string BUTTON_KEY = "Button Color";
        const string LABEL_KEY = "Label Color";
        const string PLACE_HOLDER = "{AUX}";
        const string BLINK_LENGTH = "Blink Length";

        const int PAGE_LIMIT = 9;
        const int CHAR_LIMIT = 7;
        const int BUTTON_LIMIT = 9;
        const int ILLUMINATION_TIME = 3;

        const float THRESHHOLD = 0.95f;
        const float DV_BLINK = 0.5f; // Default length of single blink cycle (in sec)
        

        static Dictionary<int, Menu> _menus;
        //static bool _buttonsLit = false;
        static string _nextCommand;
        //static bool _menusAssigned = false;

        int _currentMenuKey; // Key for current menu to be drawn


        // MENU //
        public class Menu
        {
            public int IDNumber;
            public int PageCount;
            public int CurrentPage;
            public int MaxButtons;
            public IMyTerminalBlock Block;
            public IMyTextSurface Surface;
            public RectangleF Viewport;
            public string Alignment;
            //public string Decals;
            public Dictionary<int, MenuPage> Pages;

            public Color BackgroundColor;
            public Color TitleColor;
            public Color LabelColor;
            public Color ButtonColor;

            public int BlinkCycle;
            MyIni Ini;

            public Menu(IMyTerminalBlock block)
            {
                //_menusAssigned = true;
                Pages = new Dictionary<int, MenuPage>();

                // Default value if no ID recorded in INI
                int idKey = _menus.Count + 1;

                // Read Parameters from Custom Data
                Block = block;
                Ini = GetIni(block);
                IDNumber = ParseInt(GetKey(MENU_HEAD, ID_KEY, idKey.ToString()), idKey);

                // Set Menu Surface
                int screenIndex = ParseInt(GetKey(MENU_HEAD, "Screen Index", "0"), 0);
                Surface = SurfaceFromBlock(block as IMyTextSurfaceProvider, screenIndex);
                PrepareTextSurfaceForSprites(Surface);

                SetPageCount();
                SetCurrentPage();
                SetButtonLimit(ParseInt(GetKey(MENU_HEAD, MAX_BUTTONS, "8"), 8));

                // Vertical Alignment
                Alignment = GetKey(MENU_HEAD, "Alignment", "BOTTOM");

                // Decals
                //Decals = GetKey(MENU_HEAD, "Decals", "").ToUpper();

                // Blink Cycle
                float cycleLength = ParseFloat(GetKey(MENU_HEAD, "Blink Cycle", DV_BLINK.ToString()), DV_BLINK);
                BlinkCycle = (int)(cycleLength * 6);

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
                    SetKey(MENU_HEAD, ID_KEY, IDNumber.ToString());

                // Set currently available menu parameters
                BackgroundColor = ParseColor(GetKey(MENU_COLOR, BG_KEY, "0,0,0"));
                TitleColor = ParseColor(GetKey(MENU_COLOR, TITLE_KEY, "160,160,0"));
                LabelColor = ParseColor(GetKey(MENU_COLOR, LABEL_KEY, "160,160,160"));
                ButtonColor = ParseColor(GetKey(MENU_COLOR, BUTTON_KEY, "0,160,160"));
            }


            // SET BUTTON LIMIT //
            public void SetButtonLimit(int buttonLimit)
            {
                MaxButtons = buttonLimit;

                // Ensure a valid MaxButton Value was supplied.
                if (MaxButtons < 1)
                    MaxButtons = 1;
                else if (MaxButtons > BUTTON_LIMIT)
                    MaxButtons = BUTTON_LIMIT;
            }


            // GET CURRENT PAGE //
            public MenuPage GetCurrentPage()
            {
                return Pages[CurrentPage];
            }

            // SET PAGE COUNT //
            void SetPageCount()
            {
                PageCount = ParseInt(GetKey(MENU_HEAD, PAGE_COUNT, "1"), 1);

                if (PageCount > PAGE_LIMIT)
                {
                    PageCount = PAGE_LIMIT;
                    SetKey(MENU_HEAD, PAGE_COUNT, PAGE_LIMIT.ToString());
                }
            }

            // SET CURRENT PAGE //
            void SetCurrentPage()
            {
                CurrentPage = ParseInt(GetKey(MENU_HEAD, PAGE_KEY, "1"), 1);
                

                if(CurrentPage > PageCount || CurrentPage < 1)
                {
                    CurrentPage = 1;
                    SetKey(MENU_HEAD, PAGE_KEY, "1");
                }
            }

            //ENSURE KEY
            void EnsureKey(string header, string key, string defaultVal)
            {
                if (!Ini.ContainsKey(header, key))
                    SetKey(header, key, defaultVal);
            }

            // GET KEY
            public string GetKey(string header, string key, string defaultVal)
            {
                EnsureKey(header, key, defaultVal);
                return Ini.Get(header, key).ToString();
            }

            // SET KEY
            public void SetKey(string header, string key, string arg)
            {
                Ini.Set(header, key, arg);
                UpdateCustomData();
            }

            // SET COMMENT
            public void SetComment(string header, string comment)
            {
                if (Ini.ContainsSection(header))
                    Ini.SetSectionComment(header, comment);
            }

            public void SetComment(string header, string key, string comment)
            {
                if (Ini.ContainsSection(header) && Ini.ContainsKey(header, key))
                    Ini.SetComment(header, key, comment);
            }

            // UPDATE CUSTOM DATA
            public void UpdateCustomData()
            {
                Block.CustomData = Ini.ToString();
            }


            // REDRAW NEEDED
            public bool RedrawNeeded()
            {
                MenuPage page = Pages[CurrentPage];
                bool redraw = false;

                foreach (int key in page.Buttons.Keys)
                {
                    MenuButton button = page.Buttons[key];

                    if (button.IsToggleButton && button.IsActive != button.ToggleBlock.IsActive())
                        redraw = true;
                }

                return redraw;
            }

            // SET BUTTON KEYS //
            public void SetButtonKeys(string header, string blockName, string blockLabel, string action, string actionLabel, string toggleBlockName, string blinkLength = "0")
            {
                SetKey(header, BUTTON_BLOCK, blockName);
                SetKey(header, BLOCK_LABEL, blockLabel);
                SetKey(header, BUTTON_ACTION, action);
                SetKey(header, ACTION_LABEL, actionLabel);
                SetKey(header, TOGGLE_KEY, toggleBlockName);
                SetKey(header, BLINK_LENGTH, blinkLength);
            }

            // SET PAGE TITLE //
            public void SetPageTitleKey(int page, string title)
            {
                SetKey(PageHeader(page), "Title", title);
            }


            // COPY PAGE //
            public void CopyPage(int sourcePage, int destPage)
            {
                if (sourcePage > PageCount || destPage > PAGE_LIMIT || sourcePage == destPage)
                {
                    _statusMessage += "INVALID Page Numbers: " + sourcePage + " " + destPage + "\n";
                    return;
                }

                if (destPage > PageCount)
                {
                    destPage = PageCount + 1;
                    PageCount = destPage;
                    SetKey(MENU_HEAD, PAGE_COUNT, PageCount.ToString());
                }

                string sourcePageHeader, destPageHeader, sourceMenuHeader, destMenuHeader;

                sourcePageHeader = PageHeader(sourcePage);
                destPageHeader = PageHeader(destPage);

                SetKey(destPageHeader, "Title", GetKey(sourcePageHeader, "Title", ""));

                for (int i = 1; i <= MaxButtons; i++)
                {
                    sourceMenuHeader = ButtonHeader(sourcePage, i);
                    destMenuHeader = ButtonHeader(destPage, i);

                    SetButtonKeys(destMenuHeader,
                        GetKey(sourceMenuHeader, BUTTON_BLOCK, ""),
                        GetKey(sourceMenuHeader, BLOCK_LABEL, ""),
                        GetKey(sourceMenuHeader, BUTTON_ACTION, ""),
                        GetKey(sourceMenuHeader, ACTION_LABEL, ""),
                        GetKey(sourceMenuHeader, TOGGLE_KEY, ""),
                        GetKey(sourceMenuHeader, BLINK_LENGTH, "")
                    );
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
            public ToggleBlock ToggleBlock;

            public bool IsProgramButton;
            public bool IsTransponder;
            public bool IsToggleButton;
            public bool IsActive;
            public bool IsPlaceHolder;
            public bool IsBlinkButton;

            public string BlockLabel;
            public string [] ActionLabel;
            public string Action;
            public int BlinkDuration; // in cycles
            
            public int IlluminationTime;

            public MenuButton(int buttonNumber)
            {
                Blocks = new List<IMyTerminalBlock>();
                Number = buttonNumber;
                IsActive = false;
                IsPlaceHolder = false;
                IsProgramButton = false;
                IsTransponder = false;
                IsBlinkButton = false;
                ProgramBlock = null;
                ActionLabel = new string[] { "#", "" };
                BlinkDuration = 0;
                IlluminationTime = 0;
            }

            // Set Blink Duration
            public void SetBlinkDuration(float timeInSeconds)
            {
                BlinkDuration = (int)(timeInSeconds * 6);
                IsBlinkButton = BlinkDuration > 0;
            }

            // SET PROGRAM BLOCK //
            public void SetProgramBlock(IMyProgrammableBlock programBlock)
            {
                ProgramBlock = programBlock;
                IsProgramButton = !(programBlock == null);
            }


            // SET TOGGLE BLOCK //
            public void SetToggleBlock(ToggleBlock toggleBlock)
            {
                ToggleBlock = toggleBlock;
                IsToggleButton = !(toggleBlock == null);

                if (IsToggleButton)
                    IsActive = toggleBlock.IsActive();
            }

            // SET BLOCK LABEL //
            public void SetBlockLabel(string label)
            {
                string newLabel = label.ToUpper();

                if(newLabel == "")
                {
                    if (IsProgramButton)
                        newLabel = ProgramBlock.CustomName;
                    else if (Blocks.Count > 0)
                        newLabel = Blocks[0].CustomName;
                }

                if (newLabel.Length > CHAR_LIMIT)
                    BlockLabel = newLabel.Substring(0, CHAR_LIMIT);
                else
                    BlockLabel = newLabel;
            }

            // SET ACTION LABEL //
            public void SetActionLabel(string label)
            {
                string newLabel = label.ToUpper();

                if(newLabel.StartsWith("{"))
                {
                    ActionLabel[0] = newLabel;
                }
                else if(newLabel.Contains(' '))
                {
                    // User can specify multi-line label with comma separator
                    string[] labels = newLabel.Split(' ');

                    if (labels.Length > 1)
                    {
                        ActionLabel[0] = labels[0];
                        ActionLabel[1] = labels[1];
                    }
                    else
                    {
                        ActionLabel[0] = newLabel;
                    }
                }
                else if(newLabel.Length > CHAR_LIMIT)
                {
                    // Split Label up if longer than char limit
                    int length = newLabel.Length;

                    ActionLabel[0] = newLabel.Substring(0, CHAR_LIMIT);
                    ActionLabel[1] = newLabel.Substring(CHAR_LIMIT, length - CHAR_LIMIT);
                }
                else
                {
                    ActionLabel[0] = newLabel;
                }


                for(int i = 0; i < 2; i++)
                {
                    string entry = ActionLabel[i];

                    if (entry.Length > CHAR_LIMIT && !entry.StartsWith("{"))
                        ActionLabel[i] = entry.Substring(0, CHAR_LIMIT);
                }       
            }

            // ACTIVATE ILLUMINATION //
            public void ActivateIllumination()
            {
                if(IsBlinkButton)
                {
                    IsActive = true;
                    IlluminationTime = BlinkDuration;
                    //_buttonsLit = true;
                }
                else if (IsToggleButton && ToggleBlock != null)
                {
                    IsActive = ToggleBlock.IsActive();
                }
                else
                {
                    IsActive = true;
                    IlluminationTime = ILLUMINATION_TIME;
                    //_buttonsLit = true;
                }
            }

            // IS UNASSIGNED //
            public bool IsUnassigned()
            {
                if (IsPlaceHolder || ProgramBlock != null || Blocks.Count > 0)
                    return false;
                else
                    return true;
            }
        }

        


        // ASSIGN MENUS //
        void AssignMenus()
        {
            _menus = new Dictionary<int, Menu>();
            _nextCommand = "";

            List<IMyTerminalBlock> menuBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(MENU_TAG, menuBlocks);

            if (menuBlocks.Count < 1)
            {
                _currentMenuKey = 0;
                return;
            }
                

            foreach(IMyTerminalBlock menuBlock in menuBlocks)
            {
                if(SameGridID(menuBlock) && (menuBlock as IMyTextSurfaceProvider).SurfaceCount > 0)
                {
                    Menu menu = InitializeMenu(menuBlock);
                    _menus[menu.IDNumber] = menu;

                    //DrawMenu(menu);
                }
            }

            _currentMenuKey = GetFirstMenuKey();

            DrawAllMenus();
        }


        // INITIALIZE MENU //
        Menu InitializeMenu(IMyTerminalBlock menuBlock)
        {
            Menu menu = new Menu(menuBlock);

            for(int i = 1; i <= menu.PageCount; i++)
            {
                menu.Pages[i] = InitializeMenuPage(menu, i);
            }

            menu.UpdateCustomData();
            return menu;
        }


        // INITIALIZE MENU PAGE //
        MenuPage InitializeMenuPage(Menu menu, int pageNumber)
        {
            MenuPage menuPage = new MenuPage(pageNumber);
            string header = PageHeader(pageNumber);

            menuPage.Name = menu.GetKey(header, "Title", "");

            for(int i = 1; i < menu.MaxButtons + 1; i++)
            {
                menuPage.Buttons[i] = InitializeButton(menu, pageNumber, i);
            }

            return menuPage;
        }


        // INITIALIZE BUTTON //
        MenuButton InitializeButton(Menu menu, int pageNumber, int buttonNumber)
        {
            MenuButton button = new MenuButton(buttonNumber);

            string header = ButtonHeader(pageNumber, buttonNumber);
            
            string blockString = menu.GetKey(header, BUTTON_BLOCK, ""); // Block #
            string blockLabelString = menu.GetKey(header, BLOCK_LABEL, ""); // Block # Label

            button.Action = menu.GetKey(header, BUTTON_ACTION, ""); // Action #
            string actionLabelString = menu.GetKey(header, ACTION_LABEL, ""); // Action # Label

            AssignToggleBlock(button, menu.GetKey(header, TOGGLE_KEY, "")); // Toggle Block (if any)

            button.SetBlinkDuration(ParseFloat(menu.GetKey(header, BLINK_LENGTH, "0"), 0));

            // Get Block Name and Prefix from string
            string[] buttonData = GetBlockDataFromKey(blockString);
            string blockName = buttonData[0];
            string prefix = buttonData[1];

            // Set Block Group, Program Block or Blocks
            if (prefix.ToUpper() == "G")
            {
                Echo("GROUP: " + blockName);
                IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(blockName);

                try
                {
                    //Assign group blocks to button's block list.
                    group.GetBlocks(button.Blocks);
                }
                catch
                {
                    _statusMessage += "NO GROUP WITH NAME \"" + blockName + "\" FOUND!\n";
                } 
            }
            else if(prefix.ToUpper() == "P")
            {
                Echo("PROGRAM: " + blockName);
                string programName = blockName;
                button.SetProgramBlock(GetProgramBlock(programName));
            }
            else if(blockName.ToUpper() == PLACE_HOLDER)
            {
                button.IsPlaceHolder = true;
            }
            else if(!string.IsNullOrEmpty(blockName))
            {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.SearchBlocksOfName(blockName, blocks);

                if(blocks.Count > 0)
                {
                    foreach (IMyTerminalBlock block in blocks)
                    {
                        if(SameGridID(block) && block.CustomName == blockName)
                        button.Blocks.Add(block);
                    }

                    if(button.Blocks.Count == 1 && button.Blocks[0].GetType().ToString().ToLower().Contains("transponder"))
                        button.IsTransponder = true;
                }
            }
            Echo("E");
            button.SetBlockLabel(blockLabelString);
/*
            if (prefix.Contains("T"))
                AssignToggleBlock(button, toggleName);
            else
                button.ToggleBlock = null;*/

            if (actionLabelString != "")
                button.SetActionLabel(actionLabelString);
            else
                button.SetActionLabel(button.Action);
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
            Menu menu = GetMenuByString(menuKey);

            if (MenuNotFound(menu))
                return;

            menu.CurrentPage++;

            if (menu.CurrentPage > menu.PageCount)
                menu.CurrentPage = 1;

            menu.SetKey(MENU_HEAD, PAGE_KEY, menu.CurrentPage.ToString());

            DrawMenu(menu);
        }


        // PREVIOUS MENU PAGE //
        void PreviousMenuPage(string menuKey)
        {
            Menu menu = GetMenuByString(menuKey);

            if (MenuNotFound(menu))
                return;

            menu.CurrentPage--;

            if (menu.CurrentPage < 1)
                menu.CurrentPage = menu.PageCount;

            menu.SetKey(MENU_HEAD, PAGE_KEY, menu.CurrentPage.ToString());

            DrawMenu(menu);
        }


        // GET MENU //
        Menu GetMenuByInt(int menuKey)
        {
            if (_menus.Count < 1)
                return null;

            if (menuKey == 0)
                return GetFirstMenu();
            else if (!_menus.ContainsKey(menuKey))
                return null;
            else
                return _menus[menuKey];
        }

        Menu GetMenuByString(string menuKey)
        {
            if (_menus.Count < 1)
                return null;

            int key;

            if (menuKey == "0" || menuKey == "")
                key = 0;
            else
                key = ParseInt(menuKey, 0);

            return GetMenuByInt(key);
        }




        // GET FIRST MENU //
        Menu GetFirstMenu()
        {
            if (_menus.Count < 1)
                return null;

            int index = GetFirstMenuKey();

            return _menus[index];
        }


        // GET FIRST MENU KEY //
        int GetFirstMenuKey()
        {
            if (_menus.Count > 0)
                return _menus.Keys.Min();
            else
                return 0;
        }


        // NEXT MENU KEY //
        void IncrementMenuKey()
        {
            if (_menus.Count < 2)
                return;

            int[] keys = _menus.Keys.ToArray();
            int length = keys.Length;
            int nextKey;

            for(int i = 0; i < length; i++)
            {
                if(keys[i] == _currentMenuKey)
                {
                    nextKey = i + 1;

                    if (nextKey >= length)
                        nextKey = GetFirstMenuKey();

                    _currentMenuKey = nextKey;

                    return;
                }
            }
        }


        // PRESS BUTTON //
        void PressButton(string menuKey, int buttonNumber)
        {
            Menu menu = GetMenuByString(menuKey);

            if (MenuNotFound(menu))
                return;

            MenuPage page = menu.Pages[menu.CurrentPage];
            MenuButton button = page.Buttons[buttonNumber];

            /*
            Echo("Button " + button.Number);
            if (button.ProgramBlock != null)
                Echo("Program: " + button.ProgramBlock.CustomName);
            else if (button.Blocks.Count > 0)
                Echo("Block 1: " + button.Blocks[0].CustomName);
            else
                Echo("UNASSIGNED");

            //Echo(" - Toggle: " + button.IsToggleButton);
            
            if (button.IsToggleButton)
                Echo(" - Block: " + button.ToggleBlock.Block.CustomName);


            if(button.Action == "")
            {
                Echo("No action set for Menu:" + menu.IDNumber + " Button:" + button.Number);
                return;
            }
            */
            ActivateButton(menu, button);
            /*
            // Set update loop for normal button presses
            if (!(button.IsToggleButton) || button.IsBlinkButton)
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
            */
            DrawMenu(menu);
        }


        // ACTIVATE BUTTON //
        public void ActivateButton(Menu menu, MenuButton button)
        {
            if (button.IsProgramButton && button.ProgramBlock != null)
            {
                if (button.ProgramBlock == Me)
                    RunNext(button.Action);
                else
                    button.ProgramBlock.TryRun(button.Action);
            }
            else if (button.IsTransponder)
            {
                ActivateTransponder(button);
            }
            else if (button.Blocks.Count > 0)
            {
                foreach (IMyTerminalBlock block in button.Blocks)
                {
                    try
                    {
                        block.GetActionWithName(button.Action).Apply(block);
                    }
                    catch
                    {
                        _statusMessage += block.CustomName + " cannot perform action \"" + button.Action + "\"!\n";
                    }
                }
            }

            button.ActivateIllumination();
        }


        // ACTIVATE TRANSPONDER //
        void ActivateTransponder(MenuButton button)
        {
            try
            {
                IMyTransponder transponder = button.Blocks[0] as IMyTransponder;

                string[] args = button.Action.Split(' ');
                if (args.Length < 2)
                {
                    _statusMessage += "INVALID TRANSPONDER ACTION:\n " + button.Action + "\n";
                    return;
                }

                string action = args[0].ToUpper();
                int channel = ParseInt(args[1], 0);

                

                if (action == "SET")
                    transponder.Channel = channel;
                else if (action == "SEND")
                    transponder.SendSignal(channel);
            }
            catch (Exception e)
            {
                _statusMessage += "TRANSPONDER: " + e.Message + "\n";
            }
        }


        // MENU NOT FOUND //
        bool MenuNotFound(Menu menu)
        {
            if(menu == null)
            {
                _statusMessage = "NO MENU FOUND!";
                return true;
            }

            return false;
        }


        // ASSIGN TOGGLE BLOCK // t:
        void AssignToggleBlock(MenuButton button, string toggleArg)
        {
            if (toggleArg == "")
            {
                button.SetToggleBlock(null);
                return;
            }

            string [] toggleData = toggleArg.Split(';');
            string blockName = toggleData[0].Trim();
            IMyTerminalBlock toggleBlock = GridTerminalSystem.GetBlockWithName(blockName);

            if (toggleBlock == null)
            {
                button.SetToggleBlock(null);
                _statusMessage += "WARNING: No toggle block with name " + blockName + " found!\n";
                return;
            }

            string argData;

            if (toggleData.Length > 1)
                argData = toggleData[1].Trim();
            else
                argData = "";

            button.SetToggleBlock(new ToggleBlock(toggleBlock, argData));

            /*
            if(toggleArg == "")
            {
                if (button.IsProgramButton)
                    button.SetToggleBlock(button.ProgramBlock);
                else
                    button.SetToggleBlock(button.Blocks[0]);
            }
            else
            {
                IMyTerminalBlock toggleBlock = GridTerminalSystem.GetBlockWithName(toggleArg);

                if (toggleBlock != null && SameGridID(toggleBlock))
                    button.SetToggleBlock(toggleBlock);
                else
                    button.SetToggleBlock(null);
            }*/

        }


        // CHECK LIT BUTTONS //
        void CheckLitButtons()
        {
            /*
            if (!_buttonsLit)
                return;

            //Run previously stored command
            if(_nextCommand != "")
            {
                MainSwitch(_nextCommand);
                _nextCommand = "";
            }*/

            foreach(int menuKey in _menus.Keys)
            {
                Menu menu = _menus[menuKey];
                bool redrawNeeded = false;

                foreach (int pageKey in menu.Pages.Keys)
                {
                    MenuPage page = menu.Pages[pageKey];

                    foreach(int buttonKey in page.Buttons.Keys)
                    {
                        MenuButton button = page.Buttons[buttonKey];

                        if (button.IsBlinkButton && button.IlluminationTime > 0)
                            redrawNeeded = redrawNeeded || CheckBlinkButton(menu, button);
                        else if(button.IsToggleButton)
                            redrawNeeded = redrawNeeded || CheckToggleButton(menu, button);
                        else
                            redrawNeeded = redrawNeeded || CheckNormalButton(menu, button);
                    }
                }

                if (redrawNeeded)
                    DrawMenu(menu);
            }

            /*
            _buttonsLit = redrawNeeded;

            /*
            if (!redrawNeeded && !_cruiseThrustersOn)
                Runtime.UpdateFrequency = UpdateFrequency.None;
            */
        }


        // CHECK NORMAL BUTTON - Decriment button light time. Return true if button needs to be redrawn.
        bool CheckNormalButton(Menu menu, MenuButton button)
        {
            if (button.IlluminationTime < 1)
                return false;

            button.IlluminationTime--;

            if (button.IlluminationTime < 1)
            {
                button.IsActive = false;
                return true;
            }
            else
            {
                return false;
            }
        }


        // CHECK TOGGLE BUTTON // - Make sure button status matches toggle block status. Return true if redraw needed
        bool CheckToggleButton(Menu menu, MenuButton button)
        {
            if(button.IsActive != button.ToggleBlock.IsActive())
            {
                button.IsActive = button.ToggleBlock.IsActive();
                return true;
            }

            return false;
        }


        // CHECK BLINK BUTTON - Decrement button light time. Check set current light. Return true redraw needed.
        bool CheckBlinkButton(Menu menu, MenuButton button)
        {
            if (button.IlluminationTime < 1)
                return false;

            button.IlluminationTime--;

            if (button.IlluminationTime > 0)
            {
                return UpdateBlink(menu, button);
            }
            else
            {
                if (button.IsToggleButton)
                    button.IsActive = button.ToggleBlock.IsActive();
                else
                    button.IsActive = false;

                return true;
            }
        }


        // UPDATE BLINK
        bool UpdateBlink(Menu menu, MenuButton button)
        {
            int remainder = button.IlluminationTime % menu.BlinkCycle;

            if (remainder == 0)
            {
                button.IsActive = !button.IsActive;
                return true;
            }

            return false;
        }


        // RUN NEXT // - set an argument to be run by this program on the next activation
        static void RunNext(string arg)
        {
            // Don't allow user to call menu button commands
            if (arg.ToUpper().StartsWith("BUTTON"))
                return;

            //_buttonsLit = true;
            _nextCommand = arg;
            //Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }


        // GET BUTTON BLOCK NAME // -  Gets Block Data from INI Key { BlockName, Group/Program/Toggle, Alternate Toggle Block Name }
        string [] GetBlockDataFromKey(string arg)
        {
            // Split at colon to detect prefix
            string[] rawData = arg.Split(':');

            string name;
            string prefix;

            if(rawData.Length > 1)
            {
                prefix = rawData[0];
                name = rawData[1];   
            }
            else
            {
                prefix = "";
                name = arg;
            }

            return new string[] {name, prefix};
        }

        
        // MENU DEBUG //
        void MenuDebug()
        {
            if (_menus.Count < 1)
                return;

            Menu menu = GetFirstMenu();
            MenuPage page = menu.Pages[menu.CurrentPage];

            Echo("MENU " + menu.IDNumber + "\n  Page " + menu.CurrentPage + ": " + page.Name + "\n  Buttons:");

            foreach(int key in page.Buttons.Keys)
            {
                MenuButton button = page.Buttons[key];

                IMyTerminalBlock block;

                if (button.IsProgramButton)
                    block = button.ProgramBlock as IMyTerminalBlock;
                else if (button.Blocks.Count > 0)
                    block = button.Blocks[0];
                else
                    block = null;

                if (block != null || button.IsPlaceHolder)
                {
                    //Echo("   " + button.Number + ": " + button.BlockLabel);
                    //Echo("Blink: " + button.IsBlinkButton.ToString());

                    if(button.IsToggleButton)
                    {
                        ToggleBlock toggle = button.ToggleBlock;
                        //Echo("   TOGGLE: " + toggle.ToggleType);
                    }
                }     
            }
        }

        /*
        // DRAW CURRENT MENU //
        void DrawCurrentMenu()
        {
            Menu menu = GetMenuByInt(_currentMenuKey);

            if (menu == null)
                return;

            DrawMenu(menu);
            IncrementMenuKey();
        }
        */


        // BUTTON HEADER //
        public static string ButtonHeader(int pageNumber, int buttonNumber)
        {
            return "Button " + buttonNumber + " (page " + pageNumber + ")";
        }


        // PAGE HEADER //
        public static string PageHeader(int pageNumber)
        {
            return DASHES + PAGE_HEAD + pageNumber + DASHES;
        }


        // COPY PAGE //
        public void CopyPage(string argString)
        {
            int sourcePage, destPage;
            string menuString;
            string [] args = argString.Split(' ');

            if(args.Length < 2) {
                _statusMessage += "INSUFFICIENT COPY ARGUMENTS\n Please Provide:\n * Source Page Number\n * Destination Page Number\n * Menu Number (Optional)\n";
                return;
            }

            sourcePage = ParseInt(args[0], 0);
            destPage = ParseInt(args[1], 0);

            if (args.Length == 2)
                menuString = "";
            else
                menuString = args[2];

            Menu menu = GetMenuByString(menuString);
            if (MenuNotFound(menu)) { return; }

            menu.CopyPage(sourcePage, destPage);

            Build();
        }
    }
}
