using System;
using System.Linq.Expressions;
using System.Reflection;

using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

using UnityEngine;

namespace OneShotPvP.Client
{
    internal static class HkmpElegyFix
    {
        private static bool _initialized;

        private static ILHook _hook;

        private static readonly string
            _slashBaseTypeName =
                "Hkmp.Animation.Effects.SlashBase";

        public static bool IsInitialized
        {
            get
            {
                return _initialized;
            }
        }

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            try
            {
                Assembly hkmpAssembly =
                    FindHkmpAssembly();

                if (hkmpAssembly == null)
                {
                    Modding.Logger.LogError(
                        "[OneShotPvP] HkmpElegyFix: " +
                        "HKMP assembly was not found."
                    );

                    return;
                }

                Type slashBaseType =
                    hkmpAssembly.GetType(
                        _slashBaseTypeName,
                        false
                    );

                if (slashBaseType == null)
                {
                    Modding.Logger.LogError(
                        "[OneShotPvP] HkmpElegyFix: " +
                        "SlashBase type was not found."
                    );

                    return;
                }

                Type slashType =
                    slashBaseType.GetNestedType(
                        "SlashType",
                        BindingFlags.NonPublic
                    );

                if (slashType == null)
                {
                    Modding.Logger.LogError(
                        "[OneShotPvP] HkmpElegyFix: " +
                        "SlashType was not found."
                    );

                    return;
                }

                MethodInfo playMethod =
                    slashBaseType.GetMethod(
                        "Play",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic,
                        null,
                        new Type[]
                        {
                            typeof(GameObject),
                            typeof(bool[]),
                            typeof(GameObject),
                            slashType
                        },
                        null
                    );

                if (playMethod == null)
                {
                    Modding.Logger.LogError(
                        "[OneShotPvP] HkmpElegyFix: " +
                        "SlashBase.Play method was not found."
                    );

                    return;
                }

                _hook =
                    new ILHook(
                        playMethod,
                        PatchSlashBasePlay
                    );

                Modding.Logger.Log(
                    "[OneShotPvP] HkmpElegyFix initialized."
                );
            }
            catch (Exception exception)
            {
                Modding.Logger.LogError(
                    "[OneShotPvP] HkmpElegyFix initialization failed. " +
                    "Exception=" +
                    exception
                );
            }
        }

        private static Assembly FindHkmpAssembly()
        {
            Assembly[] assemblies =
                AppDomain.CurrentDomain.GetAssemblies();

            for (
                int i = 0;
                i < assemblies.Length;
                i++
            )
            {
                Assembly assembly =
                    assemblies[i];

                if (assembly == null)
                {
                    continue;
                }

                AssemblyName assemblyName =
                    assembly.GetName();

                if (assemblyName == null)
                {
                    continue;
                }

                if (string.Equals(
                    assemblyName.Name,
                    "HKMP",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return assembly;
                }
            }

            return null;
        }

        private static void PatchSlashBasePlay(
            ILContext context)
        {
            try
            {
                ILCursor cursor =
                    new ILCursor(
                        context
                    );

                /*
                 * HKMP v2.4.3 компилирует локальные переменные
                 * SlashBase.Play() в следующем порядке:
                 *
                 * local 0 = isOnOneHealth
                 * local 1 = isOnFullHealth
                 * local 2 = hasFuryCharm
                 *
                 * Нас интересует участок:
                 *
                 *     if (!hasGrubberflyElegyCharm
                 *         || isOnOneHealth && !hasFuryCharm
                 *         || !isOnFullHealth)
                 *     {
                 *         return;
                 *     }
                 *
                 * Перед существующей проверкой
                 * isOnFullHealth мы добавляем:
                 *
                 *     isOnFullHealth || hasFuryCharm
                 *
                 * Таким образом Fury + 1 HP разрешает пройти
                 * к оригинальному созданию Elegy Beam.
                 */

                if (!cursor.TryGotoNext(
                    MoveType.After,
                    instruction =>
                        instruction.MatchLdloc(1)))
                {
                    Modding.Logger.LogError(
                        "[OneShotPvP] HkmpElegyFix: " +
                        "could not find isOnFullHealth local load."
                    );

                    return;
                }

                Instruction nextInstruction =
                    cursor.Next;

                if (nextInstruction == null)
                {
                    Modding.Logger.LogError(
                        "[OneShotPvP] HkmpElegyFix: " +
                        "unexpected end of IL after isOnFullHealth."
                    );

                    return;
                }

                if (
                    nextInstruction.OpCode !=
                    OpCodes.Brtrue &&
                    nextInstruction.OpCode !=
                    OpCodes.Brtrue_S)
                {
                    Modding.Logger.LogError(
                        "[OneShotPvP] HkmpElegyFix: " +
                        "isOnFullHealth is not followed by " +
                        "the expected branch."
                    );

                    return;
                }

                /*
                 * До этого места на стеке уже находится:
                 *
                 *     isOnFullHealth
                 *
                 * Добавляем:
                 *
                 *     hasFuryCharm
                 *
                 * и объединяем значения через наш delegate.
                 */
                cursor.Emit(
      OpCodes.Ldloc,
      2
  );

                cursor.EmitDelegate(
                    new Func<bool, bool, bool>(
                        AllowElegyAtOneHealth
                    )
                );

                Modding.Logger.Log(
                    "[OneShotPvP] HkmpElegyFix patched " +
                    "SlashBase.Play successfully."
                );
            }
            catch (Exception exception)
            {
                Modding.Logger.LogError(
                    "[OneShotPvP] HkmpElegyFix patch failed. " +
                    "Exception=" +
                    exception
                );
            }
        }

        private static bool AllowElegyAtOneHealth(
            bool isOnFullHealth,
            bool hasFuryCharm)
        {
            if (isOnFullHealth)
            {
                return true;
            }

            if (hasFuryCharm)
            {
                return true;
            }

            return false;
        }

        public static void Clear()
        {
            if (_hook != null)
            {
                try
                {
                    _hook.Dispose();

                    Modding.Logger.Log(
                        "[OneShotPvP] HkmpElegyFix hook disposed."
                    );
                }
                catch (Exception exception)
                {
                    Modding.Logger.LogError(
                        "[OneShotPvP] Failed to dispose HkmpElegyFix. " +
                        "Exception=" +
                        exception
                    );
                }

                _hook = null;
            }

            _initialized = false;
        }
    }
}