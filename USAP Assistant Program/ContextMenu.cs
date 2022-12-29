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
        const string PLACE_HOLDER = "{AUX}";
        const int PAGE_LIMIT = 9;
        const int CHAR_LIMIT = 7;
        const int ILLUMINATION_TIME = 3;
        

        static Dictionary<int, Menu> _menus;
        static bool _buttonsLit = false;
        string _nextCommand;
        //static bool _menusAssigned = false;


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
            public string Decals;
            public Dictionary<int, MenuPage> Pages;

            public Color BackgroundColor;
            public Color TitleColor;
            public Color LabelColor;
            public Color ButtonColor;
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

                SetPageCount();
                SetCurrentPage();

                Alignment = GetKey(MENU_HEAD, "Alignment", "BOTTOM");

                // Set Menu Surface
                int screenIndex = ParseInt(GetKey(MENU_HEAD, "Screen Index", "0"), 0);
                Surface = SurfaceFromBlock(block as IMyTextSurfaceProvider, screenIndex);
                PrepareTextSurfaceForSprites(Surface);

                // Decals
                Decals = GetKey(MENU_HEAD, "Decals", "").ToUpper();

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
                //string oldComment = Ini.GetSectionComment(header);
                Ini.SetSectionComment(header, comment);
            }

            public void SetComment(string header, string key, string comment)
            {
                Ini.SetComment(header, key, comment);
            }

            public void UpdateCustomData()
            {
                Block.CustomData = Ini.ToString();
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
            public bool IsActive;
            public bool IsPlaceHolder;

            public string BlockLabel;
            public string [] ActionLabel;
            public string Action;

            public int IlluminationTime;

            public MenuButton(int buttonNumber)
            {
                Blocks = new List<IMyTerminalBlock>();
                Number = buttonNumber;
                IsActive = false;
                IsPlaceHolder = false;
                IsProgramButton = false;
                ProgramBlock = null;
                ActionLabel = new string[] { "#", "" };
                IlluminationTime = 0;
            }

            // SET PROGRAM BLOCK //
            public void SetProgramBlock(IMyProgrammableBlock programBlock)
            {
                ProgramBlock = programBlock;
                IsProgramButton = !(programBlock == null);
            }

            // SET TOGGLE BLOCK //
            public void SetToggleBlock(IMyTerminalBlock toggleBlock)
            {
                ToggleBlock = toggleBlock;
                IsToggleButton = !(toggleBlock == null);

                if (IsToggleButton)
                    IsActive = toggleBlock.IsWorking;
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
                if (IsToggleButton && ToggleBlock != null)
                {
                    IsActive = ToggleBlock.IsWorking;
                }
                else
                {
                    IsActive = true;
                    IlluminationTime = ILLUMINATION_TIME;
                    _buttonsLit = true;
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
                return;

            foreach(IMyTerminalBlock menuBlock in menuBlocks)
            {
                if(SameGridID(menuBlock) && (menuBlock as IMyTextSurfaceProvider).SurfaceCount > 0)
                {
                    Menu menu = InitializeMenu(menuBlock);
                    _menus[menu.IDNumber] = menu;

                    //DrawMenu(menu);
                }
            }

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
            string header = PAGE_HEAD + pageNumber;

            menuPage.Name = menu.GetKey(header, "Title", "");
            menu.SetComment(header, DASHES);
            menu.SetComment(header, "Title", DASHES);

            for(int i = 1; i < 8; i++)
            {
                menuPage.Buttons[i] = InitializeButton(menu, pageNumber, i);
            }

            return menuPage;
        }


        // INITIALIZE BUTTON //
        MenuButton InitializeButton(Menu menu, int pageNumber, int buttonNumber)
        {
            MenuButton button = new MenuButton(buttonNumber);

            string header = PAGE_HEAD + pageNumber;
            string blockKey = BUTTON_BLOCK + button.Number;
            string actionKey = BUTTON_ACTION + button.Number;
            
            string blockString = menu.GetKey(header, blockKey, ""); // Block #
            string blockLabelString = menu.GetKey(header, blockKey + " Label", ""); // Block # Label
            button.Action = menu.GetKey(header, actionKey, ""); // Action #
            string actionLabelString = menu.GetKey(header, actionKey + " Label", ""); // Action # Label


            string[] buttonData = GetBlockDataFromKey(blockString);

            //Echo("Menu: " + pageNumber + " - Button: " + buttonNumber);
            string blockName = buttonData[0];
            string prefix = buttonData[1];
            string toggleName = buttonData[2];
            //Echo("Toggle Name: [" + toggleName + "]");


            // Set Block Group, Program Block or Blocks
            if(prefix.ToUpper().Contains("G"))
            {
                IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(blockName);

                if (group == null)
                {
                    _statusMessage += "NO GROUP WITH NAME \"" + blockName + "\" FOUND!\n";
                }

                //Assign group blocks to button's block list.
                group.GetBlocks(button.Blocks);
            }
            else if(prefix.ToUpper().Contains("P"))
            {
                string programName = blockName;
                button.SetProgramBlock(GetProgramBlock(programName));
            }
            else if(blockName.ToUpper() == PLACE_HOLDER)
            {
                button.IsPlaceHolder = true;
            }
            else
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
                }
            }

            button.SetBlockLabel(blockLabelString);

            if (prefix.Contains("T"))
                AssignToggleBlock(button, toggleName);
            else
                button.ToggleBlock = null;

            if (actionLabelString != "")
                button.SetActionLabel(actionLabelString);
            else
                button.SetActionLabel(button.Action);

            /*
            if (actionData.Length > 1)
            {
                string entry = actionData[1].Trim();

                if (entry.StartsWith("T:"))
                {
                    
                    button.SetActionLabel(button.Action);
                }
                else
                {
                    button.SetActionLabel(entry);
                }
            }
            else
            {
                button.SetActionLabel(actionString);
            }*/

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
            Menu menu = GetMenu(menuKey);

            if (MenuNotFound(menu))
                return;

            menu.CurrentPage--;

            if (menu.CurrentPage < 1)
                menu.CurrentPage = menu.PageCount;

            menu.SetKey(MENU_HEAD, PAGE_KEY, menu.CurrentPage.ToString());

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


        // PRESS BUTTON //
        void PressButton(string menuKey, int buttonNumber)
        {
            Menu menu = GetMenu(menuKey);

            if (MenuNotFound(menu))
                return;

            MenuPage page = menu.Pages[menu.CurrentPage];
            MenuButton button = page.Buttons[buttonNumber];

            Echo("Button " + button.Number);
            if (button.ProgramBlock != null)
                Echo("Program: " + button.ProgramBlock.CustomName);
            else if (button.Blocks.Count > 0)
                Echo("Block 1: " + button.Blocks[0].CustomName);
            else
                Echo("UNASSIGNED");

            Echo(" - Toggle: " + button.IsToggleButton);
            if (button.IsToggleButton)
                Echo(" - Block: " + button.ToggleBlock.CustomName);


            if(button.Action == "")
            {
                Echo("No action set for Menu:" + menu.IDNumber + " Button:" + button.Number);
                return;
            }

            ActivateButton(button);

            // Set update loop for normal button presses
            if (!(button.IsToggleButton))
                Runtime.UpdateFrequency = UpdateFrequency.Update10;

            DrawMenu(menu);
        }


        // ACTIVATE BUTTON //
        public void ActivateButton(MenuButton button)
        {
            if (button.IsProgramButton && button.ProgramBlock != null)
            {
                if (button.ProgramBlock == Me)
                    RunNext(button.Action);
                else
                    button.ProgramBlock.TryRun(button.Action);
            }
            else if (button.Blocks.Count > 0)
            {
                foreach (IMyTerminalBlock block in button.Blocks)
                {
                    try
                    {
                        block.GetActionWithName(button.Action).Apply(block);
                    }
                    catch { }
                }
            }

            button.ActivateIllumination();
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
            Echo("Button " + button.Number + " Toggle: " + toggleArg);
            //if(toggleArg.EndsWith("T:") || !toggleArg.StartsWith("T:"))
               // return;
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
            }

        }


        // CHECK LIT BUTTONS //
        void CheckLitButtons()
        {
            if (!_buttonsLit)
                return;

            //Run previously stored command
            if(_nextCommand != "")
            {
                MainSwitch(_nextCommand);
                _nextCommand = "";
            }


            bool activeButtonsRemain = false;

            foreach(int menuKey in _menus.Keys)
            {
                Menu menu = _menus[menuKey];

                foreach(int pageKey in menu.Pages.Keys)
                {
                    MenuPage page = menu.Pages[pageKey];

                    foreach(int buttonKey in page.Buttons.Keys)
                    {
                        MenuButton button = page.Buttons[buttonKey];

                        if(button.IlluminationTime > 0)
                        {
                            button.IlluminationTime--;

                            if (button.IlluminationTime > 0)
                            {
                                activeButtonsRemain = true;
                            }
                            else
                            {
                                button.IsActive = false;
                                DrawMenu(menu);
                            }    
                        }
                    }
                }
            }

            _buttonsLit = activeButtonsRemain;

            if (!activeButtonsRemain && !_cruiseThrustersOn)
                Runtime.UpdateFrequency = UpdateFrequency.None;
        }


        // RUN NEXT // - set an argument to be run by this program on the next activation
        void RunNext(string arg)
        {
            // Don't allow user to call menu button commands
            if (arg.ToUpper().StartsWith("BUTTON"))
                return;

            _buttonsLit = true;
            _nextCommand = arg;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }


        // GET BUTTON BLOCK NAME // -  Gets Block Data from INI Key { BlockName, Group/Program/Toggle, Alternate Toggle Block Name }
        string [] GetBlockDataFromKey(string arg)
        {
            string[] dataOut = new string[] {"","",""};

            // Split at colon to detect prefix
            string[] rawData = arg.Split(':');

            // Middle assignment variable
            string processedData = "";

            if(rawData.Length > 1)
            {
                string prefix = rawData[0].ToUpper();

                if(prefix.Contains("T") || prefix.Contains("G") || prefix.Contains("P"))
                {
                    processedData = rawData[1];
                    dataOut[1] = prefix;

                    // Rebuild string if their are more collons
                    if(rawData.Length > 2)
                    {
                        for (int i = 2; i < rawData.Length; i++)
                        {
                            processedData += ":" + rawData[i];
                        }
                    }
                }
            }
            else
            {
                processedData = arg;
            }

            //Echo("ALERT: " + processedData);

            if (processedData.Contains("{") && processedData.Contains("}"))
            {
                dataOut[2] = GetBracedInfo(processedData);
                //Echo("HEY YOU GUYS: " + dataOut[2]);
                dataOut[0] = processedData.Substring(0, processedData.IndexOf("{")).Trim();
            }
            else
            {
                dataOut[0] = processedData.Trim();
            }

            return dataOut;
        }
    }
}
