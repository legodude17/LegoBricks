using HarmonyLib;
using ModBase;
using UnityEngine;
using Verse;

namespace Setttings
{
    public abstract class BaseMod<T> : Mod where T : ModSettings, new()
    {
        public static T Settings;
        public Harmony Harm;
        protected SettingsRenderer Renderer;

        public BaseMod(ModContentPack content) : base(content)
        {
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                Harm = new Harmony(Id);
                DoPatches();
                Log.Message("Applied patches for " + Harm.Id);
                InitSettings();
                Log.Message("Initialized settings for " + Id);
            });
        }

        public virtual string Id => Content.PackageId;

        public abstract void DoPatches();

        public virtual void InitSettings()
        {
            Settings = GetSettings<T>();
            Renderer = new SettingsRenderer(Settings, typeof(T).Namespace);
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
    }
}