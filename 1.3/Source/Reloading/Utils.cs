using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Reloading
{
    public static class Utils
    {
        public static IEnumerable<IReloadable> AllReloadComps(this Pawn p)
        {
            if (p?.equipment != null)
                foreach (var comp in p.equipment.AllEquipmentListForReading.SelectMany(eq => eq.AllComps))
                    if (comp is IReloadable reloadable)
                        yield return reloadable;
            if (p?.apparel != null)
                foreach (var comp in p.apparel.WornApparel.SelectMany(app => app.AllComps))
                    if (comp is IReloadable reloadable)
                        yield return reloadable;

            if (p?.health?.hediffSet != null)
                foreach (var comp in p.health.hediffSet.hediffs.OfType<HediffWithComps>()
                    .SelectMany(hediff => hediff.comps))
                    if (comp is IReloadable reloadable)
                        yield return reloadable;
        }

        public static IReloadable GetReloadableComp(this Thing thing)
        {
            switch (thing)
            {
                case Pawn p:
                    return p.health?.hediffSet?.hediffs?.OfType<HediffWithComps>()?.SelectMany(hediff => hediff.comps)
                        .OfType<IReloadable>()?.FirstOrDefault();
                case ThingWithComps twc:
                    return twc.AllComps.OfType<IReloadable>().FirstOrDefault();
                default:
                    return thing.TryGetComp<CompReloadable>();
            }
        }
    }
}