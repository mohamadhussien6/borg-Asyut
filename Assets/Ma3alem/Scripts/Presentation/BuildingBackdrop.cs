using UnityEngine;

namespace Ma3alem
{
    /// <summary>A permanent camera-anchored decorative frame, independent of gameplay physics.</summary>
    public sealed class BuildingBackdrop : MonoBehaviour
    {
        [SerializeField] private Sprite[] environmentPanels;

        // Retain existing scene asset data for the historical import commands; never instantiate it.
        [SerializeField, HideInInspector] private GameObject floorPrefab;
        [SerializeField, HideInInspector] private Sprite landmarkSprite;
        [SerializeField, HideInInspector] private Sprite[] wallMaterials;
        [SerializeField, HideInInspector] private Sprite[] componentSprites;
        [SerializeField, HideInInspector] private Sprite[] architecturalVariants;
        [SerializeField, HideInInspector] private Sprite[] completeFacadeVariants;
        [SerializeField, HideInInspector] private int scenerySeed = 4313;
        [SerializeField, HideInInspector] private float moduleWidth = 3.6f;
        [SerializeField, HideInInspector] private float foregroundStoreyHeight = 3.3f;

        private readonly SpriteRenderer[] panels = new SpriteRenderer[3];
        private Camera view;
        private float artworkWidth, artworkHeight;
        public int PanelCount => panels[0] == null ? 0 : panels.Length;

        public void Initialize(GameManager game)
        {
            view = game.Follow.GetComponent<Camera>();
            GetComponent<SpriteRenderer>().enabled = false;
            if (environmentPanels == null || environmentPanels.Length != 3)
            { Debug.LogError("The complete Assiut environment needs left, center and right panels.", this); return; }
            foreach (var sprite in environmentPanels)
                if (sprite == null) { Debug.LogError("Missing Assiut environment panel.", this); return; }
            foreach (var sprite in environmentPanels) artworkWidth += sprite.bounds.size.x;
            artworkHeight = environmentPanels[0].bounds.size.y;
            float x = -artworkWidth / 2;
            for (int i = 0; i < panels.Length; i++)
            {
                var renderer = new GameObject(i == 0 ? "Fixed left facade" : i == 1 ? "Fixed central skyline" : "Fixed right facade", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                renderer.transform.SetParent(transform, false);
                renderer.sprite = environmentPanels[i]; renderer.drawMode = SpriteDrawMode.Simple;
                renderer.sortingOrder = -30; renderer.color = Color.white;
                float width = renderer.sprite.bounds.size.x;
                renderer.transform.localPosition = new Vector3(x + width / 2, 0, 0); x += width;
                panels[i] = renderer;
            }
            Refresh();
        }
        public void Refresh()
        {
            if (panels[0] == null) return;
            // Equal camera/world displacement means zero screen displacement, at every gameplay height.
            transform.position = new Vector3(view.transform.position.x, view.transform.position.y, 5);
            float scale = Mathf.Max((view.orthographicSize * 2 + 0.02f) / artworkHeight,
                (view.orthographicSize * 2 * view.aspect + 0.02f) / artworkWidth);
            transform.localScale = new Vector3(scale, scale, 1);
        }
    }
}
