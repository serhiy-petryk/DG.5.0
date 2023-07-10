using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WpfSpLib.Helpers
{
    public static partial class EventHelper
    {
        public static List<string> LogEvent(object o, int maxLevel, int currentLevel = 0, string prefix = null)
        {
            var log = new List<string>();
            RemoveRoutedEventHandlers(o, log); // routed events
            RemoveDependencyPropertyEventHandlers(o, log); // dpd.RemoveValueChanged
            RemoveEventSubscriptions(o, log);
            var localLog = new List<string>();
            var pis = o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).ToArray();
            // foreach (var pi in pis.Where(p => !p.PropertyType.IsValueType))
            if (!(o is BitmapMetadata))
            {
                foreach (var pi in pis)
                {
                    if (pi.Name != "TempDefinitions" && pi.Name != "DefinitionIndices" && pi.Name != "RoundingErrors" &&
                        pi.Name != "WorkAreaBoundsForNearestMonitor" && pi.Name != "WindowSize" &&
                        pi.Name != "Current" && pi.Name != "Item" && pi.Name != "PrefixLanguage" &&
                        pi.Name != "System.Collections.IList.Item" &&
                        pi.Name != "System.Collections.IEnumerator.Current"
                        && pi.Name != "Lines" && pi.Name != "Handle" && pi.Name != "ParentHandle" &&
                        pi.Name != "System.Collections.ICollection.SyncRoot"
                        && pi.Name != "System.Collections.Generic.IList<T>.Item" &&
                        pi.Name != "System.Collections.IDictionary.Item" && pi.Name != "Stack")
                    {
                        var o2 = pi.GetValue(o);
                        if (o2 != null && !(o2 is Dispatcher))
                        {
                            RemoveRoutedEventHandlers(o2, localLog); // routed events
                            RemoveDependencyPropertyEventHandlers(o2, localLog); // dpd.RemoveValueChanged
                            RemoveEventSubscriptions(o2, localLog);
                            var piType = o2.GetType();
                            if (maxLevel > currentLevel && !piType.IsValueType && piType != typeof(string) && piType.Name != "RuntimeType")
                                LogEvent(o2, maxLevel, currentLevel + 1);
                        }

                        if (o2 is ResourceDictionary rd)
                        {
                            if (rd.Count != 1)
                            {

                            }
                            LogResources(rd, maxLevel, currentLevel);
                        }
                    }
                }

                var fis = o.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .ToArray();
                foreach (var fi in fis.Where(p => !p.FieldType.IsValueType))
                {
                    var o2 = fi.GetValue(o);
                    if (o2 != null && !(o2 is Dispatcher))
                    {
                        RemoveRoutedEventHandlers(o2, localLog); // routed events
                        RemoveDependencyPropertyEventHandlers(o2, localLog); // dpd.RemoveValueChanged
                        RemoveEventSubscriptions(o2, localLog);
                        var piType = o2.GetType();
                        if (maxLevel > currentLevel && !piType.IsValueType && piType != typeof(string) &&
                            piType.Name != "RuntimeType")
                            LogEvent(o2, maxLevel, currentLevel + 1);
                    }
                }
            }

            if (localLog.Count > 0)
            {
                log.Add($"{prefix} PROPERTIES. {o.GetType().Name}");
                log.AddRange(localLog);
            }

            return log;
        }

        public static void LogResources(ResourceDictionary rd, int maxLevel, int currentLevel)
        {
            foreach (var child in rd.MergedDictionaries)
                LogResources(child, maxLevel, currentLevel);

            foreach (var value in rd.Values)
            {
                if (value != null) 
                    LogEvent(value, maxLevel, currentLevel, "RD");
            }
        }

    }
}
