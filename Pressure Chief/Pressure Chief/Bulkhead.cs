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
				LcdOrientations = new List<bool>();
				LcdFlips = new List<bool>();
				LcdBrightnesses = new List<UInt16>();

				Doors.Add(myDoor);
				Override = false;

				TagA = IniKey.GetKey(myDoor, INI_HEAD, "Sector_A", "");
				TagB = IniKey.GetKey(myDoor, INI_HEAD, "Sector_B", "");
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
				if (Surfaces.Count < 1)
					return;

				bool locked = !Doors[0].IsWorking;

				for (int i = 0; i < LCDs.Count; i++)
				{
					IMyTerminalBlock lcd = LCDs[i] as IMyTerminalBlock;
					IMyTextSurface surface = Surfaces[i];
					bool vertical = LcdOrientations[i];
					bool flipped = LcdFlips[i];
					float brightness = LcdBrightnesses[i] * 0.01f;

					string side = IniKey.GetKey(lcd, INI_HEAD, "Side", "Select A or B");

					if (side == "A")
						DrawGauge(surface, Sectors[0], Sectors[1], locked, vertical, flipped, brightness, false);
					else if (side == "B")
						DrawGauge(surface, Sectors[1], Sectors[0], locked, vertical, flipped, brightness, false);
					else
						DrawGauge(surface, Sectors[0], Sectors[1], locked, vertical, flipped, brightness, true);
				}
			}

			// SURFACE TO BULKHEAD // Add LCD, Surface, and related variables to lists in assigned bulkhead.
			public void AddSurfaceFromBlock(IMyTerminalBlock block)
			{
				string name = block.CustomName;
				string side;
				string tagA = "(" + IniKey.GetKey(block, INI_HEAD, "Sector_A", "") + ")";
				string tagB = "(" + IniKey.GetKey(block, INI_HEAD, "Sector_B", "") + ")";

				if (name.Contains(tagA) && name.Contains(TagB))
                {
					_buildMessage += "WARNING: LCD " + name + " is named with tags for both its sectors!\nOnly include a tag for the sector it is pysically located in!";
					side = "Select A or B";
				}	
				else if (name.Contains(tagA))
					side = "A";
				else if (name.Contains(tagB))
					side = "B";
				else
					side = "Select A or B";

				IniKey.EnsureKey(block, INI_HEAD, "Side", side);


				bool vertical;
				if (block.BlockDefinition.SubtypeId.ToString().Contains("Corner_LCD"))
					vertical = false;
				else
					vertical = true;

				IniKey.EnsureKey(block, INI_HEAD, "Screen_Index", "0");
				IniKey.EnsureKey(block, INI_HEAD, "Vertical", vertical.ToString());
				LCDs.Add(block as IMyTextSurfaceProvider);
				Surfaces.Add(MyScreen.PrepareTextSurface(block as IMyTextSurfaceProvider));
				LcdOrientations.Add(Util.ParseBool(IniKey.GetKey(block, INI_HEAD, "Vertical", vertical.ToString())));
				LcdFlips.Add(Util.ParseBool(IniKey.GetKey(block, INI_HEAD, "Flipped", "False")));

				ushort brightness;
				if (!UInt16.TryParse(IniKey.GetKey(block, INI_HEAD, "Brightness", "100"), out brightness))
					brightness = 100;

				LcdBrightnesses.Add(brightness);
			}
		}
	}
}
