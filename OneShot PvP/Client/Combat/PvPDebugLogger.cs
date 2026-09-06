using System.Text;
using UnityEngine;

namespace OneShotPvP.Client
{
    internal static class PvPDebugLogger
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            On.HealthManager.TakeDamage += OnTakeDamage;
        }

        private static void OnTakeDamage(
            On.HealthManager.orig_TakeDamage orig,
            HealthManager self,
            HitInstance hitInstance)
        {
            LogHit(hitInstance);

            orig(self, hitInstance);
        }

        private static void LogHit(
            HitInstance hitInstance)
        {
            if (hitInstance.Source == null)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] PvP DEBUG: Source = NULL"
                );

                return;
            }

            Transform current =
                hitInstance.Source.transform;

            StringBuilder builder =
                new StringBuilder();

            builder.AppendLine(
                "[OneShotPvP] ===== HIT DEBUG ====="
            );

            builder.AppendLine(
                "AttackType: " +
                hitInstance.AttackType
            );

            builder.AppendLine(
                "Damage: " +
                hitInstance.DamageDealt
            );

            builder.AppendLine(
                "Source: " +
                hitInstance.Source.name
            );

            builder.AppendLine(
                "Source Tag: " +
                hitInstance.Source.tag
            );

            builder.AppendLine(
                "Source Layer: " +
                hitInstance.Source.layer
            );

            builder.AppendLine(
                "Hierarchy:"
            );

            int depth = 0;

            while (current != null)
            {
                builder.AppendLine(
                    GetIndent(depth) +
                    current.name +
                    " | Tag=" +
                    current.gameObject.tag +
                    " | Layer=" +
                    current.gameObject.layer
                );

                current = current.parent;
                depth++;

                if (depth >= 30)
                {
                    builder.AppendLine(
                        "... hierarchy limit reached"
                    );

                    break;
                }
            }

            builder.AppendLine(
                "[OneShotPvP] ===== END HIT DEBUG ====="
            );

            Modding.Logger.Log(
                builder.ToString()
            );
        }

        private static string GetIndent(
            int depth)
        {
            StringBuilder builder =
                new StringBuilder();

            for (int i = 0; i < depth; i++)
            {
                builder.Append("  ");
            }

            return builder.ToString();
        }
    }
}