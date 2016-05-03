using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Verse;

namespace PSI
{
    internal class Materials
    {
        private Material[] data = new Material[21];
        public readonly string matLibName;

        public Material this[Icons icon]
        {
            get
            {
                return data[(int)icon];
            }
        }

        public Materials(string matLib = "default")
        {
            matLibName = matLib;
        }

        public Material loadIconMat(string path, bool smooth = false)
        {
            var tex = ContentFinder<Texture2D>.Get("UI/Overlays/PawnStateIcons/" + path, false);
            Material material;
            if ((UnityEngine.Object)tex == (UnityEngine.Object)null)
            {
                material = (Material)null;
            }
            else
            {
                if (smooth)
                {
                    tex.filterMode = FilterMode.Trilinear;
                    tex.mipMapBias = -0.5f;
                    tex.anisoLevel = 9;
                    tex.wrapMode = TextureWrapMode.Repeat;
                    tex.Apply();
                    tex.Compress(true);
                }
                else
                {
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Repeat;
                    tex.Apply();
                    tex.Compress(true);
                }
                material = MaterialPool.MatFrom(new MaterialRequest(tex, ShaderDatabase.MetaOverlay));
            }
            return material;
        }

        public void reloadTextures(bool smooth = false)
        {
            foreach (var icons in ((IEnumerable)Enum.GetValues(typeof(Icons))).Cast<Icons>())
            {
                switch (icons)
                {
                    case Icons.None:
                    case Icons.Length:
                        continue;
                    default:
                        var path = matLibName + "/" + Enum.GetName(typeof(Icons), (object)icons);
                        data[(int)icons] = loadIconMat(path, smooth);
                        continue;
                }
            }
        }
    }
}
