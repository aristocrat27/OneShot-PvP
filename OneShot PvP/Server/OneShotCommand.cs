using Hkmp.Api.Command.Server;

namespace OneShotPvP.Server
{
    internal sealed class OneShotCommand : IServerCommand
    {
        private readonly RoundManager _roundManager;

        public OneShotCommand(
            RoundManager roundManager)
        {
            _roundManager = roundManager;
        }

        public string Trigger
        {
            get { return "/start"; }
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
                    "Раунд уже идёт."
                );

                return;
            }

            if (!_roundManager.StartRound())
            {
                commandSender.SendMessage(
                    "Не удалось начать раунд. " +
                    "Нужно минимум 2 игрока."
                );

                return;
            }

            commandSender.SendMessage(
                "Раунд запущен."
            );
        }
    }
}