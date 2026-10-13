using System.Globalization;
using System.Text;
using NexAlbum.Core.Projects;

namespace NexAlbum.Integrations.Photoshop;

/// <summary>Photoshop'taki bir action.</summary>
public sealed record PhotoshopAction(string Set, string Name)
{
    public string DisplayName => Set + " / " + Name;
}

/// <summary>
/// Photoshop'a gönderilen ExtendScript metinleri. Dışarıdan gelen her değer (yol, action adı) JSON kaçışıyla
/// dize sabiti olarak gömülür; betik içine kod enjekte edilemez (Karar 20 §3).
/// </summary>
public static class PhotoshopScripts
{
    /// <summary>Başarılı işlemin yanıtı.</summary>
    public const string Ok = "OK";

    /// <summary>Hata yanıtının öneki; ardından Photoshop'un hata metni gelir.</summary>
    public const string ErrorPrefix = "ERR:";

    /// <summary>Tüm action setlerini ve action'ları "set\taction" satırları olarak döndüren betik (ActionManager).</summary>
    public static string ListActions() => """
        (function () {
            var out = [];
            for (var i = 1; ; i++) {
                var ref = new ActionReference();
                ref.putIndex(charIDToTypeID("ASet"), i);
                var desc;
                try { desc = executeActionGet(ref); } catch (e) { break; }
                var setName = desc.getString(charIDToTypeID("Nm  "));
                var count = desc.hasKey(charIDToTypeID("NmbC")) ? desc.getInteger(charIDToTypeID("NmbC")) : 0;
                for (var j = 1; j <= count; j++) {
                    var r2 = new ActionReference();
                    r2.putIndex(charIDToTypeID("Actn"), j);
                    r2.putIndex(charIDToTypeID("ASet"), i);
                    out.push(setName + "\t" + executeActionGet(r2).getString(charIDToTypeID("Nm  ")));
                }
            }
            return out.join("\n");
        })();
        """;

    /// <summary>
    /// Dosyayı açar, action'ı diyaloglar kapalıyken oynatır, sonucu kopya olarak kaydeder ve açtığı her belgeyi
    /// kaydetmeden kapatır. JPEG için 8 bit'e çevrilir ve renk profili gömülür.
    /// </summary>
    public static string Process(string input, string output, EditRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        var save = recipe.Output == EditOutputFormat.Png
            ? """
                var png = new PNGSaveOptions();
                png.compression = 6;
                png.interlaced = false;
                target.saveAs(outFile, png, true, Extension.LOWERCASE);
              """
            : $$"""
                if (target.bitsPerChannel != BitsPerChannelType.EIGHT) { target.bitsPerChannel = BitsPerChannelType.EIGHT; }
                var jpg = new JPEGSaveOptions();
                jpg.quality = {{recipe.JpegQuality.ToString(CultureInfo.InvariantCulture)}};
                jpg.embedColorProfile = true;
                jpg.formatOptions = FormatOptions.STANDARDBASELINE;
                jpg.matte = MatteType.NONE;
                target.saveAs(outFile, jpg, true, Extension.LOWERCASE);
              """;

        return $$"""
            (function () {
                var before = app.documents.length;
                var dialogs = app.displayDialogs;
                app.displayDialogs = DialogModes.NO;
                try {
                    app.open(new File({{Js(input)}}));
                    app.doAction({{Js(recipe.Action ?? string.Empty)}}, {{Js(recipe.ActionSet ?? string.Empty)}});
                    var target = app.activeDocument;
                    var outFile = new File({{Js(output)}});
            {{save}}
                    return "{{Ok}}";
                } catch (e) {
                    return "{{ErrorPrefix}}" + e.message + (e.line ? " (satır " + e.line + ")" : "");
                } finally {
                    try {
                        while (app.documents.length > before) { app.activeDocument.close(SaveOptions.DONOTSAVECHANGES); }
                    } catch (closeError) { }
                    app.displayDialogs = dialogs;
                }
            })();
            """;
    }

    /// <summary>"set\taction" satırlarını ayrıştırır; boş ve bozuk satırlar atlanır.</summary>
    public static IReadOnlyList<PhotoshopAction> ParseActions(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var actions = new List<PhotoshopAction>();
        foreach (var line in text.Split('\n'))
        {
            var parts = line.TrimEnd('\r').Split('\t');
            if (parts.Length == 2 && parts[0].Length > 0 && parts[1].Length > 0)
            {
                actions.Add(new PhotoshopAction(parts[0], parts[1]));
            }
        }

        return actions;
    }

    /// <summary>JavaScript dize sabiti (çift tırnaklı, kaçışlı).</summary>
    internal static string Js(string value)
    {
        var sb = new StringBuilder(value.Length + 2).Append('"');
        foreach (var c in value)
        {
            var escaped = c switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                '\u2028' => "\\u2028",
                '\u2029' => "\\u2029",
                < ' ' => "\\u" + ((int)c).ToString("x4", CultureInfo.InvariantCulture),
                _ => null,
            };
            if (escaped is null)
            {
                sb.Append(c);
            }
            else
            {
                sb.Append(escaped);
            }
        }

        return sb.Append('"').ToString();
    }
}
