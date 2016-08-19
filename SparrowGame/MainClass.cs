using System;
using Sparrow.Core;
using Sparrow.Samples;
using OpenTK;
using OpenTK.Graphics;

namespace SparrowGame
{
	class MainClass
	{
		[STAThread]
		public static void Main()
		{
			new DesktopViewController (typeof(GameMain), 960, 640, "Game", GameWindowFlags.Default, DisplayDevice.Default, GraphicsContextFlags.Debug);
		}
	}
}