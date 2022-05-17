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
			public List<IMyDoor> Doors;
			public List<IMyTextSurfaceProvider> LCDs;
			public List<IMyTextSurface> Surfaces;
			public List<GaugeSurface> Gauges;
			public List<bool> LcdOrientations; // Bool list assigning whether LCDs are vertical.
			public List<bool> LcdFlips; // List of bools that designate if sectors A & B are displayed on the right and left respectively.
			public List<UInt16> LcdBrightnesses;
			public bool Override; // If True, Bulkhead ignores pressure checks and is always unlocked.

			// Variables for sectors separated by bulkhead.
			//public Sector SectorA;
			//public IMyAirVent VentA;
			public string TagA;

			//public Sector SectorB;
			//public IMyAirVent VentB;
			public string TagB;

			// Constructor - Door required
			public Bulkhead(IMyDoor myDoor)
			{
				Sectors = new List<Sector>();
				Doors = new List<IMyDoor>();
				LCDs = new List<IMyTextSurfaceProvider>();
				Surfaces = new List<IMyTextSurface>();
				Gauges = new List<GaugeSurface>();
				LcdOrientations = new List<bool>();
				LcdFlips = new List<bool>();
				LcdBrightnesses = new List<UInt16>();

				Doors.Add(myDoor);
				Override = false;

				TagA = IniKey.GetKey(myDoor, INI_HEAD, "Sector_A", "").Trim();
				TagB = IniKey.GetKey(myDoor, INI_HEAD, "Sector_B", "").Trim();
			}

			// CHECK // - Checks pressure difference between sectors and bulkhead override status and locks/unlocks accordingly.
			public void Check()
			{
				if (Sectors.Count < 2)
					return;

				foreach(Sector sector in Sectors)
					sector.Check();

				Override = Util.ParseBool(IniKey.GetKey(Doors[0], INI_HEAD, "Override", "false"));

				if (Sectors[0].IsPressurized == Sectors[1].IsPressurized || Override)
				{
					foreach (IMyDoor door in Doors)
						door.GetActionWithName("OnOff_On").Apply(door);
				}
				else
				{
					foreach (IMyDoor door in Doors)
						door.GetActionWithName("OnOff_Off").Apply(door);
				}

				DrawGauges();
			}

			// SET OVERRIDE // - Set's override status and updates custom data.
			public void SetOverride(bool overrided)
			{
				Override = overrided;
				foreach (IMyDoor door in Doors)
					IniKey.SetKey(door, INI_HEAD, "Override", overrided.ToString());
			}


			// OPEN // - openAll variable determines if doors with AutoOpen set to false are also opened.
			public void Open(bool openAll)
			{
				foreach (IMyDoor myDoor in Doors)
				{
					bool auto = Util.ParseBool(IniKey.GetKey(myDoor, INI_HEAD, "AutoOpen", "true"));
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

				bool locked = !Doors[0].IsWorking;

				foreach(GaugeSurface gauge in Gauges)
				{
					if (gauge.Side == "A")
						DrawGauge(gauge, Sectors[0], Sectors[1], locked, false);
					else if (gauge.Side == "B")
						DrawGauge(gauge, Sectors[1], Sectors[0], locked, false);
					else
						DrawGauge(gauge, Sectors[0], Sectors[1], locked, true);
				}
			}
		}
	}
}
