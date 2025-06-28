using Artisan.GameInterop;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ImGuiNET;
using System.Linq;
using System.Numerics;

namespace Artisan.UI
{
    internal static class RaphaelCacheUI
    {
        private static string _search = string.Empty;
        private static bool _oldVersion = false;
        internal static void Draw()
        {
            ImGui.TextWrapped("此标签页允许您查看 Raphael 集成缓存中的宏。");
            ImGui.Separator();

            if (Svc.ClientState.IsLoggedIn && Crafting.CurState is not Crafting.State.IdleNormal and not Crafting.State.IdleBetween)
            {
                ImGui.Text($"正在制作中。宏设置将在您停止制作后可用。");
                return;
            }
            ImGui.Spacing();

            if (ImGui.RadioButton("旧缓存（未使用）", _oldVersion))
                _oldVersion = true;
            ImGui.SameLine();
            if (ImGui.RadioButton("新缓存", !_oldVersion))
                _oldVersion = false;

            ImGui.InputText($"搜索", ref _search, 300);

            if (!_oldVersion)
            {

                if (ImGui.BeginChild("##selector", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - 32f.Scale()), true))
                {
                    ImGuiEx.TextUnderlined($"等级/进度/品质/耐久度-作业精度/加工精度/制作力-类型/初始品质");
                    foreach (var key in P.Config.RaphaelSolverCacheV3.Keys)
                    {
                        var m = P.Config.RaphaelSolverCacheV3[key];
                        if (!m.Name.Contains(_search, System.StringComparison.CurrentCultureIgnoreCase)) continue;
                        var selected = ImGui.Selectable($"{m.Name}###{m.ID}");

                        if (selected && !P.ws.Windows.Any(x => x.WindowName.Contains(m.ID.ToString())))
                        {
                            new MacroEditor(m, true);
                        }
                    }

                }
                ImGui.EndChild();

            }
            else
            {

                if (ImGui.BeginChild("##selector", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - 32f.Scale()), true))
                {
                    ImGuiEx.TextUnderlined($"等级/进度/品质/耐久度-作业精度/加工精度/制作力-类型");
                    foreach (var key in P.Config.RaphaelSolverCacheV2.Keys)
                    {
                        var m = P.Config.RaphaelSolverCacheV2[key];
                        if (!m.Name.Contains(_search, System.StringComparison.CurrentCultureIgnoreCase)) continue;
                        var selected = ImGui.Selectable($"{m.Name}###{m.ID}");

                        if (selected && !P.ws.Windows.Any(x => x.WindowName.Contains(m.ID.ToString())))
                        {
                            new MacroEditor(m, true);
                        }
                    }

                }
                ImGui.EndChild();

            }

            if (ImGui.Button("清除此 Raphael 缓存（按住 Ctrl）", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y)) && ImGui.GetIO().KeyCtrl)
            {
                if (_oldVersion)
                    P.Config.RaphaelSolverCacheV2.Clear();
                else
                    P.Config.RaphaelSolverCacheV3.Clear();
                P.Config.Save();
            }
        }
    }
}
