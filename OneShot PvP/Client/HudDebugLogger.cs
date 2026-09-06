using System;
using System.Reflection;
using UnityEngine;

namespace OneShotPvP.Client
{
    internal static class HudDebugLogger
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            On.HeroController.Update += OnHeroUpdate;
        }

        private static void OnHeroUpdate(
            On.HeroController.orig_Update orig,
            HeroController self)
        {
            orig(self);

            if (self == null)
            {
                return;
            }

            if (Time.frameCount % 120 != 0)
            {
                return;
            }

            DumpProxyFsm(self);
        }

        private static void DumpProxyFsm(
            HeroController hero)
        {
            try
            {
                Component proxyFsm =
                    FindProxyFsm(hero.gameObject);

                if (proxyFsm == null)
                {
                    Log("ProxyFSM не найден.");
                    return;
                }

                Log(
                    "=== ProxyFSM найден: " +
                    proxyFsm.GetType().FullName +
                    " ==="
                );

                DumpMembers(proxyFsm);
            }
            catch (Exception ex)
            {
                Log(
                    "Ошибка анализа ProxyFSM: " +
                    ex
                );
            }
        }

        private static Component FindProxyFsm(
            GameObject heroObject)
        {
            Component[] components =
                heroObject.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null)
                {
                    continue;
                }

                Type type = component.GetType();

                if (type.Name != "PlayMakerFSM")
                {
                    continue;
                }

                PropertyInfo fsmNameProperty =
                    type.GetProperty(
                        "FsmName",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                if (fsmNameProperty == null)
                {
                    continue;
                }

                object value =
                    fsmNameProperty.GetValue(
                        component,
                        null
                    );

                string fsmName =
                    value as string;

                if (fsmName == "ProxyFSM")
                {
                    return component;
                }
            }

            return null;
        }

        private static void DumpMembers(
            Component proxyFsm)
        {
            Type type =
                proxyFsm.GetType();

            FieldInfo[] fields =
                type.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

            foreach (FieldInfo field in fields)
            {
                object value;

                try
                {
                    value =
                        field.GetValue(
                            proxyFsm
                        );
                }
                catch
                {
                    value = null;
                }

                if (value == null)
                {
                    Log(
                        "FIELD " +
                        field.Name +
                        " = null"
                    );

                    continue;
                }

                Log(
                    "FIELD " +
                    field.Name +
                    " = " +
                    value.GetType().FullName
                );
            }

            PropertyInfo[] properties =
                type.GetProperties(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

            foreach (PropertyInfo property in properties)
            {
                if (!property.CanRead)
                {
                    continue;
                }

                object value;

                try
                {
                    value =
                        property.GetValue(
                            proxyFsm,
                            null
                        );
                }
                catch
                {
                    continue;
                }

                if (value == null)
                {
                    Log(
                        "PROPERTY " +
                        property.Name +
                        " = null"
                    );

                    continue;
                }

                Log(
                    "PROPERTY " +
                    property.Name +
                    " = " +
                    value.GetType().FullName
                );
            }
        }

        private static void Log(
            string message)
        {
            Debug.Log(
                "[OneShotPvP][HudDebug] " +
                message
            );
        }

        public static void Clear()
        {
            _initialized = false;
        }
    }
}