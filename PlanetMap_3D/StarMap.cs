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
		const string MOTION_KEY = "Motion Vector";
		const string INFO_KEY = "Show Info";
		const string GPS_KEY = "GPS Mode";
		const string SHIP_KEY = "Show Ship";
		const string PLANET_KEY = "Selected Planet";
		const string WAYPOINT_KEY = "Selected Waypoint";
		const string BRIGHTNESS_KEY = "Brightness";


		

		const string ORIGIN = "(0,0,0)";
		const string DV_MOTION = "0,0,0,0"; // Default string for Motion Vector: dX,dY,dZ,dAz
		static List<StarMap> _mapList;

		public class StarMap
		{
			public IMyTerminalBlock Block;
			public string Header;
			public IMyTextSurface DrawingSurface;
			public RectangleF Viewport;

			public Vector3 Center;
			public string Mode;
			public int Altitude;
			public int Azimuth;
			public int RotationalRadius;
			public int FocalLength;
			public int Number;
			public int Index;
			public int dX;
			public int dY;
			public int dZ;
			public int dAz;
			public int GpsState;
			public float BrightnessMod;

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
				Header = header;
				PlanetIndex = 0;
				WaypointIndex = -1;

				Index = screenIndex;

				Mode = GetMapKey(MODE_KEY, "FREE");

				Center = StringToVector3(GetMapKey(CENTER_KEY, ORIGIN));
				Azimuth = ParseInt(GetMapKey(AZ_KEY, "0"), 0);	
				Altitude = ParseInt(GetMapKey(ALT_KEY, DV_ALTITUDE.ToString()), DV_ALTITUDE);

				FocalLength = ParseInt(GetMapKey(ZOOM_KEY, DV_FOCAL.ToString()), DV_FOCAL);
				RotationalRadius = ParseInt(GetMapKey(RADIUS_KEY, DV_RADIUS.ToString()), DV_RADIUS);

				// Get movement velocities
				string[] movements = GetMapKey(MOTION_KEY, DV_MOTION).Split(',');
				if (movements.Length < 4)
					movements = new string [] { "0", "0", "0", "0" };

				dX = ParseInt(movements[0], 0);
				dY = ParseInt(movements[1], 0);
				dZ = ParseInt(movements[2], 0);
				dAz = ParseInt(movements[3], 0);

				ShowInfo = ParseBool(GetMapKey(INFO_KEY, "True"));
				GpsMode = GetMapKey(GPS_KEY, "NORMAL").ToUpper();
				GpsModeToState();
				ShowShip = ParseBool(GetMapKey(SHIP_KEY, "True"));

				//lcdIni.Set("mapDisplay", "Names", newNames);			GetKey(block, header,
				ShowNames = true;

				ActivePlanetName = GetMapKey(PLANET_KEY, "");
				ActivePlanet = GetPlanet(ActivePlanetName);

				ActiveWaypointName = GetMapKey(WAYPOINT_KEY, "");
				ActiveWaypoint = GetWaypoint(ActiveWaypointName);

				BrightnessMod = ParseFloat(GetMapKey(BRIGHTNESS_KEY, "1"), 1);
			}

			// Yaw //
			void Yaw(int angle)
			{
				if (Mode.ToUpper() == "PLANET" || Mode.ToUpper() == "CHASE" || Mode.ToUpper() == "ORBIT")
				{
					AddMessage("Yaw controls locked in PLANET, CHASE & ORBIT modes.");
					return;
				}
				Azimuth = DegreeAdd(Azimuth, angle);

				SetMapKey( AZ_KEY, Azimuth.ToString());
			}

			// Pitch //
			void Pitch(int angle)
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
					AddMessage("Pitch controls locked in PLANET & ORBIT modes.");
				}

				SetMapKey( ALT_KEY, Altitude.ToString());
			}

			// Rotate //
			public void Rotate(string direction)
			{
				switch (direction)
				{
					case "LEFT":
						Yaw(ANGLE_STEP);
						break;
					case "RIGHT":
						Yaw(-ANGLE_STEP);
						break;
					case "UP":
						Pitch(-ANGLE_STEP);
						break;
					case "DOWN":
						Pitch(ANGLE_STEP);
						break;
				}
			}

			// MOVE  //
			public void Move(string direction)
			{
				float step = (float)MOVE_STEP;
				float x = 0;
				float y = 0;
				float z = 0;

				switch (direction)
				{
					case "LEFT":
						x = step;
						break;
					case "RIGHT":
						x = -step;
						break;
					case "UP":
						y = step;
						break;
					case "DOWN":
						y = -step;
						break;
					case "FORWARD":
					case "FORWARDS":
						z = step;
						break;
					case "BACKWARD":
					case "BACKWARDS":
						z = -step;
						break;
				}
				Vector3 moveVector = new Vector3(x, y, z);

				if (Mode == "FREE" || Mode == "WORLD")
				{
					Center += rotateMovement(moveVector, this);
				}
				else
				{
					AddMessage("Translation controls only available in FREE & WORLD modes.");
				}

				SetMapKey( CENTER_KEY, Vector3ToString(Center));
			}


			// TRACK //		Adjust translational speed of map.
			public void Track(string direction)
			{
				switch (direction)
				{
					case "LEFT":
						dX += MOVE_STEP;
						break;
					case "RIGHT":
						dX -= MOVE_STEP;
						break;
					case "UP":
						dY += MOVE_STEP;
						break;
					case "DOWN":
						dY -= MOVE_STEP;
						break;
					case "FORWARD":
					case "FORWARDS":
						dZ += MOVE_STEP;
						break;
					case "BACKWARD":
					case "BACKWARDS":
						dZ -= MOVE_STEP;
						break;
					default:
						AddMessage("Error with Track Command");
						break;
				}

				UpdateMotionParameters();
			}

			// SPIN //
			public void Spin(string direction)
			{
				int deltaAz = ANGLE_STEP / 2;

				if (direction == "RIGHT")
					deltaAz *= -1;

				dAz += deltaAz;

				UpdateMotionParameters();
			}

			// Zoom // - Changes Focal Length of Maps. true => Zoom In / false => Zoom Out
			public void Zoom(bool zoomIn)
			{
				int doF = FocalLength;
				float newScale;

				if (zoomIn)
				{
					newScale = doF * ZOOM_STEP;
				}
				else
				{
					newScale = doF / ZOOM_STEP;
				}


				if (newScale > ZOOM_MAX)
				{
					doF = ZOOM_MAX;
				}
				else if (newScale < 1)
				{
					doF = 1;
				}
				else
				{
					doF = (int)newScale;
				}

				FocalLength = doF;

				SetMapKey(ZOOM_KEY, FocalLength.ToString());
			}

			// ADJUST RADIUS //
			public void AdjustRadius(bool increase)
			{
				int radius = RotationalRadius;

				if (increase)
				{
					radius *= 2;
				}
				else
				{
					radius /= 2;
				}

				if (radius < FocalLength)
				{
					radius = FocalLength;
				}
				else if (radius > MAX_VALUE)
				{
					radius = MAX_VALUE;
				}

				RotationalRadius = radius;
				SetMapKey(RADIUS_KEY, RotationalRadius.ToString());
			}

			// Stop //
			public void Stop()
            {
				dX = 0;
				dY = 0;
				dZ = 0;
				dAz = 0;

				UpdateMotionParameters();
			}

			// GPS State To Mode //
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

			// GPS Mode To State //
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

			// DEFAULT VIEW //
			public void DefaultView()
			{
				Mode = "FREE";

				Center = new Vector3(0, 0, 0);
				FocalLength = DV_FOCAL;

				if (Viewport.Width > 500)
				{
					FocalLength *= 3;
				}

				RotationalRadius = DV_RADIUS;
				Azimuth = 0;
				Altitude = DV_ALTITUDE;

				UpdateBasicParameters();
			}

			// Brighten //
			public void Brighten(bool brighten)
			{
				if (brighten)
				{
					BrightnessMod += BRIGHTNESS_STEP;
					if (BrightnessMod > BRIGHTNESS_LIMIT)
						BrightnessMod = BRIGHTNESS_LIMIT;
				}
				else
				{
					BrightnessMod -= BRIGHTNESS_STEP;

					// Don't let brightness fall below level of single step.
					if (BrightnessMod < BRIGHTNESS_STEP)
						BrightnessMod = BRIGHTNESS_STEP;
				}

				SetMapKey(BRIGHTNESS_KEY, BrightnessMod.ToString());
			}

			// Update Motion Parameters //
			void UpdateMotionParameters()
            {
				// Interface directly with Ini to reduce complexity
				MyIni ini = GetIni(Block);

				// Write Motion Parameters to 4D String Vector
				string motionVector = dX.ToString() + ',' + dY.ToString() + ',' + dZ.ToString() + ',' + dAz.ToString();
				ini.Set(Header, MOTION_KEY, motionVector);

				// Update Center and Azimuth for when map is stopped.
				ini.Set(Header, CENTER_KEY, Vector3ToString(Center));
				ini.Set(Header, AZ_KEY, Azimuth.ToString());

				Block.CustomData = ini.ToString();
            }

			// Update Basic Parameters //
			public void UpdateBasicParameters()
            {
				MyIni ini = GetIni(Block);

				ini.Set(Header, MODE_KEY, Mode);
				ini.Set(Header, CENTER_KEY, Vector3ToString(Center));
				ini.Set(Header, AZ_KEY, Azimuth.ToString());
				ini.Set(Header, ALT_KEY, Altitude.ToString());
				ini.Set(Header, ZOOM_KEY, FocalLength.ToString());
				ini.Set(Header, RADIUS_KEY, RotationalRadius.ToString());
				ini.Set(Header, INFO_KEY, ShowInfo.ToString());
				ini.Set(Header, GPS_KEY, GpsMode);
				ini.Set(Header, SHIP_KEY, ShowShip.ToString());
				ini.Set(Header, PLANET_KEY, ActivePlanetName);
				ini.Set(Header, WAYPOINT_KEY, ActiveWaypointName);
				ini.Set(Header, BRIGHTNESS_KEY, BrightnessMod.ToString());

				Block.CustomData = ini.ToString();
			}

			public void SetMapKey(string key, string value)
            {
				SetKey(Block, Header, key, value);
            }

			public string GetMapKey(string key, string value)
            {
				return GetKey(Block, Header, key, value);
            }
		}

		// NON CLASS FUNCTIONS // -----------------------------------------------------------------------------------------------------------------------------------------------------

		// PARAMETERS TO MAPS // Build all maps associated with specified block
		public List<StarMap> ParametersToMaps(IMyTerminalBlock mapBlock)
		{
			List<StarMap> mapsOut = new List<StarMap>();
			List<string> headers = new List<string>();
			List<int> indexes = new List<int>();

			int surfaceCount = (mapBlock as IMyTextSurfaceProvider).SurfaceCount;
			
			if (surfaceCount < 1)
            {
				AddMessage("Block \"" + mapBlock.CustomName + "\" has no screens!");
				return mapsOut;
            }
			else if (surfaceCount == 1)
            {
				// Add a basic header and screen index 0 for single screen block
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
						// Add numbered header as well as index of screen
						headers.Add(MAP_HEADER + " " + i);
						indexes.Add(i);
					}

					defaultBool = "False";
                }
            }

			for (int j = 0; j < headers.Count; j++)
            {
				mapsOut.Add(new StarMap(mapBlock, indexes[j], headers[j]));
            }

			return mapsOut;
		}


		// ARG TO MAPS // Get map or maps from string argument
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
						AddMessage("screenID " + argValue + " outside range of Map List!");
					}
				}
			}

			return mapsToEdit;
		}


		// NO MAPS //
		public bool NoMaps(List<StarMap> maps)
		{
			if (maps.Count < 1)
			{
				AddMessage("No relevant maps found! Check arguments!");
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
