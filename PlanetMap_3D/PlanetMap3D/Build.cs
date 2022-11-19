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
			//_statusMessage = "";
			_messages = new List<string>();

			//Grid Tag
			_gridID = GetKey(Me, SHARED, "Grid_ID", Me.CubeGrid.EntityId.ToString());
			_cycleStep = CYCLE_LENGTH;

			_planetList = new List<Planet>();
			_unchartedList = new List<Planet>();
			_waypointList = new List<Waypoint>();
			_mapList = new List<StarMap>();
			_mapBlocks = new List<IMyTerminalBlock>();
			_mapMenus = new List<MapMenu>();

			_mapLog = DataToIni(Me);

			//Name of screen to print map to.
			_mapTag = _mapLog.Get(PROGRAM_HEAD, "MAP_Tag").ToString();

			//Index of screen to print map to.
			//_mapIndex = _mapLog.Get(PROGRAM_HEAD, "MAP_Index").ToUInt16();

			//Index of screen to print map data to.
			//_dataIndex = _mapLog.Get(PROGRAM_HEAD, "Data_Index").ToUInt16();

			//Name of reference block
			_refName = _mapLog.Get(PROGRAM_HEAD, "Reference_Name").ToString();

			//Name of Data Display Block
			//_dataName = _mapLog.Get(PROGRAM_HEAD, "Data_Tag").ToString();

			//Slow Mode
			_slowMode = ParseBool(GetKey(Me, PROGRAM_HEAD, "Slow_Mode", "false"));



			if (_gridID == "")
			{
				_gridID = Me.CubeGrid.EntityId.ToString();
				_mapLog.Set(SHARED, "Grid_ID", _gridID);
				Me.CustomData = _mapLog.ToString();
			}



			if (_mapTag == "" || _mapTag == "<name>")
			{
				Echo("No LCD specified!!!");
			}
			else
			{
				GridTerminalSystem.SearchBlocksOfName(_mapTag, _mapBlocks);
				foreach (IMyTerminalBlock mapBlock in _mapBlocks)
				{
					if (onGrid(mapBlock))
					{
						List<StarMap> maps = ParametersToMaps(mapBlock);

						if (maps.Count > 0)
						{
							foreach (StarMap map in maps)
							{
								activateMap(map);
								//MapToParameters(map);
							}
						}
					}
				}
			}
			/*
			if (_dataName != "" || _dataName != "<name>")
			{
				GridTerminalSystem.SearchBlocksOfName(_dataName, _dataBlocks);
				if (_dataBlocks.Count > 0)
				{
					IMyTextSurfaceProvider dataBlock = _dataBlocks[0] as IMyTextSurfaceProvider;
					_dataSurface = dataBlock.GetSurface(_dataIndex);
					_dataSurface.ContentType = ContentType.TEXT_AND_IMAGE;
				}
			}*/

			if (_refName == "" || _refName == "<name>")
			{
				AddMessage("WARNING: No Reference Block Name Specified!\nMay result in false orientation!");
				_refBlock = Me as IMyTerminalBlock;
			}
			else
			{
				List<IMyTerminalBlock> refBlocks = new List<IMyTerminalBlock>();
				GridTerminalSystem.SearchBlocksOfName(_refName, refBlocks);
				if (refBlocks.Count > 0)
				{
					_refBlock = refBlocks[0] as IMyTerminalBlock;
					Echo("Reference: " + _refBlock.CustomName);
				}
				else
				{
					AddMessage("WARNING: No Block containing " + _refName + " found.\nMay result in false orientation!");
					_refBlock = Me as IMyTerminalBlock;
				}
			}

			_myPos = _refBlock.GetPosition();

			string planetData = _mapLog.Get(PROGRAM_HEAD, "Planet_List").ToString();

			string[] mapEntries = planetData.Split('\n');
			foreach (string planetString in mapEntries)
			{
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
				}
			}
			_planets = _planetList.Count > 0;

			string waypointData = _mapLog.Get(PROGRAM_HEAD, "Waypoint_List").ToString();
			string[] gpsEntries = waypointData.Split('\n');

			foreach (string waypointString in gpsEntries)
			{
				if (waypointString.Contains(";"))
				{
					Waypoint waypoint = StringToWaypoint(waypointString);
					_waypointList.Add(waypoint);
				}
			}

			UpdateMapDataPage();
			AssignDataDisplays();
			
			AssignMenus();

			SetScanCamera();

			// Start with indicator light on.
			_lightOn = true;
		}
	}
}
