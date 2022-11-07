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
		List<Planet> _planetList;
		List<Planet> _unchartedList;
		List<Waypoint> _waypointList;


		// LOCATION //
		public class Location
		{
			public String name;
			public Vector3 position;
			public List<Vector3> transformedCoords;
			public String color;

			public Location() { }
		}


		// WAYPOINT //
		public class Waypoint : Location
		{
			public String marker;
			public bool isActive;

			public Waypoint()
			{
				this.transformedCoords = new List<Vector3>();
			}
		}


		// PLANET //
		public class Planet : Location
		{
			public float radius;
			public Vector3 point1;
			public Vector3 point2;
			public Vector3 point3;
			public Vector3 point4;
			public Vector2 mapPos;
			public bool isCharted;


			public Planet(String planetString)
			{
				string[] planetData = planetString.Split(';');

				this.name = planetData[0];

				this.transformedCoords = new List<Vector3>();

				if (planetData.Length < 8)
				{
					return;
				}

				this.color = planetData[3];

				if (planetData[1] != "")
				{
					this.position = StringToVector3(planetData[1]);
				}

				if (planetData[2] != "")
				{
					this.radius = float.Parse(planetData[2]);
					this.isCharted = true;
				}
				else
				{
					this.isCharted = false;
				}

				if (planetData[4] != "")
				{
					this.SetPoint(1, StringToVector3(planetData[4]));
				}

				if (planetData[5] != "")
				{
					this.SetPoint(2, StringToVector3(planetData[5]));
				}

				if (planetData[6] != "")
				{
					this.SetPoint(3, StringToVector3(planetData[6]));
				}

				if (planetData[7] != "")
				{
					this.SetPoint(4, StringToVector3(planetData[7]));
				}
			}

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
			}

			public override String ToString()
			{
				String[] planetData = new String[8];

				planetData[0] = this.name;
				planetData[1] = Vector3ToString(this.position);

				float radius = this.radius;
				if (radius > 0)
				{
					planetData[2] = radius.ToString();
				}
				else
				{
					planetData[2] = "";
				}

				planetData[3] = this.color;

				for (int c = 4; c < 8; c++)
				{
					if (this.GetPoint(c - 3) != Vector3.Zero)
					{
						planetData[c] = Vector3ToString(this.GetPoint(c - 3));
					}
				}

				String planetString = planetData[0];
				for (int i = 1; i < 8; i++)
				{
					planetString = planetString + ";" + planetData[i];
				}
				return planetString;
			}

			public void SetMajorAxes()
			{
				//USE RADIUS AND CENTER OF PLANET TO SET POINTS 1, 2 & 3 TO BE ALONG X,Y & Z AXES FROM PLANET CENTER
				float xCenter = position.X;
				float yCenter = position.Y;
				float zCenter = position.Z;

				Vector3 xMajor = new Vector3(xCenter + radius, yCenter, zCenter);
				Vector3 yMajor = new Vector3(xCenter, yCenter + radius, zCenter);
				Vector3 zMajor = new Vector3(xCenter, yCenter, zCenter + radius);

				this.SetPoint(1, xMajor);
				this.SetPoint(2, yMajor);
				this.SetPoint(3, zMajor);
			}

			public void CalculatePlanet()
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
				this.position = newCenter;

				double newRad = Math.Sqrt(detD * detD + detE * detE + detF * detF - 4 * detG) / 2;
				this.radius = (float)newRad;

				this.SetMajorAxes();
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
				_statusMessage = "No Planets Logged!";
				return;
			}

			DefaultView(map);

			if (next)
			{
				map.planetIndex++;
			}
			else
			{
				map.planetIndex--;
			}

			if (map.planetIndex < 0)
			{
				map.planetIndex = _planetList.Count - 1;
			}
			else if (map.planetIndex >= _planetList.Count)
			{
				map.planetIndex = 0;
			}

			SelectPlanet(_planetList[map.planetIndex], map);
        }



		// GET PLANET //
		Planet GetPlanet(string planetName)
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
		Waypoint GetWaypoint(string waypointName)
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
				_statusMessage = "No Waypoints Logged!";
				return;
			}

			DefaultView(map);

			if (next)
			{
				map.waypointIndex++;
			}
			else
			{
				map.waypointIndex--;
			}


			if (map.waypointIndex == -1)
			{
				map.activeWaypoint = null;
				map.activeWaypointName = "";
				//MapToParameters(map);
				SetListKey(map.block, MAP_HEADER, "Waypoint", map.activeWaypointName, "", map.index);
				SetListKey(map.block, MAP_HEADER, "Center", Vector3ToString(map.center), "(0,0,0)", map.index);
				return;
			}
			else if (map.waypointIndex < -1)
			{
				map.waypointIndex = gpsCount - 1;
			}
			else if (map.waypointIndex >= gpsCount)
			{
				map.waypointIndex = -1;
				map.activeWaypoint = null;
				map.activeWaypointName = "";
				//MapToParameters(map);
				SetListKey(map.block, MAP_HEADER, "Waypoint", map.activeWaypointName, "", map.index);
				SetListKey(map.block, MAP_HEADER, "Center", Vector3ToString(map.center), "(0,0,0)", map.index);
				return;
			}

			Waypoint waypoint = _waypointList[map.waypointIndex];
			map.center = waypoint.position;
			map.activeWaypoint = waypoint;
			map.activeWaypointName = waypoint.name;
			//MapToParameters(map);
			SetListKey(map.block, MAP_HEADER, "Waypoint", map.activeWaypointName, "", map.index);
			SetListKey(map.block, MAP_HEADER, "Center", Vector3ToString(map.center), "(0,0,0)", map.index);
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
			map.center = planet.position;
			map.activePlanetName = planet.name;

			if (planet.name != "" && planet.name != "[null]")
				map.activePlanet = GetPlanet(planet.name);


			if (planet.radius < 27000)
			{
				map.focalLength *= 4;
			}
			else if (planet.radius < 40000)
			{
				map.focalLength *= 3;
				map.focalLength /= 2;
			}
		}
	}
}
