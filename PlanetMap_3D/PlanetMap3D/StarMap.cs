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
		const string MAP_HEADER = "mapDisplay";
		static List<StarMap> _mapList;

		public class StarMap
		{
			public Vector3 center;
			public string mode;
			public int altitude;
			public int azimuth;
			public int rotationalRadius;
			public int focalLength;
			public int azSpeed; // Rotational velocity of Azimuth
			public int number;
			public int index;
			public int dX;
			public int dY;
			public int dZ;
			public int dAz;
			public int gpsState;
			public float BrightnessMod;
			public IMyTextSurface drawingSurface;
			public RectangleF viewport;
			public IMyTerminalBlock block;
			public bool showNames;
			public bool showShip;
			public bool showInfo;
			public int planetIndex;
			public int waypointIndex;
			public string activePlanetName;
			public string activeWaypointName;
			public string gpsMode;
			public Planet activePlanet;
			public Waypoint activeWaypoint;

			public StarMap()
			{
				this.azSpeed = 0;
				this.planetIndex = 0;
				this.waypointIndex = -1;
			}

			public void yaw(int angle)
			{
				if (this.mode.ToUpper() == "PLANET" || this.mode.ToUpper() == "CHASE" || this.mode.ToUpper() == "ORBIT")
				{
					_statusMessage = "Yaw controls locked in PLANET, CHASE & ORBIT modes.";
					return;
				}
				this.azimuth = DegreeAdd(this.azimuth, angle);
			}

			public void pitch(int angle)
			{
				if (this.mode.ToUpper() != "PLANET" || this.mode.ToUpper() == "ORBIT")
				{
					int newAngle = DegreeAdd(this.altitude, angle);

					if (newAngle > MAX_PITCH)
					{
						newAngle = MAX_PITCH;
					}
					else if (newAngle < -MAX_PITCH)
					{
						newAngle = -MAX_PITCH;
					}

					this.altitude = newAngle;
				}
				else
				{
					_statusMessage = "Pitch controls locked in PLANET & ORBIT modes.";
				}
			}

			public void Stop()
            {
				dX = 0;
				dY = 0;
				dZ = 0;
				dAz = 0;
			}

			public string gpsStateToMode()
			{
				switch (this.gpsState)
				{
					case 0:
						this.gpsMode = "OFF";
						break;
					case 1:
						this.gpsMode = "NORMAL";
						break;
					case 2:
						this.gpsMode = "SHOW_ACTIVE";
						break;
					default:
						this.gpsMode = "ERROR";
						break;
				}

				return this.gpsMode;
			}
		}


		// PARAMETERS TO MAPS //
		public List<StarMap> ParametersToMaps(IMyTerminalBlock mapBlock)
		{
			List<StarMap> mapsOut = new List<StarMap>();
			/*
			MyIni lcdIni = DataToIni(mapBlock);

			if (!mapBlock.CustomData.Contains("[mapDisplay]"))
			{
				string oldData = mapBlock.CustomData.Trim();
				string newData = _defaultDisplay;

				if (oldData.StartsWith("["))
				{
					newData += "\n\n" + oldData;
				}
				else if (oldData != "")
				{
					newData += "\n---\n" + oldData;
				}

				mapBlock.CustomData = newData;
			}

			string indexString;

			if (!mapBlock.CustomData.Contains("Indexes"))
			{
				indexString = "";
			}
			else
			{
				indexString = lcdIni.Get("mapDisplay", "Indexes").ToString();
			}
			*/

			string indexString = GetKey(mapBlock, MAP_HEADER, "Indexes", "0");
			string[] indexStrings = indexString.Split(SEPARATOR);
			int iLength = indexStrings.Length;

			//Split Ini parameters into string lists. Insure all lists are the same length.

			List<string> centers = StringToEntries(GetKey(mapBlock, MAP_HEADER, "Center", "(0,0,0)"), iLength, "(0,0,0)");
			//List<string> centers = StringToEntries(lcdIni.Get("mapDisplay", "Center").ToString(), ';', iLength, "(0,0,0)");

			List<string> fLengths = StringToEntries(GetKey(mapBlock, MAP_HEADER, "FocalLength", DV_FOCAL.ToString()), iLength, DV_FOCAL.ToString());
			//List<string> fLengths = StringToEntries(lcdIni.Get("mapDisplay", "FocalLength").ToString(), ',', iLength, DV_FOCAL.ToString());

			List<string> radii = StringToEntries(GetKey(mapBlock, MAP_HEADER, "RotationalRadius", DV_RADIUS.ToString()), iLength, DV_RADIUS.ToString());
			//List<string> radii = StringToEntries(lcdIni.Get("mapDisplay", "RotationalRadius").ToString(), ',', iLength, DV_RADIUS.ToString());
			
			List<string> azimuths = StringToEntries(GetKey(mapBlock, MAP_HEADER, "Azimuth", DV_ALTITUDE.ToString()), iLength, "0");
			//List<string> azimuths = StringToEntries(lcdIni.Get("mapDisplay", "Azimuth").ToString(), ',', iLength, "0");

			List<string> altitudes = StringToEntries(GetKey(mapBlock, MAP_HEADER, "Altitude", DV_ALTITUDE.ToString()), iLength, DV_ALTITUDE.ToString());
			//List<string> altitudes = StringToEntries(lcdIni.Get("mapDisplay", "Altitude").ToString(), ',', iLength, DV_ALTITUDE.ToString());

			List<string> modes = StringToEntries(GetKey(mapBlock, MAP_HEADER, "Mode", "FREE"), iLength, "FREE");
			//List<string> modes = StringToEntries(lcdIni.Get("mapDisplay", "Mode").ToString(), ',', iLength, "FREE");

			List<string> gpsModes = StringToEntries(GetKey(mapBlock, MAP_HEADER, "GPS", "true"), iLength, "true");
			//List<string> gpsModes = StringToEntries(lcdIni.Get("mapDisplay", "GPS").ToString(), ',', iLength, "true");

			List<string> nameBools = StringToEntries(GetKey(mapBlock, MAP_HEADER, "Names", "true"), iLength, "true");
			//List<string> nameBools = StringToEntries(lcdIni.Get("mapDisplay", "Names").ToString(), ',', iLength, "true");

			List<string> shipBools = StringToEntries(GetKey(mapBlock, MAP_HEADER, "Ship", "true"), iLength, "true");
			//List<string> shipBools = StringToEntries(lcdIni.Get("mapDisplay", "Ship").ToString(), ',', iLength, "true");

			List<string> infoBools = StringToEntries(GetKey(mapBlock, MAP_HEADER, "Info", "true"), iLength, "true");
			//List<string> infoBools = StringToEntries(lcdIni.Get("mapDisplay", "Info").ToString(), ',', iLength, "true");

			List<string> planets = StringToEntries(GetKey(mapBlock, MAP_HEADER, "Planet", "[null]").ToString(), iLength, "[null]");
			//List<string> planets = StringToEntries(lcdIni.Get("mapDisplay", "Planet").ToString(), ',', iLength, "[null]");

			List<string> waypoints = StringToEntries(GetKey(mapBlock, MAP_HEADER, "Waypoint", "[null]").ToString(), iLength, "[null]");
			//List<string> waypoints = StringToEntries(lcdIni.Get("mapDisplay", "Waypoint").ToString(), ',', iLength, "[null]");

			List<string> brightnessMods = StringToEntries(GetKey(mapBlock, MAP_HEADER, "Brightness", "1").ToString(), iLength, "1");

			//assemble maps by position in string lists.
			for (int i = 0; i < iLength; i++)
			{
				StarMap map = new StarMap();
				if (indexString == "")
				{
					map.index =  _mapIndex;
				}
				else
				{
					map.index = ParseInt(indexStrings[i], -1);
				}

				map.block = mapBlock;
				map.center = StringToVector3(centers[i]);
				map.focalLength = ParseInt(fLengths[i], DV_FOCAL);
				map.rotationalRadius = ParseInt(radii[i], DV_RADIUS);
				map.azimuth = ParseInt(azimuths[i], 0);
				map.altitude = ParseInt(altitudes[i], DV_ALTITUDE);
				map.mode = modes[i];
				map.BrightnessMod = ParseFloat(brightnessMods[i], 1);

				map.gpsMode = gpsModes[i];
				switch (map.gpsMode.ToUpper())
				{
					case "OFF":
					case "FALSE":
						map.gpsState = 0;
						break;
					case "SHOW_ACTIVE":
						map.gpsState = 2;
						break;
					default:
						map.gpsState = 1;
						break;
				}

				map.showNames = ParseBool(nameBools[i]);
				map.showShip = ParseBool(shipBools[i]);
				map.showInfo = ParseBool(infoBools[i]);
				map.activePlanetName = planets[i];
				map.activePlanet = GetPlanet(map.activePlanetName);
				map.activeWaypointName = waypoints[i];
				map.activeWaypoint = GetWaypoint(map.activeWaypointName);

				if (map.index > -1)
					mapsOut.Add(map);
				else
					_statusMessage += "Invalid map index in block \"" + mapBlock.CustomName + "\"\nIndex:" + indexString; 
			}

			return mapsOut;
		}


		// ARG TO MAPS //
		public List<StarMap> ArgToMaps(string arg)
		{
			if (arg.ToUpper() == "ALL")
			{
				return _mapList;
			}

			List<StarMap> mapsToEdit = new List<StarMap>();

			string[] args = arg.Split(',');

			foreach (string argValue in args)
			{
				int number;

				bool success = Int32.TryParse(argValue, out number);
				if (success)
				{
					if (number < _mapList.Count)
					{
						mapsToEdit.Add(_mapList[number]);
					}
					else
					{
						_statusMessage = "screenID " + argValue + " outside range of Map List!";
					}
				}
			}

			return mapsToEdit;
		}


		public bool NoMaps(List<StarMap> maps)
		{
			if (maps.Count < 1)
			{
				_statusMessage = "No relevant maps found! Check arguments!";
				return true;
			}

			return false;
		}


		// GET MAP //
		StarMap GetMap(int mapNumber)
        {
			if (mapNumber > -1 && mapNumber < _mapList.Count)
				return _mapList[mapNumber];
			else
				return null;
        }
	}
}
