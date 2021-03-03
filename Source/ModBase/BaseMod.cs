using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModBase
{
    public abstract class BaseMod<T> : Mod where T : BaseModSettings, new()
    {
        public static T Settings;
        public Harmony Harm;
        protected SettingsRenderer Renderer;

        protected BaseMod(string id, string defGenerator, ModContentPack content) : base(content)
        {
            Harm = new Harmony(id);
            if (!defGenerator.NullOrEmpty())
                Harm.Patch(AccessTools.Method(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve"),
                    postfix: new HarmonyMethod(GetType(), defGenerator));
            Settings = GetSettings<T>();
            Renderer = new SettingsRenderer(Settings, typeof(T).Namespace);
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                DoPostLoadSetup();
                Settings.Init();
                ApplySettings();
            });
        }

        public virtual void DoPostLoadSetup()
        {
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Renderer.Render(inRect);
        }

        public override string SettingsCategory()
        {
            return Content.Name;
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            ApplySettings();
        }

        public virtual void ApplySettings()
        {
        }
    }
}