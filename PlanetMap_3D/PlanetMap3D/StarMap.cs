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
		const string MAP_HEADER = "MAP DISPLAY";

		// Key Constants
		const string MODE_KEY = "Mode";
		const string CENTER_KEY = "Center";
		const string AZ_KEY = "Azimuth";
		const string ALT_KEY = "Altitude";
		const string ZOOM_KEY = "Focal Length";
		const string RADIUS_KEY = "Rotational Radius";
		const string MOVEMENT_KEY = "Movement Vector";
		const string GPS_KEY = "GPS Mode";
		const string SHIP_KEY = "Show Ship";
		const string PLANET_KEY = "Selected Planet";
		const string WAYPOINT_KEY = "Selected Waypoint";
		const string BRIGHTNESS_KEY = "Brightness";


		const string INFO_KEY = "Show Info";

		const string ORIGIN = "(0,0,0)";
		const string VECTOR_4 = "0,0,0,0";
		static List<StarMap> _mapList;

		public class StarMap
		{
			public Vector3 Center;
			public string Mode;
			public int Altitude;
			public int Azimuth;
			public int RotationalRadius;
			public int FocalLength;
			//public int azSpeed; // Rotational velocity of Azimuth
			public int Number;
			public int Index;
			public int dX;
			public int dY;
			public int dZ;
			public int dAz;
			public int GpsState;
			public float BrightnessMod;
			public IMyTextSurface DrawingSurface;
			public RectangleF Viewport;
			public IMyTerminalBlock Block;
			public bool ShowNames;
			public bool ShowShip;
			public bool ShowInfo;
			public int PlanetIndex;
			public int WaypointIndex;
			public string ActivePlanetName;
			public string ActiveWaypointName;
			public string GpsMode;
			public Planet ActivePlanet;
			public Waypoint ActiveWaypoint;

			// Constructor
			public StarMap(IMyTerminalBlock block, int screenIndex, string header)
			{
				Block = block;
				//azSpeed = 0;
				PlanetIndex = 0;
				WaypointIndex = -1;

				Index = screenIndex;

				Mode = GetKey(block, header, MODE_KEY, "FREE");

				Center = StringToVector3(GetKey(block, header, CENTER_KEY, ORIGIN));
				Azimuth = ParseInt(GetKey(block, header, AZ_KEY, "0"), 0);	
				Altitude = ParseInt(GetKey(block, header, ALT_KEY, DV_ALTITUDE.ToString()), DV_ALTITUDE);

				FocalLength = ParseInt(GetKey(block, header, ZOOM_KEY, DV_FOCAL.ToString()), DV_FOCAL);
				RotationalRadius = ParseInt(GetKey(block, header, RADIUS_KEY, DV_RADIUS.ToString()), DV_RADIUS);

				// Get movement velocities
				string[] movements = GetKey(block, header, MOVEMENT_KEY, VECTOR_4).Split(',');
				if (movements.Length < 4)
					movements = new string [] { "0", "0", "0", "0" };

				dX = ParseInt(movements[0], 0);
				dY = ParseInt(movements[1], 0);
				dZ = ParseInt(movements[2], 0);
				dAz = ParseInt(movements[3], 0);

				ShowInfo = ParseBool(GetKey(block, header, INFO_KEY, "True"));
				GpsMode = GetKey(block, header, GPS_KEY, "NORMAL").ToUpper();
				GpsModeToState();
				ShowShip = ParseBool(GetKey(block, header, SHIP_KEY, "True"));

				//lcdIni.Set("mapDisplay", "Names", newNames);			GetKey(block, header,
				ShowNames = true;

				ActivePlanetName = GetKey(block, header, PLANET_KEY, "");
				ActivePlanet = GetPlanet(ActivePlanetName);

				ActiveWaypointName = GetKey(block, header, WAYPOINT_KEY, "");
				ActiveWaypoint = GetWaypoint(ActiveWaypointName);

				BrightnessMod = ParseFloat(GetKey(block, header, BRIGHTNESS_KEY, "1"), 1);
			}

			public void yaw(int angle)
			{
				if (Mode.ToUpper() == "PLANET" || Mode.ToUpper() == "CHASE" || Mode.ToUpper() == "ORBIT")
				{
					_statusMessage = "Yaw controls locked in PLANET, CHASE & ORBIT modes.";
					return;
				}
				Azimuth = DegreeAdd(Azimuth, angle);

				SetKey(Block, MAP_HEADER, AZ_KEY, Azimuth.ToString());
			}

			public void pitch(int angle)
			{
				if (Mode.ToUpper() != "PLANET" || Mode.ToUpper() == "ORBIT")
				{
					int newAngle = DegreeAdd(Altitude, angle);

					if (newAngle > MAX_PITCH)
					{
						newAngle = MAX_PITCH;
					}
					else if (newAngle < -MAX_PITCH)
					{
						newAngle = -MAX_PITCH;
					}

					Altitude = newAngle;
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

				SetKey(Block, MAP_HEADER, MOVEMENT_KEY, VECTOR_4);
			}

			public string GpsStateToMode()
			{
				switch (GpsState)
				{
					case 0:
						GpsMode = "OFF";
						break;
					case 1:
						GpsMode = "NORMAL";
						break;
					case 2:
						GpsMode = "SHOW_ACTIVE";
						break;
					default:
						GpsMode = "ERROR";
						break;
				}

				return GpsMode;
			}


			public void GpsModeToState()
            {
				switch (GpsMode)
				{
					case "OFF":
					case "FALSE":
						GpsState = 0;
						break;
					case "SHOW_ACTIVE":
						GpsState = 2;
						break;
					default:
						GpsState = 1;
						break;
				}
			}
		}


		// PARAMETERS TO MAPS //
		public List<StarMap> ParametersToMaps(IMyTerminalBlock mapBlock)
		{
			List<StarMap> mapsOut = new List<StarMap>();
			List<string> headers = new List<string>();
			List<int> indexes = new List<int>();
			int surfaceCount = (mapBlock as IMyTextSurfaceProvider).SurfaceCount;

			if (surfaceCount < 1)
            {
				_statusMessage += "Block \"" + mapBlock.CustomName + "\" has no screens!\n";
				return mapsOut;
            }
			else if (surfaceCount == 1)
            {
				headers.Add(MAP_HEADER);
				indexes.Add(0);
            }
			else
            {
				string defaultBool = "True";

				for(int i = 0; i < surfaceCount; i++)
                {
					if(ParseBool(GetKey(mapBlock, "MAP DISPLAYS", "Show On Screen " + i, defaultBool)))
                    {
						headers.Add(MAP_HEADER + " " + i);
						indexes.Add(i);
					}

					defaultBool = "False";
                }
            }

			for(int j = 0; j < headers.Count; j++)
            {
				mapsOut.Add(new StarMap(mapBlock, indexes[j], headers[j]));
            }

            #region
            /*
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

				map.index = ParseInt(indexStrings[i], -1);

				map.block = mapBlock;
				map.center = StringToVector3(centers[i]);
				map.focalLength = ParseInt(fLengths[i], DV_FOCAL);
				map.rotationalRadius = ParseInt(radii[i], DV_RADIUS);
				map.azimuth = ParseInt(azimuths[i], 0);
				map.altitude = ParseInt(altitudes[i], DV_ALTITUDE);
				map.mode = modes[i];
				map.BrightnessMod = ParseFloat(brightnessMods[i], 1);

				map.gpsMode = gpsModes[i];


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
			*/
            #endregion

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

		/*
		// MAP TO PARAMETERS // Writes map object to CustomData of Display Block
		public void MapToParameters(StarMap map)
		{

			MyIni lcdIni = DataToIni(map.block);

			int i = 0;

			string blockIndex = lcdIni.Get("mapDisplay", "Indexes").ToString();
			string[] indexes = blockIndex.Split(',');

			int entries = indexes.Length;

			if (entries > 0)
			{
				for (int j = 0; j < entries; j++)
				{
					if (map.index.ToString() == indexes[j])
					{
						i = j;  //This is the array position of the screen index for this map.
					}
				}
			}

			// Read the old Ini Data and split into string arrays. Insert the new data into the arrays.
			string newIndexes = InsertEntry(map.index.ToString(), blockIndex, i, entries, "0");
			string newCenters = InsertEntry(Vector3ToString(map.center), lcdIni.Get("mapDisplay", "Center").ToString(), i, entries, "(0,0,0)");
			string newModes = InsertEntry(map.mode, lcdIni.Get("mapDisplay", "Mode").ToString(), i, entries, "FREE");
			string newFocal = InsertEntry(map.focalLength.ToString(), lcdIni.Get("mapDisplay", "FocalLength").ToString(), i, entries, DV_FOCAL.ToString());
			string newRadius = InsertEntry(map.rotationalRadius.ToString(), lcdIni.Get("mapDisplay", "RotationalRadius").ToString(), i, entries, DV_RADIUS.ToString());
			string newAzimuth = InsertEntry(map.azimuth.ToString(), lcdIni.Get("mapDisplay", "Azimuth").ToString(), i, entries, "0");
			string newAltitude = InsertEntry(map.altitude.ToString(), lcdIni.Get("mapDisplay", "Altitude").ToString(), i, entries, DV_ALTITUDE.ToString());
			string newDX = InsertEntry(map.dX.ToString(), lcdIni.Get("mapDisplay", "dX").ToString(), i, entries, "0");
			string newDY = InsertEntry(map.dY.ToString(), lcdIni.Get("mapDisplay", "dY").ToString(), i, entries, "0");
			string newDZ = InsertEntry(map.dZ.ToString(), lcdIni.Get("mapDisplay", "dZ").ToString(), i, entries, "0");
			string newDAz = InsertEntry(map.dAz.ToString(), lcdIni.Get("mapDisplay", "dAz").ToString(), i, entries, "0");
			string newGPS = InsertEntry(map.gpsStateToMode(), lcdIni.Get("mapDisplay", "GPS").ToString(), i, entries, "True");
			string newNames = InsertEntry(map.showNames.ToString(), lcdIni.Get("mapDisplay", "Names").ToString(), i, entries, "True");
			string newShip = InsertEntry(map.showShip.ToString(), lcdIni.Get("mapDisplay", "Ship").ToString(), i, entries, "True");
			string newInfo = InsertEntry(map.showInfo.ToString(), lcdIni.Get("mapDisplay", "Info").ToString(), i, entries, "True");
			string newPlanets = InsertEntry(map.activePlanetName, lcdIni.Get("mapDisplay", "Planet").ToString(), i, entries, "[null]");
			string newWaypoints = InsertEntry(map.activeWaypointName, lcdIni.Get("mapDisplay", "Waypoint").ToString(), i, entries, "[null]");
			string brightnesses = InsertEntry(map.BrightnessMod.ToString(), lcdIni.Get("mapDisplay", "Brightness").ToString(), i, entries, "1");
			
			// Update the Ini Data.
			//lcdIni.Set("mapDisplay", "Center", newCenters);
			//lcdIni.Set("mapDisplay", "Mode", newModes);
			//lcdIni.Set("mapDisplay", "FocalLength", newFocal);
			//lcdIni.Set("mapDisplay", "RotationalRadius", newRadius);
			//lcdIni.Set("mapDisplay", "Azimuth", newAzimuth);
			//lcdIni.Set("mapDisplay", "Altitude", newAltitude);
			//lcdIni.Set("mapDisplay", "Indexes", newIndexes);
			//lcdIni.Set("mapDisplay", "dX", newDX);
			//lcdIni.Set("mapDisplay", "dY", newDY);
			//lcdIni.Set("mapDisplay", "dZ", newDZ);
			//lcdIni.Set("mapDisplay", "dAz", newDAz);
			//lcdIni.Set("mapDisplay", "GPS", newGPS);
			//lcdIni.Set("mapDisplay", "Names", newNames);
			//lcdIni.Set("mapDisplay", "Ship", newShip);
			//lcdIni.Set("mapDisplay", "Info", newInfo);
			//lcdIni.Set("mapDisplay", "Planet", newPlanets);
			//lcdIni.Set("mapDisplay", "Waypoint", newWaypoints);

			map.block.CustomData = lcdIni.ToString();
			

			IMyTerminalBlock block = map.block;
			int index = map.index;

			SetListKey(block, MAP_HEADER, "Center", map.center.ToString(), ORIGIN, index);
			SetListKey(block, MAP_HEADER, "Mode", map.mode, "FREE", index);
		}
		*/
	}
}
