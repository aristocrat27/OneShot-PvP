using System;
using System.Collections.Generic;

namespace OneShotPvP.Server
{
    internal sealed class ServerManaManager
    {
        private readonly Dictionary<ushort, int> _mana =
            new Dictionary<ushort, int>();

        private readonly HashSet<ushort> _deadPlayers =
            new HashSet<ushort>();

        public int GetMana(
            ushort playerId)
        {
            int mana;

            if (_mana.TryGetValue(
                playerId,
                out mana))
            {
                return mana;
            }

            return 0;
        }

        public bool HasPlayer(
            ushort playerId)
        {
            return _mana.ContainsKey(
                playerId
            );
        }

        public bool IsDead(
            ushort playerId)
        {
            return _deadPlayers.Contains(
                playerId
            );
        }

        public void AddPlayer(
            ushort playerId)
        {
            _mana[playerId] =
                OneShotConstants.CastMana;

            _deadPlayers.Remove(
                playerId
            );
        }

        public void RemovePlayer(
            ushort playerId)
        {
            _mana.Remove(
                playerId
            );

            _deadPlayers.Remove(
                playerId
            );
        }

        public void ResetPlayer(
            ushort playerId)
        {
            if (!HasPlayer(playerId))
            {
                return;
            }

            _mana[playerId] = 0;
        }

        public bool TryGiveKillReward(
            ushort killerId,
            ushort victimId,
            out int reward)
        {
            reward = 0;

            if (!HasPlayer(victimId))
            {
                return false;
            }

            if (!HasPlayer(killerId))
            {
                return false;
            }

            if (killerId == victimId)
            {
                return false;
            }

            // Выбывший игрок не может убивать.
            if (_deadPlayers.Contains(killerId))
            {
                return false;
            }

            // Нельзя повторно убить уже погибшего игрока.
            if (_deadPlayers.Contains(victimId))
            {
                return false;
            }

            int victimMana =
                GetMana(victimId);

            // Награда:
            // минимум 33 MP,
            // либо вся мана жертвы, если её больше 33.
            reward = Math.Max(
                OneShotConstants.CastMana,
                victimMana
            );

            // Жертва становится мёртвой.
            _deadPlayers.Add(
                victimId
            );

            // Вся её мана исчезает.
            _mana[victimId] = 0;

            // Награда добавляется убийце.
            int killerMana =
                GetMana(killerId);

            _mana[killerId] =
                killerMana + reward;

            return true;
        }

        public int SpendMana(
            ushort playerId,
            int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            if (!HasPlayer(playerId))
            {
                return 0;
            }

            // Выбывший игрок больше не должен
            // расходовать игровую ману.
            if (_deadPlayers.Contains(playerId))
            {
                return 0;
            }

            int currentMana =
                GetMana(playerId);

            int spent =
                Math.Min(
                    currentMana,
                    amount
                );

            _mana[playerId] =
                currentMana - spent;

            return spent;
        }

        public void SetMana(
            ushort playerId,
            int mana)
        {
            if (!HasPlayer(playerId))
            {
                return;
            }

            if (mana < 0)
            {
                mana = 0;
            }

            _mana[playerId] = mana;
        }

        public void Clear()
        {
            _mana.Clear();
            _deadPlayers.Clear();
        }
    }
}