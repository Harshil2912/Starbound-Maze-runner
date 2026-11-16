using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class UIPolishGenerator
{
    const string HdDir = "Assets/UI/HD";
    const string FontDir = "Assets/Fonts";
    const string RawEmblemPath = @"C:\Users\HP\.gemini\antigravity\brain\b6fab93b-1ba9-454b-816f-e3f717ad030e\starbound_emblem_raw_1794481239842.jpg";

    struct PendingSpriteMeta
    {
        public string path;
        public Vector4 border;
        public float ppu;
        public TextureImporterCompression comp;
        public int maxSize;
    }

    [MenuItem("Tools/UI Polish/Generate HD UI Assets")]
    public static void GenerateAllHDAssets()
    {
        if (!Directory.Exists(HdDir)) Directory.CreateDirectory(HdDir);
        if (!Directory.Exists(FontDir)) Directory.CreateDirectory(FontDir);

        CopyProjectFonts();

        var metas = new List<PendingSpriteMeta>();

        // 1. Baked Deep Space Glass Panels (@2x Retina: ppu = 200)
        WriteSpritePng(metas, "UI_Panel_Modal_HD", GenerateModalPanel(384, 384), new Vector4(36, 36, 36, 36), 150f, TextureImporterCompression.Uncompressed);
        WriteSpritePng(metas, "UI_Banner_HD", GenerateBannerPanel(384, 192), new Vector4(34, 32, 34, 32), 150f, TextureImporterCompression.Uncompressed);

        // 2. Neutral High-Key Tintable 9-Slice Buttons / Cards / Pads
        WriteSpritePng(metas, "UI_Btn_Primary_HD", GenerateTintableChamferButton(384, 192, 18f, 3.6f, 0.78f, 1.00f), new Vector4(34, 32, 34, 32), 150f, TextureImporterCompression.Uncompressed);
        WriteSpritePng(metas, "UI_Btn_Tech_HD", GenerateTintableChamferButton(384, 192, 14f, 3.2f, 0.70f, 0.96f), new Vector4(30, 28, 30, 28), 150f, TextureImporterCompression.Uncompressed);
        WriteSpritePng(metas, "UI_Btn_Tab_HD", GenerateTintableTab(384, 192), new Vector4(28, 16, 28, 28), 150f, TextureImporterCompression.Uncompressed);
        WriteSpritePng(metas, "UI_Card_Level_HD", GenerateTintableLevelCard(384, 384), new Vector4(36, 102, 36, 90), 150f, TextureImporterCompression.Uncompressed);
        WriteSpritePng(metas, "UI_HudPad_HD", GenerateTintableHudPad(384, 384), new Vector4(40, 40, 40, 40), 150f, TextureImporterCompression.Uncompressed);
        WriteSpritePng(metas, "UI_EditFrame_HD", GenerateEditFrame(384, 384), new Vector4(44, 44, 44, 44), 150f, TextureImporterCompression.Uncompressed);

        // 3. Sliders
        WriteSpritePng(metas, "UI_Slider_Track_HD", GenerateSliderTrack(256, 64), new Vector4(40, 12, 40, 12), 350f, TextureImporterCompression.Uncompressed);
        WriteSpritePng(metas, "UI_Slider_Fill_HD", GenerateSliderFill(256, 64), new Vector4(40, 12, 40, 12), 350f, TextureImporterCompression.Uncompressed);
        WriteSpritePng(metas, "UI_Slider_Thumb_HD", GenerateSliderThumb(256, 256), Vector4.zero, 200f, TextureImporterCompression.Uncompressed);

        // 4. Crisp HD Icons
        WriteSpritePng(metas, "UI_Heart_Full_HD", GenerateHeart(256, 256, true), Vector4.zero, 200f, TextureImporterCompression.CompressedHQ);
        WriteSpritePng(metas, "UI_Heart_Empty_HD", GenerateHeart(256, 256, false), Vector4.zero, 200f, TextureImporterCompression.CompressedHQ);
        WriteSpritePng(metas, "UI_Icon_Chevron_HD", GenerateChevron(256, 256), Vector4.zero, 200f, TextureImporterCompression.Uncompressed);
        WriteSpritePng(metas, "UI_Icon_Pause_HD", GeneratePauseIcon(256, 256), Vector4.zero, 200f, TextureImporterCompression.Uncompressed);
        WriteSpritePng(metas, "UI_HudCoreRing_HD", GenerateRing(256, 256, 0.72f, 0.92f), Vector4.zero, 200f, TextureImporterCompression.Uncompressed);
        WriteSpritePng(metas, "UI_HudCoreOrb_HD", GenerateCoreOrb(256, 256), Vector4.zero, 200f, TextureImporterCompression.Uncompressed);
        WriteSpritePng(metas, "UI_Icon_Scan_HD", GenerateScanReticleIcon(256, 256), Vector4.zero, 200f, TextureImporterCompression.Uncompressed);
        WriteSpritePng(metas, "UI_Icon_Core_HD", GenerateCoreIcon(256, 256), Vector4.zero, 200f, TextureImporterCompression.Uncompressed);
        WriteSpritePng(metas, "UI_Icon_Shield_HD", GenerateShieldIcon(256, 256), Vector4.zero, 200f, TextureImporterCompression.Uncompressed);
        WriteSpritePng(metas, "UI_TitleGlow_HD", GenerateTitleGlow(512, 256), Vector4.zero, 100f, TextureImporterCompression.CompressedHQ);

        // 5. Key & write generated Starbound Emblem & Title Logos
        WriteEmblemPng(metas);
        WriteLogosPng(metas);

        // Batch import once so Editor does all imports in one pass!
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var m in metas)
            {
                var ti = AssetImporter.GetAtPath(m.path) as TextureImporter;
                if (ti == null) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = m.ppu;
                ti.spriteBorder = m.border;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.filterMode = FilterMode.Bilinear;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.textureCompression = m.comp;
                if (m.maxSize > 0) ti.maxTextureSize = m.maxSize;
                ti.SaveAndReimport();
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        AssetDatabase.Refresh();
        Debug.Log($"[UIPolishGenerator] Generated {metas.Count} HD UI sprites + fonts.");
    }

    static void CopyProjectFonts()
    {
        CopyIfMissing("Packages/com.unity.searcher/Editor/Resources/FlatSkin/Font/Roboto-Black.ttf", $"{FontDir}/Roboto-Black.ttf");
        CopyIfMissing("Packages/com.unity.searcher/Editor/Resources/FlatSkin/Font/Roboto-Bold.ttf", $"{FontDir}/Roboto-Bold.ttf");
        CopyIfMissing("Packages/com.unity.searcher/Editor/Resources/FlatSkin/Font/Roboto-Medium.ttf", $"{FontDir}/Roboto-Medium.ttf");
        CopyIfMissing("Packages/com.unity.searcher/Editor/Resources/FlatSkin/Font/Roboto-Regular.ttf", $"{FontDir}/Roboto-Regular.ttf");
        CopyIfMissing("Packages/com.unity.collab-proxy/Editor/Assets/Fonts/Cascadia.ttf", $"{FontDir}/Cascadia.ttf");
    }

    static void CopyIfMissing(string srcRel, string dstRel)
    {
        if (!File.Exists(srcRel)) return;
        if (!File.Exists(dstRel)) File.Copy(srcRel, dstRel, false);
    }

    static void WriteEmblemPng(List<PendingSpriteMeta> metas)
    {
        if (!File.Exists(RawEmblemPath))
        {
            Debug.LogWarning("[UIPolishGenerator] Raw emblem file not found at: " + RawEmblemPath);
            return;
        }

        byte[] bytes = File.ReadAllBytes(RawEmblemPath);
        var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!src.LoadImage(bytes))
        {
            Object.DestroyImmediate(src);
            return;
        }

        int w = src.width, h = src.height;
        Color[] pix = src.GetPixels();
        for (int i = 0; i < pix.Length; i++)
        {
            Color c = pix[i];
            float lum = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            int px = i % w;
            int py = i / w;
            float nx = (px / (float)(w - 1)) * 2f - 1f;
            float ny = (py / (float)(h - 1)) * 2f - 1f;
            float radial = 1f - SmoothEdge(0.72f, 0.94f, Mathf.Sqrt(nx * nx + ny * ny));
            // Tight black-floor cutoff so faint compressed JPEG dark halo becomes 100% transparent
            float keyAlpha = SmoothEdge(0.065f, 0.24f, lum);
            float alpha = Mathf.Clamp01(keyAlpha * radial);
            // Un-premultiply dark background out of semi-transparent glow pixels so outer halo is pure cyan, never grey smoke
            if (alpha > 0.005f && lum > 0.001f && alpha < 0.98f)
            {
                float gain = Mathf.Min(3.2f, 1.0f / Mathf.Max(alpha, 0.31f));
                c.r = Mathf.Clamp01(c.r * gain);
                c.g = Mathf.Clamp01(c.g * gain);
                c.b = Mathf.Clamp01(c.b * gain);
            }
            c.a = alpha;
            pix[i] = c;
        }
        var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
        dst.SetPixels(pix);
        dst.Apply();

        string outPath = $"{HdDir}/Starbound_Emblem_HD.png";
        File.WriteAllBytes(outPath, dst.EncodeToPNG());
        Object.DestroyImmediate(src);
        Object.DestroyImmediate(dst);

        metas.Add(new PendingSpriteMeta
        {
            path = outPath,
            border = Vector4.zero,
            ppu = 100f,
            comp = TextureImporterCompression.CompressedHQ,
            maxSize = 512
        });
    }

    static void WriteLogosPng(List<PendingSpriteMeta> metas)
    {
        string[] rawNames = {
            "logo_raw_cyber_cyan.jpg",
            "logo_raw_chrome_crimson.jpg",
            "logo_raw_mecha_speed.jpg",
            "logo_raw_cosmic_minimal.jpg"
        };
        string[] outNames = {
            "Starbound_Logo_CyberCyan",
            "Starbound_Logo_ChromeCrimson",
            "Starbound_Logo_MechaSpeed",
            "Starbound_Logo_CosmicMinimal"
        };

        for (int k = 0; k < rawNames.Length; k++)
        {
            string rawPath = $"{HdDir}/{rawNames[k]}";
            if (!File.Exists(rawPath)) continue;

            byte[] bytes = File.ReadAllBytes(rawPath);
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!src.LoadImage(bytes))
            {
                Object.DestroyImmediate(src);
                continue;
            }

            int w = src.width, h = src.height;
            Color[] pix = src.GetPixels();
            for (int i = 0; i < pix.Length; i++)
            {
                Color c = pix[i];
                float lum = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                int px = i % w;
                int py = i / w;

                // Subtle edge fade to prevent any border artifact
                float edgeX = Mathf.Min(px, w - 1 - px) / (float)w;
                float edgeY = Mathf.Min(py, h - 1 - py) / (float)h;
                float edgeFade = Mathf.Clamp01(edgeX * 35f) * Mathf.Clamp01(edgeY * 35f);

                // Tight black floor cutoff so pure black background becomes transparent
                float alpha = SmoothEdge(0.045f, 0.20f, lum) * edgeFade;

                // Un-premultiply glow for vibrant luminous text
                if (alpha > 0.005f && lum > 0.001f && alpha < 0.98f)
                {
                    float gain = Mathf.Min(3.0f, 1.0f / Mathf.Max(alpha, 0.35f));
                    c.r = Mathf.Clamp01(c.r * gain);
                    c.g = Mathf.Clamp01(c.g * gain);
                    c.b = Mathf.Clamp01(c.b * gain);
                }
                c.a = alpha;
                pix[i] = c;
            }

            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
            dst.SetPixels(pix);
            dst.Apply();

            string outPath = $"{HdDir}/{outNames[k]}.png";
            File.WriteAllBytes(outPath, dst.EncodeToPNG());
            Object.DestroyImmediate(src);
            Object.DestroyImmediate(dst);

            metas.Add(new PendingSpriteMeta
            {
                path = outPath,
                border = Vector4.zero,
                ppu = 100f,
                comp = TextureImporterCompression.CompressedHQ,
                maxSize = 1024
            });
        }
    }

    static void WriteSpritePng(List<PendingSpriteMeta> metas, string name, Texture2D tex, Vector4 border, float ppu, TextureImporterCompression comp)
    {
        string path = $"{HdDir}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        metas.Add(new PendingSpriteMeta
        {
            path = path,
            border = border,
            ppu = ppu,
            comp = comp,
            maxSize = 512
        });
    }

    delegate Color PixelSampler(float x, float y, float w, float h);

    static Texture2D RenderSupersampled(int w, int h, PixelSampler sampler, int ss = 2)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var cols = new Color[w * h];
        float invSs = 1f / (ss * ss);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color acc = Color.clear;
                for (int sy = 0; sy < ss; sy++)
                {
                    float fy = y + (sy + 0.5f) / ss;
                    for (int sx = 0; sx < ss; sx++)
                    {
                        float fx = x + (sx + 0.5f) / ss;
                        Color s = sampler(fx, fy, w, h);
                        acc.r += s.r * s.a;
                        acc.g += s.g * s.a;
                        acc.b += s.b * s.a;
                        acc.a += s.a;
                    }
                }
                if (acc.a > 0.0001f)
                {
                    acc.r /= acc.a;
                    acc.g /= acc.a;
                    acc.b /= acc.a;
                    acc.a *= invSs;
                }
                else
                {
                    acc = new Color(0f, 0f, 0f, 0f);
                }
                cols[y * w + x] = acc;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    static float SdChamferBox(float px, float py, float hw, float hh, float chamfer)
    {
        float ax = Mathf.Abs(px);
        float ay = Mathf.Abs(py);
        float dRect = Mathf.Max(ax - hw, ay - hh);
        float dDiag = ((ax + ay) - (hw + hh - chamfer)) * 0.70710678f;
        return Mathf.Max(dRect, dDiag);
    }

    static float SdRoundBox(float px, float py, float hw, float hh, float r)
    {
        float qx = Mathf.Abs(px) - (hw - r);
        float qy = Mathf.Abs(py) - (hh - r);
        float mx = Mathf.Max(qx, 0f);
        float my = Mathf.Max(qy, 0f);
        return Mathf.Sqrt(mx * mx + my * my) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
    }

    static float SmoothMask(float d, float aa = 1.2f)
    {
        return Mathf.Clamp01(0.5f - d / aa);
    }

    static float SmoothEdge(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    static Texture2D GenerateModalPanel(int w, int h)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float cx = x - W * 0.5f;
            float cy = y - H * 0.5f;
            float hw = W * 0.5f - 6f;
            float hh = H * 0.5f - 6f;
            float d = SdChamferBox(cx, cy, hw, hh, 18f);
            float ny = y / H;

            if (d > 0f)
            {
                float halo = Mathf.Exp(-d * 0.45f) * 0.38f;
                return new Color(0.18f, 0.85f, 1.0f, halo);
            }

            Color topBg = new Color(0.055f, 0.115f, 0.245f, 0.95f);
            Color botBg = new Color(0.028f, 0.058f, 0.145f, 0.96f);
            Color c = Color.Lerp(botBg, topBg, ny);

            float topSpec = SmoothEdge(0.84f, 0.99f, ny) * 0.08f;
            c.r += topSpec * 0.4f;
            c.g += topSpec * 0.85f;
            c.b += topSpec;

            float innerLine = 1f - Mathf.Clamp01(Mathf.Abs(d + 8.5f) / 1.0f);
            c = Color.Lerp(c, new Color(0.18f, 0.72f, 0.96f, 0.52f), innerLine * 0.55f);

            float rimMask = Mathf.Clamp01(1f - Mathf.Max(0f, -(d + 2.6f)) / 1.3f);
            Color rimCol = Color.Lerp(new Color(0.14f, 0.65f, 0.94f, 1f), new Color(0.45f, 0.95f, 1.0f, 1f), ny * 0.85f + 0.15f);
            c = Color.Lerp(c, rimCol, rimMask);

            float cornerDist = Mathf.Abs(cx) + Mathf.Abs(cy) - (hw + hh - 24f);
            float cornerMask = Mathf.Clamp01(1f - Mathf.Abs(d + 4.2f) / 1.8f) * SmoothEdge(-30f, -6f, cornerDist);
            c = Color.Lerp(c, new Color(0.75f, 0.98f, 1f, 1f), cornerMask * 0.8f);

            c.a *= SmoothMask(d, 1.2f);
            return c;
        });
    }

    static Texture2D GenerateBannerPanel(int w, int h)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float cx = x - W * 0.5f;
            float cy = y - H * 0.5f;
            float hw = W * 0.5f - 5f;
            float hh = H * 0.5f - 5f;
            float d = SdChamferBox(cx, cy, hw, hh, 14f);
            float ny = y / H;

            if (d > 0f)
            {
                float halo = Mathf.Exp(-d * 0.52f) * 0.35f;
                return new Color(0.16f, 0.84f, 1.0f, halo);
            }

            Color c = Color.Lerp(new Color(0.03f, 0.07f, 0.17f, 0.93f), new Color(0.06f, 0.15f, 0.29f, 0.93f), ny);
            float rimMask = Mathf.Clamp01(1f - Mathf.Max(0f, -(d + 2.4f)) / 1.2f);
            Color rimCol = Color.Lerp(new Color(0.15f, 0.68f, 0.92f, 1f), new Color(0.48f, 0.96f, 1.0f, 1f), ny);
            c = Color.Lerp(c, rimCol, rimMask);
            c.a *= SmoothMask(d, 1.1f);
            return c;
        });
    }

    static Texture2D GenerateTintableChamferButton(int w, int h, float chamfer, float borderW, float bodyAlphaBot, float bodyAlphaTop)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float cx = x - W * 0.5f;
            float cy = y - H * 0.5f;
            float hw = W * 0.5f - 4f;
            float hh = H * 0.5f - 4f;
            float d = SdChamferBox(cx, cy, hw, hh, chamfer);
            float ny = y / H;

            if (d > 0f)
            {
                float halo = Mathf.Exp(-d * 0.65f) * 0.25f;
                return new Color(0.95f, 0.99f, 1.0f, halo);
            }

            float shade = Mathf.Lerp(0.76f, 0.96f, ny);
            float topGloss = SmoothEdge(0.55f, 0.95f, ny) * 0.08f;
            float v = Mathf.Clamp01(shade + topGloss);

            float innerLip = Mathf.Clamp01(1f - Mathf.Abs(d + borderW + 1.2f) / 1.2f);
            v *= Mathf.Lerp(1f, 0.88f, innerLip * 0.5f);

            float a = Mathf.Lerp(bodyAlphaBot, bodyAlphaTop, ny);
            Color c = new Color(v, v, v, a);

            float rim = Mathf.Clamp01(1f - Mathf.Max(0f, -(d + borderW)) / 1.2f);
            c = Color.Lerp(c, Color.white, rim);
            c.a *= SmoothMask(d, 1.1f);
            return c;
        });
    }

    static Texture2D GenerateTintableTab(int w, int h)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float cx = x - W * 0.5f;
            float cy = y - H * 0.5f;
            float hw = W * 0.5f - 3f;
            float hh = H * 0.5f - 3f;
            float ax = Mathf.Abs(cx);
            float dRect = Mathf.Max(ax - hw, Mathf.Abs(cy) - hh);
            float topDiag = ((ax + cy) - (hw + hh - 16f)) * 0.70710678f;
            float d = Mathf.Max(dRect, topDiag);
            float ny = y / H;

            if (d > 0f) return Color.clear;

            float v = Mathf.Lerp(0.78f, 0.98f, ny);
            Color c = new Color(v, v, v, 0.95f);
            float rim = Mathf.Clamp01(1f - Mathf.Max(0f, -(d + 2.5f)) / 1.2f);
            c = Color.Lerp(c, Color.white, rim);
            c.a *= SmoothMask(d, 1.1f);
            return c;
        });
    }

    static Texture2D GenerateTintableLevelCard(int w, int h)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float cx = x - W * 0.5f;
            float cy = y - H * 0.5f;
            float hw = W * 0.5f - 5f;
            float hh = H * 0.5f - 5f;
            float d = SdChamferBox(cx, cy, hw, hh, 20f);
            float ny = y / H;

            if (d > 0f)
            {
                float halo = Mathf.Exp(-d * 0.52f) * 0.28f;
                return new Color(0.95f, 0.99f, 1.0f, halo);
            }

            float v = Mathf.Lerp(0.72f, 0.96f, ny);
            if (ny > 0.78f) v = Mathf.Min(1f, v + 0.08f);
            if (ny < 0.25f) v *= 0.88f;

            float divTop = 1f - Mathf.Clamp01(Mathf.Abs(ny - 0.78f) * H / 1.4f);
            float divBot = 1f - Mathf.Clamp01(Mathf.Abs(ny - 0.25f) * H / 1.4f);
            v = Mathf.Lerp(v, 1f, Mathf.Max(divTop, divBot) * 0.45f);

            Color c = new Color(v, v, v, 0.94f);
            float rim = Mathf.Clamp01(1f - Mathf.Max(0f, -(d + 3.0f)) / 1.3f);
            c = Color.Lerp(c, Color.white, rim);
            c.a *= SmoothMask(d, 1.2f);
            return c;
        });
    }

    static Texture2D GenerateTintableHudPad(int w, int h)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float cx = x - W * 0.5f;
            float cy = y - H * 0.5f;
            float hw = W * 0.5f - 5f;
            float hh = H * 0.5f - 5f;
            float d = SdChamferBox(cx, cy, hw, hh, 26f);
            float ny = y / H;

            if (d > 0f)
            {
                float halo = Mathf.Exp(-d * 0.48f) * 0.30f;
                return new Color(0.92f, 0.98f, 1f, halo);
            }

            float v = Mathf.Lerp(0.74f, 0.96f, ny);
            Color c = new Color(v, v, v, Mathf.Lerp(0.72f, 0.88f, ny));

            float rim = Mathf.Clamp01(1f - Mathf.Max(0f, -(d + 4.4f)) / 1.4f);
            c = Color.Lerp(c, Color.white, rim);
            c.a *= SmoothMask(d, 1.2f);
            return c;
        });
    }

    static Texture2D GenerateEditFrame(int w, int h)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float cx = x - W * 0.5f;
            float cy = y - H * 0.5f;
            float hw = W * 0.5f - 8f;
            float hh = H * 0.5f - 8f;
            float d = SdChamferBox(cx, cy, hw, hh, 18f);
            if (d > 0f) return Color.clear;

            float border = Mathf.Clamp01(1f - Mathf.Max(0f, -(d + 4f)) / 1.1f);
            float dash = (Mathf.Sin((Mathf.Abs(cx) + Mathf.Abs(cy)) * 0.24f) > -0.2f) ? 1f : 0.25f;
            float a = Mathf.Max(0.06f, border * dash);
            return new Color(1f, 1f, 1f, a * SmoothMask(d, 1.1f));
        });
    }

    static Texture2D GenerateSliderTrack(int w, int h)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float cx = x - W * 0.5f;
            float cy = y - H * 0.5f;
            float hh = H * 0.5f - 4f;
            float d = SdRoundBox(cx, cy, W * 0.5f - 4f, hh, hh);
            if (d > 0f) return Color.clear;
            float rim = Mathf.Clamp01(1f - Mathf.Max(0f, -(d + 3f)) / 1.4f);
            Color c = Color.Lerp(new Color(0.60f, 0.70f, 0.85f, 0.92f), new Color(0.95f, 0.98f, 1f, 1f), rim);
            c.a *= SmoothMask(d, 1.1f);
            return c;
        });
    }

    static Texture2D GenerateSliderFill(int w, int h)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float cx = x - W * 0.5f;
            float cy = y - H * 0.5f;
            float hh = H * 0.5f - 4f;
            float d = SdRoundBox(cx, cy, W * 0.5f - 4f, hh, hh);
            if (d > 0f) return Color.clear;
            float ny = y / H;
            float v = Mathf.Lerp(0.82f, 1.0f, ny);
            return new Color(v, v, v, SmoothMask(d, 1.1f));
        });
    }

    static Texture2D GenerateSliderThumb(int w, int h)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float cx = (x - W * 0.5f) / (W * 0.5f);
            float cy = (y - H * 0.5f) / (H * 0.5f);
            float r = Mathf.Sqrt(cx * cx + cy * cy);
            if (r > 0.90f) return new Color(0.5f, 0.95f, 1f, Mathf.Exp(-(r - 0.85f) * 6f) * 0.35f);
            float outer = SmoothMask((r - 0.85f) * W * 0.5f, 2f);
            float innerCore = SmoothMask((r - 0.42f) * W * 0.5f, 2f);
            Color ringCol = new Color(0.75f, 0.96f, 1f, 1f);
            Color centerCol = Color.white;
            Color c = Color.Lerp(ringCol, centerCol, innerCore);
            c.a *= outer;
            return c;
        });
    }

    static float SdHeartNorm(float x, float y)
    {
        x = Mathf.Abs(x);
        if (y + x > 1.0f)
        {
            float dx = x - 0.25f;
            float dy = y - 0.75f;
            return Mathf.Sqrt(dx * dx + dy * dy) - 0.35355339f;
        }
        float d1x = x, d1y = y - 1.0f;
        float m = 0.5f * Mathf.Max(x + y, 0.0f);
        float d2x = x - m, d2y = y - m;
        float distSq = Mathf.Min(d1x * d1x + d1y * d1y, d2x * d2x + d2y * d2y);
        return Mathf.Sqrt(distSq) * Mathf.Sign(x - y);
    }

    static Texture2D GenerateHeart(int w, int h, bool full)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float nx = (x / W - 0.5f) * 1.82f;
            float ny = (y / H - 0.16f) * 1.82f;
            float d = SdHeartNorm(nx, ny) * (W / 1.82f);

            if (d > 0f)
            {
                if (!full) return Color.clear;
                float halo = Mathf.Exp(-d * 0.28f) * 0.26f;
                return new Color(1.0f, 0.24f, 0.36f, halo);
            }

            float vGrad = Mathf.Clamp01(y / H);
            if (full)
            {
                Color bot = new Color(0.84f, 0.10f, 0.24f, 1f);
                Color top = new Color(1.00f, 0.34f, 0.44f, 1f);
                Color c = Color.Lerp(bot, top, vGrad);

                float lx = (x / W) - 0.34f;
                float ly = (y / H) - 0.72f;
                float spec = Mathf.Exp(-(lx * lx + ly * ly) * 38f) * 0.52f;
                c = Color.Lerp(c, Color.white, spec);

                float rim = Mathf.Clamp01(1f - Mathf.Max(0f, -(d + 3.2f)) / 1.8f);
                c = Color.Lerp(c, new Color(1f, 0.72f, 0.78f, 1f), rim * 0.75f);
                c.a *= SmoothMask(d, 1.5f);
                return c;
            }
            else
            {
                Color body = new Color(0.18f, 0.22f, 0.34f, 0.34f);
                float rim = Mathf.Clamp01(1f - Mathf.Max(0f, -(d + 5.2f)) / 2f);
                Color rimCol = new Color(0.48f, 0.56f, 0.72f, 0.90f);
                Color c = Color.Lerp(body, rimCol, rim);
                c.a *= SmoothMask(d, 1.5f);
                return c;
            }
        });
    }

    static Texture2D GenerateChevron(int w, int h)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float nx = (x / W - 0.5f) * 2f;
            float ny = (y / H - 0.5f) * 2f;
            float ax = Mathf.Abs(nx);
            float upper = 0.48f - ax * 1.15f;
            float lower = -0.02f - ax * 1.15f;
            float dV = Mathf.Max(ny - upper, lower - ny);
            float dWidth = ax - 0.68f;
            float d = Mathf.Max(dV, dWidth) * (W * 0.5f);
            if (d > 0f)
            {
                float halo = Mathf.Exp(-d * 0.35f) * 0.25f;
                return new Color(0.85f, 0.98f, 1f, halo);
            }
            Color c = Color.Lerp(new Color(0.86f, 0.96f, 1f, 1f), Color.white, Mathf.Clamp01(y / H));
            c.a *= SmoothMask(d, 1.5f);
            return c;
        });
    }

    static Texture2D GeneratePauseIcon(int w, int h)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float cx = x - W * 0.5f;
            float cy = y - H * 0.5f;
            float dLeft = SdRoundBox(cx + W * 0.16f, cy, W * 0.085f, H * 0.28f, W * 0.03f);
            float dRight = SdRoundBox(cx - W * 0.16f, cy, W * 0.085f, H * 0.28f, W * 0.03f);
            float d = Mathf.Min(dLeft, dRight);
            if (d > 0f) return Color.clear;
            return new Color(0.92f, 0.98f, 1f, SmoothMask(d, 1.5f));
        });
    }

    static Texture2D GenerateRing(int w, int h, float rIn, float rOut)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float nx = (x / W - 0.5f) * 2f;
            float ny = (y / H - 0.5f) * 2f;
            float r = Mathf.Sqrt(nx * nx + ny * ny);
            float mid = (rIn + rOut) * 0.5f;
            float halfW = (rOut - rIn) * 0.5f;
            float d = (Mathf.Abs(r - mid) - halfW) * (W * 0.5f);
            if (d > 0f) return Color.clear;
            return new Color(1f, 1f, 1f, SmoothMask(d, 1.5f));
        });
    }

    static Texture2D GenerateCoreOrb(int w, int h)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float nx = (x / W - 0.5f) * 2f;
            float ny = (y / H - 0.5f) * 2f;
            float r = Mathf.Sqrt(nx * nx + ny * ny);
            float d = (r - 0.78f) * (W * 0.5f);
            if (d > 0f)
            {
                float halo = Mathf.Exp(-d * 0.25f) * 0.4f;
                return new Color(1f, 1f, 1f, halo);
            }
            float centerGlow = Mathf.Exp(-r * r * 2.2f);
            Color c = Color.Lerp(new Color(0.75f, 0.92f, 1f, 1f), Color.white, centerGlow);
            c.a *= SmoothMask(d, 1.5f);
            return c;
        });
    }

    static Texture2D GenerateScanReticleIcon(int w, int h)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float nx = (x / W - 0.5f) * 2f;
            float ny = (y / H - 0.5f) * 2f;
            float r = Mathf.Sqrt(nx * nx + ny * ny);
            float angle = Mathf.Atan2(ny, nx);
            float cardinal = Mathf.Min(Mathf.Abs(Mathf.Sin(angle)), Mathf.Abs(Mathf.Cos(angle)));
            float gapMask = (cardinal > 0.24f) ? 1f : 0f;
            float dRing = Mathf.Abs(r - 0.68f) * (W * 0.5f) - 8f;
            float dDot = (r - 0.20f) * (W * 0.5f);
            float aRing = SmoothMask(dRing, 1.5f) * gapMask;
            float aDot = SmoothMask(dDot, 1.5f);
            float a = Mathf.Clamp01(Mathf.Max(aRing, aDot));
            return new Color(0.50f, 0.96f, 1.0f, a);
        });
    }

    static Texture2D GenerateCoreIcon(int w, int h)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float nx = (x / W - 0.5f) * 2f;
            float ny = (y / H - 0.5f) * 2f;
            float d = (Mathf.Abs(nx) + Mathf.Abs(ny) - 0.72f) * (W * 0.5f);
            if (d > 0f)
            {
                float halo = Mathf.Exp(-d * 0.25f) * 0.35f;
                return new Color(0.35f, 1.0f, 0.90f, halo);
            }
            float core = Mathf.Exp(-(nx * nx + ny * ny) * 3.5f);
            Color c = Color.Lerp(new Color(0.25f, 0.96f, 0.86f, 1f), Color.white, core * 0.8f);
            c.a *= SmoothMask(d, 1.5f);
            return c;
        });
    }

    static Texture2D GenerateShieldIcon(int w, int h)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float nx = (x / W - 0.5f) * 2f;
            float ny = (y / H - 0.5f) * 2f;
            float ax = Mathf.Abs(nx);
            float topClip = ny - 0.72f;
            float sideClip = ax - 0.68f;
            float bottomClip = (-ny - 0.78f + ax * 0.95f) * 0.707f;
            float d = Mathf.Max(Mathf.Max(topClip, sideClip), bottomClip) * (W * 0.5f);
            if (d > 0f) return Color.clear;
            float rim = Mathf.Clamp01(1f - Mathf.Max(0f, -(d + 8f)) / 2f);
            Color c = Color.Lerp(new Color(0.24f, 0.75f, 1.0f, 0.85f), new Color(0.75f, 0.97f, 1f, 1f), rim);
            c.a *= SmoothMask(d, 1.5f);
            return c;
        });
    }

    static Texture2D GenerateTitleGlow(int w, int h)
    {
        return RenderSupersampled(w, h, (x, y, W, H) =>
        {
            float nx = (x / W - 0.5f) * 2f;
            float ny = (y / H - 0.5f) * 2f;
            float r2 = nx * nx * 1.15f + ny * ny * 1.85f;
            float a = Mathf.Exp(-r2 * 2.4f) * 0.55f;
            return new Color(0.03f, 0.14f, 0.28f, a);
        }, 1);
    }
}
