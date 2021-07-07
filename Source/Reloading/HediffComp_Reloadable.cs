﻿// HediffComp_Reloadable.cs by Joshua Bennett
// 
// Created 2021-02-06

using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;

namespace Reloading
{
    public class HediffComp_Reloadable : HediffComp, IReloadable
    {
        public HediffCompProperties_Reloadable Props => props as HediffCompProperties_Reloadable;
        public int ShotsRemaining { get; set; }
        public int ItemsPerShot => Props.ItemsPerShot;
        public virtual ThingDef CurrentProjectile => null;
        public int MaxShots => Props.MaxShots;
        public Thing Thing => parent.pawn;
        public object Parent => parent;

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
            return Props.AmmoFilter.Allows(ammo) && ammo.stackCount >= ItemsPerShot;
        }

        public virtual void Unload()
        {
            var thing = ThingMaker.MakeThing(Props.AmmoFilter.AnyAllowedDef);
            thing.stackCount = ShotsRemaining;
            ShotsRemaining = 0;
            GenPlace.TryPlaceThing(thing, parent.pawn.Position, parent.pawn.Map, ThingPlaceMode.Near);
        }

        public virtual void Notify_ProjectileFired()
        {
            ShotsRemaining--;
        }

        public void ReloadEffect(int curTick, int ticksTillDone)
        {
            if (curTick == ticksTillDone - 2f.SecondsToTicks()) Props.ReloadSound?.PlayOneShot(parent.pawn);
        }

        public ThingDef AmmoExample => Props.AmmoFilter.AnyAllowedDef;

        private int ShotsToReload(Thing ammo)
        {
            return Math.Min(ammo.stackCount / Props.ItemsPerShot, Props.MaxShots - ShotsRemaining);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            var sr = ShotsRemaining;
            Scribe_Values.Look(ref sr, "ShotsRemaining");
            ShotsRemaining = sr;
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            ShotsRemaining = Props.MaxShots;
        }
    }

    public class HediffCompProperties_Reloadable : HediffCompProperties
    {
        public ThingFilter AmmoFilter;
        public int ItemsPerShot;
        public int MaxShots;
        public SoundDef ReloadSound;
        public float ReloadTimePerShot;
        public string VerbLabel;

        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            AmmoFilter.ResolveReferences();
            if (TargetVerb(parentDef) == null) yield return "Cannot find verb to be reloaded.";
            else ReloadingMod.RegisterVerb(TargetVerb(parentDef).verbClass);

            foreach (var e in base.ConfigErrors(parentDef)) yield return e;
        }

        private VerbProperties TargetVerb(HediffDef parent)
        {
            var verbs = parent.CompProps<HediffCompProperties_VerbGiver>().verbs;
            return VerbLabel.NullOrEmpty()
                ? verbs.FirstOrDefault()
                : verbs.FirstOrDefault(v => v.label == VerbLabel);
        }
    }
}