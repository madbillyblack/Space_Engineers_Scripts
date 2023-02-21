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
	partial class Program : MyGridProgram
	{
		// This file contains your actual script.
		//
		// You can either keep all your code here, or you can create separate
		// code files to make your program easier to navigate while coding.
		//
		// In order to add a new utility class, right-click on your project, 
		// select 'New' then 'Add Item...'. Now find the 'Space Engineers'
		// category under 'Visual C# Items' on the left hand side, and select
		// 'Utility Class' in the main area. Name it in the box below, and
		// press OK. This utility class will be merged in with your code when
		// deploying your final script.
		//
		// You can also simply create a new utility class manually, you don't
		// have to use the template if you don't want to. Just do so the first
		// time to see what a utility class looks like.
		// 
		// Go to:
		// https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
		//
		// to learn more about ingame scripts.



		//////////////////////////////////
		//		PRESSURE CHIEF			  		  //
		// Pressure management script 	//
		//				 Author: SJ_Omega		 		//
		//////////////////////////// v1.0

		// USER CONSTANTS -  Feel free to change as needed -------------------------------------------------------------------------------------------------
		const string VAC_TAG = "Exterior"; // Tag used to designate External reference vents (i.e. Vacuum vents).

		// Background Colors // - RGB values for LCD background
		const int BG_RED = 127;
		const int BG_GREEN = 127;
		const int BG_BLUE = 127;

		// Pressurized colors // - RGB values for pressurized chambers
		const int PRES_RED = 64;
		const int PRES_GREEN = 16;
		const int PRES_BLUE = 32;

		// Text Color // - General Text Color for LCDs
		const int TEXT_RED = 255;
		const int TEXT_GREEN = 255;
		const int TEXT_BLUE = 255;

		// Room Color // - Text Color for current room
		const int ROOM_RED = 255;
		const int ROOM_GREEN = 255;
		const int ROOM_BLUE = 0;

		// AVOID CHANGING ANYTHING BELOW THIS LINE!!!!!-----------------------------------------------------------------------------------------------------
		const string INI_HEAD = "Pressure Chief";
		const string GAUGE_HEAD = "Pressure Gauge";
		const string SHARED = "Shared Data";
		const string MONITOR_HEAD = "Pressure Data";
		const string NORMAL = "255,255,255";
		const string EMERGENCY = "255,0,0";
		const int DELAY = 3;
		const float THRESHHOLD = 0.2f;
		const int SCREEN_THRESHHOLD = 180;
		const string UNIT = "%"; // Default pressure unit.
		const string DATA_TAG = "[P_Data]";
		const string SLASHES = "//////////////////////////////////////////////////////////////////////////////////////////////////////\n";
		const string GROUP_TAG = "SECTOR";


		// Globals //
		static string _statusMessage;
		static string _buildMessage;
		static string _previosCommand;
		static string _overview;
		static string _gridID;
		static string _vacTag;
		static string _unit; // Display unit of pressure.
		static float _atmo; // Atmospheric conversion factor decided by unit.
		static float _factor; // Multiplier of pressure reading.
		static string _systemsName;

		// Breather Array - Used to indicate that program is running in terminal
		static string[] _breather =	{"\\//////////////",
											"/\\/////////////",
											"//\\////////////",
											"///\\///////////",
											"////\\//////////",
											"/////\\/////////",
											"//////\\////////",
											"///////\\///////",
											"////////\\//////",
											"/////////\\/////",
											"//////////\\////",
											"///////////\\///",
											"////////////\\//",
											"/////////////\\/",
											"//////////////\\",
											"/////////////\\/",
											"////////////\\//",
											"///////////\\///",
											"//////////\\////",
											"/////////\\/////",
											"////////\\//////",
											"///////\\///////",
											"//////\\////////",
											"/////\\/////////",
											"////\\//////////",
											"///\\///////////",
											"//\\////////////",
											"/\\/////////////"};
		static int _breatherStep; //Current index of breather array
		static int _breatherLength;

		int _currentBulkhead; //Sector that the program is currently checking
							//static bool _autoCheck;
		static bool _autoClose;

		static Color _backgroundColor; //Global used to store background color of LCD displays
		static Color _textColor; //Global used to store default text color of LCD displays
		static Color _roomColor; //Global used to store highlight text color for room in which LCD is located

		//Lists	
		static List<IMyBlockGroup> _sectorGroups;
		static List<Sector> _sectors;
		static List<Bulkhead> _bulkheads;
		static List<DataDisplay> _dataDisplays;

		// PROGRAM /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public Program()
		{
			// Set Global Variables
			_previosCommand = "NEWLY LOADED";
			_statusMessage = "";
			_currentBulkhead = 0;
			_breatherStep = 0;
			_breatherLength = _breather.Length;

			_backgroundColor = new Color(BG_RED, BG_GREEN, BG_BLUE);
			_textColor = new Color(TEXT_RED, TEXT_GREEN, TEXT_BLUE);
			_roomColor = new Color(ROOM_RED, ROOM_GREEN, ROOM_BLUE);

			_gridID = GetKey(Me, SHARED, "Grid_ID", Me.CubeGrid.EntityId.ToString());
			//_autoCheck = true;

			// Assign all correctly named components to sectors.
			Build();

			// Check program block's custom data for Refresh_Rate and set UpdateFrequency accordingly.
			string updateFactor = GetKey(Me, INI_HEAD, "Refresh_Rate", "10");
			switch (updateFactor)
			{
				case "1":
					Runtime.UpdateFrequency = UpdateFrequency.Update1;
					break;
				case "10":
					Runtime.UpdateFrequency = UpdateFrequency.Update10;
					break;
				case "100":
					Runtime.UpdateFrequency = UpdateFrequency.Update100;
					break;
				default:
					Runtime.UpdateFrequency = UpdateFrequency.Update10;
					break;
			}
		}


		// SAVE ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void Save() { }


		// MAIN /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void Main(string arg)
		{
			// Print basic terminal output
			UpdateOverview();
			Echo("PRESSURE CHIEF " + _breather[_breatherStep]);
			Echo(_overview);

			// Echo Sector Data to Terminal
			string sectorData = "";
			foreach (Sector mySector in _sectors)
			{
				Echo("Name: " + mySector.Name);
				Echo("Type: " + mySector.Type);
				Echo("Vents: " + mySector.Vents.Count);
				Echo("Doors: " + mySector.Doors.Count);
				Echo("Lights: " + mySector.Lights.Count);
				sectorData += "\n" + mySector.Type + " " + mySector.Name + " --- " + mySector.Vents[0].Status.ToString()
					+ "\n * Doors: " + mySector.Doors.Count + "  * Lights: " + mySector.Lights.Count;
				if (mySector.Type == "Dock")
					sectorData += "\n * Merge Blocks: " + mySector.MergeBlocks.Count + "  * Connectors: " + mySector.Connectors.Count;
			}
			Echo(sectorData);

			UpdateData();

			// Check for vents to run script from
			if (_sectors.Count < 1)
			{
				Echo("No Sectors Found!");

				//If no vents search for newly named components
				if (_breatherStep == 0)
					Build();

				return;
			}

			
			if (arg == "")
			{
				// Normal Execution - Monitor Current Sector
				if (_bulkheads.Count > 0)
					_currentBulkhead++;
				if (_currentBulkhead >= _bulkheads.Count)
					_currentBulkhead = 0;

				Bulkhead bulkhead = _bulkheads[_currentBulkhead];
				Echo("\nCurrent Check: Bulkhead[" + bulkhead.TagA + " / " + bulkhead.TagB + "]");
				bulkhead.Check();
			}
			else 
			{
				_statusMessage = "";
				_previosCommand = arg;

				string[] args = arg.Split(' ');

				// Main command
				string command = args[0].ToUpper();

				// Trailing command arguments
				string cmdArg = "";
				if (args.Length > 1)
				{
					for (int i = 1; i < args.Length; i++)
						cmdArg += args[i] + " ";

					cmdArg = cmdArg.Trim();
				}

				// Command Switch
				switch (command)
				{
					case "OPEN_LOCK":
					case "OPENLOCK":
						OpenLock(cmdArg, false);
						break;
					case "OPEN_ALL":
						OpenLock(cmdArg, true);
						break;
					case "TIMER_CALL":
						TimerCall(cmdArg);
						break;
					case "CLOSE_LOCK":
					case "CLOSELOCK":
						CloseLock(cmdArg);
						break;
					case "CYCLE_LOCK":
					case "CYCLELOCK":
						CycleLock(cmdArg);
						break;
					case "DOCK_SEAL":
						DockSeal(GetSector(cmdArg));
						break;
					case "UNDOCK":
						Undock(GetSector(cmdArg));
						break;
					case "OVERRIDE":
						Bulkhead overrideDoor = GetBulkhead(cmdArg);
						overrideDoor.SetOverride(true);
						break;
					case "RESTORE":
						Bulkhead restoreDoor = GetBulkhead(cmdArg);
						restoreDoor.SetOverride(false);
						break;
					case "REFRESH":
						Build();
						break;
					case "SET_GRID_ID":
						SetGridID(cmdArg);
						break;
					case "SET_LIGHT_COLOR":
						SetSectorParameter(cmdArg, "color", false);
						break;
					case "SET_EMERGENCY_COLOR":
						SetSectorParameter(cmdArg, "color", true);
						break;
					case "SET_LIGHT_RADIUS":
						SetSectorParameter(cmdArg, "radius", false);
						break;
					case "SET_EMERGENCY_RADIUS":
						SetSectorParameter(cmdArg, "radius", true);
						break;
					case "SET_LIGHT_INTENSITY":
						SetSectorParameter(cmdArg, "intensity", false);
						break;
					case "SET_EMERGENCY_INTENSITY":
						SetSectorParameter(cmdArg, "intensity", true);
						break;
					case "VENT_CHECK_1": //For event triggered operation of script. Call from Vent actions, etc.
					case "VENT_CHECK_2":
						Sector mySector = GetSector(cmdArg);
						if (!UnknownSector(mySector, "Room"))
							mySector.Check();
						break;
					case "CLEAR":
						_statusMessage = "";
						break;
					case "SHOW_BUILD":
						_statusMessage = _buildMessage;
						break;
					default:
						_statusMessage = "UNRECOGNIZED COMMAND: " + arg;
						break;
				}

				// Exit after command executed.
				return;
			}
		}


		// TOOL FUNCTIONS //--------------------------------------------------------------------------------------------------------------------------------

		// GET SECTOR // Returns sector with given tag.
		static Sector GetSector(string name)
		{
			foreach (Sector sector in _sectors)
			{
				if (sector.Name.Trim() == name.Trim())
					return sector;
			}

			return null;
		}


		// GET BULKHEAD // Returns bulkhead with given double-tag.
		static Bulkhead GetBulkhead(string tag)
		{
			if (_bulkheads.Count < 1)
				return null;

			foreach (Bulkhead bulkhead in _bulkheads)
			{
				if (tag.Contains(bulkhead.TagA) && tag.Contains(bulkhead.TagB))
					return bulkhead;
			}

			return null;
		}


		// SET GRID ID // Updates Grid ID parameter for all designated blocks in Grid, then rebuilds the grid.
		void SetGridID(string arg)
		{
			string gridID;
			if (arg != "")
				gridID = arg;
			else
				gridID = Me.CubeGrid.EntityId.ToString();

			SetKey(Me, SHARED, "Grid_ID", gridID);
			_gridID = gridID;

			List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks);

			foreach (IMyTerminalBlock block in blocks)
			{
				if (block.CustomData.Contains(SHARED))
					SetKey(block, SHARED, "Grid_ID", gridID);
			}

			Build();
		}


		// UNKNOWN SECTOR // - Checks if Sector exists and is valid type.  Returns true if errors detected. Type can be "Room", "Lock", or "Dock". 
		bool UnknownSector(Sector sector, string type)
		{
			if (sector == null)
			{
				_statusMessage = "INVALID SECTOR CALL!";
				return true;
			}

			if (type == "Dock" && sector.Type != "Dock")
			{
				_statusMessage = "INVALID DOCK CALL!";
				return true;
			}

			if (type == "Lock" && sector.Type != "Dock" && sector.Type != "Lock")
			{
				_statusMessage = "INVALID LOCK CALL!";
				return true;
			}

			return false;
		}


		// UPDATE OVERVIEW // Updates _overview string for terminal and monitors
		void UpdateOverview()
		{
			_overview = /*"PRESSURE CHIEF " + _breather[_breatherStep] + */"--Pressure Management System--" + "\nCmd: "
				+ _previosCommand + "\n" + _statusMessage + "\n----------------------" + "Sector Count: " + _sectors.Count;

			_breatherStep++;
			if (_breatherStep >= _breatherLength)
				_breatherStep = 0;
		}


		/* SET SECTOR PARAMETER // - Updates the default color for a sector
			* Emergency bool determines if it's normal or emergency color*/
		void SetSectorParameter(string sectorData, string parameter, bool emergency)
		{
			string[] data = sectorData.Split(' ');
			bool incomplete = data.Length < 2;

			string argData = data[0];
			string tag = "";

			for (int i = 1; i < data.Length; i++)
			{
				tag += data[i] + " ";
			}

			Sector sector = GetSector(tag.Trim());
			if (UnknownSector(sector, "Room"))
				return;

			if (parameter == "color")
			{
				if (incomplete)
				{
					_statusMessage = "INCOMPLETE COLOR DATA!!!\nPlease Follow Format:\n<red_value>,<green_value>,<blue_value> <sector_tag>";
					return;
				}

				sector.SetColor(argData, emergency);
			}
			else if (parameter == "radius")
			{
				if (incomplete)
				{
					_statusMessage = "INCOMPLETE RADIUS DATA!!!\nPlease Follow Format:\n<radius> <sector_tag>";
					return;
				}

				sector.SetRadius(argData, emergency);
			}
			else if (parameter == "intensity")
			{
				if (incomplete)
				{
					_statusMessage = "INCOMPLETE RADIUS DATA!!!\nPlease Follow Format:\n<radius> <sector_tag>";
					return;
				}

				sector.SetIntensity(argData, emergency);
			}
			else
			{
				_statusMessage = "WARNING: Error in function SetSectorParameter.  Contact SJ_Omega to report bug.";
			}
		}




		// INIT FUNCIONS -----------------------------------------------------------------------------------------------------------------------------------

		// BUILD // Searches grid for all components and adds them to current run.
		void Build()
		{
			// Initialize global groups
			_buildMessage = "BUILD: ";
			_sectorGroups = new List<IMyBlockGroup>();
			_sectors = new List<Sector>();
			_bulkheads = new List<Bulkhead>();
			_dataDisplays = new List<DataDisplay>();
			_vacTag = GetKey(Me, INI_HEAD, "Vac_Tag", VAC_TAG);
			_systemsName = GetKey(Me, INI_HEAD, "Systems_Group", "");
			_autoClose = ParseBool(GetKey(Me, INI_HEAD, "Auto-Close", "true"));

			// Get tagged Sector Groups and add to List

			List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
			GridTerminalSystem.GetBlockGroups(groups);
			if (groups.Count < 1)
				return;

			// Collect groups with sector tag
			foreach(IMyBlockGroup group in groups)
            {
				if (group.Name.ToUpper().Contains(GROUP_TAG) && group.Name.Contains(":"))
					_sectorGroups.Add(group);
            }

			if (_sectorGroups.Count < 1)
            {
				_buildMessage += "\nNO SECTOR GROUPS LOCATED!";
				return;
			}
				
			// Add Sector Names to all doors in groups so that they can be used to build bulkhead objects.
			MarkDoorsAndScreens();

			// Populate Sectors
			foreach (IMyBlockGroup sectorGroup in _sectorGroups)
            {
				// Get first vent in group to check that it shares the same Grid ID. If there is no Grid ID, add current.
				List<IMyAirVent> tempVents = new List<IMyAirVent>();
				sectorGroup.GetBlocksOfType<IMyAirVent>(tempVents);

				if(tempVents.Count > 0 && GetKey(tempVents[0], SHARED, "Grid_ID", _gridID) == _gridID)
					_sectors.Add(new Sector(sectorGroup));
			}


			// Add LCD Blocks from sector groups
			foreach (IMyBlockGroup group in _sectorGroups)
				AssignLCDBlocks(group);

			// Add LCD Blocks from Pressure Data group
			AssignDataDisplays();


			//Set Pressure Unit as well as Atmospheric and User-Defined Factors
			_unit = GetKey(Me, INI_HEAD, "Unit", UNIT);
			if (float.TryParse(GetKey(Me, INI_HEAD, "Factor", "1"), out _factor))
				Echo("Unit: " + _unit + "   Factor: " + _factor);
			else
				_statusMessage = "UNPARSABLE FACTOR INPUT!!!";
			switch (_unit.ToLower())
			{
				case "atm":
					_atmo = 1f;
					break;
				case "psi":
					_atmo = 14.696f;
					break;
				case "kpa":
					_atmo = 101.325f;
					break;
				case "bar":
					_atmo = 1.013f;
					break;
				case "torr":
					_atmo = 760;
					break;
				default:
					_atmo = 100;
					break;
			}

			//_statusMessage = _buildMessage;
			SetAutoCloseDelays();
		}


		// MARK DOOR and SCREENS // Ensures that Door is marked with name of both Sectors in custom data.
		static void MarkDoorsAndScreens()
        {
			foreach(IMyBlockGroup group in _sectorGroups)
            {
				List<IMyDoor> doors = new List<IMyDoor>();
				group.GetBlocksOfType<IMyDoor>(doors);

				List<IMyTextSurfaceProvider> lcdBlocks = new List<IMyTextSurfaceProvider>();
				group.GetBlocksOfType<IMyTextSurfaceProvider>(lcdBlocks);

				string[] nameArray = group.Name.Split(':');
				if(nameArray.Length > 1)
                {
					string sectorName = nameArray[1].Trim();

					if (doors.Count > 0)
					{
						foreach (IMyDoor door in doors)
							MarkBlock(door, sectorName);
					}

					if (lcdBlocks.Count > 0)
					{
						foreach (IMyTextSurfaceProvider lcdBlock in lcdBlocks)
                        {
							if(lcdBlock.SurfaceCount > 0)
								MarkLCDBlock(lcdBlock as IMyTerminalBlock, sectorName);
						}
					}
				}
			}
		}


		// MARK BLOCK // Mark Doors and LCD blocks with both of their sectors so that they can later be assigned to the appropriate Bulkhead.
		static void MarkBlock(IMyDoor door, string sectorName)
        {
			EnsureKey(door, SHARED, "Grid_ID", _gridID);

			string sectorA = GetKey(door, INI_HEAD, "Sector_A", "");
			string sectorB = GetKey(door, INI_HEAD, "Sector_B", "");

			if (sectorA == "" && sectorB.ToUpper() != sectorName.ToUpper())
			{
				SetKey(door, INI_HEAD, "Sector_A", sectorName);
				_buildMessage += "\n" + door.CustomName + "Added to [" + sectorName + "]";
			}
			else if (sectorB == "" && sectorA.ToUpper() != sectorName.ToUpper())
			{
				SetKey(door, INI_HEAD, "Sector_B", sectorName);
				_buildMessage += "\n" + door.CustomName + "Added to [" + sectorName + "]";
			}
			else if (sectorA.ToUpper() != sectorName.ToUpper() && sectorB.ToUpper() != sectorName.ToUpper())
			{
				_buildMessage += "\nWARNING: " + door.CustomName + " is assigned to more than two sector groups!";
			}
		}


		// MARK LCD BLOCK //
		static void MarkLCDBlock(IMyTerminalBlock block, string sectorName)
        {
			EnsureKey(block, SHARED, "Grid_ID", _gridID);

			SetKey(block, GAUGE_HEAD, "Added", "False");

			string sectorList = GetKey(block, GAUGE_HEAD, "Sectors", "");

			if(sectorList == "")
            {
				SetKey(block, GAUGE_HEAD, "Sectors", sectorName);
			}
            else
            {
				string[] sectorStrings = sectorList.Split('\n');
				string newList = "";

				foreach(string sectorString in sectorStrings)
                {
					if (sectorString.Trim() == sectorName)
						return;
					else
						newList += sectorString.Trim() + "\n";
                }

				newList += sectorName;

				SetKey(block, GAUGE_HEAD, "Sectors", newList);
			}
		}


		// BULKHEAD FROM DOOR //
		public static Bulkhead BulkheadFromDoor(IMyDoor door)
        {
			string sectorA = GetKey(door, INI_HEAD, "Sector_A", "");
			string sectorB = GetKey(door, INI_HEAD, "Sector_B", "");

			if (sectorA == "" || sectorB == "")
			{
				_statusMessage = "SECTOR ERROR for " + door.CustomName;
				return null;
			}

			// Search for existing bulkhead
			Bulkhead bulkhead = GetBulkhead(sectorA + "," + sectorB);
				
			if(bulkhead == null)
            {
				bulkhead = new Bulkhead(door);
				_bulkheads.Add(bulkhead);
			}
			else
            {
				bulkhead.Doors.Add(door);
            }

			return bulkhead;
        }


		//ASSIGN DATA DISPLAYS// Get and set up blocks and surfaces designated as data displays
		void AssignDataDisplays()
		{
			IMyBlockGroup dataGroup = GridTerminalSystem.GetBlockGroupWithName(GetKey(Me, INI_HEAD, "Data_Group", "Pressure Data"));
			if (dataGroup == null)
            {
				_buildMessage += "\nOptional: No Pressure Data group found.";
				return;
			}
				

			List<IMyTerminalBlock> dataBlocks = new List<IMyTerminalBlock>();
			dataGroup.GetBlocks(dataBlocks);


			foreach (IMyTerminalBlock dataBlock in dataBlocks)
			{
				if((dataBlock as IMyTextSurfaceProvider).SurfaceCount > 0 && GetKey(dataBlock, SHARED, "Grid_ID", _gridID) == _gridID)
					_dataDisplays.Add(new DataDisplay(dataBlock));
			}
		}


		// ASSIGN LCD BLOCKS
		private void AssignLCDBlocks(IMyBlockGroup group)
		{
			List<IMyTextSurfaceProvider> lcdBlocks = new List<IMyTextSurfaceProvider>();
			group.GetBlocksOfType<IMyTextSurfaceProvider>(lcdBlocks);

			if (lcdBlocks.Count < 1)
				return;

			foreach (IMyTextSurfaceProvider lcdBlock in lcdBlocks)
			{
				if (lcdBlock.SurfaceCount > 0)
				{
					Echo((lcdBlock as IMyTerminalBlock).CustomName);
					bool added = ParseBool(GetKey(lcdBlock as IMyTerminalBlock, GAUGE_HEAD, "Added", "True"));

					if (!added)
					{
						SetKey(lcdBlock as IMyTerminalBlock, GAUGE_HEAD, "Added", "True");
						GaugeBlock gaugeBlock = new GaugeBlock(lcdBlock as IMyTerminalBlock);
					}
				}
			}
		}


		// SET AUTOCLOSE DELAYS
		public void SetAutoCloseDelays()
        {
			if (_sectors.Count < 1)
				return;

			foreach(Sector sector in _sectors)
            {
				if(sector.Vents.Count > 0)
                {
					IMyAirVent vent = sector.Vents[0];

					// If sector is a dock or lock, disable autoclose by default
					if (sector.Type != "Room")
						EnsureKey(vent, INI_HEAD, "Auto_Close_Delay", "0");
					
						
					sector.AutoCloseDelay = ParseUInt(GetKey(vent, INI_HEAD, "Auto_Close_Delay", "20")) * (int)(20 / _sectors.Count);
					sector.CurrentDelayTime = sector.AutoCloseDelay;
				}
            }
        }

		// END HERE ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	}
}
