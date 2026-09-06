using Hkmp.Api.Command.Server;

namespace OneShotPvP.Server
{
    internal sealed class ServerManaTestCommand : IServerCommand
    {
        public string Trigger
        {
            get { return "/os_test"; }
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
            ServerManaManager mana =
                new ServerManaManager();

            const ushort playerA = 1;
            const ushort playerB = 2;

            try
            {
                // -------------------------------------------------
                // Test 1:
                // 33 -> kill player with 33 -> 66
                // -------------------------------------------------

                mana.AddPlayer(playerA);
                mana.AddPlayer(playerB);

                AssertEqual(
                    33,
                    mana.GetMana(playerA),
                    "Test 1: initial killer mana"
                );

                AssertEqual(
                    33,
                    mana.GetMana(playerB),
                    "Test 1: initial victim mana"
                );

                int reward;

                bool accepted =
                    mana.TryGiveKillReward(
                        playerA,
                        playerB,
                        out reward
                    );

                AssertTrue(
                    accepted,
                    "Test 1: kill accepted"
                );

                AssertEqual(
                    33,
                    reward,
                    "Test 1: reward"
                );

                AssertEqual(
                    66,
                    mana.GetMana(playerA),
                    "Test 1: killer mana after kill"
                );

                AssertEqual(
                    0,
                    mana.GetMana(playerB),
                    "Test 1: victim mana after death"
                );

                AssertTrue(
                    mana.IsDead(playerB),
                    "Test 1: victim marked dead"
                );

                // -------------------------------------------------
                // Test 2:
                // 66 -> kill player with 33 -> 99
                // -------------------------------------------------

                mana.Clear();

                mana.AddPlayer(playerA);
                mana.AddPlayer(playerB);

                mana.SetMana(playerA, 66);

                accepted =
                    mana.TryGiveKillReward(
                        playerA,
                        playerB,
                        out reward
                    );

                AssertTrue(
                    accepted,
                    "Test 2: kill accepted"
                );

                AssertEqual(
                    33,
                    reward,
                    "Test 2: reward"
                );

                AssertEqual(
                    99,
                    mana.GetMana(playerA),
                    "Test 2: killer mana after kill"
                );

                // -------------------------------------------------
                // Test 3:
                // 99 -> spend 33 -> 66
                // -------------------------------------------------

                mana.Clear();

                mana.AddPlayer(playerA);

                mana.SetMana(playerA, 99);

                int spent =
                    mana.SpendMana(
                        playerA,
                        OneShotConstants.CastMana
                    );

                AssertEqual(
                    33,
                    spent,
                    "Test 3: spent amount"
                );

                AssertEqual(
                    66,
                    mana.GetMana(playerA),
                    "Test 3: mana after cast"
                );

                // -------------------------------------------------
                // Test 4:
                // 66 + kill player with 66 -> 132
                // -------------------------------------------------

                mana.Clear();

                mana.AddPlayer(playerA);
                mana.AddPlayer(playerB);

                mana.SetMana(playerA, 66);
                mana.SetMana(playerB, 66);

                accepted =
                    mana.TryGiveKillReward(
                        playerA,
                        playerB,
                        out reward
                    );

                AssertTrue(
                    accepted,
                    "Test 4: kill accepted"
                );

                AssertEqual(
                    66,
                    reward,
                    "Test 4: reward"
                );

                AssertEqual(
                    132,
                    mana.GetMana(playerA),
                    "Test 4: killer mana after kill"
                );

                // -------------------------------------------------
                // Test 5:
                // 0 + kill player with 0 -> 33
                // -------------------------------------------------

                mana.Clear();

                mana.AddPlayer(playerA);
                mana.AddPlayer(playerB);

                mana.SetMana(playerA, 0);
                mana.SetMana(playerB, 0);

                accepted =
                    mana.TryGiveKillReward(
                        playerA,
                        playerB,
                        out reward
                    );

                AssertTrue(
                    accepted,
                    "Test 5: kill accepted"
                );

                AssertEqual(
                    33,
                    reward,
                    "Test 5: reward"
                );

                AssertEqual(
                    33,
                    mana.GetMana(playerA),
                    "Test 5: killer mana after kill"
                );

                // -------------------------------------------------
                // Test 6:
                // Same death cannot be processed twice
                // -------------------------------------------------

                accepted =
                    mana.TryGiveKillReward(
                        playerA,
                        playerB,
                        out reward
                    );

                AssertTrue(
                    !accepted,
                    "Test 6: duplicate death rejected"
                );

                AssertEqual(
                    33,
                    mana.GetMana(playerA),
                    "Test 6: mana unchanged"
                );

                // -------------------------------------------------
                // Test 7:
                // Dead player cannot spend mana
                // -------------------------------------------------

                mana.Clear();

                mana.AddPlayer(playerA);
                mana.AddPlayer(playerB);

                mana.SetMana(playerB, 66);

                accepted =
                    mana.TryGiveKillReward(
                        playerA,
                        playerB,
                        out reward
                    );

                AssertTrue(
                    accepted,
                    "Test 7: kill accepted"
                );

                spent =
                    mana.SpendMana(
                        playerB,
                        33
                    );

                AssertEqual(
                    0,
                    spent,
                    "Test 7: dead player cannot spend mana"
                );

                AssertEqual(
                    0,
                    mana.GetMana(playerB),
                    "Test 7: dead player mana"
                );

                // -------------------------------------------------
                // All tests passed
                // -------------------------------------------------

                commandSender.SendMessage(
                    "OneShotPvP: ВСЕ ТЕСТЫ МАНЫ ПРОЙДЕНЫ."
                );
            }
            catch (System.Exception exception)
            {
                commandSender.SendMessage(
                    "OneShotPvP: ТЕСТ ПРОВАЛЕН."
                );

                commandSender.SendMessage(
                    exception.Message
                );
            }
        }

        private static void AssertEqual(
            int expected,
            int actual,
            string testName)
        {
            if (expected != actual)
            {
                throw new System.Exception(
                    testName +
                    " | expected: " +
                    expected +
                    ", actual: " +
                    actual
                );
            }
        }

        private static void AssertTrue(
            bool value,
            string testName)
        {
            if (!value)
            {
                throw new System.Exception(
                    testName +
                    " | expected: true"
                );
            }
        }
    }
}