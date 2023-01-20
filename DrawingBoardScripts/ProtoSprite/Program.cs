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
    partial class Program : MyGridProgram
    {
        const string LCD_TAG = "LCD";
        const float PI = (float) Math.PI;
        IMyTextSurface _surface;
        RectangleF _viewport;
        const float FONTSIZE = 1.5f;

        Color _bgColor = Color.Black;
        Color _buttonColor = new Color(0,48,48);


        public Program()
        {
            _surface = GetFirstSurface();

            if (_surface != null)
            {
                PrepareTextSurfaceForSprites(_surface);
                _viewport = new RectangleF((_surface.TextureSize - _surface.SurfaceSize) / 2f, _surface.SurfaceSize);

                DrawPrototype();
            }
                

        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (_surface != null)
            {
                Echo("Drawing ...");
                DrawPrototype();
            }
            else
            {
                Echo("NO DRAW SURFACE FOUND!");
            }
                
        }


        IMyTextSurface GetFirstSurface()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(LCD_TAG, blocks);

            if(blocks.Count > 0)
            {
                foreach(IMyTerminalBlock block in blocks)
                {
                    try
                    {
                        return (block as IMyTextSurfaceProvider).GetSurface(0);
                    } catch{/* SHRUG */}
                }
            }

            return null;
        }


        void DrawPrototype()
        {
            _frame = _surface.DrawFrame();

            float width = _viewport.Width;
            float height = _viewport.Height;
            Vector2 center = _viewport.Center;

            float scale;

            if (height > width)
                scale = width;
            else
                scale = height;

            DrawTexture(SQUARE, center - new Vector2(width * 0.5f, 0), new Vector2(scale, scale), 0, _buttonColor);

            //DrawThruster("+", center, scale, FONTSIZE, _bgColor, _buttonColor);
            DrawMissile(center, scale, _bgColor, _buttonColor);


            DrawTexture(RING, center - new Vector2(50, 0), new Vector2(100, 100), 0, Color.Red);



            Echo("... Thruster");
            _frame.Dispose();
        }
    }
}
