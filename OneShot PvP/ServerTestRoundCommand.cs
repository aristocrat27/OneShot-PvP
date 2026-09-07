using Hkmp.Api.Command.Server;
using Hkmp.Api.Server;

namespace OneShotPvP.Server
{
    internal sealed class ServerTestRoundCommand : IServerCommand
    {
        private readonly IServerApi _serverApi;
        private readonly ServerManaManager _manaManager;
        private readonly ServerNetManager _network;
        private readonly RoundManager _roundManager;

        public ServerTestRoundCommand(
            IServerApi serverApi,
            ServerManaManager manaManager,
            ServerNetManager network,
            RoundManager roundManager)
        {
            _serverApi = serverApi;
            _manaManager = manaManager;
            _network = network;
            _roundManager = roundManager;
        }

        public string Trigger
        {
            get { return "/os_test_round"; }
        }

        public string[] Aliases
        {
            get { return new string[0]; }
        }

        public bool AuthorizedOnly
        {
            get { return true; }
        }

        public void Execute(
            ICommandSender commandSender,
            string[] arguments)
        {
            if (_roundManager.IsRoundActive)
            {
                commandSender.SendMessage(
                    "OneShotPvP: раунд уже идёт."
                );

                return;
            }

            if (_serverApi.ServerManager.Players.Count != 1)
            {
                commandSender.SendMessage(
                    "OneShotPvP: для /os_test_round " +
                    "должен быть подключён ровно 1 реальный игрок."
                );

                return;
            }

            IServerPlayer player = null;

            foreach (IServerPlayer currentPlayer
                in _serverApi.ServerManager.Players)
            {
                player = currentPlayer;

                break;
            }

            if (player == null)
            {
                commandSender.SendMessage(
                    "OneShotPvP: реальный игрок не найден."
                );

                return;
            }

            /*
             * Два виртуальных игрока.
             *
             * Они существуют только внутри серверной
             * тестовой логики и не являются реальными
             * HKMP-клиентами.
             */
            ushort virtualPlayer1 = 60000;
            ushort virtualPlayer2 = 60001;

            if (player.Id == virtualPlayer1 ||
                player.Id == virtualPlayer2)
            {
                commandSender.SendMessage(
                    "OneShotPvP: ID тестовых игроков " +
                    "совпали с реальным игроком."
                );

                return;
            }

            if (!_roundManager.StartTestRound(
                player.Id,
                virtualPlayer1,
                virtualPlayer2))
            {
                commandSender.SendMessage(
                    "OneShotPvP: не удалось запустить " +
                    "тестовый раунд."
                );

                return;
            }

            Modding.Logger.Log(
                "[OneShotPvP] Starting full single-player " +
                "round simulation."
            );

            /*
             * --------------------------------------------------
             * ШАГ 1
             *
             * Проверяем стартовую ману.
             * --------------------------------------------------
             */

            int initialRealMana =
                _manaManager.GetMana(
                    player.Id
                );

            int initialVirtualMana1 =
                _manaManager.GetMana(
                    virtualPlayer1
                );

            int initialVirtualMana2 =
                _manaManager.GetMana(
                    virtualPlayer2
                );

            Modding.Logger.Log(
                "[OneShotPvP] Test step 1: initial mana. " +
                "Real=" +
                initialRealMana +
                " Virtual1=" +
                initialVirtualMana1 +
                " Virtual2=" +
                initialVirtualMana2
            );

            if (initialRealMana != OneShotConstants.CastMana ||
                initialVirtualMana1 != OneShotConstants.CastMana ||
                initialVirtualMana2 != OneShotConstants.CastMana)
            {
                commandSender.SendMessage(
                    "OneShotPvP: ОШИБКА теста начальной маны. " +
                    "Ожидалось 33/33/33."
                );

                _roundManager.Reset();

                return;
            }

            /*
             * --------------------------------------------------
             * ШАГ 2
             *
             * Реальный игрок убивает первого виртуального.
             *
             * 33 + 33 = 66
             * --------------------------------------------------
             */

            int reward1;

            bool kill1 =
                _manaManager.TryGiveKillReward(
                    player.Id,
                    virtualPlayer1,
                    out reward1
                );

            int realManaAfterKill1 =
                _manaManager.GetMana(
                    player.Id
                );

            int victimManaAfterKill1 =
                _manaManager.GetMana(
                    virtualPlayer1
                );

            Modding.Logger.Log(
                "[OneShotPvP] Test kill #1. " +
                "Accepted=" +
                kill1 +
                " Reward=" +
                reward1 +
                " RealMana=" +
                realManaAfterKill1 +
                " VictimMana=" +
                victimManaAfterKill1
            );

            if (!kill1 ||
                reward1 != OneShotConstants.CastMana ||
                realManaAfterKill1 != 66 ||
                victimManaAfterKill1 != 0)
            {
                commandSender.SendMessage(
                    "OneShotPvP: ОШИБКА теста 33 -> 66."
                );

                _roundManager.Reset();

                return;
            }

            /*
             * ВАЖНО:
             *
             * Реальный игрок должен увидеть новую ману.
             *
             * В обычной игре это делает OnDeathReport(),
             * но здесь убийство симулируется напрямую.
             */
            _network.SendMana(
                player.Id
            );

            Modding.Logger.Log(
                "[OneShotPvP] Test kill #1 mana update sent. " +
                "PlayerId=" +
                player.Id +
                " Mana=" +
                realManaAfterKill1
            );

            /*
             * Удаляем первого виртуального игрока
             * из списка живых.
             */
            _roundManager.OnPlayerDeath(
                virtualPlayer1
            );

            if (!_roundManager.IsRoundActive)
            {
                commandSender.SendMessage(
                    "OneShotPvP: ОШИБКА — раунд завершился " +
                    "слишком рано после первого убийства."
                );

                _roundManager.Reset();

                return;
            }

            /*
             * --------------------------------------------------
             * ШАГ 3
             *
             * Реальный игрок убивает второго виртуального.
             *
             * 66 + 33 = 99
             * --------------------------------------------------
             */

            int reward2;

            bool kill2 =
                _manaManager.TryGiveKillReward(
                    player.Id,
                    virtualPlayer2,
                    out reward2
                );

            int realManaAfterKill2 =
                _manaManager.GetMana(
                    player.Id
                );

            int victimManaAfterKill2 =
                _manaManager.GetMana(
                    virtualPlayer2
                );

            Modding.Logger.Log(
                "[OneShotPvP] Test kill #2. " +
                "Accepted=" +
                kill2 +
                " Reward=" +
                reward2 +
                " RealMana=" +
                realManaAfterKill2 +
                " VictimMana=" +
                victimManaAfterKill2
            );

            if (!kill2 ||
                reward2 != OneShotConstants.CastMana ||
                realManaAfterKill2 != 99 ||
                victimManaAfterKill2 != 0)
            {
                commandSender.SendMessage(
                    "OneShotPvP: ОШИБКА теста 66 -> 99."
                );

                _roundManager.Reset();

                return;
            }

            /*
             * Отправляем реальные 99 MP клиенту.
             */
            _network.SendMana(
                player.Id
            );

            Modding.Logger.Log(
                "[OneShotPvP] Test kill #2 mana update sent. " +
                "PlayerId=" +
                player.Id +
                " Mana=" +
                realManaAfterKill2
            );

            /*
             * Удаляем второго виртуального игрока.
             *
             * Теперь остаётся только реальный игрок.
             *
             * RoundManager должен автоматически:
             *
             * 1. определить победителя;
             * 2. завершить раунд;
             * 3. отправить RoundEnd;
             * 4. восстановить PvP settings;
             * 5. очистить серверную ману.
             */
            _roundManager.OnPlayerDeath(
                virtualPlayer2
            );

            bool roundEnded =
                !_roundManager.IsRoundActive;

            int finalMana =
                _manaManager.GetMana(
                    player.Id
                );

            Modding.Logger.Log(
                "[OneShotPvP] Full test finished. " +
                "RoundEnded=" +
                roundEnded +
                " FinalServerManaAfterRound=" +
                finalMana
            );

            if (!roundEnded)
            {
                commandSender.SendMessage(
                    "OneShotPvP: ОШИБКА — раунд не завершился."
                );

                _roundManager.Reset();

                return;
            }

            /*
             * После EndRound серверная мана должна быть очищена.
             */
            if (finalMana != 0)
            {
                commandSender.SendMessage(
                    "OneShotPvP: ОШИБКА — ServerManaManager " +
                    "не очистил ману после завершения раунда."
                );

                _roundManager.Reset();

                return;
            }

            commandSender.SendMessage(
                "OneShotPvP: ТЕСТ УСПЕШЕН! " +
                "33 -> 66 -> 99, мана отправлена клиенту, " +
                "оба убийства приняты, победитель определён, " +
                "раунд завершён."
            );

            Modding.Logger.Log(
                "[OneShotPvP] Full single-player round " +
                "simulation PASSED."
            );
        }
    }
}