using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ModBase
{
    public interface ICustomRenderer
    {
        void Render(string label, string tooltip, FieldInfo info, object obj, Listing_Standard listing,
            SettingsRenderer renderer);
    }

    public class CustomDefDrawer : ICustomRenderer
    {
        private Def curDef;
        private Vector2 scrollPos = new Vector2(0, 0);
        private string searchBar = "";
        private bool setRect;
        private Rect viewRect;

        public void Render(string label, string tooltip, FieldInfo info, object obj, Listing_Standard listing,
            SettingsRenderer renderer)
        {
            curDef = (Def) info.GetValue(obj);
            var rect = listing.GetRect(300f);
            Widgets.Label(rect.LeftHalf(), label);
            var rect1 = rect.RightHalf();
            searchBar = Widgets.TextField(rect1.TopPartPixels(30f), searchBar);
            var defs = GenDefDatabase.GetAllDefsInDatabaseForDef(info.FieldType);
            var listing1 = new Listing_Standard();
            var rect2 = rect1.BottomPartPixels(270f);
            if (!setRect)
            {
                viewRect = rect2.AtZero();
                setRect = true;
            }

            listing1.BeginScrollView(rect2, ref scrollPos, ref viewRect);
            foreach (var def1 in defs.Where(def =>
                def.label.Contains(searchBar) || def.description.Contains(searchBar) ||
                def.defName.Contains(searchBar)))
            {
                var rect4 = listing1.GetRect(20f);
                if (Widgets.ButtonText(rect4, def1.label))
                {
                    curDef = def1 == curDef ? null : def1;
                    info.SetValue(obj, curDef, renderer);
                }

                TooltipHandler.TipRegion(rect4, def1.description);

                if (def1 == curDef)
                {
                    GUI.color = Color.yellow;
                    Widgets.DrawBox(rect4, 2);
                    GUI.color = Color.white;
                }
            }

            listing1.EndScrollView(ref viewRect);
            TooltipHandler.TipRegion(rect.LeftHalf(), tooltip);
        }
    }

    public abstract class ChooseFromList<T> : ICustomSettingsDraw, IExposable
    {
        private Dictionary<T, bool> state = new Dictionary<T, bool>();
        public float Height => GetOptions().Count * 20f + 20f;

        public void Render(Listing_Standard listing, string label, string tooltip)
        {
            var rect = listing.GetRect(20f);
            Widgets.Label(rect, label);
            TooltipHandler.TipRegion(rect, tooltip);
            foreach (var t in GetOptions())
            {
                var rect2 = listing.GetRect(20f);
                if (Widgets.ButtonText(rect2, Label(t))) state.SetOrAdd(t, !IsEnabled(t));

                GUI.color = IsEnabled(t) ? Color.green : Color.red;
                Widgets.DrawBox(rect2, 2);
                GUI.color = Color.white;
            }
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref state, "state", valueLookMode: LookMode.Value,
                keyLookMode: BaseModSettings.LookModeForType(typeof(T)));
        }

        public virtual bool InitialState(T thing)
        {
            return false;
        }

        public virtual string Label(T thing)
        {
            return thing.ToString();
        }

        public abstract List<T> GetOptions();

        public bool IsEnabled(T thing)
        {
            return state.TryGetValue(thing, out var result) ? result : InitialState(thing);
        }
    }
}