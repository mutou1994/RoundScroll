namespace UnityEngine.UI
{
    public class HotArea : Graphic
    {
        protected override void UpdateGeometry()
        {

        }

        protected override void UpdateMaterial()
        {

        }

        protected HotArea()
        {
            useLegacyMeshGeneration = false;
        }

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            toFill.Clear();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            var hotArea = this.GetComponent<HotArea>();
            if (hotArea != null)
            {
                hotArea.raycastTarget = true;
            }

            base.Reset();
        }

#endif

    }
}
