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
		const string DEFAULT_LOG_TAG = "ELV_LOG";
		const string PLATFORM_TAG = "MAIN";
		const int ERROR = 10000; // (Hopefully) Unusable value that can be used as an error for Shaft and Floor counts.
		const char SPLITTER = ':';
		const float P_TOLERANCE = 0.25f; //Acceptable distance threshold (in meters) for floor pistons to be considered in position.
		const float DEFAULT_TIME = 10;
		const float DEFAULT_CLOSE_TIME = 2;
		const float DEFAULT_WAIT_TIME = 5;
		const float AUX_DELAY = 3;
		const string DELAY_LABEL = "Delay_Floor_";

		const float SENSOR_BOTTOM = 0.1f;
		const float SENSOR_TOP = 0.1f;
		const float SENSOR_LEFT = 0.1f;
		const float SENSOR_RIGHT = 0.1f;
		const float SENSOR_FRONT = 4;
		const float SENSOR_BACK = 4;

		const int MARGIN = 10;

		// Color Constants
		const int ON_RED = 127;
		const int ON_GREEN = 127;
		const int ON_BLUE = 127;

		const int OFF_RED = 24;
		const int OFF_GREEN = 16;
		const int OFF_BLUE = 64;


		// Globals
		public List<Elevator> _elevators;
		public List<IMyTextSurface> _logScreens;

		string _unusable;
		public static string _statusMessage;
		public static string _logTag;

		public static Color _onColor;
		public static Color _offColor;

		// CLASSES /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// ELEVATOR //
		public class Elevator
		{
			public List<Floor> Floors;
			public List<Page> FloorQueue;
			//public List<IMyTerminalBlock> DisplayBlocks;
			public List<Display> Displays;
			public List<IMySoundBlock> SoundBlocks;
			public IMyTimerBlock Timer;
			public Platform Platform;
			public UInt16 Phase;
			public int Number;
			public int CurrentFloor;
			public int GroundFloor;
			public int TopFloor;
			public float TravelTime;
			public float CloseTime;
			public float WaitTime;
			public bool GoingUp;
			public bool Travelling;
			public bool PlayingMusic;

			public Elevator(IMyTimerBlock timer)
			{
				Platform = new Platform();
				Floors = new List<Floor>();
				FloorQueue = new List<Page>();
				//DisplayBlocks = new List<IMyTerminalBlock>();
				Displays = new List<Display>();
				
				Timer = timer;
				SoundBlocks = new List<IMySoundBlock>();
				GoingUp = ParseBool(GetKey(timer, INI_HEAD, "Going_Up", "true"));
				Travelling = ParseBool(GetKey(timer, INI_HEAD, "Travelling", "true"));
				CurrentFloor = ParseInt(GetKey(timer, INI_HEAD, "Current_Floor", "0"), 0);
				CloseTime = ParseFloat(GetKey(timer, INI_HEAD, "Close_Time", DEFAULT_CLOSE_TIME.ToString()), DEFAULT_CLOSE_TIME);
				WaitTime = ParseFloat(GetKey(timer, INI_HEAD, "Wait_Time", DEFAULT_WAIT_TIME.ToString()), DEFAULT_WAIT_TIME);
				PlayingMusic = false;

				int[] tags = SplitTag(TagFromName(timer.CustomName));

				Number = tags[0];

				ushort phase;
				if (UInt16.TryParse(GetKey(timer, INI_HEAD, "Phase", "0"), out phase))
				{
					Phase = phase;
				}
				else
				{
					Phase = 0;
				}
			}

			// SORT FLOORS //
			public void SortFloors(bool lowToHigh)
			{
				int length = Floors.Count;
				if (length < 2)
					return;
				
				for(int e = 0; e < Floors.Count -1 ; e++)
				{
					for(int f = 1; f < length; f++)
					{
						Floor floorA = Floors[f - 1];
						Floor floorB = Floors[f];

						if((lowToHigh && floorA.Number > floorB.Number) || (!lowToHigh && floorA.Number < floorB.Number))
						{
							Floors[f - 1] = floorB;
							Floors[f] = floorA;
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
				if (Floors.Count < 2)
					return;

				if (floorNumber < CurrentFloor)
				{
					GoingUp = false;
					SetKey(Timer, INI_HEAD, "Going_Up", "false");
				}
				else
				{
					GoingUp = true;
					SetKey(Timer, INI_HEAD, "Going_Up", "true");
				}

				foreach (Floor floor in Floors)
				{
					if (floor.Number > floorNumber)
					{
						floor.Deactivate();
					}
					else if (floor.Number == floorNumber)
					{
						Timer.TriggerDelay = AUX_DELAY;//floor.TravelTime;
						floor.Activate();
					}
					else
					{
						floor.Activate();						
					}
				}

				//Timer.TriggerDelay = TravelTime;
				Phase = 2;
				SetKey(Timer, INI_HEAD, "Phase", "2");
				Timer.StartCountdown();
			}

			// SORT QUEUE //
			public void SortQueue()
			{
				if (FloorQueue.Count < 2)
					return;

				List<Page> listA = new List<Page>();
				List<Page> listB = new List<Page>();
				List<Page> listC = new List<Page>();

				if (GoingUp)
				{
					foreach(Page page in FloorQueue)
					{
						if (page.Up && page.Floor >= CurrentFloor)
							listA.Add(page);
						else if (page.Up && page.Floor < CurrentFloor)
							listC.Add(page);
						else
							listB.Add(page);
					}

					SortPages(listA, true);
					SortPages(listB, false);
					SortPages(listC, true);
				}
				else
				{
					foreach (Page page in FloorQueue)
					{
						if (!page.Up && page.Floor <= CurrentFloor)
							listA.Add(page);
						else if (!page.Up && page.Floor > CurrentFloor)
							listC.Add(page);
						else
							listB.Add(page);
					}

					SortPages(listA, false);
					SortPages(listB, true);
					SortPages(listC, false);
				}

				// Reverse Elevator direction if first queue is empty
				/*if(listA.Count == 0)
				{
					GoingUp = !GoingUp;
				}*/

				listA.AddList(listB);
				listA.AddList(listC);
				FloorQueue = listA;
			}

			// UPDATE TRAVEL TIMES //
			public void UpdateTravelTimes()
			{
				if (Floors.Count < 1)
					return;
				
				//float time = 0;
				
				foreach(Floor floor in Floors)
				{
					floor.TravelTime = ParseFloat(GetKey(Timer, INI_HEAD, DELAY_LABEL + floor.Number, DEFAULT_TIME.ToString()), DEFAULT_TIME);
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
				TravelTime = time;
				Timer.TriggerDelay = time;
				SetKey(Timer, INI_HEAD, "Travel_Time",time.ToString("n2"));
				*/
			}

			// SET TRAVEL TIMES
			public void SetTravelTimes(string arg)
			{
				if (NoFloors())
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

				foreach(Floor floor in Floors)
				{
					TravelTime = number;
					SetKey(Timer, INI_HEAD, DELAY_LABEL + floor.Number, number.ToString());
				}
			}

			// HAS ARRIVED //
			public bool HasArrived()
			{
				if (Floors.Count < 2)
					return true;

				foreach(Floor floor in Floors)
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
				if(Floors.Count < 1)
				{
					_statusMessage = "Elevator " + Number + " has no floors!";
					return true;
				}

				return false;
			}

			// GET FLOOR //
			public Floor GetFloor(int number)
			{
				if(Floors.Count > 0)
				{
					foreach(Floor floor in Floors)
					{
						if (floor.Number == number)
							return floor;
					}
				}
				return null;
			}

			// OPEN DOORS //
			public void OpenDoors()
			{
				if (NoFloors() || !HasArrived())
					return;

				Platform.OpenDoors();

				foreach (Floor floor in Floors)
				{
					if (floor.Number == CurrentFloor)
					{
						floor.OpenDoors();
					}
				}
			}

			// CLOSE DOORS //
			public void CloseDoors()
			{
				if (NoFloors())
					return;

				Platform.CloseDoors();

				foreach (Floor floor in Floors)
				{
					floor.CloseDoors();
				}
			}

			// LOCK DOORS //
			public void LockDoors()
			{
				if (NoFloors())
					return;

				Platform.LockDoors();
				foreach(Floor floor in Floors)
				{
					floor.LockDoors();
				}
			}

			// SET TRAVEL //
			public void SetTravel(bool travelling)
			{
				Travelling = travelling;
				SetKey(Timer, INI_HEAD, "Travelling", travelling.ToString());
			}

			// SET PHASE //
			public void SetPhase(ushort phase)
			{
				Phase = phase;
				SetKey(Timer, INI_HEAD, "Phase", phase.ToString());
			}

			// START DELAY //
			public void StartDelay(float delay)
			{
				Timer.TriggerDelay = delay;
				Timer.StartCountdown();
			}

			// GO TO NEXT //
			public void GoToNext()
			{ 
				// If Queue is empty or next floor is current floor STOP
				if(FloorQueue.Count < 1)// || FloorQueue[0].Floor == CurrentFloor
				{
					SetPhase(0);
					//StopMusic();
					Timer.StopCountdown();
					SetTravel(false);
					return;
				}

				PlayMusic();
				GoToFloor(FloorQueue[0].Floor);
				SetPhase(2);
			}

			// CHECK ARRIVAL //
			public void CheckArrival()
			{
				if (HasArrived())
				{
					OpenDoors();
					if(FloorQueue.Count > 0)
					{
						FloorQueue.Remove(FloorQueue[0]);
						if (FloorQueue.Count < 1)
							StopMusic();
					}
					SetPhase(3);
					StartDelay(DEFAULT_WAIT_TIME);
				}
				else
				{
					StartDelay(AUX_DELAY);
				}
			}

			// PAGE DIRECTION //
			public bool GetPageDirection(int destination, string direction)
			{
				bool goingUp;

				switch (direction.ToLower())
				{
					case "up":
						goingUp = true;
						break;
					case "down":
						goingUp = false;
						break;
					default:
						if (destination < CurrentFloor)
							goingUp = false;
						else
							goingUp = true;
						break;
				}

				return goingUp;
			}

			// DRAW DISPLAYS //
			public void DrawDisplays()
			{
				if (Displays.Count < 1)
					return;

				foreach (Display display in Displays)
				{
					DrawDisplay(display, this);
				}
			}


			// PLAY MUSIC //
			public void PlayMusic()
            {
				if (PlayingMusic || SoundBlocks.Count < 1)
					return;

				PlayingMusic = true;
				foreach (IMySoundBlock soundBlock in SoundBlocks)
                {
					soundBlock.Play();
				}
            }


			// STOP MUSIC //
			public void StopMusic()
			{
				if (SoundBlocks.Count < 1)
					return;

				PlayingMusic = false;
				foreach (IMySoundBlock soundBlock in SoundBlocks)
				{
					soundBlock.Stop();
				}
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
			public bool IsTop;
			public float TravelTime;
			public Floor(int number, Elevator elevator)
			{
				Number = number;
				Elevator = elevator;
				Pistons = new List<ElevatorPiston>();
				Doors = new List<IMyDoor>();
				Sensors = new List<IMySensorBlock>();
				IsGround = false;
				IsTop = false;
				TravelTime = ParseFloat(GetKey(elevator.Timer, INI_HEAD, DELAY_LABEL + number, DEFAULT_TIME.ToString()), DEFAULT_TIME);
			}

			// ACTIVATE // - Activate all pistons associated with this floor.
			public void Activate()
			{
				if (Pistons.Count < 1)
					return;

				foreach(ElevatorPiston piston in Pistons)
				{
					piston.Activate();
				}
			}

			// DEACTIVATE // - Deactivate all pistons associate with this floor.
			public void Deactivate()
			{
				if (Pistons.Count < 1)
					return;

				foreach (ElevatorPiston piston in Pistons)
				{
					piston.Deactivate();
				}
			}

			// AT TARGET //
			public bool AtTarget()
			{
				if(Pistons.Count > 0)
				{
					foreach(ElevatorPiston piston in Pistons)
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

			// OPEN DOORS //
			public void OpenDoors()
			{
				if (Doors.Count < 1)
					return;

				foreach(IMyDoor door in Doors)
				{
					// Check if elevator doors are locked down by pressure chief program
					bool lockDown;
					if (door.CustomData.Contains("Pressure Chief"))
					{
						lockDown = ParseBool(GetKey(door, "Pressure Chief", "Lock_Down", "False"));
					}
					else
						lockDown = false;

					if (!lockDown)
					{
						door.GetActionWithName("OnOff_On").Apply(door);
						door.OpenDoor();
					}
					else
						_statusMessage = "Can't open Elevator-" + Elevator.Number + " Floor-" + Number + "\n  due to pressure differential.";
				}
			}

			// CLOSE DOORS //
			public void CloseDoors()
			{
				if (Doors.Count < 1)
					return;

				foreach (IMyDoor door in Doors)
				{
					//door.GetActionWithName("OnOff_On").Apply(door);
					door.CloseDoor();
				}
			}

			// LOCK DOORS //
			public void LockDoors()
			{
				if (Doors.Count < 1)
					return;

				foreach (IMyDoor door in Doors)
				{
					door.GetActionWithName("OnOff_Off").Apply(door);
				}
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
				Piston = piston;
				Inverted = ParseBool(GetKey(piston, INI_HEAD, "Inverted", "False"));

				float velocity = piston.Velocity;
				if (velocity < 0)
					Retracting = true;
				else
					Retracting = false;

				

				Max = ParseFloat(GetKey(piston, INI_HEAD, "Max", piston.MaxLimit.ToString()), piston.MaxLimit);
				piston.MaxLimit = Max;
				Min = ParseFloat(GetKey(piston, INI_HEAD, "Min", piston.MinLimit.ToString()), piston.MinLimit);
				piston.MinLimit = Min;
				Speed = Math.Abs(ParseFloat(GetKey(piston, INI_HEAD, "Speed", piston.Velocity.ToString()), piston.Velocity));

				velocity = Speed;
				if (Retracting)
					velocity *= -1;

				piston.Velocity = velocity;
			}

			// ACTIVATE // - Set Piston to its Activated Position
			public void Activate()
			{
				if(Inverted)
				{
					Piston.Retract();
					Retracting = true;
				}
				else
				{
					Piston.Extend();
					Retracting = false;
				}
			}

			// DEACTIVATE // - Set Piston to its Deactivated Position
			public void Deactivate()
			{
				if (Inverted)
				{
					Piston.Extend();
					Retracting = false;
				}
				else
				{
					Piston.Retract();
					Retracting = true;
				}
			}
		}


		// PLATFORM //
		public class Platform
		{
			public List<IMyDoor> Doors;

			public Platform()
			{
				Doors = new List<IMyDoor>();
			}

			// OPEN DOORS //
			public void OpenDoors()
			{
				if (Doors.Count < 1)
					return;

				foreach(IMyDoor door in Doors)
				{
					door.GetActionWithName("OnOff_On").Apply(door);
					door.OpenDoor();
				}
			}

			// CLOSE DOORS //
			public void CloseDoors()
			{
				if (Doors.Count < 1)
					return;

				foreach (IMyDoor door in Doors)
				{
					door.CloseDoor();
				}
			}

			// LOCK DOORS //
			public void LockDoors()
			{
				if (Doors.Count < 1)
					return;

				foreach (IMyDoor door in Doors)
				{
					door.GetActionWithName("OnOff_Off").Apply(door);
				}
			}
		}


		// Page //
		public class Page
		{
			public int Floor;
			public bool Up;

			public Page(int floor, bool up)
			{
				Floor = floor;
				Up = up;
			}
		}


		public class Display
		{
			public IMyTextSurface Surface;
			public bool ShowAll;
			public int Floor;
			public Color OnColor;
			public Color OffColor;
			public string ShapeName;
			public string Shape;


			public Display(IMyTerminalBlock block, int index, Elevator elevator)
			{
				string floorData = GetKey(block, INI_HEAD, "Screen_" + index + "_Floor", "All");


				if(floorData.ToLower() == "all")
				{
					ShowAll = true;
				}
				else
				{
					Floor = ParseInt(floorData, elevator.GroundFloor);
				}

				Surface = (block as IMyTextSurfaceProvider).GetSurface(index);
				PrepareTextSurface(Surface, GetKey(block, INI_HEAD, "Background_Color", "0,0,0"));


				ShapeName = GetKey(block, INI_HEAD, "Shape", "Square");
				switch(ShapeName.ToUpper())
				{
					case "TRIANGLE":
					case "TRIANGLEINVERTED":
					case "DIRECTIONAL":
					case "SCI-FI":
						Shape = "Triangle";
						break;
					case "CIRCLE":
						Shape = "Circle";
						break;
					case "SQUARETAPERED":
						Shape = "SquareTapered";
						break;
					case "FULL":
					default:
						Shape = "SquareSimple";
						break;
				}

				OnColor = ColorFromString(GetKey(block, INI_HEAD, "On_Color", ON_RED + "," + ON_GREEN + "," + ON_BLUE));
				OffColor = ColorFromString(GetKey(block, INI_HEAD, "Off_Color", OFF_RED + "," + OFF_GREEN + "," + OFF_BLUE));
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

			foreach(Elevator elevator in _elevators)
            {
				Echo("\nElevator " + elevator.Number);
				Echo("  * Floors: " + elevator.Floors.Count);
				Echo("  * Current: " + elevator.CurrentFloor);
				Echo("  * Sound Blocks: " + elevator.SoundBlocks.Count);
            }

			Echo("Cmd: " + argument);
			if (argument != "")
			{
				string[] args = argument.Split(' ');
				string arg = args[0];
				string argData = "";
				if (args.Length > 1)
				{
					for (int i = 1; i < args.Length; i++)
					{
						argData += args[i] + " ";
					}

					argData.Trim();
					Echo("Data: " + argData);
				}

				switch (arg.ToUpper())
				{
					case "TIMER_CALL":
						TimerCall(argData);
						break;
					case "SENSOR_CALL":
					case "SENSOR_IN":
						SensorCall(argData, true);
						break;
					case "REFRESH":
						Build();
						break;
					case "GO_TO":
					case "GOTO":
						GoTo(argData);
						break;
					case "PAGE_UP":
						PageElevator(argData, "up");
						break;
					case "PAGE_DOWN":
						PageElevator(argData, "down");
						break;
					case "PAGE":
						PageElevator(argData, "none");
						break;
					case "SET_TIMES":
						SetElevatorTimes(argData);
						break;
					default:
						_statusMessage = "Unrecognized Command: " + argument;
						break;
				}
			}

			Echo(_statusMessage);
			if(_logScreens.Count > 0)
			{
				foreach(IMyTextSurface screen in _logScreens)
				{
					InsertText(screen, "Cmd: " + argument);

					if(_statusMessage != "")
                    {
						InsertText(screen, "\nALERT: " + _statusMessage);
						_statusMessage = "";
					}
				}
			}

			foreach (Elevator elevator in _elevators)
			{
				string elevatorDirection;
				if (elevator.GoingUp)
					elevatorDirection = "Up";
				else
					elevatorDirection = "Down";

				Echo("\nELEVATOR " + elevator.Number + " - Going " + elevatorDirection);
				Echo("Floor: " + elevator.CurrentFloor + " - Phase: " + elevator.Phase);
				Echo("Screens: " + elevator.Displays.Count);
				Echo("Arrived: " + elevator.HasArrived().ToString());

				if (elevator.Floors.Count > 0)
				{
					Echo("Queue:");
					if(elevator.FloorQueue.Count > 0)
					{
						foreach(Page page in elevator.FloorQueue)
						{
							string direction;
							if (page.Up)
								direction = "Up";
							else
								direction = "Down";
							Echo("Floor " + page.Floor + " - " + direction);
						}
					}
					else
					{
						Echo("  <EMPTY>");
					}
					/*
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
						
					}*/
				}
			}
		}


		// FUNCTIONS ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// PAGE Elevator//
		public void PageElevator(string arg, string direction)
		{
			string[] args = arg.Trim().Split(SPLITTER);

			if(args.Length != 2)
			{
				_statusMessage = "INVALID PAGE ARGUMENT!\nPlease follow format <Elevator> <Floor>";
				return;
			}

			Elevator elevator = ElevatorFromTag(args[0]);
			int floor = ParseInt(args[1], 0);

			bool goingUp = elevator.GetPageDirection(floor, direction);

			Page page = new Page(floor, goingUp);

			if(DuplicatePages(page, elevator.FloorQueue))
			{
				return;
			}

			if (elevator.CurrentFloor == floor && elevator.HasArrived())
			{
				elevator.OpenDoors();
				elevator.SetPhase(2);
				elevator.StartDelay(elevator.WaitTime);
			}
			else if(elevator.FloorQueue.Count == 0)//!elevator.Timer.IsCountingDown
			{
				elevator.CloseDoors();
				elevator.SetPhase(1);
				elevator.StartDelay(elevator.CloseTime);
			}

			elevator.FloorQueue.Add(page);
			elevator.SortQueue();

			// If elevator is mid-travel, send elevator to next floor in queue. Current floor excluded to avoid hiccups.
			if(elevator.Phase == 2 && elevator.FloorQueue[0].Floor != elevator.CurrentFloor)
			{
				elevator.GoToNext();
			}
		}


		// SORT PAGES //
		public static void SortPages(List<Page> list, bool lowToHigh)
		{
			int length = list.Count;
			if (length < 2)
				return;

			for (int e = 0; e < list.Count - 1; e++)
			{
				for (int f = 1; f < length; f++)
				{
					Page pageA = list[f - 1];
					Page pageB = list[f];

					//if ((lowToHigh && pageA.Floor > pageB.Floor && !pageA.Up) || (!lowToHigh && pageA.Floor < pageB.Floor && pageA.Up))
					if ((lowToHigh && pageA.Floor > pageB.Floor) || (!lowToHigh && pageA.Floor < pageB.Floor))
					{
						list[f - 1] = pageB;
						list[f] = pageA;
					}
				}

				length--;
				if (length < 2)
					return;
			}
		}


		// DUPLICATE PAGES // - Returns true if list contains page matching the current page.
		bool DuplicatePages(Page newPage, List<Page> pages)
		{
			if(pages.Count > 0)
			{
				foreach(Page page in pages)
				{
					if (page.Floor == newPage.Floor && page.Up == newPage.Up)
						return true;
				}
			}

			return false;
		}


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
			if(elevator == null)
			{
				_statusMessage = "No Elevator \"" + elevatorTag + " \"found!";
				return;
			}

			switch(ParseInt(GetKey(elevator.Timer, INI_HEAD, "Phase", "0"), 0))
			{
				case 0:
				case 3:
					elevator.CloseDoors();
					elevator.SetPhase(1);
					elevator.StartDelay(elevator.CloseTime);
					break;
				case 1:
					if(elevator.FloorQueue.Count > 0)
                    {
						elevator.LockDoors();
					}
					elevator.GoToNext(); //Sends Elevator to next floor, or stops if Queue is empty.
					break;
				case 2:
					elevator.CheckArrival();
					break;
				default:
					_statusMessage = "Invalid Timer Phase! Please check Custom Data of timer block.";
					break;
			}

			elevator.DrawDisplays();
		}


		// SENSOR CALL //
		public void SensorCall(string tag, bool detected)
		{
			Elevator elevator = ElevatorFromTag(tag);
			if(elevator == null)
			{
				_statusMessage = "Elevator not Found!";
				return;
			}

			Floor floor = FloorFromTag(elevator, tag);
			if(floor == null)
			{
				_statusMessage = "Invalid Floor Tag!";
				return;
			}

			if(detected)
			{
				elevator.CurrentFloor = floor.Number;
				SetKey(elevator.Timer, INI_HEAD, "Current_Floor", elevator.CurrentFloor.ToString());

				if (floor.IsGround)
					elevator.GoingUp = true;
				else if (floor.IsTop)
					elevator.GoingUp = false;

			}

			elevator.DrawDisplays();
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
			_logScreens = new List<IMyTextSurface>();
			_logTag = GetKey(Me, INI_HEAD, "Log_Tag", DEFAULT_LOG_TAG);

			_onColor = new Color(ON_RED, ON_GREEN, ON_BLUE);
			_offColor = new Color(OFF_RED, OFF_GREEN, OFF_BLUE);

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

				elevator.TopFloor = elevator.Floors.Count - 1;
				elevator.Floors[elevator.TopFloor].IsTop = true;
				int bottom = elevator.Floors[0].Number - 1;

				if(!int.TryParse(GetKey(elevator.Timer, INI_HEAD, "Ground_Floor", bottom.ToString()), out elevator.GroundFloor))
				{
					elevator.GroundFloor = bottom;
				}

				Floor floor = new Floor(elevator.GroundFloor, elevator);
				floor.IsGround = true;
				elevator.Floors.Insert(0,floor);

				if (elevator.CurrentFloor == elevator.GroundFloor)
					elevator.GoingUp = true;
				else if (elevator.CurrentFloor == elevator.TopFloor)
					elevator.GoingUp = false;
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
					AssignSurfaces(surfaceBlock);
			}

			if(_elevators.Count > 0)
			{
				foreach(Elevator elevator in _elevators)
				{
					elevator.DrawDisplays();
				}
			}

			AssignSoundBlocks();
			AssignLogs();
		}


		// ASSIGN SURFACES // Check if terminal block has surfaces that can be used to display gauges.
		public void AssignSurfaces(IMyTerminalBlock block)
		{
			IMyTextSurfaceProvider screenHaver = block as IMyTextSurfaceProvider;

			try
			{
				if (block.CustomName.Contains(OPENER) && HasSurfaces(block))
				{
					Elevator elevator = ElevatorFromTag(TagFromName(block.CustomName));
					//elevator.DisplayBlocks.Add(block);
					for(int i = 0; i < screenHaver.SurfaceCount; i++)
					{
						string filler = "";
						if (i == 0)
							filler = "All";

						string floor = GetKey(block, INI_HEAD, "Screen_" + i + "_Floor", filler);
						if (floor != "")
						{
							Display lcd = new Display(block, i, elevator);
							elevator.Displays.Add(lcd);
						}
					}
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


		// HAS SURFACES //
		bool HasSurfaces(IMyTerminalBlock block)
		{
			try
			{
				if ((block as IMyTextSurfaceProvider).SurfaceCount > 0)
					return true;
				else
					return false;
			}
			catch
			{
				return false;
			}
		}


		// ASSIGN SOUND BLOCKS //
		void AssignSoundBlocks()
        {
			List<IMySoundBlock> soundBlocks = new List<IMySoundBlock>();
			GridTerminalSystem.GetBlocksOfType<IMySoundBlock>(soundBlocks);

			if (soundBlocks.Count < 1)
				return;

			foreach(IMySoundBlock soundBlock in soundBlocks)
            {
				foreach(Elevator elevator in _elevators)
                {
					if(soundBlock.CustomName.Contains(OPENER + elevator.Number + CLOSER))
                    {
						elevator.SoundBlocks.Add(soundBlock);
						if (!soundBlock.IsSoundSelected)
                        {
							soundBlock.SelectedSound = "MusComp_11";
							soundBlock.Range = 7.5f;
							soundBlock.Volume = 0.25f;
						}
                    }
                }
            }
        }

		// ASSIGN LOGS //
		void AssignLogs()
		{
			List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
			GridTerminalSystem.SearchBlocksOfName(_logTag, blocks);

			foreach(IMyTerminalBlock block in blocks)
			{
				if(HasSurfaces(block))
				{
					_logScreens.AddRange(ScreensFromData(block as IMyTextSurfaceProvider, "indexes"));
				}
			}

			if(_logScreens.Count > 0)
			{
				foreach(IMyTextSurface screen in _logScreens)
				{
					screen.ContentType = ContentType.TEXT_AND_IMAGE;
					screen.WriteText("---NEW LOAD---");
				}
			}
		}


		// SCREENS FROM DATA //
		List<IMyTextSurface> ScreensFromData(IMyTextSurfaceProvider block, string key)
		{
			List<IMyTextSurface> screens = new List<IMyTextSurface>();

			string[] entries = GetKey(block as IMyTerminalBlock, INI_HEAD, key, "0").Split(',');
			if (entries.Length > 0)
			{
				foreach (string entry in entries)
				{
					int index = ParseInt(entry, 0);
					if(index < block.SurfaceCount)
					{
						screens.Add(block.GetSurface(index));
					}
				}
			}

			return screens;
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

			if(door.CustomData.Contains("Pressure Chief"))
            {
				EnsureKey(door, "Pressure Chief", "Elevator_Door", "True");
            }

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

			// Disable un-needed functions
			sensor.DetectAsteroids = false;
			sensor.DetectEnemy = false;
			sensor.DetectFloatingObjects = false;
			sensor.DetectLargeShips = false;
			sensor.DetectOwner = true;
			sensor.DetectPlayers = false;
			sensor.DetectStations = false;

			// Enable required functions
			sensor.DetectFriendly = true;
			sensor.DetectNeutral = true;
			sensor.DetectSubgrids = true;

			// Set Configurable parameters
			sensor.PlayProximitySound = ParseBool(GetKey(sensor, INI_HEAD, "Proximity_Sound", "false"));
			sensor.LeftExtend = ParseFloat(GetKey(sensor, INI_HEAD, "Left_Extent", SENSOR_LEFT.ToString()), SENSOR_LEFT);
			sensor.RightExtend = ParseFloat(GetKey(sensor, INI_HEAD, "Right_Extent", SENSOR_RIGHT.ToString()), SENSOR_RIGHT);
			sensor.TopExtend = ParseFloat(GetKey(sensor, INI_HEAD, "Top_Extent", SENSOR_TOP.ToString()), SENSOR_TOP);
			sensor.BottomExtend = ParseFloat(GetKey(sensor, INI_HEAD, "Bottom_Extent", SENSOR_BOTTOM.ToString()), SENSOR_BOTTOM);
			sensor.FrontExtend = ParseFloat(GetKey(sensor, INI_HEAD, "Front_Extent", SENSOR_FRONT.ToString()), SENSOR_FRONT);
			sensor.BackExtend = ParseFloat(GetKey(sensor, INI_HEAD, "Back_Extent", SENSOR_BACK.ToString()), SENSOR_BACK);

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


		// SPRIT FUNCTIONS ------------------------------------------------------------------------------------------------------------------------------

		// DRAW DISPLAY // - Draws Display showing current position of elevator
		static void DrawDisplay(Display display, Elevator elevator)
		{
			IMyTextSurface drawSurface = display.Surface;
			
			// Get Single or All Floors for Display
			List<Floor> floors;
			if (display.ShowAll)
			{
				floors = elevator.Floors;
			}
			else
			{
				floors = new List<Floor>();
				Floor newFloor = elevator.GetFloor(display.Floor);
				if (newFloor == null)
				{
					_statusMessage = "Unrecognized Floor " +  display.Floor + " in Elevator " + elevator.Number + "!!!";
					return;
				}

				floors.Add(newFloor);
			}
	
			RectangleF viewport = new RectangleF((drawSurface.TextureSize - drawSurface.SurfaceSize) / 2f, drawSurface.SurfaceSize);
			var frame = drawSurface.DrawFrame();

			float size = Math.Min(viewport.Height, viewport.Width);
			float totalWidth = size * floors.Count;

			if(totalWidth > viewport.Width)
			{
				size = viewport.Width / floors.Count;
				totalWidth = viewport.Width;
			}
			
			float textSize = size * 0.03f;

			
			Vector2 scale = new Vector2(size - MARGIN, size - MARGIN);
			string shape = display.Shape;
			string shapeName = display.ShapeName.ToUpper();

			float horizontalOffset = 0;

			// Fill display if shape is "Full"
			if (shapeName == "FULL")
			{
				scale.Y = viewport.Height;

				if (!display.ShowAll)
				{
					scale.X = viewport.Width;
					totalWidth = viewport.Width;
					horizontalOffset = size * 0.25f;
				}	
			}
			else if(shapeName == "TRIANGLEINVERTED" || (shapeName == "DIRECTIONAL" && !elevator.GoingUp))
			{
				scale.Y *= -1;
			}
			
			float offset;
			if (shape == "Triangle")
			{
				offset = -size * 0.2f;
				textSize *= 0.75f;
				if (shapeName == "TRIANGLEINVERTED" || (shapeName == "DIRECTIONAL" && !elevator.GoingUp))
					offset = -size * 0.5f;
			}
			else
			{
				offset = -size * 0.45f;
			}

			if(shapeName == "SCI-FI")
			{
				size *= 0.5f;
				totalWidth *= 0.5f;
			}

			Vector2 position = viewport.Center - new Vector2(totalWidth * 0.5f, 0);
			Vector2 startPosition = position;
			if(shapeName == "SCI-FI" && elevator.Floors.Count % 2 == 0)
			{
				position -= new Vector2(size * 0.5f,0);
			}

			foreach (Floor floor in floors)
			{
				Color color;
				Color textColor;
				Vector2 newPosition = position;
				if (floor.Number == elevator.CurrentFloor)
				{
					color = display.OnColor;
					textColor = display.OffColor;
				}
				else
				{
					color = display.OffColor;
					textColor = display.OnColor;
				}

				DrawTexture(shape, position + new Vector2(MARGIN * 0.5f, 0), scale, 0, color, frame);
				if(shapeName == "SCI-FI")
				{
					if(scale.Y < 0)
					{
						offset = -size;
					}
					else
					{
						offset = -size * 0.4f;
					}	

					WriteText(floor.Number.ToString(), position + new Vector2(size + horizontalOffset, offset), TextAlignment.CENTER, textSize, textColor, frame);
					scale.Y *= -1;
				}
				else
				{
					WriteText(floor.Number.ToString(), position + new Vector2(size * 0.5f + horizontalOffset, offset), TextAlignment.CENTER, textSize, textColor, frame);
				}
				
				position += new Vector2(size, 0);
			}

			frame.Dispose();
		}


		// DRAW TEXTURE //
		static void DrawTexture(string shape, Vector2 position, Vector2 scale, float rotation, Color color, MySpriteDrawFrame frame)
		{
			MySprite sprite = new MySprite()
			{
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


		// PREPARE TEXT SURFACE
		public static void PrepareTextSurface(IMyTextSurface textSurface, string backgroundColor)
		{
			// Set the sprite display mode
			textSurface.ContentType = ContentType.SCRIPT;
			// Make sure no built-in script has been selected
			textSurface.Script = "";

			// Set Background Color
			textSurface.ScriptBackgroundColor = ColorFromString(backgroundColor);
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


		// PARSE INT //
		static int ParseInt(string arg, int defaultVal)
		{
			int number;
			if (int.TryParse(arg, out number))
				return number;
			else
				return defaultVal;
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

		// INSERT TEXT
		static void InsertText(IMyTextSurface surface, string text)
		{
			surface.WriteText(text + "\n" + surface.GetText());
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
	}
}
