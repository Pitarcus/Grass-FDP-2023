using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;

[ExecuteInEditMode]
public class GrassMaskDisplayer : MonoBehaviour
{
    public TerrainPainterComponent textureObject;   // Object from which we will take the texture we want to display
    public Texture2D texture;   // this is public for debuggin purposes

    Terrain terrain;
    private TerrainData terrainData;

    TerrainLayer terrainLayer;

    //public Texture2D newTex;

    public DecalProjector decalProjector;   // Decal object that should be updated
    Material decalMaterial;
    public Shader decalShader;

    private void OnValidate()
    {
        textureObject = GetComponent<TerrainPainterComponent>();
    }

    public void InitDisplayer()
    {
        terrain = textureObject.terrain;

        if (terrain != null && textureObject.maskTexture != null)
        {
            texture = textureObject.maskTexture;

            /* FOR NOW IT HAS NO UTILITY
            // Create the new texture that will have the proper size
            newTex = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, true);
            newTex.wrapMode = TextureWrapMode.Clamp;
            newTex.filterMode = FilterMode.Point;
            */
            terrainLayer = new TerrainLayer();

            decalMaterial = new Material(decalShader);
            decalProjector.material = decalMaterial;
            decalProjector.size = new Vector3( terrain.terrainData.size.x, terrain.terrainData.size.z, 75);
            decalMaterial.SetTexture("Base_Map", texture);
        }
    }
#if UNITY_EDITOR
    private void OnEnable()
    {
        Selection.selectionChanged += ToggleDecal;
        textureObject.onInitFinished.AddListener(InitDisplayer);
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= ToggleDecal;
        textureObject.onInitFinished.RemoveListener(InitDisplayer);
    }
  
    void ToggleDecal()
    {
        if (Selection.activeGameObject != this.gameObject)
        {
            decalProjector.enabled = false;
        }
        else
        {
            decalProjector.enabled = true;
        }
    }
#endif
}
