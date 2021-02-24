using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TendExt
{
    public class CompTendHediff : ThingComp
    {
        protected CompProperties_TendHediff Props => props as CompProperties_TendHediff;

        public virtual void ApplyHediffs(Pawn pawn, BodyPartRecord part = null)
        {
            foreach (var def in Props.hediffs.Where(def =>
                pawn.health.hediffSet.hediffs.Where(h => h.def == def && (Props.wholeBody || h.Part == part))
                    .Sum(h => h.Severity) <
                Props.maxStacks - Props.initialSeverity))
                pawn.health.AddHediff(MakeHediff(def, pawn, Props.wholeBody ? null : part),
                    Props.wholeBody ? null : part);
        }

        protected virtual Hediff MakeHediff(HediffDef def, Pawn p, BodyPartRecord part)
        {
            var hediff = HediffMaker.MakeHediff(def, p, part);
            hediff.Severity = Props.initialSeverity;
            return hediff;
        }
    }

    // ReSharper disable InconsistentNaming
    public class CompProperties_TendHediff : CompProperties
    {
        public List<HediffDef> hediffs;
        public float initialSeverity = 1f;
        public int maxStacks = int.MaxValue;
        public bool wholeBody;

        public CompProperties_TendHediff()
        {
            compClass = typeof(CompTendHediff);
        }
    }
    // ReSharper restore InconsistentNaming
}