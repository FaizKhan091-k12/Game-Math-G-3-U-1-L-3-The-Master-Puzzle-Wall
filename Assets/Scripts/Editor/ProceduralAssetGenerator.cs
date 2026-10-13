using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MasterPuzzleWall.Editor
{
    public static class ProceduralAssetGenerator
    {
        [MenuItem("Tools/Master Puzzle Wall/1. Generate Audio & Prepare Textures")]
        public static void GenerateAllAssets()
        {
            GenerateAudioClips();
            PrepareTexturesAndTransparency();
            CreateParticleGlowAsset();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00FF88><b>[MasterPuzzleWall]</b> Generated Audio, Processed Sprites, and Particle Materials Successfully!</color>");
        }

        public static void CreateParticleGlowAsset()
        {
            string imgDir = Path.Combine(Application.dataPath, "Images");
            if (!Directory.Exists(imgDir)) Directory.CreateDirectory(imgDir);

            string imgPath = Path.Combine(imgDir, "particle_glow.png");
            if (!File.Exists(imgPath))
            {
                int size = 64;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float center = (size - 1) / 2f;
                float maxDist = size / 2f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                        float norm = Mathf.Clamp01(dist / maxDist);
                        float alpha = Mathf.Clamp01((1f - norm) * (1f - norm));
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
                tex.Apply();
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(imgPath, bytes);
                UnityEngine.Object.DestroyImmediate(tex);

                AssetDatabase.ImportAsset("Assets/Images/particle_glow.png", ImportAssetOptions.ForceUpdate);
                TextureImporter ti = AssetImporter.GetAtPath("Assets/Images/particle_glow.png") as TextureImporter;
                if (ti != null)
                {
                    ti.textureType = TextureImporterType.Default;
                    ti.alphaIsTransparency = true;
                    ti.mipmapEnabled = false;
                    ti.SaveAndReimport();
                }
            }

            string matDir = Path.Combine(Application.dataPath, "Materials");
            if (!Directory.Exists(matDir)) Directory.CreateDirectory(matDir);

            string matPath = "Assets/Materials/ParticleGlow.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("UI/Default");

                mat = new Material(shader);
                Texture2D glowTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Images/particle_glow.png");
                if (glowTex != null)
                {
                    mat.mainTexture = glowTex;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", glowTex);
                }
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
                if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f); // Alpha blend

                AssetDatabase.CreateAsset(mat, matPath);
                AssetDatabase.SaveAssets();
            }
        }

        public static void GenerateAudioClips()
        {
            string audioDir = Path.Combine(Application.dataPath, "Audio");
            if (!Directory.Exists(audioDir))
            {
                Directory.CreateDirectory(audioDir);
            }

            CreateWavFile(Path.Combine(audioDir, "sfx_click.wav"), GenerateClick());
            CreateWavFile(Path.Combine(audioDir, "sfx_snap.wav"), GenerateSnap());
            CreateWavFile(Path.Combine(audioDir, "sfx_chime.wav"), GenerateChime());
            CreateWavFile(Path.Combine(audioDir, "sfx_fanfare.wav"), GenerateFanfare());
            CreateWavFile(Path.Combine(audioDir, "sfx_error.wav"), GenerateError());
            CreateWavFile(Path.Combine(audioDir, "sfx_ratchet.wav"), GenerateRatchet());
            CreateWavFile(Path.Combine(audioDir, "sfx_magic_hum.wav"), GenerateMagicHum());
            CreateWavFile(Path.Combine(audioDir, "sfx_ice_slide.wav"), GenerateIceSlide());
            CreateWavFile(Path.Combine(audioDir, "sfx_bg_ambience.wav"), GenerateAmbience());

            Debug.Log("[MasterPuzzleWall] Generated 9 procedural WAV sound files in Assets/Audio.");
        }

        private static float[] GenerateClick()
        {
            int sampleRate = 44100;
            float duration = 0.08f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 60f);
                float f = 1200f * Mathf.Exp(-t * 30f);
                data[i] = Mathf.Sin(2 * Mathf.PI * f * t) * env * 0.7f;
            }
            return data;
        }

        private static float[] GenerateSnap()
        {
            int sampleRate = 44100;
            float duration = 0.22f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envThud = Mathf.Exp(-t * 25f);
                float envPing = Mathf.Exp(-t * 15f);
                float thud = Mathf.Sin(2 * Mathf.PI * 180f * t) * envThud * 0.7f;
                float ping = Mathf.Sin(2 * Mathf.PI * 1480f * t) * envPing * 0.35f;
                data[i] = Mathf.Clamp(thud + ping, -1f, 1f);
            }
            return data;
        }

        private static float[] GenerateChime()
        {
            int sampleRate = 44100;
            float duration = 1.0f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            float[] freqs = { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.51f }; // C5, E5, G5, C6, E6
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float s = 0f;
                for (int c = 0; c < freqs.Length; c++)
                {
                    float noteDelay = c * 0.06f;
                    if (t >= noteDelay)
                    {
                        float noteT = t - noteDelay;
                        float env = Mathf.Exp(-noteT * 3.8f);
                        s += Mathf.Sin(2 * Mathf.PI * freqs[c] * noteT) * env * 0.22f;
                    }
                }
                data[i] = Mathf.Clamp(s, -1f, 1f);
            }
            return data;
        }

        private static float[] GenerateFanfare()
        {
            int sampleRate = 44100;
            float duration = 2.2f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            // Triumphant ascending sequence: C5, F5, G5, C6
            float[] melodyTimes = { 0.0f, 0.22f, 0.44f, 0.70f };
            float[] melodyFreqs = { 523.25f, 698.46f, 783.99f, 1046.50f };
            float[] melodyDurs = { 0.25f, 0.25f, 0.30f, 1.4f };

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float sum = 0f;
                for (int m = 0; m < melodyTimes.Length; m++)
                {
                    if (t >= melodyTimes[m])
                    {
                        float localT = t - melodyTimes[m];
                        float dur = melodyDurs[m];
                        if (localT < dur)
                        {
                            float env = Mathf.Exp(-localT * (m == melodyTimes.Length - 1 ? 1.8f : 4f));
                            float f = melodyFreqs[m];
                            float wave = Mathf.Sin(2 * Mathf.PI * f * localT) +
                                         0.4f * Mathf.Sin(2 * Mathf.PI * f * 2f * localT) +
                                         0.2f * Mathf.Sin(2 * Mathf.PI * f * 3f * localT);
                            sum += wave * env * 0.28f;
                        }
                    }
                }
                data[i] = Mathf.Clamp(sum, -1f, 1f);
            }
            return data;
        }

        private static float[] GenerateError()
        {
            int sampleRate = 44100;
            float duration = 0.35f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 10f);
                float f = 190f - t * 60f;
                float wave = Mathf.Sin(2 * Mathf.PI * f * t) + 0.3f * Mathf.Sin(2 * Mathf.PI * (f * 1.414f) * t);
                data[i] = Mathf.Clamp(wave * env * 0.5f, -1f, 1f);
            }
            return data;
        }

        private static float[] GenerateRatchet()
        {
            int sampleRate = 44100;
            float duration = 0.5f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            int clicks = 8;
            for (int c = 0; c < clicks; c++)
            {
                int startSample = (int)(c * (samples / (float)clicks));
                for (int i = 0; i < 600 && startSample + i < samples; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = Mathf.Exp(-t * 90f);
                    float click = Mathf.Sin(2 * Mathf.PI * 1600f * t) * env * 0.4f;
                    data[startSample + i] += click;
                }
            }
            for (int i = 0; i < samples; i++)
            {
                data[i] = Mathf.Clamp(data[i], -1f, 1f);
            }
            return data;
        }

        private static float[] GenerateMagicHum()
        {
            int sampleRate = 44100;
            float duration = 1.2f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
                float tremolo = 1f + 0.25f * Mathf.Sin(2 * Mathf.PI * 5f * t);
                float wave = Mathf.Sin(2 * Mathf.PI * 220f * t) * 0.4f +
                             Mathf.Sin(2 * Mathf.PI * 440f * t) * 0.3f +
                             Mathf.Sin(2 * Mathf.PI * 660f * t) * 0.15f;
                data[i] = Mathf.Clamp(wave * env * tremolo * 0.6f, -1f, 1f);
            }
            return data;
        }

        private static float[] GenerateIceSlide()
        {
            int sampleRate = 44100;
            float duration = 0.4f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            System.Random rnd = new System.Random(42);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.25f;
                float tone = Mathf.Sin(2 * Mathf.PI * (800f + t * 400f) * t) * 0.3f;
                data[i] = Mathf.Clamp((noise + tone) * env * 0.6f, -1f, 1f);
            }
            return data;
        }

        private static float[] GenerateAmbience()
        {
            int sampleRate = 44100;
            float duration = 4.0f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float drone1 = Mathf.Sin(2 * Mathf.PI * 110f * t) * 0.18f;
                float drone2 = Mathf.Sin(2 * Mathf.PI * 164.81f * t) * 0.12f;
                float wave = drone1 + drone2;
                data[i] = Mathf.Clamp(wave * 0.4f, -1f, 1f);
            }
            return data;
        }

        private static void CreateWavFile(string path, float[] samples)
        {
            int sampleRate = 44100;
            short channels = 1;
            short bitsPerSample = 16;
            int subChunk2Size = samples.Length * channels * (bitsPerSample / 8);
            int chunkSize = 36 + subChunk2Size;

            using (FileStream fs = new FileStream(path, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                // RIFF header
                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(chunkSize);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

                // fmt chunk
                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16); // Subchunk1Size
                bw.Write((short)1); // PCM
                bw.Write(channels);
                bw.Write(sampleRate);
                bw.Write(sampleRate * channels * (bitsPerSample / 8)); // ByteRate
                bw.Write((short)(channels * (bitsPerSample / 8))); // BlockAlign
                bw.Write(bitsPerSample);

                // data chunk
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(subChunk2Size);

                for (int i = 0; i < samples.Length; i++)
                {
                    short sampleVal = (short)Mathf.Clamp(samples[i] * 32767f, -32768f, 32767f);
                    bw.Write(sampleVal);
                }
            }
        }

        public static void PrepareTexturesAndTransparency()
        {
            string imgDir = Path.Combine(Application.dataPath, "Images");
            if (!Directory.Exists(imgDir)) return;

            string[] filesToTransparentize = new string[]
            {
                "stone_golem_bot",
                "stone_golem_confused",
                "gemstone_trophy",
                "stone_dial_wheel",
                "glowing_starter_gem",
                "stone_twin_plug",
                "game_title_logo"
            };

            foreach (string baseName in filesToTransparentize)
            {
                string jpgPath = Path.Combine(imgDir, baseName + ".jpg");
                string pngPath = Path.Combine(imgDir, baseName + ".png");
                string relativeJpg = "Assets/Images/" + baseName + ".jpg";

                if (File.Exists(jpgPath))
                {
                    TextureImporter ti = AssetImporter.GetAtPath(relativeJpg) as TextureImporter;
                    if (ti != null)
                    {
                        ti.isReadable = true;
                        ti.textureType = TextureImporterType.Default;
                        ti.SaveAndReimport();
                    }

                    byte[] bytes = File.ReadAllBytes(jpgPath);
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(bytes))
                    {
                        Color[] pixels = tex.GetPixels();
                        for (int p = 0; p < pixels.Length; p++)
                        {
                            Color c = pixels[p];
                            // Check if near white
                            float brightness = (c.r + c.g + c.b) / 3f;
                            float maxDiff = Mathf.Max(Mathf.Abs(c.r - c.g), Mathf.Max(Mathf.Abs(c.r - c.b), Mathf.Abs(c.g - c.b)));
                            if (brightness > 0.90f && maxDiff < 0.12f)
                            {
                                float alpha = Mathf.Clamp01((1f - brightness) / 0.10f);
                                pixels[p] = new Color(c.r, c.g, c.b, alpha);
                            }
                        }
                        tex.SetPixels(pixels);
                        tex.Apply();
                        byte[] pngBytes = tex.EncodeToPNG();
                        File.WriteAllBytes(pngPath, pngBytes);
                        UnityEngine.Object.DestroyImmediate(tex);
                        Debug.Log("[MasterPuzzleWall] Processed transparent PNG: " + baseName + ".png");
                    }
                }
            }

            AssetDatabase.Refresh();

            // Configure all Images as Sprites
            string[] allImageAssets = Directory.GetFiles(imgDir);
            foreach (string file in allImageAssets)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext == ".png" || ext == ".jpg")
                {
                    string assetPath = "Assets/Images/" + Path.GetFileName(file);
                    TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (ti != null)
                    {
                        ti.textureType = TextureImporterType.Sprite;
                        ti.spriteImportMode = SpriteImportMode.Single;
                        ti.alphaIsTransparency = true;
                        ti.mipmapEnabled = false;
                        ti.SaveAndReimport();
                    }
                }
            }
        }
    }
}
