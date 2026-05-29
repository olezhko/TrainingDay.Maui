# App Size Reduction

## Immediate — bugs/mistakes

### 1. `confetti.json` included twice (csproj line 133)
```xml
<MauiImage Include="Resources\Raw\confetti.json" />  <!-- REMOVE THIS -->
```
Already picked up by `<MauiAsset Include="Resources\Raw\**" .../>`. Having it also as `<MauiImage>` makes the build try to process a JSON file as an image — wasted work and potentially doubled inclusion.

---

## High impact, low risk

### 2. `UseInterpreter=True` on iOS Release (csproj line 57)
Embeds the entire Mono interpreter into the iOS binary. Unless a specific AOT crash forced this, remove it and test.

### 3. `Microsoft.Maui.Controls.Compatibility` (csproj line 142)
Zero usages of Compatibility APIs found in the codebase. This package exists purely for Xamarin.Forms migration and adds significant native code on both platforms. Try removing it.

### 4. Enable IL linking for Android Release
No `<AndroidLinkMode>` is set anywhere. Add to the Android Release config:
```xml
<PropertyGroup Condition="'$(Configuration)|$(TargetFramework)|$(Platform)'=='Release|net10.0-android|AnyCPU'">
    <AndroidLinkMode>Full</AndroidLinkMode>
</PropertyGroup>
```
Removes all unreachable managed code from the APK.

---

## Medium impact

### 5. Font subsetting — 612 KB for two Inter weights
Most characters in the TTF are unused. Options:
- Switch to the system font (drop both files, saves 612 KB)
- Keep only Inter-Regular, saves ~309 KB

### 6. Prepared training images — ~1.4 MB total, embedded raw in DLL
Declared as `EmbeddedResource`, bypassing MAUI's image optimisation pipeline entirely.
- Run through a PNG compressor (`pngquant` / `oxipng`) for an easy 40–60% reduction
- Or host server-side and download on first use

| File | Size |
|---|---|
| morning.png | 162 KB |
| shoulders.png | 139 KB |
| fitness.png | 132 KB |
| home.png | 130 KB |
| chest.png | 129 KB |
| press.png | 127 KB |
| up.png | 110 KB |
| arms.png | 104 KB |
| legs_and_glutes.png | 97 KB |
| flat_stomach.png | 90 KB |
| back.png | 74 KB |
| biceps_triceps.png | 73 KB |
| begin.png | 72 KB |
| cardio.png | 56 KB |
| wide_shoulders.png | ~50 KB |

### 7. `no_exercises.png` — 182 KB placeholder image
Largest regular image in the app. Replace with a lightweight SVG or vector drawable.

---

## Investigate

### 8. `CommunityToolkit.Maui.MediaElement` (~3–4 MB on Android)
Used in `SuperSetControl.xaml` for exercise video demos. If videos are a rarely-used feature, initialise MediaElement lazily or move it behind a feature flag to avoid the baseline cost.

### 9. `SkiaSharp.Views` + `SkiaSharp.Views.Maui.Controls` — possibly redundant
`SkiaSharp.Views.Maui.Controls` usually includes everything in `SkiaSharp.Views`. Try removing `SkiaSharp.Views` alone and verify the build still succeeds.

### 10. `Newtonsoft.Json` (transitive dependency)
Used directly in `TrainingImplementPage.cs` via `JsonConvert.SerializeObject`. Replacing with `System.Text.Json` removes the dependency entirely — `System.Text.Json` ships with .NET and adds zero size.

---

## Summary

| Item | Estimated saving |
|---|---|
| IL linker — Android | 1–3 MB |
| `UseInterpreter` — iOS | 2–5 MB |
| Compatibility package | 0.5–1 MB |
| Font subsetting | 300–600 KB |
| Prepared image compression | 500–800 KB |
| `confetti.json` double-include | 170 KB |
| Newtonsoft.Json removal | ~500 KB |
