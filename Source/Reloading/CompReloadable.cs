using System;
using Verse;
using Verse.Sound;

namespace Reloading
{
    public class CompReloadable : ThingComp, IReloadable
    {
        public CompProperties_Reloadable Props => props as CompProperties_Reloadable;
        public int ShotsRemaining { get; set; }
        public int ItemsPerShot => Props.ItemsPerShot;
        public int MaxShots => Props.MaxShots;
        public Thing Thing => parent;
        public ThingDef AmmoExample => Props.AmmoFilter.AnyAllowedDef;
        public object Parent => parent;

        public virtual ThingDef CurrentProjectile => null;

        public virtual Thing Reload(Thing ammo)
        {
            if (!CanReloadFrom(ammo)) return null;
            var shotsToFill = ShotsToReload(ammo);
            ShotsRemaining += shotsToFill;
            return ammo.SplitOff(shotsToFill * ItemsPerShot);
        }

        public virtual int ReloadTicks(Thing ammo)
        {
            return ammo == null ? 0 : (Props.ReloadTimePerShot * ShotsToReload(ammo)).SecondsToTicks();
        }

        public virtual bool NeedsReload()
        {
            return ShotsRemaining < MaxShots;
        }

        public virtual bool CanReloadFrom(Thing ammo)
        {
            // Log.Message(ammo + " x" + ammo.stackCount);
            if (ammo == null) return false;
            return Props.AmmoFilter.Allows(ammo) && ammo.stackCount >= Props.ItemsPerShot;
        }

        public virtual void Unload()
        {
            var thing = ThingMaker.MakeThing(Props.AmmoFilter.AnyAllowedDef);
            thing.stackCount = ShotsRemaining;
            ShotsRemaining = 0;
            GenPlace.TryPlaceThing(thing, parent.Position, parent.Map, ThingPlaceMode.Near);
        }

        public virtual void Notify_ProjectileFired()
        {
            ShotsRemaining--;
        }

        public void ReloadEffect(int curTick, int ticksTillDone)
        {
            if (curTick == ticksTillDone - 2f.SecondsToTicks()) Props.ReloadSound?.PlayOneShot(parent);
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            ShotsRemaining = Props.MaxShots;
        }

        private int ShotsToReload(Thing ammo)
        {
            return Math.Min(ammo.stackCount / ItemsPerShot, MaxShots - ShotsRemaining);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            var sr = ShotsRemaining;
            Scribe_Values.Look(ref sr, "ShotsRemaining");
            ShotsRemaining = sr;
        }

        public override string CompInspectStringExtra()
        {
            return base.CompInspectStringExtra() + (ShotsRemaining == 0
                ? "Reloading.NoAmmo".Translate()
                : "Reloading.Ammo".Translate(ShotsRemaining, Props.MaxShots));
        }
    }

    public class CompProperties_Reloadable : CompProperties
    {
        public ThingFilter AmmoFilter;
        public int ItemsPerShot;
        public int MaxShots;
        public SoundDef ReloadSound;
        public float ReloadTimePerShot;

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            AmmoFilter.ResolveReferences();
        }
    }
}