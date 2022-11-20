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
		// Vanilla Planet Strings
		const string EARTH = "EARTHLIKE;(0,0,0);60000;GREEN;1";
		const string MOON = "MOON;(16388,136375,-113547);9394;GRAY;1";
		const string MARS = "MARS;(1032762,134086,1632659);64606;RUST;1";
		const string EUROPA = "EUROPA;(916410,16373.72,1616441);9600;LIGHTBLUE;1";
		const string ALIEN = "ALIEN;(131110.8,131220.6,5731113);60894.06;MAGENTA;1";
		const string TITAN = "TITAN;(36385.04,226384,5796385);9238.224;CYAN;1";
		const string TRITON = "TRITON;(-284463.6,-2434464,365536.2);38128.81;WHITE;1";
		const string PERTAM = "PERTAM;(-3967231.50,-32231.50,-767231.50);30066.50;BROWN;1";

		const int STRING_LENGTH = 5; // Length of Planet Data String
		

		static List<Planet> _planetList;
		static List<Planet> _unchartedList;
		static List<Waypoint> _waypointList;


		// LOCATION //
		public class Location
		{
			public String name;
			public Vector3 position;
			public List<Vector3> transformedCoords;
			public String color;
			public float Distance;

			public Location() { }
		}


		// WAYPOINT //
		public class Waypoint : Location
		{
			public String marker;
			public bool isActive;

			public Waypoint()
			{
				transformedCoords = new List<Vector3>();
			}
		}


		// PLANET //
		public class Planet : Location
		{
			public float radius;
			public Vector2 mapPos;
			public int SampleCount;

			public Planet(String planetString)
			{
				string[] planetData = planetString.Split(';');

				name = planetData[0];

				transformedCoords = new List<Vector3>();

				if (planetData.Length < STRING_LENGTH)
				{
					radius = 0;
					return;
				}

				if (planetData[1] != "")
				{
					position = StringToVector3(planetData[1]);
				}

				if (planetData[2] != "")
				{
					radius = float.Parse(planetData[2]);
					//isCharted = true;
				}
				else
				{
					radius = 0;
				}

				color = planetData[3];
				SampleCount = ParseInt(planetData[4], 1);
			}

			public override String ToString()
			{
				String[] planetData = new String[9];

				planetData[0] = name;
				planetData[1] = Vector3ToString(position);

				float radius = this.radius;
				if (radius > 0)
				{
					planetData[2] = radius.ToString();
				}
				else
				{
					planetData[2] = "";
				}

				planetData[3] = color;

				planetData[4] = SampleCount.ToString();

				String planetString = planetData[0];
				for (int i = 1; i < STRING_LENGTH; i++)
				{
					planetString = planetString + ";" + planetData[i];
				}
				return planetString;
			}
		}


		// CYCLE PLANETS //
		void CyclePlanetsForList(List<StarMap> maps, bool next)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				CyclePlanets(map, next);
			}
		}

		void CyclePlanets(StarMap map, bool next)
        {
			if (!_planets)
			{
				AddMessage("No Planets Logged!");
				return;
			}

			map.DefaultView();

			if (next)
			{
				map.PlanetIndex++;
			}
			else
			{
				map.PlanetIndex--;
			}

			if (map.PlanetIndex < 0)
			{
				map.PlanetIndex = _planetList.Count - 1;
			}
			else if (map.PlanetIndex >= _planetList.Count)
			{
				map.PlanetIndex = 0;
			}

			SelectPlanet(_planetList[map.PlanetIndex], map);
        }



		// GET PLANET //
		static Planet GetPlanet(string planetName)
		{
			if (planetName == "" || planetName == "[null]")
				return null;

			if (_unchartedList.Count > 0)
			{
				foreach (Planet uncharted in _unchartedList)
				{
					if (uncharted.name.ToUpper() == planetName.ToUpper())
					{
						return uncharted;
					}
				}
			}

			if (_planets)
			{
				foreach (Planet planet in _planetList)
				{
					if (planet.name.ToUpper() == planetName.ToUpper())
					{
						return planet;
					}
				}
			}

			return null;
		}


		// GET WAYPOINT //
		static Waypoint GetWaypoint(string waypointName)
		{
			if (_waypointList.Count > 0)
			{
				foreach (Waypoint waypoint in _waypointList)
				{
					if (waypoint.name.ToUpper() == waypointName.ToUpper())
					{
						return waypoint;
					}
				}
			}

			return null;
		}


		// CYCLE WAYPOINTS //
		void CycleWaypoints(StarMap map, bool next)
        {
			int gpsCount = _waypointList.Count;

			if (gpsCount < 1)
			{
				AddMessage("No Waypoints Logged!");
				return;
			}

			map.DefaultView();

			if (next)
			{
				map.WaypointIndex++;
			}
			else
			{
				map.WaypointIndex--;
			}


			if (map.WaypointIndex == -1)
			{
				map.ActiveWaypoint = null;
				map.ActiveWaypointName = "";
				//MapToParameters(map);
				return;
			}
			else if (map.WaypointIndex < -1)
			{
				map.WaypointIndex = gpsCount - 1;
			}
			else if (map.WaypointIndex >= gpsCount)
			{
				map.WaypointIndex = -1;
				map.ActiveWaypoint = null;
				map.ActiveWaypointName = "";
				//MapToParameters(map);
				return;
			}

			Waypoint waypoint = _waypointList[map.WaypointIndex];
			map.Center = waypoint.position;
			map.ActiveWaypoint = waypoint;
			map.ActiveWaypointName = waypoint.name;
			map.UpdateBasicParameters();
			//MapToParameters(map);
		}

		void CycleWaypointsForList(List<StarMap> maps, bool next)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				CycleWaypoints(map, next);
			}
		}




		// SELECT PLANET //
		void SelectPlanet(Planet planet, StarMap map)
		{
			map.Center = planet.position;
			map.ActivePlanetName = planet.name;

			if (planet.name != "" && planet.name != "[null]")
				map.ActivePlanet = GetPlanet(planet.name);


			if (planet.radius < 27000)
			{
				map.FocalLength *= 4;
			}
			else if (planet.radius < 40000)
			{
				map.FocalLength *= 3;
				map.FocalLength /= 2;
			}

			map.UpdateBasicParameters();
		}


		// GET DISTANCE //
		float GetDistance(Location location)
        {
			return Vector3.Distance(location.position, _myPos);
        }

		
		// UPDATE DISTANCES //
		void UpdateDistances()
        {


			if(_waypointList.Count > 0)
            {
				foreach (Waypoint waypoint in _waypointList)
                {
					waypoint.Distance = GetDistance(waypoint);
				}
					
            }

			if(_planetList.Count > 0)
            {
				foreach (Planet planet in _planetList)
                {
					planet.Distance = GetDistance(planet) - planet.radius;
				}
					
            }
        }



		// SORT BY NEAREST //	 Sorts Planets by nearest to farthest.
		public void SortByNearest(List<Planet> planets)
		{
			int length = planets.Count;
			if (length > 1)
			{
				for (int i = 0; i < length - 1; i++)
				{
					for (int p = 1; p < length; p++)
					{
						Planet planetA = planets[p - 1];
						Planet planetB = planets[p];

						float distA = planetA.Distance;
						float distB = planetB.Distance;

						if (distB < distA)
						{
							planets[p - 1] = planetB;
							planets[p] = planetA;
						}
					}

					length--;
					if (length < 2)
					{
						return;
					}
				}
			}
		}


		// SORT WAYPOINTS //
		void SortWaypoints(List<Waypoint> waypoints)
		{
			int length = waypoints.Count;

			for (int i = 0; i < length - 1; i++)
			{
				for (int w = 1; w < length; w++)
				{
					Waypoint pointA = waypoints[w - 1];
					Waypoint pointB = waypoints[w];

					float distA = pointA.Distance;
					float distB = pointB.Distance;

					if (distB < distA)
					{
						waypoints[w - 1] = pointB;
						waypoints[w] = pointA;
					}
				}

				length--;
				if (length < 2)
				{
					return;
				}
			}
		}


		// SORT GLOBAL WAYPOINTS //
		void SortGlobalWaypoints()
        {
			if (_waypointList.Count < 1)
				return;

			_sortCounter++;
			if (_sortCounter >= 10)
			{
				SortWaypoints(_waypointList);
				_sortCounter = 0;
			}
		}


		// SORT PLANETS FOR MAPS //
		void SortPlanetsForMaps()
        {
			if (_planets)
			{
				//Sort Planets by proximity to ship.
				SortByNearest(_planetList);
				_nearestPlanet = _planetList[0];

				if (_mapList.Count < 1)
					return;

				foreach (StarMap map in _mapList)
				{
					if (map.Mode == "PLANET" || map.Mode == "CHASE" || map.Mode == "ORBIT")
					{
						UpdateMap(map);
					}
				}
			}
		}


		// LOAD VANILLA PLANETS //
		void LoadVanillaPlanets()
        {
			// List of Vanilla Planet Data Strings
			List<string> planetData = new List<string> { EARTH, MOON, MARS, EUROPA, ALIEN, TITAN, TRITON, PERTAM };
			
			foreach(string entry in planetData)
            {
				// Check to see if planet with same name is already logged
				string planetName = entry.Split(';')[0];
				Planet planet = GetPlanet(planetName);

				if (planet == null)
					_planetList.Add(new Planet(entry));
				else
					AddMessage("Planet of name \"" + planetName + "\" already logged.\n");
            }

			DataToLog();
        }


		// CONVERT OLD PLANET DATA //
		string ConvertOldPlanetData(string dataToCheck)
        {
			string [] data = dataToCheck.Split(';');
			int length = data.Length;

			if (length == 5)
				return dataToCheck;

			string dataOut = "";
			
			// Name
			if (dataToCheck != "")
				dataOut += data[0] + ";";
			else
				dataOut += "ERROR;";

			// Center
			if (length > 1 && data[1] != "")
				dataOut += data[1] + ";";
			else
				dataOut += "(0,0,0);";

			// Radius
			if (length > 2 && data[2] != "")
				dataOut += data[2] + ";";
			else
				dataOut += "0";

			// Color
			if (length > 3)
				dataOut += data[3];

			dataOut += ";1";

			return dataOut;
		}
	}
}
