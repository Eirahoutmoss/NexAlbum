namespace NexAlbum.Export.Tests;

/// <summary>Pillow ile üretilmiş gerçek görüntüler (piksel verisi var).</summary>
internal static class Fixtures
{
    /// <summary>60×40 JPEG, EXIF yönü 6 (görüntülenen 40×60).</summary>
    public static readonly byte[] Jpeg = Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/4QA2RXhpZgAATU0AKgAAAAgAAgEQAAIAAAAIAAAAJgESAAMAAAABAAYAAAAAAABUZXN0Q2FtAP/bAEMABgQFBgUEBgYFBgcHBggKEAoKCQkKFA4PDBAXFBgYFxQWFhodJR8aGyMcFhYgLCAjJicpKikZHy0wLSgwJSgpKP/bAEMBBwcHCggKEwoKEygaFhooKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKP/AABEIACgAPAMBIgACEQEDEQH/xAAfAAABBQEBAQEBAQAAAAAAAAAAAQIDBAUGBwgJCgv/xAC1EAACAQMDAgQDBQUEBAAAAX0BAgMABBEFEiExQQYTUWEHInEUMoGRoQgjQrHBFVLR8CQzYnKCCQoWFxgZGiUmJygpKjQ1Njc4OTpDREVGR0hJSlNUVVZXWFlaY2RlZmdoaWpzdHV2d3h5eoOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4eLj5OXm5+jp6vHy8/T19vf4+fr/xAAfAQADAQEBAQEBAQEBAAAAAAAAAQIDBAUGBwgJCgv/xAC1EQACAQIEBAMEBwUEBAABAncAAQIDEQQFITEGEkFRB2FxEyIygQgUQpGhscEJIzNS8BVictEKFiQ04SXxFxgZGiYnKCkqNTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqCg4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2dri4+Tl5ufo6ery8/T19vf4+fr/2gAMAwEAAhEDEQA/AOGooor9KPkgrQ8P/wDIXt/+Bf8AoJrPrQ8P/wDIXt/+Bf8AoJrzs3/3Cv8A4Jf+ks7cu/3ul/ij+aOyoq9LpV7E8KSQENNbm6T5hzGFLbuv90E461Rr8LcWt0fqqknswoooqRnntFFFf0OfjgVoeH/+Qvb/APAv/QTWfWh4f/5C9v8A8C/9BNedm/8AuFf/AAS/9JZ25d/vdL/FH80euxavZSeck8xCx2IW3fYT+8NoInTp3bHPT5feuUoor8NnVc9z9ShTUNgooorM0PPaKKK/oc/HArQ8P/8AIXt/+Bf+gmiivOzf/cK/+CX/AKSzty7/AHul/ij+aOyooor8JP1YKKKKAP/Z");

    /// <summary>20×30 PNG.</summary>
    public static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAABQAAAAeCAIAAACjcKk8AAAAIklEQVR4nGPkWhDFQC5gIlvnqOZRzaOaRzWPah7VPAQ0AwAeUgFAi1B09gAAAABJRU5ErkJggg==");

    /// <summary>20×30 GIF (PDFsharp desteklemez).</summary>
    public static readonly byte[] Gif = Convert.FromBase64String("R0lGODdhFAAeAIEAAAqgWgAAAAAAAAAAACwAAAAAFAAeAEAIKgABCBxIsKDBgwgTKlzIsKHDhxAjSpxIsaLFixgzatzIsaPHjyBDiiwYEAA7");
}
