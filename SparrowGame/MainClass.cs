using System;
using Sparrow.Core;
using SparrowGame.Shared;
using System.Windows.Forms;

namespace SparrowGame
{
	class MainClass
	{
		[STAThread]
		public static void Main()
		{
			if (Environment.OSVersion.Version.Major >= 6) SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DesktopViewController dvc = new DesktopViewController (typeof(GameMain), 960, 640);
            Application.Run(dvc);
        }

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		private static extern bool SetProcessDPIAware();
	}
}