#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;

[ExecuteInEditMode]
public class GrassMaskDisplayer : MonoBehaviour
{
    private TerrainPainterComponent painter;   // Object from which we will take the texture we want to display
    private Texture2D texture;  // Actual mask texture

    private DecalProjector decalProjector;   // Decal object that should be updated
    private Material decalMaterial;


    public void InitDisplayer()
    {
        if(Application.isPlaying) { return; }

        if (painter.GetMaskDisplayTexture() != null)
        {
            texture = painter.GetMaskDisplayTexture();

            decalProjector.transform.localPosition = new Vector3(0, painter.terrainDimensions.y / 2, 0);
            decalProjector.size = new Vector3(painter.terrainDimensions.x, painter.terrainDimensions.z, painter.terrainDimensions.y + 1);
            decalMaterial.SetTexture("Base_Map", texture);
        }
    }

    private void OnEnable()
    {
        // Get displayer
        GameObject child = transform.GetChild(0).gameObject;
        decalProjector = child.GetComponent<DecalProjector>();
        decalMaterial = decalProjector.material;

        if (painter == null)
        {
            // Object with the texture
            painter = GetComponent<TerrainPainterComponent>();

            // Set up displayer
            painter.onInitFinished.AddListener(InitDisplayer);
        }

        InitDisplayer();

        Selection.selectionChanged += ToggleDecal;
        ToggleDecal();
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= ToggleDecal;
        painter.onInitFinished.RemoveListener(InitDisplayer);
    }

    private void OnDestroy()
    {
        Selection.selectionChanged -= ToggleDecal;
        painter.onInitFinished.RemoveListener(InitDisplayer);
    }

    void ToggleDecal()
    {
        if (gameObject == null || decalProjector == null)
        {
            return;
        }
        if (Selection.activeGameObject != gameObject)
        {
            decalProjector.enabled = false;
        }
        else
        {
            InitDisplayer();
            decalProjector.enabled = true;
        }
    }
}
#endif
