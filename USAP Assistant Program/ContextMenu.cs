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
        const string PAGE_HEAD = "MENU PAGE ";
        const string BUTTON_BLOCK = "Block";
        const string BLOCK_LABEL = "Block Label";
        const string BUTTON_ACTION = "Action";
        const string ACTION_LABEL = "Action Label";
        const string TOGGLE_KEY = "Toggle Block";
        const string MENU_COLOR = "Menu Color Settings";
        const string BG_KEY = "Background Color";
        const string TITLE_KEY = "Title Color";
        const string BUTTON_KEY = "Button Color";
        const string LABEL_KEY = "Label Color";
        const string PLACE_HOLDER = "{AUX}";

        const string NORMAL = "NORMAL";
        const string ACTUATOR = "ACTUATOR";
        const string VENT = "VENT";
        const string DOOR = "DOOR";
        const string SENSOR = "SENSOR";
        const string TANK = "TANK";
        const string MAG_PLATE = "MAG_PLATE";
        const string EJECTOR = "EJECTOR";
        const string BATTERY = "BATTERY";

        const int PAGE_LIMIT = 9;
        const int CHAR_LIMIT = 7;
        const int ILLUMINATION_TIME = 3;

        const float THRESHHOLD = 0.95f;
        const float DV_BLINK = 0.5f; // Default length of single blink cycle (in sec)
        

        static Dictionary<int, Menu> _menus;
        //static bool _buttonsLit = false;
        string _nextCommand;
        //static bool _menusAssigned = false;

        int _currentMenuKey; // Key for current menu to be drawn


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

                // Vertical Alignment
                Alignment = GetKey(MENU_HEAD, "Alignment", "BOTTOM");

                // Decals
                Decals = GetKey(MENU_HEAD, "Decals", "").ToUpper();

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


        public class ToggleBlock
        {
            public IMyTerminalBlock Block;
            public string ToggleType;
            public string BlockType;
            public bool IsInverted;
            public float ToggleValue;

            public ToggleBlock (IMyTerminalBlock block, string toggleData)
            {
                Block = block;
                IsInverted = false;
                //ToggleValue = toggleValue;

                string data = toggleData.ToUpper().Trim();
                string[] blockData = Block.GetType().ToString().Split('.');
                BlockType = blockData[blockData.Length - 1].Trim();

                _statusMessage += block.CustomName + ": " + BlockType + ".\n";

                switch (data)
                {
                    case "":
                        MakeNormal();
                        break;
                    case "OFF":
                        MakeNormal(true);
                        break;
                    case "PRESSURIZED":
                    case "DEPRESSURIZED":
                        MakeVent(data);
                        break;
                    case "OPEN":
                    case "CLOSED":
                        MakeDoor(data);
                        break;
                    case "DETECTED":
                    case "NOT DETECTED":
                        MakeSensor(data);
                        break;
                    case "LOCKED":
                    case "AUTOLOCK":
                        MakeMagPlate(data);
                        break;
                    case "RECHARGE":
                    case "CHARGED":
                        MakeBattery(data);
                        break;
                    case "THROW OUT":
                    case "COLLECT ALL":
                        MakeEjector(data);
                        break;
                    case "STOCKPILE":
                    case "FULL":
                        MakeTank(data);
                        break;
                    default:
                        MakeActuator(data);
                        break;

                }
            }

            // MAKE NORMAL
            void MakeNormal(bool invert = false)
            {
                ToggleType = NORMAL;
                IsInverted = invert;
            }

            // MAKE ACTUATOR
            void MakeActuator(string data)
            {
                ToggleType = ACTUATOR;

                if(BlockType != "MyExtendedPistonBase" && BlockType != "MyMotorStator" && BlockType != "MyMotorAdvancedStator")
                {
                    MakeNormal();
                    return;
                }
                if(data.StartsWith(">") && data.Length > 1)
                {
                    ToggleValue = ParseFloat(data.Substring(1), 0);
                }
                else if(data.StartsWith("<") && data.Length > 1)
                {
                    ToggleValue = ParseFloat(data.Substring(1), 0);
                    IsInverted = true;
                }
                else
                {
                    MakeNormal();
                }
            }


            // ACTUATOR STATE //
            bool ActuatorState()
            {
                bool state =  false;

                if(IsInverted)
                {
                    if (BlockType == "MyMotorStator" || BlockType == "MyMotorAdvancedStator")
                        state = ToDegrees((Block as IMyMotorStator).Angle) <= ToggleValue;
                    else if (BlockType == "MyExtendedPistonBase")
                        state = (Block as IMyPistonBase).CurrentPosition <= ToggleValue;
                }
                else
                {
                    if (BlockType == "MyMotorStator" || BlockType == "MyMotorAdvancedStator")
                        state = ToDegrees((Block as IMyMotorStator).Angle) >= ToggleValue;
                    else if (BlockType == "MyExtendedPistonBase")
                        state = (Block as IMyPistonBase).CurrentPosition >= ToggleValue;
                }

                return state;
            }

            // MAKE VENT
            void MakeVent(string data)
            {
                if(BlockType != "MyAirVent")
                {
                    MakeNormal();
                    return;
                }

                ToggleType = VENT;

                if (data == "DEPRESSURIZED")
                    IsInverted = true;
            }

            // VENT STATE
            bool VentState()
            {
                bool state = (Block as IMyAirVent).GetOxygenLevel() >= THRESHHOLD;

                if (IsInverted)
                    state = !state;

                return state;
            }

            // MAKE DOOR
            void MakeDoor(string data)
            {
                if(BlockType != "MyDoor" && BlockType != "MyAirtightHangarDoor" && BlockType != "MyAirtightSlideDoor")
                {
                    MakeNormal();
                    return;
                }

                ToggleType = DOOR;

                if (data == "CLOSED")
                    IsInverted = true;
            }

            // DOOR STATUS
            bool DoorState()
            {
                bool state = (Block as IMyDoor).OpenRatio > 0;

                if (IsInverted)
                    state = !state;

                return state;
            }

            // MAKE SENSOR
            void MakeSensor(string data)
            {
                if(BlockType != "MySensorBlock")
                {
                    MakeNormal();
                    return;
                }

                ToggleType = SENSOR;

                if (data == "NOT DETECTED")
                    IsInverted = true;
            }

            // SENSOR STATE
            bool SensorState()
            {
                bool state = (Block as IMySensorBlock).IsActive;
                
                if (IsInverted)
                    state = !state;

                return state;
            }


            // MAKE EJECTOR
            void MakeEjector(string data)
            {
                if(BlockType != "MyShipConnector")
                {
                    MakeNormal();
                    return;
                }

                ToggleType = EJECTOR;

                if (data == "COLLECT ALL")
                    IsInverted = true;
            }


            // EJECTOR STATE
            bool EjectorState()
            {
                IMyShipConnector ejector = Block as IMyShipConnector;

                if (IsInverted)
                    return ejector.CollectAll;
                else
                    return ejector.ThrowOut;
            }


            // MAKE MAG PLATE
            void MakeMagPlate(string data)
            {
                if (BlockType != "MyLandingGear" && BlockType != "MyShipConnector")
                {
                    MakeNormal();
                    return;
                }

                ToggleType = MAG_PLATE;

                if (data == "AUTOLOCK")
                {
                    if (BlockType == "MyShipConnector")
                        _statusMessage += "Warning: \"AUTOLOCK\" is not a valid parameter for Connector Blocks!";
                    else
                        IsInverted = true;
                }   
            }

            // MAG PLATE STATE
            bool MagPlateState()
            {
                if (BlockType == "MyShipConnector")
                {
                    IMyShipConnector connector = Block as IMyShipConnector;
                    return connector.Status == MyShipConnectorStatus.Connected;                        
                }
                else
                {
                    IMyLandingGear magPlate = Block as IMyLandingGear;


                    if (IsInverted)
                        return magPlate.AutoLock;
                    else
                        return magPlate.IsLocked;
                }
            }

            // MAKE BATTERY
            void MakeBattery(string data)
            {
                if (BlockType != "MyBatteryBlock" && BlockType != "MyJumpDrive")
                {
                    MakeNormal();
                    return;
                }

                ToggleType = BATTERY;

                if (data == "CHARGED") // Treat Charged Batter/Jump Drive as Inverted case
                    IsInverted = true;
            }

            // BATTERY STATE
            bool BatteryState()
            {
                if(BlockType == "MyBatteryBlock")
                {
                    IMyBatteryBlock battery = Block as IMyBatteryBlock;

                    if (IsInverted)
                    {
                        float charge = battery.CurrentStoredPower / battery.MaxStoredPower;
                        return  charge > THRESHHOLD;
                    }
                    else
                    {
                        return battery.ChargeMode == ChargeMode.Recharge;
                    }
                        
                }
                else if(BlockType == "MyJumpDrive")
                {
                    IMyJumpDrive jumpDrive = Block as IMyJumpDrive;

                    if(IsInverted)
                    {
                        float jump = jumpDrive.CurrentStoredPower / jumpDrive.MaxStoredPower;
                        return jump > THRESHHOLD;
                    }
                    else
                    {
                        return jumpDrive.Recharge;
                    }
                }

                return false;
            }

            // MAKE TANK
            void MakeTank(string data)
            {
                if (BlockType != "MyGasTank")
                {
                    MakeNormal();
                    return;
                }

                ToggleType = TANK;

                if (data == "FULL") // Treat full tank as inverted case
                    IsInverted = true;
            }

            // TANK STATE
            bool TankState()
            {
                IMyGasTank tank = Block as IMyGasTank;

                if (IsInverted) // Light if full
                    return tank.FilledRatio > THRESHHOLD;
                else
                    return tank.Stockpile;
            }


            public bool IsActive()
            {
                bool active;

                switch (ToggleType)
                {
                    case NORMAL:
                        active = Block.IsWorking;
                        break;
                    case ACTUATOR:
                        active = ActuatorState();
                        break;
                    case VENT:
                        active = VentState();
                        break;
                    case DOOR:
                        active = DoorState();
                        break;
                    case SENSOR:
                        active = SensorState();
                        break;
                    case TANK:
                        active = TankState();
                        break;
                    case MAG_PLATE:
                        active = MagPlateState();
                        break;
                    case EJECTOR:
                        active = EjectorState();
                        break;
                    case BATTERY:
                        active = BatteryState();
                        break;
                    default:
                        active = IsInverted;
                        break;                
                }

                if (IsInverted)
                    active = !active;

                return active;
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
            string header = DASHES + PAGE_HEAD + pageNumber + DASHES;

            menuPage.Name = menu.GetKey(header, "Title", "");

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

            string header = "Button " + buttonNumber + " (page " + pageNumber + ")";
            //string blockKey = BUTTON_BLOCK + button.Number;
            //string actionKey = BUTTON_ACTION + button.Number;
            
            string blockString = menu.GetKey(header, BUTTON_BLOCK, ""); // Block #
            string blockLabelString = menu.GetKey(header, BLOCK_LABEL, ""); // Block # Label

            button.Action = menu.GetKey(header, BUTTON_ACTION, ""); // Action #
            string actionLabelString = menu.GetKey(header, ACTION_LABEL, ""); // Action # Label

            AssignToggleBlock(button, menu.GetKey(header, TOGGLE_KEY, "")); // Toggle Block (if any)

            button.SetBlinkDuration(ParseFloat(menu.GetKey(header, "Blink Length", "0"), 0));
            //Echo("Blink: " + button.IsBlinkButton + ": " + button.BlinkDuration);

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

            ActivateButton(button);
            /*
            // Set update loop for normal button presses
            if (!(button.IsToggleButton) || button.IsBlinkButton)
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
            */
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
                return;*/

            //Run previously stored command
            if(_nextCommand != "")
            {
                MainSwitch(_nextCommand);
                _nextCommand = "";
            }

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
        void RunNext(string arg)
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

                if (block != null)
                {
                    Echo("   " + button.Number + ": " + button.BlockLabel);

                    if(button.IsToggleButton)
                    {
                        ToggleBlock toggle = button.ToggleBlock;
                        Echo("   TOGGLE: " + toggle.ToggleType);
                    }
                }     
            }
        }


        // DRAW CURRENT MENU //
        void DrawCurrentMenu()
        {
            Menu menu = GetMenuByInt(_currentMenuKey);

            if (menu == null)
                return;

            DrawMenu(menu);
            IncrementMenuKey();
        }
    }
}
