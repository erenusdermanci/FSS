using Entities;
using UnityEditor;
using UnityEngine;

// ReSharper disable CheckNamespace

public class TexturePostProcessor : AssetPostprocessor
{
    private const string TexturesFolder = "Assets/Assets/";

    public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach(var assetPath in importedAssets)
        {
            // ReSharper disable once StringIndexOfIsCultureSpecific.1
            if (assetPath.IndexOf(TexturesFolder) == -1)
                continue;

            var spriteAsset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Sprite)) as Sprite;
            if (spriteAsset == null)
                continue;

            CustomPostprocessSprites(spriteAsset, assetPath);
        }
    }

    private static void CustomPostprocessSprites(Sprite sprite, string assetPath)
    {
        var splitPath = assetPath.Split('/');
        var filenameWithExtension = splitPath[splitPath.Length - 1];
        var filename = filenameWithExtension.Substring(0, filenameWithExtension.IndexOf('.'));

        var prefabGameObj = new GameObject(filename);
        var spriteRenderer = prefabGameObj.AddComponent<SpriteRenderer>();
        var entity = prefabGameObj.AddComponent<Entity>();
        prefabGameObj.AddComponent<EntitySnap>();

        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingLayerName = "ForegroundEntities";

        entity.ResourceName = filename;

        PrefabUtility.SaveAsPrefabAsset(prefabGameObj, $"Assets/Resources/{filename}.prefab");
        // ReSharper disable once AccessToStaticMemberViaDerivedType
        GameObject.DestroyImmediate(prefabGameObj);
    }
}
