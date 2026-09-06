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

                _originalNailDamage =
                    currentSettings.NailDamage;

                _originalGreatSlashDamage =
                    currentSettings.GreatSlashDamage;

                _originalDashSlashDamage =
                    currentSettings.DashSlashDamage;

                _originalCycloneSlashDamage =
                    currentSettings.CycloneSlashDamage;

                _originalVengefulSpiritDamage =
                    currentSettings.VengefulSpiritDamage;

                _originalShadeSoulDamage =
                    currentSettings.ShadeSoulDamage;

                _originalDesolateDiveDamage =
                    currentSettings.DesolateDiveDamage;

                _originalDescendingDarkDamage =
                    currentSettings.DescendingDarkDamage;

                _originalHowlingWraithDamage =
                    currentSettings.HowlingWraithDamage;

                _originalAbyssShriekDamage =
                    currentSettings.AbyssShriekDamage;

                _originalGrubberflyElegyDamage =
                    currentSettings.GrubberflyElegyDamage;

                _originalSporeShroomDamage =
                    currentSettings.SporeShroomDamage;

                _originalSporeDungShroomDamage =
                    currentSettings.SporeDungShroomDamage;

                _originalThornOfAgonyDamage =
                    currentSettings.ThornOfAgonyDamage;

                _originalSharpShadowDamage =
                    currentSettings.SharpShadowDamage;

                ServerSettings newSettings =
                    CreateCopy(currentSettings);

                // ==========================================
                // ЗАПРЕЩЁННЫЕ ИСТОЧНИКИ
                // ==========================================

                // Обычный Nail.
                newSettings.NailDamage = 0;

                // Cyclone Slash.
                newSettings.CycloneSlashDamage = 0;

                // Thorns of Agony.
                newSettings.ThornOfAgonyDamage = 0;

                // Sharp Shadow.
                newSettings.SharpShadowDamage = 0;

                // Spore Shroom.
                newSettings.SporeShroomDamage = 0;

                // Spore-Dung Shroom.
                newSettings.SporeDungShroomDamage = 0;

                // ==========================================
                // РАЗРЕШЁННЫЕ ИСТОЧНИКИ
                // ==========================================

                // Great Slash.
                newSettings.GreatSlashDamage = 2;

                // Dash Slash.
                newSettings.DashSlashDamage = 2;

                // Vengeful Spirit.
                newSettings.VengefulSpiritDamage = 1;

                // Shade Soul.
                newSettings.ShadeSoulDamage = 2;

                // Desolate Dive.
                newSettings.DesolateDiveDamage = 1;

                // Descending Dark.
                newSettings.DescendingDarkDamage = 2;

                // Howling Wraiths.
                newSettings.HowlingWraithDamage = 1;

                // Abyss Shriek.
                newSettings.AbyssShriekDamage = 2;

                // Grubberfly's Elegy.
                newSettings.GrubberflyElegyDamage = 1;

                _serverApi.ServerManager.ApplyServerSettings(
                    newSettings
                );

                _saved = true;

                Modding.Logger.Log(
                    "[OneShotPvP] Round PvP damage settings applied. " +
                    "Nail=0, " +
                    "GreatSlash=2, " +
                    "DashSlash=2, " +
                    "Cyclone=0, " +
                    "VS=1, " +
                    "ShadeSoul=2, " +
                    "Dive=1, " +
                    "Dark=2, " +
                    "Wraith=1, " +
                    "Shriek=2, " +
                    "Elegy=1, " +
                    "Spore=0, " +
                    "DungSpore=0, " +
                    "Thorns=0, " +
                    "SharpShadow=0."
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

                newSettings.NailDamage =
                    _originalNailDamage;

                newSettings.GreatSlashDamage =
                    _originalGreatSlashDamage;

                newSettings.DashSlashDamage =
                    _originalDashSlashDamage;

                newSettings.CycloneSlashDamage =
                    _originalCycloneSlashDamage;

                newSettings.VengefulSpiritDamage =
                    _originalVengefulSpiritDamage;

                newSettings.ShadeSoulDamage =
                    _originalShadeSoulDamage;

                newSettings.DesolateDiveDamage =
                    _originalDesolateDiveDamage;

                newSettings.DescendingDarkDamage =
                    _originalDescendingDarkDamage;

                newSettings.HowlingWraithDamage =
                    _originalHowlingWraithDamage;

                newSettings.AbyssShriekDamage =
                    _originalAbyssShriekDamage;

                newSettings.GrubberflyElegyDamage =
                    _originalGrubberflyElegyDamage;

                newSettings.SporeShroomDamage =
                    _originalSporeShroomDamage;

                newSettings.SporeDungShroomDamage =
                    _originalSporeDungShroomDamage;

                newSettings.ThornOfAgonyDamage =
                    _originalThornOfAgonyDamage;

                newSettings.SharpShadowDamage =
                    _originalSharpShadowDamage;

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