using System.Windows.Media;
using System.Windows.Media.Imaging;
using FluentAssertions;
using NexAlbum.Core.Design;
using NexAlbum.Core.Projects;
using NexAlbum.UI.Services;
using NexAlbum.UI.ViewModels;
using Xunit;

namespace NexAlbum.UI.Tests;

/// <summary>Fotoğraf ayarları paneli: kaydırıcılar, siyah-beyaz, düzeltme, döndürme, öncesi/sonrası ve geri alma.</summary>
public sealed partial class MainViewModelTests
{
    [Fact]
    public void PhotoAdjustmentsAreEditedComparedRotatedAndUndone()
    {
        var dialogs = new FakeDialogs();
        using var vm = new MainViewModel(dialogs, settings: new MemoryAppSettingsStore());
        vm.History.MergeWindow = TimeSpan.Zero;
        vm.Open(PagedProject(2));
        vm.SelectedPage = vm.Pages[0];
        vm.ResetAdjustmentsCommand.CanExecute(null).Should().BeFalse("seçili fotoğraf yok");

        vm.SelectSlot(vm.SelectedPage!.Slots[0]);
        vm.HasAdjustments.Should().BeFalse();
        vm.AdjustExposure = 40;
        vm.AdjustTemperature = -250;
        vm.AdjustBlackWhite = true;

        var adjustments = vm.SelectedSlot!.Slot.Placement!.Adjustments;
        adjustments.Exposure.Should().BeApproximately(0.4, 1e-9);
        adjustments.Temperature.Should().Be(-1, "kaydırıcı ±100 ile sınırlı");
        adjustments.BlackWhite.Should().BeTrue();
        vm.AdjustTemperature.Should().Be(-100);
        vm.HasAdjustments.Should().BeTrue();
        vm.ResetAdjustmentsCommand.CanExecute(null).Should().BeTrue();
        vm.UndoToolTip.Should().Contain("Fotoğraf ayarı");

        vm.Straighten = 4.5;
        vm.SelectedSlot!.ImageRotation.Should().Be(4.5);
        vm.SelectedSlot!.ImageWidth.Should().BeGreaterThan(vm.SelectedSlot.PhotoWidth, "düzeltilen fotoğraf yuvayı kaplamak için büyür");

        vm.CompareAdjustments = true;
        vm.SelectedSlot!.ShowUnadjusted.Should().BeTrue();
        vm.SelectSlot(null);
        vm.CompareAdjustments.Should().BeFalse("seçim değişince öncesi görünümü kapanır");
        vm.SelectSlot(vm.SelectedPage!.Slots[0]);

        vm.AdjustmentsToPage();
        dialogs.Confirmations.Should().Be(0, "sayfada tek fotoğraf varken sorulmaz");

        var asset = vm.SelectedSlot!.Asset!;
        var aspect = asset.DisplayAspectRatio;
        vm.RotateRightCommand.Execute(null);
        vm.Project!.FindAsset(asset.Id)!.DisplayAspectRatio.Should().BeApproximately(1 / aspect, 1e-9);
        vm.SelectedSlot.Should().NotBeNull("döndürme seçimi korur");
        vm.RotateLeftCommand.Execute(null);
        vm.Project!.FindAsset(asset.Id)!.ExifOrientation.Should().Be(1);

        vm.ResetAdjustments();
        vm.SelectedSlot!.Slot.Placement!.Adjustments.IsNone.Should().BeTrue();
        vm.Straighten.Should().Be(0);
        vm.UndoCommand.Execute(null);
        vm.AdjustBlackWhite.Should().BeTrue("sıfırlama geri alınır");
        vm.Straighten.Should().Be(4.5);
    }

    [Fact]
    public void ScreenPreviewUsesTheSameColorMatrixAsPrint()
    {
        WpfApp.Run(() =>
        {
            var pixels = new byte[] { 0, 0, 255, 255, 255, 0, 0, 255 }; // BGRA: kırmızı, mavi
            var source = BitmapSource.Create(2, 1, 96, 96, PixelFormats.Bgra32, null, pixels, 8);
            source.Freeze();

            var gray = new ImageAdjustments { BlackWhite = true };
            var image = (BitmapSource)AdjustedImages.Get(source, gray)!;
            var result = new byte[8];
            image.CopyPixels(result, 8, 0);
            var (r, g, b) = ColorMatrix.From(gray).Apply(1, 0, 0);
            result[2].Should().BeCloseTo((byte)Math.Round(r * 255), 1);
            result[1].Should().BeCloseTo((byte)Math.Round(g * 255), 1);
            result[0].Should().BeCloseTo((byte)Math.Round(b * 255), 1);
            result[3].Should().Be(255, "saydamlık korunur");

            AdjustedImages.Get(source, gray).Should().BeSameAs(image, "aynı ayar önbellekten gelir");
            AdjustedImages.Get(source, ImageAdjustments.None).Should().BeSameAs(source);
            AdjustedImages.Get(null, gray).Should().BeNull();
        });
    }
}

/// <summary>Metni sürüklerken akıllı hizalama.</summary>
public sealed partial class MainViewModelTests
{
    [Fact]
    public void DraggedTextSnapsToThePageCenterAndShowsGuides()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.Open(PagedProject(1));
        vm.SelectedPage = vm.Pages[0];
        vm.AddText(Core.Projects.TextPresets.Find("title"));
        var page = vm.SelectedPage!;
        var text = vm.SelectedText!;
        var (x, y) = (text.X, text.Y);
        var centerX = page.PageArea.X + (page.PageArea.Width / 2);
        var dx = centerX + 1.2 - (x + (text.Width / 2));

        vm.DragText(text, x, y, dx, 0, toleranceMm: 2);
        (text.X + (text.Width / 2)).Should().BeApproximately(centerX, 1e-9);
        page.SnapLines.Should().Contain(l => l.IsCenter && l.X1 == l.X2);

        vm.DragText(text, x, y, dx, 0, toleranceMm: 2, snap: false);
        (text.X + (text.Width / 2)).Should().BeApproximately(centerX + 1.2, 1e-9, "Alt ile serbest");
        page.SnapLines.Should().BeEmpty();

        vm.DragText(text, x, y, dx, 0, toleranceMm: 2);
        vm.CommitTextMove(text);
        vm.SelectedPage!.SnapLines.Should().BeEmpty("bırakınca çizgiler kalkar");
        var moved = vm.SelectedPage.Texts.Single();
        (moved.X + (moved.Width / 2)).Should().BeApproximately(centerX, 1e-6, "yapışmış konum kaydedilir");
    }
}
