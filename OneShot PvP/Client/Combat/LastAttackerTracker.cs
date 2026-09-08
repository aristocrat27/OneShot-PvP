using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using GlobalEnums;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace OneShotPvP.Client
{
    internal static class LastAttackerTracker
    {
        private static bool _initialized;

        private static ushort? _lastAttackerId;

        /*
         * Время последней HKMP-атаки.
         *
         * Используется как fallback для атак, у которых
         * существующий DamageHero переиспользуется и новый
         * DamageHero не появляется.
         */
        private static float _lastAttackTime =
            -Mathf.Infinity;

        /*
         * Максимальное время, в течение которого
         * последний атакующий считается актуальным.
         */
        private const float LastAttackFallbackWindow =
            2.0f;

        private static readonly Dictionary<int, ushort>
            _attackOwners =
                new Dictionary<int, ushort>();

        private static readonly List<Hook>
            _hkmpHooks =
                new List<Hook>();

        private static readonly string[]
            _hkmpAttackTypes =
            {
                "Hkmp.Animation.Effects.VengefulSpirit",
                "Hkmp.Animation.Effects.ShadeSoul",

                "Hkmp.Animation.Effects.Slash",
                "Hkmp.Animation.Effects.AltSlash",
                "Hkmp.Animation.Effects.DownSlash",
                "Hkmp.Animation.Effects.UpSlash",
                "Hkmp.Animation.Effects.WallSlash"
            };

        public static bool HasAttacker
        {
            get
            {
                return _lastAttackerId.HasValue;
            }
        }

        public static ushort LastAttackerId
        {
            get
            {
                if (_lastAttackerId.HasValue)
                {
                    return _lastAttackerId.Value;
                }

                return ushort.MaxValue;
            }
        }

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            _lastAttackerId = null;

            _lastAttackTime =
                -Mathf.Infinity;

            _attackOwners.Clear();

            On.HeroController.TakeDamage +=
                OnHeroTakeDamage;

            InstallHkmpAttackHooks();

            Modding.Logger.Log(
                "[OneShotPvP] LastAttackerTracker initialized."
            );
        }

        private static void InstallHkmpAttackHooks()
        {
            Assembly hkmpAssembly =
                FindHkmpAssembly();

            if (hkmpAssembly == null)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] HKMP assembly not found. " +
                    "HKMP attack hooks were not installed."
                );

                return;
            }

            Modding.Logger.Log(
                "[OneShotPvP] HKMP assembly found: " +
                hkmpAssembly.FullName
            );

            for (
                int i = 0;
                i < _hkmpAttackTypes.Length;
                i++
            )
            {
                InstallHkmpPlayHook(
                    hkmpAssembly,
                    _hkmpAttackTypes[i]
                );
            }

            Modding.Logger.Log(
                "[OneShotPvP] HKMP attack hooks installed. " +
                "Count=" +
                _hkmpHooks.Count +
                "/" +
                _hkmpAttackTypes.Length
            );
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

        private static void InstallHkmpPlayHook(
            Assembly hkmpAssembly,
            string typeName)
        {
            Type effectType =
                hkmpAssembly.GetType(
                    typeName,
                    false
                );

            if (effectType == null)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] HKMP type not found: " +
                    typeName
                );

                return;
            }

            MethodInfo playMethod =
                effectType.GetMethod(
                    "Play",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic,
                    null,
                    new Type[]
                    {
                        typeof(GameObject),
                        typeof(bool[])
                    },
                    null
                );

            if (playMethod == null)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] HKMP Play method not found: " +
                    typeName
                );

                return;
            }

            if (playMethod.ReturnType != typeof(void))
            {
                Modding.Logger.Log(
                    "[OneShotPvP] HKMP Play method has unexpected " +
                    "return type: " +
                    typeName +
                    " ReturnType=" +
                    playMethod.ReturnType.FullName
                );

                return;
            }

            try
            {
                Type selfType =
                    effectType;

                Type origDelegateType =
                    Expression.GetDelegateType(
                        selfType,
                        typeof(GameObject),
                        typeof(bool[]),
                        typeof(void)
                    );

                Type hookDelegateType =
                    Expression.GetDelegateType(
                        origDelegateType,
                        selfType,
                        typeof(GameObject),
                        typeof(bool[]),
                        typeof(void)
                    );

                MethodInfo genericHookMethod =
                    typeof(LastAttackerTracker).GetMethod(
                        "OnHkmpPlayGeneric",
                        BindingFlags.Static |
                        BindingFlags.NonPublic
                    );

                if (genericHookMethod == null)
                {
                    Modding.Logger.Log(
                        "[OneShotPvP] Generic HKMP hook method was not found."
                    );

                    return;
                }

                MethodInfo closedHookMethod =
                    genericHookMethod.MakeGenericMethod(
                        origDelegateType,
                        selfType
                    );

                Delegate hookDelegate =
                    Delegate.CreateDelegate(
                        hookDelegateType,
                        closedHookMethod
                    );

                Hook hook =
                    new Hook(
                        playMethod,
                        hookDelegate
                    );

                _hkmpHooks.Add(
                    hook
                );

                Modding.Logger.Log(
                    "[OneShotPvP] HKMP Play hook installed: " +
                    typeName
                );
            }
            catch (Exception ex)
            {
                Modding.Logger.LogError(
                    "[OneShotPvP] Failed to install HKMP Play hook: " +
                    typeName +
                    " Exception=" +
                    ex
                );
            }
        }

        private static void OnHkmpPlayGeneric<TOrig, TSelf>(
            TOrig orig,
            TSelf self,
            GameObject playerObject,
            bool[] effectInfo)
            where TOrig : class
        {
            HashSet<int> existingDamageHeroes =
                CaptureDamageHeroIds();

            ushort attackerId;

            bool attackerResolved =
                TryGetPlayerIdFromObject(
                    playerObject,
                    out attackerId
                );

            if (attackerResolved)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] HKMP attack Play detected. " +
                    "AttackerId=" +
                    attackerId +
                    " PlayerObject=" +
                    GetObjectName(playerObject) +
                    " Effect=" +
                    typeof(TSelf).FullName
                );
            }
            else
            {
                Modding.Logger.Log(
                    "[OneShotPvP] HKMP attack Play detected, " +
                    "but attacker ID could not be resolved. " +
                    "PlayerObject=" +
                    GetObjectName(playerObject) +
                    " Effect=" +
                    typeof(TSelf).FullName
                );
            }

            /*
             * ВАЖНО:
             *
             * Некоторые HKMP-атаки используют уже существующий
             * DamageHero и поэтому после Play не появляется
             * новый объект.
             *
             * В этом случае RegisterNewDamageHeroes() не сможет
             * сопоставить источник урона с атакующим.
             *
             * Поэтому при успешном определении playerObject
             * сразу запоминаем атакующего.
             */
            if (
                attackerResolved &&
                RoundClientManager.IsRoundActive
            )
            {
                _lastAttackerId =
                    attackerId;

                _lastAttackTime =
                    Time.time;

                Modding.Logger.Log(
                    "[OneShotPvP] Recent attacker updated. " +
                    "AttackerId=" +
                    attackerId +
                    " Effect=" +
                    typeof(TSelf).FullName
                );
            }

            origDynamic(
                orig,
                self,
                playerObject,
                effectInfo
            );

            if (!attackerResolved)
            {
                return;
            }

            if (!RoundClientManager.IsRoundActive)
            {
                return;
            }

            RegisterNewDamageHeroes(
                existingDamageHeroes,
                attackerId,
                typeof(TSelf).Name
            );
        }

        private static void origDynamic<TOrig, TSelf>(
            TOrig orig,
            TSelf self,
            GameObject playerObject,
            bool[] effectInfo)
            where TOrig : class
        {
            Delegate delegateValue =
                orig as Delegate;

            if (delegateValue == null)
            {
                throw new InvalidOperationException(
                    "[OneShotPvP] HKMP original delegate is null."
                );
            }

            delegateValue.DynamicInvoke(
                self,
                playerObject,
                effectInfo
            );
        }

        private static HashSet<int> CaptureDamageHeroIds()
        {
            HashSet<int> result =
                new HashSet<int>();

            DamageHero[] damageHeroes =
                UnityEngine.Object.FindObjectsOfType<DamageHero>();

            if (damageHeroes == null)
            {
                return result;
            }

            for (
                int i = 0;
                i < damageHeroes.Length;
                i++
            )
            {
                DamageHero damageHero =
                    damageHeroes[i];

                if (damageHero == null)
                {
                    continue;
                }

                GameObject gameObject =
                    damageHero.gameObject;

                if (gameObject == null)
                {
                    continue;
                }

                result.Add(
                    gameObject.GetInstanceID()
                );
            }

            return result;
        }

        private static void RegisterNewDamageHeroes(
            HashSet<int> existingDamageHeroes,
            ushort attackerId,
            string attackType)
        {
            if (!RoundClientManager.IsRoundActive)
            {
                return;
            }

            DamageHero[] damageHeroes =
                UnityEngine.Object.FindObjectsOfType<DamageHero>();

            if (damageHeroes == null)
            {
                return;
            }

            int registeredCount = 0;

            for (
                int i = 0;
                i < damageHeroes.Length;
                i++
            )
            {
                DamageHero damageHero =
                    damageHeroes[i];

                if (damageHero == null)
                {
                    continue;
                }

                GameObject gameObject =
                    damageHero.gameObject;

                if (gameObject == null)
                {
                    continue;
                }

                int instanceId =
                    gameObject.GetInstanceID();

                if (existingDamageHeroes.Contains(
                    instanceId))
                {
                    continue;
                }

                _attackOwners[
                    instanceId
                ] = attackerId;

                registeredCount++;

                Modding.Logger.Log(
                    "[OneShotPvP] Attack object registered. " +
                    "AttackerId=" +
                    attackerId +
                    " AttackType=" +
                    attackType +
                    " Object=" +
                    gameObject.name +
                    " InstanceId=" +
                    instanceId +
                    " Damage=" +
                    damageHero.damageDealt +
                    " HazardType=" +
                    damageHero.hazardType
                );
            }

            if (registeredCount == 0)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] HKMP attack created no new " +
                    "DamageHero objects. " +
                    "AttackType=" +
                    attackType
                );
            }
        }

        private static void OnHeroTakeDamage(
            On.HeroController.orig_TakeDamage orig,
            HeroController self,
            GameObject go,
            CollisionSide damageSide,
            int damageAmount,
            int hazardType)
        {
            ushort attackerId;

            bool attackerFound =
                TryResolveAttacker(
                    go,
                    out attackerId
                );

            bool usedRecentAttackerFallback =
                false;

            /*
             * Сначала используем точное сопоставление
             * конкретного DamageHero с атакующим.
             */
            if (attackerFound)
            {
                _lastAttackerId =
                    attackerId;

                Modding.Logger.Log(
                    "[OneShotPvP] Attack source resolved. " +
                    "AttackerId=" +
                    attackerId +
                    " Source=" +
                    GetObjectName(go) +
                    " Damage=" +
                    damageAmount +
                    " HazardType=" +
                    hazardType
                );
            }
            else
            {
                /*
                 * Если конкретный DamageHero не зарегистрирован,
                 * проверяем, относится ли источник к DamageHero.
                 *
                 * Только тогда разрешаем fallback по последней
                 * HKMP-атаке.
                 */
                if (
                    RoundClientManager.IsRoundActive &&
                    HasDamageHeroInHierarchy(go) &&
                    TryGetRecentAttacker(
                        out attackerId
                    )
                )
                {
                    attackerFound = true;

                    usedRecentAttackerFallback =
                        true;

                    Modding.Logger.Log(
                        "[OneShotPvP] Attack source unresolved. " +
                        "Using recent HKMP attacker fallback. " +
                        "AttackerId=" +
                        attackerId +
                        " Source=" +
                        GetObjectName(go) +
                        " Age=" +
                        (Time.time - _lastAttackTime)
                    );
                }
            }

            orig(
                self,
                go,
                damageSide,
                damageAmount,
                hazardType
            );

            if (!attackerFound)
            {
                return;
            }

            if (!RoundClientManager.IsRoundActive)
            {
                return;
            }

            if (self == null)
            {
                return;
            }

            if (HeroController.instance == null)
            {
                return;
            }

            if (self != HeroController.instance)
            {
                return;
            }

            if (self.playerData == null)
            {
                return;
            }

            int health =
                self.playerData.GetInt(
                    "health"
                );

            if (health > 0)
            {
                return;
            }

            Modding.Logger.Log(
                "[OneShotPvP] Lethal PvP hit detected. " +
                "KillerId=" +
                attackerId +
                " Source=" +
                GetObjectName(go) +
                " UsedRecentFallback=" +
                usedRecentAttackerFallback
            );

            ClientDeathTracker.ReportPvpDeath(
                attackerId
            );
        }

        private static bool TryResolveAttacker(
            GameObject source,
            out ushort attackerId)
        {
            attackerId = 0;

            if (source == null)
            {
                return false;
            }

            int instanceId =
                source.GetInstanceID();

            ushort registeredId;

            if (_attackOwners.TryGetValue(
                instanceId,
                out registeredId))
            {
                attackerId =
                    registeredId;

                return true;
            }

            Transform current =
                source.transform;

            while (current != null)
            {
                if (TryParsePlayerContainer(
                    current.name,
                    out attackerId))
                {
                    return true;
                }

                current =
                    current.parent;
            }

            return false;
        }

        private static bool HasDamageHeroInHierarchy(
            GameObject source)
        {
            if (source == null)
            {
                return false;
            }

            Transform current =
                source.transform;

            while (current != null)
            {
                DamageHero damageHero =
                    current.GetComponent<DamageHero>();

                if (damageHero != null)
                {
                    return true;
                }

                current =
                    current.parent;
            }

            return false;
        }

        private static bool TryGetRecentAttacker(
            out ushort playerId)
        {
            playerId = 0;

            if (!_lastAttackerId.HasValue)
            {
                return false;
            }

            if (!RoundClientManager.IsRoundActive)
            {
                return false;
            }

            if (float.IsNegativeInfinity(
                _lastAttackTime))
            {
                return false;
            }

            float age =
                Time.time -
                _lastAttackTime;

            if (age < 0f)
            {
                return false;
            }

            if (age > LastAttackFallbackWindow)
            {
                return false;
            }

            playerId =
                _lastAttackerId.Value;

            return true;
        }

        private static bool TryGetPlayerIdFromObject(
            GameObject playerObject,
            out ushort playerId)
        {
            playerId = 0;

            if (playerObject == null)
            {
                return false;
            }

            Transform current =
                playerObject.transform;

            while (current != null)
            {
                if (TryParsePlayerContainer(
                    current.name,
                    out playerId))
                {
                    return true;
                }

                current =
                    current.parent;
            }

            return false;
        }

        private static bool TryParsePlayerContainer(
            string objectName,
            out ushort playerId)
        {
            playerId = 0;

            if (string.IsNullOrEmpty(
                objectName))
            {
                return false;
            }

            const string prefix =
                "Player Container ";

            if (!objectName.StartsWith(
                prefix,
                StringComparison.Ordinal))
            {
                return false;
            }

            string idText =
                objectName.Substring(
                    prefix.Length
                );

            ushort parsedId;

            if (!ushort.TryParse(
                idText,
                out parsedId))
            {
                return false;
            }

            playerId =
                parsedId;

            return true;
        }

        private static string GetObjectName(
            GameObject gameObject)
        {
            if (gameObject == null)
            {
                return "<null>";
            }

            return gameObject.name;
        }

        public static bool TryGetLastAttacker(
            out ushort playerId)
        {
            if (!_lastAttackerId.HasValue)
            {
                playerId = 0;

                return false;
            }

            playerId =
                _lastAttackerId.Value;

            return true;
        }

        public static void Clear()
        {
            _lastAttackerId = null;

            _lastAttackTime =
                -Mathf.Infinity;

            _attackOwners.Clear();

            Modding.Logger.Log(
                "[OneShotPvP] Last attacker state cleared."
            );
        }
    }
}