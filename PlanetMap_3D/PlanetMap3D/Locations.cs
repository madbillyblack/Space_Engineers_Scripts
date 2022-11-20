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
			/*
			public Vector3 point1;
			public Vector3 point2;
			public Vector3 point3;
			public Vector3 point4;
			*/
			public Vector2 mapPos;
			//public bool isCharted;

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
				/*
				if (planetData[4] != "")
				{
					SetPoint(1, StringToVector3(planetData[4]));
				}

				if (planetData[5] != "")
				{
					SetPoint(2, StringToVector3(planetData[5]));
				}

				if (planetData[6] != "")
				{
					SetPoint(3, StringToVector3(planetData[6]));
				}

				if (planetData[7] != "")
				{
					SetPoint(4, StringToVector3(planetData[7]));
				}*/
			}

			/*
			public void SetPoint(int point, Vector3 vec3)
			{
				switch (point)
				{
					case 1:
						point1 = vec3;
						break;
					case 2:
						point2 = vec3;
						break;
					case 3:
						point3 = vec3;
						break;
					case 4:
						point4 = vec3;
						break;
				}
			}

			public Vector3 GetPoint(int point)
			{
				Vector3 pointN = new Vector3();

				switch (point)
				{
					case 1:
						pointN = point1;
						break;
					case 2:
						pointN = point2;
						break;
					case 3:
						pointN = point3;
						break;
					case 4:
						pointN = point4;
						break;
				}
				return pointN;
			}*/

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

				/*
				for (int c = 4; c < 8; c++)
				{
					if (GetPoint(c - 3) != Vector3.Zero)
					{
						planetData[c] = Vector3ToString(GetPoint(c - 3));
					}
				}*/

				planetData[4] = SampleCount.ToString();

				String planetString = planetData[0];
				for (int i = 1; i < STRING_LENGTH; i++)
				{
					planetString = planetString + ";" + planetData[i];
				}
				return planetString;
			}

			/*
			public void SetMajorAxes()
			{
				//USE RADIUS AND CENTER OF PLANET TO SET POINTS 1, 2 & 3 TO BE ALONG X,Y & Z AXES FROM PLANET CENTER
				float xCenter = position.X;
				float yCenter = position.Y;
				float zCenter = position.Z;

				Vector3 xMajor = new Vector3(xCenter + radius, yCenter, zCenter);
				Vector3 yMajor = new Vector3(xCenter, yCenter + radius, zCenter);
				Vector3 zMajor = new Vector3(xCenter, yCenter, zCenter + radius);

				SetPoint(1, xMajor);
				SetPoint(2, yMajor);
				SetPoint(3, zMajor);
			}

			public void Calculate()
			{
				//GET TVALUES OF ALL POINTS THEN ADD TO ARRAY
				double t1 = TValue(point1);
				double t2 = TValue(point2);
				double t3 = TValue(point3);
				double t4 = TValue(point4);
				double[] arrT = new double[] { t1, t2, t3, t4 };

				//BUILD MATRIX T WITH POINTS 1,2,3 & 4, AND A COLUMN OF 1's
				double[,] matrixT = new double[4, 4];
				for (int c = 0; c < 3; c++)
				{
					matrixT[0, c] = point1.GetDim(c);
				}
				for (int d = 0; d < 3; d++)
				{
					matrixT[1, d] = point2.GetDim(d);
				}
				for (int e = 0; e < 3; e++)
				{
					matrixT[2, e] = point3.GetDim(e);
				}
				for (int f = 0; f < 3; f++)
				{
					matrixT[3, f] = point4.GetDim(f);
				}
				for (int g = 0; g < 4; g++)
				{
					matrixT[g, 3] = 1;
				}

				double[,] matrixD = new double[4, 4];
				ReplaceColumn(matrixT, matrixD, arrT, 0);

				double[,] matrixE = new double[4, 4];
				ReplaceColumn(matrixT, matrixE, arrT, 1);

				double[,] matrixF = new double[4, 4];
				ReplaceColumn(matrixT, matrixF, arrT, 2);

				double[,] matrixG = new double[4, 4];
				ReplaceColumn(matrixT, matrixG, arrT, 3);

				double detT = Det4(matrixT);
				double detD = Det4(matrixD) / detT;
				double detE = Det4(matrixE) / detT;
				double detF = Det4(matrixF) / detT;
				double detG = Det4(matrixG) / detT;

				Vector3 newCenter = new Vector3(detD / -2, detE / -2, detF / -2);
				position = newCenter;

				double newRad = Math.Sqrt(detD * detD + detE * detE + detF * detF - 4 * detG) / 2;
				radius = (float)newRad;

				SetMajorAxes();
			}*/
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
