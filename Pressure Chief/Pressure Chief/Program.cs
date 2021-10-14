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



		// START HERE ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////




		//////////////////////////////////
		//        PRESSURE CHIEF			  	          //
		// Pressure management script 	//
		//                 Author: SJ_Omega		 		//
		//////////////////////////// v1.0

		// USER CONSTANTS -  Feel free to change as needed -------------------------------------------------------------------------------------------------
		const string VAC_TAG = "XoX"; // Tag used to designate External reference vents (i.e. Vacuum vents).
		
		// Background Colors // - RGB values for LCD background
		const int BG_RED = 127;
		const int BG_GREEN = 127;
		const int BG_BLUE = 127;

		// Pressurized colors // - RGB values for pressurized chambers
		const int PRES_RED = 0;
		const int PRES_GREEN = 4;
		const int PRES_BLUE = 16;

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


		// Globals //
		static string _statusMessage;
		static string _previosCommand;
		static string _gridID;
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
										"//////////////\\",};
		static int _breatherStep;
		static int _breatherLength;

		int _currentSector;
		static bool _autoCheck;
		static bool _autoClose;

		static Color _backgroundColor;
		static Color _textColor;
		static Color _roomColor;
		

		static List<IMyAirVent> _vents;
		static List<IMyDoor> _doors;
		static List<IMyTextPanel> _lcds;
		static List<IMySoundBlock> _lockAlarms;
		static List<IMyTimerBlock> _lockTimers;
		static List<IMyShipConnector> _connectors;
		static List<IMyShipMergeBlock> _mergeBlocks;
		static List<IMyLightingBlock> _lights;
		static List<Sector> _sectors;
		static List<Bulkhead> _bulkheads;




		// CLASSES //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// SECTOR //
		public class Sector
		{

			public string Tag;
			public IMyAirVent Vent;
			public List<IMyDoor> Doors;
			public List<IMyLightingBlock> Lights;
			public List<IMyTextPanel> LCDs;
			public List<IMyShipMergeBlock> MergeBlocks;
			public List<IMyShipConnector> Connectors;
			public List<Bulkhead> Bulkheads;
			public string Type; // Room, Lock, Dock, or Vacuum
			public string NormalColor;
			public string EmergencyColor;
			public string Status;
			public IMyTimerBlock LockTimer;
			public IMySoundBlock LockAlarm;

			public Sector(IMyAirVent airVent)
			{
				this.Vent = airVent;
				this.Tag = TagFromName(airVent.CustomName);
				this.NormalColor = GetKey(airVent, "Normal_Color", NORMAL);
				this.EmergencyColor = GetKey(airVent, "Emergency_Color", EMERGENCY);
				this.Status = GetKey(airVent, "Status", airVent.Status.ToString());

				if (this.Tag == VAC_TAG)
					this.Type = "Vacuum";
				else
					this.Type = "Room";

				this.Doors = new List<IMyDoor>();
				this.Lights = new List<IMyLightingBlock>();
				this.LCDs = new List<IMyTextPanel>();
				this.MergeBlocks = new List<IMyShipMergeBlock>();
				this.Connectors = new List<IMyShipConnector>();
				this.Bulkheads = new List<Bulkhead>();
			}

			public void Monitor()
			{
				foreach (Bulkhead myBulkhead in this.Bulkheads)
					myBulkhead.Monitor();

				bool depressurized = this.Vent.GetOxygenLevel() < 0.7 || this.Vent.Depressurize;

				if (this.Lights.Count > 0)
				{
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

			public void CloseDoors()
			{
				if (this.Doors.Count < 1)
					return;

				foreach (IMyDoor myDoor in this.Doors)
				{
					myDoor.CloseDoor();
				}
			}

			public void UpdateStatus()
			{
				IMyAirVent airVent = this.Vent;
				string oldStatus = GetKey(airVent, "Status", "Depressurized");

				if (this.Type == "Room")
				{

					if (oldStatus == "Pressurized" && airVent.Status.ToString() != "Pressurized")
						this.CloseDoors();
				}

				SetKey(airVent, "Status", airVent.Status.ToString());
			}
		}


		// BULKHEAD //   Wrapper class for doors so that they can directly access their sectors.
		public class Bulkhead
		{
			public List<IMyDoor> Doors;
			public string TagA;
			public string TagB;
			public Sector SectorA;
			public Sector SectorB;
			public IMyAirVent VentA;
			public IMyAirVent VentB;
			public IMyTextPanel LCDa;
			public IMyTextPanel LCDb;
			public bool Override;

			public Bulkhead(IMyDoor myDoor)
			{
				this.Doors = new List<IMyDoor>();
				this.Doors.Add(myDoor);
				string[] tags = MultiTags(myDoor.CustomName);

				this.TagA = tags[0];
				this.TagB = tags[1];
				this.Override = false;
			}

			public void Monitor()
			{
				if (this.SectorA == null || this.SectorB == null)
					return;

				float pressureA = this.VentA.GetOxygenLevel();
				float pressureB = this.VentB.GetOxygenLevel();
				this.Override = ParseBool(GetKey(this.Doors[0], "Override", "false"));

				if (this.Override || Math.Abs(pressureA - pressureB) < THRESHHOLD)
				{
					foreach (IMyDoor door in this.Doors)
						door.GetActionWithName("OnOff_On").Apply(door);
				}
				else
				{
					foreach (IMyDoor door in this.Doors)
						door.GetActionWithName("OnOff_Off").Apply(door);
				}

				this.DrawGauges();
			}

			public void SetOverride(bool overrided)
			{
				this.Override = overrided;
				SetKey(this.Doors[0], "Override", overrided.ToString());
				//_statusMessage = this.Doors[0].CustomName + " Override status set to " + overrided.ToString();
			}

			public void Open()
			{
				foreach (IMyDoor myDoor in this.Doors)
				{
					bool auto = ParseBool(GetKey(myDoor, "AutoOpen", "true"));
					if(auto)
                    {
						myDoor.OpenDoor();
					}
				}
			}

			public void DrawGauges()
			{
				if (this.LCDa == null && this.LCDb == null)
					return;

				bool locked = !this.Doors[0].IsWorking;

				if (this.LCDa != null) 
					DrawGauge(this.LCDa as IMyTextSurfaceProvider, this.SectorA, this.SectorB, locked);

				if(this.LCDb != null)
					DrawGauge(this.LCDb as IMyTextSurfaceProvider, this.SectorB, this.SectorA, locked);
			}
		}


		// PROGRAM /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public Program()
		{
			_previosCommand = "NEWLY LOADED";
			_statusMessage = "";
			_currentSector = 0;
			_breatherStep = 0;
			_breatherLength = _breather.Length;

			_backgroundColor = new Color(BG_RED, BG_GREEN, BG_BLUE);
			_textColor = new Color(TEXT_RED, TEXT_GREEN, TEXT_BLUE);
			_roomColor = new Color(ROOM_RED, ROOM_GREEN, ROOM_BLUE);

			_gridID = GetKey(Me, "Grid_ID", Me.CubeGrid.EntityId.ToString());
			
			Build();

			string updateFactor = GetKey(Me, "Refresh_Rate", "10");
			if (updateFactor == "1")
				Runtime.UpdateFrequency = UpdateFrequency.Update1;
			else if (updateFactor == "100")
				Runtime.UpdateFrequency = UpdateFrequency.Update100;
			else
				Runtime.UpdateFrequency = UpdateFrequency.Update10;

			
		}


		// SAVE ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void Save() { }


		// MAIN /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void Main(string arg)
		{
			PrintHeader();

			if (_vents.Count < 1)
			{
				Echo("No Vents Found to Build Network!  Please add sector tags to vent names then recompile!");
				return;
			}

			if (arg != "")
			{
				_statusMessage = "";
				_previosCommand = arg;

				string[] args = arg.Split(' ');

				string command = args[0].ToUpper();

				string cmdArg = "";

				if (args.Length > 1)
				{
					for (int i = 1; i < args.Length; i++)
						cmdArg += args[i];
				}

				switch (command)
				{
					case "OPEN_LOCK":
						OpenLock(cmdArg);
						break;
					case "TIMER_CALL":
						TimerCall(cmdArg);
						break;
					case "CLOSE_LOCK":
						CloseLock(cmdArg);
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
					default:
						_statusMessage = "UNRECOGNIZED COMMAND: " + arg;
						break;
				}
				return;
			}




			_currentSector++;
			if (_currentSector >= _sectors.Count)
				_currentSector = 0;


			Sector sector = _sectors[_currentSector];
			Echo("\nCurrent Check: " + sector.Type + " " + sector.Tag);
			sector.Monitor();
		}


		// TOOL FUNCTIONS //--------------------------------------------------------------------------------------------------------------------------------

		// TAG FROM NAME //
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

		// STRIP TAG //
		string StripTag(string tag)
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


		// GET SECTOR //
		Sector GetSector(string tag)
		{
			foreach (Sector sector in _sectors)
			{
				string myTag = StripTag(tag);

				if (sector.Tag == myTag)
					return sector;
			}

			return null;
		}


		// GET BULKHEAD //
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


		// COLOR FROM STRING //
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


		// SET GRID ID //
		void SetGridID(string arg)
		{
			string gridID = _gridID;
			if (arg != "")
				gridID = arg;

			SetKey(Me, "Grid_ID", gridID);
			_gridID = gridID;

			if (_vents.Count > 0)
			{
				foreach (IMyAirVent vent in _vents)
				{
					SetKey(vent as IMyTerminalBlock, "Grid_ID", gridID);
				}
			}

			if (_doors.Count > 0)
			{
				foreach (IMyDoor door in _doors)
				{
					SetKey(door as IMyTerminalBlock, "Grid_ID", gridID);
				}
			}

			if (_lights.Count > 0)
			{
				foreach (IMyLightingBlock light in _lights)
				{
					SetKey(light as IMyTerminalBlock, "Grid_ID", gridID);
				}
			}

			if (_lcds.Count > 0)
			{
				foreach (IMyTextPanel lcd in _lcds)
				{
					SetKey(lcd as IMyTerminalBlock, "Grid_ID", gridID);
				}
			}

			if (_lockTimers.Count > 0)
			{
				foreach (IMyTimerBlock timer in _lockTimers)
				{
					SetKey(timer as IMyTerminalBlock, "Grid_ID", gridID);
				}
			}

			if (_lockAlarms.Count > 0)
			{
				foreach (IMySoundBlock alarm in _lockAlarms)
				{
					SetKey(alarm as IMyTerminalBlock, "Grid_ID", gridID);
				}
			}

			if (_mergeBlocks.Count > 0)
			{
				foreach (IMyShipMergeBlock mergeBlock in _mergeBlocks)
				{
					SetKey(mergeBlock as IMyTerminalBlock, "Grid_ID", gridID);
				}
			}

			if(_connectors.Count > 0)
            {
				foreach(IMyShipConnector connector in _connectors)
                {
					SetKey(connector as IMyTerminalBlock, "Grid_ID", gridID);
				}
            }
		}

		// UNKNOWN SECTOR //
		bool UnknownSector(Sector sector)
		{
			if (sector != null)
				return false;

			_statusMessage = "UNKOWN SECTOR!";
			return true;
		}


		// PRINT HEADER // Prints program data in terminal
		void PrintHeader()
        {
			Echo("PRESSURE CHIEF " + _breather[_breatherStep]);
			Echo("--Pressure Management System--");
			Echo("Cmd: " + _previosCommand);
			Echo(_statusMessage + "\n----------------------");
			Echo("Sector Count: " + _sectors.Count);
			foreach (Sector sector in _sectors)
			{
				Echo(sector.Type + " " + sector.Tag);
				Echo(" * Doors: " + sector.Doors.Count + "  * Lights: " + sector.Lights.Count);
			}				

			_breatherStep++;
			if (_breatherStep >= _breatherLength)
				_breatherStep = 0;
        }

		// LOCK & DOCK FUNCTIONS --------------------------------------------------------------------------------------------------------------------------

		// OPEN LOCK //
		void OpenLock(string tag) {
			Sector sector = GetSector(tag);

			if (sector == null || (sector.Type != "Lock" && sector.Type != "Dock"))
			{
				_statusMessage = "INVALID OPEN LOCK CALL: " + tag;
				return;
			}

			sector.CloseDoors();
			sector.Vent.Depressurize = true;

			StageLock(sector, "1", 1); //phase 1, alert sound 1
		}


		// TIMER CALL //
		void TimerCall(string tag)
		{
			Sector sector = GetSector(tag);

			if (sector == null || (sector.Type != "Lock" && sector.Type != "Dock"))
			{
				_statusMessage = "INVALID TIMER CALL: " + tag;
				return;
			}

			IMyTimerBlock timer = sector.LockTimer;
			string phase = GetKey(timer, "Phase", "0");

			Bulkhead bulkhead = GetBulkhead(sector.Tag + SPLITTER + VAC_TAG);
			if (bulkhead == null)
			{
				_statusMessage = "TIMER CALL BULKHEAD ERROR: " + tag;
				return;
			}

			switch (phase)
			{
				case "1":
					bulkhead.SetOverride(true);
					SetKey(timer, "Phase", "2");
					timer.TriggerDelay = 1;
					timer.StartCountdown();
					break;
				case "2":
					bulkhead.Open();
					SetKey(timer, "Phase", "0");
					_statusMessage = sector.Type + " " + sector.Tag + " opened.";
					break;
				case "3":
					SetDockedOverride(sector, true);
					SetKey(timer, "Phase", "4");
					timer.TriggerDelay = 1;
					timer.StartCountdown();
					break;
				case "4":
					bulkhead.Open();
					SetKey(timer, "Phase", "0");
					break;
				case "5":
					ActivateDock(sector, false);
					SetKey(timer, "Phase", "6");
					timer.TriggerDelay = 10;
					timer.StartCountdown();
					break;
				case "6":
					ActivateDock(sector, true);
					SetKey(timer, "Phase", "0");
					break;
				default:
					_statusMessage = sector.Tag + " timer phase: " + phase;
					break;
			}
		}


		// CLOSE LOCK //
		void CloseLock(string tag)
		{
			Sector sector = GetSector(tag);

			if (sector == null || (sector.Type != "Lock" && sector.Type != "Dock"))
			{
				_statusMessage = "INVALID CLOSE LOCK CALL: " + tag;
				return;
			}

			sector.CloseDoors();
			sector.Vent.Depressurize = false;

			foreach (Bulkhead bulkhead in sector.Bulkheads)
			{
				if (bulkhead.TagB == VAC_TAG || bulkhead.TagA == VAC_TAG)
				{
					bulkhead.SetOverride(false);
				}
			}
		}


		// DOCK SEAL //
		void DockSeal(Sector sector)
        {
			if (sector.Type != "Dock" || UnknownSector(sector))
				return;

			foreach (IMyShipConnector connector in sector.Connectors)
				connector.Connect();

			StageLock(sector, "3", 2); // phase "3", alert 2
		}


		// UNDOCK //
		void Undock(Sector sector)
        {
			if (sector.Type != "Dock" || UnknownSector(sector))
				return;

			sector.CloseDoors();
			List<IMyDoor> dockedDoors = GetDockedDoors(sector);

			foreach (IMyDoor door in dockedDoors)
				door.CloseDoor();

			StageLock(sector, "5", 1);
        }


		// GET DOCKED DOORS//
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
				if (door.CustomName.Contains(dockTag) && door.CustomName.Contains(VAC_TAG) && GetKey(door, "Grid_ID", "unspecified") != _gridID)
					docked.Add(door as IMyDoor);
			}

			return docked;
		}



		// GET CONNECTED MERGE BLOCK //
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


		// STAGE LOCK //
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
				alarm.SelectedSound = "SoundBlockAlert" + alert;
				alarm.LoopPeriod = DELAY;
				alarm.Play();
			}
		}


		// SET DOCKED OVERIDE //
		void SetDockedOverride(Sector sector, bool overriding)
		{
			List<IMyDoor> dockedDoors = GetDockedDoors(sector);
			if (dockedDoors.Count < 1)
				return;

			foreach (Bulkhead bulkhead in sector.Bulkheads)
			{
				if (bulkhead.TagB == VAC_TAG || bulkhead.TagA == VAC_TAG)
					bulkhead.SetOverride(overriding);
			}

			foreach (IMyDoor door in dockedDoors)
			{
				SetKey(door, "Override", overriding.ToString());
			}
		}


		// ACTIVATE DOCK //
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

		// SPRITE FUNCTIONS --------------------------------------------------------------------------------------------------------------------------------

		// DRAW GAUGE //
		static void DrawGauge(IMyTextSurfaceProvider lcd, Sector sectorA, Sector sectorB, bool locked) 
		{
			IMyTextSurface drawSurface = lcd.GetSurface(0);
			RectangleF viewport = new RectangleF((drawSurface.TextureSize - drawSurface.SurfaceSize) / 2f, drawSurface.SurfaceSize);

			float pressureA = sectorA.Vent.GetOxygenLevel();
			float pressureB = sectorB.Vent.GetOxygenLevel();

			var frame = drawSurface.DrawFrame();

			float height = drawSurface.SurfaceSize.Y;
			float width = viewport.Width;

			Vector2 position = viewport.Center - new Vector2(width/2, 0);

			DrawTexture("SquareSimple", position, new Vector2(width, height), 0, _backgroundColor, frame);

			int redA = (int) (PRES_RED * pressureA);
			int greenA = (int)(PRES_GREEN * pressureA);
			int blueA = (int)(PRES_BLUE * pressureA);
			int redB = (int)(PRES_RED * pressureB);
			int greenB = (int)(PRES_GREEN * pressureB);
			int blueB = (int)(PRES_BLUE * pressureB);

			// Left Chamber
			position = viewport.Center - new Vector2(width *0.475f, 0);
			DrawTexture("SquareSimple", position, new Vector2(width*0.4f, height * 0.67f), 0, new Color(redA, greenA, blueA), frame);
			position -= new Vector2(height *-0.1f, height / 3);
			WriteText("*" + sectorA.Tag, position, TextAlignment.LEFT, 0.8f, _roomColor, frame);
			position += new Vector2(width * 0.375f, height / 3);
			WriteText(((int)(pressureA*100)) +"kPa", position, TextAlignment.RIGHT, 0.8f, _textColor, frame);

			// Right Chamber
			position = viewport.Center + new Vector2(width * 0.075f, 0);
			DrawTexture("SquareSimple", position, new Vector2(width*0.4f, height * 0.67f), 0, new Color(redB, greenB, blueB), frame);
			position -= new Vector2(height * -0.1f, height / 3);
			WriteText(sectorB.Tag, position, TextAlignment.LEFT, 0.8f, _textColor, frame);
			position += new Vector2(width * 0.375f, height / 3);
			WriteText(((int)(pressureB * 100)) + "kPa", position, TextAlignment.RIGHT, 0.8f, _textColor, frame);


			// Door Background
			position = viewport.Center - new Vector2(height * 0.25f, 0);
			DrawTexture("SquareSimple", position, new Vector2(height * 0.5f, height * 0.5f), 0, Color.Black, frame);

			// Door Status
			if (locked)
			{
				position = viewport.Center - new Vector2(height * 0.2f,0);
				DrawTexture("Cross", position, new Vector2(height * 0.4f, height * 0.4f), 0, Color.White, frame);
			}
			else
			{
				position = viewport.Center - new Vector2(height * 0.275f,0);
				DrawTexture("Arrow", position, new Vector2(height * 0.4f, height * 0.35f), (float) Math.PI/-2, Color.White, frame);
				position += new Vector2(height * 0.15f, 0);
				DrawTexture("Arrow", position, new Vector2(height * 0.4f, height * 0.35f), (float)Math.PI/2, Color.White, frame);
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
			_lockAlarms = new List<IMySoundBlock>();
			_lockTimers = new List<IMyTimerBlock>();
			_connectors = new List<IMyShipConnector>();
			_mergeBlocks = new List<IMyShipMergeBlock>();
			_lights = new List<IMyLightingBlock>();
			_sectors = new List<Sector>();
			_bulkheads = new List<Bulkhead>();

			_autoCheck = ParseBool(GetKey(Me, "Auto-Check", "true"));
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
					Sector sector = new Sector(vent);
					_sectors.Add(sector);
				}

				if (_sectors.Count < 1 || _doors.Count < 1)
					return;

				// Assign double-tagged components
				AssignDoors();
				AssignLCDs();

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


		// ASSIGN DOORS //
		void AssignDoors()
		{
			foreach (IMyDoor door in _doors)
			{
				string[] tags = MultiTags(door.CustomName);
				Bulkhead bulkhead = GetBulkhead(tags[0] + SPLITTER + tags[1]);
				if (bulkhead == null)
				{
					Echo("A");
					bulkhead = new Bulkhead(door);
					_bulkheads.Add(bulkhead);
				}
				else
				{
					bulkhead.Doors.Add(door);
				}

				if (tags[0] == VAC_TAG || tags[1] == VAC_TAG)
					EnsureKey(door, "AutoOpen", "true");

				Echo("B");
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
						bulkhead.VentA = sector.Vent;
						SetKey(door, "Vent_A", sector.Vent.CustomName);
					}
					else if (sector.Tag == tags[1])
					{
						sector.Doors.Add(door);
						sector.Bulkheads.Add(bulkhead);
						bulkhead.SectorB = sector;
						bulkhead.VentB = sector.Vent;
						SetKey(door, "Vent_B", sector.Vent.CustomName);
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


		// ASSIGN LCD //
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
					string doorName = bulkhead.Doors[0].CustomName;
					if (doorName.Contains(tag))
					{
						PrepareTextSurface(lcd as IMyTextSurfaceProvider);
						bulkhead.LCDa = lcd;
					}
					else if (doorName.Contains(reverseTag))
					{
						PrepareTextSurface(lcd as IMyTextSurfaceProvider);
						bulkhead.LCDb = lcd;
					}
				}
			}
		}


		// ASSIGN LIGHT //
		void AssignLight(IMyLightingBlock light)
		{
			string tag = TagFromName(light.CustomName);

			foreach (Sector sector in _sectors)
			{
				if (sector.Tag == tag)
				{
					sector.Lights.Add(light);

					if (GetKey(light, "Normal_Color", sector.NormalColor) == "") ;
					SetKey(light, "Normal_Color", sector.NormalColor);
					if (GetKey(light, "Emergency_Color", sector.EmergencyColor) == "") ;
					SetKey(light, "Emergency_Color", sector.EmergencyColor);


					return;
				}
			}
		}


		// ASSIGN TIMER //
		void AssignTimer(IMyTimerBlock timer)
		{
			string tag = TagFromName(timer.CustomName);

			foreach (Sector sector in _sectors)
			{
				if (sector.Tag == tag)
				{
					string delayString = GetKey(timer, "delay", "5");
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


		// ASSIGN ALARM //
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


		// ASSIGN MERGE BLOCK //
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


		// ASSIGN CONNECTOR //
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


		// ENSURE KEY //
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


		// GET INI //
		static MyIni GetIni(IMyTerminalBlock block)
		{
			MyIni iniOuti = new MyIni();

			MyIniParseResult result;
			if (!iniOuti.TryParse(block.CustomData, out result))
				throw new Exception(result.ToString());

			return iniOuti;
		}


		// BORROWED FUNCTIONS --------------------------------------------------------------------------------------------------------------

		// PREPARE TEXT SURFACE
		public void PrepareTextSurface(IMyTextSurfaceProvider lcd)
		{
			IMyTextSurface textSurface = lcd.GetSurface(0);

			// Set the sprite display mode
			textSurface.ContentType = ContentType.SCRIPT;
			// Make sure no built-in script has been selected
			textSurface.Script = "";
		}



		// END HERE ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	}
}
