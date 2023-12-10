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
		// BUILD // - Updates map info from map's custom data
		void Build()
		{
			// Initialize Basic System Settings
			_messages = new List<string>();
			_cycleStep = CYCLE_LENGTH;
			_lightOn = true;

			// Initialize Lists
			_planetList = new List<Planet>();
			_unchartedList = new List<Planet>();
			_waypointList = new List<Waypoint>();
			_mapList = new List<StarMap>();
			_mapBlocks = new List<IMyTerminalBlock>();
			_mapMenus = new List<MapMenu>();

			//Grid Tag
			_gridID = GetKey(Me, SHARED, "Grid_ID", Me.CubeGrid.EntityId.ToString());

			AssignRefBlock();
			AssignMaps();

			LoadPlanetData();
			LoadWaypointData();

			UpdateMapDataPage();
			AssignDataDisplays();
			
			AssignMenus();

			SetScanCamera();

			SetRefreshRate();
		}


		// ASSIGN REFBLOCK //
		void AssignRefBlock()
        {
			//Name of reference block
			string refName = GetKey(Me, PROGRAM_HEAD, "Reference_Name", "[Reference]");

			if (refName == "")
			{
				AddMessage("WARNING: No Reference Block Name Specified!\nMay result in false orientation!");
				_refBlock = Me as IMyTerminalBlock;
			}
			else
			{
				List<IMyTerminalBlock> refBlocks = new List<IMyTerminalBlock>();
				GridTerminalSystem.SearchBlocksOfName(refName, refBlocks);



				if (refBlocks.Count > 0)
				{
					foreach (IMyTerminalBlock block in refBlocks)
					{
						if(GetKey(block, SHARED, "Grid_ID", _gridID) == _gridID)
						{
                            _refBlock = block as IMyTerminalBlock;
                            Echo("Reference: " + _refBlock.CustomName);
                            _myPos = _refBlock.GetPosition();
							return;
                        }
					}
				}

                AddMessage("WARNING: No Block containing " + refName + " found.\nMay result in false orientation!");
                _refBlock = Me as IMyTerminalBlock;
                _myPos = _refBlock.GetPosition();

            }
		}


		// ASSIGN MAPS //
		void AssignMaps()
        {
			//Tag to designate screens for map displays
			_mapTag = GetKey(Me, PROGRAM_HEAD, "MAP_TAG", "[MAP]");

			if (_mapTag == "")
			{
				Echo("No LCD specified!!!");
			}
			else
			{
				GridTerminalSystem.SearchBlocksOfName(_mapTag, _mapBlocks);

				if(_mapBlocks.Count < 1)
                {
					AddMessage("No screens with tag \"" + _mapTag + "\" found!");
					return;
                }

				foreach (IMyTerminalBlock mapBlock in _mapBlocks)
				{
					Echo(mapBlock.CustomName);

					if (onGrid(mapBlock) && GetSurfaceCount(mapBlock) > 0)
					{
						List<StarMap> maps = ParametersToMaps(mapBlock);

						if (maps.Count > 0)
						{
							foreach (StarMap map in maps)
								activateMap(map);	
						}
					}
				}
			}
		}


		// LOAD PLANET DATA
		void LoadPlanetData()
        {
			string planetData = GetKey(Me, PROGRAM_HEAD, "Planet_List", "");

			string[] mapEntries = planetData.Split('\n');
			foreach (string planetString in mapEntries)
			{
				string newPlanetString = ConvertOldPlanetData(planetString);
				Planet planet = new Planet(newPlanetString);

				if (planet.radius > 0)
					_planetList.Add(planet);
				/*
				if (planetString.Contains(";"))
				{
					Planet planet = new Planet(planetString);
					if (planet.isCharted)
					{
						_planetList.Add(planet);
					}
					else
					{
						_unchartedList.Add(planet);
					}
				}*/
			}

			// Write planets to INI in case of converted strings
			DataToLog();

			_planets = _planetList.Count > 0;
		}


		// LOAD WAYPOINT DATA //
		void LoadWaypointData()
        {
			string waypointData = GetKey(Me, PROGRAM_HEAD, "Waypoint_List", "");
			string[] gpsEntries = waypointData.Split('\n');

			foreach (string waypointString in gpsEntries)
			{
				if (waypointString.Contains(";"))
				{
					Waypoint waypoint = StringToWaypoint(waypointString);
					_waypointList.Add(waypoint);
				}
			}
		}


		// SET REFRESH RATE //
		void SetRefreshRate()
        {
			//Slow Mode
			_slowMode = ParseBool(GetKey(Me, PROGRAM_HEAD, "Slow_Mode", "false"));
			// Set the continuous update frequency of this script
			if (_slowMode)
				Runtime.UpdateFrequency = UpdateFrequency.Update100;
			else
				Runtime.UpdateFrequency = UpdateFrequency.Update10;
		}
	}
}
