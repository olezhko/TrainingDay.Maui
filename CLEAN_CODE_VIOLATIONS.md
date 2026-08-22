# Clean Code Violations — TrainingDay MAUI

Audit of `TrainingDay.Maui/` (Services, Extensions, Models, ViewModels, Views, Controls, Converters, Platforms) against standard clean-code principles: naming, method/class size & cohesion, DRY, error handling, encapsulation, and MVVM separation. Complements `PERFORMANCE_ISSUES.md` — items that are purely performance-related (sync DB on UI thread, N+1 queries, missing transactions) are only cross-referenced here, not re-explained, unless they also represent a distinct clean-code smell (e.g. a god class, a bug hidden by duplicated code).

## Summary
~130 violations found across 3 layers (Services/Extensions/Models, ViewModels, Views/Controls/Converters/Platforms) and 12 categories. The most structurally significant:

- **`Repository.cs`** — unabstracted god object for 10+ entity types, ~9x copy-pasted save boilerplate, a real bug (`DeleteSuperSetsByTrainingId` deletes from the wrong table — also `PERFORMANCE_ISSUES.md` DATA-04).
- **`TrainingImplementPage.xaml.cs`** and **`FilterPage.xaml.cs`** — 625 and 813-line code-behind god classes with no/unused ViewModel, mixing UI, direct SQLite writes, timers, and business rules.
- **`Controls/StepProgressBar.cs:325`** — `static` `CollectionChanged` event subscribed to by every instance, never unsubscribed: confirmed memory leak *and* cross-instance event cross-talk (also `PERFORMANCE_ISSUES.md` SVC-03).
- **Inconsistent error handling** everywhere: `LoggingService.TrackError`, bare `Console.WriteLine`/`Debug.WriteLine`, and fully silent `catch { }` are used interchangeably for the same kind of failure across all three layers.
- **Duplicated request/save/delete boilerplate** across `Services/*.cs` and `Repository.cs`, and duplicated business logic across `ViewModels/Pages/*.cs`.
- **`Data/` folder is dead code** — `SqliteRepository.cs` references an undefined field and would not compile; excluded from the build entirely.

---

## 1. Long Methods / Multi-Responsibility Methods

### LM-01 — Services/Repository.cs:38
`async void InitBasic()` mixes table creation, resource loading, exercise upsert, and blog cleanup in one method. (Also `PERFORMANCE_ISSUES.md` DATA-08 for the `async void` race.)

### LM-02 — Services/IWorkoutService.cs:73
`CreateWeightAndReps` builds a throwaway `TrainingExerciseViewModel` just to serialize it; mixes tag branching, regex parsing, and JSON conversion.

### LM-03 — ViewModels/Pages/PreparedTrainingsPageViewModel.cs:91-302
`FillTrainings()` — ~210 lines hardcoding 12 near-identical training templates via copy-pasted lambdas.

### LM-04 — ViewModels/Pages/WeightViewAndSetPageViewModel.cs:185-265
`PrepareChart()` — ~80 lines mixing chart-entry building, theme-color selection, and `LineChart` config with dozens of magic numbers.

### LM-05 — ViewModels/DataManageViewModel.cs:53-146
`SetRepositoryData()` — ~90 lines sequentially importing 7 entity types with ad hoc id-remapping dictionaries, no transaction.

### LM-06 — ViewModels/Pages/ExerciseListPageViewModel.cs:119-184
`LoadItems()` — ~65 lines combining name/tag/difficulty/muscle filtering via accumulated `match &= ...` boolean flags.

### LM-07 — ViewModels/Pages/TrainingItemsBasePageViewModel.cs:333-389
`CreateNewGroup()` — ~55 lines mixing prompt display, DB lookup/creation, UI collection updates, and error handling.

### LM-08 — ViewModels/Pages/StatisticsViewModel.cs:24-79
`LoadData()` computes six unrelated statistics inline in ~55 lines with direct synchronous DB access (also `PERFORMANCE_ISSUES.md` VM-01).

### LM-09 — Views/TrainingImplementPage.xaml.cs:303-368
`FinishButtonClicked`→`FinishAndSave` chain: timer teardown, DB writes, social-feed sharing, confetti animation, popup, navigation — all in one flow with no ViewModel to delegate to.

### LM-10 — Views/TrainingItemsBasePage.xaml.cs:38-91
`IsStartNotFinishedTraining` mixes file I/O, JSON deserialization, manual entity→ViewModel mapping (14+ fields), confirmation dialog, and navigation.

### LM-11 — Views/FilterPage.xaml.cs:189-253
`DrawMuscles` is an 18-case switch dispatching to 18 near-identical `DrawXxx` methods spanning ~500 lines of hardcoded path-coordinate data — should be a data table, not code.

---

## 2. God Classes / Low Cohesion

### GOD-01 — Services/Repository.cs:10-455
Owns CRUD for 10+ unrelated entities (exercises, trainings, weight notes, last-trainings, super-sets, images, blogs, social-workout cache, training groups) with zero shared abstraction; every method is fully synchronous.

### GOD-02 — Services/Repository.cs:262-278
`GetTrainingExerciseItemByTrainingId` — N+1 query, loads full table then queries per-row. (`PERFORMANCE_ISSUES.md` DATA-01.)

### GOD-03 — Services/Repository.cs:350-360 — **BUG**
`DeleteSuperSetsByTrainingId` loops `SuperSetEntity` items but calls `DeleteTrainingExerciseItem(item.Id)` instead of `DeleteSuperSetItem(item.Id)` — deletes rows from the wrong table. Root cause: copy-pasted "load all, filter, delete" boilerplate with no reuse. (`PERFORMANCE_ISSUES.md` DATA-04.)

### GOD-04 — ViewModels/Pages/TrainingExercisesPageViewModel.cs (631 lines)
~14 commands mixing training loading, exercise CRUD, superset creation, move/copy workflow, drag-and-drop reordering, and file sharing.

### GOD-05 — ViewModels/Pages/TrainingItemsBasePageViewModel.cs (546 lines)
Mixes training list loading/grouping, Firebase push-token registration, group CRUD, duplicate/delete workflows, and action-sheet routing.

### GOD-06 — ViewModels/Pages/PreparedTrainingsPageViewModel.cs (431 lines)
Combines 200+ lines of static training-template data with DB import/save logic in the same class.

### GOD-07 — Views/TrainingImplementPage.xaml.cs (625 lines, no ViewModel)
Owns timer/stopwatch logic, push-notification scheduling, direct SQLite writes, file-based crash-recovery serialization, social-feed sharing, confetti UI, and step navigation. CLAUDE.md itself flags this as "code-behind heavy."

### GOD-08 — Views/FilterPage.xaml.cs (813 lines)
Sets `BindingContext = this` and owns all filter state directly instead of using the existing (unused) `ViewModels/FilterViewModel.cs`; also embeds ~500 lines of SkiaSharp muscle-map drawing and popup-onboarding logic.

---

## 3. Poor / Unclear Naming

### NAME-01 — Services/Repository.cs:118
`AddorUpdateExercise` — inconsistent casing (should be `AddOrUpdateExercise`); mixed tabs/spaces indentation inside the method (lines 120-141).

### NAME-02 — Services/IWorkoutService.cs:102-137
`TryGetReps(string input, out int number1)` — parameter named `number1`, a `number2` local computed but never meaningfully used, a captured regex group discarded.

### NAME-03 — Extensions/ResizeImageExtension.cs:5
File is `ResizeImageExtension.cs` but the class is `ResizeImageService` — not an extension class at all (no `this` parameter), inconsistent with the file name.

### NAME-04 — ViewModels/Pages/PreparedTrainingsPageViewModel.cs:23
`NavigateToQuestionsCommnd` — misspelled ("Commnd").

### NAME-05 — ViewModels/Pages/HistoryTrainingPageViewModel.cs:18
`DaysAndTextLimits` uses `Tuple<string,int>` instead of a named type — `.Item1`/`.Item2` usages are opaque at call sites.

### NAME-06 — ViewModels/Pages/TrainingExercisesPageViewModel.cs:24
`ExerciseActionString` actually holds a UI action-mode display label ("Move"/"Copy"/"Training"), not a generic string.

### NAME-07 — ViewModels/FilterViewModel.cs:1-6
File lives in `ViewModels/` but declares `namespace TrainingDay.Maui.Models;` and class `FilterModel` (not `...ViewModel`) — namespace/folder/class name all mismatch.

### NAME-08 — ViewModels layer, cross-cutting
Inconsistent private-field convention: underscore-prefixed (`_dataService`) vs. non-prefixed (`dataService`) across different ViewModels.

### NAME-09 — Views/TrainingImplementPage.xaml.cs:33
Field `notificator` — non-standard term for "notifier".

### NAME-10 — Controls/StepProgressBar.cs:302,305
Parameter/variable named `co` — cryptic single-letter abbreviation.

### NAME-11 — Converters/IsNoItemsConverter.cs:8-22
Class is named `IsNoItemsConverter` but `Convert` returns `true` when `count > 0` (items exist) — the name is the exact opposite of the behavior.

### NAME-12 — Converters/HalfValueConverter.cs:7-17
Name implies halving a value; logic actually divides by an arbitrary `offset` and subtracts a magic `4` — name doesn't describe the transform.

### NAME-13 — Controls/ToolTipControl.xaml.cs:134
Local delegate `properyChanged` — misspelled ("property").

---

## 4. Magic Numbers / Magic Strings

### MAGIC-01 — Services/DataService.cs:52
`cultureId = ... ? 1 : 2` — hardcoded culture→ID mapping, no enum/constant.

### MAGIC-02 — Services/DataService.cs:30 vs AuthService.cs:38 vs UserSettingsService.cs:27 vs SocialWorkoutsService.cs:31
Inconsistent hardcoded HTTP timeouts (2s vs 10s) with no shared constant.

### MAGIC-03 — Extensions/PushMessagesExtensions.cs:41-51
Notification IDs `100`–`105` as separate static properties instead of a named enum.

### MAGIC-04 — Services/Repository.cs:448
`items.Take(10)` — undocumented cache-size limit as a bare literal.

### MAGIC-05 — Extensions/ConstantKeys.cs:10-11
`Version => "1.3.3"` and `ExerciseMaxCodeNum = 144` will silently drift out of sync with the actual csproj version / exercise resource data.

### MAGIC-06 — ViewModels/Pages/PreparedTrainingsPageViewModel.cs (10 call sites)
Exercise `CodeNum` values hardcoded as bare int literals across ~12 templates.

### MAGIC-07 — ViewModels/BodyControlItem.cs:71,85
`value > 100 ? 3 : 4` — unexplained field-length thresholds.

### MAGIC-08 — ViewModels/Pages/WeightViewAndSetPageViewModel.cs:220-241
Chart config literals (`LabelTextSize = 42`, `LineSize = 5`, `PointSize = 20`, `Margin = 30`) unexplained.

### MAGIC-09 — ViewModels/Pages/ExerciseListPageViewModel.cs:200 & WorkoutQuestinariumPageViewModel.cs:158
Route `"//workouts"` hardcoded independently in two files.

### MAGIC-10 — Multiple ViewModels
Shell query-param dictionary keys (`"ExistedExercises"`, `"Item"`, `"Context"`, `"TrainingItem"`, `"Filter"`) hardcoded and repeated across `TrainingExercisesPageViewModel.cs`, `ExerciseListPageViewModel.cs`, `BlogsPageViewModel.cs`, `SocialWorkoutsPageViewModel.cs` — no shared constants.

### MAGIC-11 — ViewModels/Pages/TrainingExercisesPageViewModel.cs:543-558 & TrainingItemsBasePageViewModel.cs:501-530
Branches compare localized `DisplayActionSheetAsync` result strings against `AppResources.*` directly instead of stable action codes — fragile, breaks if wording changes.

### MAGIC-12 — ViewModels/Pages/StatisticsViewModel.cs:120-123
`ShareResults()` uses hardcoded non-localized `"Share"`, `"ShareResults"`, `"OK"` instead of `AppResources.*`.

### MAGIC-13 — Controls/StepProgressBar.cs:118,119,202,231
Button size `45`, corner radius `20`, separator-width formula `(45 + 2)`, threshold `15` — unexplained repeated pixel values.

### MAGIC-14 — Views/TrainingImplementPage.xaml.cs:186,385,582
`TimeSpan.FromSeconds(10)` warning threshold, `Task.Delay(2700)` confetti duration, `restSeconds = 120` default — unexplained literals.

### MAGIC-15 — Converters/HalfValueConverter.cs:11,16
`0.00001` epsilon and `- 4` offset have no named constant or comment.

### MAGIC-16 — Converters/IsEmptyStringConverter.cs:11
Parameter compared against literal `"inverse"` string instead of a typed/bool parameter.

### MAGIC-17 — Controls/ExerciseView.xaml.cs:33 / SuperSetControl.xaml.cs:122 / RepsAndWeightControl.xaml.cs:13
Default `WeightAndRepsViewModel` values inconsistently hardcoded — `(0, 15)` in two files, `(5, 15)` in a third.

### MAGIC-18 — Views/ExerciseItemPage.xaml.cs:218,229,241
`CompressionQuality = 85` (duplicated) and `ResizeImage(imageData, 600, 600, false)` unexplained.

### MAGIC-19 — Platforms/Android/MyFirebaseMessagingService.cs:115 & PushNotificationService.cs:28
`"pushData"` key literal duplicated across two files.

### MAGIC-20 — Views/FilterPage.xaml.cs:159-160,44,105-106,139
`imageSourceSizeWidth/Height` (496/514), popup image size `100x100`, `CornerRadius(20,20,20,20)` unexplained.

---

## 5. Duplicated Code (DRY Violations)

### DUP-01 — Services/DataService.cs:39-48 & AuthService.cs:50-59
Near-identical private `CreateRequest(resource, method, body)` duplicated verbatim.

### DUP-02 — Services/UserSettingsService.cs:86-91 & SocialWorkoutsService.cs:92-97
Identical private `CreateAuthorizedRequest(resource, method)` duplicated — 4 near-identical request builders exist across Services with no shared base client.

### DUP-03 — Services/Repository.cs (9 methods)
"If Id != 0 Update else Insert" save pattern copy-pasted ~9× (`SaveWeightNotesItem`, `SaveLastTrainingItem`, `SaveLastTrainingExerciseItem`, `SaveExerciseItem`, `SaveTrainingExerciseItem`, `SaveTrainingItem`, `SaveSuperSetItem`, `SaveImage`, `SaveTrainingGroup`) — a generic `SaveItem<T>` already exists at line 408 but isn't reused.

### DUP-04 — Services/Repository.cs:280-290,350-360,420-430
"Load full table, filter by FK, delete" duplicated 3× instead of a parameterized helper or SQL `WHERE` — root cause of the GOD-03 bug.

### DUP-05 — Models/Database/*.cs (8 files)
Every DB entity re-declares `public new int Id { get; set; }` to shadow the base class's `Id`, identical across 8 files instead of a shared base/interface.

### DUP-06 — ViewModels/Pages/TrainingExercisesPageViewModel.cs:95-131 & HistoryTrainingPageViewModel.cs:177-214
`PrepareTrainingViewModel()` copy-pasted verbatim (including the N+1 pattern) in both files. (`PERFORMANCE_ISSUES.md` DATA-02.)

### DUP-07 — ViewModels/Pages/TrainingExercisesPageViewModel.cs:439-473 & 475-513
`AcceptTrainingForMoveOrCopy()` and `CreateTrainingFromSelectedExercises()` share near-identical while-loops differing only in destination training id.

### DUP-08 — ViewModels/Pages/PreparedTrainingsPageViewModel.cs:330-360 & 362-382
`GetExerciseByMuscles()` and `GetExercisesByCodeNum()` share near-identical try/catch/iterate/add structure.

### DUP-09 — ViewModels/Pages/TrainingItemsBasePageViewModel.cs:129-137,144-151,156-158
`if (lastTraining != null) item.LastImplementedDateTime = ...` repeated 3× inside `FillGroupedTraining()`.

### DUP-10 — ViewModels/BodyControlItem.cs:15-24,29-38 & WeightAndRepsViewModel.cs:57-63
`double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, ...)` pattern copy-pasted 3×.

### DUP-11 — Controls/ExerciseView.xaml.cs:28-40 & SuperSetControl.xaml.cs:117-129
`AddWeightAndRepsItem_Clicked`/`AddRequestWeightAndReps` and paired delete handlers near-identical across two controls.

### DUP-12 — Views/ExerciseItemPage.xaml.cs:180-193 & Views/FilterPage.xaml.cs:772-785
`GetDifficultyLevelLabel` duplicated line-for-line (identical switch expression) in two files.

### DUP-13 — Platforms/Android/MyFirebaseMessagingService.cs:108-148 & PushNotificationService.cs:21-61
Nearly identical Intent/PendingIntent/`NotificationCompat.Builder` construction duplicated across two classes, plus a duplicated static field `webContentList` declared separately in both — with inconsistent icon resources (`Resource.Drawable.main` vs `icon`/`small_icon`), suggesting the copies have already drifted.

### DUP-14 — Controls/ToolTipControl.xaml.cs:93-98,119-121
`ToolTipPosition.Bottom`, `.Right`, `.Start` cases in `CalculatePosition` have identical bodies.

### DUP-15 — Controls/StepProgressBar.cs:110-137
`unselectedCircleStyle` and `selectedCircleStyle` share nearly all setters — candidate for a shared base style.

### DUP-16 — Views/ExerciseItemPage.xaml.cs:195-247
Two near-duplicate permission/media-picker branches inside `Button_Clicked`.

---

## 6. Deep Nesting / High Complexity

### NEST-01 — Services/Repository.cs:85-116
`DeleteUnused` nests `GroupBy` → `foreach` → `for` → `foreach` four levels deep to dedupe exercises and re-point references — untested, migration-like logic running on every app start.

### NEST-02 — Services/AuthService.cs:103-157
`EnsureValidTokenAsync` — 4 sequential early-return guards plus a lock; the "is session usable" check is itself duplicated at two line ranges within the method.

### NEST-03 — ViewModels/Pages/TrainingExercisesPageViewModel.cs:446-467
Nested `while`+`if`+`if` inside `AcceptTrainingForMoveOrCopy` mutating state across three levels.

### NEST-04 — Views/FilterPage.xaml.cs:189-252
18-branch switch dispatching to per-muscle draw methods — better modeled as a data lookup table.

### NEST-05 — Views/ExerciseItemPage.xaml.cs:195-247
`Button_Clicked` mixes action-sheet selection, two near-duplicate permission/picker branches, and image resizing in one tangled linear method.

---

## 7. Large Parameter Lists / Boolean Flag Parameters

### PARAM-01 — Extensions/ResizeImageExtension.cs:7
`ResizeImage(byte[] imageData, float width, float height, bool rotate)` — `rotate` bool controls orientation logic; reads clearer as an enum.

### PARAM-02 — Models/Notifications/PushMessage.cs:13-17
`IsUpdateCurrent`, `IsSilent`, `IsDisableSwipe` — three independent booleans on one DTO (low severity, plain data object).

### PARAM-03 — ViewModels/Pages/TrainingItemsBasePageViewModel.cs:113-117
`FillGroupedTraining(TrainingEntity, IEnumerable<TrainingUnionEntity>, IEnumerable<LastTrainingEntity>, List<Grouping<...>>)` — 4 params including a list mutated in place.

### PARAM-04 — ViewModels/Pages/PreparedTrainingsPageViewModel.cs:304-309
`CreatePreparedTraining` — 5 params including a `Func<>` builder delegate and an optional list.

### PARAM-05 — ViewModels/Pages/TrainingItemsBasePageViewModel.cs:48
`LoadItems(bool isOverride = false)` — unclear flag controlling reload short-circuit.

### PARAM-06 — ViewModels/Pages/TrainingExercisesPageViewModel.cs:370
`StopAction(bool result = false)` — generic bool name hides "action succeeded, show toast" semantics.

---

## 8. Dead Code / Commented-Out Code / Redundant Logic

### DEAD-01 — Data/SqliteRepository.cs:11 — **does not compile**
`_database = database;` references an undefined identifier (`database` doesn't exist; the ctor param is `filename`, itself unused). The entire `Data/` folder (`IEntity.cs`, `IRepository.cs`, `SqliteRepository.cs`) is excluded from the build via `<Compile Remove="Data\**" />` and is unreferenced anywhere else — dead scaffolding.

### DEAD-02 — Extensions/ResizeImageExtension.cs:35-36
Stale commented-out lines (`//var newHeight = height;`, `//create a 24bit RGB image`).

### DEAD-03 — Extensions/TrainingExerciseViewModelExtensions.cs:30
`value.Length < 0` can never be true — dead branch left over from a refactor.

### DEAD-04 — Extensions/StringExtensions.cs:5-13
Custom `Contains(this string, string, StringComparison)` reimplements the BCL overload (available since .NET Core 2.0) with different validation behavior — redundant and a hidden behavioral trap.

### DEAD-05 — Extensions/StringExtensions.cs:20-23
`IsNullOrEmptyOrWhiteSpace` ORs `IsNullOrEmpty` with `IsNullOrWhiteSpace` — the first check is fully subsumed by the second; dead/no-op logic.

### DEAD-06 — ViewModels/Pages/TrainingExercisesPageViewModel.cs:240
`selectedExercise.IsSelected = selectedExercise.IsSelected;` — self-assignment no-op, likely meant to toggle.

### DEAD-07 — ViewModels/Pages/StatisticsViewModel.cs:120-123,171
`ShareResultsCommand` only shows a placeholder alert — sharing was never implemented.

### DEAD-08 — Controls/StepProgressBar.cs:322
`//if (args.OldItems != null) RemoveItems(args.OldItems);` — commented-out call to a method that doesn't exist; old items are never removed, so stale buttons/separators accumulate on collection removals.

### DEAD-09 — Controls/LongPressedEffect.cs:93,101
Leftover `Console.WriteLine("Invoking click command")` / `"Invoking long click command"` debug statements in production code.

### DEAD-10 — Controls/LongPressedEffect.cs:145-151
`OnDetached()` sets `_attached = false;` twice — once inside `#if __IOS__`, once unconditionally after — redundant/confused cleanup.

---

## 9. Empty or Swallowed Catch Blocks

### CATCH-01 — Services/Repository.cs:29-33
Constructor catches all exceptions from DB-path setup, logs via `Console.WriteLine`, leaves `database` null — object is broken with no clear signal why.

### CATCH-02 — Services/Repository.cs:301-308
`GetTrainingItem` catches and silently returns `new TrainingEntity()` on *any* failure, masking real errors as an empty training.

### CATCH-03 — Services/IWorkoutService.cs:38-41
Per-exercise loop in `UpdateExerciseNameAndDescription` only `Console.WriteLine`s, silently skipping failed exercises.

### CATCH-04 — Services/IWorkoutService.cs:94-97
`catch (Exception) { }` — fully swallows parsing errors, no logging.

### CATCH-05 — Extensions/MuscleViewModelExtensions.cs:42-45 & 70-73
`Console.WriteLine(e)` in one method; fully silent `catch` returning `string.Empty` in another.

### CATCH-06 — Extensions/TrainingExerciseViewModelExtensions.cs:47-50
`Console.WriteLine(e)` instead of `LoggingService`.

### CATCH-07 — ViewModels/Pages/TrainingItemsBasePageViewModel.cs:43-45
`catch { }` in `UpdateFirebaseToken()` — silently swallows all exceptions.

### CATCH-08 — ViewModels/DataManageViewModel.cs:123-126
`catch { }` inside the `TrainingExercises` import loop — silently drops import failures.

### CATCH-09 — ViewModels/Pages/WorkoutQuestinariumPageViewModel.cs:117-120
`catch (Exception) { }` in `PrepareRequest()` — silently swallows AI-request build errors.

### CATCH-10 — Views/TrainingImplementPage.xaml.cs:146-149
`catch { }` in `LoadVideoItemsAsync` — discards all video-fetch failures.

### CATCH-11 — Controls/ImageCache.cs:52-54
`catch { }` in `LoadImage()` — discards DB/stream failures.

### CATCH-12 — Controls/ToolTipControl.xaml.cs:68-70
`catch (Exception) { }` in `ToolTipControl_PropertyChanged` — discards position-recalculation errors.

### CATCH-13 — Views/WeightViewAndSetPage.xaml.cs:60-63
`Console.WriteLine(exception)` only — no `LoggingService`, no user feedback.

### CATCH-14 — Platforms/Android/LoadActivity.cs:35-38
`Console.WriteLine(ex)` for file-content load failures — not surfaced or logged via `LoggingService`.

### CATCH-15 — Platforms/Android/MyFirebaseMessagingService.cs:55-58
Inner `catch` uses `Console.WriteLine(e)`, inconsistent with the outer catch in the *same method* which correctly uses `LoggingService.TrackError(e)` (line 104).

---

## 10. Inconsistent Error Handling (Cross-Cutting)

### ERR-01
Across all three layers, error handling is split inconsistently between `LoggingService.TrackError(ex)`, bare `Console.WriteLine(e)`, `System.Diagnostics.Debug.WriteLine(...)`, and fully silent `catch { }` — with no documented policy for which to use where. See CATCH-01…15 and ERR entries under each layer's agent findings for the full list of sites. **Fix:** standardize on `LoggingService.TrackError` for all non-trivial catches; reserve silent catches only for genuinely expected, documented conditions.

---

## 11. Improper Async Patterns

### ASYNC-01 — Services/Repository.cs:38
`async void InitBasic()` — exceptions during startup are unobservable. (`PERFORMANCE_ISSUES.md` DATA-08.)

### ASYNC-02 — ViewModels/Pages/TrainingItemsBasePageViewModel.cs:196
`async void DeleteSelectedTraining(Border viewCell)`.

### ASYNC-03 — ViewModels/Pages/TrainingExercisesPageViewModel.cs:515
`async void ShareTraining()`, invoked fire-and-forget from `ShowTrainingSettingsPage`.

### ASYNC-04 — ViewModels/Pages/BlogsPageViewModel.cs:21
Public `async void LoadItems()` — exceptions will crash the app instead of being catchable. (`PERFORMANCE_ISSUES.md` VM-09.)

### ASYNC-05 — ViewModels/Pages/SocialWorkoutsPageViewModel.cs:48
`async void OnAppearing()`.

### ASYNC-06 — ViewModels/Pages/ExerciseListPageViewModel.cs:102-104
Public sync `UpdateItems()` wraps `async void LoadItemsAsync()` — awaitability is lost entirely at the call boundary.

---

## 12. Static / Global Mutable State & Event Leaks

### STATIC-01 — Controls/StepProgressBar.cs:69,325 — **confirmed memory leak + correctness bug**
`CollectionChanged` is declared `static`, but every `StepProgressBar` instance subscribes an instance handler in its constructor and never unsubscribes. Every instance ever constructed is kept alive for the app's lifetime. Worse: because the event is static, a collection change on *any* instance's `ItemsSource` fires the handler on *every* `StepProgressBar` ever created, including ones on pages already navigated away from. (`PERFORMANCE_ISSUES.md` SVC-03.)

### STATIC-02 — Views/TrainingImplementPage.xaml.cs:48,158
`StepProgressBarControl.CurrentItemChanged +=` subscribed in the constructor but never unsubscribed (only `PropertyChanged` is unsubscribed in `OnDisappearing`).

### STATIC-03 — Controls/ToolTipControl.xaml.cs:149
`view.PropertyChanged += properyChanged;` in `LinkToLinksPreviewControl` has no matching unsubscribe.

### STATIC-04 — Controls/MaterialLabelAttached.cs:40,100-111,126-139
`Loaded`, `Focused`/`Unfocused`/`TextChanged`/`SelectedIndexChanged` all subscribed via anonymous lambdas with no unsubscription — handlers accumulate if the attached property toggles repeatedly.

### STATIC-05 — Controls/ImageCache.cs:10
`Loaded += ImageCache_Loaded;` never unsubscribed (lower severity — `Loaded` typically fires once).

### STATIC-06 — Extensions/PushMessagesExtensions.cs:41-63
A dozen `public static { get; set; }` fields (notification IDs, state flags, localized strings resolved once at class load) — `NewWorkoutMessageTitle` etc. won't update on later language switches (stale-localization risk).

### STATIC-07 — Extensions/ToolTipManager.cs:89-103
11 near-identical property wrappers pack/unpack tooltip flags through a `BitArray` over a single int instead of per-key `Preferences` or a dictionary.

### STATIC-08 — Services/Settings.cs:1-173
Entire app config/auth-session state (including `AuthToken`/`RefreshToken`) exposed as a static class over `Preferences` — every consumer has an implicit, untestable dependency on process-wide static state instead of an injected settings interface. (Acceptable as a documented pattern per CLAUDE.md, but worth flagging.)

### STATIC-09 — Controls/StepProgressBar.cs:325
The `static event` itself (see STATIC-01) — used for what should be instance-scoped collection-change notification.

### STATIC-10 — Platforms/Android/MyFirebaseMessagingService.cs:21 & PushNotificationService.cs:14
`public static string webContentList = "";` declared independently in two classes, both mutated as global state with no single owner.

---

## 13. MVVM Violations — Business Logic / Direct DB Access in Code-Behind

### MVVM-01 — Views/FilterPage.xaml.cs:18-813
Page sets `BindingContext = this` and manages all filter state itself, even though `ViewModels/FilterViewModel.cs` exists and is unused (see NAME-07, GOD-08).

### MVVM-02 — Views/TrainingImplementPage.xaml.cs (whole file)
No ViewModel at all — rest-timer countdown, workout-completion logic, and social-sharing payload construction (`ShareToSocialFeedIfEnabled`, lines 445-500) live entirely in code-behind.

### MVVM-03 — Views/ExerciseItemPage.xaml.cs:87-118
`Remove_clicked` builds a confirmation message by directly querying the DB for trainings referencing the exercise — this cross-entity rule belongs in `ExerciseViewModel`.

### MVVM-04 — Views/TrainingItemsBasePage.xaml.cs:38-91
Crash-recovery deserialization and manual entity reconstruction done in the page instead of a ViewModel/service.

### MVVM-05 — Views/TrainingImplementPage.xaml.cs:392,409,433,541
`App.Database.SaveItem`, `SaveLastTrainingExerciseItem`, `SaveTrainingExerciseItem` called directly from the page.

### MVVM-06 — Views/ExerciseItemPage.xaml.cs:63,68,92,97,109,110
`App.Database.SaveExerciseItem`, `SaveImage`, `GetTrainingExerciseItems`, `GetTrainingItems`, `DeleteExerciseItem`, `DeleteTrainingExerciseItemByExerciseId` all called directly from code-behind.

### MVVM-07 — Views/TrainingExerciseItemPage.xaml.cs:49
`App.Database.SaveTrainingExerciseItem(...)` called directly in `Save_clicked`.

### MVVM-08 — Views/TrainingExercisesPage.xaml.cs:29
`App.Database.SaveTrainingItem(...)` called directly in `OnDisappearing`, even though an injected `TrainingExercisesPageViewModel` is already available on the page.

### MVVM-09 — Controls/ImageCache.cs:44
`App.Database.GetImage(key)` called directly from a reusable `Image`-derived control.

### MVVM-10 — Controls/PopupBuilders.cs:108
`App.Database.GetTrainingsGroups()` called directly from a static UI-builder helper class.

---

## 14. Overly Long Files

### FILE-01 — Services/Repository.cs — 455 lines (see GOD-01)
### FILE-02 — ViewModels/Pages/TrainingExercisesPageViewModel.cs — 631 lines (see GOD-04)
### FILE-03 — ViewModels/Pages/TrainingItemsBasePageViewModel.cs — 546 lines (see GOD-05)
### FILE-04 — Views/FilterPage.xaml.cs — 813 lines (see GOD-08)
### FILE-05 — Views/TrainingImplementPage.xaml.cs — 625 lines (see GOD-07)

---

## 15. Bugs Found In Passing (not strictly "clean code," but surfaced by the audit)

### BUG-01 — Views/WeightViewAndSetPage.xaml.cs:45
`vm.BodyControlItems.Any(item => item.Type == WeightType.Waist) && vm.BodyControlItems.Any(item => item.Type == WeightType.Waist)` checks `Waist` twice — the second check should be `WeightType.Hip` (matching `.First(item => item.Type == WeightType.Hip)` two lines below). Risks `InvalidOperationException` if no Hip entry exists.

### BUG-02 — Views/TrainingExerciseItemPage.xaml.cs:32-40
`OnDisappearing` skips calling `base.OnDisappearing()` when `isSaved` is true — inverted logic plus a vague comment makes intent unclear; worth a correctness review.

### BUG-03 — Views/ExerciseListPage.xaml.cs:63-76
Uses `.First()` + catch-`Exception` as control flow to detect "item not in list" instead of `FirstOrDefault`/`TryGetValue`; the delete/update branch always runs even after the `Added` case is already handled above.

---

*Generated by an automated multi-agent code audit. Line numbers reflect the state of the repository at the time of this scan — re-verify before acting on any single item, as the codebase moves quickly.*
