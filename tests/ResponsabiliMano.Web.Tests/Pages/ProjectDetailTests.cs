using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FeatureManagement;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Web;
using ResponsabiliMano.Web.Components.Pages;
using ResponsabiliMano.Web.Tests.TestHelpers;

namespace ResponsabiliMano.Web.Tests.Pages;

/// <summary>
/// bUnit tests for the ProjectDetail page: change-request summaries and the
/// "compare progress" partner name.
/// </summary>
public class ProjectDetailTests : TestContext
{
    private readonly FakeFeatureManager _featureManager = new();
    private readonly FakeProjectService _projectService = new();
    private readonly FakeAuthStateProvider _authStateProvider = new();

    public ProjectDetailTests()
    {
        Services.AddSingleton<IProjectService>(_projectService);
        Services.AddSingleton<IFeatureManager>(_featureManager);
        Services.AddSingleton<AuthenticationStateProvider>(_authStateProvider);
        Services.AddSingleton<IStringLocalizer<AppStrings>>(new ProjectDetailLocalizer());
        Services.AddSingleton<ILogger<ProjectDetail>>(NullLogger<ProjectDetail>.Instance);
    }

    [Fact]
    public void Renders_change_request_end_date_summary()
    {
        _featureManager.Enabled = true;
        var newEndDate = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc);
        var payload = $"{{\"EndDate\":\"{newEndDate:O}\"}}";

        var project = ActiveProjectWithChangeRequest(ChangeRequestType.EndDate, payload);

        var cut = RenderComponent<ProjectDetail>(p => p.Add(x => x.ProjectId, project.Id));
        SelectChangeRequestsTab(cut);

        var summary = cut.Find("[data-test='change-request-summary']").TextContent;
        Assert.Contains("28/08/2026", summary);
    }

    [Fact]
    public void Renders_change_request_frequency_summary()
    {
        _featureManager.Enabled = true;
        var payload = "{\"Frequency\":3}";

        var project = ActiveProjectWithChangeRequest(ChangeRequestType.Frequency, payload);

        var cut = RenderComponent<ProjectDetail>(p => p.Add(x => x.ProjectId, project.Id));
        SelectChangeRequestsTab(cut);

        var summary = cut.Find("[data-test='change-request-summary']").TextContent;
        Assert.Contains("Mensal", summary);
    }

    [Fact]
    public void Renders_change_request_goals_summary()
    {
        _featureManager.Enabled = true;
        var payload = "{\"Goals\":[{\"Label\":\"Peso\",\"DataType\":0,\"Unit\":\"Kg\",\"MinValue\":80,\"MaxValue\":120}]}";

        var project = ActiveProjectWithChangeRequest(ChangeRequestType.Goals, payload);

        var cut = RenderComponent<ProjectDetail>(p => p.Add(x => x.ProjectId, project.Id));
        SelectChangeRequestsTab(cut);

        var summary = cut.Find("[data-test='change-request-summary']").TextContent;
        Assert.Contains("Peso", summary);
        Assert.Contains("Kg", summary);
        Assert.Contains("80", summary);
        Assert.Contains("120", summary);
    }

    [Fact]
    public void Renders_dashboard_compare_text_with_partner_name_for_creator()
    {
        _featureManager.Enabled = true;

        var creatorId = FakeAuthStateProvider.UserId;
        var partnerId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var project = ActiveProject(creatorId, partnerId);

        var cut = RenderComponent<ProjectDetail>(p => p.Add(x => x.ProjectId, project.Id));

        var element = cut.Find("[data-test='dashboard-compare-text']");
        Assert.Contains("Marcelo Arruda", element.TextContent);
        Assert.DoesNotContain("Eduardo Arruda", element.TextContent);
    }

    [Fact]
    public void Renders_dashboard_compare_text_with_creator_name_for_partner()
    {
        _featureManager.Enabled = true;

        var creatorId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var partnerId = FakeAuthStateProvider.UserId;
        var project = ActiveProject(creatorId, partnerId);

        var cut = RenderComponent<ProjectDetail>(p => p.Add(x => x.ProjectId, project.Id));

        var element = cut.Find("[data-test='dashboard-compare-text']");
        Assert.Contains("Eduardo Arruda", element.TextContent);
        Assert.DoesNotContain("Marcelo Arruda", element.TextContent);
    }

    [Fact]
    public void Invalid_payload_does_not_break_rendering()
    {
        _featureManager.Enabled = true;

        var project = ActiveProjectWithChangeRequest(ChangeRequestType.Goals, "not-json");

        var cut = RenderComponent<ProjectDetail>(p => p.Add(x => x.ProjectId, project.Id));
        SelectChangeRequestsTab(cut);

        Assert.Contains("change-request-summary", cut.Markup);
        Assert.DoesNotContain("not-json", cut.Markup);
    }

    private static void SelectChangeRequestsTab(IRenderedComponent<ProjectDetail> cut)
    {
        IElement? secondTab = null;
        foreach (var tab in cut.FindAll(".tabs__trigger"))
        {
            if (secondTab is null)
            {
                secondTab = tab;
                continue;
            }

            tab.Click();
            return;
        }
    }

    private Project ActiveProjectWithChangeRequest(ChangeRequestType type, string payload)
    {
        var creatorId = FakeAuthStateProvider.UserId;
        var partnerId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var project = ActiveProject(creatorId, partnerId);
        project.ChangeRequests.Add(new ProjectChangeRequest
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            RequestedByUserId = creatorId,
            Type = type,
            PayloadJson = payload,
            Status = ChangeRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        return project;
    }

    private Project ActiveProject(Guid creatorId, Guid partnerId)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Projeto Verão",
            Icon = "🎯",
            CreatorId = creatorId,
            PartnerId = partnerId,
            StartDate = new DateTime(2026, 7, 29, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc),
            Frequency = ProjectFrequency.Weekly,
            Status = ProjectStatus.Active,
            Creator = new User { Id = creatorId, Name = "Eduardo Arruda", Email = "eduardo@example.com" },
            Partner = new User { Id = partnerId, Name = "Marcelo Arruda", Email = "marcelo@example.com" }
        };

        _projectService.Result = project;
        return project;
    }

    /// <summary>
    /// Localizer that echoes keys by default, but formats the specific strings used
    /// by ProjectDetail so assertions can verify real interpolated output.
    /// </summary>
    private sealed class ProjectDetailLocalizer : IStringLocalizer<AppStrings>
    {
        public LocalizedString this[string name]
        {
            get
            {
                var value = name switch
                {
                    "ChangeRequestsTitle" => "Solicitações de Alteração",
                    "ChangeRequestSummaryEndDate" => "Nova data de fim: {0:dd/MM/yyyy}",
                    "ChangeRequestSummaryFrequency" => "Nova frequência: {0}",
                    "ChangeRequestSummaryGoals" => "Metas propostas:",
                    "ChangeRequestSummaryGoalLine" => "{0}: {1} {2}",
                    "ChangeRequestSummaryGoalMeta" => "Tipo: {0} · Unidade: {1}",
                    "ChangeRequestSummaryGoalRange" => "Intervalo: {0} a {1} {2}",
                    "ChangeRequestSummaryNoGoals" => "Nenhuma meta informada.",
                    "DashboardCompareText" => "Você e {0} lado a lado",
                    "DataTypeDecimal" => "Decimal",
                    "DataTypeInteger" => "Inteiro",
                    "DataTypePercent" => "Percentual",
                    "FrequencyDaily" => "Diária",
                    "FrequencyWeekly" => "Semanal",
                    "FrequencyBiweekly" => "Quinzenal",
                    "FrequencyMonthly" => "Mensal",
                    "NoPartnerYet" => "Sem parceiro",
                    _ => name
                };

                return new LocalizedString(name, value);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var format = this[name].Value;
                return new LocalizedString(name, string.Format(CultureInfo.GetCultureInfo("pt-BR"), format, arguments));
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
