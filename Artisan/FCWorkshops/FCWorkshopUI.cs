using Artisan.CraftingLists;
using Artisan.IPC;
using Artisan.RawInformation;
using Dalamud.Interface.Colors;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Artisan.FCWorkshops
{
    internal static class FCWorkshopUI
    {
        internal static uint SelectedProject = 0;
        internal static CompanyCraftSequence CurrentProject => LuminaSheets.WorkshopSequenceSheet[SelectedProject];
        private static string Search = string.Empty;
        private static int NumberOfLoops = 1;

        internal static void Draw()
        {
            if (NumberOfLoops <= 0)
            {
                NumberOfLoops = 1;
            }

            ImGui.TextWrapped($"在此选项卡中, 你可以浏览游戏内所有的部队工房制作项目。 " +
                $"包含3个部分. 第一部分展示项目信息概览。 " +
                $"第二部分展示每个部件." +
                $"第三个部分展示每个阶段。" +
                $"在每个部分，你可以点击“创建制作清单”按钮来创建一个制作清单，" +
                $"其中包含制作该部分所需要的一切素材的清单。");


            ImGui.Separator();
            string preview = SelectedProject != 0 ? LuminaSheets.ItemSheet[LuminaSheets.WorkshopSequenceSheet[SelectedProject].ResultItem.RowId].Name.ToString() : "";
            if (ImGui.BeginCombo("###Workshop Project", preview))
            {
                ImGui.Text("搜索");
                ImGui.SameLine();
                ImGui.InputText("###ProjectSearch", ref Search, 100);

                if (ImGui.Selectable("", SelectedProject == 0))
                {
                    SelectedProject = 0;
                }

                foreach (var project in LuminaSheets.WorkshopSequenceSheet.Values.Where(x => x.RowId > 0).Where(x => x.ResultItem.Value.Name.ToString().Contains(Search, StringComparison.CurrentCultureIgnoreCase)))
                {
                    bool selected = ImGui.Selectable($"{project.ResultItem.Value.Name.ToString()}", project.RowId == SelectedProject);

                    if (selected)
                    {
                        SelectedProject = project.RowId;
                    }
                }

                ImGui.EndCombo();
            }

            if (SelectedProject != 0)
            {
                var project = LuminaSheets.WorkshopSequenceSheet[SelectedProject];

                if (ImGui.CollapsingHeader("项目信息"))
                {
                    if (ImGui.BeginTable($"FCWorkshopProjectContainer", 2, ImGuiTableFlags.Resizable))
                    {
                        ImGui.TableSetupColumn($"###Description", ImGuiTableColumnFlags.WidthFixed);

                        ImGui.TableNextColumn();

                        ImGuiEx.Text($"选中的项目：");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{project.ResultItem.Value.Name.ToString()}");
                        ImGui.TableNextColumn();
                        ImGuiEx.Text($"部件编号：");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{project.CompanyCraftPart.Where(x => x.RowId > 0).Count()}");
                        ImGui.TableNextColumn();
                        ImGuiEx.Text($"阶段总数：");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{project.CompanyCraftPart.Where(x => x.RowId > 0).SelectMany(x => x.Value.CompanyCraftProcess).Where(x => x.RowId > 0).Count()}");

                        ImGui.EndTable();
                    }
                    if (ImGui.BeginTable($"###FCWorkshopProjectItemsContainer", RetainerInfo.ATools ? 4 : 3, ImGuiTableFlags.Borders))
                    {
                        ImGui.TableSetupColumn($"物品", ImGuiTableColumnFlags.WidthFixed);
                        ImGui.TableSetupColumn($"所需总数", ImGuiTableColumnFlags.WidthFixed);
                        ImGui.TableSetupColumn($"包裹库存", ImGuiTableColumnFlags.WidthFixed);
                        if (RetainerInfo.ATools) ImGui.TableSetupColumn($"雇员", ImGuiTableColumnFlags.WidthFixed);

                        ImGui.TableHeadersRow();

                        Dictionary<uint, int> TotalItems = new Dictionary<uint, int>();
                        foreach (var item in project.CompanyCraftPart.Where(x => x.RowId > 0).SelectMany(x => x.Value.CompanyCraftProcess).Where(x => x.RowId > 0).SelectMany(x => x.Value.SupplyItems()).Where(x => x.SupplyItem.RowId > 0).GroupBy(x => x))
                        {
                            if (TotalItems.ContainsKey(LuminaSheets.WorkshopSupplyItemSheet[item.Select(x => x.SupplyItem.RowId).First()].Item.RowId))
                                TotalItems[LuminaSheets.WorkshopSupplyItemSheet[item.Select(x => x.SupplyItem.RowId).First()].Item.RowId] += item.Sum(x => x.SetQuantity * x.SetsRequired);
                            else
                                TotalItems.TryAdd(LuminaSheets.WorkshopSupplyItemSheet[item.Select(x => x.SupplyItem.RowId).First()].Item.RowId, item.Sum(x => x.SetQuantity * x.SetsRequired));
                        }

                        foreach (var item in TotalItems)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text($"{LuminaSheets.ItemSheet[item.Key].Name.ToString()}");
                            ImGui.TableNextColumn();
                            ImGui.Text($"{item.Value * NumberOfLoops}");
                            ImGui.TableNextColumn();
                            int invCount = CraftingListUI.NumberOfIngredient(LuminaSheets.ItemSheet[item.Key].RowId);
                            ImGui.Text($"{invCount}");
                            bool hasEnoughInInv = invCount >= item.Value * NumberOfLoops;
                            if (hasEnoughInInv)
                            {
                                var color = ImGuiColors.HealerGreen;
                                color.W -= 0.3f;
                                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.ColorConvertFloat4ToU32(color));
                            }
                            if (RetainerInfo.ATools)
                            {
                                ImGui.TableNextColumn();
                                ImGui.Text($"{RetainerInfo.GetRetainerItemCount(LuminaSheets.ItemSheet[item.Key].RowId)}");

                                bool hasEnoughWithRetainer = (invCount + RetainerInfo.GetRetainerItemCount(LuminaSheets.ItemSheet[item.Key].RowId) >= item.Value * NumberOfLoops);
                                if (!hasEnoughInInv && hasEnoughWithRetainer)
                                {
                                    var color = ImGuiColors.DalamudOrange;
                                    color.W -= 0.6f;
                                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.ColorConvertFloat4ToU32(color));
                                }
                            }

                        }

                        ImGui.EndTable();
                    }

                    ImGui.InputInt("Number of Times###LoopProject", ref NumberOfLoops);

                    if (ImGui.Button($"为此项目创建制作清单", new Vector2(ImGui.GetContentRegionAvail().X, 24f.Scale())))
                    {
                        Notify.Info($"正在创建清单，请等待...");
                        Task.Run(() => CreateProjectList(project, false)).ContinueWith((_) => Notify.Success("部队工房清单已创建"));
                    }

                    if (ImGui.Button($"为此项目创建制作清单（包含前置配方）", new Vector2(ImGui.GetContentRegionAvail().X, 24f.Scale())))
                    {
                        Notify.Info($"正在创建清单，请等待...");
                        Task.Run(() => CreateProjectList(project, true)).ContinueWith((_) => Notify.Success("部队工房清单已创建"));
                    }
                }
                if (ImGui.CollapsingHeader("项目部件"))
                {
                    ImGui.Indent();
                    string partNum = "";
                    foreach (var part in project.CompanyCraftPart.Where(x => x.RowId > 0).Select(x => x.Value))
                    {
                        partNum = part.CompanyCraftType.Value.Name.ToString();
                        if (ImGui.CollapsingHeader($"{partNum}"))
                        {
                            if (ImGui.BeginTable($"FCWorkshopPartsContainer###{part.RowId}", 2, ImGuiTableFlags.None))
                            {
                                ImGui.TableSetupColumn($"###PartType{part.RowId}", ImGuiTableColumnFlags.WidthFixed);
                                ImGui.TableSetupColumn($"###Phases{part.RowId}", ImGuiTableColumnFlags.WidthFixed);
                                ImGui.TableNextColumn();

                                ImGuiEx.Text($"部件类型：");
                                ImGui.TableNextColumn();
                                ImGui.Text($"{part.CompanyCraftType.Value.Name.ToString()}");
                                ImGui.TableNextColumn();
                                ImGuiEx.Text($"阶段数量：");
                                ImGui.TableNextColumn();
                                ImGui.Text($"{part.CompanyCraftProcess.Where(x => x.RowId > 0).Count()}");
                                ImGui.TableNextColumn();

                                ImGui.EndTable();
                            }
                            if (ImGui.BeginTable($"###FCWorkshopPartItemsContainer{part.RowId}", RetainerInfo.ATools ? 4 : 3, ImGuiTableFlags.Borders))
                            {
                                ImGui.TableSetupColumn($"物品", ImGuiTableColumnFlags.WidthFixed);
                                ImGui.TableSetupColumn($"所需总数", ImGuiTableColumnFlags.WidthFixed);
                                ImGui.TableSetupColumn($"包裹库存", ImGuiTableColumnFlags.WidthFixed);
                                if (RetainerInfo.ATools) ImGui.TableSetupColumn($"雇员", ImGuiTableColumnFlags.WidthFixed);
                                ImGui.TableHeadersRow();

                                Dictionary<uint, int> TotalItems = new Dictionary<uint, int>();
                                foreach (var item in part.CompanyCraftProcess.Where(x => x.RowId > 0).SelectMany(x => x.Value.SupplyItems()).Where(x => x.SupplyItem.RowId > 0).GroupBy(x => x.SupplyItem))
                                {
                                    if (TotalItems.ContainsKey(LuminaSheets.WorkshopSupplyItemSheet[item.Select(x => x.SupplyItem.RowId).First()].Item.RowId))
                                        TotalItems[LuminaSheets.WorkshopSupplyItemSheet[item.Select(x => x.SupplyItem.RowId).First()].Item.RowId] += item.Sum(x => x.SetQuantity * x.SetsRequired);
                                    else
                                        TotalItems.TryAdd(LuminaSheets.WorkshopSupplyItemSheet[item.Select(x => x.SupplyItem.RowId).First()].Item.RowId, item.Sum(x => x.SetQuantity * x.SetsRequired));

                                }

                                foreach (var item in TotalItems)
                                {
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    ImGui.Text($"{LuminaSheets.ItemSheet[item.Key].Name.ToString()}");
                                    ImGui.TableNextColumn();
                                    ImGui.Text($"{item.Value * NumberOfLoops}");
                                    ImGui.TableNextColumn();
                                    int invCount = CraftingListUI.NumberOfIngredient(item.Key);
                                    ImGui.Text($"{invCount}");
                                    bool hasEnoughInInv = invCount >= item.Value * NumberOfLoops;
                                    if (hasEnoughInInv)
                                    {
                                        var color = ImGuiColors.HealerGreen;
                                        color.W -= 0.3f;
                                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.ColorConvertFloat4ToU32(color));
                                    }
                                    if (RetainerInfo.ATools)
                                    {
                                        ImGui.TableNextColumn();
                                        ImGui.Text($"{RetainerInfo.GetRetainerItemCount(item.Key)}");

                                        bool hasEnoughWithRetainer = (invCount + RetainerInfo.GetRetainerItemCount(item.Key)) >= item.Value * NumberOfLoops;
                                        if (!hasEnoughInInv && hasEnoughWithRetainer)
                                        {
                                            var color = ImGuiColors.DalamudOrange;
                                            color.W -= 0.6f;
                                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.ColorConvertFloat4ToU32(color));
                                        }
                                    }
                                }

                                ImGui.EndTable();
                            }

                            ImGui.InputInt("次数###LoopPart", ref NumberOfLoops);


                            if (ImGui.Button($"为此部件创建制作清单", new Vector2(ImGui.GetContentRegionAvail().X, 24f.Scale())))
                            {
                                Notify.Info($"正在创建清单，请等待...");
                                Task.Run(() => CreatePartList(part, partNum, false)).ContinueWith((_) => Notify.Success("部队工房清单已创建"));
                            }

                            if (ImGui.Button($"为此项目创建制作清单（包含前置配方）", new Vector2(ImGui.GetContentRegionAvail().X, 24f.Scale())))
                            {
                                Notify.Info($"正在创建清单，请等待...");
                                Task.Run(() => CreatePartList(part, partNum, true)).ContinueWith((_) => Notify.Success("部队工房清单已创建"));
                            }
                        }
                    }
                    ImGui.Unindent();
                }

                if (ImGui.CollapsingHeader("项目阶段"))
                {
                    string pNum = "";
                    foreach (var part in project.CompanyCraftPart.Where(x => x.RowId > 0).Select(x => x.Value))
                    {
                        ImGui.Indent();
                        int phaseNum = 1;
                        pNum = part.CompanyCraftType.Value.Name.ToString();
                        foreach (var phase in part.CompanyCraftProcess.Where(x => x.RowId > 0))
                        {
                            if (ImGui.CollapsingHeader($"{pNum} - 阶段 {phaseNum}"))
                            {
                                if (ImGui.BeginTable($"###FCWorkshopPhaseContainer{phase.RowId}", RetainerInfo.ATools ? 6 : 5, ImGuiTableFlags.Borders))
                                {
                                    ImGui.TableSetupColumn($"物品", ImGuiTableColumnFlags.WidthFixed);
                                    ImGui.TableSetupColumn($"每合建需求数量", ImGuiTableColumnFlags.WidthFixed);
                                    ImGui.TableSetupColumn($"合建次数", ImGuiTableColumnFlags.WidthFixed);
                                    ImGui.TableSetupColumn($"需求总数量", ImGuiTableColumnFlags.WidthFixed);
                                    ImGui.TableSetupColumn($"包裹库存", ImGuiTableColumnFlags.WidthFixed);
                                    if (RetainerInfo.ATools) ImGui.TableSetupColumn($"雇员", ImGuiTableColumnFlags.WidthFixed);
                                    ImGui.TableHeadersRow();

                                    foreach (var item in phase.Value.SupplyItems().Where(x => x.SupplyItem.RowId > 0))
                                    {
                                        ImGui.TableNextRow();
                                        ImGui.TableNextColumn();
                                        ImGui.Text($"{LuminaSheets.WorkshopSupplyItemSheet[item.SupplyItem.RowId].Item.Value.Name.ToString()}");
                                        ImGui.TableNextColumn();
                                        ImGui.Text($"{item.SetQuantity}");
                                        ImGui.TableNextColumn();
                                        ImGui.Text($"{item.SetsRequired * NumberOfLoops}");
                                        ImGui.TableNextColumn();
                                        ImGui.Text($"{item.SetsRequired * item.SetQuantity * NumberOfLoops}");
                                        ImGui.TableNextColumn();
                                        int invCount = CraftingListUI.NumberOfIngredient(LuminaSheets.WorkshopSupplyItemSheet[item.SupplyItem.RowId].Item.RowId);
                                        ImGui.Text($"{invCount}");
                                        bool hasEnoughInInv = invCount >= (item.SetQuantity * item.SetsRequired);
                                        if (hasEnoughInInv)
                                        {
                                            var color = ImGuiColors.HealerGreen;
                                            color.W -= 0.3f;
                                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.ColorConvertFloat4ToU32(color));
                                        }
                                        if (RetainerInfo.ATools)
                                        {
                                            ImGui.TableNextColumn();
                                            ImGui.Text($"{RetainerInfo.GetRetainerItemCount(LuminaSheets.WorkshopSupplyItemSheet[item.SupplyItem.RowId].Item.RowId)}");

                                            bool hasEnoughWithRetainer = (invCount + RetainerInfo.GetRetainerItemCount(LuminaSheets.WorkshopSupplyItemSheet[item.SupplyItem.RowId].Item.RowId)) >= (item.SetQuantity * item.SetsRequired);
                                            if (!hasEnoughInInv && hasEnoughWithRetainer)
                                            {
                                                var color = ImGuiColors.DalamudOrange;
                                                color.W -= 0.6f;
                                                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.ColorConvertFloat4ToU32(color));
                                            }
                                        }

                                    }

                                    ImGui.EndTable();
                                }

                                ImGui.InputInt("次数###LoopPhase", ref NumberOfLoops);

                                if (ImGui.Button($"为此阶段创建制作清单###PhaseButton{phaseNum}", new Vector2(ImGui.GetContentRegionAvail().X, 24f.Scale())))
                                {
                                    Notify.Info($"正在创建清单，请等待...");
                                    Task.Run(() => CreatePhaseList(phase.Value!, pNum, phaseNum, false)).ContinueWith((_) => Notify.Success("部队工房清单已创建"));
                                }

                                if (ImGui.Button($"为此阶段创建制作清单（包含前置配方）###PhaseButtonPC{phaseNum}", new Vector2(ImGui.GetContentRegionAvail().X, 24f.Scale())))
                                {
                                    Notify.Info($"正在创建清单，请等待...");
                                    Task.Run(() => CreatePhaseList(phase.Value!, pNum, phaseNum, true)).ContinueWith((_) => Notify.Success("部队工房清单已创建"));
                                }

                            }
                            phaseNum++;
                        }
                        ImGui.Unindent();
                    }

                }
            }
        }

        private static void CreatePartList(CompanyCraftPart value, string partNum, bool includePrecraft, NewCraftingList? existingList = null)
        {
            if (existingList == null)
            {
                existingList = new NewCraftingList();
                existingList.Name = $"{CurrentProject.ResultItem.Value.Name.ToString()} - 部件 {partNum} x{NumberOfLoops}";
                existingList.SetID();
                existingList.Save(true);
            }

            var phaseNum = 1;
            foreach (var phase in value.CompanyCraftProcess.Where(x => x.RowId > 0))
            {
                CreatePhaseList(phase.Value!, partNum, phaseNum, includePrecraft, existingList);
                phaseNum++;
            }
        }

        private static void CreateProjectList(CompanyCraftSequence value, bool includePrecraft)
        {
            NewCraftingList existingList = new NewCraftingList();
            existingList.Name = $"{CurrentProject.ResultItem.Value.Name.ToString()} x{NumberOfLoops}";
            existingList.SetID();
            existingList.Save(true);

            foreach (var part in value.CompanyCraftPart.Where(x => x.RowId > 0))
            {
                string partNum = part.Value.CompanyCraftType.Value.Name.ToString();
                var phaseNum = 1;
                foreach (var phase in part.Value.CompanyCraftProcess.Where(x => x.RowId > 0))
                {
                    CreatePhaseList(phase.Value!, partNum, phaseNum, includePrecraft, existingList);
                    phaseNum++;
                }

            }
        }

        public static void CreatePhaseList(CompanyCraftProcess value, string partNum, int phaseNum, bool includePrecraft, NewCraftingList? existingList = null, CompanyCraftSequence? projectOverride = null)
        {
            if (existingList == null)
            {
                existingList = new NewCraftingList();
                if (projectOverride != null)
                {
                    existingList.Name = $"{projectOverride.Value.ResultItem.Value.Name.ToString()} - {partNum}, 阶段 {phaseNum} x{NumberOfLoops}";
                }
                else
                {
                    existingList.Name = $"{CurrentProject.ResultItem.Value.Name.ToString()} - {partNum}, 阶段 {phaseNum} x{NumberOfLoops}";
                }
                existingList.SetID();
                existingList.Save(true);
            }


                foreach (var item in value.SupplyItems().Where(x => x.SupplyItem.RowId > 0))
                {
                    var timesToAdd = item.SetsRequired * item.SetQuantity * NumberOfLoops;
                    var supplyItemID = LuminaSheets.WorkshopSupplyItemSheet[item.SupplyItem.RowId].Item.Value.RowId;
                    if (LuminaSheets.RecipeSheet.Values.Any(x => x.ItemResult.RowId == supplyItemID))
                    {
                        var recipeID = LuminaSheets.RecipeSheet.Values.First(x => x.ItemResult.RowId == supplyItemID);
                        if (includePrecraft)
                        {
                            Svc.Log.Debug($"我想添加 {recipeID.ItemResult.Value.Name.ToDalamudString().ToString()} {timesToAdd} 次");
                            CraftingListUI.AddAllSubcrafts(recipeID, existingList, timesToAdd);
                        }

                        if (existingList.Recipes.Any(x => x.ID == recipeID.RowId))
                        {
                            var addition = timesToAdd / recipeID.AmountResult;
                            existingList.Recipes.First(x => x.ID == recipeID.RowId).Quantity += addition;
                        }
                        else
                        {
                            ListItem listItem = new ListItem()
                            {
                                ID = recipeID.RowId,
                                Quantity = timesToAdd / recipeID.AmountResult,
                                ListItemOptions = new ListItemOptions()
                            };

                            existingList.Recipes.Add(listItem);
                        }
                    }
                }
            
            P.Config.Save();

        }
    }
}
