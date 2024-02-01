#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;

[ExecuteInEditMode]
public class GrassMaskDisplayer : MonoBehaviour
{
    private TerrainPainterComponent textureObject;   // Object from which we will take the texture we want to display
    private Texture2D texture;  // Actual mask texture

    private DecalProjector decalProjector;   // Decal object that should be updated
    private Material decalMaterial;


    private void OnValidate()   // Script load
    {
        // Get displayer
        GameObject child = transform.GetChild(0).gameObject;
        decalProjector = child.GetComponent<DecalProjector>();
        decalMaterial = decalProjector.material;

        if (textureObject == null)
        {
            // Object with the texture
            textureObject = GetComponent<TerrainPainterComponent>();

            // Set up displayer
            textureObject.onInitFinished.AddListener(InitDisplayer);
        }

        InitDisplayer();
    }

    public void InitDisplayer()
    {
        if (textureObject.GetMaskDisplayTexture() != null)
        {
            texture = textureObject.GetMaskDisplayTexture();

            decalProjector.transform.localPosition = new Vector3(0, textureObject.terrainDimensions.y / 2, 0);
            decalProjector.size = new Vector3(textureObject.terrainDimensions.x, textureObject.terrainDimensions.z, textureObject.terrainDimensions.y + 1);
            decalMaterial.SetTexture("Base_Map", texture);
        }
    }

    private void OnEnable()
    {
        Selection.selectionChanged += ToggleDecal;
        ToggleDecal();
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= ToggleDecal;
        textureObject.onInitFinished.RemoveListener(InitDisplayer);
    }

    private void OnDestroy()
    {
        Selection.selectionChanged -= ToggleDecal;
        textureObject.onInitFinished.RemoveListener(InitDisplayer);
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
