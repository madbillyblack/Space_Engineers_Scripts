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
					bool auto = ParseBool(MainDoor.GetKey("AutoOpen", "true"));
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

			/*
			public string GetKey(string key, string defaultValue)
			{
				EnsureKey(key, defaultValue);
				return Ini.Get(INI_HEAD, key).ToString();
			}

			void EnsureKey(string key, string defaultValue)
			{
				if (!Ini.ContainsKey(INI_HEAD, key))
					SetKey(key, defaultValue);
			}

			public void SetKey(string key, string value)
			{
				Ini.Set(INI_HEAD, key, value);
				MainDoor.CustomData = Ini.ToString();
			}
			*/
		}
	
	
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
	}
}
