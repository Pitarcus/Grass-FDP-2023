using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;

[ExecuteInEditMode]
public class GrassMaskDisplayer : MonoBehaviour
{
    public TerrainPainterComponent textureObject;   // Object from which we will take the texture we want to display
    public Texture2D texture;   // this is public for debuggin purposes

    public Terrain terrain;
    private TerrainData terrainData;

    TerrainLayer terrainLayer;

    public Texture2D newTex;

    public DecalProjector decalProjector;   // Decal object that should be updated
    public Material decalMaterial;
    public Shader decalShader;

    private void OnValidate()
    {
        if(terrain!= null && textureObject.maskTexture != null)
        {
            texture = textureObject.maskTexture;

            // Create the new texture that will have the proper 
            newTex = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, true);
            newTex.wrapMode = TextureWrapMode.Clamp;
            newTex.filterMode = FilterMode.Point;

            terrainLayer = new TerrainLayer();

            decalMaterial = new Material(decalShader);
            decalProjector.material = decalMaterial;
            decalMaterial.SetTexture("Base_Map", texture);
        }
    }
#if UNITY_EDITOR
    private void OnEnable()
    {
        Selection.selectionChanged += ToggleDecal;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= ToggleDecal;
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
