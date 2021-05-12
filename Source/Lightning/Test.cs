using HarmonyLib;
using UnityEngine;
using Verse;

namespace Lightning
{
    [StaticConstructorOnStartup]
    public class Test
    {
        private static readonly float scale = 0.5f;

        static Test()
        {
            var harm = new Harmony("legodude17.testing");
            harm.Patch(AccessTools.Method(typeof(Widgets), "ButtonTextWorker"),
                new HarmonyMethod(typeof(Test), "Pre"), new HarmonyMethod(typeof(Test), "Post"));
        }

        public static void Pre(out float __state, ref Rect rect, string label)
        {
            __state = Prefs.UIScale;
            GUI.Box(rect, label);
            Prefs.UIScale = scale;
            UI.ApplyUIScale();
        }

        public static void Post(float __state)
        {
            Prefs.UIScale = __state;
            UI.ApplyUIScale();
        }
    }
}