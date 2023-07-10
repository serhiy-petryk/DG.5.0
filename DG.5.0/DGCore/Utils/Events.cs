using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace DGCore.Utils {
  public static class Events {

    public static void RemoveAllEventSubscriptions(object target) {
      RemoveEventSubscriptions(target, null);
    }
    public static void RemoveEventSubscriptions(object target, object subscriber) {
      EventInfo[] eis = target.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      foreach (EventInfo ei in eis) {
        RemoveDelegates(ei, target, subscriber);
      }
      PropertyInfo[] pis = target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      foreach (PropertyInfo pi in pis) {
        if (pi.PropertyType == typeof(EventHandlerList) || pi.PropertyType.IsSubclassOf(typeof(EventHandlerList))) {
          EventHandlerList ehl = (EventHandlerList)pi.GetValue(target, null);
          RemoveDelegates(ehl, subscriber);
        }
      }
      FieldInfo[] fis = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      foreach (FieldInfo fi in fis) {
        if (fi.FieldType == typeof(EventHandlerList) || fi.FieldType.IsSubclassOf(typeof(EventHandlerList))) {
          EventHandlerList ehl = (EventHandlerList)fi.GetValue(target);
          RemoveDelegates(ehl, subscriber);
        }
      }
    }

    //==============  Private section  ==============
    private static FieldInfo fiHandler;
    private static FieldInfo fiNext;
    private static FieldInfo fiKey;
    private static void RemoveDelegates(EventHandlerList ehl, object subscriber) {
      if (subscriber == null && false) {// Remove all events
        ehl.Dispose();
        return;
      }
      List<Delegate> delegates = new List<Delegate>();
      List<object> keys = new List<object>();
      if (ehl != null) {
        FieldInfo fiHead = typeof(EventHandlerList).GetField("_head", BindingFlags.Instance | BindingFlags.NonPublic);
        object head = fiHead.GetValue(ehl);
        if (fiHandler == null) {
          fiHandler = head.GetType().GetField("_handler", BindingFlags.Instance | BindingFlags.NonPublic);
          fiNext = head.GetType().GetField("_next", BindingFlags.Instance | BindingFlags.NonPublic);
          fiKey = head.GetType().GetField("_key", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        //    FieldInfo fiHandler = head.GetType().GetField("handler", BindingFlags.Instance | BindingFlags.NonPublic);
        //  FieldInfo fiNext = head.GetType().GetField("next", BindingFlags.Instance | BindingFlags.NonPublic);
        while (head != null) {
          Delegate d = (Delegate)fiHandler.GetValue(head);
          if (d != null) {
            delegates.Add(d);
            keys.Add(fiKey.GetValue(head));
          }
          head = fiNext.GetValue(head);
        }
      }
      if (subscriber == null) {// delete all events of target
        for (int i =0; i<delegates.Count; i++) {
          string s = delegates[i].Method.Name;
          ehl.RemoveHandler(keys[i], delegates[i]);
        }
      }
      else {
        for (int i = 0; i < delegates.Count; i++) {
          string s = delegates[i].Method.Name;
          if (delegates[i].Target == subscriber) {
            ehl.RemoveHandler(keys[i], delegates[i]);
          }
        }
      }
    }

    private static void RemoveDelegates(EventInfo ei, object target, object subcriber) {
      FieldInfo fi = ei.DeclaringType.GetField(ei.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
      if (fi == null) {
        if (ei.DeclaringType == typeof(Component)) {
        }
      }
      if (fi != null) {
        Delegate handler = (Delegate)fi.GetValue(target);
        if (handler != null) {
          Delegate[] dd = handler.GetInvocationList();
          if (subcriber == null) {
            foreach (Delegate d in dd) {
              string s = d.Method.Name;
              ei.RemoveEventHandler(target, d);
            }
          }
          else {
            foreach (Delegate d in dd) {
              string s = d.Method.Name;
              if (d.Target == subcriber) ei.RemoveEventHandler(target, d);
            }
          }
        }
      }
    }

  }
}
