using FluentAssertions;
using NexAlbum.Core.Common;
using NexAlbum.Core.Projects;
using NexAlbum.UI.Controls;
using NexAlbum.UI.Services;
using NexAlbum.UI.ViewModels;
using Xunit;

namespace NexAlbum.UI.Tests;

/// <summary>Metin etkileri, harf aralığı ve yuvadaki fotoğrafı konumlandırma.</summary>
public sealed partial class MainViewModelTests
{
    [Fact]
    public void TextEffectsAreChosenTunedAndUndoable()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.History.MergeWindow = TimeSpan.Zero;
        vm.Open(PagedProject(1));
        vm.SelectedPage = vm.Pages[0];
        vm.AddText(TextPresets.Find("title"));

        vm.SelectedTextEffect!.Kind.Should().Be(TextEffectKind.None);
        vm.HasTextEffect.Should().BeFalse();
        vm.TextEffectOptions.Should().HaveCount(6);
        vm.TextEffectOptions.Single(o => o.Kind == TextEffectKind.Glow).Preview.Font.Family.Should().Be("Great Vibes", "önizleme seçili metnin yazı tipiyle");

        vm.SelectedTextEffect = vm.TextEffectOptions.Single(o => o.Kind == TextEffectKind.Glow);
        vm.HasTextEffect.Should().BeTrue();
        vm.SelectedText!.Text.Effect.Color.Should().Be(RgbaColor.White, "koyu yazının halesi açık renklidir");
        vm.TextEffectStrength = 90;
        vm.SelectedText!.Text.Effect.Strength.Should().BeApproximately(0.9, 1e-9);
        vm.ApplyTextEffectColorCommand.Execute(vm.TextEffectColors.Single(c => c.Name == "Krem"));
        vm.TextEffectColorHex.Should().Be("#F3EAD8");

        vm.SelectedTextEffect = vm.TextEffectOptions.Single(o => o.Kind == TextEffectKind.Band);
        vm.SelectedText!.Text.Effect.Strength.Should().BeApproximately(0.9, 1e-9, "tür değişince güç korunur");

        vm.TextColorHex = "#FFFFFF";
        vm.MakeTextReadable();
        vm.SelectedText!.Text.Effect.Kind.Should().Be(TextEffectKind.Shadow, "açık yazıya koyu gölge");

        var height = vm.SelectedText.Height;
        vm.TextLetterSpacing = 30;
        vm.SelectedText!.Text.LetterSpacing.Should().BeApproximately(0.3, 1e-9);
        vm.SelectedText.Height.Should().BeGreaterThanOrEqualTo(height, "genişleyen yazı gerekirse alt satıra geçer");

        vm.UndoCommand.Execute(null);
        vm.UndoCommand.Execute(null);
        vm.SelectedText!.Text.Effect.Kind.Should().Be(TextEffectKind.Band);

        vm.SelectedTextEffect = vm.TextEffectOptions[0];
        vm.SelectedText!.Text.Effect.IsNone.Should().BeTrue();
        vm.HasTextEffect.Should().BeFalse();
    }

    [Fact]
    public void EveryEffectRendersOnScreen()
    {
        foreach (var kind in Enum.GetValues<TextEffectKind>())
        {
            var text = TextPresets.Find("names")!.Create("t", "Ayşe & Mert", new Core.Geometry.RectMm(0, 0, 300, 300), 1000) with
            {
                Effect = new TextEffect { Kind = kind, Color = RgbaColor.White, Strength = 0.8 },
                LetterSpacing = kind == TextEffectKind.Outline ? 0.2 : 0,
            };
            var drawing = AlbumTextView.Render(text);
            drawing.Bounds.IsEmpty.Should().BeFalse(kind.ToString());
            drawing.Bounds.Width.Should().BeGreaterThan(10, kind.ToString());
        }

        AlbumTextView.Measurer(TextPresets.Find("title")!.Create("t", null, new Core.Geometry.RectMm(0, 0, 300, 300), 1000))("Birlikte")
            .Should().BeGreaterThan(5, "gömülü yazı tipi ölçülür");
    }

    [Fact]
    public void PhotosArePositionedWithWheelKeysAndTheAnchorGrid()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.History.MergeWindow = TimeSpan.Zero;
        vm.Open(PagedProject(2));
        vm.SelectedPage = vm.Pages[0];
        var slot = vm.SelectedPage!.Slots[0];
        vm.SelectSlot(slot);

        // Yuvanın sağ alt çeyreğine doğru yakınlaş: fotoğraf o yöne kayar (merkez sola-yukarı gider).
        vm.ZoomSlotAt(vm.SelectedSlot!, 2, 80, 80);
        var zoomed = vm.SelectedSlot!.Placement!;
        zoomed.Scale.Should().BeApproximately(2, 1e-9);
        zoomed.OffsetX.Should().BeLessThan(0);
        zoomed.OffsetY.Should().BeLessThan(0);

        vm.Nudge(1, 0, large: true).Should().BeTrue();
        vm.SelectedSlot!.Placement!.OffsetX.Should().BeApproximately(zoomed.OffsetX + 0.1, 1e-9, "Shift + sağ: yuvanın %10'u kadar sağa");

        vm.AlignPhotoCommand.Execute("-1 -1");
        var left = vm.SelectedSlot!.Placement!;
        left.OffsetX.Should().BeGreaterThan(0, "sol üst bölge görünür: fotoğraf sağa-aşağı kayar");
        left.Scale.Should().BeApproximately(2, 1e-9);
        vm.AlignPhotoCommand.Execute("0 0");
        vm.SelectedSlot!.Placement!.OffsetX.Should().Be(0);

        vm.ZoomSelected(1.25).Should().BeTrue();
        vm.SlotZoomPercent.Should().BeApproximately(250, 1e-6);

        vm.UndoCommand.Execute(null);
        vm.SlotZoomPercent.Should().BeApproximately(200, 1e-6);
        vm.SelectedSlot.Should().NotBeNull("geri alma yuva seçimini korur");
    }

    [Fact]
    public void PositioningModeSurvivesEditsAndEndsOnAnotherSelection()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.Open(PagedProject(2));
        vm.SelectedPage = vm.Pages[0];
        vm.IsPositioning = true;
        vm.IsPositioning.Should().BeFalse("seçili dolu yuva yokken kip açılmaz");

        vm.SelectSlot(vm.SelectedPage!.Slots[0]);
        vm.IsPositioning = true;
        vm.IsPositioning.Should().BeTrue();
        vm.SelectedSlot!.IsPositioning.Should().BeTrue();
        vm.PositioningButtonText.Should().Be("Konumlandırmayı bitir");

        var before = vm.SelectedSlot.Placement!.OffsetX;
        vm.Nudge(1, 0, large: false);
        vm.SelectedSlot!.Placement!.OffsetX.Should().BeGreaterThan(before);
        vm.IsPositioning.Should().BeTrue("düzenleme sonrası sayfa yeniden kurulsa da kip sürer");
        vm.SelectedSlot!.IsPositioning.Should().BeTrue();

        vm.SelectSlot(null);
        vm.IsPositioning.Should().BeFalse();

        vm.SelectSlot(vm.SelectedPage!.Slots[0]);
        vm.IsPositioning = true;
        vm.SelectedPage = vm.Pages[1];
        vm.IsPositioning.Should().BeFalse("başka sayfaya geçince kip kapanır");
    }

    [Fact]
    public void ArrowKeysMoveTheSelectedText()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.Open(PagedProject(1));
        vm.SelectedPage = vm.Pages[0];
        vm.AddText(TextPresets.Find("date"));
        var before = vm.SelectedText!.Text.Bounds;

        vm.Nudge(-1, 0, large: false).Should().BeTrue();
        vm.Nudge(0, 1, large: true).Should().BeTrue();

        var after = vm.SelectedText!.Text.Bounds;
        after.X.Should().BeApproximately(before.X - MainViewModel.TextNudgeMm, 1e-9);
        after.Y.Should().BeApproximately(before.Y + (MainViewModel.TextNudgeMm * 10), 1e-9);
        AlbumFonts.Find("Montserrat").Should().NotBeNull();
    }
}
