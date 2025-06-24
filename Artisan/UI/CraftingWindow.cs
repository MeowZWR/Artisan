using Artisan.Autocraft;
using Artisan.CraftingLists;
using Artisan.CraftingLogic;
using Artisan.CraftingLogic.Solvers;
using Artisan.GameInterop;
using Artisan.RawInformation;
using Artisan.RawInformation.Character;
using Dalamud.Game.Gui.Toast;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ImGuiNET;
using System;

namespace Artisan.UI
{
    internal class CraftingWindow : Window, IDisposable
    {
        public bool RepeatTrial;
        private DateTime _estimatedCraftEnd;

        public CraftingWindow() : base("Artisan Crafting Window###MainCraftWindow", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
        {
            IsOpen = true;
            ShowCloseButton = false;
            RespectCloseHotkey = false;
            this.SizeConstraints = new()
            {
                MinimumSize = new System.Numerics.Vector2(150f, 0f),
                MaximumSize = new System.Numerics.Vector2(310f, 500f)
            };

            CraftingProcessor.SolverStarted += OnSolverStarted;
            CraftingProcessor.SolverFailed += OnSolverFailed;
            CraftingProcessor.SolverFinished += OnSolverFinished;
            CraftingProcessor.RecommendationReady += OnRecommendationReady;
        }

        public void Dispose()
        {
            CraftingProcessor.SolverStarted -= OnSolverStarted;
            CraftingProcessor.SolverFailed -= OnSolverFailed;
            CraftingProcessor.SolverFinished -= OnSolverFinished;
            CraftingProcessor.RecommendationReady -= OnRecommendationReady;
        }

        public override bool DrawConditions()
        {
            return P.PluginUi.CraftingVisible;
        }

        public override void PreDraw()
        {
            if (!P.Config.DisableTheme)
            {
                P.Style.Push();
                P.StylePushed = true;
            }
        }

        public override void PostDraw()
        {
            if (P.StylePushed)
            {
                P.Style.Pop();
                P.StylePushed = false;
            }
        }

        public override void Draw()
        {
            if (!P.Config.DisableHighlightedAction)
                Hotbars.MakeButtonsGlow(CraftingProcessor.NextRec.Action);

            if (ImGuiEx.AddHeaderIcon("OpenConfig", FontAwesomeIcon.Cog, new ImGuiEx.HeaderIconOptions() { Tooltip = "打开设置" }))
            {
                P.PluginUi.IsOpen = true;
            }

            if (Crafting.CurCraft != null && !Crafting.CurCraft.CraftExpert && Crafting.CurRecipe?.SecretRecipeBook.RowId > 0 && Crafting.CurCraft?.CraftLevel == Crafting.CurCraft?.StatLevel && !CraftingProcessor.ActiveSolver.IsType<MacroSolver>())
            {
                ImGui.Dummy(new System.Numerics.Vector2(12f));
                ImGuiEx.TextWrapped(ImGuiColors.DalamudYellow, "这是当前等级的高难度配方。成功率可能会不稳定，建议使用Artisan的宏功能或手动完成该配方。");
            }

            bool autoMode = P.Config.AutoMode;
            if (ImGui.Checkbox("自动制作模式", ref autoMode))
            {
                P.Config.AutoMode = autoMode;
                P.Config.Save();
            }

            if (autoMode && !P.Config.ReplicateMacroDelay)
            {
                var delay = P.Config.AutoDelay;
                ImGui.PushItemWidth(200);
                if (ImGui.SliderInt("设置延迟（毫秒）", ref delay, 0, 1000))
                {
                    if (delay < 0) delay = 0;
                    if (delay > 1000) delay = 1000;

                    P.Config.AutoDelay = delay;
                    P.Config.Save();
                }
            }

            if (Endurance.RecipeID != 0 && !CraftingListUI.Processing && Endurance.Enable)
            {
                if (ImGui.Button("禁用耐力模式"))
                {
                    Endurance.ToggleEndurance(false);
                    P.TM.Abort();
                    CraftingListFunctions.CLTM.Abort();
                    PreCrafting.Tasks.Clear();
                }
            }

            if (!Endurance.Enable && Crafting.IsTrial)
                ImGui.Checkbox("重复制作练习", ref RepeatTrial);

            if (CraftingProcessor.ActiveSolver)
            {
                var text = $"使用[{CraftingProcessor.ActiveSolver.Name}]";
                if (CraftingProcessor.NextRec.Comment.Length > 0)
                    text += $" ({CraftingProcessor.NextRec.Comment})";
                ImGuiEx.TextWrapped(text.Replace("%", ""));
            }

            if (P.Config.CraftingX && Endurance.Enable)
                ImGui.Text($"剩余制作：{P.Config.CraftX}");

            if (_estimatedCraftEnd != default)
            {
                var diff = _estimatedCraftEnd - DateTime.Now;
                string duration = string.Format("{0:D2}h {1:D2}m {2:D2}s", diff.Hours, diff.Minutes, diff.Seconds);
                ImGui.Text($"大致剩余时间：{duration}");
            }

            if (!P.Config.AutoMode)
            {
                ImGui.Text("半手动模式");

                var action = CraftingProcessor.NextRec.Action;
                using var disable = ImRaii.Disabled(action == Skills.None);

                if (ImGui.Button("执行建议的操作"))
                {
                    ActionManagerEx.UseSkill(action);
                }
                if (ImGui.Button("获取建议"))
                {
                    ShowRecommendation(action);
                }
            }
        }

        private void ShowRecommendation(Skills action)
        {
            if (!P.Config.DisableToasts)
            {
                QuestToastOptions options = new() { IconId = action.IconOfAction(CharacterInfo.JobID) };
                Svc.Toasts.ShowQuest($"使用{action.NameOfAction()}", options);
            }
        }

        private void OnSolverStarted(Lumina.Excel.Sheets.Recipe recipe, SolverRef solver, CraftState craft, StepState initialStep)
        {
            if (P.Config.AutoMode && solver)
            {
                var estimatedTime = SolverUtils.EstimateCraftTime(solver.Clone()!, craft, initialStep.Quality);
                var count = P.Config.CraftingX && Endurance.Enable ? P.Config.CraftX : 1;
                _estimatedCraftEnd = DateTime.Now + count * estimatedTime;
            }
        }

        private void OnSolverFailed(Lumina.Excel.Sheets.Recipe recipe, string reason)
        {
            var text = $"{reason}. Artisan不会继续。";
            Svc.Toasts.ShowError(text);
            DuoLog.Error(text);
        }

        private void OnSolverFinished(Lumina.Excel.Sheets.Recipe recipe, SolverRef solver, CraftState craft, StepState finalStep)
        {
            _estimatedCraftEnd = default;
        }

        private void OnRecommendationReady(Lumina.Excel.Sheets.Recipe recipe, SolverRef solver, CraftState craft, StepState step, Solver.Recommendation recommendation)
        {
            if (!Simulator.CanUseAction(craft, step, recommendation.Action))
            {
                return;
            }
            ShowRecommendation(recommendation.Action);
            if (P.Config.AutoMode || Endurance.IPCOverride)
            {
                if (!P.Config.ReplicateMacroDelay)
                    P.CTM.DelayNext(P.Config.AutoDelay);
                P.CTM.Enqueue(() => Crafting.CurState == Crafting.State.InProgress, 3000, true, "WaitForStateToUseAction");
                P.CTM.Enqueue(() => ActionManagerEx.UseSkill(recommendation.Action));
                if (P.Config.ReplicateMacroDelay)
                    P.CTM.DelayNext(Calculations.ActionIsLengthyAnimation(recommendation.Action) ? 3000 : 2000);
            }
        }
    }
}
