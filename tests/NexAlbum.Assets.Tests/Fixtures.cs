namespace NexAlbum.Assets.Tests;

/// <summary>
/// Elle üretilmiş küçük görüntü dosyaları (yalnızca başlık; piksel verisi yok).
/// JPEG: 6000×4000, EXIF yön 6, model "TestCam", lens "TestLens", 2024:09:15 14:30:00 +03:00, ISO 400, 50 mm.
/// PNG: 1200×1800, EXIF yok.
/// </summary>
internal static class Fixtures
{
    public static readonly byte[] Jpeg = Convert.FromBase64String("/9j/4QCyRXhpZgAASUkqAAgAAAADABABAgAIAAAAMgAAABIBAwABAAAABgAAAGmHBAABAAAAOgAAAAAAAABUZXN0Q2FtAAUAJ4gDAAEAAACQAQAAA5ACABQAAAB8AAAAEZACAAcAAACQAAAACpIFAAEAAACYAAAANKQCAAkAAACgAAAAAAAAADIwMjQ6MDk6MTUgMTQ6MzA6MDAAKzAzOjAwAAAyAAAAAQAAAFRlc3RMZW5zAAD/wAARCA+gF3ADAREAAhEBAxEB/9k=");

    public static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAABLAAAAcICAIAAACjMq8dAAAAAElFTkSuQmCC");
}
