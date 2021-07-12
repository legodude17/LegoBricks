// BaseModSettings.cs by Joshua Bennett
// 
// Created 2021-02-24

using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace ModBase
{
    public class BaseModSettings : ModSettings
    {
        public virtual bool CustomSave(string name)
        {
            return false;
        }

        public virtual void Init()
        {
        }

        public virtual object DefaultValue(FieldInfo info)
        {
            if (info.HasAttribute<DefaultAttribute>())
            {
                var attr = info.TryGetAttribute<DefaultAttribute>();
                if (attr.Static != null) return attr.Static;
                if (!attr.Getter.NullOrEmpty())
                    return AccessTools.Method(GetType(), attr.Getter)
                        .Invoke(this, attr.GetterWantsObject ? new object[] {this} : null);
                if (!attr.Field.NullOrEmpty()) return AccessTools.Field(GetType(), attr.Field).GetValue(this);
            }

            return null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            foreach (var field in GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => !f.HasAttribute<UnsavedAttribute>()))
            {
                if (CustomSave(field.Name)) continue;
                var val = field.GetValue(this);
                var lookMode = LookModeForType(field.FieldType);
                var type = field.FieldType;
                Scribe_Universal.Look(ref val, field.Name, ref lookMode, ref type);
                field.SetValue(this, val);
            }
        }

        public static LookMode LookModeForType(Type t)
        {
            if (t == null) return LookMode.Undefined;
            if (t.IsValueType || typeof(string).IsAssignableFrom(t) ||
                t.IsEnum)
                return LookMode.Value;
            if (typeof(IExposable).IsAssignableFrom(t))
                return LookMode.Deep;
            if (typeof(Def).IsAssignableFrom(t))
                return LookMode.Def;
            if (typeof(GlobalTargetInfo).IsAssignableFrom(t)) return LookMode.GlobalTargetInfo;
            if (typeof(LocalTargetInfo).IsAssignableFrom(t)) return LookMode.LocalTargetInfo;
            if (typeof(TargetInfo).IsAssignableFrom(t)) return LookMode.TargetInfo;

            return LookMode.Reference;
        }
    }
}