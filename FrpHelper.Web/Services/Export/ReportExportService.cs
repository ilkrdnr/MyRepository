using System.Xml.Linq;
using FrpHelper.Web.Models;
using Microsoft.JSInterop;

namespace FrpHelper.Web.Services.Export;

public sealed class ReportExportService(IJSRuntime jsRuntime) : IReportExportService
{
    private static readonly string[] SqlAttributeCandidates = ["SelectCommand", "SQL.Text"];

    public async Task DownloadReportXmlAsync(FrpReportDocument report, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(report.OriginalXml))
        {
            throw new InvalidOperationException("Rapor XML içeriği bulunamadı.");
        }

        var document = XDocument.Parse(report.OriginalXml, LoadOptions.PreserveWhitespace);
        var sqlNodes = document.Descendants()
            .Where(static element => element.Attributes().Any(static attr =>
                string.Equals(attr.Name.LocalName, "SelectCommand", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(attr.Name.LocalName, "SQL.Text", StringComparison.OrdinalIgnoreCase) ||
                attr.Name.LocalName.Contains("SQL.Text", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        for (var i = 0; i < sqlNodes.Count && i < report.SqlSections.Count; i++)
        {
            SetAttributeValue(sqlNodes[i], report.SqlSections[i].Content, SqlAttributeCandidates);
        }

        var scriptIndex = 0;
        var root = document.Root;
        if (root is not null && HasAttribute(root, "ScriptText.Text") && scriptIndex < report.ScriptSections.Count)
        {
            SetAttributeValue(root, report.ScriptSections[scriptIndex].Content, "ScriptText.Text");
            scriptIndex++;
        }

        var scriptNodes = document.Descendants()
            .Where(static element => string.Equals(element.Name.LocalName, "ScriptText", StringComparison.OrdinalIgnoreCase))
            .ToList();

        for (var i = 0; i < scriptNodes.Count && scriptIndex < report.ScriptSections.Count; i++)
        {
            scriptNodes[i].Value = report.ScriptSections[scriptIndex].Content;
            scriptIndex++;
        }

        if (root is not null && scriptIndex < report.ScriptSections.Count)
        {
            SetAttributeValue(root, report.ScriptSections[scriptIndex].Content, "ScriptText.Text");
        }

        var xmlContent = document.ToString(SaveOptions.None);
        var bytes = System.Text.Encoding.UTF8.GetBytes(xmlContent);
        var base64 = Convert.ToBase64String(bytes);
        var targetFileName = $"{Path.GetFileNameWithoutExtension(report.FileName)}.frp";

        await jsRuntime.InvokeVoidAsync("frpHelper.downloadBase64", cancellationToken, targetFileName, "application/octet-stream", base64);
    }

    private static void SetAttributeValue(XElement element, string value, params string[] candidateNames)
    {
        foreach (var attribute in element.Attributes())
        {
            if (candidateNames.Any(name =>
                    string.Equals(name, attribute.Name.LocalName, StringComparison.OrdinalIgnoreCase) ||
                    attribute.Name.LocalName.Contains(name, StringComparison.OrdinalIgnoreCase)))
            {
                attribute.Value = value;
                return;
            }
        }

        element.SetAttributeValue(candidateNames[0], value);
    }

    private static bool HasAttribute(XElement element, string name) =>
        element.Attributes().Any(attribute =>
            string.Equals(attribute.Name.LocalName, name, StringComparison.OrdinalIgnoreCase));
}
