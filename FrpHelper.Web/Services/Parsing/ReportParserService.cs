using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using FrpHelper.Web.Models;

namespace FrpHelper.Web.Services.Parsing;

public sealed class ReportParserService : IReportParserService
{
    public async Task<FrpReportDocument> ParseAsync(string fileName, Stream fileStream, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, cancellationToken);

        var sourceBytes = memoryStream.ToArray();
        var (xmlDocument, xmlText, sourceFormat) = LoadDocument(sourceBytes);

        var metadata = ReadMetadata(xmlDocument, fileName);
        var sqlSections = ReadSqlSections(xmlDocument);
        var scriptSections = ReadScriptSections(xmlDocument);

        return new FrpReportDocument
        {
            FileName = fileName,
            SourceBytes = sourceBytes,
            OriginalXml = xmlText,
            SourceFormat = sourceFormat,
            Metadata = metadata,
            SqlSections = sqlSections,
            ScriptSections = scriptSections
        };
    }

    private static (XDocument Document, string XmlText, string SourceFormat) LoadDocument(byte[] sourceBytes)
    {
        if (LooksLikeGZip(sourceBytes))
        {
            var unpackedBytes = DecompressGZip(sourceBytes);
            var document = ParseXml(unpackedBytes);
            return (document, document.ToString(SaveOptions.DisableFormatting), "gzip");
        }

        if (LooksLikeZip(sourceBytes))
        {
            var unpackedBytes = ExtractXmlFromZip(sourceBytes);
            var document = ParseXml(unpackedBytes);
            return (document, document.ToString(SaveOptions.DisableFormatting), "zip");
        }

        var xmlDocument = ParseXml(sourceBytes);
        return (xmlDocument, xmlDocument.ToString(SaveOptions.DisableFormatting), "xml");
    }

    private static XDocument ParseXml(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        try
        {
            return XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Dosya XML olarak okunamadı. FRP/FRX ya da sıkıştırılmış bir XML dosyası yükleyin.", exception);
        }
    }

    private static byte[] DecompressGZip(byte[] sourceBytes)
    {
        using var input = new MemoryStream(sourceBytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] ExtractXmlFromZip(byte[] sourceBytes)
    {
        using var input = new MemoryStream(sourceBytes);
        using var zip = new ZipArchive(input, ZipArchiveMode.Read);

        var entry = zip.Entries
            .FirstOrDefault(static e =>
                e.FullName.EndsWith(".frx", StringComparison.OrdinalIgnoreCase) ||
                e.FullName.EndsWith(".frp", StringComparison.OrdinalIgnoreCase) ||
                e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            throw new InvalidOperationException("ZIP içinde .frx/.frp/.xml uzantılı dosya bulunamadı.");
        }

        using var entryStream = entry.Open();
        using var output = new MemoryStream();
        entryStream.CopyTo(output);
        return output.ToArray();
    }

    private static bool LooksLikeGZip(byte[] bytes) =>
        bytes.Length > 2 && bytes[0] == 0x1F && bytes[1] == 0x8B;

    private static bool LooksLikeZip(byte[] bytes) =>
        bytes.Length > 3 && bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04;

    private static ReportMetadata ReadMetadata(XDocument document, string fileName)
    {
        var root = document.Root;
        var reportInfo = document.Descendants().FirstOrDefault(static node =>
            string.Equals(node.Name.LocalName, "ReportInfo", StringComparison.OrdinalIgnoreCase));

        var reportName =
            GetAttributeValue(root, "ReportName", "Name", "Alias") ??
            GetAttributeValue(reportInfo, "Name") ??
            Path.GetFileNameWithoutExtension(fileName) ??
            fileName;

        var reportCode =
            GetAttributeValue(root, "ReportCode", "Code", "Alias") ??
            GetAttributeValue(reportInfo, "Code") ??
            string.Empty;

        var createdAtRaw =
            GetAttributeValue(reportInfo, "Created") ??
            GetAttributeValue(root, "Created");

        var modifiedAtRaw =
            GetAttributeValue(reportInfo, "Modified") ??
            GetAttributeValue(root, "Modified");

        return new ReportMetadata
        {
            ReportName = reportName,
            ReportCode = reportCode,
            CreatedAt = ParseDate(createdAtRaw),
            ModifiedAt = ParseDate(modifiedAtRaw)
        };
    }

    private static List<ReportSqlSection> ReadSqlSections(XDocument document)
    {
        var sqlElements = document.Descendants()
            .Where(static element => !string.IsNullOrWhiteSpace(GetAttributeValue(element, "SelectCommand")))
            .ToList();

        var sections = new List<ReportSqlSection>();

        for (var i = 0; i < sqlElements.Count; i++)
        {
            var element = sqlElements[i];
            var title =
                GetAttributeValue(element, "Name", "Alias", "TableName", "ReferenceName") ??
                $"SQL {i + 1}";

            sections.Add(new ReportSqlSection
            {
                Title = title,
                Content = GetAttributeValue(element, "SelectCommand") ?? string.Empty
            });
        }

        return sections;
    }

    private static List<ReportScriptSection> ReadScriptSections(XDocument document)
    {
        var scriptNodes = document.Descendants()
            .Where(static element => string.Equals(element.Name.LocalName, "ScriptText", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var sections = new List<ReportScriptSection>();

        for (var i = 0; i < scriptNodes.Count; i++)
        {
            sections.Add(new ReportScriptSection
            {
                Title = $"Pascal Script {i + 1}",
                Content = scriptNodes[i].Value
            });
        }

        if (sections.Count == 0)
        {
            sections.Add(new ReportScriptSection
            {
                Title = "Pascal Script",
                Content = string.Empty
            });
        }

        return sections;
    }

    private static string? GetAttributeValue(XElement? element, params string[] attributeNames)
    {
        if (element is null)
        {
            return null;
        }

        foreach (var attribute in element.Attributes())
        {
            if (attributeNames.Any(name => string.Equals(name, attribute.Name.LocalName, StringComparison.OrdinalIgnoreCase)))
            {
                return attribute.Value;
            }
        }

        return null;
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
