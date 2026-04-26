# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TrainingDay is a cross-platform fitness training app built with .NET MAUI 10, targeting Android (API 26+) and iOS (15.0+). It uses MVVM, SQLite for local storage, and a REST API at `https://api.trainingday.space/api` for blogs, exercise library, and push notifications.

## Build & Test Commands

```bash
# Build
dotnet build TrainingDay.Maui.sln

# Run tests
dotnet test TrainingDay.Maui.Tests/TrainingDay.Maui.Tests.csproj

# Publish release
dotnet publish TrainingDay.Maui/TrainingDay.Maui.csproj -c Release -f net10.0-android
dotnet publish TrainingDay.Maui/TrainingDay.Maui.csproj -c Release -f net10.0-ios
```

## Architecture

### MVVM with Shell Navigation

- **Views** (`/Views/*.xaml`) — XAML pages, thin code-behind
- **ViewModels** (`/ViewModels/Pages/`) — business logic, extend `BaseViewModel` which implements `INotifyPropertyChanged` via `SetProperty`
- **Models** (`/Models/Database/`) — SQLite entity classes (Exercise, Training, TrainingExerciseItem, etc.)
- **AppShell.xaml** — tab-based shell navigation with registered routes

### Data Layer

- **Repository** (`/Services/Repository.cs`) — direct SQLite CRUD; synchronous by default (known issue: some methods block the UI thread)
- **IDataService / DataService** (`/Services/DataService.cs`) — REST HTTP client using RestSharp; fetches blogs, exercises, sends FCM tokens
- **Settings** (`/Extensions/Settings.cs`) — static wrapper around `Preferences` for key-value app config
- Database file name and other constants live in `ConstantKeys` (`/Extensions/ConstantKeys.cs`)

### Dependency Injection

All services are registered in `MauiProgram.cs`. ViewModels are also registered there and resolved via constructor injection into pages.

### Commands

`DoOnceCommand` and `DoOnceCommand<T>` (`/Controls/`) wrap `ICommand` to prevent concurrent execution — use these for any async button handler instead of plain `Command`.

### Messaging

Cross-ViewModel communication uses `WeakReferenceMessenger` from `CommunityToolkit.Mvvm.Messaging`. Message types live in `/Models/Messages/`.

### Localization

`LocalizationResourceManager` (`/Extensions/`) is a singleton that drives runtime culture switching. String resources are `.resx` files under `/Resources/Languages/` (English, Russian, German).

## Key Dependencies

| Purpose | Package |
|---|---|
| MVVM helpers | CommunityToolkit.Mvvm 8.4.0 |
| MAUI controls | CommunityToolkit.Maui 14.0.0 |
| SQLite ORM | sqlite-net-e 1.11.0 |
| Charts | SkiaSharp + Microcharts (local `/Plugins`) |
| HTTP | RestSharp 112.1.0 |
| Push notifications | Plugin.Firebase.CloudMessaging (iOS), Firebase Messaging (Android) |
| Analytics/crash | SentinelAnalytics.MAUI 1.0.7 |
| Testing | NUnit 3.14.0 + NUnit3TestAdapter |

## External Dependency

`TrainingDay.Common` is a sibling project at `../TrainingDay-Core/TrainingDay.Common/`. It must be present on disk for the solution to build.

## Pages

### TrainingItemsBasePage
**ViewModel:** `TrainingItemsBasePageViewModel`  
Main dashboard. Lists all user-created trainings grouped by category (tabs). Long-press a training for context menu (delete, duplicate, move). Navigates to `PreparedTrainingsPage` to add new trainings and to `TrainingExercisesPage` to edit an existing one.  
DB: reads `TrainingEntity`, `LastTrainingEntity` (last performed date).

### PreparedTrainingsPage
**ViewModel:** `PreparedTrainingsPageViewModel`  
Gallery of 12 built-in workout templates (Beginner, Morning, Cardio, Abs, etc.) plus a "Create Custom" button and an AI questionnaire entry point. Selecting a template saves a new `TrainingEntity` and its `TrainingExerciseEntity` rows, then navigates to `TrainingExercisesPage`.  
DB: reads `ExerciseEntity`; writes `TrainingEntity`, `TrainingExerciseEntity`, `SuperSetEntity`.

### TrainingExercisesPage
**ViewModel:** `TrainingExercisesPageViewModel`  
Edit a training's exercise list. Supports drag-to-reorder, bulk select for copy/move/superset, inline name editing. Navigates to `ExerciseListPage` to pick exercises and to `TrainingExercisesMoveOrCopy` for bulk move/copy. "Make Training" button starts the workout.  
DB: reads/writes `TrainingEntity`, `TrainingExerciseEntity`; messaging: `ExercisesSelectFinishedMessage`.

### TrainingExerciseItemPage
**ViewModel:** none (uses `ExerciseView` control)  
Edit a single exercise's parameters within a training (sets, reps, weight, time). Saves on toolbar button. Receives exercise data via Shell query parameters.  
DB: writes `TrainingExerciseEntity`.

### ExerciseItemPage
**ViewModel:** `ExerciseViewModel`  
Create or edit an exercise definition: name, image, targeted muscles, difficulty (1–3), type (Reps / Reps+Weight / Time), and three instruction sections. Shows a muscle-map overlay for muscle selection. Deletes are blocked if the exercise is used in any training.  
DB: reads/writes `ExerciseEntity`; writes `ImageEntity`.

### ExerciseListPage
**ViewModel:** `ExerciseListPageViewModel`  
Browse and multi-select exercises to add to a training. Has a search bar and a filter button that opens `FilterPage`. Already-added exercises show a green checkmark. Sends `ExercisesSelectFinishedMessage` on confirm.  
DB: reads `ExerciseEntity`.

### FilterPage
**ViewModel:** `FilterViewModel`  
Filter exercises by equipment (none / barbell / dumbbell), difficulty, and muscles. Muscle selection uses a body image map plus tag buttons. Sends `FilterAcceptedForExercisesMessage` on save.

### TrainingImplementPage
**Code-behind heavy** (`TrainingImplementPage.xaml.cs`)  
Active workout execution screen. Displays a step progress bar across exercises, a do/rest timer toggle with configurable rest duration, and a `SuperSetControl` for entering weight/reps/time per set. Bottom buttons: Cancel (with confirmation), Skip, Finish. Shows confetti on completion.  
DB: writes `LastTrainingEntity`, `LastTrainingExerciseEntity`. API: `PostActionAsync`. Messaging: `IncomingTrainingAddedMessage`.

### HistoryTrainingPage
**ViewModel:** `HistoryTrainingPageViewModel`  
Paginated list of completed workouts grouped by recency (Week / Month / 3 Months / Half-Year / Year / Older). Tapping a row navigates to `HistoryTrainingExercisesPage`.  
DB: reads `LastTrainingEntity` with threshold-based pagination.

### HistoryTrainingExercisesPage
**ViewModel:** `HistoryTrainingPageViewModel` (shared)  
Detail view for a completed workout. Shows all exercises with recorded results. Toolbar: "Start Again" (re-creates a new training from the history entry) and delete (removes the history record).  
DB: reads `LastTrainingExerciseEntity`; deletes `LastTrainingEntity`.

### StatisticsPage
**ViewModel:** `StatisticsViewModel`  
Read-only achievement summary: total trainings, total exercises, total time, distinct-exercise percentage, most active weekday, top-3 most-performed exercises (gold/silver/bronze medals).  
DB: reads `LastTrainingEntity`, `LastTrainingExerciseEntity`, `ExerciseEntity`.

### WeightViewAndSetPage
**ViewModel:** `WeightViewAndSetPageViewModel`  
Body measurement tracker (weight, waist, hips). Period selector (Week → Year) drives a line chart comparing recorded values to goal. Values are entered inline and saved on unfocus.  
DB: reads/writes `WeightNoteEntity`. API: `PostActionAsync`.

### BlogsPage
**ViewModel:** `BlogsPageViewModel`  
List of news/blog posts fetched from the remote API and cached locally. Unread posts show a blue dot. Tapping navigates to `BlogItemPage`. New blogs are fetched on every page appearance.  
DB: reads/writes `BlogEntity`. API: `GetBlogsAsync`.

### BlogItemPage
**ViewModel:** `BlogViewModel`  
Full blog post rendered in a `WebView`. Fetches full HTML content from the API on first open and caches it.  
DB: writes `BlogEntity`. API: `GetBlogAsync`.

### SettingsPage
**ViewModel:** `DataManageViewModel` (for import/export)  
App configuration: language (en/ru/de, triggers app restart), weight unit (kg/lbs), keep-screen-on toggle, show-exercise-advice toggle. Also exposes data export and import. Toolbar button navigates to `StatisticsPage`. Android-only donate link.  
Calls: `UpdateExerciseNameAndDescription()` on language change.

### WorkoutQuestinariumPage
**ViewModel:** `WorkoutQuestinariumPageViewModel`  
6-step wizard that collects user preferences (goals, equipment, frequency, etc.) and calls the remote API to generate a personalised training plan. Each step renders either radio buttons (single choice) or checkboxes (multiple choice). Step progress shown in `StepProgressBar`.  
API: `GetExercisesByQueryAsync`, creates training via `CreateWorkoutAsync`. DB: writes `TrainingEntity`, `TrainingExerciseEntity`.

### TrainingExercisesMoveOrCopy
Destination picker shown as a modal when bulk-moving or bulk-copying exercises. Lists existing trainings; also has a "Create New" option. Used internally from `TrainingExercisesPageViewModel`.  
DB: reads `TrainingEntity`; writes `TrainingExerciseEntity`.

---

## Known Performance Issues

`PERFORMANCE_ISSUES.md` documents 31 open issues. The highest-impact ones to be aware of:

- **Synchronous DB on UI thread** — `LoadData`/`LoadItems` calls in ViewModels block rendering; wrap with `Task.Run()`
- **N+1 queries** — `Repository.cs:247` loads all training exercises then queries each exercise individually; should use a JOIN or single WHERE clause
- **Memory leak in StepProgressBar** — static event subscription at line 311 holds instance references; needs explicit unsubscription
- **Missing DB transactions** — batch inserts/updates should be wrapped in `RunInTransaction`

## Platform-Specific Code

Platform implementations live under `Platforms/Android/` and `Platforms/iOS/`. Push notification handling (`IPushNotification`) has separate implementations per platform.
