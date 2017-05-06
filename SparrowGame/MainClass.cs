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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DesktopViewController dvc = new DesktopViewController (typeof(GameMain), 960, 640);
            Application.Run(dvc);
        }
	}
}