// Assets/Editor/ExportSpritesToPng.cs
using System.IO;
using System.Text;
using System.Globalization;
using UnityEditor;
using UnityEngine;

public static class ExportSpritesToPng
{
    [MenuItem("Assets/Export/Export Selected Sprites to PNG+JSON...", true)]
    private static bool ValidateMenu() => Selection.objects != null && Selection.objects.Length > 0;

    [MenuItem("Assets/Export/Export Selected Sprites to PNG+JSON...")]
    private static void ExportSelectedSprites()
    {
        string outFolder = EditorUtility.SaveFolderPanel("Choose export folder", "", "");
        if (string.IsNullOrEmpty(outFolder)) return;

        Object[] selection = Selection.objects;
        int exported = 0;

        foreach (Object obj in selection)
        {
            if (obj is Sprite s)
            {
                if (ExportOneSprite(s, outFolder)) exported++;
            }
            else if (obj is Texture2D tex)
            {
                string path = AssetDatabase.GetAssetPath(tex);
                var reps = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
                bool hadSprites = false;
                foreach (var rep in reps)
                {
                    if (rep is Sprite sub)
                    {
                        hadSprites = true;
                        if (ExportOneSprite(sub, outFolder)) exported++;
                    }
                }

                // If it's not a sliced spritesheet, export the whole texture+JSON
                if (!hadSprites)
                {
                    if (ExportWholeTexture(tex, outFolder)) exported++;
                }
            }
        }

        EditorUtility.RevealInFinder(outFolder);
        Debug.Log($"Exported {exported} file(s) (PNG+JSON pairs where applicable) to: {outFolder}");
    }

    // ---------- PNG helpers ----------

    private static bool ExportWholeTexture(Texture2D tex, string folder)
    {
        string path = AssetDatabase.GetAssetPath(tex);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);

        // Remember settings
        bool wasReadable = importer.isReadable;
        var oldCompression = importer.textureCompression;
        bool hadMip = importer.mipmapEnabled;

        try
        {
            // Make CPU readable & uncompressed for clean export
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var reloaded = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var copy = new Texture2D(reloaded.width, reloaded.height, TextureFormat.RGBA32, false);
            copy.SetPixels(reloaded.GetPixels());
            copy.Apply();

            string baseName = Sanitize(tex.name);
            string outPng = Path.Combine(folder, $"{baseName}.png");
            File.WriteAllBytes(outPng, copy.EncodeToPNG());

            // Minimal JSON for non-sliced texture
            string outJson = Path.Combine(folder, $"{baseName}.json");
            var json = new StringBuilder();
            json.Append("{\n");
            json.AppendLine($"  \"type\": \"texture\",");
            json.AppendLine($"  \"name\": \"{Escape(baseName)}\",");
            json.AppendLine($"  \"size\": {{ \"w\": {reloaded.width}, \"h\": {reloaded.height} }},");
            json.AppendLine($"  \"pixelsPerUnit\": {Float(importer.spritePixelsPerUnit)}");
            json.Append("}\n");
            File.WriteAllText(outJson, json.ToString(), Encoding.UTF8);

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed exporting texture {tex.name}: {e.Message}");
            return false;
        }
        finally
        {
            importer.isReadable = wasReadable;
            importer.textureCompression = oldCompression;
            importer.mipmapEnabled = hadMip;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }

    private static bool ExportOneSprite(Sprite sprite, string folder)
    {
        Texture2D sourceTex = sprite.texture;
        string path = AssetDatabase.GetAssetPath(sourceTex);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);

        // Remember settings
        bool wasReadable = importer.isReadable;
        var oldCompression = importer.textureCompression;
        bool hadMip = importer.mipmapEnabled;

        try
        {
            // Ensure readable & uncompressed so GetPixels works and colors are clean
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            // Reload after import change
            sourceTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            Rect tr = sprite.textureRect; // sprite pixel rect inside the source texture
            int x = Mathf.RoundToInt(tr.x);
            int y = Mathf.RoundToInt(tr.y);
            int w = Mathf.RoundToInt(tr.width);
            int h = Mathf.RoundToInt(tr.height);

            // Extract pixels
            Color[] pixels = sourceTex.GetPixels(x, y, w, h);
            var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            outTex.SetPixels(pixels);
            outTex.Apply();

            string baseName = Sanitize(sprite.name);
            string outPng = Path.Combine(folder, $"{baseName}.png");
            File.WriteAllBytes(outPng, outTex.EncodeToPNG());

            // ---------- JSON sidecar ----------
            // Pivot: Unity stores in pixels relative to rect; also provide normalized 0..1
            Vector2 pivotPx = sprite.pivot; // in pixels, relative to the sprite rect
            Vector2 pivotNorm = new Vector2(
                SafeDiv(pivotPx.x, tr.width),
                SafeDiv(pivotPx.y, tr.height)
            );

            // 9-slice border in pixels (left, bottom, right, top)
            Vector4 b = sprite.border;

            // Pixels Per Unit
            float ppu = sprite.pixelsPerUnit;

            // Source texture size
            int srcW = sourceTex.width;
            int srcH = sourceTex.height;

            string outJson = Path.Combine(folder, $"{baseName}.json");
            var json = new StringBuilder();
            json.Append("{\n");
            json.AppendLine($"  \"type\": \"sprite\",");
            json.AppendLine($"  \"name\": \"{Escape(baseName)}\",");
            json.AppendLine($"  \"textureName\": \"{Escape(Path.GetFileNameWithoutExtension(path))}\",");
            json.AppendLine($"  \"sourceTextureSize\": {{ \"w\": {srcW}, \"h\": {srcH} }},");
            json.AppendLine($"  \"rect\": {{ \"x\": {x}, \"y\": {y}, \"w\": {w}, \"h\": {h} }},");
            json.AppendLine($"  \"pixelsPerUnit\": {Float(ppu)},");
            json.AppendLine($"  \"pivot\": {{ \"pixels\": {{ \"x\": {Float(pivotPx.x)}, \"y\": {Float(pivotPx.y)} }}, \"normalized\": {{ \"x\": {Float(pivotNorm.x)}, \"y\": {Float(pivotNorm.y)} }} }},");
            json.AppendLine($"  \"border\": {{ \"left\": {Float(b.x)}, \"bottom\": {Float(b.y)}, \"right\": {Float(b.z)}, \"top\": {Float(b.w)} }},");
            json.AppendLine($"  \"hasNineSlice\": {(b != Vector4.zero ? "true" : "false")},");
            json.AppendLine($"  \"pixels\": {{ \"format\": \"RGBA32\", \"premultipliedAlpha\": false }}");
            json.Append("}\n");

            File.WriteAllText(outJson, json.ToString(), Encoding.UTF8);

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed exporting sprite {sprite.name}: {e.Message}");
            return false;
        }
        finally
        {
            // Restore original import settings
            importer.isReadable = wasReadable;
            importer.textureCompression = oldCompression;
            importer.mipmapEnabled = hadMip;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }

    // ---------- Utils ----------

    private static string Sanitize(string name)
    {
        // Keep it simple: replace path separators & trim spaces
        return name.Replace("/", "_").Replace("\\", "_").Trim();
    }

    private static string Escape(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string Float(float f)
    {
        return f.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static float SafeDiv(float a, float b)
    {
        return Mathf.Approximately(b, 0f) ? 0f : a / b;
    }
}
