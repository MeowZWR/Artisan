using Artisan.Autocraft;
using Artisan.CraftingLists;
using Artisan.CraftingLogic;
using Artisan.FCWorkshops;
using Artisan.RawInformation;
using Artisan.RawInformation.Character;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Lumina.Excel.Sheets;
using PunishLib.ImGuiMethods;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using ThreadLoadImageHandler = ECommons.ImGuiMethods.ThreadLoadImageHandler;

namespace Artisan.UI
{
    unsafe internal class PluginUI : Window
    {
        public event EventHandler<bool>? CraftingWindowStateChanged;


        private bool visible = false;
        public OpenWindow OpenWindow { get; set; }

        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        private bool craftingVisible = false;
        public bool CraftingVisible
        {
            get { return this.craftingVisible; }
            set { if (this.craftingVisible != value) CraftingWindowStateChanged?.Invoke(this, value); this.craftingVisible = value; }
        }

        public PluginUI() : base($"{P.Name} {P.GetType().Assembly.GetName().Version}###Artisan")
        {
            this.RespectCloseHotkey = false;
            this.SizeConstraints = new()
            {
                MinimumSize = new(250, 100),
                MaximumSize = new(9999, 9999)
            };
            P.ws.AddWindow(this);
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

        public void Dispose()
        {

        }

        public override void Draw()
        {
            if (DalamudInfo.IsOnStaging())
            {
                var scale = ImGui.GetIO().FontGlobalScale;
                ImGui.GetIO().FontGlobalScale = scale * 1.5f;
                using (var f = ImRaii.PushFont(ImGui.GetFont()))
                {
                    ImGuiEx.TextWrapped($"听着，兄弟，你现在用的是Dalamud的测试版，遇到任何问题都可能是在Dalamud测试版上特有的，跟Artisan无关。这个插件不是为测试版开发的，所以除非问题出现在Dalamud正式版中，否则别指望我会修复。");
                    ImGui.Separator();

                    ImGui.Spacing();
                    ImGui.GetIO().FontGlobalScale = scale;
                }

            }
            var region = ImGui.GetContentRegionAvail();
            var itemSpacing = ImGui.GetStyle().ItemSpacing;

            var topLeftSideHeight = region.Y;

            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(5f.Scale(), 0));
            try
            {
                ShowEnduranceMessage();

                using (var table = ImRaii.Table($"ArtisanTableContainer", 2, ImGuiTableFlags.Resizable))
                {
                    if (!table)
                        return;

                    ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.GetWindowWidth() / 2);

                    ImGui.TableNextColumn();

                    var regionSize = ImGui.GetContentRegionAvail();

                    ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
                    using (var leftChild = ImRaii.Child($"###ArtisanLeftSide", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
                    {
                        var imagePath = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/artisan-icon.png");

                        if (ThreadLoadImageHandler.TryGetTextureWrap(imagePath, out var logo))
                        {
                            ImGuiEx.LineCentered("###ArtisanLogo", () =>
                            {
                                ImGui.Image(logo.ImGuiHandle, new(100f.Scale(), 100f.Scale()));
                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.Text($"真棒！你是第69个发现这个秘密的人。");
                                    ImGui.EndTooltip();
                                }
                            });

                        }
                        ImGui.Spacing();
                        ImGui.Separator();

                        if (ImGui.Selectable("插件概述", OpenWindow == OpenWindow.Overview))
                        {
                            OpenWindow = OpenWindow.Overview;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("插件设置", OpenWindow == OpenWindow.Main))
                        {
                            OpenWindow = OpenWindow.Main;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("耐力模式", OpenWindow == OpenWindow.Endurance))
                        {
                            OpenWindow = OpenWindow.Endurance;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("宏", OpenWindow == OpenWindow.Macro))
                        {
                            OpenWindow = OpenWindow.Macro;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("Raphael 缓存", OpenWindow == OpenWindow.RaphaelCache))
                        {
                            OpenWindow = OpenWindow.RaphaelCache;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("配方分配器", OpenWindow == OpenWindow.Assigner))
                        {
                            OpenWindow = OpenWindow.Assigner;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("制作清单", OpenWindow == OpenWindow.Lists))
                        {
                            OpenWindow = OpenWindow.Lists;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("清单生成器", OpenWindow == OpenWindow.SpecialList))
                        {
                            OpenWindow = OpenWindow.SpecialList;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("部队工房", OpenWindow == OpenWindow.FCWorkshop))
                        {
                            OpenWindow = OpenWindow.FCWorkshop;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("模拟器", OpenWindow == OpenWindow.Simulator))
                        {
                            OpenWindow = OpenWindow.Simulator;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("关于", OpenWindow == OpenWindow.About))
                        {
                            OpenWindow = OpenWindow.About;
                        }


#if DEBUG
                        ImGui.Spacing();
                        if (ImGui.Selectable("调试", OpenWindow == OpenWindow.Debug))
                        {
                            OpenWindow = OpenWindow.Debug;
                        }
                        ImGui.Spacing();
#endif

                    }

                    ImGui.PopStyleVar();
                    ImGui.TableNextColumn();
                    using (var rightChild = ImRaii.Child($"###ArtisanRightSide", Vector2.Zero, false))
                    {
                        switch (OpenWindow)
                        {
                            case OpenWindow.Main:
                                DrawMainWindow();
                                break;
                            case OpenWindow.Endurance:
                                Endurance.Draw();
                                break;
                            case OpenWindow.Lists:
                                CraftingListUI.Draw();
                                break;
                            case OpenWindow.About:
                                AboutTab.Draw("Artisan");
                                break;
                            case OpenWindow.Debug:
                                DebugTab.Draw();
                                break;
                            case OpenWindow.Macro:
                                MacroUI.Draw();
                                break;
                            case OpenWindow.RaphaelCache:
                                RaphaelCacheUI.Draw();
                                break;
                            case OpenWindow.Assigner:
                                AssignerUI.Draw();
                                break;
                            case OpenWindow.FCWorkshop:
                                FCWorkshopUI.Draw();
                                break;
                            case OpenWindow.SpecialList:
                                SpecialLists.Draw();
                                break;
                            case OpenWindow.Overview:
                                DrawOverview();
                                break;
                            case OpenWindow.Simulator:
                                SimulatorUI.Draw();
                                break;
                            case OpenWindow.None:
                                break;
                            default:
                                break;
                        }
                        ;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Log();
            }
            ImGui.PopStyleVar();
        }

        private void DrawOverview()
        {
            var imagePath = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/artisan.png");

            if (ThreadLoadImageHandler.TryGetTextureWrap(imagePath, out var logo))
            {
                ImGuiEx.LineCentered("###ArtisanTextLogo", () =>
                {
                    ImGui.Image(logo.ImGuiHandle, new Vector2(logo.Width, 100f.Scale()));
                });
            }

            ImGuiEx.LineCentered("###ArtisanOverview", () =>
            {
                ImGuiEx.TextUnderlined("Artisan - 概述");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"首先感谢你下载我的这个小小的生产插件。自2022年6月以来，我一直在开发Artisan，这是我的一系列插件中的代表作。");
            ImGuiEx.TextWrapped($"它是免费的，你不应该从其他渠道花钱获取它。");
            ImGui.Spacing();
            ImGuiEx.TextWrapped($"在开始使用Artisan之前，我们应该先了解一下插件的工作原理。一旦你了解了几个关键因素，Artisan就很容易使用。");

            ImGui.Spacing();
            ImGuiEx.LineCentered("###ArtisanModes", () =>
            {
                ImGuiEx.TextUnderlined("制作模式");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"Artisan具有\"自动制作模式\"该模式仅接受内置解算器给出的建议并且代替你自动操作。" +
                                " 默认情况下，它会以游戏允许的间隔速度使用制作技能，这比使用游戏内的宏更快。" +
                                " 使用它并没有绕过任何形式的游戏限制，但如果你打算制作速度慢一些，你可以设置延迟。" +
                                " 启用此选项不影响Artisan默认使用的建议生成过程。");

            var automode = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/AutoMode.png");

            if (ThreadLoadImageHandler.TryGetTextureWrap(automode, out var example))
            {
                ImGuiEx.LineCentered("###AutoModeExample", () =>
                {
                    ImGui.Image(example.ImGuiHandle, new Vector2(example.Width, example.Height));
                });
            }

            ImGuiEx.TextWrapped($"如果你没有启用自动模式，你将可以接触另外2种模式：\"半自动模式\"和\"全手动模式\"。" +
                                $" \"半自动模式\"将在你开始制作作业时，出现在一个弹出的小窗口中。");

            var craftWindowExample = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/ThemeCraftingWindowExample.png");

            if (ThreadLoadImageHandler.TryGetTextureWrap(craftWindowExample, out example))
            {
                ImGuiEx.LineCentered("###CraftWindowExample", () =>
                {
                    ImGui.Image(example.ImGuiHandle, new Vector2(example.Width, example.Height));
                });
            }

            ImGuiEx.TextWrapped($"通过点击\"执行建议的操作\"按钮，你将使用插件当前建议的制作技能。" +
                $" 这被认为是半自动的，因为你仍然需要每一步点击一次操作按钮，但不必去热键栏上找到相应的技能。" +
                $" \"全手动模式\"是通过正常按下热键栏上的技能来执行的。" +
                $" 默认情况下，你将得到辅助，如果你将技能放在了热键栏上，Artisan会对相应技能进行高亮提示（可以在设置中禁用）。");

            var outlineExample = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/OutlineExample.png");

            if (ThreadLoadImageHandler.TryGetTextureWrap(outlineExample, out example))
            {
                ImGuiEx.LineCentered("###OutlineExample", () =>
                {
                    ImGui.Image(example.ImGuiHandle, new Vector2(example.Width, example.Height));
                });
            }

            ImGui.Spacing();
            ImGuiEx.LineCentered("###ArtisanSuggestions", () =>
            {
                ImGuiEx.TextUnderlined("解算器/宏");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"默认情况下，Artisan会为你提供下一步制作技能的建议。然而，这个解算器并不完美，它并不能替代一套合适的生产装备。" +
                $"除了启用Artisan之外，你无需执行任何操作。" +
                $"\r\n\r\n" +
                $"如果你正在尝试处理默认解算器无法完成的制作，Artisan允许你构建宏来替代默认解算器。" +
                $"Artisan宏的好处是不受长度限制，可以在游戏允许的间隔范围内执行宏里的技能，并且还允许设置一些额外的条件在宏运行过程中作出调整。");

            ImGui.Spacing();
            ImGuiEx.TextUnderlined($"单击此处转到“宏”菜单。");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            if (ImGui.IsItemClicked())
            {
                OpenWindow = OpenWindow.Macro;
            }
            ImGui.Spacing();
            ImGuiEx.TextWrapped($"创建宏后，你需要将其分配给配方来使用。这可以通过使用“配方窗口”下拉列表轻松完成。默认情况下，它将附加到游戏内制作笔记窗口的右上角，但可以在设置中取消附加。");


            var recipeWindowExample = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/RecipeWindowExample.png");

            if (ThreadLoadImageHandler.TryGetTextureWrap(recipeWindowExample, out example))
            {
                ImGuiEx.LineCentered("###RecipeWindowExample", () =>
                {
                    ImGui.Image(example.ImGuiHandle, new Vector2(example.Width, example.Height));
                });
            }


            ImGuiEx.TextWrapped($"从下拉框中选择已创建的宏。" +
                $"当你去制作这个物品时，技能建议将被你的宏的内容所取代。");


            ImGui.Spacing();
            ImGuiEx.LineCentered("###Endurance", () =>
            {
                ImGuiEx.TextUnderlined("耐力模式");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"Artisan具有名为\"耐力模式\"的功能，其实就是\"自动重复模式\"文艺一点的说法。" +
                $"耐力模式的工作原理是从游戏内制作笔记中选择一个配方并启用该功能。" +
                $"然后，你的角色会尝试在你有素材的情况下制作该物品的许多次。" +
                $"\r\n\r\n" +
                $"其他功能应该是不言自明的，因为耐力模式还可以管理你的食物、药水、手册、维修和制作物品之间的素材提取的使用。" +
                $"装备修理功能仅支持使用暗物质，不支持使用修理NPC。");

            ImGui.Spacing();
            ImGuiEx.TextUnderlined($"单击此处进入耐力菜单。");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            if (ImGui.IsItemClicked())
            {
                OpenWindow = OpenWindow.Endurance;
            }

            ImGui.Spacing();
            ImGuiEx.LineCentered("###Lists", () =>
            {
                ImGuiEx.TextUnderlined("制作清单");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"Artisan还能够生成一个物品清单，并让它开始自动制作清单内的每个物品。" +
                $"制作清单有很多强大的工具来简化从素材到最终产品的过程。" +
                $"它还支持在Artisan和Teamcraft之间导入导出。");

            ImGui.Spacing();
            ImGuiEx.TextUnderlined($"单击此处进入制作清单菜单。");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            if (ImGui.IsItemClicked())
            {
                OpenWindow = OpenWindow.Lists;
            }

            ImGui.Spacing();
            ImGuiEx.LineCentered("###Questions", () =>
            {
                ImGuiEx.TextUnderlined("有其他疑问吗？");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"如果您对此处未概述的内容有疑问，可以访问我们的");
            ImGui.SameLine(ImGui.GetCursorPosX(), 1.5f);
            ImGuiEx.TextUnderlined($"Discord服务器");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (ImGui.IsItemClicked())
                {
                    Util.OpenLink("https://discord.gg/Zzrcc8kmvy");
                }
            }

            ImGuiEx.TextWrapped($"你也可以来我们的");
            ImGui.SameLine(ImGui.GetCursorPosX(), 2f);
            ImGuiEx.TextUnderlined($"Github页面");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                if (ImGui.IsItemClicked())
                {
                    Util.OpenLink("https://github.com/PunishXIV/Artisan");
                }
            }

        }

        public static void DrawMainWindow()
        {
            ImGui.TextWrapped($"在这里，你可以修改一些影响使用Artisan的设置。其中有些设置可以在制作过程中开关。");
            ImGui.TextWrapped($"要使用Artisan的技能建议高亮提示，请将你已解锁的每个技能放入可见的热键栏。");
            bool autoEnabled = P.Config.AutoMode;
            bool delayRec = P.Config.DelayRecommendation;
            bool failureCheck = P.Config.DisableFailurePrediction;
            int maxQuality = P.Config.MaxPercentage;
            bool useTricksGood = P.Config.UseTricksGood;
            bool useTricksExcellent = P.Config.UseTricksExcellent;
            bool useSpecialist = P.Config.UseSpecialist;
            //bool showEHQ = P.Config.ShowEHQ;
            //bool useSimulated = P.Config.UseSimulatedStartingQuality;
            bool disableGlow = P.Config.DisableHighlightedAction;
            bool disableToasts = P.Config.DisableToasts;

            ImGui.Separator();

            if (ImGui.CollapsingHeader("常规设置"))
            {
                if (ImGui.Checkbox("自动制作模式", ref autoEnabled))
                {
                    P.Config.AutoMode = autoEnabled;
                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker($"自动使用内置解算器建议的每个技能。");
                if (autoEnabled)
                {
                    if (ImGui.Checkbox($"重复宏延迟", ref P.Config.ReplicateMacroDelay))
                    {
                        P.Config.Save();
                    }

                    if (!P.Config.ReplicateMacroDelay)
                    {
                        var delay = P.Config.AutoDelay;
                        ImGui.PushItemWidth(200);
                        if (ImGui.SliderInt("运行延迟（毫秒）###ActionDelay", ref delay, 0, 1000))
                        {
                            if (delay < 0) delay = 0;
                            if (delay > 1000) delay = 1000;

                            P.Config.AutoDelay = delay;
                            P.Config.Save();
                        }
                    }
                }

                bool requireFoodPot = P.Config.AbortIfNoFoodPot;
                if (ImGui.Checkbox("强制使用消耗品", ref requireFoodPot))
                {
                    P.Config.AbortIfNoFoodPot = requireFoodPot;
                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker("Artisan将自动使用设置好的食物、指南或药品，如果找不到，则拒绝制作。");

                if (ImGui.Checkbox("在制作练习时使用消耗品", ref P.Config.UseConsumablesTrial))
                {
                    P.Config.Save();
                }

                if (ImGui.Checkbox("在简易制作作业时使用消耗品", ref P.Config.UseConsumablesQuickSynth))
                {
                    P.Config.Save();
                }

                ImGui.Indent();
                if (ImGui.CollapsingHeader("默认消耗品"))
                {
                    bool changed = false;
                    changed |= P.Config.DefaultConsumables.DrawFood();
                    changed |= P.Config.DefaultConsumables.DrawPotion();
                    changed |= P.Config.DefaultConsumables.DrawManual();
                    changed |= P.Config.DefaultConsumables.DrawSquadronManual();

                    if (changed)
                    {
                        P.Config.Save();
                    }
                }
                ImGui.Unindent();

                if (ImGui.Checkbox($"优先使用修理工而不是自己修理", ref P.Config.PrioritizeRepairNPC))
                {
                    P.Config.Save();
                }

                ImGuiComponents.HelpMarker("自动修理时，优先尝试利用附近的修理工进行修理，如果没有找到修理工，并且你满足修理所需的条件，尝试自己修理。");

                if (ImGui.Checkbox($"无法修理时，禁用耐久模式", ref P.Config.DisableEnduranceNoRepair))
                    P.Config.Save();

                ImGuiComponents.HelpMarker($"一旦达到修理阈值，如果你自己或通过NPC都无法进行修理，自动禁用耐久模式。");

                if (ImGui.Checkbox($"无法修理时暂停制作清单进度", ref P.Config.DisableListsNoRepair))
                    P.Config.Save();

                ImGuiComponents.HelpMarker($"一旦达到修理阈值，如果你自己或通过NPC都无法进行修理，自动暂停当前清单进度。");

                bool requestStop = P.Config.RequestToStopDuty;
                bool requestResume = P.Config.RequestToResumeDuty;
                int resumeDelay = P.Config.RequestToResumeDelay;

                if (ImGui.Checkbox("在任务进入出发准备时，自动关闭耐力模式或暂停清单制作。", ref requestStop))
                {
                    P.Config.RequestToStopDuty = requestStop;
                    P.Config.Save();
                }

                if (requestStop)
                {
                    if (ImGui.Checkbox("在离开副本后，自动恢复关闭耐力模式或暂停清单制作。", ref requestResume))
                    {
                        P.Config.RequestToResumeDuty = requestResume;
                        P.Config.Save();
                    }

                    if (requestResume)
                    {
                        if (ImGui.SliderInt("延迟恢复（秒）", ref resumeDelay, 5, 60))
                        {
                            P.Config.RequestToResumeDelay = resumeDelay;
                        }
                    }
                }

                if (ImGui.Checkbox("禁止自动装备制作所需物品。", ref P.Config.DontEquipItems))
                    P.Config.Save();

                if (ImGui.Checkbox("耐力模式制作完成后播放提示音", ref P.Config.PlaySoundFinishEndurance))
                    P.Config.Save();

                if (ImGui.Checkbox($"清单制作完成后播放提示音", ref P.Config.PlaySoundFinishList))
                    P.Config.Save();

                if (P.Config.PlaySoundFinishEndurance || P.Config.PlaySoundFinishList)
                {
                    if (ImGui.SliderFloat("音量", ref P.Config.SoundVolume, 0f, 1f, "%.2f"))
                        P.Config.Save();
                }

                if (ImGuiEx.ButtonCtrl("Reset Cosmic Exploration Crafting Configs"))
                {
                    var copy = P.Config.RecipeConfigs;
                    foreach (var c in copy)
                    {
                        if (Svc.Data.GetExcelSheet<Recipe>().GetRow(c.Key).Number == 0)
                            P.Config.RecipeConfigs.Remove(c.Key);
                    }
                }
            }
            if (ImGui.CollapsingHeader("宏设置"))
            {
                if (ImGui.Checkbox("如果无法使用那个技能，则跳过那一步。", ref P.Config.SkipMacroStepIfUnable))
                    P.Config.Save();

                if (ImGui.Checkbox($"阻止Artisan在宏完成后继续。", ref P.Config.DisableMacroArtisanRecommendation))
                    P.Config.Save();
            }
            if (ImGui.CollapsingHeader("解算器设置"))
            {
                if (ImGui.Checkbox($"使用 {Skills.TricksOfTrade.NameOfAction()} - {LuminaSheets.AddonSheet[227].Text.ToString()}", ref useTricksGood))
                {
                    P.Config.UseTricksGood = useTricksGood;
                    P.Config.Save();
                }
                ImGui.SameLine();
                if (ImGui.Checkbox($"使用 {Skills.TricksOfTrade.NameOfAction()} - {LuminaSheets.AddonSheet[228].Text.ToString()}", ref useTricksExcellent))
                {
                    P.Config.UseTricksExcellent = useTricksExcellent;
                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker($"这两个选项允许你在出现“{LuminaSheets.AddonSheet[227].Text.ToString}”或“{LuminaSheets.AddonSheet[228].Text.ToString}”状态时优先使用“{Skills.TricksOfTrade.NameOfAction()}”。\n\n这将替代{Skills.PreciseTouch.NameOfAction()}和{Skills.IntensiveSynthesis.NameOfAction()}的使用时机。\n\n不管如何设置，在学会前或特定状况下仍将使用{Skills.TricksOfTrade.NameOfAction()}。");
                if (ImGui.Checkbox("使用专家技能", ref useSpecialist))
                {
                    P.Config.UseSpecialist = useSpecialist;
                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker("若当前职业有专家认证，使用消耗“能工巧匠图纸”道具的技能。\n“设计变动”将会取代“观察”。");
                ImGui.TextWrapped("最大品质%%");
                ImGuiComponents.HelpMarker($"一旦品质达到以下百分比，Artisan将会专注于推动进展。");
                if (ImGui.SliderInt("###SliderMaxQuality", ref maxQuality, 0, 100, $"%d%%"))
                {
                    P.Config.MaxPercentage = maxQuality;
                    P.Config.Save();
                }

                ImGui.Text($"收藏品收藏价值断点");
                ImGuiComponents.HelpMarker("一旦收藏品达到以下断点，解算器将停止推进品质。");

                if (ImGui.RadioButton($"低档", P.Config.SolverCollectibleMode == 1))
                {
                    P.Config.SolverCollectibleMode = 1;
                    P.Config.Save();
                }
                ImGui.SameLine();
                if (ImGui.RadioButton($"中档", P.Config.SolverCollectibleMode == 2))
                {
                    P.Config.SolverCollectibleMode = 2;
                    P.Config.Save();
                }
                ImGui.SameLine();
                if (ImGui.RadioButton($"最高档", P.Config.SolverCollectibleMode == 3))
                {
                    P.Config.SolverCollectibleMode = 3;
                    P.Config.Save();
                }

                if (ImGui.Checkbox($"提升品质起手，使用 ({Skills.Reflect.NameOfAction()})", ref P.Config.UseQualityStarter))
                    P.Config.Save();
                ImGuiComponents.HelpMarker($"在耐久度较低的制作中更有利。");

                //if (ImGui.Checkbox("Low Stat Mode", ref P.Config.LowStatsMode))
                //    P.Config.Save();

                //ImGuiComponents.HelpMarker("This swaps out Waste Not II & Groundwork for Prudent Synthesis");

                ImGui.TextWrapped($"{Skills.PreparatoryTouch.NameOfAction()} - 最大{Buffs.InnerQuiet.NameOfBuff()}层数");
                ImGui.SameLine();
                ImGuiComponents.HelpMarker($"将仅使用{Skills.PreparatoryTouch.NameOfAction()}来提高{Buffs.InnerQuiet.NameOfBuff()}的层数。这有助于调整制作力用量。");
                if (ImGui.SliderInt($"###MaxIQStacksPrepTouch", ref P.Config.MaxIQPrepTouch, 0, 10))
                    P.Config.Save();

                if (ImGui.Checkbox($"Use Material Miracle when available", ref P.Config.UseMaterialMiracle))
                    P.Config.Save();

                ImGuiComponents.HelpMarker($"This will switch the Standard Recipe Solver over to the Expert Solver for the duration of the buff. This will not give you proper simulator results as it's a timed buff, not a permanent one with stacks, so we can't really simulate it properly.");

                if (P.Config.UseMaterialMiracle)
                {
                    ImGui.Indent();
                    if (ImGui.Checkbox($"Use more than once per craft.", ref P.Config.MaterialMiracleMulti))
                        P.Config.Save();

                    ImGui.Unindent();
                }

            }
            bool openExpert = false;
            if (ImGui.CollapsingHeader("专家配方解算器设置"))
            {
                openExpert = true;
                if (P.Config.ExpertSolverConfig.expertIcon is not null)
                {
                    ImGui.SameLine();
                    ImGui.Image(P.Config.ExpertSolverConfig.expertIcon.ImGuiHandle, new(P.Config.ExpertSolverConfig.expertIcon.Width * ImGuiHelpers.GlobalScaleSafe, ImGui.GetItemRectSize().Y), new(0, 0), new(1, 1), new(0.94f, 0.57f, 0f, 1f));
                }
                if (P.Config.ExpertSolverConfig.Draw())
                    P.Config.Save();
            }
            if (!openExpert)
            {
                if (P.Config.ExpertSolverConfig.expertIcon is not null)
                {
                    ImGui.SameLine();
                    ImGui.Image(P.Config.ExpertSolverConfig.expertIcon.ImGuiHandle, new(P.Config.ExpertSolverConfig.expertIcon.Width * ImGuiHelpers.GlobalScaleSafe, ImGui.GetItemRectSize().Y), new(0, 0), new(1, 1), new(0.94f, 0.57f, 0f, 1f));
                }
            }

            if (ImGui.CollapsingHeader("Raphael Solver Settings"))
            {
                if (P.Config.RaphaelSolverConfig.Draw())
                    P.Config.Save();
            }

            using (ImRaii.Disabled())
            {
                if (ImGui.CollapsingHeader("脚本解算器设置（当前已禁用）"))
                {
                    if (P.Config.ScriptSolverConfig.Draw())
                        P.Config.Save();
                }
            }
            if (ImGui.CollapsingHeader("界面设置"))
            {
                if (ImGui.Checkbox("禁用技能热键栏提示", ref disableGlow))
                {
                    P.Config.DisableHighlightedAction = disableGlow;
                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker("禁用后就不会在热键栏上用矩形框标记建议使用的技能了。");

                if (ImGui.Checkbox($"禁用技能屏幕提示", ref disableToasts))
                {
                    P.Config.DisableToasts = disableToasts;
                    P.Config.Save();
                }

                ImGuiComponents.HelpMarker("禁用后就不会在屏幕中间用文字和图标提示建议使用的技能了。");

                bool lockMini = P.Config.LockMiniMenuR;
                if (ImGui.Checkbox("保持迷你菜单吸附至制作笔记窗口。", ref lockMini))
                {
                    P.Config.LockMiniMenuR = lockMini;
                    P.Config.Save();
                }

                if (!P.Config.LockMiniMenuR)
                {
                    if (ImGui.Checkbox($"固定迷你菜单位置", ref P.Config.PinMiniMenu))
                    {
                        P.Config.Save();
                    }
                }

                if (ImGui.Button("重置制作清单迷你菜单位置"))
                {
                    AtkResNodeFunctions.ResetPosition = true;
                }

                if (ImGui.Checkbox($"扩展搜索栏功能", ref P.Config.ReplaceSearch))
                {
                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker($"扩展配方菜单中的搜索栏，提供即时结果和单击打开配方的功能。");

                bool hideQuestHelper = P.Config.HideQuestHelper;
                if (ImGui.Checkbox($"隐藏任务助手", ref hideQuestHelper))
                {
                    P.Config.HideQuestHelper = hideQuestHelper;
                    P.Config.Save();
                }

                bool hideTheme = P.Config.DisableTheme;
                if (ImGui.Checkbox("禁用自定义主题", ref hideTheme))
                {
                    P.Config.DisableTheme = hideTheme;
                    P.Config.Save();
                }
                ImGui.SameLine();

                if (IconButtons.IconTextButton(FontAwesomeIcon.Clipboard, "复制主题"))
                {
                    ImGui.SetClipboardText("DS1H4sIAAAAAAAACq1YS3PbNhD+Kx2ePR6AeJG+xXYbH+KOJ3bHbW60REusaFGlKOXhyX/v4rEACEqumlY+ECD32/cuFn7NquyCnpOz7Cm7eM1+zy5yvfnDPL+fZTP4at7MHVntyMi5MGTwBLJn+HqWLZB46Ygbx64C5kQv/nRo8xXQ3AhZZRdCv2jdhxdHxUeqrJO3Ftslb5l5u/Fa2rfEvP0LWBkBPQiSerF1Cg7wApBn2c5wOMv2juNn9/zieH09aP63g+Kqyr1mI91mHdj5mj3UX4bEG+b5yT0fzRPoNeF1s62e2np+EuCxWc+7z5cLr1SuuCBlkTvdqBCEKmaQxCHJeZmXnFKlgMHVsmnnEZ5IyXMiFUfjwt6yCHvDSitx1212m4gHV0QURY4saMEYl6Q4rsRl18/rPuCZQ+rFJxeARwyAJb5fVmD4NBaJEK3eL331UscuAgflOcY0J5zLUioHpHmhCC0lCuSBwU23r3sfF/0N0wKdoxcGFqHezYZmHypJIkgiSCJIalc8NEM7Utb6ErWlwngt9aUoFRWSB3wilRUl5SRwISUFvhJt9lvDrMgLIjgLzK66tq0228j0H+R3W693l1UfmUd9kqA79MKn9/2sB9lPI8hbofb073vdh1BbQYRgqKzfGbTfTWVqHmnMOcXUpI6BXhzGJjEQCNULmy4x9GpZz1a3Vb8KqaIDz4RPVGZin6dlZPKDSS29baAyRqYfzVGnr0ekaaowTbEw9MLjLnfD0GGT1unHSSlKr2lRyqLA2qU5ESovi6m+lkvqYiZ1/ygxyqrgjDKF8Yr2lp1pd4R7dokhvOBUQk37TCVKQbX4TMVtyuymruKWJCURVEofClYWbNpWCQfFifDwsWnYyXXS8ZxDOI+H0uLToPzrhKg3VV8N3amt1dP/t5goW/E85pg2pB8N8sd623yr3/dNOPYVstELg9cLA8zFCJKapQpEYkPVi9CMA/L/Uv8hrk1hmg9WKKMQXyIxnGFrm6i06MkhBHlIiQ8rI0xx4k/rsLWBsWpbTmmhqFIypcvUHTRgQ859V/bbKaPf1s/dbBcfD0R6NnCWwg/dS3lB4MfQMSrnCY9EK8qEw9uUl4YdHjRQRVFTuu5mq2a9uOvrfVOH0SDHqtXxMjDfi1RA/fyyGb7G5y5KdJg8EnTXdsOHZl1vQyJJQrlCQTDsEBi80HdhO+VwrEP48hwdTRp202yHbgGzhRfu03/UCA4gjglDd44mUT2D2i4UH9coSy8mfjEYN54NfbcOOIZnn15M7YqAH5rFEmdl3eJ8r0N5E9zH0fz71nQQyN+1/zSP6yR2A/l93dazoY6n5DdyiumWc91Xi+u+2zxU/aI+Jipq2QD5tdrfgO3t2P5jcqz9gLEXAEjgFHzcMJUgr5uXyDQsNSxZtCvX81s3r1qLOw0EztC3ORiEs4vssu9W9fqn2263HqpmncFF016PqklGjh1kjQ2NUyUJH08mcIk9gSrqn+jg0XFoqeqTrmDPwQv+PDEr6wl3oljaxcRSRTCyMc/lJJ/lAcnNhMr3WWZ+ES3exrXE+HJ2yNOrowkb97A2cExdXcrYjaFToVDfGSMqnCaDa0pi/vzNMyLG/wQEyzmzfhx7KAwJUn93Fz6v5shD8B+DRAG4Oh+QHYapovAd3/OEQzuiDSdE4c8wjJHh7iiBFFozvP3+NxT8RWGlEQAA");
                    Notify.Success("主题数据已复制到剪贴板");
                }

                if (ImGui.Checkbox("禁用Allagan Tools插件与清单的集成", ref P.Config.DisableAllaganTools))
                    P.Config.Save();

                if (ImGui.Checkbox("禁用Artisan上下文菜单选项", ref P.Config.HideContextMenus))
                    P.Config.Save();

                ImGuiComponents.HelpMarker("如果不禁用，在你右键点击制作笔记中的配方时，在弹出的右键菜单中会有新增选项。");

                ImGui.Indent();
                if (ImGui.CollapsingHeader("模拟器设置"))
                {
                    if (ImGui.Checkbox("隐藏配方窗口模拟器结果", ref P.Config.HideRecipeWindowSimulator))
                        P.Config.Save();

                    if (ImGui.SliderFloat("模拟器技能图标尺寸", ref P.Config.SimulatorActionSize, 5f, 70f))
                    {
                        P.Config.Save();
                    }
                    ImGuiComponents.HelpMarker("设置模拟器选项卡中显示的技能图标的比例。");

                    if (ImGui.Checkbox("启用手动模式鼠标悬停预览", ref P.Config.SimulatorHoverMode))
                        P.Config.Save();

                    if (ImGui.Checkbox($"隐藏技能提示窗口", ref P.Config.DisableSimulatorActionTooltips))
                        P.Config.Save();

                    ImGuiComponents.HelpMarker("When hovering over actions in manual mode, the description tooltip will not show.");
                }
                ImGui.Unindent();
            }
            if (ImGui.CollapsingHeader("清单设置"))
            {
                ImGui.TextWrapped($"当创建制作清单时，这些设置会自动生效。");

                if (ImGui.Checkbox("跳过你已经拥有了足够数量的物品", ref P.Config.DefaultListSkip))
                {
                    P.Config.Save();
                }

                if (ImGui.Checkbox("自动精制魔晶石", ref P.Config.DefaultListMateria))
                {
                    P.Config.Save();
                }

                if (ImGui.Checkbox("自动修理", ref P.Config.DefaultListRepair))
                {
                    P.Config.Save();
                }

                if (P.Config.DefaultListRepair)
                {
                    ImGui.TextWrapped($"修理临界值");
                    ImGui.SameLine();
                    if (ImGui.SliderInt("###SliderRepairDefault", ref P.Config.DefaultListRepairPercent, 0, 100, $"%d%%"))
                    {
                        P.Config.Save();
                    }
                }

                if (ImGui.Checkbox("设置新添加的物品制作模式为快速制作", ref P.Config.DefaultListQuickSynth))
                {
                    P.Config.Save();
                }

                if (ImGui.Checkbox($@"添加到列表后重置“添加次数”。", ref P.Config.ResetTimesToAdd))
                    P.Config.Save();

                ImGui.PushItemWidth(100);
                if (ImGui.InputInt("通过菜单添加的次数", ref P.Config.ContextMenuLoops))
                {
                    if (P.Config.ContextMenuLoops <= 0)
                        P.Config.ContextMenuLoops = 1;

                    P.Config.Save();
                }

                ImGui.PushItemWidth(400);
                if (ImGui.SliderFloat("制作间隔延迟", ref P.Config.ListCraftThrottle2, 0f, 2f, "%.1f"))
                {
                    if (P.Config.ListCraftThrottle2 < 0f)
                        P.Config.ListCraftThrottle2 = 0f;

                    if (P.Config.ListCraftThrottle2 > 2f)
                        P.Config.ListCraftThrottle2 = 2f;

                    P.Config.Save();
                }

                ImGui.Indent();
                if (ImGui.CollapsingHeader("素材表设置"))
                {
                    ImGuiEx.TextWrapped(ImGuiColors.DalamudYellow, $"如果你已经查看了清单的素材表，则“所有列设置”不会起作用。");

                    if (ImGui.Checkbox($@"默认隐藏""背包"" 列", ref P.Config.DefaultHideInventoryColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"默认隐藏“雇员”列", ref P.Config.DefaultHideRetainerColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"默认隐藏“剩余需求”列", ref P.Config.DefaultHideRemainingColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"默认隐藏“来源”列", ref P.Config.DefaultHideCraftableColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"默认隐藏“可制作数量”列", ref P.Config.DefaultHideCraftableCountColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"默认隐藏“制作目标”列", ref P.Config.DefaultHideCraftItemsColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"默认隐藏“类型”列", ref P.Config.DefaultHideCategoryColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"默认隐藏“采集区域”列", ref P.Config.DefaultHideGatherLocationColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"默认隐藏“ID”列", ref P.Config.DefaultHideIdColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"默认启用“仅显示HQ制作”", ref P.Config.DefaultHQCrafts))
                        P.Config.Save();

                    if (ImGui.Checkbox($"默认启用“颜色验证”", ref P.Config.DefaultColourValidation))
                        P.Config.Save();

                    if (ImGui.Checkbox($"从Universalis获取价格（加载时间较慢）", ref P.Config.UseUniversalis))
                        P.Config.Save();

                    if (P.Config.UseUniversalis)
                    {
                        if (ImGui.Checkbox($"将Universalis限制为当前区服", ref P.Config.LimitUnversalisToDC))
                            P.Config.Save();

                        if (ImGui.Checkbox($"仅按需获取价格", ref P.Config.UniversalisOnDemand))
                            P.Config.Save();

                        ImGuiComponents.HelpMarker("你必须点击一个按钮才能获取每件商品的价格。");
                    }
                }

                ImGui.Unindent();
            }
        }

        private void ShowEnduranceMessage()
        {
            if (!P.Config.ViewedEnduranceMessage)
            {
                P.Config.ViewedEnduranceMessage = true;
                P.Config.Save();

                ImGui.OpenPopup("EndurancePopup");

                var windowSize = new Vector2(512 * ImGuiHelpers.GlobalScale,
                    ImGui.GetTextLineHeightWithSpacing() * 13 + 2 * ImGui.GetFrameHeightWithSpacing() * 2f);
                ImGui.SetNextWindowSize(windowSize);
                ImGui.SetNextWindowPos((ImGui.GetIO().DisplaySize - windowSize) / 2);

                using var popup = ImRaii.Popup("EndurancePopup",
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.Modal);
                if (!popup)
                    return;

                ImGui.TextWrapped($@"I have been receiving quite a number of messages regarding ""buggy"" Endurance mode not setting ingredients anymore. As of the previous update, the old functionality of Endurance has been moved to a new setting.");
                ImGui.Dummy(new Vector2(0));

                var imagePath = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/EnduranceNewSetting.png");

                if (ThreadLoadImageHandler.TryGetTextureWrap(imagePath, out var img))
                {
                    ImGuiEx.ImGuiLineCentered("###EnduranceNewSetting", () =>
                    {
                        ImGui.Image(img.ImGuiHandle, new Vector2(img.Width, img.Height));
                    });
                }

                ImGui.Spacing();

                ImGui.TextWrapped($"This change was made to bring back the very original behaviour of Endurance mode. If you do not care about your ingredient ratio, please make sure to enable Max Quantity Mode.");

                ImGui.SetCursorPosY(windowSize.Y - ImGui.GetFrameHeight() - ImGui.GetStyle().WindowPadding.Y);
                if (ImGui.Button("关闭", -Vector2.UnitX))
                {
                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }

    public enum OpenWindow
    {
        None = 0,
        Main = 1,
        Endurance = 2,
        Macro = 3,
        Lists = 4,
        About = 5,
        Debug = 6,
        FCWorkshop = 7,
        SpecialList = 8,
        Overview = 9,
        Simulator = 10,
        RaphaelCache = 11,
        Assigner = 12,
    }
}
