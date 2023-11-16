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
		// BULKHEAD //   Class for barrier between sectors, which has doors and can also have LCD surfaces.
		public class Bulkhead
		{
			public List<Sector> Sectors;
			public List<PressureDoor> Doors;
			public List<IMyTextSurfaceProvider> LCDs;
			public List<IMyTextSurface> Surfaces;
			public List<GaugeSurface> Gauges;
			public List<bool> LcdOrientations; // Bool list assigning whether LCDs are vertical.
			public List<bool> LcdFlips; // List of bools that designate if sectors A & B are displayed on the right and left respectively.
			public List<UInt16> LcdBrightnesses;
			public bool Override; // If True, Bulkhead ignores pressure checks and is always unlocked.
			public bool ElevatorDoor;

			public int AutoCloseDelay;
			public int DelayCount;

			// Variables for sectors separated by bulkhead.
			//public Sector SectorA;
			//public IMyAirVent VentA;
			public string TagA;

			//public Sector SectorB;
			//public IMyAirVent VentB;
			public string TagB;

			//MyIni Ini;
			PressureDoor MainDoor;

			// Constructor - Door required
			public Bulkhead(IMyDoor myDoor)
			{
				MainDoor = new PressureDoor(myDoor);
				//Ini = GetIni(MainDoor);
				Sectors = new List<Sector>();
				Doors = new List<PressureDoor>();
				LCDs = new List<IMyTextSurfaceProvider>();
				Surfaces = new List<IMyTextSurface>();
				Gauges = new List<GaugeSurface>();
				LcdOrientations = new List<bool>();
				LcdFlips = new List<bool>();
				LcdBrightnesses = new List<UInt16>();

				// Check to see if door is also being used by Elevator Manager Script
				if (myDoor.CustomData.Contains("Elevator_Door"))
					ElevatorDoor = ParseBool(MainDoor.GetKey("Elevator_Door", "False"));
				else
					ElevatorDoor = false;

				Doors.Add(MainDoor);
				Override = false;

				TagA = MainDoor.GetKey("Sector_A", "").Trim();
				TagB = MainDoor.GetKey("Sector_B", "").Trim();
			}

			// CHECK // - Checks pressure difference between sectors and bulkhead override status and locks/unlocks accordingly.
			public void Check()
			{
				if (Sectors.Count < 2)
					return;

				foreach(Sector sector in Sectors)
					sector.Check();

				Override = ParseBool(MainDoor.GetKey("Override", "false"));

				if (Sectors[0].IsPressurized == Sectors[1].IsPressurized || Override)
				{
					foreach (PressureDoor door in Doors)
					{
						if (ElevatorDoor && !Override)
							MainDoor.SetKey("Lock_Down", "False");
						else
							door.Door.GetActionWithName("OnOff_On").Apply(door.Door);
					}		
				}
				else
				{
					foreach (PressureDoor door in Doors)
                    {
						door.Door.GetActionWithName("OnOff_Off").Apply(door.Door);
						if(ElevatorDoor)
                        {
							MainDoor.SetKey("Lock_Down", "True");
                        }
					}
				}

				AutoClose();
				DrawGauges();
			}

			// SET OVERRIDE // - Set's override status and updates custom data.
			public void SetOverride(bool overrided)
			{
				Override = overrided;
				MainDoor.SetKey("Override", overrided.ToString());
			}


			// OPEN // - openAll variable determines if doors with AutoOpen set to false are also opened.
			public void Open(bool openAll)
			{
				foreach (PressureDoor myDoor in Doors)
				{
					bool auto = ParseBool(myDoor.GetKey("AutoOpen", "true"));
					if (auto || openAll)
					{
						myDoor.OpenDoor();
					}
				}
			}


			// DRAW GAUGES // - Calls DrawGauge with side parameters for all LCD displays in Bulkhead
			public void DrawGauges()
			{
				if (Gauges.Count < 1)
					return;

				bool locked = !MainDoor.Door.IsWorking;

				foreach(GaugeSurface gauge in Gauges)
				{
					if (gauge.Side == "A")
						DrawGauge(gauge, gauge.SectorA, gauge.SectorB, locked, false);
					else if (gauge.Side == "B")
						DrawGauge(gauge, gauge.SectorB, gauge.SectorA, locked, false);
					else
						DrawGauge(gauge, gauge.SectorA, gauge.SectorB, locked, true);
				}
			}

			// SET AUTO CLOSE
			public void SetAutoClose()
            {
				int delay;

				if (Sectors.Count < 2)
                {
					AutoCloseDelay = 0;
					return;
				}

				int timeA = Sectors[0].AutoCloseDelay;
				int timeB = Sectors[1].AutoCloseDelay;

				if (timeA > timeB)
					delay = timeA;
				else
					delay = timeB;

				if (delay == 0)
					AutoCloseDelay = 0;
				else
					AutoCloseDelay = 60 * delay /(_autoCloseFactor * _bulkheads.Count) + 1;
				
				DelayCount = AutoCloseDelay;
            }


			// AUTO CLOSE
			public void AutoClose()
            {
				if (AutoCloseDelay == 0)
					return;

				if(IsClosed())
                {
					DelayCount = AutoCloseDelay;
					return;
				}
					
				DelayCount--;
				if(DelayCount < 0)
					CloseDoors();
            }

			// CLOSE DOORS
			public void CloseDoors()
            {
				foreach (PressureDoor door in Doors)
					door.CloseDoor();
            }

			// IS CLOSED
			bool IsClosed()
			{
				foreach (PressureDoor door in Doors)
                {
					if (door.Door.OpenRatio > 0)
						return false;
                }

				return true;
			}
		}
	
	
		// PRESSURE DOOR
		public class PressureDoor
        {
			public IMyDoor Door;
			BlockIni Ini;

			// Constructor
			public PressureDoor(IMyDoor door)
            {
				Door = door;
				Ini = new BlockIni(Door, INI_HEAD);
            }

			// GET KEY
			public string GetKey(string key, string defaultValue)
			{
				return Ini.GetKey(key, defaultValue);
			}

			// SET KEY
			public void SetKey(string key, string value)
			{
				Ini.SetKey(key, value);
			}

			// OPEN DOOR
			public void OpenDoor()
            {
				Door.OpenDoor();
            }

			// CLOSE DOOR
			public void CloseDoor()
            {
				Door.CloseDoor();
            }


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


		/* OVERRIDE // - Get Bulkhead and set Override State:
		 * 0: Override = False (Restore)
		 * 1: Override = True
		 * 2: Toggle Override
		 */
		void Override(string bulkheadTags, int state = 1)
        {
			Bulkhead bulkhead = GetBulkhead(bulkheadTags);

			if (bulkhead == null)
            {
				_statusMessage = "No bulkhead with tags \"" + bulkheadTags + "\" found!";
				return;
			}

			switch(state)
            {
				case 0: // Restore Bulkhead
					bulkhead.SetOverride(false);
					break;
				case 1: // Override Bulkhead
					bulkhead.SetOverride(true);
					break;
				case 2: // Toggle Bulkhead
					bulkhead.SetOverride(!bulkhead.Override);
					break;
				default:
					_statusMessage = "INVALID OVERRIDE CASE!!!";
					break;
            }
        }
	}
}
