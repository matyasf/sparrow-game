using Sparrow.Rendering;
using Sparrow.Styles;

namespace SparrowGame.Shared.CustomMesh
{
    public class CustomStyle : MeshStyle
    {
        public override MeshEffect CreateEffect()
        {
            return new CustomEffect();
        }

        /*
        public override void UpdateEffect(MeshEffect effect, RenderState state)
        {
            base.UpdateEffect(effect, state);
        }

        public override void CopyFrom(MeshStyle meshStyle)
        {
            base.CopyFrom(meshStyle);
        }
        */
    }
}