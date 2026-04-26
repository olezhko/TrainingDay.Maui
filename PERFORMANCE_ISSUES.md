# Performance Issues — TrainingDay MAUI

## Summary
31 issues found across 5 categories.

## HIGH SEVERITY

### VM-01 — StatisticsViewModel.cs:23
`LoadData()` runs 3 synchronous DB reads on UI thread.
**Fix:** Use `async Task` + `Task.Run` for all DB calls.

### VM-02 — WeightViewAndSetPageViewModel.cs:142
Sync DB + chart rebuild on UI thread in `PrepareBodyControlItems`.
**Fix:** Offload to `Task.Run`.

### VM-03 — TrainingItemsBasePageViewModel.cs:49
`LoadItems()` — 3 sync DB reads on UI thread.
**Fix:** Convert to `async Task`, wrap DB calls with `Task.Run`.

### VM-04 — HistoryTrainingPageViewModel.cs:54
Sync DB + full ObservableCollection rebuild on UI thread.
**Fix:** Offload DB read to `Task.Run`, batch collection updates.

### VM-05 — HistoryTrainingPageViewModel.cs:93
Full table scan + in-memory filter on every history item tap.
**Fix:** Add filtered query `GetLastTrainingExercisesByTrainingId(id)` in repository.

### VM-06 — PreparedTrainingsPageViewModel.cs:28
DB read (`GetExerciseItems`) inside ViewModel constructor.
**Fix:** Move to `InitializeAsync` or `OnAppearing`.

### DATA-01 — Repository.cs:247
`GetTrainingExerciseItemByTrainingId` loads ALL rows then fires N individual `GetExerciseItem` queries (N+1).
**Fix:** Use `WHERE TrainingId = ?` in SQLite + single `GetExerciseItems` batch load.

### DATA-02 — TrainingExercisesPageViewModel.cs:80
`PrepareTrainingViewModel` full table scan in 2 ViewModels, filter in RAM.
**Fix:** Use corrected `GetTrainingExerciseItemByTrainingId`.

### DATA-03 — Repository.cs:265
`DeleteTrainingExerciseItemByTrainingId` — 1 SELECT + N individual DELETEs.
**Fix:** `database.Execute("DELETE FROM TrainingExerciseComm WHERE TrainingId = ?", id)`

### DATA-04 — Repository.cs:335
`DeleteSuperSetsByTrainingId` — **BUG: deletes from wrong table** + loop without transaction.
**Fix:** `database.Execute("DELETE FROM SuperSets WHERE TrainingId = ?", id)`

### DATA-05 — Controls/ImageCache.cs:34
Sync SQLite read per cell on every scroll — causes visible jank.
**Fix:** Add static in-memory `Dictionary<string, byte[]>` cache, wrap DB call in `Task.Run`.

### DATA-06 — App.xaml.cs:95
142 sequential HTTP downloads at startup, `HttpClient` not reused, no `CancellationToken`.
**Fix:** Use `static readonly HttpClient`, parallel `Task.WhenAll`, add `CancellationToken`.

### SVC-03 — Controls/StepProgressBar.cs:311
Static event holds instance references — unbounded memory leak.
**Fix:** Use instance-level event subscription with proper unsubscribe on detach.

### DATA-08 — Repository.cs:38
`async void InitBasic` — startup race condition, exceptions silently swallowed.
**Fix:** Use factory `static async Task<Repository> CreateAsync(...)`.

---

## MEDIUM SEVERITY

### VM-07 — TrainingExercisesPageViewModel.cs:32
`new Command(...)` allocated on every property access (expression-bodied getters).
**Fix:** Back each command with a lazy field: `_cmd ??= new Command(...)`.

### VM-08 — ExerciseListPageViewModel.cs:106
Entire `ObservableCollection` replaced on every search keystroke.
**Fix:** Clear + re-add in place, or use RangeObservableCollection.

### VM-09 — BlogsPageViewModel.cs:21
`async void LoadItems()` — exceptions silently swallowed.
**Fix:** Change to `async Task LoadItemsAsync()`.

### VM-10 — ExerciseListPageViewModel.cs:109
`BaseItems` rebuilt without staleness check.
**Fix:** Track last-loaded state and skip rebuild if unchanged.

### VM-13 — TrainingViewModel.cs:40
`ExercisesBySuperSet` property rebuilds `ObservableCollection` on every binding access.
**Fix:** Cache result, invalidate on `Exercises.CollectionChanged`.

### XAML-01 — BlogsPage.xaml:16
`ItemSizingStrategy="MeasureAllItems"` disables CollectionView virtualization.
**Fix:** Remove attribute (defaults to `MeasureFirstItem`).

### XAML-02 — ExerciseListPage.xaml:68
7-level nested layout per cell (same pattern in TrainingExercisesPage.xaml, ExerciseItemPage.xaml).
**Fix:** Flatten hierarchy, remove unnecessary wrapper Grid and AbsoluteLayout.

### XAML-03 — HistoryTrainingExercisesPage.xaml:56
Two FlexLayouts always created per row, only IsVisible toggled.
**Fix:** Use `DataTemplateSelector` to create only the needed template.

### DATA-09 — Services/IWorkoutService.cs:26
N individual UPDATE calls in `UpdateExerciseNameAndDescription` without transaction.
**Fix:** Use `database.UpdateAll(toUpdate)`.

### DATA-10 — DataManageViewModel.cs:52
Import runs 2000+ individual INSERTs without transaction.
**Fix:** Wrap all inserts in `database.RunInTransaction(() => { ... })`.

### DATA-11 — Entity models
No `[Indexed]` on `TrainingId`, `ExerciseId`, `LastTrainingId` foreign key columns.
**Fix:** Add `[Indexed]` attribute to those properties.

### CS-01 — Repository.cs:103
`JsonConvert.SerializeObject` called for all ~150 exercises on every app start with no change detection.
**Fix:** Add version/hash check before serializing unchanged exercises.

### CS-02 — DataManageViewModel.cs:41
`File.ReadAllText` + JSON deserialization + DB writes all blocking UI thread.
**Fix:** Use `File.ReadAllTextAsync` + `Task.Run` for parse and DB writes.

### SVC-01 — Services/DataService.cs:49
HTTP calls have no `CancellationToken` — requests continue after navigation.
**Fix:** Add `CancellationToken ct` parameter and pass to `ExecuteAsync`.

### CS-04 — TrainingExercisesPageViewModel.cs:543
N individual UPDATEs on every drag-drop reorder without transaction.
**Fix:** Use `database.RunInTransaction` + `UpdateAll`.

---

## LOW SEVERITY

### VM-11 — ExerciseListPageViewModel.cs:204
`List<int>.Contains` O(n) in `FillSelectedIndexes`.
**Fix:** Use `HashSet<int>` for `selectedIndexes`.

### VM-12 — ExerciseListPageViewModel.cs:71
O(n²) selection lookup — `FirstOrDefault` inside loop over IDs.
**Fix:** Build `Dictionary<int, ExerciseListItemViewModel>` from `BaseItems` once.

### VM-14 — TrainingViewModel.cs:137
Repeated LINQ chain in `GetSuperSetNum` called per `AddExercise`.
**Fix:** Compute distinct superset IDs once before the loop.

### VM-15 — HistoryTrainingPageViewModel.cs:17
Static list appended on every constructor call — grows unboundedly.
**Fix:** Use `static readonly` with static initializer.

### XAML-04 — StatisticsPage.xaml:20
8 hand-written StackLayout blocks should use BindableLayout.

### XAML-05 — TrainingExercisesPage.xaml:48
Unused `SelectionMode="Single"` overhead — tap handled by gesture recognizer.
**Fix:** Set `SelectionMode="None"`.

### XAML-06 — ExerciseItemPage.xaml:123
Duplicate Editor+Label per description field, both always created.
**Fix:** Use single control with `IsReadOnly` toggle or DataTemplateSelector.

### XAML-07 — TrainingImplementPage.xaml:121
Lottie `SKLottieView` always instantiated even when `IsVisible=False`.
**Fix:** Lazy-load only when confetti is triggered.

### SVC-02 — Services/Settings.cs:100
`new CultureInfo(CultureName)` allocated on every `GetLanguage()` call.
**Fix:** Cache with invalidation on `CultureName` setter.

### CS-03 — WorkoutQuestinariumPageViewModel.cs:100
String interpolation `$"..."` inside `StringBuilder.Append` — defeats StringBuilder purpose.
**Fix:** Use `sb.Append(value).Append(' ')` directly.

### CS-05 — Controls/StepProgressBar.cs:231
LINQ scan of all children in `RecalculateSeparatorLineWidth` on every add.
**Fix:** Maintain a separate `List<BoxView> _separatorLines` field.

---

## Top 5 Highest-Impact Fixes

1. **ImageCache.cs** — sync DB hit per scroll cell → add in-memory dictionary cache
2. **Repository.cs:247** — N+1 queries → push `WHERE` into SQLite
3. **StepProgressBar.cs:311** — static event leak → instance-level subscription
4. **All LoadData/LoadItems ViewModels** — sync DB on UI thread → `Task.Run`
5. **Missing transactions** (delete, import, drag-reorder) → `RunInTransaction`
