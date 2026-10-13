using FluentAssertions;
using NexAlbum.Core.Projects;
using NexAlbum.UI.Services;
using NexAlbum.UI.ViewModels;
using Xunit;

namespace NexAlbum.UI.Tests;

/// <summary>Sayfa metinleri: ekle, düzenle, taşı, sil, geri al.</summary>
public sealed partial class MainViewModelTests
{
    [Fact]
    public void TextIsAddedEditedAndUndoable()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.History.MergeWindow = TimeSpan.Zero;
        vm.Open(PagedProject(2));
        vm.SelectedPage = vm.Pages[0];

        vm.HasSelectedText.Should().BeFalse();
        vm.AddTextCommand.Execute(TextPresets.Find("title"));

        vm.SelectedPage!.Texts.Should().ContainSingle();
        vm.HasSelectedText.Should().BeTrue("eklenen metin seçilir");
        vm.PanelTabIndex.Should().Be(MainViewModel.TextTabIndex);
        vm.TextFont!.Family.Should().Be("Great Vibes");
        vm.TextContent.Should().Be("Birlikte");
        vm.SelectedText!.IsSelected.Should().BeTrue();

        vm.TextContent = "Ayşe & Mert";
        vm.SelectedText!.Content.Should().Be("Ayşe & Mert", "düzenleme sonrası seçim korunur");
        vm.SelectedPage!.Texts.Should().ContainSingle();

        vm.TextSize = 500;
        vm.TextSize.Should().Be(144, "punto sınırlanır");
        vm.TextBold = true;
        vm.TextBold.Should().BeTrue();
        vm.IsTextLeft = true;
        vm.IsTextLeft.Should().BeTrue();
        vm.IsTextCenter.Should().BeFalse();
        vm.ApplyTextColorCommand.Execute(vm.TextColors.First(c => c.Name == "Bordo"));
        vm.TextColorHex.Should().Be("#6B2433");
        vm.TextColorHex = "renk değil";
        vm.TextColorHex.Should().Be("#6B2433", "geçersiz renk yok sayılır");
        vm.TextFont = vm.FontOptions.First(f => f.Family == "Playfair Display");
        vm.SelectedText!.Text.Font.Family.Should().Be("Playfair Display");

        vm.UndoCommand.Execute(null);
        vm.SelectedText!.Text.Font.Family.Should().Be("Great Vibes", "geri al seçimi bırakmaz");
        vm.UndoCommand.Execute(null);
        vm.UndoCommand.Execute(null);
        vm.UndoCommand.Execute(null);
        vm.UndoCommand.Execute(null);
        vm.UndoCommand.Execute(null);
        vm.TextContent.Should().Be("Birlikte");
        vm.UndoCommand.Execute(null);
        vm.SelectedPage!.Texts.Should().BeEmpty();
        vm.HasSelectedText.Should().BeFalse();
    }

    [Fact]
    public void TextHeightFollowsLinesAndBoxWidth()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.Open(PagedProject(1));
        vm.SelectedPage = vm.Pages[0];
        vm.AddText(TextPresets.Find("names"));
        var oneLine = vm.SelectedText!.Height;

        vm.TextContent = "Elif\nCan";
        vm.SelectedText!.Height.Should().BeApproximately(oneLine * 2, 1e-6, "her satır sabit yükseklikte");

        vm.TextLineHeight = 2;
        vm.TextLineHeight.Should().Be(2);
        vm.SelectedText!.LineHeight.Should().BeApproximately(vm.SelectedText.FontSize * 2, 1e-9);

        var center = vm.SelectedText.X + (vm.SelectedText.Width / 2);
        vm.TextWidthPercent = 30;
        vm.TextWidthPercent.Should().Be(30);
        (vm.SelectedText!.X + (vm.SelectedText.Width / 2)).Should().BeApproximately(center, 1e-6, "kutu ortadan daralır");
    }

    [Fact]
    public void TextIsMovedSelectedAndDeleted()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.History.MergeWindow = TimeSpan.Zero;
        vm.Open(PagedProject(2));
        vm.SelectedPage = vm.Pages[0];
        vm.AddText(TextPresets.Find("quote"));
        var text = vm.SelectedText!;
        var before = text.Text.Bounds;

        MainViewModel.PreviewMoveText(text, text.X, text.Y, 12, -4);
        vm.CommitTextMove(text);
        var after = vm.SelectedText!.Text.Bounds;
        after.X.Should().BeApproximately(before.X + 12, 1e-6);
        after.Y.Should().BeApproximately(before.Y - 4, 1e-6);
        vm.Status.Should().Contain("taşındı");

        vm.SelectText(null);
        vm.HasSelectedText.Should().BeFalse();
        vm.DeleteTextCommand.CanExecute(null).Should().BeFalse();
        vm.SelectText(vm.SelectedPage!.Texts[0]);
        vm.DeleteTextCommand.CanExecute(null).Should().BeTrue();

        vm.SelectedPage = vm.Pages[1];
        vm.HasSelectedText.Should().BeFalse("başka sayfaya geçince seçim kalkar");
        vm.SelectedPage = vm.Pages[0];
        vm.SelectText(vm.SelectedPage!.Texts[0]);

        vm.DeleteTextCommand.Execute(null);
        vm.SelectedPage!.Texts.Should().BeEmpty();
        vm.HasSelectedText.Should().BeFalse();
        vm.UndoCommand.Execute(null);
        vm.SelectedPage!.Texts.Should().ContainSingle();
    }

    [Fact]
    public void DatePresetUsesTodayInTurkishCapitals()
    {
        using var vm = new MainViewModel(new FakeDialogs(), new FixedClock(new DateTimeOffset(2026, 8, 29, 12, 0, 0, TimeSpan.Zero)), new MemoryAppSettingsStore());
        vm.Open(PagedProject(1));
        vm.SelectedPage = vm.Pages[0];
        vm.AddText(TextPresets.Find("date"));
        vm.TextContent.Should().Be("29 AĞUSTOS 2026");
    }

    [Fact]
    public void LockedPagesRejectNewText()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.Open(PagedProject(1));
        vm.SelectedPage = vm.Pages[0];
        vm.ToggleLock();
        vm.AddTextCommand.CanExecute(MainViewModel.DefaultTextPreset).Should().BeFalse();
    }

    [Fact]
    public void AlbumFontsResolveEmbeddedFaces()
    {
        AlbumFonts.All.Should().Contain(f => f.Family == "Great Vibes");
        AlbumFonts.IsAvailable("Cormorant Garamond").Should().BeTrue();
        AlbumFonts.Resolve("Cormorant Garamond", bold: false, italic: true)!.Value.Face.Should().EndWith("CormorantGaramond_400Regular_Italic.ttf");
        var script = AlbumFonts.Resolve("Great Vibes", bold: true, italic: false)!.Value;
        script.SimulateBold.Should().BeTrue("kalın dosyası olmayan yazı tipinde kalınlık taklit edilir");
        AlbumFonts.Resolve("Yok Böyle Bir Yazı", false, false).Should().BeNull();
        AlbumFonts.Load(script.Face).Should().NotBeNullOrEmpty("dosya uygulamaya gömülüdür");
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
