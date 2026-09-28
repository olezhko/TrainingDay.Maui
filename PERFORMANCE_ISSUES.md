# Performance Issues — TrainingDay MAUI

## Summary

_Last rescan: 2026-09-28._

| | Count |
|---|---|
| Original issues | 38 |
| ✅ Fixed | 10 (VM-06, VM-07, VM-10, VM-15, XAML-01, DATA-01, DATA-03, DATA-04, DATA-05, DATA-12) |
| 🟡 Partially addressed (still open) | 2 (DATA-02, VM-08) |
| ❌ Still open | 29 |
| 🆕 New issues found in rescan | 21 (1 fixed) |
| **Total open** | **49** |

Line numbers were refreshed for all open issues. Status markers: ✅ fixed · 🟡 partially fixed · ❌ open · 🆕 new.

---

## HIGH SEVERITY

### ❌ VM-01 — StatisticsViewModel.cs:24
`LoadData()` runs 3 synchronous DB reads on UI thread.
**Fix:** Use `async Task` + `Task.Run` for all DB calls.

### ❌ VM-02 — WeightViewAndSetPageViewModel.cs:143
Sync DB + chart rebuild on UI thread in `PrepareBodyControlItems`.
**Fix:** Offload to `Task.Run`.

### ❌ VM-03 — TrainingItemsBasePageViewModel.cs:48
`LoadItems()` — 3 sync DB reads on UI thread.
**Fix:** Convert to `async Task`, wrap DB calls with `Task.Run`.

### ❌ VM-04 — HistoryTrainingPageViewModel.cs:56
Sync DB + full ObservableCollection rebuild on UI thread.
**Fix:** Offload DB read to `Task.Run`, batch collection updates.

### ❌ VM-05 — HistoryTrainingPageViewModel.cs:98
Full table scan + in-memory filter on every history item tap.
**Fix:** Add filtered query `GetLastTrainingExercisesByTrainingId(id)` in repository.

### ✅ VM-06 — PreparedTrainingsPageViewModel.cs — FIXED
DB read was moved out of the constructor into `FillTrainings()`, which is called from `PreparedTrainingsPage.OnAppearing`.
_Follow-up:_ it now runs on **every** appearance — see VM-21.

### ✅ DATA-01 — Repository.cs — FIXED
`GetTrainingExercisesByTrainingId` now runs 2 queries regardless of size:
- `select * from TrainingExerciseComm where TrainingId = ? order by OrderNumber`
- `select * from Exercises where _id in (...)`

It returns `(TrainingExerciseEntity, ExerciseEntity)` pairs, and the caller (`TrainingItemsBasePageViewModel.DuplicateSelectedTraining`) builds the `TrainingExerciseViewModel`s, which removes the Repository → ViewModels dependency. Rows pointing to a deleted exercise are skipped (previously `Get<ExerciseEntity>` threw and aborted the duplicate). SQL translation was verified against sqlite-net-e 1.11.

### 🟡 DATA-02 — TrainingExercisesPageViewModel.cs:95, HistoryTrainingPageViewModel.cs:177
`PrepareTrainingViewModel` is duplicated in 2 ViewModels. Each copy does a full table scan, filters in RAM, and calls `exerciseItems.First(...)` per row (O(n·m)).
**Progress:** `TrainingExercisesPageViewModel` now runs it inside `Task.Run` (off the UI thread). The `HistoryTrainingPageViewModel` copy is still synchronous, and both still do full scans.
**Fix:** Use corrected `GetTrainingExerciseItemByTrainingId` + `Dictionary<int, ExerciseEntity>`; extract the shared method.

### ✅ DATA-03 — Repository.cs — FIXED
`DeleteTrainingExerciseItemByTrainingId` and `DeleteTrainingExerciseItemByExerciseId` now issue a single `DELETE ... WHERE` via `database.Table<TrainingExerciseEntity>().Delete(item => ...)` instead of SELECT + N DELETEs.

### ✅ DATA-04 — Repository.cs — FIXED
`DeleteSuperSetsByTrainingId` now deletes from the correct table in one statement: `database.Table<SuperSetEntity>().Delete(item => item.TrainingId == trainingId)`. The typed query is used because the real table name is `SuperSetEntity` (no `[Table]` attribute), not `SuperSets`.
_Note:_ existing user databases may already have lost `TrainingExerciseComm` rows to the old bug (rows whose `_id` matched a deleted superset id); that data cannot be recovered.

### ✅ DATA-05 — Controls/ImageCache.cs, Services/Repository.cs — FIXED
`Repository` now keeps image bytes in a `ConcurrentDictionary<string, byte[]?>` (misses are cached too). It exposes `TryGetCachedImageData` / `GetImageData` and invalidates an entry in `SaveImage`. `ImageCache` shows cache hits immediately; on a miss it loads via `Task.Run` and drops the result if the cell was recycled meanwhile. The data layer has no dependency on UI controls.
_Remaining:_ index on `Url`, duplicate loads per bind, and full-resolution decode are tracked in DATA-13.

### ❌ DATA-06 — App.xaml.cs:95
142 sequential HTTP downloads at startup, `HttpClient` not reused, no `CancellationToken`.
**Rescan note:** every launch re-downloads **all** images in full (no ETag / `If-Modified-Since`, change detection only compares byte length). The loop runs from `Dispatcher.Dispatch`, so the 142 `GetImage` / `SaveImage` calls run synchronously on the UI thread.
**Fix:** Use `static readonly HttpClient`, parallel `Task.WhenAll` (bounded), add `CancellationToken`, use conditional requests or a server-side version manifest, and do the DB work on a background thread.

### ❌ SVC-03 — Controls/StepProgressBar.cs:325
A static event holds instance references, causing an unbounded memory leak. Instances subscribe at line 69 and never unsubscribe; the static relay is at lines 298–331.
**Fix:** Use instance-level event subscription with proper unsubscribe on detach.

### ❌ DATA-08 — Repository.cs:38
`async void InitBasic` — startup race condition, exceptions silently swallowed.
**Fix:** Use factory `static async Task<Repository> CreateAsync(...)`.

### 🆕 IMPL-01 — Views/TrainingImplementPage.xaml.cs:178-259
The 1-second workout timer tick does all of this on the UI thread:
- rebuilds a `TrainingSerialize` of the whole workout
- `JsonSerializer.Serialize`s every exercise description
- writes the file with `File.WriteAllText`
- writes `Settings.IsTrainingNotFinishedTime` to Preferences
- reposts the ongoing notification

For a 10-exercise workout that is roughly 3,600 full serializations and disk writes per hour, which drains battery and causes frame drops while the user types reps and weight.
**Fix:** Persist only on state change (finish, skip, value edit) or throttle to every 15–30 s. Always persist in `Window.Stopped` / `OnSleep`. Serialize and write off the UI thread.

### ✅ DATA-12 — Repository.cs — FIXED
- `GetTrainingExerciseItems()` now returns a materialized `List`, so enumerating it more than once no longer re-runs the SQL. This also covers the chained `Where`s in both `PrepareTrainingViewModel` copies (DATA-02).
- New targeted query `GetTrainingExerciseItemsByExerciseId(id)` (`WHERE ExerciseId = ?`).
- `ExerciseItemPage.Remove_clicked` uses it: one targeted query + a `HashSet` of training ids, materialized once. Previously it did one full-table query per training.
- `Repository.DeleteUnused` no longer loads the whole `TrainingExerciseComm` table on every app start. It queries only the rows of a duplicate exercise, which is normally none.

---

## MEDIUM SEVERITY

### ✅ VM-07 — TrainingExercisesPageViewModel.cs — FIXED
All commands are now backed by lazy fields (`_cmd ??= new ...`).
_Follow-up:_ the same anti-pattern still exists in other ViewModels — see VM-17.

### 🟡 VM-08 — ExerciseListPageViewModel.cs:108
The entire `ObservableCollection` is still replaced on every search keystroke.
**Progress:** the load now runs in `Task.Run`, so it no longer blocks the UI thread.
**Fix:** Clear + re-add in place, or use RangeObservableCollection. See VM-16 for the related debounce/race problem.

### ❌ VM-09 — BlogsPageViewModel.cs:21
`async void LoadItems()` — exceptions silently swallowed. The same pattern is in `ExerciseListPageViewModel.LoadItemsAsync` (:104) and `SocialWorkoutsPageViewModel.OnAppearing` (:48).
**Fix:** Change to `async Task LoadItemsAsync()`.

### ✅ VM-10 — ExerciseListPageViewModel.cs:110 — FIXED
`BaseItems` is now only built once (`if (!BaseItems.Any())`).

### ❌ VM-13 — TrainingViewModel.cs:40
The `ExercisesBySuperSet` property rebuilds an `ObservableCollection` on every binding access.
**Fix:** Cache result, invalidate on `Exercises.CollectionChanged`.

### ✅ XAML-01 — BlogsPage.xaml:16 — FIXED
Now `ItemSizingStrategy="MeasureFirstItem"`.

### ❌ XAML-02 — ExerciseListPage.xaml:68
7-level nested layout per cell. The same pattern is in TrainingExercisesPage.xaml and ExerciseItemPage.xaml, and each muscle tag is still wrapped in `AbsoluteLayout > Border > Label` (line 100).
**Fix:** Flatten hierarchy, remove unnecessary wrapper Grid and AbsoluteLayout.

### ❌ XAML-03 — HistoryTrainingExercisesPage.xaml:56
Two FlexLayouts are always created per row, and only `IsVisible` is toggled.
**Rescan note:** the same pattern exists in `SocialWorkoutDetailPage.xaml:72/87` and `Controls/ExerciseView.xaml:45/70`.
**Fix:** Use `DataTemplateSelector` to create only the needed template.

### ❌ DATA-09 — Services/IWorkoutService.cs:22
N individual UPDATE calls in `UpdateExerciseNameAndDescription` without a transaction, plus an O(n·m) `inits.FirstOrDefault` per exercise.
**Fix:** Build a `Dictionary<int, BaseExercise>` by CodeNum; `database.UpdateAll(toUpdate)` inside `RunInTransaction`.

### ❌ DATA-10 — DataManageViewModel.cs:85
Import (`SetRepositoryData`) runs 2000+ individual INSERTs without a transaction.
**Fix:** Wrap all inserts in `database.RunInTransaction(() => { ... })`.

### ❌ DATA-11 — Entity models
No `[Indexed]` on the `TrainingId`, `ExerciseId`, `LastTrainingId` foreign key columns, or on `ImageEntity.Url` (see DATA-13).
**Fix:** Add `[Indexed]` attribute to those properties.

### ❌ CS-01 — Repository.cs:118
Every app start re-serializes the description of all ~150 exercises (now `System.Text.Json`) and issues an UPDATE per exercise. There is no change detection and no transaction.
**Fix:** Store a resource version/hash in `Settings` and skip when unchanged; otherwise batch in `RunInTransaction`.

### ❌ CS-02 — DataManageViewModel.cs:54
`File.ReadAllText` + JSON deserialization + DB writes all block the UI thread. Export (`BuildExportJson` + `File.WriteAllText`, :203-205) does the same.
**Fix:** Use `File.ReadAllTextAsync` + `Task.Run` for parse and DB writes.

### ❌ SVC-01 — Services/DataService.cs:50
HTTP calls have no `CancellationToken`, so requests continue after navigation.
**Fix:** Add `CancellationToken ct` parameter and pass to `ExecuteAsync`.

### ❌ CS-04 — TrainingExercisesPageViewModel.cs:561
`SaveNewExerciseOrder` does N individual UPDATEs on every drag-drop reorder, without a transaction.
**Fix:** Use `database.RunInTransaction` + `UpdateAll`.

### 🆕 DATA-13 — Controls/ImageCache.cs:10-32, Models/Database/ImageData.cs:10
`LoadImage()` runs from `Loaded` **and** from each of the `CodeNum` / `ExerciseId` property-changed callbacks, so a recycled cell hits the DB 2–3× per bind. `GetImage` filters by `Url`, which is not indexed, so each hit is a full table scan. Images are decoded at full resolution into a 60×60 view.
**Fix:** Coalesce into one load after both properties are set, index `Url` (or key by CodeNum), cache decoded/downsampled bytes in memory.

### 🆕 DATA-14 — Views/ExerciseItemPage.xaml.cs:68
Saving a custom exercise with an image **always inserts a new `ImageEntity`** (Id = 0), even when one exists for `new_{id}`. The images table grows on every save, and `Find` returns the oldest row, so a stale image is shown. Deleting an exercise never deletes its image.
**Fix:** Look up the existing row and update it; delete the image in `Remove_clicked`.

### 🆕 NET-01 — TrainingItemsBasePageViewModel.cs:50-53
`LoadItems()` runs on every appearance of the main tab. Each call dispatches `UpdateFirebaseToken()`, which on iOS makes 4 network calls (`CheckIfValidAsync`, `GetTokenAsync`, `SendFirebaseTokenAsync`, `PostActionAsync(Enter)`). This duplicates the work `App.OnStart` already does and inflates the "Enter" analytics.
**Fix:** Run once per app session (or on token refresh), not per page appearance.

### 🆕 VM-16 — Views/ExerciseListPage.xaml.cs:90, ExerciseListPageViewModel.cs:102-127
Each search keystroke calls `UpdateItems()`, with no debounce. Every call:
- re-reads the whole `Exercises` table from SQLite
- re-creates every `ExerciseListItemViewModel` (muscle parsing + description JSON)
- runs as a fire-and-forget `async void` with no cancellation, so a slow earlier search can finish last and **overwrite newer results**

The list is also hidden while `IsBusy`, so it flickers on every keystroke.
**Fix:** Debounce (~300 ms) with a `CancellationTokenSource`; filter the already-loaded `BaseItems` in memory instead of hitting the DB; keep the list visible during filtering.

### 🆕 VM-18 — TrainingItemsBasePageViewModel.cs:117-127
`FillGroupedTraining` creates `new TrainingUnion(union)` (JSON-deserializing `TrainingIDsString`) for **every union for every training**, which is O(T·U) JSON parses. It also runs `lastTrainings.Where(...).OrderByDescending(...)` per training (O(T·L)) and calls `Settings.GetLanguage()` (new `CultureInfo`, see SVC-02) per item.
**Fix:** Precompute `Dictionary<int trainingId, TrainingUnion>` and `Dictionary<int trainingId, DateTime lastTime>` once per load.

### 🆕 VM-20 — BlogsPageViewModel.cs:35, :76
- The list loads full `BlogEntity` rows, including the HTML `Content` of every post, just to show titles.
- `OpenBlog` **always** re-downloads the post from the API and rewrites it to the DB, even when content is already cached.
- New blogs are inserted one by one without a transaction.

**Fix:** Project only list columns (or store content in a separate table), fetch content only when `Content` is empty, and batch inserts.

### 🆕 DATA-15 — Missing transactions (additional sites)
Multi-row writes are not wrapped in `RunInTransaction`, so each row is a separate fsync:
- `TrainingImplementPage.xaml.cs:390-444` — `SaveLastTrainingWithExercises` + `SaveChangedExercises` (also on the UI thread when a workout finishes)
- `App.xaml.cs:164` — `IncomingTraining`
- `PreparedTrainingsPageViewModel.cs:383` — `SaveNewTrainingViewModelToDatabase`
- `IWorkoutService.cs:45` — `CreateWorkoutAsync`
- `TrainingItemsBasePageViewModel.cs:245` — `DuplicateSelectedTraining`
- `TrainingExercisesPageViewModel.cs:309/439/475` — `CreateSuperSet`, `AcceptTrainingForMoveOrCopy`, `CreateTrainingFromSelectedExercises`
- `HistoryTrainingPageViewModel.cs:216` — `RemoveLastTraining` (N deletes; use `DELETE ... WHERE LastTrainingId = ?`)
- `Repository.cs:72/85` — `DeleteBrokenBlogs`, `DeleteUnused`

**Fix:** Wrap each in `RunInTransaction`; replace delete loops with single `DELETE ... WHERE`.

### 🆕 XAML-07 — Compiled bindings missing on most pages
Only 8 XAML files declare `x:DataType`. The busiest list pages use slower reflection-based bindings for every cell:
- TrainingItemsBasePage
- ExerciseListPage
- HistoryTrainingPage
- HistoryTrainingExercisesPage
- BlogsPage
- PreparedTrainingsPage
- StatisticsPage
- WeightViewAndSetPage
- SuperSetControl

**Fix:** Add `x:DataType` to pages and `DataTemplate`s (enable XC0022/XC0025 warnings).

---

## LOW SEVERITY

### ❌ VM-11 — ExerciseListPageViewModel.cs:207
`List<int>.Contains` O(n) in `FillSelectedIndexes`. It is also used per item at :161.
**Fix:** Use `HashSet<int>` for `selectedIndexes`.

### ❌ VM-12 — ExerciseListPageViewModel.cs:72
O(n²) selection lookup — `FirstOrDefault` inside a loop over IDs.
**Fix:** Build `Dictionary<int, ExerciseListItemViewModel>` from `BaseItems` once.

### ❌ VM-14 — TrainingViewModel.cs:137
The LINQ chain in `GetSuperSetNum` is repeated on every `AddExercise` call, which makes loading a training O(n²).
**Fix:** Compute distinct superset IDs once before the loop.

### ✅ VM-15 — HistoryTrainingPageViewModel.cs:18 — FIXED
The list is now `static readonly` and is `Clear()`ed before re-adding, so it no longer grows unboundedly.

### ❌ XAML-04 — StatisticsPage.xaml:20
8 hand-written StackLayout blocks should use BindableLayout.

### ❌ XAML-05 — TrainingExercisesPage.xaml:37
Unused `SelectionMode="Single"` overhead, because taps are handled by a gesture recognizer. The same applies to ExerciseListPage.xaml:46.
**Fix:** Set `SelectionMode="None"`.

### ❌ XAML-06 — ExerciseItemPage.xaml:136
Each description field has both an Editor and a Label, and both are always created.
**Fix:** Use single control with `IsReadOnly` toggle or DataTemplateSelector.

### ❌ SVC-02 — Services/Settings.cs:162
`new CultureInfo(CultureName)` is allocated on every `GetLanguage()` call. It is called in hot loops: per chart entry (WeightViewAndSetPageViewModel.cs:201), per training (VM-18), per exercise at startup.
**Fix:** Cache with invalidation on `CultureName` setter.

### ❌ CS-03 — WorkoutQuestinariumPageViewModel.cs:110
String interpolation `$"..."` inside `StringBuilder.Append` defeats the purpose of `StringBuilder`.
**Fix:** Use `sb.Append(value).Append(' ')` directly.

### 🆕 VM-17 — Commands allocated per property access (remaining sites)
Same anti-pattern as the fixed VM-07:
- WeightViewAndSetPageViewModel.cs:35
- TrainingItemsBasePageViewModel.cs:549-551
- HistoryTrainingPageViewModel.cs:42-46
- SocialWorkoutsPageViewModel.cs:40-46
- DataManageViewModel.cs:40-41
- Controls/SuperSetControl.xaml.cs:53-56
- Views/TrainingImplementPage.xaml.cs:606

**Fix:** `_cmd ??= new ...` or initialize in constructor.

### 🆕 VM-21 — PreparedTrainingsPageViewModel.cs:90
`FillTrainings()` now runs on every `OnAppearing`. Each run re-reads the full `Exercises` table and rebuilds 12 template VMs + 12 embedded `ImageSource`s. The collection is then assigned without raising `PropertyChanged`.
**Fix:** Build once (lazy) and reuse; raise `PropertyChanged` if replacing the collection.

### 🆕 VM-22 — SocialWorkoutsPageViewModel.cs:78/109/134, SocialWorkoutViewModel.cs:41
Each refresh or next page synchronously reads the whole `Exercises` table on the UI thread. `MergeExercises` then does `FirstOrDefault` per exercise, which is O(workouts × exercises × library).
**Fix:** Cache a `Dictionary<int CodeNum, ExerciseEntity>` once per session.

### 🆕 XAML-08 — Controls/SuperSetControl.xaml:64, :132
A `CollectionView` is nested inside a `ScrollView` (line 22). It cannot virtualize and measures all items.
**Fix:** Use `BindableLayout` on a `VerticalStackLayout` for the small set list.

### 🆕 UI-01 — Views/FilterPage.xaml.cs:737
`DrawStringImplement` re-parses coordinate strings (`Split` + `Convert.ToSingle`) and rebuilds every `SKPath` on each paint. `SetMuscleFilter` toggles each muscle item, and each toggle calls `InvalidateSurface` (~20×).
**Fix:** Pre-parse points once (static `SKPoint[]`), cache paths per scale, suspend invalidation while bulk-setting.

### 🆕 UI-02 — Views/TrainingImplementPage.xaml.cs:212
`PlaySound()` creates a new Android `Ringtone` on every tick during the last 10 s of rest, without stopping or releasing the previous one.
**Fix:** Keep one `Ringtone` / `SoundPool` instance; play once per threshold crossing.

### 🆕 SVC-04 — Services/DataService.cs:65, :30
- `GetBlogAsync` allocates a new `JsonSerializerOptions` on every call, which discards the serializer metadata cache.
- The shared `RestClient` has a **2 s** timeout for all endpoints, including `/exercises/query` (AI generation) and full blog content, so on slow networks requests may fail and be retried by the user.

**Fix:** `static readonly JsonSerializerOptions`; per-request timeouts for long endpoints.

### 🆕 CS-05 — DataManageViewModel.cs:77-81
On import failure, the **entire file content** (potentially megabytes) is attached to the telemetry event.
**Fix:** Log size/hash or a truncated prefix only.

### 🆕 MEM-01 — MauiProgram.cs:58-85
All pages and ViewModels are registered as singletons, so their visual trees and data stay in memory for the app's lifetime. This includes the per-workout detail page `TrainingExercisesPage` and Login/Register/ForgotPassword.
**Fix:** Register detail and auth pages (+ VMs) as `Transient`.

---

## Non-performance bugs noticed during rescan

- **DATA-14** (above) — duplicate image rows, so a stale image is shown.
- `StatisticsViewModel.cs:57` — "most often day" orders by `DayOfWeek` instead of by count, so it always returns the earliest weekday present.
- `TrainingExercisesPageViewModel.cs:312-315` — `CreateSuperSet` shows the "need ≥ 2 exercises" alert but has no `return`, so it still creates the superset.
- `ExerciseListPage.xaml.cs:23/48` — `SearchButtonPressed` is subscribed in the constructor but unsubscribed in `OnDisappearing`, so it stops working after the first navigation away. The same happens in `TrainingImplementPage.xaml.cs:47/158` for `StepProgressBarControl.PropertyChanged`.
- `SocialWorkoutsService.cs:52` — `GetFeedAsync` doesn't call `EnsureValidTokenAsync()`, unlike the other authorized calls.

---

## Top 5 Highest-Impact Fixes

1. **TrainingImplementPage timer (IMPL-01)** — full serialize + file write every second during workouts → persist on change / throttle
2. **ImageCache (DATA-05 + DATA-13)** — 2–3 unindexed sync DB hits per scroll cell → single load + index + in-memory cache
3. **Repository query shape (DATA-02, DATA-11)** — remaining full-table scans and missing FK indexes
4. **All LoadData/LoadItems ViewModels (VM-01…05)** — sync DB on UI thread → `Task.Run`
5. **Missing transactions (DATA-09/10, CS-04, DATA-15)** → `RunInTransaction` / single `DELETE ... WHERE`
