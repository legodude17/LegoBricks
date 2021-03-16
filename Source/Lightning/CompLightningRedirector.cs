using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Lightning
{
    [StaticConstructorOnStartup]
    public class CompLightningRedirector : ThingComp
    {
        private static readonly List<CompLightningRedirector> instances = new List<CompLightningRedirector>();

        static CompLightningRedirector()
        {
            var harm = new Harmony("legodude17.lightningredirect");
            harm.Patch(AccessTools.Method(typeof(WeatherEvent_LightningStrike), "FireEvent"),
                new HarmonyMethod(typeof(CompLightningRedirector), "ChangeStrikePos"));
        }

        public static void ChangeStrikePos(WeatherEvent_LightningStrike __instance)
        {
            if (__instance.strikeLoc.IsValid) return;
            var mapInstances = instances.Where(comp => comp.parent.Map == __instance.map).ToList();
            if (mapInstances.Any()) __instance.strikeLoc = mapInstances.RandomElement().parent.Position;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            instances.Add(this);
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            instances.Remove(this);
        }
    }
}