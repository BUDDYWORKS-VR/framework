using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace BUDDYWORKS.AvatarFramework.Importv2
{
    public class AvatarFramework : ScriptableObject
    {
        public const string packageName = "BUDDYWORKS";
        
        public static string[] excludeRegexs =
        {
            ".*\\.cs",
            ".*\\.asmdef",
            "Avatar Framework v1",
            "package.json"
        };

        [MenuItem("BUDDYWORKS/Avatar Framework/Import Framework v2", false, 0)]
        public static void FancyPackage()
        {
            Type instancerType = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => x.GetType("BUDDYWORKS.AvatarFramework.Instancer.Instancer") != null)
                .Select(x => x.GetType("BUDDYWORKS.AvatarFramework.Instancer.Instancer")).FirstOrDefault();

            if (instancerType == null)
            {
                Debug.LogError("Instancer not found. To use this functionality, install the VRLabs Instancer from https://github.com/VRLabs/Instancer");    
                return;
            }

            MethodInfo instanceMethod = instancerType.GetMethod("Instance", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (instanceMethod == null)
            {
                Debug.LogError("Instance method not found");
                return;
            }
            
            var editor = ScriptableObject.CreateInstance<AvatarFramework>();
            var script = MonoScript.FromScriptableObject(editor);
            var assetPath =  (AssetDatabase.GetAssetPath(script));
            
            instanceMethod.Invoke(null, new object[] { packageName, assetPath, excludeRegexs });
        }
    }   
}
