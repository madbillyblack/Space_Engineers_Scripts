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
		class DataDisplay
        {
			int CurrentPage;
			List<string> Page1;
			//TODO - More Pages
        }

		// DISPLAY DATA // Page selection interface for Data Display
		void DisplayData()
		{
			if (_dataSurface == null)
				return;

			switch (_pageIndex)
			{
				case 0:
					DisplayGPSInput();
					break;
				case 1:
					DisplayPlanetData();
					break;
				case 2:
					DisplayWaypointData();
					break;
				case 3:
					DisplayMapData();
					break;
				case 4:
					DisplayClipboard();
					break;
			}
		}


		// DISPLAY GPS INPUT //
		void DisplayGPSInput()
		{
			if (_cycleStep > 0 || _dataSurface == null)
				return;

			StringBuilder menuText = new StringBuilder();
			_dataSurface.ReadText(menuText, false);

			string output = menuText.ToString();

			if (!output.StartsWith(GPS_INPUT))
				output = GPS_INPUT + SLASHES + "\n~Input Terminal Coords Below This Line~";

			_dataSurface.WriteText(output);
		}


		// DISPLAY PLANET DATA //
		void DisplayPlanetData()
		{
			string output = "// PLANET LIST" + SLASHES;
			List<string> planetData = new List<string>();
			planetData.Add("Charted: " + _planetList.Count + "	  Uncharted: " + _unchartedList.Count);

			if (_planets)
			{
				foreach (Planet planet in _planetList)
				{
					float surfaceDistance = (Vector3.Distance(planet.position, _myPos) - planet.radius) / 1000;
					if (surfaceDistance < 0)
					{
						surfaceDistance = 0;
					}
					string planetEntry;
					if (planet.name.ToUpper() == _activePlanet.ToUpper())
					{
						planetEntry = ">>> ";
					}
					else
					{
						planetEntry = "		   ";
					}

					planetEntry += planet.name + "	  R: " + abbreviateValue(planet.radius) + "m    dist: " + surfaceDistance.ToString("N1") + "km";

					planetData.Add(planetEntry);
				}
			}

			if (_unchartedList.Count > 0)
			{
				string unchartedHeader = "\n----Uncharted Planets----";
				planetData.Add(unchartedHeader);
				foreach (Planet uncharted in _unchartedList)
				{
					float unchartedDistance = Vector3.Distance(_myPos, uncharted.GetPoint(1)) / 1000;

					string unchartedEntry = "  " + uncharted.name + "	 dist: " + unchartedDistance.ToString("N1") + "km";

					planetData.Add(unchartedEntry);
				}
			}

			output += ScrollToString(planetData);

			_dataSurface.WriteText(output);
		}


		// DISPLAY WAYPOINT DATA //
		void DisplayWaypointData()
		{
			string output = "// WAYPOINT LIST" + SLASHES;

			List<string> waypointData = new List<string>();
			waypointData.Add("Count: " + _waypointList.Count);

			if (_waypointList.Count > 0)
			{
				foreach (Waypoint waypoint in _waypointList)
				{
					float distance = Vector3.Distance(_myPos, waypoint.position) / 1000;
					string status = "Active";
					if (!waypoint.isActive)
					{
						status = "Inactive";
					}
					string waypointEntry = "		";
					if (waypoint.name.ToUpper() == _activeWaypoint.ToUpper())
					{
						waypointEntry = ">>> ";
					}
					waypointEntry += waypoint.name + "	  " + waypoint.marker + "	  " + status + "	  dist: " + distance.ToString("N1") + "km";
					waypointData.Add(waypointEntry);
				}
			}

			output += ScrollToString(waypointData);

			_dataSurface.WriteText(output);
		}


		// DISPLAY MAP DATA //
		void DisplayMapData()
		{
			string output = "// MAP DATA" + SLASHES;

			if (_statusMessage != "")
				output += "\n" + _statusMessage;

			if (_mapBlocks.Count > 0)
			{
				List<string> mapData = new List<string>();
				foreach (StarMap map in _mapList)
				{
					mapData.Add("MAP " + map.Number + " --- " + map.Viewport.Width.ToString("N0") + " x " + map.Viewport.Height.ToString("N0") + " --- " + map.Mode + " Mode");
					mapData.Add("   On: " + map.Block.CustomName + "   Screen: " + map.Index);

					if (map.ActivePlanetName != "")
						mapData.Add("   Selected Planet: " + map.ActivePlanetName);

					string hidden = "   Hidden:";
					if (!map.ShowInfo)
						hidden += " Stat-Bars ";

					if (map.GpsState == 0)
						hidden += " GPS ";

					if (!map.ShowNames)
						hidden += " Names ";

					if (!map.ShowShip)
						hidden += " Ship";

					if (map.GpsState == 0 || !map.ShowInfo || !map.ShowShip && !map.ShowNames)
						mapData.Add(hidden);

					mapData.Add("   Center: " + Vector3ToString(map.Center));
					mapData.Add("   Azimuth: " + map.Azimuth + "°   Altitude: " + map.Altitude * -1 + "°");
					mapData.Add("   Focal Length: " + abbreviateValue(map.FocalLength) + "   Radius: " + abbreviateValue(map.RotationalRadius) + "\n");
				}

				output += ScrollToString(mapData);
			}
			else
			{
				output += "\n\n NO MAP BLOCKS TO DISPLAY!!!";
			}

			_dataSurface.WriteText(output);
		}


		// DISPLAY CLIPBOARD //
		void DisplayClipboard()
		{
			string output = "// CLIPBOARD" + SLASHES + "\n\n" + _clipboard;
			_dataSurface.WriteText(output);
		}


		// SCROLL TO STRING // Returns string from List based on ScrollIndex
		string ScrollToString(List<string> dataList)
		{
			string output = "";

			if (dataList.Count > 0 && _scrollIndex < dataList.Count)
			{
				for (int d = _scrollIndex; d < dataList.Count; d++)
				{
					output += "\n" + dataList[d];
				}
			}

			return output;
		}


		// NEXT PAGE //
		void NextPage(bool next)
		{
			if (next)
			{
				_pageIndex++;
				if (_pageIndex >= DATA_PAGES)
					_pageIndex = 0;
				return;
			}

			if (_pageIndex == 0)
				_pageIndex = DATA_PAGES;

			_pageIndex--;
		}


		// PAGE SCROLL //
		void pageScroll(string arg)
		{
			switch (arg)
			{
				case "UP":
					_scrollIndex--;
					if (_scrollIndex < 0)
						_scrollIndex = 0;
					break;
				case "DOWN":
					_scrollIndex++;
					break;
				case "HOME":
					_scrollIndex = 0;
					break;
				default:
					_statusMessage = "INVALID SCROLL COMMAND";
					break;
			}
		}
	}
}
