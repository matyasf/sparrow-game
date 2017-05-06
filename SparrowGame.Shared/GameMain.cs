
using Sparrow.Display;

namespace SparrowGame.Shared
{
    public class GameMain : Sprite
    {
        private ComputeShaderTest test;

        public GameMain()
        {
            AddedToStage += AddedToStageHandler;
        }

        private void AddedToStageHandler(DisplayObject target, DisplayObject currentTarget)
        {
            test = new ComputeShaderTest();
            AddChild(test);

        }

    } 
}