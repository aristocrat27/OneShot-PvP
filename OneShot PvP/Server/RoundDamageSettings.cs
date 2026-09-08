using System;
using System.Reflection;

using Hkmp.Api.Server;
using Hkmp.Game.Settings;

namespace OneShotPvP.Server
{
    internal sealed class RoundDamageSettings
    {
        private readonly IServerApi _serverApi;

        private bool _saved;

        private byte _originalNailDamage;
        private byte _originalGreatSlashDamage;
        private byte _originalDashSlashDamage;
        private byte _originalCycloneSlashDamage;

        private byte _originalVengefulSpiritDamage;
        private byte _originalShadeSoulDamage;

        private byte _originalDesolateDiveDamage;
        private byte _originalDescendingDarkDamage;

        private byte _originalHowlingWraithDamage;
        private byte _originalAbyssShriekDamage;

        private byte _originalGrubberflyElegyDamage;

        private byte _originalSporeShroomDamage;
        private byte _originalSporeDungShroomDamage;

        private byte _originalThornOfAgonyDamage;
        private byte _originalSharpShadowDamage;

        // Единственный активный экземпляр RoundDamageSettings.
        // Нужен save-guard, который работает отдельно от RoundManager.
        private static RoundDamageSettings _activeInstance;

        /// <summary>
        /// Показывает, изменяет ли OneShotPvP сейчас HKMP ServerSettings.
        /// </summary>
        public static bool IsActive
        {
            get
            {
                return _activeInstance != null &&
                       _activeInstance._saved;
            }
        }

        public RoundDamageSettings(
            IServerApi serverApi)
        {
            _serverApi = serverApi;

            _activeInstance = this;
        }

        public bool ApplyForRound()
        {
            if (_saved)
            {
                return true;
            }

            try
            {
                IServerSettings interfaceSettings =
                    _serverApi.ServerManager.ServerSettings;

                if (interfaceSettings == null)
                {
                    Modding.Logger.Log(
                        "[OneShotPvP] Cannot apply round damage settings: " +
                        "ServerSettings is null."
                    );

                    return false;
                }

                ServerSettings settings =
                    interfaceSettings as ServerSettings;

                if (settings == null)
                {
                    Modding.Logger.Log(
                        "[OneShotPvP] Cannot apply round damage settings: " +
                        "ServerSettings is not the HKMP ServerSettings type."
                    );

                    return false;
                }

                // ==========================================
                // СОХРАНЯЕМ ТЕКУЩИЕ НАСТРОЙКИ ХОСТА
                // ==========================================

                _originalNailDamage =
                    settings.NailDamage;

                _originalGreatSlashDamage =
                    settings.GreatSlashDamage;

                _originalDashSlashDamage =
                    settings.DashSlashDamage;

                _originalCycloneSlashDamage =
                    settings.CycloneSlashDamage;

                _originalVengefulSpiritDamage =
                    settings.VengefulSpiritDamage;

                _originalShadeSoulDamage =
                    settings.ShadeSoulDamage;

                _originalDesolateDiveDamage =
                    settings.DesolateDiveDamage;

                _originalDescendingDarkDamage =
                    settings.DescendingDarkDamage;

                _originalHowlingWraithDamage =
                    settings.HowlingWraithDamage;

                _originalAbyssShriekDamage =
                    settings.AbyssShriekDamage;

                _originalGrubberflyElegyDamage =
                    settings.GrubberflyElegyDamage;

                _originalSporeShroomDamage =
                    settings.SporeShroomDamage;

                _originalSporeDungShroomDamage =
                    settings.SporeDungShroomDamage;

                _originalThornOfAgonyDamage =
                    settings.ThornOfAgonyDamage;

                _originalSharpShadowDamage =
                    settings.SharpShadowDamage;

                // ==========================================
                // НАСТРОЙКИ ONE SHOT PVP
                // ==========================================

                settings.NailDamage = 0;

                settings.GreatSlashDamage = 2;

                settings.DashSlashDamage = 2;

                settings.CycloneSlashDamage = 0;

                settings.VengefulSpiritDamage = 1;

                settings.ShadeSoulDamage = 2;

                settings.DesolateDiveDamage = 1;

                settings.DescendingDarkDamage = 2;

                settings.HowlingWraithDamage = 1;

                settings.AbyssShriekDamage = 2;

                settings.GrubberflyElegyDamage = 1;

                settings.SporeShroomDamage = 0;

                settings.SporeDungShroomDamage = 0;

                settings.ThornOfAgonyDamage = 0;

                settings.SharpShadowDamage = 0;

                // ==========================================
                // УВЕДОМЛЯЕМ HKMP
                // ==========================================

                if (!NotifyServerSettingsUpdated())
                {
                    Modding.Logger.Log(
                        "[OneShotPvP] Failed to notify HKMP " +
                        "about changed ServerSettings."
                    );

                    return false;
                }

                _saved = true;

                Modding.Logger.Log(
                    "[OneShotPvP] OneShotPvP damage settings applied " +
                    "directly to HKMP ServerSettings."
                );

                return true;
            }
            catch (Exception exception)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] Failed to apply round damage settings: " +
                    exception
                );

                return false;
            }
        }

        public void Restore()
        {
            if (!_saved)
            {
                return;
            }

            try
            {
                IServerSettings interfaceSettings =
                    _serverApi.ServerManager.ServerSettings;

                if (interfaceSettings == null)
                {
                    Modding.Logger.Log(
                        "[OneShotPvP] Cannot restore damage settings: " +
                        "ServerSettings is null."
                    );

                    _saved = false;

                    return;
                }

                ServerSettings settings =
                    interfaceSettings as ServerSettings;

                if (settings == null)
                {
                    Modding.Logger.Log(
                        "[OneShotPvP] Cannot restore damage settings: " +
                        "ServerSettings is not the HKMP ServerSettings type."
                    );

                    _saved = false;

                    return;
                }

                RestoreOriginalValues(settings);

                NotifyServerSettingsUpdated();

                _saved = false;

                Modding.Logger.Log(
                    "[OneShotPvP] Original HKMP PvP damage settings restored."
                );
            }
            catch (Exception exception)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] Failed to restore PvP damage settings: " +
                    exception
                );
            }
        }

        /// <summary>
        /// Записывает оригинальные значения OneShotPvP в переданную
        /// копию ServerSettings.
        ///
        /// ВАЖНО:
        /// этот метод НЕ изменяет живые настройки HKMP.
        ///
        /// Он используется только HkmpGlobalSettingsSaveGuard,
        /// чтобы HKMP записал в GlobalSettings оригинальные значения,
        /// даже если раунд всё ещё активен.
        /// </summary>
        public static bool PreparePersistentServerSettings(
            object serverSettingsObject)
        {
            if (!IsActive)
            {
                return false;
            }

            if (serverSettingsObject == null)
            {
                return false;
            }

            ServerSettings settings =
                serverSettingsObject as ServerSettings;

            if (settings == null)
            {
                return false;
            }

            try
            {
                _activeInstance.RestoreOriginalValues(settings);

                return true;
            }
            catch (Exception exception)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] Failed to prepare persistent " +
                    "HKMP ServerSettings: " +
                    exception
                );

                return false;
            }
        }

        /// <summary>
        /// Возвращает сохранённые до начала раунда значения
        /// в конкретный объект ServerSettings.
        /// </summary>
        private void RestoreOriginalValues(
            ServerSettings settings)
        {
            settings.NailDamage =
                _originalNailDamage;

            settings.GreatSlashDamage =
                _originalGreatSlashDamage;

            settings.DashSlashDamage =
                _originalDashSlashDamage;

            settings.CycloneSlashDamage =
                _originalCycloneSlashDamage;

            settings.VengefulSpiritDamage =
                _originalVengefulSpiritDamage;

            settings.ShadeSoulDamage =
                _originalShadeSoulDamage;

            settings.DesolateDiveDamage =
                _originalDesolateDiveDamage;

            settings.DescendingDarkDamage =
                _originalDescendingDarkDamage;

            settings.HowlingWraithDamage =
                _originalHowlingWraithDamage;

            settings.AbyssShriekDamage =
                _originalAbyssShriekDamage;

            settings.GrubberflyElegyDamage =
                _originalGrubberflyElegyDamage;

            settings.SporeShroomDamage =
                _originalSporeShroomDamage;

            settings.SporeDungShroomDamage =
                _originalSporeDungShroomDamage;

            settings.ThornOfAgonyDamage =
                _originalThornOfAgonyDamage;

            settings.SharpShadowDamage =
                _originalSharpShadowDamage;
        }

        private bool NotifyServerSettingsUpdated()
        {
            if (_serverApi == null ||
                _serverApi.ServerManager == null)
            {
                return false;
            }

            try
            {
                MethodInfo updateMethod =
                    _serverApi.ServerManager.GetType().GetMethod(
                        "OnUpdateServerSettings",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                if (updateMethod == null)
                {
                    Modding.Logger.Log(
                        "[OneShotPvP] HKMP OnUpdateServerSettings " +
                        "method was not found."
                    );

                    return false;
                }

                updateMethod.Invoke(
                    _serverApi.ServerManager,
                    null
                );

                return true;
            }
            catch (Exception exception)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] Failed to call HKMP " +
                    "OnUpdateServerSettings: " +
                    exception
                );

                return false;
            }
        }
    }
}