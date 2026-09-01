#if UNITY_EDITOR
using UnityEditor;

public sealed class TutorialSpriteImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Tutorial/"))
            return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = assetPath.EndsWith("UI Settings Buttons.png")
            ? SpriteImportMode.Multiple
            : SpriteImportMode.Single;
        importer.filterMode = UnityEngine.FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        if (assetPath.EndsWith("objective_panel.png"))
            importer.spriteBorder = new UnityEngine.Vector4(42f, 42f, 42f, 42f);
    }
}
#endif
