using System;
using Sparrow.Core;
using SparrowGame.Shared;

namespace SparrowGame
{
	class MainClass
	{
		[STAThread]
		public static void Main()
		{
			
			//if (Environment.OSVersion.Version.Major >= 6) SetProcessDPIAware();
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);

            System.Windows.Application app = new System.Windows.Application();
            app.Run(new DesktopViewController(typeof(GameMain), 960, 640));
        }

		//[System.Runtime.InteropServices.DllImport("user32.dll")]
		//private static extern bool SetProcessDPIAware();
	}
}