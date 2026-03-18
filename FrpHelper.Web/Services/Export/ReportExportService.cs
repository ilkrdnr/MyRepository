using System.Xml.Linq;
using FrpHelper.Web.Models;
using Microsoft.JSInterop;

namespace FrpHelper.Web.Services.Export;

public sealed class ReportExportService(IJSRuntime jsRuntime) : IReportExportService
{
    public async Task DownloadReportXmlAsync(FrpReportDocument report, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(report.OriginalXml))
        {
            throw new InvalidOperationException("Rapor XML içeriği bulunamadı.");
        }

        var document = XDocument.Parse(report.OriginalXml, LoadOptions.PreserveWhitespace);
        var sqlNodes = document.Descendants()
            .Where(static element => element.Attributes().Any(static attr =>
                string.Equals(attr.Name.LocalName, "SelectCommand", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        for (var i = 0; i < sqlNodes.Count && i < report.SqlSections.Count; i++)
        {
            SetAttributeValue(sqlNodes[i], report.SqlSections[i].Content, "SelectCommand");
        }

        var scriptNodes = document.Descendants()
            .Where(static element => string.Equals(element.Name.LocalName, "ScriptText", StringComparison.OrdinalIgnoreCase))
            .ToList();

        for (var i = 0; i < scriptNodes.Count && i < report.ScriptSections.Count; i++)
        {
            scriptNodes[i].Value = report.ScriptSections[i].Content;
        }

        var xmlContent = document.ToString(SaveOptions.None);
        var bytes = System.Text.Encoding.UTF8.GetBytes(xmlContent);
        var base64 = Convert.ToBase64String(bytes);
        var targetFileName = $"{Path.GetFileNameWithoutExtension(report.FileName)}.frx";

        await jsRuntime.InvokeVoidAsync("frpHelper.downloadBase64", cancellationToken, targetFileName, "application/xml", base64);
    }

    private static void SetAttributeValue(XElement element, string value, params string[] candidateNames)
    {
        foreach (var attribute in element.Attributes())
        {
            if (candidateNames.Any(name => string.Equals(name, attribute.Name.LocalName, StringComparison.OrdinalIgnoreCase)))
            {
                attribute.Value = value;
                return;
            }
        }

        element.SetAttributeValue(candidateNames[0], value);
    }
}
