using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MasterPuzzleWall.Editor
{
    public static class WhiteBackgroundRemover
    {
        [MenuItem("Tools/Master Puzzle Wall/Remove White Backgrounds")]
        public static void ProcessAllWhiteBackgrounds()
        {
            string imgDir = Path.Combine(Application.dataPath, "Images");
            if (!Directory.Exists(imgDir)) return;

            string[] targetFiles = new string[]
            {
                "stone_golem_bot",
                "stone_golem_confused",
                "gemstone_trophy",
                "stone_dial_wheel",
                "glowing_starter_gem",
                "stone_twin_plug",
                "game_title_logo",
                "ice_shelf_key_tile"
            };

            foreach (string baseName in targetFiles)
            {
                string srcPath = Path.Combine(imgDir, baseName + ".jpg");
                if (!File.Exists(srcPath))
                {
                    srcPath = Path.Combine(imgDir, baseName + ".png");
                }
                if (!File.Exists(srcPath)) continue;

                string outPngPath = Path.Combine(imgDir, baseName + ".png");
                ProcessImageTransparency(srcPath, outPngPath);
            }

            AssetDatabase.Refresh();

            // Re-import all PNGs as Sprites with alphaIsTransparency = true
            foreach (string baseName in targetFiles)
            {
                string assetPath = "Assets/Images/" + baseName + ".png";
                TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (ti != null)
                {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.alphaIsTransparency = true;
                    ti.mipmapEnabled = false;
                    ti.SaveAndReimport();
                    Debug.Log($"<color=#00FF88>[WhiteBackgroundRemover] Configured Sprite for {baseName}.png</color>");
                }
            }
        }

        public static void ProcessImageTransparency(string inputPath, string outputPath)
        {
            byte[] fileData = File.ReadAllBytes(inputPath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(fileData))
            {
                UnityEngine.Object.DestroyImmediate(tex);
                return;
            }

            int w = tex.width;
            int h = tex.height;
            Color32[] pixels = tex.GetPixels32();

            // Flood-fill from borders to only remove external white background
            bool[] visited = new bool[pixels.Length];
            Queue<int> queue = new Queue<int>();

            // Seed with all border pixels
            for (int x = 0; x < w; x++)
            {
                queue.Enqueue(0 * w + x);         // bottom edge
                queue.Enqueue((h - 1) * w + x);   // top edge
            }
            for (int y = 0; y < h; y++)
            {
                queue.Enqueue(y * w + 0);         // left edge
                queue.Enqueue(y * w + (w - 1));   // right edge
            }

            // High-tolerance white detection
            bool IsBackgroundWhite(Color32 c)
            {
                int r = c.r;
                int g = c.g;
                int b = c.b;
                int maxDiff = Mathf.Max(Mathf.Abs(r - g), Mathf.Max(Mathf.Abs(r - b), Mathf.Abs(g - b)));
                // Near white/gray background
                return (r > 215 && g > 215 && b > 215 && maxDiff < 35);
            }

            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                if (visited[idx]) continue;
                visited[idx] = true;

                Color32 c = pixels[idx];
                if (IsBackgroundWhite(c))
                {
                    // Compute smooth alpha feathering
                    int brightness = (c.r + c.g + c.b) / 3;
                    byte alpha = 0;
                    if (brightness < 240)
                    {
                        alpha = (byte)Mathf.Clamp((240 - brightness) * 4, 0, 255);
                    }
                    pixels[idx] = new Color32(c.r, c.g, c.b, alpha);

                    int px = idx % w;
                    int py = idx / w;

                    // 4-neighborhood
                    if (px > 0 && !visited[idx - 1]) queue.Enqueue(idx - 1);
                    if (px < w - 1 && !visited[idx + 1]) queue.Enqueue(idx + 1);
                    if (py > 0 && !visited[idx - w]) queue.Enqueue(idx - w);
                    if (py < h - 1 && !visited[idx + w]) queue.Enqueue(idx + w);
                }
            }

            // Also do a safety pass on any remaining isolated white pixels near borders
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    Color32 c = pixels[i];
                    if (c.r > 245 && c.g > 245 && c.b > 245)
                    {
                        // Check if adjacent to a transparent pixel
                        bool nearTransparent = false;
                        if (x > 0 && pixels[i - 1].a == 0) nearTransparent = true;
                        if (x < w - 1 && pixels[i + 1].a == 0) nearTransparent = true;
                        if (y > 0 && pixels[i - w].a == 0) nearTransparent = true;
                        if (y < h - 1 && pixels[i + w].a == 0) nearTransparent = true;

                        if (nearTransparent)
                        {
                            pixels[i] = new Color32(c.r, c.g, c.b, 0);
                        }
                    }
                }
            }

            Texture2D outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            outTex.SetPixels32(pixels);
            outTex.Apply();

            byte[] pngBytes = outTex.EncodeToPNG();
            File.WriteAllBytes(outputPath, pngBytes);

            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(outTex);
            Debug.Log($"<color=#00FF88>[WhiteBackgroundRemover] Saved transparent PNG: {outputPath}</color>");
        }
    }
}
