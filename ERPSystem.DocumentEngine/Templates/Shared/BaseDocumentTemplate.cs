using System.Text;
using ERPSystem.DocumentEngine.Components;
using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Services;

namespace ERPSystem.DocumentEngine.Templates.Shared;

/// <summary>
/// Composes a complete, professional document body from the neutral
/// <see cref="DocumentModel"/> using only shared components. Every concrete
/// template inherits this and, by default, only declares its
/// <see cref="IDocumentTemplate.Type"/>. Templates may override
/// <see cref="RenderBody"/> to reorder sections, but the design language and
/// component set stay identical everywhere (zero duplicated markup).
/// </summary>
public abstract class BaseDocumentTemplate : IDocumentTemplate
{
    public abstract DocumentType Type { get; }

    public virtual string RenderBody(DocumentModel model, RenderContext ctx)
    {
        var sb = new StringBuilder();

        sb.Append(TopMeta(model));
        sb.Append(SummarySection(model));
        sb.Append(TablesSection(model));
        sb.Append(FinancialSection(model));
        sb.Append(ApprovalSection(ctx, model));
        sb.Append(Components.Components.Timeline(ctx, model.Timeline));
        sb.Append(NotesTermsSection(ctx, model));
        sb.Append(Components.Components.Attachments(ctx, model.Attachments));
        sb.Append(ClosingSection(ctx, model));

        return sb.ToString();
    }

    /// <summary>Parties on one side, header meta grid on the other.</summary>
    protected static string TopMeta(DocumentModel model)
    {
        var hasParties = model.PrimaryParty is not null || model.SecondaryParty is not null;
        var hasFields = model.HeaderFields.Count > 0;
        if (!hasParties && !hasFields)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append("<div class=\"doc-row doc-row--between\">");

        sb.Append("<div class=\"doc-col flex-col gap-3\">");
        sb.Append(Components.Components.PartyCard(model.PrimaryParty));
        sb.Append(Components.Components.PartyCard(model.SecondaryParty));
        sb.Append("</div>");

        if (hasFields)
        {
            sb.Append("<div class=\"doc-col\">");
            sb.Append(Components.Components.InfoFields(model.HeaderFields));
            sb.Append("</div>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    protected static string SummarySection(DocumentModel model) =>
        Components.Components.SummaryCards(model.SummaryCards);

    protected static string TablesSection(DocumentModel model)
    {
        if (model.Tables.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var table in model.Tables)
        {
            sb.Append("<section class=\"doc-section\">");
            sb.Append(Components.Components.Table(table));
            sb.Append("</section>");
        }

        return sb.ToString();
    }

    /// <summary>Tax panel + notes on the start side, totals on the end side.</summary>
    protected static string FinancialSection(DocumentModel model)
    {
        var totals = Components.Components.TotalsPanel(model.Totals);
        var tax = Components.Components.TaxPanel(model.TaxLines);
        if (string.IsNullOrEmpty(totals) && string.IsNullOrEmpty(tax))
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append("<div class=\"doc-row doc-row--between\">");
        sb.Append("<div class=\"doc-col\">");
        sb.Append(tax);
        sb.Append("</div>");
        sb.Append("<div class=\"doc-col\">");
        sb.Append(totals);
        sb.Append("</div>");
        sb.Append("</div>");
        return sb.ToString();
    }

    protected static string ApprovalSection(RenderContext ctx, DocumentModel model)
    {
        var badge = Components.Components.ApprovalBadge(ctx, model.Approval);
        return string.IsNullOrEmpty(badge) ? string.Empty : $"<div class=\"flex justify-end\">{badge}</div>";
    }

    protected static string NotesTermsSection(RenderContext ctx, DocumentModel model)
    {
        var notes = Components.Components.NotesBlock(ctx, model.Notes);
        var terms = Components.Components.TermsBlock(ctx, model.Terms);
        if (string.IsNullOrEmpty(notes) && string.IsNullOrEmpty(terms))
        {
            return string.Empty;
        }

        return $"<div class=\"doc-grid doc-grid--2\">{notes}{terms}</div>";
    }

    /// <summary>Signatures + stamp + QR area at the foot of the content.</summary>
    protected static string ClosingSection(RenderContext ctx, DocumentModel model)
    {
        var signatures = Components.Components.Signatures(ctx, model.Signatures);
        var showQr = !string.IsNullOrWhiteSpace(ctx.Branding.QrContent) ||
                     !string.IsNullOrWhiteSpace(ctx.Branding.QrImageUrl);

        if (string.IsNullOrEmpty(signatures) && !showQr)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append("<div class=\"doc-row doc-row--between mt-6\">");
        sb.Append($"<div class=\"doc-col\">{signatures}</div>");
        if (showQr)
        {
            sb.Append($"<div class=\"flex-col items-center gap-2\">{Components.Components.Qr(ctx)}</div>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }
}
