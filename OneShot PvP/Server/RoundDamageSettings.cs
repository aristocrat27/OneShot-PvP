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
        private byte _originalCycloneSlashDamage;
        private byte _originalThornOfAgonyDamage;
        private byte _originalSharpShadowDamage;

        public RoundDamageSettings(
            IServerApi serverApi)
        {
            _serverApi = serverApi;
        }

        public bool ApplyForRound()
        {
            if (_saved)
            {
                return true;
            }

            try
            {
                IServerSettings currentSettings =
                    _serverApi.ServerManager.ServerSettings;

                if (currentSettings == null)
                {
                    Modding.Logger.Log(
                        "[OneShotPvP] Cannot apply round damage settings: " +
                        "ServerSettings is null."
                    );

                    return false;
                }

                // Сохраняем исходные значения только тех
                // источников урона, которые будем изменять.
                _originalNailDamage =
                    currentSettings.NailDamage;

                _originalCycloneSlashDamage =
                    currentSettings.CycloneSlashDamage;

                _originalThornOfAgonyDamage =
                    currentSettings.ThornOfAgonyDamage;

                _originalSharpShadowDamage =
                    currentSettings.SharpShadowDamage;

                ServerSettings newSettings =
                    CreateCopy(currentSettings);

                // ==========================================
                // ЗАПРЕЩЁННЫЕ PvP-ИСТОЧНИКИ
                // ==========================================

                // Обычный Nail.
                newSettings.NailDamage = 0;

                // Cyclone Slash.
                newSettings.CycloneSlashDamage = 0;

                // Thorns of Agony.
                newSettings.ThornOfAgonyDamage = 0;

                // Sharp Shadow.
                newSettings.SharpShadowDamage = 0;

                // ==========================================
                // Great Slash и Dash Slash НЕ ИЗМЕНЯЕМ.
                //
                // Их значения остаются такими, какие были
                // установлены в текущих ServerSettings HKMP.
                // ==========================================

                _serverApi.ServerManager.ApplyServerSettings(
                    newSettings
                );

                _saved = true;

                Modding.Logger.Log(
                    "[OneShotPvP] Round PvP damage settings applied. " +
                    "Nail=0, " +
                    "Cyclone=0, " +
                    "Thorns=0, " +
                    "SharpShadow=0. " +
                    "GreatSlash/DashSlash unchanged."
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
                IServerSettings currentSettings =
                    _serverApi.ServerManager.ServerSettings;

                if (currentSettings == null)
                {
                    Modding.Logger.Log(
                        "[OneShotPvP] Cannot restore damage settings: " +
                        "ServerSettings is null."
                    );

                    _saved = false;

                    return;
                }

                ServerSettings newSettings =
                    CreateCopy(currentSettings);

                // Восстанавливаем исходные значения.

                newSettings.NailDamage =
                    _originalNailDamage;

                newSettings.CycloneSlashDamage =
                    _originalCycloneSlashDamage;

                newSettings.ThornOfAgonyDamage =
                    _originalThornOfAgonyDamage;

                newSettings.SharpShadowDamage =
                    _originalSharpShadowDamage;

                // Great Slash и Dash Slash здесь тоже
                // не трогаются — их текущие значения
                // остаются без изменений.

                _serverApi.ServerManager.ApplyServerSettings(
                    newSettings
                );

                _saved = false;

                Modding.Logger.Log(
                    "[OneShotPvP] Original PvP damage settings restored."
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

        private static ServerSettings CreateCopy(
            IServerSettings source)
        {
            ServerSettings copy =
                new ServerSettings();

            PropertyInfo[] properties =
                typeof(ServerSettings).GetProperties(
                    BindingFlags.Instance |
                    BindingFlags.Public
                );

            foreach (PropertyInfo property
                in properties)
            {
                if (!property.CanRead ||
                    !property.CanWrite)
                {
                    continue;
                }

                PropertyInfo sourceProperty =
                    typeof(IServerSettings).GetProperty(
                        property.Name
                    );

                if (sourceProperty == null ||
                    !sourceProperty.CanRead)
                {
                    continue;
                }

                object value =
                    sourceProperty.GetValue(
                        source,
                        null
                    );

                property.SetValue(
                    copy,
                    value,
                    null
                );
            }

            return copy;
        }
    }
}