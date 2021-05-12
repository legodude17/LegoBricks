using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Lightning
{
    [StaticConstructorOnStartup]
    public class CompArmorChange : ThingComp
    {
        static CompArmorChange()
        {
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                ThingDef.Named("Apparel_PowerArmor").comps.Add(new CompProperties(typeof(CompArmorChange)));
            });
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            return base.CompGetWornGizmosExtra().Append(new Command_Action
            {
                defaultLabel = "Switch",
                icon = parent.def.uiIcon,
                action = () => Find.WindowStack.Add(new FloatMenu(DefDatabase<ThingDef>.AllDefs
                    .Where(def => def.IsApparel)
                    .Select(def => new FloatMenuOption(def.LabelCap, () =>
                    {
                        parent.def = def;
                        parent.graphicInt = null;
                        if (parent is Apparel apparel) apparel.Wearer.drawer.renderer.graphics.ResolveApparelGraphics();
                    })).ToList()))
            });
        }
    }
}