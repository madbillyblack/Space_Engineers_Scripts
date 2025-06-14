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
        const string SQUARE = "SquareSimple";
        const string BOX = "SquareHollow";
        const string SEMI = "SemiCircle";
        const string CIRCLE = "Circle";
        const string TRIANGLE = "Triangle";
        const string RING = "CircleHollow";

        static MySpriteDrawFrame _frame;



        // DRAW TEXTURE //
        static void DrawTexture(string shape, Vector2 position, Vector2 size, float rotation, Color color)
        {
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = shape,
                Position = position,
                Size = size,
                RotationOrScale = rotation,
                Color = color
            };
            _frame.Add(sprite);
        }

        // DRAW TEXT //
        static void DrawText(string text, Vector2 position, float scale, TextAlignment alignment, Color color, string font = "White")
        {
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = position,
                RotationOrScale = scale,
                Color = color,
                Alignment = alignment,
                FontId = font
            };
            _frame.Add(sprite);
        }

        // PREPARE TEXT SURFACE FOR SPRITES //
        public static void PrepareTextSurfaceForSprites(IMyTextSurface textSurface)
        {
            // Set the sprite display mode
            textSurface.ContentType = ContentType.SCRIPT;
            textSurface.ScriptBackgroundColor = new Color(0, 0, 0);
            // Make sure no built-in script has been selected
            textSurface.Script = "";
        }
    }
}
