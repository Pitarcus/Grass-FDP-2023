// CreateMaterialFromTextures.cs
// C#
using UnityEngine;
using UnityEditor;
using System.Linq;

public class CreateMaterialFromTextures : Editor
{
    [MenuItem("Tools/CreateMaterialFromTextures")]
    static void CreateMaterials()
    {
        try
        {
            AssetDatabase.StartAssetEditing();
            var textures = Selection.GetFiltered(typeof(Texture), SelectionMode.Assets).Cast<Texture>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            string path = AssetDatabase.GetAssetPath(textures.ElementAt(0));
            path = path.Substring(0, path.LastIndexOf("_")) + ".mat";

            if (AssetDatabase.LoadAssetAtPath(path, typeof(Material)) != null)
            {
                Debug.LogWarning("Can't create material, it already exists: " + path);
                return;
            }

            foreach (var tex in textures)
            {

                if (tex.name.Contains("Albedo") || tex.name.Contains("Base"))
                {
                    mat.mainTexture = tex;
                }
                else if (tex.name.Contains("Metallic"))
                {
                    mat.SetTexture("_MetallicGlossMap", tex);
                }
                else if (tex.name.Contains("Occlusion"))
                {
                    mat.SetTexture("_OcclusionMap", tex);
                }
                else if (tex.name.Contains("Normal"))
                {
                    mat.SetTexture("_BumpMap", tex);
                }

            }

            AssetDatabase.CreateAsset(mat, path);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
        }
    }
}
