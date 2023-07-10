using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WpfSpLib.Common;

namespace WpfSpLib.Helpers
{
    public static partial class EventHelper
    {
        public static void RemoveWpfEventHandlers(object o)
        {
            RemoveRoutedEventHandlers(o); // routed events
            RemoveDependencyPropertyEventHandlers(o); // dpd.RemoveValueChanged
            RemoveEventSubscriptions(o);
        }

        #region =========  RemovePropertyChangeEventHandlers  ========
        private static Dictionary<Type, Tuple<PropertyInfo, MethodInfo>> _eventHandlersStoreData =
            new Dictionary<Type, Tuple<PropertyInfo, MethodInfo>>
            {
                { typeof(UIElement), new Tuple<PropertyInfo, MethodInfo>(
                    typeof(UIElement).GetProperty("EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic),
                    typeof(UIElement).GetMethod("EventHandlersStoreRemove", BindingFlags.Instance | BindingFlags.NonPublic)) },
                { typeof(UIElement3D), new Tuple<PropertyInfo, MethodInfo>(
                    typeof(UIElement3D).GetProperty("EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic),
                    typeof(UIElement3D).GetMethod("EventHandlersStoreRemove", BindingFlags.Instance | BindingFlags.NonPublic)) },
                { typeof(ContentElement), new Tuple<PropertyInfo, MethodInfo>(
                    typeof(ContentElement).GetProperty("EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic),
                    typeof(ContentElement).GetMethod("EventHandlersStoreRemove", BindingFlags.Instance | BindingFlags.NonPublic)) },
                { typeof(Timeline), new Tuple<PropertyInfo, MethodInfo>(
                    typeof(Timeline).GetProperty("InternalEventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic),
                    typeof(Timeline).GetMethod("RemoveEventHandler", BindingFlags.Instance | BindingFlags.NonPublic)) },
                //{ typeof(UIElement3D), typeof(UIElement3D).GetProperty("EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic) },
                //{ typeof(ContentElement), typeof(ContentElement).GetProperty("EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic) },
                //{ typeof(Timeline), typeof(Timeline).GetProperty("InternalEventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic) },
                //{ typeof(Style), typeof(Style).GetProperty("EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic) },
                //{ typeof(FrameworkTemplate), typeof(FrameworkTemplate).GetProperty("EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic) }
            };
        private static FieldInfo _fiEntriesOfEventHandlersStore;
        private static PropertyInfo _piCountOfEntries;
        private static MethodInfo _miGetKeyValuePairOfEntries;
        private static Type _globalEventManagerType;
        private static FieldInfo _globalIndexToEventMapOfGlobalEventManager;
        private static ArrayList _globalIndexOfEvents;
        // private static MethodInfo _miEventHandlersStoreRemove = typeof(UIElement).GetMethod("EventHandlersStoreRemove", BindingFlags.NonPublic | BindingFlags.Instance);
        // private static MethodInfo _miEventHandlersStoreRemoveOfTimeline = typeof(Timeline).GetMethod("RemoveEventHandler", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo _miToArrayOfFrugalObjectList;
        public static void RemoveRoutedEventHandlers(object o, List<string> log = null)
        {
            if (o == null) return;

            var type = o.GetType();
            var eventHandlersStoreData = _eventHandlersStoreData.Where(kvp=> kvp.Key.IsAssignableFrom(type)).Select(kvp=> kvp.Value).FirstOrDefault();
            if (eventHandlersStoreData == null)
                return;

            var eventHandlersStore = eventHandlersStoreData.Item1.GetValue(o, null);
            if (eventHandlersStore == null) return;

            if (_fiEntriesOfEventHandlersStore == null)
                _fiEntriesOfEventHandlersStore = eventHandlersStore.GetType().GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance);
            var entries = _fiEntriesOfEventHandlersStore.GetValue(eventHandlersStore);

            if (_piCountOfEntries == null)
                _piCountOfEntries= entries.GetType().GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
            var count = (int)_piCountOfEntries.GetValue(entries);

            if(_miGetKeyValuePairOfEntries == null)
                _miGetKeyValuePairOfEntries = entries.GetType().GetMethod("GetKeyValuePair", BindingFlags.Public | BindingFlags.Instance);
            var values = new List<Tuple<int, object>>();
            for (var k = 0; k < count; k++)
            {
                var args = new object[] { k, null, null };
                _miGetKeyValuePairOfEntries.Invoke(entries, args);
                values.Add(new Tuple<int, object>((int)args[1], args[2]));
            }

            foreach (var a1 in values)
            {
                if (a1.Item2.GetType().Name == "FrugalObjectList`1")
                {
                    var eventPrivateKey = GetEventByGlobalIndex(a1.Item1);
                    if (eventPrivateKey is RoutedEvent routedEvent && o is UIElement uiElement)
                    {
                        if (routedEvent.Name != "Unloaded")
                        {
                            if (_miToArrayOfFrugalObjectList == null)
                                _miToArrayOfFrugalObjectList = a1.Item2.GetType().GetMethod("ToArray", BindingFlags.Instance | BindingFlags.Public);
                            var handlerInfos = _miToArrayOfFrugalObjectList.Invoke(a1.Item2, null) as RoutedEventHandlerInfo[];
                            foreach (var handlerInfo in handlerInfos)
                            {
                                if (log != null)
                                    log.Add($"RoutedEvent: {o.GetType().Name}, {(o is FrameworkElement fe ? fe.Name : null)}, {routedEvent.Name}");
                                else
                                {
                                    // Debug.Print($"Remove RoutedEvent: {o.GetType().Name}, {(o is FrameworkElement fe3 ? fe3.Name : null)}, {routedEvent.Name}");
                                    uiElement.RemoveHandler(routedEvent, handlerInfo.Handler);
                                }
                            }
                        }
                    }
                    else
                        throw new NotImplementedException($"RemoveRoutedEventHandlers not implemented yet for FrugalObjectList where EventPrivateKey is " + $" {eventPrivateKey.GetType().Name}");
                }
                /*else if (a1.Item2 is Delegate _delegate)
                {
                    var eventPrivateKey = GetEventByGlobalIndex(a1.Item1);
                    if (eventPrivateKey != null)
                    {
                        //if (o is Timeline)
                          //  _miEventHandlersStoreRemoveOfTimeline.Invoke(o, new[] { eventPrivateKey, a1.Item2 });
                        //else 
                        // Debug.Print($"RemovePropertyChangeEventHandlers2. {o.GetType().Name}, {(o is FrameworkElement fe ? fe.Name : null)}, {_delegate.Method.Name}");
                        if (log!=null)
                            log.Add($"RoutedEvent2: {o.GetType().Name}, {(o is FrameworkElement fe ? fe.Name : null)}, {_delegate.Method.Name}");
                        else
                            eventHandlersStoreData.Item2.Invoke(o, new[] { eventPrivateKey, a1.Item2 });
                    }
                }
                else
                    throw new NotImplementedException($"RemovePropertyChangeEventHandlers not implemented yet for {a1.Item2.GetType().Name}");*/
            }
        }

        private static object GetEventByGlobalIndex(int globalIndex)
        {
            if (_globalIndexOfEvents == null || _globalIndexOfEvents.Count < globalIndex)
            {
                if (_globalEventManagerType == null)
                    _globalEventManagerType = Tips.TryGetType("System.Windows.GlobalEventManager");
                if (_globalEventManagerType == null) return null;

                if (_globalIndexToEventMapOfGlobalEventManager == null)
                    _globalIndexToEventMapOfGlobalEventManager = _globalEventManagerType.GetField("_globalIndexToEventMap", BindingFlags.Static | BindingFlags.NonPublic);
                _globalIndexOfEvents = _globalIndexToEventMapOfGlobalEventManager.GetValue(null) as ArrayList;
            }

            return _globalIndexOfEvents.Count < globalIndex ? null : _globalIndexOfEvents[globalIndex];
        }
        #endregion

        #region =========  RemoveDependencyPropertyEventHandlers  ========
        private static Dictionary<Type, DependencyPropertyDescriptor[]> _dpdOfType = new Dictionary<Type, DependencyPropertyDescriptor[]>();
        private static PropertyInfo _piPropertyOfDpd = typeof(DependencyPropertyDescriptor).GetProperty("Property", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo _fiTrackersOfProperty = null;
        private static FieldInfo _fiChangedHandlerOfTrackers = null;
        public static void RemoveDependencyPropertyEventHandlers(object o, List<string> log = null)
        {
            var type = o.GetType();
            while (type != typeof(Visual) && type != typeof(object))
            {
                foreach (var dpd in GetDependencyPropertyDescriptorsForType(type))
                {
                    var property = _piPropertyOfDpd.GetValue(dpd);
                    if (_fiTrackersOfProperty == null)
                        _fiTrackersOfProperty = property.GetType().GetField("_trackers", BindingFlags.Instance | BindingFlags.NonPublic);
                    var trackers = _fiTrackersOfProperty.GetValue(property) as IDictionary;
                    if (trackers != null && trackers.Contains(o))
                    {
                        if (log != null)
                            log.Add($"DPD: {o.GetType().Name}, {dpd.Name}");
                        else
                        {
                            // Debug.Print($"RemoveDPD: {o.GetType().Name}, {dpd.Name}");
                            var tracker = trackers[o];
                            if (_fiChangedHandlerOfTrackers == null)
                                _fiChangedHandlerOfTrackers = tracker.GetType().GetField("Changed", BindingFlags.Instance | BindingFlags.NonPublic);
                            var changed = _fiChangedHandlerOfTrackers.GetValue(tracker) as EventHandler;
                            dpd.RemoveValueChanged(o, changed);
                        }
                    }
                }
                type = type.BaseType;
            }
        }
        private static DependencyPropertyDescriptor[] GetDependencyPropertyDescriptorsForType(Type type)
        {
            // if (type == typeof(Typography) || type == typeof(SpellCheck)) return new DependencyPropertyDescriptor[0];

            if (!_dpdOfType.ContainsKey(type))
                _dpdOfType[type] = type.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => f.FieldType == typeof(DependencyProperty))
                    .Select(fieldInfo => DependencyPropertyDescriptor.FromProperty(fieldInfo.GetValue(null) as DependencyProperty, type)).ToArray();
            return _dpdOfType[type];
        }
        #endregion

        #region =========  RemoveSimpleEventSubsriptions  ========
        // Based on my old code (2010 year): VS2005, Windows Form
        private static Dictionary<Type, Tuple<EventInfo, FieldInfo>[]> _simpleIventInfoCache = new Dictionary<Type, Tuple<EventInfo, FieldInfo>[]>();
        public static void RemoveEventSubscriptions(object target, List<string> log = null)
        {
            var type = target.GetType();
            if (!_simpleIventInfoCache.ContainsKey(type))
            {
                _simpleIventInfoCache.Add(type,
                    type.GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(ei =>
                            new Tuple<EventInfo, FieldInfo>(ei,
                                ei.DeclaringType?.GetField(ei.Name,
                                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)))
                        .Where(a => a.Item2 != null).ToArray());
            }

            var eis = _simpleIventInfoCache[type];
            foreach (var ei in eis)
            {
                if (ei.Item2.GetValue(target) is Delegate handler)
                {
                    var miRemove = ei.Item1.GetRemoveMethod() ?? ei.Item1.GetRemoveMethod(true);
                    if (log != null)
                    {
                        if (ei.Item1.EventHandlerType.Name != "HighlightChangedEventHandler")
                            log.Add($"Delegate: {target.GetType()}, {ei.Item2.Name}");
                    }
                    else
                    {
                        // Debug.Print($"RemoveDelegates: {target.GetType().Name}, {ei.Item2.Name}");
                        miRemove.Invoke(target, new object[] {handler});
                    }

                    /*foreach (var d in handler.GetInvocationList())
                    {
                        // string s = d.Method.Name;
                        // Debug.Print($"RemoveDelegates: {target.GetType()}, {s}, {ei.Name}");
                        miRemove.Invoke(target, new object[] { d });
                        // ei.RemoveEventHandler(target, d);
                    }*/
                }
            }
        }
        #endregion
        private static string GetName(object d)
        {
            return d is FrameworkElement fe ? fe.Name : null;
        }
    }
}
