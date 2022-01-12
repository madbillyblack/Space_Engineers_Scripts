﻿using Sandbox.Game.EntityComponents;
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



		// START HERE ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////




		//////////////////////////////////
		//        PRESSURE CHIEF			  	          //
		// Pressure management script 	//
		//                 Author: SJ_Omega		 		//
		//////////////////////////// v1.0

		// USER CONSTANTS -  Feel free to change as needed -------------------------------------------------------------------------------------------------
		const string VAC_TAG = "EXT"; // Tag used to designate External reference vents (i.e. Vacuum vents).
		
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
		const string OPENER = "[|";
		const string CLOSER = "|]";
		const char SPLITTER = '|';
		const string INI_HEAD = "Pressure Chief";
		const string NORMAL = "255,255,255";
		const string EMERGENCY = "255,0,0";
		const int DELAY = 3;
		const float THRESHHOLD = 0.2f;
		const int SCREEN_THRESHHOLD = 180;
		const string UNIT = "%"; // Default pressure unit.
		const string MONITOR_TAG = "[Pressure]";


		// Globals //
		static string _statusMessage;
		static string _previosCommand;
		static string _overview;
		static string _gridID;
		static string _vacTag;
		static string _monitorTag;
		static string _unit; // Display unit of pressure.
		static float _atmo; // Atmospheric conversion factor decided by unit.
		static float _factor; // Multiplier of pressure reading.
		
		// Breather Array - Used to indicate that program is running in terminal
		static string[] _breather =    {"\\//////////////",
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

		int _currentSector; //Sector that the program is currently checking
		//static bool _autoCheck;
		static bool _autoClose;

		static Color _backgroundColor; //Global used to store background color of LCD displays
		static Color _textColor; //Global used to store default text color of LCD displays
		static Color _roomColor; //Global used to store highlight text color for room in which LCD is located
		
		//Lists for tagged blocks
		static List<IMyAirVent> _vents;
		static List<IMyDoor> _doors;
		static List<IMyTextPanel> _lcds;
		static List<IMyButtonPanel> _buttons;
		static List<IMySoundBlock> _lockAlarms;
		static List<IMyTimerBlock> _lockTimers;
		static List<IMyShipConnector> _connectors;
		static List<IMyShipMergeBlock> _mergeBlocks;
		static List<IMyLightingBlock> _lights;
		static List<IMyTerminalBlock> _monitors;

		//Lists for script classes
		static List<Sector> _sectors;
		static List<Bulkhead> _bulkheads;


		// CLASSES //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// SECTOR // - Class that includes all components for a specific room.
		public class Sector
		{
			public string Tag; // Name or Designation for Sector
			public List<IMyAirVent> Vents;
			public List<IMyDoor> Doors;
			public List<IMyLightingBlock> Lights;
			public List<IMyTextSurface> Surfaces; // LCD screens used to display pressure readings
			public List<IMyShipMergeBlock> MergeBlocks;
			public List<IMyShipConnector> Connectors;
			public List<Bulkhead> Bulkheads;
			public string Type; // Room, Lock, Dock, or Vacuum
			public string NormalColor; // Default pressurized light color for sector
			public string EmergencyColor; // Default depressurized light color for sector
			public string Status; // Current pressure status read from main Vents[0]
			public IMyTimerBlock LockTimer;
			public IMySoundBlock LockAlarm;

			// Constructor - Vent required
			public Sector(IMyAirVent airVent)
			{
				// Create Lists
				this.Vents = new List<IMyAirVent>();
				this.Doors = new List<IMyDoor>();
				this.Lights = new List<IMyLightingBlock>();
				this.Surfaces = new List<IMyTextSurface>();
				this.MergeBlocks = new List<IMyShipMergeBlock>();
				this.Connectors = new List<IMyShipConnector>();
				this.Bulkheads = new List<Bulkhead>();

				// Set up sector based on primary vent
				this.Vents.Add(airVent);
				this.Tag = TagFromName(airVent.CustomName);
				this.NormalColor = GetKey(airVent, "Normal_Color", NORMAL);
				this.EmergencyColor = GetKey(airVent, "Emergency_Color", EMERGENCY);
				this.Status = GetKey(airVent, "Status", airVent.Status.ToString());

				// Designate exterior sector
				if (this.Tag == _vacTag)
					this.Type = "Vacuum";
				else
					this.Type = "Room";
			}

			// MONITOR - Check all pressure status between this and all neighboring sectors and update lights based on pressure status.
			public void Monitor()
			{
				foreach (Bulkhead myBulkhead in this.Bulkheads)
					myBulkhead.Monitor();

				if (this.Lights.Count > 0)
				{
					bool depressurized = this.Vents[0].GetOxygenLevel() < 0.7 || this.Vents[0].Depressurize;

					foreach (IMyLightingBlock myLight in this.Lights)
					{
						if (depressurized)
							myLight.Color = ColorFromString(GetKey(myLight, "Emergency_Color", this.EmergencyColor));
						else
							myLight.Color = ColorFromString(GetKey(myLight, "Normal_Color", this.NormalColor));
					}
				}

				if (_autoClose)
					this.UpdateStatus();
			}

			// UPDATE STATUS - Update the pressurization status of vent's custom data, and closed doors if status has changed. 
			public void UpdateStatus()
			{
				IMyAirVent airVent = this.Vents[0];
				string oldStatus = GetKey(airVent, "Status", "0");
				float o2Level;
				if (!float.TryParse(oldStatus, out o2Level))
					o2Level = 0;

				if (this.Type == "Room")
				{
					if (Math.Abs(o2Level - airVent.GetOxygenLevel()) > THRESHHOLD)
						this.CloseDoors();
				}

				SetKey(airVent, "Status", airVent.GetOxygenLevel().ToString());
			}

			// CLOSE DOORS
			public void CloseDoors()
			{
				if (this.Doors.Count < 1)
					return;

				foreach (IMyDoor myDoor in this.Doors)
				{
					myDoor.CloseDoor();
				}
			}

			// GET EXTERIOR BULKHEAD - Returns bulkhead between this sector and exterior
			public Bulkhead GetExteriorBulkhead()
            {
				if(this.Bulkheads.Count > 0)
                {
					// Return null if exterior vent.
					if (this.Tag == _vacTag)
					{
						_statusMessage = "Command not applicable to Exterior";
						return null;
					}

					foreach (Bulkhead bulkhead in this.Bulkheads)
                    {
						if (bulkhead.TagB == _vacTag || bulkhead.TagA == _vacTag)
							return bulkhead;
                    }
                }
			
				_statusMessage = "Can't find exterior bulkhead for " + this.Tag + "!";
				return null;
            }
		}


		// BULKHEAD //   Class for barrier between sectors, which has doors and can also have LCD surfaces.
		public class Bulkhead
		{
			public List<IMyDoor> Doors;
			public List<IMyTextSurfaceProvider> LCDs;
			public List<IMyTextSurface> Surfaces;
			public List<bool> LcdOrientations; // Bool list assigning whether LCDs are vertical.
			public bool Override; // If True, Bulkhead ignores pressure checks and is always unlocked.

			// Variables for sectors separated by bulkhead.
			public Sector SectorA;
			public IMyAirVent VentA;
			public string TagA;

			public Sector SectorB;
			public IMyAirVent VentB;
			public string TagB;

			// Constructor - Door required
			public Bulkhead(IMyDoor myDoor)
			{
				this.Doors = new List<IMyDoor>();
				this.LCDs = new List<IMyTextSurfaceProvider>();
				this.Surfaces = new List<IMyTextSurface>();
				this.LcdOrientations = new List<bool>();
				this.Doors.Add(myDoor);
				this.Override = false;

				// Get tags from door name
				string[] tags = MultiTags(myDoor.CustomName);
				this.TagA = tags[0];
				this.TagB = tags[1];

				this.SectorA = GetSector(TagA);
				this.SectorB = GetSector(TagB);
				this.VentA = this.SectorA.Vents[0];
				this.VentB = this.SectorB.Vents[0];
			}

			// MONITOR - Checks pressure difference between sectors and bulkhead override status and locks/unlocks accordingly.
			public void Monitor()
			{
				if (this.SectorA == null || this.SectorB == null)
					return;

				float pressureA = this.VentA.GetOxygenLevel();
				float pressureB = this.VentB.GetOxygenLevel();
				this.Override = ParseBool(GetKey(this.Doors[0], "Override", "false"));

				if (Math.Abs(pressureA - pressureB) < THRESHHOLD)
				{
					foreach (IMyDoor door in this.Doors)
						CheckDoor(door, true);
				}
				else
				{
					foreach (IMyDoor door in this.Doors)
						CheckDoor(door, false);
				}

				this.DrawGauges();
			}

			// Set Override - Set's override status and updates custom data.
			public void SetOverride(bool overrided)
			{
				this.Override = overrided;
				foreach(IMyDoor door in this.Doors)
					SetKey(door, "Override", overrided.ToString());
			}

			// Open - openAll variable determines if doors with AutoOpen set to false are also opened.
			public void Open(bool openAll)
			{
				foreach (IMyDoor myDoor in this.Doors)
				{
					bool auto = ParseBool(GetKey(myDoor, "AutoOpen", "true"));
					if(auto || openAll)
                    {
						myDoor.OpenDoor();
					}
				}
			}

			// Draw Gauges - Calls DrawGauge with side parameters for all LCD displays in Bulkhead
			public void DrawGauges()
			{
				if (this.Surfaces.Count < 1)
					return;

				bool locked = !this.Doors[0].IsWorking;

				for(int i = 0; i < this.LCDs.Count; i++) {
					IMyTerminalBlock lcd = this.LCDs[i] as IMyTerminalBlock;
					IMyTextSurface surface = this.Surfaces[i];
					bool vertical = this.LcdOrientations[i];

					string side = GetKey(lcd, "Side", "Select A or B");

					if(side == "A")
						DrawGauge(surface, this.SectorA, this.SectorB, locked, vertical);
					else if(side == "B")
						DrawGauge(surface, this.SectorB, this.SectorA, locked, vertical);
				}
			}
		}


		// PROGRAM /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public Program()
		{
			// Set Global Variables
			_previosCommand = "NEWLY LOADED";
			_statusMessage = "";
			_currentSector = 0;
			_breatherStep = 0;
			_breatherLength = _breather.Length;

			_backgroundColor = new Color(BG_RED, BG_GREEN, BG_BLUE);
			_textColor = new Color(TEXT_RED, TEXT_GREEN, TEXT_BLUE);
			_roomColor = new Color(ROOM_RED, ROOM_GREEN, ROOM_BLUE);

			_gridID = GetKey(Me, "Grid_ID", Me.CubeGrid.EntityId.ToString());
			//_autoCheck = true;
			
			// Assign all correctly named components to sectors.
			Build();

			// Check program block's custom data for Refresh_Rate and set UpdateFrequency accordingly.
			string updateFactor = GetKey(Me, "Refresh_Rate", "10");
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
				//case "0":
				//	Runtime.UpdateFrequency = UpdateFrequency.None;
				//	_autoCheck = false;
				//	break;
				default:
					Runtime.UpdateFrequency = UpdateFrequency.Update10;
					//_autoCheck = false;
					//Echo("'Refresh_Rate' value unrecognized. Refresh disabled.\n- Please check value and recompile. -");
					break;
			}
			/*
            if (!_autoCheck)
            {
				_breather = new string[] { "///////////////" };
				_breatherLength = _breather.Length;

				Echo("REFRESH RATE set to 0: Event Trigger.  Please set Vent Actions to trigger script commands." +
					"\nRefresh Rate can be set in Custom Data to 1, 10, or 100 to check a new sector every 1, 10, or 100 ticks.");
			}
			*/	
		}


		// SAVE ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void Save() { }


		// MAIN /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void Main(string arg)
		{
			// Print basic terminal output
			UpdateOverview();
			Echo(_overview);
			UpdateMonitors();

			// Check for vents to run script from
			if (_vents.Count < 1)
			{
				Echo("No Vents Found to Build Network!  Please add sector tags to vent names then recompile!");
				
				//If no vents search for newly named components
				if(_breatherStep == 0)
					Build();
				
				return;
			}

			// Command Switch
			if (arg != "")
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
						SetSectorColor(cmdArg, false);
						break;
					case "SET_EMERGENCY_COLOR":
						SetSectorColor(cmdArg, true);
						break;
					case "VENT_CHECK_1": //For event triggered operation of script. Call from Vent actions, etc.
					case "VENT_CHECK_2":
						Sector mySector = GetSector(cmdArg);
						if (!UnknownSector(mySector, "Room"))
							mySector.Monitor();
						break;
					default:
						_statusMessage = "UNRECOGNIZED COMMAND: " + arg;
						break;
				}

				// Exit after command executed.
				return;
			}

			// Normal Execution - Monitor Current Sector
			_currentSector++;
			if (_currentSector >= _sectors.Count)
				_currentSector = 0;

			Sector sector = _sectors[_currentSector];
			Echo("\nCurrent Check: " + sector.Type + " " + sector.Tag);
			sector.Monitor();
		}


		// TOOL FUNCTIONS //--------------------------------------------------------------------------------------------------------------------------------

		// TAG FROM NAME // Returns Single-Sector-Tag from block's name.
		public static string TagFromName(string name)
		{
			int start = name.IndexOf(OPENER) + OPENER.Length; //Start index of tag substring
			int length = name.IndexOf(CLOSER) - start; //Length of tag

			return name.Substring(start, length);
		}


		// MULTITAGS // Returns 2 entry array of tags for blocks with multiple tags.
		public static string[] MultiTags(string name)
		{
			string bigTag = TagFromName(name);
			string[] output = bigTag.Split(SPLITTER);

			if (output.Length == 2)
				return output;

			_statusMessage = "Split Error for " + name;
			return new string[] { "ERROR", "ERROR" };
		}


		// STRIP TAG // Removes identifier characters from beginning and end of tag strings.
		static string StripTag(string tag)
		{
			char[] extra = { ' ', '[', ']', SPLITTER};
			string output = tag.Trim(extra);
			return output;
		}


		// PARSE BOOL //
		static bool ParseBool(string val)
		{
			string uVal = val.ToUpper();
			if (uVal == "TRUE" || uVal == "T" || uVal == "1")
			{
				return true;
			}

			return false;
		}


		// GET SECTOR // Returns sector with given tag.
		static Sector GetSector(string tag)
		{
			foreach (Sector sector in _sectors)
			{
				string myTag = StripTag(tag);

				if (sector.Tag == myTag)
					return sector;
			}

			return null;
		}


		// GET BULKHEAD // Returns bulkhead with given double-tag.
		Bulkhead GetBulkhead(string tag)
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


		// COLOR FROM STRING // Returns color based on comma separated RGB value.
		static Color ColorFromString(string rgb)
		{
			string[] values = rgb.Split(',');
			if (values.Length < 3)
				return Color.Black;

			byte[] outputs = new byte[3];
			for (int i = 0; i < 3; i++)
			{
				bool success = byte.TryParse(values[i], out outputs[i]);
				if (!success)
					outputs[i] = 0;
			}

			return new Color(outputs[0], outputs[1], outputs[2]);
		}


		// SET GRID ID // Updates Grid ID parameter for all designated blocks in Grid, then rebuilds the grid.
		void SetGridID(string arg)
		{
			string gridID;
			if (arg != "")
				gridID = arg;
			else
				gridID = Me.CubeGrid.EntityId.ToString();

			SetKey(Me, "Grid_ID", gridID);
			_gridID = gridID;

			List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
			GridTerminalSystem.SearchBlocksOfName(OPENER, blocks);

			if (blocks.Count < 1)
				return;

			foreach (IMyTerminalBlock block in blocks)
			{ 
				if(block.CustomName.Contains(CLOSER))
					SetKey(block, "Grid_ID", gridID);
			}

			Build();
		}


		// UNKNOWN SECTOR // - Checks if Sector exists and is valid type.  Returns true if errors detected. Type can be "Room", "Lock", or "Dock". 
		bool UnknownSector(Sector sector, string type)
        {
			if(sector == null)
            {
				_statusMessage = "INVALID SECTOR CALL!";
				return true;
            }

			if(type == "Dock" && sector.Type != "Dock")
            {
				_statusMessage = "INVALID DOCK CALL!";
				return true;
            }

			if(type == "Lock" && sector.Type != "Dock" && sector.Type != "Lock")
            {
				_statusMessage = "INVALID LOCK CALL!";
				return true;
			}

			return false;
        }


		// UPDATE OVERVIEW // Updates _overview string for terminal and monitors
		void UpdateOverview()
        {
			_overview = "PRESSURE CHIEF " + _breather[_breatherStep] + "\n--Pressure Management System--" + "\nCmd: "
				+ _previosCommand + "\n"+ _statusMessage + "\n----------------------" + "Sector Count: " + _sectors.Count;

			foreach (Sector sector in _sectors)
			{
				_overview += "\n" + sector.Type + " " + sector.Tag + " --- " + sector.Vents[0].Status.ToString()
					+"\n * Doors: " + sector.Doors.Count + "  * Lights: " + sector.Lights.Count;
				if(sector.Type == "Dock")
					_overview += "\n * Merge Blocks: " + sector.MergeBlocks.Count + "  * Connectors: " + sector.Connectors.Count;
				
			}

			_breatherStep++;
			if (_breatherStep >= _breatherLength)
				_breatherStep = 0;
		}


		// UPDATE MONITORS // Print Overview text to any blocks in the _monitors list.
		void UpdateMonitors()
        {
			if (_monitors.Count < 1)
				return;

			foreach (IMyTextSurfaceProvider monitor in _monitors)
			{
				ushort index;
				if (!UInt16.TryParse(GetKey(monitor as IMyTerminalBlock, "Index", "0"), out index))
					index = 0;

				IMyTextSurface surface = monitor.GetSurface(index);
				surface.WriteText(_overview, false);
			}
        }

		/* SET SECTOR COLOR // - Updates the default color for a sector
			* Emergency bool determines if it's normal or emergency color*/			
		void SetSectorColor(string sectorData, bool emergency)
        {
			if (sectorData != "")
            {
				string[] data = sectorData.Split(' ');

				if (data.Length > 1)
				{
					string rgbCode = data[0];
					string tag = "";

					for(int i = 1; i < data.Length; i++)
                    {
						tag += data[i] + " ";
                    }

					Sector sector = GetSector(tag.Trim());
					if (UnknownSector(sector, "Room"))
						return;

					string lightCase;
					if (emergency)
                    {
						lightCase = "Emergency_Color";
						sector.EmergencyColor = rgbCode;
					}
					else
                    {
						lightCase = "Normal_Color";
						sector.NormalColor = rgbCode;
					}
						

					SetKey(sector.Vents[0], lightCase, rgbCode);

					if (sector.Lights.Count > 0)
					{
						foreach (IMyLightingBlock light in sector.Lights)
							SetKey(light, lightCase, rgbCode);
					}

					return;
				}	
			}

			_statusMessage = "INCOMPLETE COLOR DATA!!!\nPlease Follow Format:\n<red_value>,<green_value>,<blue_value> <sector_tag>";
			return;
		}

		// LOCK & DOCK FUNCTIONS --------------------------------------------------------------------------------------------------------------------------

		/* OPEN LOCK // - Initiallize lock opening sequence
		 *bool openAll determines if non-AutoOpen doors are included in sequence. */
		void OpenLock(string tag, bool openAll) {
			string phase;
			if (openAll)
				phase = "3";
			else
				phase = "1";

			Sector sector = GetSector(tag);
			if (UnknownSector(sector, "Lock"))
				return;

			sector.CloseDoors();
			sector.Vents[0].Depressurize = true;

			StageLock(sector, phase, 1); // alert sound 1
		}


		// CLOSE LOCK // - Closes lock and restores vents to pressurized.
		void CloseLock(string tag)
		{
			Sector sector = GetSector(tag);

			if (UnknownSector(sector, "Lock"))
				return;

			sector.CloseDoors();
			sector.Vents[0].Depressurize = false;

			foreach (Bulkhead bulkhead in sector.Bulkheads)
			{
				if (bulkhead.TagB == _vacTag || bulkhead.TagA == _vacTag)
				{
					bulkhead.SetOverride(false);
				}
			}
		}


		// CYCLE LOCK // - Opens or closes lock based on locks current Override State
		void CycleLock(string tag)
        {
			Sector sector = GetSector(tag);
			if (UnknownSector(sector, "Lock"))
				return;

			Bulkhead bulkhead = sector.GetExteriorBulkhead();
			if (bulkhead == null)
				return;

			if (bulkhead.Override)
				CloseLock(tag);
			else
				OpenLock(tag, false);
		}


		// TIMER CALL // - Switch command to execute timer actions depending on its current state (recorded in Custom Data of Timer)
		void TimerCall(string tag)
		{
			Sector sector = GetSector(tag);
			if (UnknownSector(sector, "lock"))
				return;

			IMyTimerBlock timer = sector.LockTimer;
			string phase = GetKey(timer, "Phase", "0");

			Bulkhead bulkhead = sector.GetExteriorBulkhead();
			if(bulkhead == null)
            {
				_statusMessage = "CANNOT FIND EXTERIOR BULKHEAD FOR " + tag + "!\nCheck Your VAC_TAGs!";
				return;
            }

			switch (phase)
			{
				case "1": // OVERRIDE EXTERIOR BULKHEAD
					TimerOverride(timer, sector, bulkhead, "2");
					break;
				case "2": // OPEN AUTO-OPEN DOORS ONLY
					TimerOpen(timer, sector, bulkhead, false);
					break;
				case "3":// OVERRIDE EXTERIOR BULKHEAD
					TimerOverride(timer, sector, bulkhead, "4");
					break;
				case "4":
					TimerOpen(timer, sector, bulkhead, true);
					break;
				case "5": // OVERRIDE SELF AND DOCKED PORT
					SetDockedOverride(sector, true);
					TimerOverride(timer, sector, bulkhead, "6");
					break;
				case "6": // OPEN SELF
					bulkhead.Open(false);
					_statusMessage = "Dock " + sector.Tag + " is sealed.";
					SetKey(timer, "Phase", "0"); // Consider Removing for HATCH call
					break;
				case "7": // DISENGAGE CONNECTIONS
					ActivateDock(sector, false);
					SetKey(timer, "Phase", "8");
					timer.TriggerDelay = 10;
					timer.StartCountdown();
					_statusMessage = "Dock " + sector.Tag + " disengaged.";
					break;
				case "8": // RE-ENGAGE CONNECTIONS & RESET
					ActivateDock(sector, true);
					SetKey(timer, "Phase", "0");
					_statusMessage = "Dock " + sector.Tag + " re-enabled.";
					break;
				default:
					_statusMessage = sector.Tag + " timer phase: " + phase;
					break;
			}
		}


		// TIMER OVERRIDE // - First Timer Action overrides lock.
		void TimerOverride(IMyTimerBlock timer, Sector sector, Bulkhead bulkhead, string phase)
		{
			_statusMessage = "Overriding Lock " + sector.Tag;
			SetKey(timer, "Phase", phase);
			timer.TriggerDelay = 1;
			timer.StartCountdown();
			bulkhead.SetOverride(true);

			if(phase == "6")
            {
				foreach (IMyDoor door in bulkhead.Doors)
				{
					bool autoOpen = ParseBool(GetKey(door, "AutoOpen", "true"));
					if (!autoOpen)
					{
						SetKey(door, "Override", "false");
					}
				}
			}

			sector.Monitor();
		}


		// TIMER OPEN // - Second Timer Call - opens overriden doors.  Set openAll to true to open normally disabled doors.
		void TimerOpen(IMyTimerBlock timer, Sector sector, Bulkhead bulkhead, bool openAll)
		{
			bulkhead.Open(openAll);
			SetKey(timer, "Phase", "0");
			_statusMessage = sector.Type + " " + sector.Tag + " opened.";
			sector.Monitor();
		}


		// DOCK SEAL // - Attempts to lock dock connectors to other dock.
		void DockSeal(Sector sector)
        {
			if (UnknownSector(sector, "Dock"))
				return;

			foreach (IMyShipConnector connector in sector.Connectors)
				connector.Connect();

			StageLock(sector, "5", 2); // phase "3", alert 2
		}


		// UNDOCK // - Close dock, get other dock, close and reset override, then start timer to separate.
		void Undock(Sector sector)
        {
			if (UnknownSector(sector, "Dock"))
				return;

			sector.CloseDoors();
			CloseLock(sector.Tag);

			List<IMyDoor> dockedDoors = GetDockedDoors(sector);

			SetDockedOverride(sector, false);
			//foreach (IMyDoor door in dockedDoors)
			//	door.CloseDoor();

			StageLock(sector, "7", 1);
        }


		// GET DOCKED DOORS// - Get doors of a connected dock and return as list.
		List<IMyDoor> GetDockedDoors(Sector sector)
		{
			List<IMyDoor> docked = new List<IMyDoor>();
			if (sector.Type != "Dock")
			{
				_statusMessage = sector.Tag + " CANNOT DOCK!\nReason: Not a Docking Port";
				return docked;
			}

			if (sector.MergeBlocks.Count == 0 || sector.LockTimer == null)
			{
				_statusMessage = sector.Tag + " CANNOT DOCK!\nReason: Missing Components";
				return docked;
			}

			// Check for connection
			IMyShipMergeBlock mergeA = null;
			bool unmerged = true;
			int i = 0;

			while (unmerged && i < sector.MergeBlocks.Count)
			{
				if (sector.MergeBlocks[i].IsConnected)
				{
					mergeA = sector.MergeBlocks[i];
					unmerged = false;
				}
				i++;
			}

			if (mergeA == null)
			{
				_statusMessage = sector.Tag + " CANNOT DOCK!\nReason: Not Connected";
				return docked;
			}

			IMyShipMergeBlock mergeB = GetMergedBlock(mergeA);
			if (mergeB == null)
			{
				_statusMessage = sector.Tag + ": Dock Connection Not Found";
				return docked;
			}

			string dockTag = TagFromName(mergeB.CustomName);
			List<IMyTerminalBlock> doors = new List<IMyTerminalBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors);
			if (doors.Count < 1)
				return docked;

			foreach (IMyTerminalBlock door in doors)
			{
				if (door.CustomName.Contains(dockTag) && door.CustomName.Contains(_vacTag) && GetKey(door, "Grid_ID", "unspecified") != _gridID)
					docked.Add(door as IMyDoor);
			}

			return docked;
		}


		// GET DOCKED VENTS // - Returns list of vents in connected Docking Port
		List<IMyAirVent> GetDockedVents(IMyDoor dockedDoor)
        {
			string gridID = GetKey(dockedDoor, "Grid_ID", "");
			string[] tags = MultiTags(dockedDoor.CustomName);

			List<IMyAirVent> vents = new List<IMyAirVent>();
			GridTerminalSystem.GetBlocksOfType<IMyAirVent>(vents);
			if (vents.Count < 1 || tags.Length < 2)
				return vents;

			List<IMyAirVent> dockedVents = new List<IMyAirVent>();
			foreach(IMyAirVent vent in vents)
            {
				string ventGrid = GetKey(vent, "Grid_ID", "");
				string ventTag = TagFromName(vent.CustomName);

				if (ventGrid == gridID && (ventTag == tags[0] || ventTag == tags[1]))
				{
					dockedVents.Add(vent);
				}
            }

			return dockedVents;
		}


		// GET CONNECTED MERGE BLOCK // -  Attempts to get nearest connected merge block that has a different GridID
		IMyShipMergeBlock GetMergedBlock(IMyShipMergeBlock mergeA)
		{
			Vector3 pos = mergeA.GetPosition();
			float nearest = 10;

			IMyShipMergeBlock mergeB = null;

			List<IMyShipMergeBlock> mergeBlocks = new List<IMyShipMergeBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(mergeBlocks);
			foreach (IMyShipMergeBlock mergeBlock in mergeBlocks)
			{
				if (mergeBlock.IsConnected && GetKey(mergeBlock, "Grid_ID", "") != GetKey(Me, "Grid_ID", _gridID))
				{
					float distance = Vector3.Distance(mergeBlock.GetPosition(), pos);
					if (distance < nearest)
					{
						nearest = distance;
						mergeB = mergeBlock;
					}
				}
			}

			return mergeB;
		}


		// STAGE LOCK // - Executes various repeated functions for Timer Calls
		void StageLock(Sector sector, string phase, int alert)
        {
			IMyTimerBlock timer = sector.LockTimer;
			UInt32 delay;
			SetKey(timer, "Phase", phase);

			if (UInt32.TryParse(GetKey(timer, "Delay", DELAY.ToString()), out delay))
				delay--;
			else
				delay = DELAY - 1;

			if (delay < 1)
				delay = 1;

			timer.TriggerDelay = delay;
			timer.StartCountdown();

			if (sector.LockAlarm != null)
			{
				IMySoundBlock alarm = sector.LockAlarm;
				bool autoSound = ParseBool(GetKey(alarm, "Auto-Sound-Select", "true"));

				if(autoSound)
					alarm.SelectedSound = "SoundBlockAlert" + alert;
				
				alarm.LoopPeriod = DELAY;
				alarm.Play();
			}
		}


		// SET DOCKED OVERIDE // - Unlocks doors in connected docking port.
		void SetDockedOverride(Sector sector, bool overriding)
		{
			List<IMyDoor> dockedDoors = GetDockedDoors(sector);
			if (dockedDoors.Count < 1)
				return;

			foreach (IMyDoor door in dockedDoors)
			{
				if (overriding)
                {
					bool AutoOpen = ParseBool(GetKey(door, "AutoOpen", "true"));
					if(AutoOpen)
						SetKey(door, "Override", "true");
					else
						SetKey(door, "Override", "false");
				}
				else
                {
					SetKey(door, "Override", "false");
					door.CloseDoor();
				}
			}

			// Get list of vents in docked sector and set depressurization to false.
			List<IMyAirVent> dockedVents = GetDockedVents(dockedDoors[0]);
			if (dockedVents.Count < 1)
				return;

			foreach (IMyAirVent vent in dockedVents)
			{
				vent.Depressurize = false;
			}
		}


		// ACTIVATE DOCK // - Turns dock merge blocks on or off, and attempts to connect any possible connectors.
		void ActivateDock(Sector sector, bool activate)
		{
			string action = "OnOff_Off";
			if (activate)
				action = "OnOff_On";
				

			if (sector.MergeBlocks.Count < 1)
				return;

			foreach (IMyShipMergeBlock mergeBlock in sector.MergeBlocks)
				mergeBlock.GetActionWithName(action).Apply(mergeBlock);

			if (sector.Connectors.Count > 0)
			{
				foreach (IMyShipConnector connector in sector.Connectors)
				{
					if (connector.Status != MyShipConnectorStatus.Connected)
						connector.GetActionWithName("SwitchLock").Apply(connector);

					connector.GetActionWithName(action).Apply(connector);
				}
			}
		}


		// CHECK DOOR // - Power/Depower Door based on if it's EQUALIZED or OVERRIDEN
		public static void CheckDoor(IMyDoor door, bool equalized)
        {
			bool doorOverride = ParseBool(GetKey(door, "Override", "false"));

			if (equalized || doorOverride)
				door.GetActionWithName("OnOff_On").Apply(door);
			else
				door.GetActionWithName("OnOff_Off").Apply(door);
		}


		// SPRITE FUNCTIONS --------------------------------------------------------------------------------------------------------------------------------

		// DRAW GAUGE // - Draws the pressure display between room the lcd is locate in and the neighboring room.
		static void DrawGauge(IMyTextSurface drawSurface, Sector sectorA, Sector sectorB, bool locked, bool vertical) 
		{
			RectangleF viewport = new RectangleF((drawSurface.TextureSize - drawSurface.SurfaceSize) / 2f, drawSurface.SurfaceSize);

			float pressureA = sectorA.Vents[0].GetOxygenLevel();
			float pressureB = sectorB.Vents[0].GetOxygenLevel();

			// Set color of status frame.
			Color statusColor;
			if (locked)
				statusColor = Color.Red;
			else
				statusColor = Color.Green;


			var frame = drawSurface.DrawFrame();

			float height = drawSurface.SurfaceSize.Y;
			float width = viewport.Width;
			float textSize = 0.8f;
			float topEdge = viewport.Center.Y - viewport.Height/2;
			float bottomEdge = viewport.Center.Y + viewport.Height / 2;
			if (width < SCREEN_THRESHHOLD)
				textSize = 0.4f;

			//Vector2 position;// = viewport.Center - new Vector2(width/2, 0);

			int redA = (int) (PRES_RED * (1-pressureA));
			int greenA = (int)(PRES_GREEN * pressureA);
			int blueA = (int)(PRES_BLUE * pressureA);
			int redB = (int)(PRES_RED * (1-pressureB));
			int greenB = (int)(PRES_GREEN * pressureB);
			int blueB = (int)(PRES_BLUE * pressureB);

			//Variables for position alignment and scale
			Vector2 leftPos, leftScale, leftTextPos, rightPos, rightTextPos, rightScale, leftReadingOffset, gridScale, position;
			TextAlignment leftChamberAlignment, rightChamberAlignment;

			if(vertical)
            {
				leftPos = new Vector2(0, bottomEdge - height * pressureA * 0.5f);
				leftScale = new Vector2(width * 0.425f, height * pressureA);
				leftTextPos = new Vector2(width*0.22f, topEdge);
				rightPos = new Vector2(width, bottomEdge - height * pressureB * 0.5f);
				rightScale = new Vector2(-width * 0.425f, height * pressureB);
				rightTextPos = new Vector2(width * 0.78f, topEdge);
				leftReadingOffset = new Vector2(0, textSize * 25);
				gridScale = new Vector2(width * 20, height);
				leftChamberAlignment = TextAlignment.CENTER;
				rightChamberAlignment = TextAlignment.CENTER;
			}
            else
            {
				leftPos = new Vector2(0, viewport.Center.Y);
				leftScale = new Vector2(width * 0.425f * pressureA, height);
				leftTextPos = new Vector2(textSize * 10, topEdge);
				rightPos = new Vector2(width, viewport.Center.Y);
				rightScale = new Vector2(-width * 0.425f * pressureB, height);
				rightTextPos = new Vector2(width - textSize * 10, topEdge);
				leftReadingOffset = new Vector2(textSize * 10, textSize * 25);
				gridScale = new Vector2(width, height * 20);
				leftChamberAlignment = TextAlignment.LEFT;
				rightChamberAlignment = TextAlignment.RIGHT;
			}

			// Left Chamber
			DrawTexture("SquareSimple", leftPos, leftScale, 0, new Color(redA, greenA, blueA), frame);
			WriteText("*" + sectorA.Tag, leftTextPos, leftChamberAlignment, textSize, _roomColor, frame);
			leftTextPos += leftReadingOffset;
			WriteText((string.Format("{0:0.##}", (pressureA * _atmo * _factor))) + _unit, leftTextPos, leftChamberAlignment, textSize * 0.75f, _textColor, frame);

			// Right Chamber
			DrawTexture("SquareSimple", rightPos, rightScale, 0, new Color(redB, greenB, blueB), frame);
			WriteText(sectorB.Tag, rightTextPos, rightChamberAlignment, textSize, _textColor, frame);
			rightTextPos += new Vector2(0, textSize * 25);
			WriteText((string.Format("{0:0.##}", (pressureB * _atmo * _factor))) + _unit, rightTextPos, rightChamberAlignment, textSize * 0.75f, _textColor, frame);

			// Grid Texture
			position = new Vector2(0, viewport.Center.Y);
			DrawTexture("Grid", position, gridScale, 0, Color.Black, frame);
			position += new Vector2(1, 0);
			DrawTexture("Grid", position, new Vector2(width, height * 20), 0, Color.Black, frame);

			// Status Frame
			position = viewport.Center - new Vector2(width * 0.075f, 0);
			DrawTexture("SquareSimple", position, new Vector2(width*0.15f, height), 0, statusColor, frame);

			// Door Background
			position = viewport.Center - new Vector2(width * 0.0625f, 0);
			DrawTexture("SquareSimple", position, new Vector2(width*0.125f, height * 0.95f), 0, Color.Black, frame);

			// Door Status
			if (locked)
			{
				position = viewport.Center - new Vector2(width * 0.04f,0);
				DrawTexture("Cross", position, new Vector2(width * 0.08f, width * 0.08f), 0, Color.White, frame);
			}
			else
			{
				position = viewport.Center - new Vector2(width * 0.06f,0);
				DrawTexture("Arrow", position, new Vector2(width * 0.08f, width * 0.08f), (float) Math.PI/-2, Color.White, frame);
				position += new Vector2(width * 0.04f, 0);
				DrawTexture("Arrow", position, new Vector2(width * 0.08f, width * 0.08f), (float)Math.PI/2, Color.White, frame);
			}

			frame.Dispose();
		}


		// DRAW TEXTURE //
		static void DrawTexture(string shape, Vector2 position, Vector2 scale, float rotation, Color color, MySpriteDrawFrame frame)
		{
			MySprite sprite = new MySprite() {
				Type = SpriteType.TEXTURE,
				Data = shape,
				Position = position,
				RotationOrScale = rotation,
				Size = scale,
				Color = color
			};

			frame.Add(sprite);
		}


		// WRITE TEXT //
		static void WriteText(string text, Vector2 position, TextAlignment alignment, float scale, Color color, MySpriteDrawFrame frame)
		{
			var sprite = new MySprite()
			{
				Type = SpriteType.TEXT,
				Data = text,
				Position = position,
				RotationOrScale = scale,
				Color = color,
				Alignment = alignment,
				FontId = "White"
			};
			frame.Add(sprite);
		}


		// INIT FUNCIONS -----------------------------------------------------------------------------------------------------------------------------------

		// BUILD // Searches grid for all components and adds them to current run.
		void Build()
		{
			_vents = new List<IMyAirVent>();
			_doors = new List<IMyDoor>();
			_lcds = new List<IMyTextPanel>();
			_buttons = new List<IMyButtonPanel>();
			_lockAlarms = new List<IMySoundBlock>();
			_lockTimers = new List<IMyTimerBlock>();
			_connectors = new List<IMyShipConnector>();
			_mergeBlocks = new List<IMyShipMergeBlock>();
			_lights = new List<IMyLightingBlock>();
			_sectors = new List<Sector>();
			_bulkheads = new List<Bulkhead>();
			_monitors = new List<IMyTerminalBlock>();
			

			_vacTag = GetKey(Me, "Vac_Tag", VAC_TAG);

			// Central displays for overview data
			_monitorTag = GetKey(Me, "Monitor_Tag", MONITOR_TAG);
			GridTerminalSystem.SearchBlocksOfName(_monitorTag, _monitors);

			//Set Pressure Unit as well as Atmospheric and User-Defined Factors
			_unit = GetKey(Me, "Unit", UNIT);
			if (float.TryParse(GetKey(Me, "Factor", "1"), out _factor))
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

			//_autoCheck = ParseBool(GetKey(Me, "Auto-Check", "true"));
			_autoClose = ParseBool(GetKey(Me, "Auto-Close", "true"));

			List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
			GridTerminalSystem.SearchBlocksOfName(OPENER, blocks);

			if (blocks.Count > 0)
			{
				foreach (IMyTerminalBlock block in blocks)
				{
					if (GetKey(block, "Grid_ID", _gridID) == GetKey(Me, "Grid_ID", _gridID))
					{
						switch (block.BlockDefinition.TypeIdString)
						{
							case "MyObjectBuilder_AirVent":
								_vents.Add(block as IMyAirVent);
								break;
							case "MyObjectBuilder_Door":
							case "MyObjectBuilder_AirtightSlideDoor":
							case "MyObjectBuilder_AirtightHangarDoor":
								_doors.Add(block as IMyDoor);
								break;
							case "MyObjectBuilder_TextPanel":
								_lcds.Add(block as IMyTextPanel);
								break;
							case "MyObjectBuilder_ButtonPanel":
								if (block.BlockDefinition.SubtypeId == "LargeSciFiButtonTerminal" || block.BlockDefinition.SubtypeId == "LargeSciFiButtonPanel")
									_buttons.Add(block as IMyButtonPanel);
								break;
							case "MyObjectBuilder_SoundBlock":
								_lockAlarms.Add(block as IMySoundBlock);
								break;
							case "MyObjectBuilder_TimerBlock":
								_lockTimers.Add(block as IMyTimerBlock);
								break;
							case "MyObjectBuilder_ShipConnector":
								_connectors.Add(block as IMyShipConnector);
								break;
							case "MyObjectBuilder_MergeBlock":
								_mergeBlocks.Add(block as IMyShipMergeBlock);
								break;
							case "MyObjectBuilder_InteriorLight":
							case "MyObjectBuilder_ReflectorLight":
								_lights.Add(block as IMyLightingBlock);
								break;
						}
					}
				}

				foreach (IMyAirVent vent in _vents)
				{

					Sector sector = GetSector(TagFromName(vent.CustomName));

					if (sector == null)
					{
						sector = new Sector(vent);
						_sectors.Add(sector);
					}
					else
					{
						sector.Vents.Add(vent);
					}
				}

				if (_sectors.Count < 1 || _doors.Count < 1)
					return;

				// Assign double-tagged components
				AssignDoors();
				AssignLCDs();
				AssignButtons();

				// Assign single-tagged components
				if (_lights.Count > 0)
				{
					foreach (IMyLightingBlock light in _lights)
						AssignLight(light);
				}

				if (_lockTimers.Count > 0)
				{
					foreach (IMyTimerBlock timer in _lockTimers)
						AssignTimer(timer);
				}

				if (_lockAlarms.Count > 0)
				{
					foreach (IMySoundBlock alarm in _lockAlarms)
						AssignAlarm(alarm);
				}

				if (_mergeBlocks.Count > 0)
				{
					foreach (IMyShipMergeBlock mergeBlock in _mergeBlocks)
						AssignMergeBlock(mergeBlock);
				}

				if (_connectors.Count > 0)
				{
					foreach (IMyShipConnector connector in _connectors)
						AssignConnector(connector);
				}
			}
		}


		// ASSIGN DOORS // Add doors to respective lists in known Sector and Bulkhead objects.
		void AssignDoors()
		{
			if(_doors.Count < 1)
            {
				Echo("NO DOORS FOUND!");
				return;
            }

			foreach (IMyDoor door in _doors)
			{
				string[] tags = MultiTags(door.CustomName);
				Bulkhead bulkhead = GetBulkhead(tags[0] + SPLITTER + tags[1]);
				if (bulkhead == null)
				{
					bulkhead = new Bulkhead(door);
					_bulkheads.Add(bulkhead);
				}
				else
				{
					bulkhead.Doors.Add(door);
				}

				if (tags[0] == _vacTag || tags[1] == _vacTag)
					EnsureKey(door, "AutoOpen", "true");

				bulkhead.Override = ParseBool(GetKey(door, "Override", "false"));
				SetKey(door, "Vent_A", "");
				SetKey(door, "Vent_B", "");

				foreach (Sector sector in _sectors)
				{
					if (sector.Tag == tags[0])
					{
						sector.Doors.Add(door);
						sector.Bulkheads.Add(bulkhead);
						bulkhead.SectorA = sector;
						bulkhead.VentA = sector.Vents[0];
						SetKey(door, "Vent_A", sector.Vents[0].CustomName);
					}
					else if (sector.Tag == tags[1])
					{
						sector.Doors.Add(door);
						sector.Bulkheads.Add(bulkhead);
						bulkhead.SectorB = sector;
						bulkhead.VentB = sector.Vents[0];
						SetKey(door, "Vent_B", sector.Vents[0].CustomName);
					}
				}

				if (bulkhead.SectorA == null)
					_statusMessage += "\nDOOR ERROR: " + door.CustomName + "\nNo Sector Found with tag " + tags[0];
				else if (bulkhead.SectorB == null)
					_statusMessage += "\nDOOR ERROR: " + door.CustomName + "\nNo Sector Found with tag " + tags[1];
				//else
				//	_bulkheads.Add(bulkhead);
			}
		}


		// ASSIGN LCDs // Add lcds to respective lists in known Bulkhead objects.
		void AssignLCDs()
		{
			if (_lcds.Count < 1)
				return;

			foreach (IMyTextPanel lcd in _lcds)
			{
				string[] tags = MultiTags(lcd.CustomName);
				string tag = tags[0] + SPLITTER + tags[1];
				string reverseTag = tags[1] + SPLITTER + tags[0];

				foreach (Bulkhead bulkhead in _bulkheads)
				{
					string subtype = lcd.BlockDefinition.SubtypeId;
					string doorName = bulkhead.Doors[0].CustomName;
					if (doorName.Contains(tag))
					{
						SetKey(lcd, "Side", "A");
						if (subtype.Contains("Corner_LCD"))
							EnsureKey(lcd, "Vertical", "False");
						else
							EnsureKey(lcd, "Vertical", "True");
						bulkhead.LCDs.Add(lcd as IMyTextSurfaceProvider);
						bulkhead.Surfaces.Add(PrepareTextSurface(lcd as IMyTextSurfaceProvider));
						bulkhead.LcdOrientations.Add(ParseBool(GetKey(lcd, "Vertical", "False")));
					}
					else if (doorName.Contains(reverseTag))
					{
						SetKey(lcd, "Side", "B");
						if (subtype.Contains("Corner_LCD"))
							EnsureKey(lcd, "Vertical", "False");
						else
							EnsureKey(lcd, "Vertical", "True");
						bulkhead.LCDs.Add(lcd as IMyTextSurfaceProvider);
						bulkhead.Surfaces.Add(PrepareTextSurface(lcd as IMyTextSurfaceProvider));
						bulkhead.LcdOrientations.Add(ParseBool(GetKey(lcd, "Vertical", "False")));
					}
				}
			}
		}


		// ASSIGN BUTTONS // Add buttons to lcd lists in known Bulkhead objects.
		void AssignButtons()
        {
			if (_buttons.Count < 1)
				return;

			foreach (IMyButtonPanel button in _buttons)
			{
				string[] tags = MultiTags(button.CustomName);
				string tag = tags[0] + SPLITTER + tags[1];
				string reverseTag = tags[1] + SPLITTER + tags[0];

				foreach (Bulkhead bulkhead in _bulkheads)
				{
					string doorName = bulkhead.Doors[0].CustomName;
					if (doorName.Contains(tag))
					{
						SetKey(button, "Side", "A");
						EnsureKey(button, "Screen_Index", "0");
						EnsureKey(button, "Vertical", "True");
						bulkhead.LCDs.Add(button as IMyTextSurfaceProvider);
						bulkhead.Surfaces.Add(PrepareTextSurface(button as IMyTextSurfaceProvider));
						bulkhead.LcdOrientations.Add(ParseBool(GetKey(button, "Vertical", "True")));
					}
					else if (doorName.Contains(reverseTag))
					{
						SetKey(button, "Side", "B");
						EnsureKey(button, "Screen_Index", "0");
						EnsureKey(button, "Vertical", "True");
						bulkhead.LCDs.Add(button as IMyTextSurfaceProvider);
						bulkhead.Surfaces.Add(PrepareTextSurface(button as IMyTextSurfaceProvider));
						bulkhead.LcdOrientations.Add(ParseBool(GetKey(button, "Vertical", "True")));
					}
				}
			}
		}

		// ASSIGN LIGHT // Add lights to light lists in designated sectors.
		void AssignLight(IMyLightingBlock light)
		{
			string tag = TagFromName(light.CustomName);

			foreach (Sector sector in _sectors)
			{
				if (sector.Tag == tag)
				{
					sector.Lights.Add(light);
					EnsureKey(light, "Normal_Color", light.Color.R.ToString() + "," + light.Color.G.ToString() + "," + light.Color.B.ToString());
					EnsureKey(light, "Emergency_Color", "255,0,0");

					return;
				}
			}
		}


		// ASSIGN TIMER // Add timers to their designated sectors.
		void AssignTimer(IMyTimerBlock timer)
		{
			string tag = TagFromName(timer.CustomName);

			foreach (Sector sector in _sectors)
			{
				if (sector.Tag == tag)
				{
					string delayString = GetKey(timer, "Delay", "5");
					UInt16 delay;

					if (UInt16.TryParse(delayString, out delay))
						timer.TriggerDelay = delay;
					else
						timer.TriggerDelay = 5;

					sector.LockTimer = timer;
					sector.Type = "Lock"; //Set Sector Type to Lock if a Timer is present
					return;
				}
			}
		}


		// ASSIGN ALARM // Add alarms to their designated sectors.
		void AssignAlarm(IMySoundBlock alarm)
		{
			string tag = TagFromName(alarm.CustomName);

			foreach (Sector sector in _sectors)
			{
				if (sector.Tag == tag)
				{
					sector.LockAlarm = alarm;
					return;
				}
			}
		}


		// ASSIGN MERGE BLOCK // Add merge blocks to their designated docks.
		void AssignMergeBlock(IMyShipMergeBlock mergeBlock)
		{
			string tag = TagFromName(mergeBlock.CustomName);

			foreach (Sector sector in _sectors)
			{
				if (sector.Tag == tag)
				{
					sector.MergeBlocks.Add(mergeBlock);
					sector.Type = "Dock"; //Set Sector Type to Dock if Merge Blocks are present.
					return;
				}
			}
		}


		// ASSIGN CONNECTOR // Add connectors to their designated docks.
		void AssignConnector(IMyShipConnector connector)
		{
			string tag = TagFromName(connector.CustomName);

			foreach (Sector sector in _sectors)
			{
				if (sector.Tag == tag)
				{
					sector.Connectors.Add(connector);
					return;
				}
			}
		}


		// INI FUNCTIONS -----------------------------------------------------------------------------------------------------------------------------------


		// ENSURE KEY // Check to see if INI key exists, and if it doesn't write with default value.
		static void EnsureKey(IMyTerminalBlock block, string key, string defaultVal)
		{
			if (!block.CustomData.Contains(INI_HEAD) || !block.CustomData.Contains(key))
				SetKey(block, key, defaultVal);
		}


		// GET KEY // Gets ini value from block.  Returns default argument if doesn't exist.
		static string GetKey(IMyTerminalBlock block, string key, string defaultVal)
		{
			EnsureKey(block, key, defaultVal);
			MyIni blockIni = GetIni(block);
			return blockIni.Get(INI_HEAD, key).ToString();
		}


		// SET KEY // Update ini key for block, and write back to custom data.
		static void SetKey(IMyTerminalBlock block, string key, string arg)
		{
			MyIni blockIni = GetIni(block);
			blockIni.Set(INI_HEAD, key, arg);
			block.CustomData = blockIni.ToString();
		}


		// GET INI // Get entire INI object from specified block.
		static MyIni GetIni(IMyTerminalBlock block)
		{
			MyIni iniOuti = new MyIni();

			MyIniParseResult result;
			if (!iniOuti.TryParse(block.CustomData, out result)) {
				block.CustomData = "---\n" + block.CustomData;
				if (!iniOuti.TryParse(block.CustomData, out result))
					throw new Exception(result.ToString());
			}
				
			return iniOuti;
		}


		// BORROWED FUNCTIONS --------------------------------------------------------------------------------------------------------------

		// PREPARE TEXT SURFACE
		public IMyTextSurface PrepareTextSurface(IMyTextSurfaceProvider lcd)
		{
			byte index = 0;
			if (lcd.SurfaceCount > 1)
			{
				if (!Byte.TryParse(GetKey(lcd as IMyTerminalBlock, "Screen_Index", "0"), out index) || index >= lcd.SurfaceCount)
				{
					index = 0;
					_statusMessage = "Invalid 'Screen_Index' value in block " + (lcd as IMyTerminalBlock).CustomName;
				}
			}
			IMyTextSurface textSurface = lcd.GetSurface(index);

			// Set the sprite display mode
			textSurface.ContentType = ContentType.SCRIPT;
			// Make sure no built-in script has been selected
			textSurface.Script = "";

			// Set Background Color to black
			textSurface.ScriptBackgroundColor = Color.Black;


			return textSurface;
		}



		// END HERE ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	}
}