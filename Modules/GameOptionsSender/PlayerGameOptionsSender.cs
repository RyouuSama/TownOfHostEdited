using AmongUs.GameOptions;
using Hazel;
using Il2CppSystem.Linq;
using InnerNet;
using System.Linq;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using Mathf = UnityEngine.Mathf;
using static TOHE.Roles.Impostor.AURoleOptions;

namespace TOHE.Modules
{
    public class PlayerGameOptionsSender : GameOptionsSender
    {
        public static void SetDirty(PlayerControl player) => SetDirty(player.PlayerId);
        public static void SetDirty(byte playerId) =>
            AllSenders.OfType<PlayerGameOptionsSender>()
                .Where(sender => sender.player.PlayerId == playerId)
                .ToList().ForEach(sender => sender.SetDirty());
        public static void SetDirtyToAll() =>
            AllSenders.OfType<PlayerGameOptionsSender>()
                .ToList().ForEach(sender => sender.SetDirty());

        public override IGameOptions BasedGameOptions =>
            Main.RealOptionsData.Restore(new NormalGameOptionsV07(new UnityLogger().Cast<ILogger>()).Cast<IGameOptions>());
        public override bool IsDirty { get; protected set; }

        public PlayerControl player;

        public PlayerGameOptionsSender(PlayerControl player)
        {
            this.player = player;
        }
        public void SetDirty() => IsDirty = true;

        public override void SendGameOptions()
        {
            if (player.AmOwner)
            {
                var opt = BuildGameOptions();
                foreach (var com in GameManager.Instance.LogicComponents.OfType<LogicOptions>())
                {
                    com.SetGameOptions(opt);
                }
                GameOptionsManager.Instance.CurrentGameOptions = opt;
            }
            else
            {
                base.SendGameOptions();
            }
        }

        public override void SendOptionsArray(byte[] optionArray)
        {
            for (byte i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
            {
                if (GameManager.Instance.LogicComponents[i] is LogicOptions)
                {
                    SendOptionsArray(optionArray, i, player.GetClientId());
                }
            }
        }

        public static void RemoveSender(PlayerControl player)
        {
            var sender = AllSenders.OfType<PlayerGameOptionsSender>()
                .FirstOrDefault(sender => sender.player.PlayerId == player.PlayerId);
            if (sender == null) return;
            sender.player = null;
            AllSenders.Remove(sender);
        }

        public override IGameOptions BuildGameOptions()
        {
            if (Main.RealOptionsData == null)
            {
                Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);
            }

            var opt = BasedGameOptions;
            AURoleOptions.SetOpt(opt);
            var state = Main.PlayerStates[player.PlayerId];
            opt.BlackOut(state.IsBlackOut);

            CustomRoles role = player.GetCustomRole();
            switch (role.GetCustomRoleTypes())
            {
                case CustomRoleTypes.Impostor:
                    ShapeshifterCooldown = DefaultShapeshiftCooldown.GetFloat();
                    break;
            }

            switch (role)
            {
                case CustomRoles.Terrorist:
                case CustomRoles.SabotageMaster:
                case CustomRoles.Mario:
                    EngineerCooldown = 0f;
                    EngineerInVentMaxTime = 0f;
                    break;
                case CustomRoles.ShapeMaster:
                    ShapeshifterCooldown = 0f;
                    ShapeshifterLeaveSkin = false;
                    ShapeshifterDuration = ShapeMasterShapeshiftDuration.GetFloat();
                    break;
                case CustomRoles.Warlock:
                    ShapeshifterCooldown = 0f;
                    ShapeshifterLeaveSkin = false;
                    break;
                case CustomRoles.Mechanic:
                    opt.ImpostorLightMod = Mathf.Clamp01(MechanicLightMod.GetFloat());
                    break;
                case CustomRoles.Jackal:
                    ShapeshifterCooldown = 0f;
                    ShapeshifterLeaveSkin = false;
                    opt.ImpostorLightMod = JackalLightMod.GetFloat();
                    break;
                case CustomRoles.SerialKiller:
                    ShapeshifterCooldown = 0f;
                    ShapeshifterLeaveSkin = false;
                    opt.ImpostorLightMod = SerialKillerLightMod.GetFloat();
                    break;
                case CustomRoles.Assassin:
                    opt.ImpostorLightMod = AssassinLightMod.GetFloat();
                    break;
                case CustomRoles.Sniper:
                    opt.ImpostorLightMod = SniperLightMod.GetFloat();
                    break;
                case CustomRoles.Swapper:
                    ShapeshifterCooldown = 0f;
                    ShapeshifterLeaveSkin = false;
                    opt.ImpostorLightMod = SwapperLightMod.GetFloat();
                    break;
            }

            if (opt.GameLengthType != GameLengthTypes.Normal)
            {
                var baseMeetingTime = opt.MeetingTime;
                opt.MeetingTime = baseMeetingTime * (int)opt.GameLengthType;
            }

            return opt;
        }
    }
}
