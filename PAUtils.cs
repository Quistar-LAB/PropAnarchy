using ColossalFramework.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace PropAnarchy {
    public static class PAUtils {
        public static Func<S, T> CreateGetter<S, T>(FieldInfo field) {
            string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
            DynamicMethod setterMethod = new DynamicMethod(methodName, typeof(T), new Type[1] { typeof(S) }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            if (field.IsStatic) {
                gen.Emit(OpCodes.Ldsfld, field);
            } else {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, field);
            }
            gen.Emit(OpCodes.Ret);
            return (Func<S, T>)setterMethod.CreateDelegate(typeof(Func<S, T>));
        }

        public static Action<S, T> CreateSetter<S, T>(FieldInfo field) {
            string methodName = field.ReflectedType.FullName + ".set_" + field.Name;
            DynamicMethod setterMethod = new DynamicMethod(methodName, null, new Type[2] { typeof(S), typeof(T) }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            if (field.IsStatic) {
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stsfld, field);
            } else {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stfld, field);
            }
            gen.Emit(OpCodes.Ret);
            return (Action<S, T>)setterMethod.CreateDelegate(typeof(Action<S, T>));
        }

        internal static UITextureAtlas CreateTextureAtlas(string atlasName, string path, string[] spriteNames, int maxSpriteSize) {
            Texture2D texture2D = new Texture2D(maxSpriteSize, maxSpriteSize, TextureFormat.ARGB32, false);
            Texture2D[] textures = new Texture2D[spriteNames.Length];
            for (int i = 0; i < spriteNames.Length; i++) {
                textures[i] = LoadTextureFromAssembly(path + spriteNames[i] + ".png");
            }
            Rect[] regions = texture2D.PackTextures(textures, 2, maxSpriteSize);
            UITextureAtlas uITextureAtlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            Material material = UnityEngine.Object.Instantiate(UIView.GetAView().defaultAtlas.material);
            material.mainTexture = texture2D;
            uITextureAtlas.material = material;
            uITextureAtlas.name = atlasName;
            for (int j = 0; j < spriteNames.Length; j++) {
                UITextureAtlas.SpriteInfo item = new UITextureAtlas.SpriteInfo() {
                    name = spriteNames[j],
                    texture = textures[j],
                    region = regions[j]
                };
                uITextureAtlas.AddSprite(item);
            }
            return uITextureAtlas;
        }

        internal static Texture2D LoadTextureFromAssembly(string filename) {
            UnmanagedMemoryStream s = (UnmanagedMemoryStream)Assembly.GetExecutingAssembly().GetManifestResourceStream(filename);
            byte[] array = new byte[s.Length];
            s.Read(array, 0, array.Length);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(array);
            return texture;
        }

        internal static UITextureAtlas GetAtlas(string name) {
            UITextureAtlas[] atlases = Resources.FindObjectsOfTypeAll(typeof(UITextureAtlas)) as UITextureAtlas[];
            for (int i = 0; i < atlases.Length; i++) {
                if (atlases[i].name == name)
                    return atlases[i];
            }
            return UIView.GetAView().defaultAtlas;
        }

        internal static IEnumerable<CodeInstruction> DebugPatchOutput(IEnumerable<CodeInstruction> instructions, MethodBase method) {
            PAModule.PALog("---- " + method.Name + " ----");
            foreach (var code in instructions) {
                PAModule.PALog(code.ToString());
                yield return code;
            }
            PAModule.PALog("------------------------------------------");
        }
    }
}
