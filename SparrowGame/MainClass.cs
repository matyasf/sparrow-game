using System;
using Sparrow.Core;
using Sparrow.Samples;

namespace SparrowGame
{
	class MainClass
	{
		[STAThread]
		public static void Main()
		{
			new DesktopViewController (typeof(Benchmark));
		}
	}
}