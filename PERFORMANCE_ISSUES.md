# Performance Issues — TrainingDay MAUI

## Summary

_Last rescan: 2026-10-31._

| | Count |
|---|---|
| Issues listed below | 57 |
| ✅ Fixed | 19 (VM-01, VM-03, VM-06, VM-07, VM-10, VM-11, VM-12, VM-15, VM-16, XAML-01, XAML-04, DATA-01, DATA-03, DATA-04, DATA-05, DATA-12, SVC-03, NET-01, CS-03) |
| 🟡 Partially addressed (still open) | 6 (DATA-02, IMPL-01, VM-04, VM-08, VM-18, XAML-05) |
| ❌ Still open | 18 |
| 🆕 Open issues from the previous rescan | 14 (VM-17, XAML-07, UI-01 partially) |
| **Total open** | **38** |

Fixed in the 2026-10-31 rescan: VM-03, VM-11, VM-12, VM-16, NET-01, CS-03. Line numbers were refreshed for the items that moved. Status markers: ✅ fixed · 🟡 partially fixed · ❌ open · 🆕 new.

---

## HIGH SEVERITY

### ✅ VM-01 — StatisticsViewModel.cs — FIXED
`LoadDataAsync()` builds all statistics inside `Task.Run` and applies the results on the UI thread.

### ❌ VM-02 — WeightViewAndSetPageViewModel.cs:204
Sync DB + chart rebuild on UI thread in `PrepareBodyControlItems`.
**Fix:** Offload to `Task.Run`.

### ✅ VM-03 — TrainingsPageViewModel.cs — FIXED
`LoadItemsAsync()` does all DB/file reads in `ReadPageData`, run through `Task.Run`, and applies the result on the UI thread.

### 🟡 VM-04 — HistoryTrainingPageViewModel.cs:66
Sync DB + full ObservableCollection rebuild on UI thread.
**Progress:** the list is no longer rebuilt on every `OnAppearing`; it reloads only on first show, after a delete or after "Start again". Paging appends to the existing groups instead of creating new ones. The reload itself still reads the DB synchronously.
**Fix:** Offload DB read to `Task.Run`, batch collection updates.

### ❌ VM-05 — HistoryTrainingPageViewModel.cs:124
Full table scan + in-memory filter on every history item tap.
**Fix:** Add filtered query `GetLastTrainingExercisesByTrainingId(id)` in repository.

### ✅ VM-06 — PreparedTrainingsPageViewModel.cs — FIXED
DB read was moved out of the constructor into `FillTrainings()`, which is called from `PreparedTrainingsPage.OnAppearing`.
_Follow-up:_ it now runs on **every** appearance — see VM-21.

### ✅ DATA-01 — Repository.cs — FIXED
`GetTrainingExercisesByTrainingId` now runs 2 queries regardless of size:
- `select * from TrainingExerciseComm where TrainingId = ? order by OrderNumber`
- `select * from Exercises where _id in (...)`

It returns `(TrainingExerciseEntity, ExerciseEntity)` pairs, and the caller (`TrainingsPageViewModel.DuplicateSelectedTraining`) builds the `TrainingExerciseViewModel`s, which removes the Repository → ViewModels dependency. Rows pointing to a deleted exercise are skipped (previously `Get<ExerciseEntity>` threw and aborted the duplicate). SQL translation was verified against sqlite-net-e 1.11.

### 🟡 DATA-02 — TrainingViewModel.cs:76
Loading a training with its exercises does a full table scan of `TrainingExerciseComm` and `Exercises`, filters in RAM, and calls `exerciseItems.First(...)` per row (O(n·m)).
**Progress:** the two `PrepareTrainingViewModel` copies were merged into one shared `TrainingViewModel.LoadWithExercises(id)`. `TrainingExercisesPageViewModel` (:89) and `TrainingsPageViewModel` (:311) call it inside `Task.Run`; `HistoryTrainingPageViewModel` (:163) still calls it synchronously. It still does the full scans.
**Fix:** Use `GetTrainingExercisesByTrainingId` (DATA-01) or a `WHERE TrainingId = ?` query + `Dictionary<int, ExerciseEntity>`; run the History call off the UI thread.

### ✅ DATA-03 — Repository.cs — FIXED
`DeleteTrainingExerciseItemByTrainingId` and `DeleteTrainingExerciseItemByExerciseId` now issue a single `DELETE ... WHERE` via `database.Table<TrainingExerciseEntity>().Delete(item => ...)` instead of SELECT + N DELETEs.

### ✅ DATA-04 — Repository.cs — FIXED
`DeleteSuperSetsByTrainingId` now deletes from the correct table in one statement: `database.Table<SuperSetEntity>().Delete(item => item.TrainingId == trainingId)`. The typed query is used because the real table name is `SuperSetEntity` (no `[Table]` attribute), not `SuperSets`.
_Note:_ existing user databases may already have lost `TrainingExerciseComm` rows to the old bug (rows whose `_id` matched a deleted superset id); that data cannot be recovered.

### ✅ DATA-05 — Controls/ImageCache.cs, Services/Repository.cs — FIXED
`Repository` now keeps image bytes in a `ConcurrentDictionary<string, byte[]?>` (misses are cached too). It exposes `TryGetCachedImageData` / `GetImageData` and invalidates an entry in `SaveImage`. `ImageCache` shows cache hits immediately; on a miss it loads via `Task.Run` and drops the result if the cell was recycled meanwhile. The data layer has no dependency on UI controls.
_Remaining:_ index on `Url`, duplicate loads per bind, and full-resolution decode are tracked in DATA-13.

### ❌ DATA-06 — App.xaml.cs:99-135
142 sequential HTTP downloads at startup, `HttpClient` not reused, no `CancellationToken`.
**Rescan note:** every launch re-downloads **all** images in full (no ETag / `If-Modified-Since`, change detection only compares byte length). The loop runs from `Dispatcher.Dispatch`, so the 142 `GetImage` / `SaveImage` calls run synchronously on the UI thread.
**Fix:** Use `static readonly HttpClient`, parallel `Task.WhenAll` (bounded), add `CancellationToken`, use conditional requests or a server-side version manifest, and do the DB work on a background thread.

### ✅ SVC-03 — Controls/StepProgressBar.cs — FIXED
The static relay event is gone. Each control subscribes to its own `ItemsSource` collection and unsubscribes when `ItemsSource` changes.

### ❌ DATA-08 — Repository.cs:66
`async void InitBasic` — startup race condition, exceptions silently swallowed.
**Fix:** Use factory `static async Task<Repository> CreateAsync(...)`.

### 🟡 IMPL-01 — Views/TrainingImplementPage.xaml.cs:261-320
**Progress:** the timer now saves only every 15 ticks (`SaveEveryTicks`, ~15 s) instead of every second, and also saves on `Window.Stopped` / `Destroying` (:154-175). The save (`TrainingViewModel.SaveToFile`: build `TrainingSerialize`, serialize, write file) still runs synchronously on the UI thread.
**Remaining fix:** serialize and write off the UI thread.

Original finding — the 1-second workout timer tick did all of this on the UI thread:
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

### 🟡 VM-08 — ExerciseListPageViewModel.cs:162
The `Items` `ObservableCollection` is still replaced on every search keystroke (`ApplyFilter`).
**Progress:** the load runs in `Task.Run`, and search now filters the already-loaded `BaseItems` in memory (see VM-16), so a keystroke no longer touches the DB.
**Fix:** Clear + re-add in place, or use RangeObservableCollection. See VM-16 for the related debounce/race problem.

### ❌ VM-09 — BlogsPageViewModel.cs:21
`async void LoadItems()` — exceptions silently swallowed. The same pattern is in `ExerciseListPageViewModel.LoadItemsAsync` (:121, called from the constructor) and `SocialWorkoutsPageViewModel.OnAppearing` (:72).
**Fix:** Change to `async Task LoadItemsAsync()`.

### ✅ VM-10 — ExerciseListPageViewModel.cs:110 — FIXED
`BaseItems` is now only built once (`if (!BaseItems.Any())`).

### ❌ VM-13 — TrainingViewModel.cs:57
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

### ❌ DATA-09 — Services/WorkoutService.cs:19
N individual UPDATE calls in `UpdateExerciseNameAndDescription` without a transaction, plus an O(n·m) `inits.FirstOrDefault` per exercise.
**Fix:** Build a `Dictionary<int, BaseExercise>` by CodeNum; `database.UpdateAll(toUpdate)` inside `RunInTransaction`.

### ❌ DATA-10 — DataManageViewModel.cs:85
Import (`SetRepositoryData`) runs 2000+ individual INSERTs without a transaction.
**Fix:** Wrap all inserts in `database.RunInTransaction(() => { ... })`.

### ❌ DATA-11 — Entity models
No `[Indexed]` on the `TrainingId`, `ExerciseId`, `LastTrainingId` foreign key columns, or on `ImageEntity.Url` (see DATA-13).
**Fix:** Add `[Indexed]` attribute to those properties.

### ❌ CS-01 — Repository.cs:145
Every app start re-serializes the description of all ~150 exercises (now `System.Text.Json`) and issues an UPDATE per exercise. There is no change detection and no transaction.
**Fix:** Store a resource version/hash in `Settings` and skip when unchanged; otherwise batch in `RunInTransaction`.

### ❌ CS-02 — DataManageViewModel.cs:54
`File.ReadAllText` + JSON deserialization + DB writes all block the UI thread. Export (`BuildExportJson` + `File.WriteAllText`, :205, :244) does the same.
**Fix:** Use `File.ReadAllTextAsync` + `Task.Run` for parse and DB writes.

### ❌ SVC-01 — Services/DataService.cs:50
HTTP calls have no `CancellationToken`, so requests continue after navigation.
**Fix:** Add `CancellationToken ct` parameter and pass to `ExecuteAsync`.

### ❌ CS-04 — TrainingExercisesPageViewModel.cs:528
`SaveNewExerciseOrder` does N individual UPDATEs on every drag-drop reorder, without a transaction.
**Fix:** Use `database.RunInTransaction` + `UpdateAll`.

### 🆕 DATA-13 — Controls/ImageCache.cs:15-39, Models/Database/ImageData.cs:10
`LoadImage()` runs from `Loaded` **and** from each of the `CodeNum` / `ExerciseId` property-changed callbacks, so a recycled cell hits the DB 2–3× per bind. `GetImage` filters by `Url`, which is not indexed, so each hit is a full table scan. Images are decoded at full resolution into a 60×60 view.
**Fix:** Coalesce into one load after both properties are set, index `Url` (or key by CodeNum), cache decoded/downsampled bytes in memory.

### 🆕 DATA-14 — Views/ExerciseItemPage.xaml.cs:71
Saving a custom exercise with an image **always inserts a new `ImageEntity`** (Id = 0), even when one exists for `new_{id}`. The images table grows on every save, and `Find` returns the oldest row, so a stale image is shown. Deleting an exercise never deletes its image.
**Fix:** Look up the existing row and update it; delete the image in `Remove_clicked`.

### ✅ NET-01 — TrainingsPageViewModel.cs — FIXED
`TrainingsPageViewModel` no longer calls `UpdateFirebaseToken()`; the token is sent once from `App` on start.

Original finding — `LoadItems()` ran on every appearance of the main tab. Each call dispatches `UpdateFirebaseToken()`, which on iOS makes 4 network calls (`CheckIfValidAsync`, `GetTokenAsync`, `SendFirebaseTokenAsync`, `PostActionAsync(Enter)`). This duplicates the work `App.OnStart` already does and inflates the "Enter" analytics.
**Fix:** Run once per app session (or on token refresh), not per page appearance.

### ✅ VM-16 — ExerciseListPageViewModel.cs:90 — FIXED
`UpdateItems()` now only runs `ApplyFilter()` over the already-loaded `BaseItems` in memory: no DB read, no item re-creation, no fire-and-forget race, and the list is no longer hidden while typing. There is still no debounce, which is acceptable for an in-memory filter. The collection replacement per keystroke is tracked in VM-08.

Original finding — each search keystroke called `UpdateItems()`, with no debounce. Every call:
- re-reads the whole `Exercises` table from SQLite
- re-creates every `ExerciseListItemViewModel` (muscle parsing + description JSON)
- runs as a fire-and-forget `async void` with no cancellation, so a slow earlier search can finish last and **overwrite newer results**

The list is also hidden while `IsBusy`, so it flickers on every keystroke.
**Fix:** Debounce (~300 ms) with a `CancellationTokenSource`; filter the already-loaded `BaseItems` in memory instead of hitting the DB; keep the list visible during filtering.

### 🟡 VM-18 — TrainingsPageViewModel.cs:133, :231
`FillGroupedTraining` creates `new TrainingUnion(union)` (JSON-deserializing `TrainingIDsString`) for **every union for every training**, which is O(T·U) JSON parses; :133 parses every union once more.
**Progress:** last-performed times are precomputed into a dictionary, `Settings.GetLanguage()` is called once per load, and the whole build runs in `Task.Run` (`ReadPageData`, see VM-03).
**Fix:** Precompute `Dictionary<int trainingId, TrainingUnion>` once per load.

### 🆕 VM-20 — BlogsPageViewModel.cs:35, :76
- The list loads full `BlogEntity` rows, including the HTML `Content` of every post, just to show titles.
- `OpenBlog` **always** re-downloads the post from the API and rewrites it to the DB, even when content is already cached.
- New blogs are inserted one by one without a transaction.

**Fix:** Project only list columns (or store content in a separate table), fetch content only when `Content` is empty, and batch inserts.

### 🆕 DATA-15 — Missing transactions (additional sites)
Multi-row writes are not wrapped in `RunInTransaction`, so each row is a separate fsync:
- ~~`TrainingImplementPage.xaml.cs` — `SaveLastTrainingWithExercises` + `SaveChangedExercises`~~ ✅ now one `RunInTransaction` inside `Task.Run` (:423)
- `App.xaml.cs:164` — `IncomingTraining`
- `PreparedTrainingsPageViewModel.cs:383` — `SaveNewTrainingViewModelToDatabase`
- `WorkoutService.cs:42` — `CreateWorkoutAsync` (no callers since the questionnaire builds workouts locally; see CLEAN_CODE_VIOLATIONS.md DEAD-11)
- `TrainingsPageViewModel.cs:245` — `DuplicateSelectedTraining`
- `TrainingExercisesPageViewModel.cs:309/439/475` — `CreateSuperSet`, `AcceptTrainingForMoveOrCopy`, `CreateTrainingFromSelectedExercises`
- `HistoryTrainingPageViewModel.cs:216` — `RemoveLastTraining` (N deletes; use `DELETE ... WHERE LastTrainingId = ?`)
- `Repository.cs:72/85` — `DeleteBrokenBlogs`, `DeleteUnused`

**Fix:** Wrap each in `RunInTransaction`; replace delete loops with single `DELETE ... WHERE`.

### 🆕 XAML-07 — Compiled bindings missing on most pages
Only 13 XAML files declare `x:DataType`. These busy pages still use slower reflection-based bindings for every cell:
- TrainingsPage
- BlogsPage
- PreparedTrainingsPage
- WeightViewAndSetPage
- SuperSetControl

**Progress:** HistoryTrainingPage, HistoryTrainingExercisesPage, StatisticsPage, FilterPage, ExerciseListPage and WorkoutQuestinariumPage now use compiled bindings (17 XAML files declare `x:DataType`).

**Fix:** Add `x:DataType` to pages and `DataTemplate`s (enable XC0022/XC0025 warnings).

---

## LOW SEVERITY

### ✅ VM-11 — ExerciseListPageViewModel.cs:20 — FIXED
Exercises already in the workout are kept in a `HashSet<int> existedExerciseIds`; `FillSelectedIndexes` is gone.

### ✅ VM-12 — ExerciseListPageViewModel.cs:78-88 — FIXED
`ApplyQueryAttributes` marks items with one `HashSet` lookup each; the nested `FirstOrDefault` loop is gone.

### ❌ VM-14 — TrainingViewModel.cs:183
The LINQ chain in `GetSuperSetNum` is repeated on every `AddExercise` call, which makes loading a training O(n²).
**Fix:** Compute distinct superset IDs once before the loop.

### ✅ VM-15 — HistoryTrainingPageViewModel.cs:18 — FIXED
The list is now `static readonly` and is `Clear()`ed before re-adding, so it no longer grows unboundedly.

### ✅ XAML-04 — StatisticsPage.xaml — FIXED
The 8 identical medal + label blocks were replaced by a 2-column grid of stat tiles and a top-3 card.

### 🟡 XAML-05 — TrainingExercisesPage.xaml:41
Unused `SelectionMode="Single"` overhead, because taps are handled by a gesture recognizer.
**Progress:** ExerciseListPage.xaml now uses `SelectionMode="None"`.
**Fix:** Set `SelectionMode="None"`.

### ❌ XAML-06 — ExerciseItemPage.xaml:162, :180, :198
Each description field has both an Editor and a Label, and both are always created.
**Fix:** Use single control with `IsReadOnly` toggle or DataTemplateSelector.

### ❌ SVC-02 — Services/Settings.cs:162
`new CultureInfo(CultureName)` is allocated on every `GetLanguage()` call. It is called in hot loops: per chart entry (WeightViewAndSetPageViewModel.cs:201), per training (VM-18), per exercise at startup.
**Fix:** Cache with invalidation on `CultureName` setter.

### ✅ CS-03 — WorkoutQuestinariumPageViewModel.cs — FIXED
`PrepareRequest()` and its `StringBuilder` were removed; the questionnaire now builds the workout locally through `WorkoutQuestinariumService`.

### 🆕 VM-17 — Commands allocated per property access (remaining sites)
Same anti-pattern as the fixed VM-07:
- DataManageViewModel.cs:40-41
- Controls/ExerciseView.xaml.cs:10
- ViewModels/ImplementTrainingExerciseViewModel.cs:105, :112, :120
- LoginPageViewModel.cs:54-58
- ForgotPasswordPageViewModel.cs:31

**Fix:** `_cmd ??= new ...` or initialize in constructor.
**Progress:** WeightViewAndSetPageViewModel, HistoryTrainingPageViewModel, SocialWorkoutsPageViewModel, TrainingsPageViewModel, SuperSetControl and TrainingImplementPage now create their commands once.

### 🆕 VM-21 — PreparedTrainingsPageViewModel.cs:90
`FillTrainings()` now runs on every `OnAppearing`. Each run re-reads the full `Exercises` table and rebuilds 12 template VMs + 12 embedded `ImageSource`s. The collection is then assigned without raising `PropertyChanged`.
**Fix:** Build once (lazy) and reuse; raise `PropertyChanged` if replacing the collection.

### 🆕 VM-22 — SocialWorkoutsPageViewModel.cs:102/134/160, SocialWorkoutViewModel.cs:41
Each refresh or next page synchronously reads the whole `Exercises` table on the UI thread. `MergeExercises` then does `FirstOrDefault` per exercise, which is O(workouts × exercises × library).
**Fix:** Cache a `Dictionary<int CodeNum, ExerciseEntity>` once per session.

### 🆕 XAML-08 — Controls/SuperSetControl.xaml:152
A `CollectionView` (line 152) is nested inside a `ScrollView` (line 109). It cannot virtualize and measures all items.
**Fix:** Use `BindableLayout` on a `VerticalStackLayout` for the small set list.

### 🆕 UI-01 — Views/FilterPage.xaml.cs:701
`DrawStringImplement` re-parses coordinate strings (`Split` + `Convert.ToSingle`) and rebuilds every `SKPath` on each paint.
**Progress:** loading a filter now redraws once (`FilterPageViewModel` raises a single `SelectedMuscles` change) instead of once per muscle (~20×).
**Fix:** Pre-parse points once (static `SKPoint[]`), cache paths per scale.

### 🆕 UI-02 — Views/TrainingImplementPage.xaml.cs:305
`PlaySound()` creates a new Android `Ringtone` on every tick during the last 10 s of rest, without stopping or releasing the previous one.
**Fix:** Keep one `Ringtone` / `SoundPool` instance; play once per threshold crossing.

### 🆕 SVC-04 — Services/DataService.cs:65, :30
- `GetBlogAsync` allocates a new `JsonSerializerOptions` on every call, which discards the serializer metadata cache.
- The shared `RestClient` has a **2 s** timeout for all endpoints, including `/exercises/query` (AI generation) and full blog content, so on slow networks requests may fail and be retried by the user.

**Fix:** `static readonly JsonSerializerOptions`; per-request timeouts for long endpoints.

### 🆕 CS-05 — DataManageViewModel.cs:75-82
On import failure, the **entire file content** (potentially megabytes) is attached to the telemetry event.
**Fix:** Log size/hash or a truncated prefix only.

### 🆕 MEM-01 — MauiProgram.cs:54-82
All pages and ViewModels are registered as singletons, so their visual trees and data stay in memory for the app's lifetime. This includes the per-workout detail page `TrainingExercisesPage` and Login/Register/ForgotPassword.
**Fix:** Register detail and auth pages (+ VMs) as `Transient`.

---

## Non-performance bugs noticed during rescan

- **DATA-14** (above) — duplicate image rows, so a stale image is shown.
- ~~`StatisticsViewModel.cs:57` — "most often day" orders by `DayOfWeek` instead of by count, so it always returns the earliest weekday present.~~ ✅ Fixed: it now orders by count.
- `TrainingExercisesPageViewModel.cs:265-268` — `CreateSuperSet` shows the "need ≥ 2 exercises" alert but has no `return`, so it still creates the superset.
- ~~`ExerciseListPage.xaml.cs` — `SearchButtonPressed` stops working after the first navigation away.~~ ✅ Fixed: the handler is now wired in XAML (`ExerciseListPage.xaml:34`).
- `TrainingImplementPage.xaml.cs:46/215` — `StepProgressBarControl.PropertyChanged` is subscribed in the constructor but unsubscribed in `OnDisappearing`, so it stops working if the page appears again.
- `SocialWorkoutsService.cs:50` — `GetFeedAsync` doesn't call `EnsureValidTokenAsync()`, unlike the other authorized calls.

---

## Top 5 Highest-Impact Fixes

1. **TrainingImplementPage timer (IMPL-01)** — now throttled to ~15 s; move the serialize + file write off the UI thread
2. **ImageCache (DATA-05 + DATA-13)** — 2–3 unindexed sync DB hits per scroll cell → single load + index + in-memory cache
3. **Repository query shape (DATA-02, DATA-11)** — remaining full-table scans and missing FK indexes
4. **Remaining LoadData/LoadItems ViewModels (VM-02, VM-04, VM-05; VM-01 and VM-03 done)** — sync DB on UI thread → `Task.Run`
5. **Missing transactions (DATA-09/10, CS-04, DATA-15)** → `RunInTransaction` / single `DELETE ... WHERE`
