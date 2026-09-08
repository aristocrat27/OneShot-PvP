using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using MonoMod.RuntimeDetour;

using OneShotPvP.Server;

namespace OneShotPvP.Settings
{
    internal static class HkmpGlobalSettingsSaveGuard
    {
        private static Hook _hook;

        private static Type _hkmpModType;
        private static Type _modSettingsType;

        private static MethodInfo _onSaveGlobalMethod;
        private static MethodInfo _memberwiseCloneMethod;

        private static PropertyInfo _serverSettingsProperty;

        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                Assembly hkmpAssembly =
                    FindHkmpAssembly();

                if (hkmpAssembly == null)
                {
                    Modding.Logger.Log(
                        "[OneShotPvP] HkmpGlobalSettingsSaveGuard: " +
                        "HKMP assembly was not found."
                    );

                    return;
                }

                _hkmpModType =
                    hkmpAssembly.GetType("Hkmp.HkmpMod");

                _modSettingsType =
                    hkmpAssembly.GetType(
                        "Hkmp.Game.Settings.ModSettings"
                    );

                if (_hkmpModType == null)
                {
                    Modding.Logger.Log(
                        "[OneShotPvP] HkmpGlobalSettingsSaveGuard: " +
                        "Hkmp.HkmpMod was not found."
                    );

                    return;
                }

                if (_modSettingsType == null)
                {
                    Modding.Logger.Log(
                        "[OneShotPvP] HkmpGlobalSettingsSaveGuard: " +
                        "ModSettings was not found."
                    );

                    return;
                }

                _onSaveGlobalMethod =
                    _hkmpModType.GetMethod(
                        "OnSaveGlobal",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                if (_onSaveGlobalMethod == null)
                {
                    Modding.Logger.Log(
                        "[OneShotPvP] HkmpGlobalSettingsSaveGuard: " +
                        "OnSaveGlobal was not found."
                    );

                    return;
                }

                _serverSettingsProperty =
                    _modSettingsType.GetProperty(
                        "ServerSettings",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                if (_serverSettingsProperty == null)
                {
                    Modding.Logger.Log(
                        "[OneShotPvP] HkmpGlobalSettingsSaveGuard: " +
                        "ModSettings.ServerSettings was not found."
                    );

                    return;
                }

                if (!_serverSettingsProperty.CanRead ||
                    !_serverSettingsProperty.CanWrite)
                {
                    Modding.Logger.Log(
                        "[OneShotPvP] HkmpGlobalSettingsSaveGuard: " +
                        "ModSettings.ServerSettings cannot be read/written."
                    );

                    return;
                }

                _memberwiseCloneMethod =
                    typeof(object).GetMethod(
                        "MemberwiseClone",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic
                    );

                if (_memberwiseCloneMethod == null)
                {
                    Modding.Logger.Log(
                        "[OneShotPvP] HkmpGlobalSettingsSaveGuard: " +
                        "MemberwiseClone was not found."
                    );

                    return;
                }

                MethodInfo dynamicHook =
                    CreateHookMethod();

                _hook = new Hook(
                    _onSaveGlobalMethod,
                    dynamicHook
                );

                _initialized = true;

                Modding.Logger.Log(
                    "[OneShotPvP] HkmpGlobalSettingsSaveGuard initialized."
                );
            }
            catch (Exception exception)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] HkmpGlobalSettingsSaveGuard " +
                    "initialization failed: " +
                    exception
                );
            }
        }

        public static void Unhook()
        {
            if (!_initialized)
            {
                return;
            }

            try
            {
                if (_hook != null)
                {
                    _hook.Dispose();
                    _hook = null;
                }

                _initialized = false;

                Modding.Logger.Log(
                    "[OneShotPvP] HkmpGlobalSettingsSaveGuard unhooked."
                );
            }
            catch (Exception exception)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] HkmpGlobalSettingsSaveGuard " +
                    "unhook failed: " +
                    exception
                );
            }
        }

        private static MethodInfo CreateHookMethod()
        {
            Type originalDelegateType =
                Expression.GetDelegateType(
                    _hkmpModType,
                    _modSettingsType
                );

            Type[] parameterTypes =
            {
                originalDelegateType,
                _hkmpModType
            };

            DynamicMethod dynamicMethod =
                new DynamicMethod(
                    "OneShotPvP_HkmpOnSaveGlobalHook",
                    _modSettingsType,
                    parameterTypes,
                    typeof(HkmpGlobalSettingsSaveGuard).Module,
                    true
                );

            ILGenerator il =
                dynamicMethod.GetILGenerator();

            /*
             * Вызываем оригинальный:
             *
             *     OnSaveGlobal(self)
             */

            il.Emit(OpCodes.Ldarg_0);

            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Newarr, typeof(object));

            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4_0);

            il.Emit(OpCodes.Ldarg_1);

            il.Emit(OpCodes.Stelem_Ref);

            MethodInfo dynamicInvoke =
                typeof(Delegate).GetMethod(
                    "DynamicInvoke",
                    new[]
                    {
                        typeof(object[])
                    }
                );

            if (dynamicInvoke == null)
            {
                throw new MissingMethodException(
                    "Delegate.DynamicInvoke was not found."
                );
            }

            il.Emit(
                OpCodes.Callvirt,
                dynamicInvoke
            );

            il.Emit(
                OpCodes.Castclass,
                _modSettingsType
            );

            /*
             * Передаём полученный ModSettings
             * в наш защитный метод.
             */

            il.Emit(
                OpCodes.Call,
                typeof(HkmpGlobalSettingsSaveGuard)
                    .GetMethod(
                        nameof(PrepareSettingsForSave),
                        BindingFlags.Static |
                        BindingFlags.NonPublic
                    )
            );

            il.Emit(OpCodes.Ret);

            return dynamicMethod;
        }

        private static object PrepareSettingsForSave(
            object modSettings)
        {
            if (modSettings == null)
            {
                return null;
            }

            /*
             * Если раунда нет, вообще ничего не меняем.
             */
            if (!RoundDamageSettings.IsActive)
            {
                return modSettings;
            }

            try
            {
                object liveServerSettings =
                    _serverSettingsProperty.GetValue(
                        modSettings,
                        null
                    );

                if (liveServerSettings == null)
                {
                    return modSettings;
                }

                /*
                 * Создаём отдельную копию ModSettings.
                 *
                 * Живой объект HKMP не трогаем.
                 */
                object saveSettings =
                    _memberwiseCloneMethod.Invoke(
                        modSettings,
                        null
                    );

                /*
                 * Создаём отдельную копию ServerSettings.
                 */
                object saveServerSettings =
                    _memberwiseCloneMethod.Invoke(
                        liveServerSettings,
                        null
                    );

                /*
                 * Возвращаем в копию оригинальные значения,
                 * которые были до начала OneShotPvP-раунда.
                 */
                bool prepared =
                    RoundDamageSettings
                        .PreparePersistentServerSettings(
                            saveServerSettings
                        );

                if (!prepared)
                {
                    return modSettings;
                }

                /*
                 * Подменяем ServerSettings только внутри
                 * объекта, который HKMP сейчас будет сохранять.
                 */
                _serverSettingsProperty.SetValue(
                    saveSettings,
                    saveServerSettings,
                    null
                );

                Modding.Logger.Log(
                    "[OneShotPvP] HKMP GlobalSettings save intercepted. " +
                    "Temporary OneShotPvP damage settings were excluded " +
                    "from persistent settings."
                );

                return saveSettings;
            }
            catch (Exception exception)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] Failed to prepare HKMP GlobalSettings " +
                    "for safe save: " +
                    exception
                );

                /*
                 * В случае ошибки ничего не ломаем.
                 */
                return modSettings;
            }
        }

        private static Assembly FindHkmpAssembly()
        {
            Assembly[] assemblies =
                AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    if (assembly.GetType("Hkmp.HkmpMod") != null)
                    {
                        return assembly;
                    }
                }
                catch
                {
                    // Игнорируем сборки, которые не удалось проверить.
                }
            }

            return null;
        }
    }
}