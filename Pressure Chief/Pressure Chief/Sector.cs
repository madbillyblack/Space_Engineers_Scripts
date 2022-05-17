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
		// SECTOR // - Class that includes all components for a specific room.
		public class Sector
		{
			public IMyBlockGroup Group;
			public string Name; // Name or Designation for Sector
			public List<IMyAirVent> Vents;
			public List<IMyDoor> Doors;
			public List<PressureLight> Lights;
			//public List<IMyTextSurface> Surfaces; // LCD screens used to display pressure readings
			public List<IMyShipMergeBlock> MergeBlocks;
			public List<IMyShipConnector> Connectors;
			//public List<Bulkhead> Bulkheads;
			public List<GaugeSurface> Gauges;
			public string Type; // Room, Lock, Dock, or Vacuum
			public string NormalColor; // Default pressurized light color for sector
			public string EmergencyColor; // Default depressurized light color for sector
			public string Status; // Current pressure status read from main Vents[0]
			public IMyTimerBlock LockTimer;
			public IMySoundBlock LockAlarm;
			public bool IsPressurized;
			public bool Depressurizing;
			public bool HasChanged;

			// Constructor
			public Sector(IMyBlockGroup blockGroup)
			{
				//Initialize Lists
				Vents = new List<IMyAirVent>();
				Doors = new List<IMyDoor>();
				Lights = new List<PressureLight>();
				MergeBlocks = new List<IMyShipMergeBlock>();
				Connectors = new List<IMyShipConnector>();
				Gauges = new List<GaugeSurface>();
				//Surfaces = new List<IMyTextSurface>(); //OBSOLETE
				//Bulkheads = new List<Bulkhead>(); //OBSOLETE

				Group = blockGroup;
				SetName();
				AssignVents();
				AssignDoors(); //And add to Bulkhead
				AssignLights();
				AssignTimer();
			}


			// SET NAME
			private void SetName()
			{
				string[] nameArray = Group.Name.Split(':');
				if (nameArray.Length < 2)
				{
					_buildMessage += "INVALID SECTOR GROUP NAME!\n" + Group.Name;
					return;
				}
				Name = nameArray[1].Trim();

				// Designate exterior sector
				if (Name == _vacTag)
					Type = "Vacuum";
				else
					Type = "Room";
			}


			// ASSIGN VENTS
			private void AssignVents()
			{
				Group.GetBlocksOfType<IMyAirVent>(Vents);
				if (Vents.Count < 1)
				{
					_buildMessage += "NO AIR VENTS ASSIGNED TO GROUP " + Group.Name + "!";
					return;
				}

				IMyAirVent airVent = Vents[0];
				Vents.Add(airVent);
				Status = IniKey.GetKey(airVent, INI_HEAD, "Status", airVent.Status.ToString());
				IsPressurized = airVent.GetOxygenLevel() >= 1 - THRESHHOLD;

				//Name = TagFromName(airVent.CustomName);
				NormalColor = IniKey.GetKey(airVent, INI_HEAD, "Normal_Color", NORMAL);
				EmergencyColor = IniKey.GetKey(airVent, INI_HEAD, "Emergency_Color", EMERGENCY);
			}


			// ASSIGN DOORS
			private void AssignDoors()
			{
				Group.GetBlocksOfType<IMyDoor>(Doors);
				if (Doors.Count < 1)
				{
					_buildMessage += "NO DOORS ASSIGNED TO GROUP" + Group.Name + "!";
					return;
				}

				// Assign Sector to Bulkhead
				foreach (IMyDoor door in Doors)
				{
					Bulkhead bulkhead = BulkheadFromDoor(door);
					if (bulkhead != null && bulkhead.Sectors.Count < 2 && !bulkhead.Sectors.Contains(this))
						bulkhead.Sectors.Add(this);

				}
			}


			// ASSIGN LIGHTS
			private void AssignLights()
			{
				List<IMyLightingBlock> lightingBlocks = new List<IMyLightingBlock>();
				Group.GetBlocksOfType<IMyLightingBlock>(lightingBlocks);
				if (lightingBlocks.Count < 1)
					return;

				foreach (IMyLightingBlock lightingBlock in lightingBlocks)
				{
					PressureLight light = new PressureLight(lightingBlock);
					Lights.Add(light);
				}
			}


			// ASSIGN TIMER
			private void AssignTimer()
			{
				List<IMyTimerBlock> timers = new List<IMyTimerBlock>();
				Group.GetBlocksOfType<IMyTimerBlock>(timers);

				if (timers.Count < 1)
					return;
				else if (timers.Count > 1)
				{
					_buildMessage += "WARNING: Sector " + Name + " has more than 1 timer assigned.\n* Only timer " + timers[0].CustomName + " will be used.";
				}
				else if (Type == "Vacuum")
                {
					_buildMessage += "WARNING: Exterior Sector has no purpose for a Timer block. Either remove timer from Exterior Sector Group, or Rename Group to not be exterior.";
					return;
                }

				LockTimer = timers[0];
				Type = "Lock";
				AssignAlarm();

				// Check if Lock has merge blocks required to be a dock.
				AssignMergeBlocks();
			}


			// ASSIGN ALARM
			private void AssignAlarm()
			{
				List<IMySoundBlock> soundBlocks = new List<IMySoundBlock>();
				Group.GetBlocksOfType<IMySoundBlock>(soundBlocks);

				if (soundBlocks.Count < 1)
					return;
				else if (soundBlocks.Count > 1)
				{
					_buildMessage += "WARNING: Sector " + Name + " has more than 1 sound block assigned.\n* Only sound block " + soundBlocks[0].CustomName + " will be used.";
				}

				LockAlarm = soundBlocks[0];

				if (LockTimer == null)
					_buildMessage += "WARNING: Sector " + Name + " has sound block assigned but no timer\n* Air Locks require a timer to function!";
			}


			// ASSIGN MERGE BLOCKS
			private void AssignMergeBlocks()
			{
				Group.GetBlocksOfType<IMyShipMergeBlock>(MergeBlocks);
				if (MergeBlocks.Count < 1)
					return;

				Type = "Dock";
				AssignConnectors();
			}


			// ASSIGN CONNECTORS
			private void AssignConnectors()
			{
				Group.GetBlocksOfType<IMyShipConnector>(Connectors);

				if (Connectors.Count > 0 && MergeBlocks.Count < 1)
				{
					_buildMessage += "WARNING: Secctor " + Name + " has no merge blocks assigned.\n* Docking Ports require at least 1 merge block in order to function.";
				}
			}


			// CHECK - Check all pressure status between this and all neighboring sectors and update lights based on pressure status.
			public void Check()
			{
				bool pressurized = Vents[0].GetOxygenLevel() >= 1 - THRESHHOLD;
				bool depressurize = Vents[0].Depressurize;
				if (IsPressurized == pressurized && Depressurizing == depressurize)
					HasChanged = false;
				else
					HasChanged = true;

				IsPressurized = pressurized;
				DrawGauges();


				if (!HasChanged)
					return;

				UpdateLights(pressurized);

				if (Type == "Room")
					CloseDoors();
			}


			// UPDATE STATUS - Update the pressurization status of vent's custom data, and closed doors if status has changed. 
			public void UpdateStatus()
			{
				IMyAirVent airVent = Vents[0];
				string oldStatus = IniKey.GetKey(airVent, INI_HEAD, "Status", "0");
				float o2Level;
				if (!float.TryParse(oldStatus, out o2Level))
					o2Level = 0;

				if (Type == "Room")
				{
					if (Math.Abs(o2Level - airVent.GetOxygenLevel()) > THRESHHOLD)
						CloseDoors();
				}

				IniKey.SetKey(airVent, INI_HEAD, "Status", airVent.GetOxygenLevel().ToString());
			}


			// CLOSE DOORS //
			public void CloseDoors()
			{
				if (Doors.Count < 1)
					return;

				foreach (IMyDoor myDoor in Doors)
				{
					myDoor.CloseDoor();
				}
			}



			// GET EXTERIOR BULKHEAD - Returns bulkhead between this sector and exterior
			public Bulkhead GetExteriorBulkhead()
			{
				if (_bulkheads.Count > 0)
				{
					// Return null if exterior vent.
					if (Name == _vacTag)
					{
						_statusMessage = "Command not applicable to Exterior";
						return null;
					}

					foreach (Bulkhead bulkhead in _bulkheads)
					{
						if (bulkhead.Sectors.Contains(this) && bulkhead.Sectors.Contains(GetSector(_vacTag)))
							return bulkhead;
					}
				}

				_statusMessage = "Can't find exterior bulkhead for " + Name + "!";
				return null;
			}


			// SET COLOR //
			public void SetColor(string colorData, bool emergency)
			{

				if (emergency)
				{
					foreach (PressureLight light in Lights)
					{
						IniKey.SetKey(light.LightBlock, INI_HEAD, "Emergency_Color", colorData);
						light.EmergencyColor = Util.ColorFromString(colorData);
					}
					//IniKey.SetKey(Vents[0], INI_HEAD, "Emergency_Color", colorData);
				}
				else
				{
					foreach (PressureLight light in Lights)
					{
						IniKey.SetKey(light.LightBlock, INI_HEAD, "Normal_Color", colorData);
						light.NormalColor = Util.ColorFromString(colorData);
					}
					//IniKey.SetKey(Vents[0], INI_HEAD, "Normal_Color", colorData);
				}
			}


			// SET RADIUS //
			public void SetRadius(string radius, bool emergency)
			{
				foreach (PressureLight light in Lights)
				{
					if (emergency)
					{
						IniKey.SetKey(light.LightBlock, INI_HEAD, "Emergency_Radius", radius);
						light.EmergencyRadius = float.Parse(radius);
					}

					else
					{
						IniKey.SetKey(light.LightBlock, INI_HEAD, "Normal_Radius", radius);
						light.NormalRadius = float.Parse(radius);
					}
				}
			}


			// SET INTENSITY //
			public void SetIntensity(string intensity, bool emergency)
			{
				foreach (IMyLightingBlock light in Lights)
				{
					if (emergency)
						IniKey.SetKey(light, INI_HEAD, "Emergency_Intensity", intensity);
					else
						IniKey.SetKey(light, INI_HEAD, "Normal_Intensity", intensity);
				}
			}


			// SET PRESSURE STATUS // Update IsPressurized bool based on Vent Pressure.
			public void SetPressureStatus()
			{
				IsPressurized = Vents[0].GetOxygenLevel() >= 1 - THRESHHOLD;
			}

			public void SetPressureStatus(bool doorOverride)
			{
				bool pressurized = Vents[0].GetOxygenLevel() >= 1 - THRESHHOLD;

				if (pressurized && !doorOverride)
					IsPressurized = true;
				else
					IsPressurized = false;
			}


			// UPDATE LIGHTS //
			public void UpdateLights(bool pressurized)
			{
				if (Lights.Count < 1)
					return;

				//bool depressurized = Vents[0].GetOxygenLevel() < 0.7 || Vents[0].Depressurize;

				foreach (PressureLight myLight in Lights)
				{
					if (!pressurized || Vents[0].Depressurize)
					{
						myLight.LightBlock.Color = Util.ColorFromString(IniKey.GetKey(myLight.LightBlock, INI_HEAD, "Emergency_Color", EmergencyColor));
						try
						{
							myLight.LightBlock.Radius = float.Parse(IniKey.GetKey(myLight.LightBlock, INI_HEAD, "Emergency_Radius", "0"));
							myLight.LightBlock.Intensity = float.Parse(IniKey.GetKey(myLight.LightBlock, INI_HEAD, "Emergency_Intensity", "0"));
						}
						catch
						{
							_statusMessage = "WARNING: " + myLight.LightBlock.CustomName +
											"\ncontains invalid parameters!" +
											"\nCheck Custom Data inputs!";
						}
					}
					else
					{
						myLight.LightBlock.Color = Util.ColorFromString(IniKey.GetKey(myLight.LightBlock, INI_HEAD, "Normal_Color", NormalColor));

						try
						{
							myLight.LightBlock.Radius = float.Parse(IniKey.GetKey(myLight.LightBlock, INI_HEAD, "Normal_Radius", "0"));
							myLight.LightBlock.Intensity = float.Parse(IniKey.GetKey(myLight.LightBlock, INI_HEAD, "Normal_Intensity", "0"));
						}
						catch
						{
							_statusMessage = "WARNING: " + myLight.LightBlock.CustomName +
											"\ncontains invalid parameters!" +
											"\nCheck Custom Data inputs!";
						}
					}
				}
			}


			// DOCKED // Returns if sector is docked to another grid
			public bool Docked()
			{
				if (Type != "DOCK" || MergeBlocks.Count < 1)
					return false;

				foreach (IMyShipMergeBlock mergeBlock in MergeBlocks)
				{
					if (mergeBlock.IsConnected)
						return true;
				}

				return false;
			}


			// DRAW GAUGES //
			public void DrawGauges()
            {
				if (Gauges.Count < 1)
					return;

				foreach(GaugeSurface gauge in Gauges)
                {
					DrawSingleSectorGauge(gauge, this);
                }
            }
		}
	}
}
