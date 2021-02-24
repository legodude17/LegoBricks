using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ModBase
{
    [StaticConstructorOnStartup]
    public class SettingsRenderer
    {
        // ReSharper disable once InconsistentNaming
        public static bool __DEBUG = false;

        private static readonly Dictionary<Type, Type> CUSTOM_DRAWERS = new Dictionary<Type, Type>();

        private readonly Dictionary<string, string> buffers = new Dictionary<string, string>();

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        internal readonly object Settings;
        private string curTab = "";
        private Dictionary<FieldInfo, ICustomRenderer> customFields;
        private FieldInfo[] drawFields;
        private Dictionary<string, FieldInfo> fieldInfos;
        private string[] keys;
        private Listing_Standard listingStandard;
        private string mainTabName;
        public bool Ready;
        private Dictionary<string, SettingsRenderer> renderers;
        private Vector2 scrollPos = new Vector2(0, 0);
        private bool setRect;
        internal Dictionary<FieldInfo, MethodInfo> Setters;
        private SettingsRenderer[] subSettings;
        private List<TabRecord> tabs;
        private string title;
        private Rect viewRect;

        static SettingsRenderer()
        {
            AddCustomDrawer(typeof(Def), typeof(CustomDefDrawer));
        }

        public SettingsRenderer(object settings, string name = "")
        {
            Settings = settings;
            title = name;
        }

        private float Height => keys.Length * 20f + subSettings.Sum(ss => ss.Height);

        public static void AddCustomDrawer(Type drawee, Type drawer)
        {
            if (!typeof(ICustomRenderer).IsAssignableFrom(drawer))
            {
                Log.Error(
                    "[LegoBricks] [Settings] Tried to register custom drawer that does not implement required interface.");
                return;
            }

            CUSTOM_DRAWERS.Add(drawee, drawer);
        }

        public void Init()
        {
            if (Settings is INamedSettings ns) title = ns.Name;
            var type = Settings.GetType();
            Debug("Settings is of type: " + type.Name);
            if (Settings is IEnumerable<object> list)
            {
                Debug("Settings is Enumerable!");
                var objects = list as object[] ?? list.ToArray();
                var srs = objects.OfType<INamedSettings>().Select(obj => new SettingsRenderer(obj.Name))
                    .ToArray();
                Debug("Created " + srs.Length + " renderers!");
                tabs = srs.Select(sr =>
                    new TabRecord(sr.title, () => curTab = sr.title, false)
                    {
                        selectedGetter = () => curTab == sr.title
                    }).ToList();
                Debug("Created " + tabs.Count + " tabs!");
                renderers = tabs.Select(tab => tab.label)
                    .ToDictionary(srs);
                Debug("Created dictionary!");
                mainTabName = title.NullOrEmpty() ? "Main" : title;
                var mainTab = new TabRecord(mainTabName, () => curTab = mainTabName, true)
                {
                    selectedGetter = () => curTab == mainTabName
                };
                curTab = mainTabName;
                Debug("Created mainTab!");
                tabs.Insert(0, mainTab);
                Debug("Inserted main tab!");
                foreach (var sr in srs)
                {
                    Debug("Initializing tab!");
                    sr.Init();
                }
            }
            else
            {
                mainTabName = title.NullOrEmpty() ? "Main" : title;
                curTab = mainTabName;
            }

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            Setters = fields.Where(info => info.HasAttribute<SettingsSetterAttribute>()).ToDictionary(info => info,
                info => type.GetMethod(info.TryGetAttribute<SettingsSetterAttribute>().MethodName,
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static));
            Debug("Found " + fields.Length + " fields");
            foreach (var fieldInfo in fields)
                Debug("  " + fieldInfo.Name + " (" + fieldInfo.FieldType.Name + ")");
            fieldInfos = fields.ToDictionary(field => field.Name, field => field);
            Debug("Created types dictionary!");
            drawFields = fields.Where(field => typeof(ICustomSettingsDraw).IsAssignableFrom(field.FieldType))
                .ToArray();
            Debug("Found " + drawFields.Length + " drawFields");
            fields = fields.Except(drawFields).ToArray();
            var valueFields = fields.Where(field =>
                field.FieldType.IsValueType || field.FieldType.IsEnum || typeof(string).IsAssignableFrom(
                    field.FieldType)).ToArray();
            Debug("Found " + valueFields.Length + " valueFields");
            fields = fields.Except(valueFields).ToArray();
            var classFields =
                fields.Where(field => field.FieldType.IsClass && !field.FieldType.IsEnum).ToArray();
            customFields = classFields
                .Where(info => CUSTOM_DRAWERS.Keys.Any(type1 => type1.IsAssignableFrom(info.FieldType))).ToDictionary(
                    info => info, info => (ICustomRenderer) Activator.CreateInstance(CUSTOM_DRAWERS.First(kv =>
                        kv.Key.IsAssignableFrom(info.FieldType)).Value));
            Debug("Found " + customFields.Count + " custom drawing fields");
            subSettings = classFields.Except(customFields.Keys)
                .Select(field => new SettingsRenderer(field.GetValue(Settings), field.Name))
                .ToArray();
            Debug("Found " + subSettings.Length + " subSettings");
            foreach (var setting in subSettings)
            {
                Debug("Initializing sub setting");
                setting.Init();
            }

            fields = fields.Except(valueFields).Except(classFields).ToArray();
            fields.ToList().ForEach(field =>
                Log.Error(field.Name + " is not a class or value! Type is " + field.FieldType.Name));
            keys = valueFields.Select(field => field.Name).ToArray();
            listingStandard = new Listing_Standard();
            Ready = true;
        }

        public void Render(Rect inRect)
        {
            if (!Ready) Init();
            RenderInternal(inRect, listingStandard);
        }

        private void RenderInternal(Rect inRect, Listing_Standard listing)
        {
            if (tabs != null) TabDrawer.DrawTabs(inRect, tabs);
            Debug("curTab is " + curTab + " and ready is " + Ready);
            if (curTab == mainTabName)
            {
                if (!setRect)
                {
                    viewRect = inRect.AtZero();
                    setRect = true;
                }

                listing.BeginScrollView(inRect, ref scrollPos, ref viewRect);
                Debug("Begun scroll view");

                foreach (var field in drawFields)
                    if (field.GetValue(Settings) is ICustomSettingsDraw icsd)
                        icsd.Render(listing.GetRect(icsd.Height), listing);
                    else
                        Log.ErrorOnce("Field " + field.Name + " is not ICustomSettingsDraw", field.Name.GetHashCode());

                Debug("Finished drawing custom fields");

                foreach (var key in keys) RenderValue(key, listing);

                Debug("Finished drawing keys");

                foreach (var subSetting in subSettings)
                {
                    var listing2 = listing.BeginSection_NewTemp(subSetting.Height);
                    var rect = listing.GetRect(subSetting.Height);
                    if (!subSetting.title.NullOrEmpty())
                    {
                        Debug("Drawing subSettings " + subSetting.title);
                        Text.Font = GameFont.Medium;
                        listing2.Label(subSetting.title);
                        Text.Font = GameFont.Small;
                    }

                    subSetting.RenderInternal(rect, listing2);
                    listing.EndSection(listing2);
                }

                Debug("Finished drawing subSettings");

                foreach (var kv in customFields)
                    kv.Value.Render(SettingLabel(kv.Key.Name, kv.Key), Tooltip(kv.Key.Name, kv.Key), kv.Key, Settings,
                        listing, this);

                Debug("Finished drawing custom renderers");

                listing.EndScrollView(ref inRect);
            }
            else
            {
                renderers[curTab].RenderInternal(inRect, listing);
            }
        }

        private void RenderValue(string key, Listing_Standard listing)
        {
            Debug("Rendering value " + key);
            var info = fieldInfos[key];
            Debug("Found info: " + info.Name + " on " + info.DeclaringType?.Name + " in " +
                  info.DeclaringType?.Namespace);
            var type = info.FieldType;
            var label = SettingLabel(key, info);
            var curValue = info.GetValue(Settings);
            if (type.IsEnum)
            {
                var names = type.GetEnumNames();
                foreach (var name in names) Debug("Found name: " + name);
                var values = names.ToDictionary(type.GetEnumValues().OfType<object>());
                foreach (var kv in values) Debug(kv.Key + " is of type " + kv.Value);
                if (listing.ButtonTextLabeled(label, curValue.ToString()))
                    Find.WindowStack.Add(new FloatMenu(names.Select(name =>
                        new FloatMenuOption(name, () => info.SetValue(Settings, values[name], this))).ToList()));
            }

            if (type == typeof(string))
            {
                Debug("Found string!");
                info.SetValue(Settings, listing.TextEntryLabeled(label, curValue?.ToString() ?? ""), this);
            }
            else if (type == typeof(bool))
            {
                Debug("Found bool!");
                var temp = (bool) curValue;
                listing.CheckboxLabeled(label, ref temp, Tooltip(key, info));
                info.SetValue(Settings, temp, this);
            }
            else if (type == typeof(int))
            {
                Debug("Found int!");
                var num = (int) curValue;
                buffers.TryGetValue(key, out var buffer);
                buffer = buffer ?? "";
                listing.TextFieldNumericLabeled(label, ref num, ref buffer);
                info.SetValue(Settings, num, this);
                buffers.SetOrAdd(key, buffer);
            }
            else if (type == typeof(float))
            {
                Debug("Found float!");
                var num = (float) curValue;
                buffers.TryGetValue(key, out var buffer);
                buffer = buffer ?? "";
                listing.TextFieldNumericLabeled(label, ref num, ref buffer);
                info.SetValue(Settings, num, this);
                buffers.SetOrAdd(key, buffer);
            }
        }

        private string SettingLabel(string key, FieldInfo info)
        {
            return (title + "." + key + ".Label").TryTranslate(out var result) ? (string) result : key;
        }

        private string Tooltip(string key, FieldInfo info)
        {
            return (title + "." + key + ".Tooltip").TryTranslate(out var result) ? result : null;
        }

        private static void Debug(string message)
        {
            if (__DEBUG) Log.Message(message);
        }
    }

    public interface ICustomSettingsDraw
    {
        float Height { get; }
        void Render(Rect inRect, Listing_Standard listing);
    }

    public interface INamedSettings
    {
        string Name { get; }
    }

    public static class Util
    {
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            return source.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IEnumerable<TKey> source, IEnumerable<TValue> other)
        {
            return source.Zip(other, (key, value) => new KeyValuePair<TKey, TValue>(key, value))
                .ToDictionary();
        }

        public static void SetValue(this FieldInfo info, object obj, object val, SettingsRenderer renderer = null)
        {
            if (renderer != null && renderer.Setters.ContainsKey(info))
                renderer.Setters[info].Invoke(renderer.Settings, new[] {obj, info, val});
            else
                info.SetValue(obj, val);
        }
    }
}