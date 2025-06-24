using Artisan.CraftingLogic.CraftData;
using Artisan.RawInformation;
using Artisan.RawInformation.Character;
using Dalamud.Interface.Components;
using Dalamud.Interface.Textures.TextureWraps;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ImGuiNET;
using System;
using static Artisan.RawInformation.AddonExtensions;

namespace Artisan.CraftingLogic.Solvers;

public class ExpertSolverSettings
{
    public bool MaxIshgardRecipes;
    public bool UseReflectOpener;
    public bool MuMeIntensiveGood = true; // if true, we allow spending mume on intensive (400p) rather than rapid (500p) if good condition procs
    public bool MuMeIntensiveMalleable = false; // if true and we have malleable during mume, use intensive rather than hoping for rapid
    public bool MuMeIntensiveLastResort = true; // if true and we're on last step of mume, use intensive (forcing via H&S if needed) rather than hoping for rapid (unless we have centered)
    public bool MuMePrimedManip = false; // if true, allow using primed manipulation after veneration is up on mume
    public bool MuMeAllowObserve = false; // if true, observe rather than use actions during unfavourable conditions to conserve durability
    public int MuMeMinStepsForManip = 2; // if this or less rounds are remaining on mume, don't use manipulation under favourable conditions
    public int MuMeMinStepsForVene = 1; // if this or less rounds are remaining on mume, don't use veneration
    public int MidMinIQForHSPrecise = 10; // min iq stacks where we use h&s+precise; 10 to disable
    public bool MidBaitPliantWithObservePreQuality = true; // if true, when very low on durability and without manip active during pre-quality phase, we use observe rather than normal manip
    public bool MidBaitPliantWithObserveAfterIQ = true; // if true, when very low on durability and without manip active after iq has 10 stacks, we use observe rather than normal manip or inno+finnesse
    public bool MidPrimedManipPreQuality = true; // if true, allow using primed manipulation during pre-quality phase
    public bool MidPrimedManipAfterIQ = true; // if true, allow using primed manipulation during after iq has 10 stacks
    public bool MidKeepHighDuraUnbuffed = true; // if true, observe rather than use actions during unfavourable conditions to conserve durability when no buffs are active
    public bool MidKeepHighDuraVeneration = false; // if true, observe rather than use actions during unfavourable conditions to conserve durability when veneration is active
    public bool MidAllowVenerationGoodOmen = true; // if true, we allow using veneration during iq phase if we lack a lot of progress on good omen
    public bool MidAllowVenerationAfterIQ = true; // if true, we allow using veneration after iq is fully stacked if we still lack a lot of progress
    public bool MidAllowIntensiveUnbuffed = false; // if true, we allow spending good condition on intensive if we still need progress when no buffs are active
    public bool MidAllowIntensiveVeneration = false; // if true, we allow spending good condition on intensive if we still need progress when veneration is active
    public bool MidAllowPrecise = true; // if true, we allow spending good condition on precise touch if we still need iq
    public bool MidAllowSturdyPreсise = false; // if true,we consider sturdy+h&s+precise touch a good move for building iq
    public bool MidAllowCenteredHasty = true; // if true, we consider centered hasty touch a good move for building iq (85% reliability)
    public bool MidAllowSturdyHasty = true; // if true, we consider sturdy hasty touch a good move for building iq (50% reliability), otherwise we use combo
    public bool MidAllowGoodPrep = true; // if true, we consider prep touch a good move for finisher under good+inno+gs
    public bool MidAllowSturdyPrep = true; // if true, we consider prep touch a good move for finisher under sturdy+inno
    public bool MidGSBeforeInno = true; // if true, we start quality combos with gs+inno rather than just inno
    public bool MidFinishProgressBeforeQuality = false; // if true, at 10 iq we first finish progress before starting on quality
    public bool MidObserveGoodOmenForTricks = false; // if true, we'll observe on good omen where otherwise we'd use tricks on good
    public bool FinisherBaitGoodByregot = true; // if true, use careful observations to try baiting good byregot
    public bool EmergencyCPBaitGood = false; // if true, we allow spending careful observations to try baiting good for tricks when we really lack cp
    public bool UseMaterialMiracle = false;

    [NonSerialized]
    public IDalamudTextureWrap? expertIcon;

    public ExpertSolverSettings()
    {
        var tex = Svc.PluginInterface.UiBuilder.LoadUld("ui/uld/RecipeNoteBook.uld");
        expertIcon = tex.LoadTexturePart("ui/uld/RecipeNoteBook_hr1.tex", 14);
    }

    public bool Draw()
    {
        ImGui.TextWrapped($"专家配方解算器并不是标准解算器的替代品。它仅用于专家配方。");
        if (expertIcon != null)
        {
            ImGui.TextWrapped($"该解算器仅适用出现在制作日志中具有");
            ImGui.SameLine();
            ImGui.Image(expertIcon.ImGuiHandle, expertIcon.Size, new(0, 0), new(1, 1), new(0.94f, 0.57f, 0f, 1f));
            ImGui.SameLine();
            ImGui.TextWrapped($"图标的食谱。");
        }
        bool changed = false;
        ImGui.Indent();
        if (ImGui.CollapsingHeader("起手设置"))
        {
            changed |= ImGui.Checkbox($"使用 [{Skills.Reflect.NameOfAction()}] 代替 [{Skills.MuscleMemory.NameOfAction()}] 作为起手", ref UseReflectOpener);
            changed |= ImGui.Checkbox($"如果在使用 [{Skills.MuscleMemory.NameOfAction()}] 后处于 [{Condition.Good.ToLocalizedString()}] {ConditionString}，使用 [{Skills.IntensiveSynthesis.NameOfAction()}]（400%）而不是 [{Skills.RapidSynthesis.NameOfAction()}]（500%）", ref MuMeIntensiveGood);
            changed |= ImGui.Checkbox($"如果在 [{Skills.MuscleMemory.NameOfAction()}] 期间出现 [{Condition.Malleable.ToLocalizedString()}] {ConditionString}，使用 [{Skills.HeartAndSoul.NameOfAction()}] + [{Skills.IntensiveSynthesis.NameOfAction()}] ", ref MuMeIntensiveMalleable);
            changed |= ImGui.Checkbox($"如果在 [{Skills.MuscleMemory.NameOfAction()}] 的最后一步时不是 [{Condition.Centered.ToLocalizedString()}] {ConditionString}，使用 [{Skills.IntensiveSynthesis.NameOfAction()}]（如果可以，强制使用 [{Skills.HeartAndSoul.NameOfAction()}] ）", ref MuMeIntensiveLastResort);
            changed |= ImGui.Checkbox($"如果 [{Skills.Veneration.NameOfAction()}] 还处于激活状态，在 [{Condition.Primed.ToLocalizedString()}] {ConditionString}时使用 [{Skills.Manipulation.NameOfAction()}] 。", ref MuMePrimedManip);
            changed |= ImGui.Checkbox($"在出现不利状态时使用 [{Skills.Observe.NameOfAction()}] ，而不是使用 [{Skills.RapidSynthesis.NameOfAction()}] 来消耗{DurabilityString}。", ref MuMeAllowObserve);
            ImGui.Text($"仅当 [{Skills.MuscleMemory.NameOfAction()}] 的剩余步骤超过此数量时，才允许使用 [{Skills.Manipulation.NameOfAction()}] 。");
            ImGui.PushItemWidth(250);
            changed |= ImGui.SliderInt("###MumeMinStepsForManip", ref MuMeMinStepsForManip, 0, 5);
            ImGui.Text($"仅当 [{Skills.MuscleMemory.NameOfAction()}] 的剩余步骤超过此数量时，才允许使用 [{Skills.Veneration.NameOfAction()}] 。");
            ImGui.PushItemWidth(250);
            changed |= ImGui.SliderInt("###MuMeMinStepsForVene", ref MuMeMinStepsForVene, 0, 5);
        }
        if (ImGui.CollapsingHeader("主循环设置"))
        {
            ImGui.Text($"[{Buffs.InnerQuiet.NameOfBuff()}] 至少有多少层时才为 [{Skills.PreciseTouch.NameOfAction()}] 使用 [{Skills.HeartAndSoul.NameOfAction()}]（10为禁用）");
            ImGui.PushItemWidth(250);
            changed |= ImGui.SliderInt($"###MidMinIQForHSPrecise", ref MidMinIQForHSPrecise, 0, 10);
            changed |= ImGui.Checkbox($"低{DurabilityString}时，优先选择 [{Skills.Observe.NameOfAction()}] 而不是非-[{Condition.Pliant.ToLocalizedString()}] [{Skills.Manipulation.NameOfAction()}]，在 {Buffs.InnerQuiet.NameOfBuff()} 有 10 层之前", ref MidBaitPliantWithObservePreQuality);
            changed |= ImGui.Checkbox($"低{DurabilityString}时，优先选择 [{Skills.Observe.NameOfAction()}] 而不是非-[{Condition.Pliant.ToLocalizedString()}] [{Skills.Manipulation.NameOfAction()}] / [{Skills.Innovation.NameOfAction()}]+[{Skills.TrainedFinesse.NameOfAction()}]，在 {Buffs.InnerQuiet.NameOfBuff()} 有 10 层之后", ref MidBaitPliantWithObserveAfterIQ);
            changed |= ImGui.Checkbox($"在 [{Condition.Primed.ToLocalizedString()}] {ConditionString} 下使用 [{Skills.Manipulation.NameOfAction()}]，在 {Buffs.InnerQuiet.NameOfBuff()} 有 10 层之前", ref MidPrimedManipPreQuality);
            changed |= ImGui.Checkbox($"在 [{Condition.Primed.ToLocalizedString()}] {ConditionString} 下使用 [{Skills.Manipulation.NameOfAction()}]，在 {Buffs.InnerQuiet.NameOfBuff()} 有 10 层之后，如果有足够的 CP 以良好利用 {DurabilityString}", ref MidPrimedManipAfterIQ);
            changed |= ImGui.Checkbox($"在不利的 {ConditionString} 下允许使用 [{Skills.Observe.NameOfAction()}]，没有增益效果", ref MidKeepHighDuraUnbuffed);
            changed |= ImGui.Checkbox($"在不利的 {ConditionString} 下，在 {Buffs.Veneration.NameOfBuff()} 下允许使用 [{Skills.Observe.NameOfAction()}]", ref MidKeepHighDuraVeneration);
            changed |= ImGui.Checkbox($"如果我们在 [{Condition.GoodOmen.ToLocalizedString()}] 上还有大 {ProgressString} 赤字（超过 [{Skills.IntensiveSynthesis.NameOfAction()}] 可以完成的），则允许使用 [{Skills.Veneration.NameOfAction()}]", ref MidAllowVenerationGoodOmen);
            changed |= ImGui.Checkbox($"在 {Buffs.InnerQuiet.NameOfBuff()} 有 10 层之后，如果我们在 [{Condition.GoodOmen.ToLocalizedString()}] 上还有大 {ProgressString} 赤字（超过 [{Skills.RapidSynthesis.NameOfAction()}] 可以完成的），则允许使用 [{Skills.Veneration.NameOfAction()}]", ref MidAllowVenerationAfterIQ);
            changed |= ImGui.Checkbox($"在没有增益效果的情况下，花费 [{Condition.Good.ToLocalizedString()}] {ConditionString} 用于 [{Skills.IntensiveSynthesis.NameOfAction()}]，如果我们需要更多 {ProgressString}", ref MidAllowIntensiveUnbuffed);
            changed |= ImGui.Checkbox($"在 [{Skills.Veneration.NameOfAction()}] 下，花费 [{Condition.Good.ToLocalizedString()}] {ConditionString} 用于 [{Skills.IntensiveSynthesis.NameOfAction()}]，如果我们需要更多 {ProgressString}", ref MidAllowIntensiveVeneration);
            changed |= ImGui.Checkbox($"如果我们需要更多 {Buffs.InnerQuiet.NameOfBuff()} 层，花费 [{Condition.Good.ToLocalizedString()}] {ConditionString} 用于 [{Skills.PreciseTouch.NameOfAction()}]", ref MidAllowPrecise);
            changed |= ImGui.Checkbox($"考虑使用 [{Condition.Sturdy.ToLocalizedString()}] {ConditionString} 的 [{Skills.HeartAndSoul.NameOfAction()}] + [{Skills.PreciseTouch.NameOfAction()}]，作为积累 {Buffs.InnerQuiet.NameOfBuff()} 层的良好选择", ref MidAllowSturdyPreсise);
            changed |= ImGui.Checkbox($"考虑使用 [{Condition.Centered.ToLocalizedString()}] {ConditionString} 的 [{Skills.HastyTouch.NameOfAction()}]，作为积累 {Buffs.InnerQuiet.NameOfBuff()} 层的良好选择（85% 成功，10 {DurabilityString}）", ref MidAllowCenteredHasty);
            changed |= ImGui.Checkbox($"考虑使用 [{Condition.Sturdy.ToLocalizedString()}] {ConditionString} 的 [{Skills.HastyTouch.NameOfAction()}]，作为积累 {Buffs.InnerQuiet.NameOfBuff()} 层的良好选择（50% 成功，5 {DurabilityString}）", ref MidAllowSturdyHasty);
            changed |= ImGui.Checkbox($"在 [{Condition.Good.ToLocalizedString()}] {ConditionString} + {Buffs.Innovation.NameOfBuff()} + {Buffs.GreatStrides.NameOfBuff()} 的情况下，考虑使用 [{Skills.PreparatoryTouch.NameOfAction()}]，假设我们有足够的 {DurabilityString}", ref MidAllowGoodPrep);
            changed |= ImGui.Checkbox($"在 [{Condition.Sturdy.ToLocalizedString()}] {ConditionString} + {Buffs.Innovation.NameOfBuff()} 的情况下，考虑使用 [{Skills.PreparatoryTouch.NameOfAction()}]，假设我们有足够的 {DurabilityString}", ref MidAllowSturdyPrep);
            changed |= ImGui.Checkbox($"在 [{Skills.Innovation.NameOfAction()}] + {QualityString} 组合之前使用 [{Skills.GreatStrides.NameOfAction()}]", ref MidGSBeforeInno);
            changed |= ImGui.Checkbox($"在开始 {QualityString} 阶段之前完成 {ProgressString}", ref MidFinishProgressBeforeQuality);
            changed |= ImGui.Checkbox($"在 [{Condition.GoodOmen.ToLocalizedString()}] {ConditionString} 下使用 [{Skills.Observe.NameOfAction()}]，如果我们本来会在 [{Condition.Good.ToLocalizedString()}] {ConditionString} 上使用 [{Skills.TricksOfTrade.NameOfAction()}]", ref MidObserveGoodOmenForTricks);
        }
        ImGui.Unindent();
        changed |= ImGui.Checkbox("充分利用伊修加德重建配方，而不是仅仅达到最大品质断点。", ref MaxIshgardRecipes);
        ImGuiComponents.HelpMarker("这将尝试最大限度地提高质量，以获得更多的技巧点。");
        changed |= ImGui.Checkbox($"终结技：使用 {Skills.CarefulObservation.NameOfAction()} 为 {Condition.Good.ToLocalizedString()} {ConditionString} 争取一下 {Skills.ByregotsBlessing.NameOfAction()}", ref FinisherBaitGoodByregot);
        changed |= ImGui.Checkbox($"紧急情况：如果制作力不够用了，使用 {Skills.CarefulObservation.NameOfAction()} 为 [{Condition.Good.ToLocalizedString()} {ConditionString} 争取一下 {Skills.TricksOfTrade.NameOfAction()}", ref EmergencyCPBaitGood);
        changed |= ImGui.Checkbox($"在宇宙探索中使用材料奇迹", ref UseMaterialMiracle);
        if (ImGuiEx.ButtonCtrl("重置高难度配方设置到默认状态"))
        {
            P.Config.ExpertSolverConfig = new();
            changed |= true;
        }
        return changed;
    }
}
