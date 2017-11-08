using Sparrow.Animation;
using Sparrow.Core;
using Sparrow.Display;

namespace SparrowGame.Shared
{
    public class SampleMovingStuff : Quad, IAnimatable
    {
        public SampleMovingStuff() : base(70, 30, 0xa112a1)
        {
            Y = 120;
            SparrowSharp.DefaultJuggler.Add(this);
        }

        public void AdvanceTime(float seconds)
        {
            X++;
            if (X > 512) X = 0;
        }

        public event Juggler.RemoveFromJugglerHandler RemoveFromJugglerEvent;
    }
}