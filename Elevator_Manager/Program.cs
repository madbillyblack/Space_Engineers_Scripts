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
		// Constants
		const string INI_HEAD = "Elevator";
		const string OPENER = "ELV[";
		const string CLOSER = "]";
		const string PLATFORM_TAG = "MAIN";
		const int ERROR = 10000; // (Hopefully) Unusable value that can be used as an error for Shaft and Floor counts.
		const char SPLITTER = ':';
		const float P_TOLERANCE = 0.25f; //Acceptable distance threshold (in meters) for floor pistons to be considered in position.
		const float DEFAULT_TIME = 10;
		const string DELAY_LABEL = "Delay_Floor_";

		// Globals
		public List<Elevator> _elevators;
		string _unusable;
		public static string _statusMessage;

		// CLASSES /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// ELEVATOR //
		public class Elevator
		{
			public List<Floor> Floors;
			public List<Page> FloorQueue;
			public List<IMyTerminalBlock> DisplayBlocks;
			public IMyTimerBlock Timer;
			public Platform Platform;
			public UInt16 State;
			public int Number;
			public int GroundFloor;
			public float TravelTime;

			public Elevator(IMyTimerBlock timer)
			{
				this.Platform = new Platform();
				this.Floors = new List<Floor>();
				this.FloorQueue = new List<Page>();
				this.DisplayBlocks = new List<IMyTerminalBlock>();
				
				this.Timer = timer;

				int[] tags = SplitTag(TagFromName(timer.CustomName));

				this.Number = tags[0];

				ushort state;
				if (UInt16.TryParse(GetKey(timer, INI_HEAD, "State", "0"), out state))
				{
					this.State = state;
				}
				else
				{
					this.State = 0;
				}
			}

			// SORT FLOORS //
			public void SortFloors(bool lowToHigh)
			{
				int length = this.Floors.Count;
				if (length < 2)
					return;
				
				for(int e = 0; e < this.Floors.Count -1 ; e++)
				{
					for(int f = 1; f < length; f++)
					{
						Floor floorA = this.Floors[f - 1];
						Floor floorB = this.Floors[f];

						if((lowToHigh && floorA.Number > floorB.Number) || (!lowToHigh && floorA.Number < floorB.Number))
						{
							this.Floors[f - 1] = floorB;
							this.Floors[f] = floorA;
						}
					}

					length--;
					if (length < 2)
						return;
				}
			}

			// GO TO FLOOR //
			public void GoToFloor(int floorNumber)
			{
				if (this.Floors.Count < 2)
					return;

				this.Platform.CloseDoors();

				foreach(Floor floor in this.Floors)
				{


					if (floor.Number > floorNumber)
					{
						floor.Deactivate();
					}
					else if (floor.Number == floorNumber)
					{
						this.Timer.TriggerDelay = floor.TravelTime;
						floor.Activate();
					}
					else
					{
						floor.Activate();						
					}
				}

				//this.Timer.TriggerDelay = this.TravelTime;
				this.Timer.StartCountdown();
			}

			// UPDATE TRAVEL TIMES //
			public void UpdateTravelTimes()
			{
				if (this.Floors.Count < 1)
					return;
				
				//float time = 0;
				
				foreach(Floor floor in this.Floors)
				{
					floor.TravelTime = ParseFloat(GetKey(this.Timer, INI_HEAD, DELAY_LABEL + floor.Number, DEFAULT_TIME.ToString()), DEFAULT_TIME);
					/*
					if (floor.Pistons.Count > 0)
					{
						foreach(ElevatorPiston piston in floor.Pistons)
						{
							float pistonTime = 2 * (piston.Max - piston.Min) / piston.Speed;
							if(pistonTime > time)
							{
								time = pistonTime;
							}
						}
					}
					*/
				}

				/*
				this.TravelTime = time;
				this.Timer.TriggerDelay = time;
				SetKey(this.Timer, INI_HEAD, "Travel_Time",time.ToString("n2"));
				*/
			}


			// SET TRAVEL TIMES
			public void SetTravelTimes(string arg)
			{
				if (this.NoFloors())
					return;

				// May seem redundant, but this is here so that an empty arg doesn't trigger _statusMessage.
				if (arg.Trim() == "")
					arg = DEFAULT_TIME.ToString();

				float number;
				if(!float.TryParse(arg, out number))
				{
					_statusMessage = "Unreadable Time Input: " + arg;
					number = DEFAULT_TIME;
				}

				foreach(Floor floor in this.Floors)
				{
					this.TravelTime = number;
					SetKey(this.Timer, INI_HEAD, DELAY_LABEL + floor.Number, number.ToString());
				}
			}


			// HAS ARRIVED //
			public bool HasArrived()
			{
				if (this.Floors.Count < 2)
					return true;

				foreach(Floor floor in this.Floors)
				{
					if(!floor.AtTarget())
					{
						return false;
					}
				}

				return true;
			}


			// NO FLOORS //
			public bool NoFloors()
			{
				if(this.Floors.Count < 1)
				{
					_statusMessage = "Elevator " + this.Number + " has no floors!";
					return true;
				}

				return false;
			}
		}


		// FLOOR //
		public class Floor
		{
			public int Number;
			public Elevator Elevator;
			public List<ElevatorPiston> Pistons;
			public List<IMyDoor> Doors;
			public List<IMySensorBlock> Sensors;
			public bool IsGround;
			public float TravelTime;
			public Floor(int number, Elevator elevator)
			{
				this.Number = number;
				this.Elevator = elevator;
				this.Pistons = new List<ElevatorPiston>();
				this.Doors = new List<IMyDoor>();
				this.Sensors = new List<IMySensorBlock>();
				this.IsGround = false;
				this.TravelTime = ParseFloat(GetKey(elevator.Timer, INI_HEAD, DELAY_LABEL + number, DEFAULT_TIME.ToString()), DEFAULT_TIME);
			}

			// ACTIVATE // - Activate all pistons associated with this floor.
			public void Activate()
			{
				if (this.Pistons.Count < 1)
					return;

				foreach(ElevatorPiston piston in this.Pistons)
				{
					piston.Activate();
				}
			}

			// DEACTIVATE // - Deactivate all pistons associate with this floor.
			public void Deactivate()
			{
				if (this.Pistons.Count < 1)
					return;

				foreach (ElevatorPiston piston in this.Pistons)
				{
					piston.Deactivate();
				}
			}

			// AT TARGET //
			public bool AtTarget()
			{
				if(this.Pistons.Count > 0)
				{
					foreach(ElevatorPiston piston in this.Pistons)
					{
						float pos = piston.Piston.CurrentPosition;
						float target;

						if (piston.Retracting)
							target = piston.Min;
						else
							target = piston.Max;

						if (Math.Abs(pos - target) > P_TOLERANCE)
							return false;
					}
				}

				return true;
			}
		}


		// ELEVATOR PISTON //
		public class ElevatorPiston
		{
			public IMyPistonBase Piston;
			public bool Inverted;
			public bool Retracting;
			public float Max;
			public float Min;
			public float Speed;

			public ElevatorPiston(IMyPistonBase piston)
			{
				this.Piston = piston;
				this.Inverted = ParseBool(GetKey(piston, INI_HEAD, "Inverted", "False"));

				float velocity = piston.Velocity;
				if (velocity < 0)
					this.Retracting = true;
				else
					this.Retracting = false;

				this.Max = ParseFloat(GetKey(piston, INI_HEAD, "Max", piston.MaxLimit.ToString()), piston.MaxLimit);
				this.Min = ParseFloat(GetKey(piston, INI_HEAD, "Min", piston.MinLimit.ToString()), piston.MinLimit);
				this.Speed = Math.Abs(ParseFloat(GetKey(piston, INI_HEAD, "Speed", piston.Velocity.ToString()), piston.Velocity));
			}

			// ACTIVATE // - Set Piston to its Activated Position
			public void Activate()
			{
				if(this.Inverted)
				{
					this.Piston.Retract();
					this.Retracting = true;
				}
				else
				{
					this.Piston.Extend();
					this.Retracting = false;
				}
			}

			// DEACTIVATE // - Set Piston to its Deactivated Position
			public void Deactivate()
			{
				if (this.Inverted)
				{
					this.Piston.Extend();
					this.Retracting = false;
				}
				else
				{
					this.Piston.Retract();
					this.Retracting = true;
				}
			}
		}


		// PLATFORM //
		public class Platform
		{
			public List<IMyDoor> Doors;

			public Platform()
			{
				this.Doors = new List<IMyDoor>();
			}

			// OPEN DOORS //
			public void OpenDoors()
			{
				if (this.Doors.Count < 1)
					return;

				foreach(IMyDoor door in this.Doors)
				{
					door.GetActionWithName("OnOff_On").Apply(door);
					door.OpenDoor();
				}
			}

			// CLOSE DOORS //
			public void CloseDoors()
			{
				if (this.Doors.Count < 1)
					return;

				foreach (IMyDoor door in this.Doors)
				{
					door.CloseDoor();
				}
			}

			// LOCK DOORS //
			public void LockDoors()
			{
				if (this.Doors.Count < 1)
					return;

				foreach (IMyDoor door in this.Doors)
				{
					door.GetActionWithName("OnOff_Off").Apply(door);
				}
			}
		}


		// Page //
		public class Page
		{
			int Floor;
			bool Up;

			public Page(int floor, bool up)
			{
				this.Floor = floor;
				this.Up = up;
			}
		}


		// CONSTRUCTOR /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public Program()
		{
			Build();
			//Runtime.UpdateFrequency = UpdateFrequency.Update100;
		}

		public void Save(){}


		// MAIN ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void Main(string argument, UpdateType updateSource)
		{
			if (_elevators.Count < 1)
				return;

			Echo("Cmd: " + argument);
			if (argument == "")
			{
				Echo(_statusMessage);
				return;
			}
				
			string[] args = argument.Split(' ');
			string arg = args[0];
			string argData = "";
			if(args.Length > 1)
			{
				for(int i = 1; i < args.Length; i++)
				{
					argData += args[i] + " ";
				}

				argData.Trim();
			}
			
			switch(arg.ToUpper())
			{
				case "TIMER_CALL":
					TimerCall(argData);
					break;
				case "REFRESH":
					Build();
					break;
				case "GO_TO":
				case "GOTO":
					GoTo(argData);
					break;
				case "SET_TIMES":
					SetElevatorTimes(argData);
					break;
				default:
					_statusMessage = "Unrecognized Command: " + argument;
					break;
			}

			Echo(_statusMessage);

			foreach (Elevator elevator in _elevators)
			{
				Echo("\nELEVATOR " + elevator.Number);
				if (elevator.Floors.Count > 0)
				{
					foreach (Floor floor in elevator.Floors)
					{
						Echo("\n* Floor " + floor.Number + " - Delay: " + floor.TravelTime.ToString());
						if (floor.Pistons.Count > 0)
						{
							foreach (ElevatorPiston piston in floor.Pistons)
							{
								Echo("   - " + piston.Piston.CustomName);
							}
						}
						if (floor.Doors.Count > 0)
						{
							foreach (IMyDoor door in floor.Doors)
							{
								Echo("   - " + door.CustomName);
							}
						}
					}
				}
			}
		}


		// FUNCTIONS ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// GO TO //
		public void GoTo(string tag)
		{
			string[] tags = tag.Split(SPLITTER);
			int floorNumber;
			Elevator elevator = ElevatorFromTag(tag);

			// Abort if no floor arg, no elevator returned, or floor arg is unparsible.
			if (tags.Length != 2 || elevator == null || !int.TryParse(tags[1], out floorNumber))
			{
				_statusMessage = "Invalid GOTO arg: " + tag;
				return;
			}

			elevator.GoToFloor(floorNumber);
		}


		// TIMER CALL //
		public void TimerCall(string elevatorTag)
		{
			Elevator elevator = ElevatorFromTag(elevatorTag);

			if(elevator.HasArrived())
			{
				elevator.Platform.OpenDoors();
			}
			else
			{
				elevator.Timer.TriggerDelay = 5;
				elevator.Timer.StartCountdown();
			}
		}


		// SET ELEVATOR TIMES //
		public void SetElevatorTimes(string arg)
		{
			string[] args = arg.Split(' ');
			if (args.Length < 1)
			{
				_statusMessage = "Incomplete Command.  Please include Elevator Number and Desired Delay Length!";
				return;
			}

			Elevator elevator = ElevatorFromTag(args[0]);
			if (elevator == null)
				return;

			string value;

			if (args.Length < 2)
				value = "";
			else
				value = args[1];

			elevator.SetTravelTimes(value);
		}


		// INIT FUNCTIONS ----------------------------------------------------------------------------------------------------------------------------------

		// BUILD //
		public void Build()
		{
			_statusMessage = "";
			_unusable = "";
			_elevators = new List<Elevator>();

			// Create local lists.
			List<IMyDoor> doors = new List<IMyDoor>();
			List<IMyPistonBase> pistons = new List<IMyPistonBase>();
			List<IMySensorBlock> sensors = new List<IMySensorBlock>();
			List<IMyTerminalBlock> leftovers = new List<IMyTerminalBlock>();

			// Gather tagged blocks
			List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
			GridTerminalSystem.SearchBlocksOfName(OPENER, blocks);

			// Sort and assign tagged blocks based on block type.
			if (blocks.Count < 1)
			{
				_statusMessage = "No Blocks Found.  Please include Tags in block names to assign them to elevators and floors.";
				return;
			}

			foreach(IMyTerminalBlock block in blocks)
			{
				switch (block.BlockDefinition.TypeIdString)
				{
					case "MyObjectBuilder_Door":
					case "MyObjectBuilder_AirtightSlideDoor":
					case "MyObjectBuilder_AirtightHangarDoor":
						doors.Add(block as IMyDoor);
						break;
					case "MyObjectBuilder_TimerBlock":
						_elevators.Add(new Elevator(block as IMyTimerBlock));
						break;
					case "MyObjectBuilder_ExtendedPistonBase":
						pistons.Add(block as IMyPistonBase);
						break;
					case "MyObjectBuilder_SensorBlock":
						sensors.Add(block as IMySensorBlock);
						break;
					default:
						leftovers.Add(block);
						break;
				}	
			}

			if (_elevators.Count < 1)
				return;

			// After Elevators created, build basic floors.
			if(pistons.Count > 0)
			{
				foreach (IMyPistonBase piston in pistons)
					AssignPiston(piston);
			}

			// Assign Ground Floor for each elevator after basic floors have been built.
			foreach(Elevator elevator in _elevators)
			{
				//elevator.UpdateTravelTimes();

				elevator.SortFloors(true);
				int bottom = elevator.Floors[0].Number - 1;

				if(!int.TryParse(GetKey(elevator.Timer, INI_HEAD, "Ground_Floor", bottom.ToString()), out elevator.GroundFloor))
				{
					elevator.GroundFloor = bottom;
				}

				Floor floor = new Floor(elevator.GroundFloor, elevator);
				elevator.Floors.Add(floor);
			}



			if (doors.Count > 0)
			{
				foreach (IMyDoor door in doors)
					AssignDoor(door);
			}


			if (sensors.Count > 0)
			{
				foreach (IMySensorBlock sensor in sensors)
					AssignSensor(sensor);
			}

			if(leftovers.Count > 0)
			{
				foreach (IMyTerminalBlock surfaceBlock in leftovers)
					CheckSurfaces(surfaceBlock);
			}
		}


		// CHECK SURFACES // Check if terminal block has surfaces that can be used to display gauges.
		void CheckSurfaces(IMyTerminalBlock block)
		{
			try
			{
				if (block.CustomName.Contains(SPLITTER) && (block as IMyTextSurfaceProvider).SurfaceCount > 0)
				{
					Elevator elevator = ElevatorFromTag(TagFromName(block.CustomName));
					elevator.DisplayBlocks.Add(block);
				}
				else
				{
					_unusable += "\n* " + block.CustomName;
					return;
				}
			}
			catch
			{
				_unusable += "\n* " + block.CustomName;
				return;
			}
		}


		// ASSIGN DOOR //
		void AssignDoor(IMyDoor door)
		{
			string tag = TagFromName(door.CustomName);
			Elevator elevator = ElevatorFromTag(tag);

			if(elevator == null)
			{
				_statusMessage = "INVALID TAG: " + tag;
				return;
			}

			if(tag.Contains(PLATFORM_TAG))
			{
				elevator.Platform.Doors.Add(door);
				return;
			}

			Floor floor = FloorFromTag(elevator, tag);
			
			if (floor == null)
				return;

			floor.Doors.Add(door);
		}


		// ASSIGN PISTON //
		public void AssignPiston(IMyPistonBase piston)
		{
			string tag = TagFromName(piston.CustomName);
			Elevator elevator = ElevatorFromTag(tag);
			string[] tags = tag.Split(SPLITTER);


			if (elevator == null || tags.Length != 2)
			{
				_statusMessage = "INVALID PISTON TAG: " + tag;
				return;
			}

			Floor floor = FloorFromTag(elevator, tag);


			if (floor == null)
			{
				int floorNumber;
				if (!int.TryParse(tags[1], out floorNumber))
				{
					return;
				}

				floor = new Floor(floorNumber, elevator);
				elevator.Floors.Add(floor);
			}

			ElevatorPiston ePiston = new ElevatorPiston(piston);

			floor.Pistons.Add(ePiston);
		}


		// ASSIGN SENSOR //
		public void AssignSensor(IMySensorBlock sensor)
		{
			string tag = TagFromName(sensor.CustomName);
			Elevator elevator = ElevatorFromTag(tag);

			if (elevator == null)
			{
				_statusMessage = "INVALID SENSOR TAG: " + tag;
				return;
			}

			Floor floor = FloorFromTag(elevator, tag);

			if (floor == null)
				return;

			floor.Sensors.Add(sensor);
		}


		// INI FUNCTIONS -----------------------------------------------------------------------------------------------------------------------------------

		// ENSURE KEY // Check to see if INI key exists, and if it doesn't write with default value.
		static void EnsureKey(IMyTerminalBlock block, string header, string key, string defaultVal)
		{
			//if (!block.CustomData.Contains(header) || !block.CustomData.Contains(key))
			MyIni ini = GetIni(block);
			if (!ini.ContainsKey(header, key))
				SetKey(block, header, key, defaultVal);
		}


		// GET KEY // Gets ini value from block.  Returns default argument if doesn't exist.
		static string GetKey(IMyTerminalBlock block, string header, string key, string defaultVal)
		{
			EnsureKey(block, header, key, defaultVal);
			MyIni blockIni = GetIni(block);
			return blockIni.Get(header, key).ToString();
		}


		// SET KEY // Update ini key for block, and write back to custom data.
		static void SetKey(IMyTerminalBlock block, string header, string key, string arg)
		{
			MyIni blockIni = GetIni(block);
			blockIni.Set(header, key, arg);
			block.CustomData = blockIni.ToString();
		}


		// GET INI // Get entire INI object from specified block.
		static MyIni GetIni(IMyTerminalBlock block)
		{
			MyIni iniOuti = new MyIni();

			MyIniParseResult result;
			if (!iniOuti.TryParse(block.CustomData, out result))
			{
				block.CustomData = "---\n" + block.CustomData;
				if (!iniOuti.TryParse(block.CustomData, out result))
					throw new Exception(result.ToString());
			}

			return iniOuti;
		}


		// TOOL FUNCTIONS -------------------------------------------------------------------------------------------------------------------------------

		// PARSE FLOAT //
		static float ParseFloat(string numString, float defaultValue)
		{
			float number;
			if (!float.TryParse(numString, out number))
				number = defaultValue;

			return number;
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


		// TAG FROM NAME // Returns Tag from block's name.
		public static string TagFromName(string name)
		{			
			int start = name.IndexOf(OPENER) + OPENER.Length; //Start index of tag substring
			int length = name.IndexOf(CLOSER) - start; //Length of tag

			return name.Substring(start, length);
		}

		
		// ELEVATOR FROM TAG //
		public Elevator ElevatorFromTag(string tag)
		{
			if (_elevators.Count < 1)
				return null;

			if (tag.Contains(SPLITTER))
				tag = tag.Split(SPLITTER)[0];

			int shaftNumber;
			if(!int.TryParse(tag, out shaftNumber))
			{
				_statusMessage = "Invalid Elevator Shaft Tag: " + tag;
				return null;
			}

			foreach(Elevator shaft in _elevators)
			{
				if(shaft.Number == shaftNumber)
					return shaft;
			}

			return null;
		}

		// FLOOR FROM TAG //
		public Floor FloorFromTag(Elevator elevator, string tag)
		{
			string[] tags = tag.Split(SPLITTER);
			int floorNumber;

			if(tags.Length != 2 || !int.TryParse(tags[1], out floorNumber))
			{
				_statusMessage = "Invalid FLOOR TAG: " + tag;
				return null;
			}

			if(elevator.Floors.Count < 1)
			{
				return null;
			}

			foreach(Floor floor in elevator.Floors)
			{
				if(floor.Number == floorNumber)
				{
					return floor;
				}
			}

			return null;
		}


		// SPLIT TAG //
		public static int[] SplitTag(string tag)
		{
			string[] tags = tag.Split(SPLITTER);

			int[] parsed = new int[tags.Length];

			for(int i = 0; i<tags.Length; i++)
			{
				if(!int.TryParse(tags[i], out parsed[i]))
				{
					parsed[i] = ERROR;
				}
			}

			return parsed;
		}
	}
}
