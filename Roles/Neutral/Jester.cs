﻿using AmongUs.GameOptions;
using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Jester : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 14400;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    
    public override CustomRoles ThisRoleBase => CanVent.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    private static OptionItem CanUseMeetingButton;
    private static OptionItem HasImpostorVision;
    private static OptionItem CanVent;
    private static OptionItem CantMoveInVents;
    private static OptionItem MeetingsNeededForWin;
    private static OptionItem HideJesterVote;
    public static OptionItem SunnyboyChance;

    private readonly HashSet<int> RememberBlockedVents = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Jester);
        CanUseMeetingButton = BooleanOptionItem.Create(Id + 2, GeneralOption.CanUseMeetingButton, false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        CanVent = BooleanOptionItem.Create(Id + 3, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        CantMoveInVents = BooleanOptionItem.Create(Id + 10, GeneralOption.CantMoveOnVents, true, TabGroup.NeutralRoles, false)
            .SetParent(CanVent);
        HasImpostorVision = BooleanOptionItem.Create(Id + 4, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        HideJesterVote = BooleanOptionItem.Create(Id + 5, GeneralOption.HideVote, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        MeetingsNeededForWin = IntegerOptionItem.Create(Id + 6, "MeetingsNeededForWin", new(0, 10, 1), 0, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester])
            .SetValueFormat(OptionFormat.Times);
        SunnyboyChance = IntegerOptionItem.Create(Id + 7, "SunnyboyChance", new(0, 100, 5), 0, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester])
            .SetValueFormat(OptionFormat.Percent);
    }
    public override void Init()
    {
        PlayerIds.Clear();
        RememberBlockedVents.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 1f;
        AURoleOptions.EngineerInVentMaxTime = 0f;

        opt.SetVision(HasImpostorVision.GetBool());
    }
    public override bool HideVote(PlayerVoteArea votedPlayer) => HideJesterVote.GetBool();
    public override bool OnCheckStartMeeting(PlayerControl reporter) => CanUseMeetingButton.GetBool();

    public override void OnCoEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!CantMoveInVents.GetBool()) return;

        foreach (var vent in ShipStatus.Instance.AllVents)
        {
            if (vent.Id == ventId) continue;

            RememberBlockedVents.Add(vent.Id);
            CustomRoleManager.BlockedVentsList[physics.myPlayer.PlayerId].Add(vent.Id);
        }
    }
    public override void OnExitVent(PlayerControl pc, int ventId)
    {
        ResetBlockedVent();
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        ResetBlockedVent();
    }
    private void ResetBlockedVent()
    {
        if (!CantMoveInVents.GetBool() || _Player == null) return;

        foreach (var ventId in RememberBlockedVents)
        {
            CustomRoleManager.BlockedVentsList[_Player.PlayerId].Remove(ventId);
        }
        RememberBlockedVents.Clear();
    }

    public override void CheckExile(NetworkedPlayerInfo exiled, ref bool DecidedWinner, bool isMeetingHud, ref string name)
    {
        if (MeetingsNeededForWin.GetInt() <= Main.MeetingsPassed)
        {
            if (isMeetingHud)
            {
                name = string.Format(Translator.GetString("ExiledJester"), Main.LastVotedPlayer, Utils.GetDisplayRoleAndSubName(exiled.PlayerId, exiled.PlayerId, true));
                DecidedWinner = true;
            }
            else
            {
                if (!CustomWinnerHolder.CheckForConvertedWinner(exiled.PlayerId))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jester);
                    CustomWinnerHolder.WinnerIds.Add(exiled.PlayerId);
                }

                // Check exile target Executioner
                foreach (var executioner in Executioner.playerIdList)
                {
                    if (Executioner.IsTarget(executioner, exiled.PlayerId))
                    {
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Executioner);
                        CustomWinnerHolder.WinnerIds.Add(executioner);
                    }
                }
                DecidedWinner = true;
            }
        }
        else if (CEMode.GetInt() == 2 && isMeetingHud)
            name += string.Format(Translator.GetString("JesterMeetingLoose"), MeetingsNeededForWin.GetInt() + 1);
    }
}
