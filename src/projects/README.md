# Projects Module

A comprehensive project management feature for organizing work with milestones, tasks, and notes.

## Prerequisites

- **.NET 10 SDK** is required to build and test
- Install via script: `curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0`
- After installation, add to PATH: `export PATH="$HOME/.dotnet:$PATH"`

## Features

### Projects
- Top-level organizational unit with name, description, and success criteria
- Status tracking: NotStarted, InProgress, Completed, OnHold
- Tags for categorization
- File references for linking related documents
- Contains milestones, tasks, and notes

### Milestones
- Nested within projects
- Name, description, status, and due date
- Ordered via sortOrder for custom sequencing
- Status tracking: NotStarted, InProgress, Completed
- Contains tasks and notes

### Tasks
- Can exist **standalone** or within a project/milestone
- Title and description (markdown supported)
- Priority levels: Low, Medium, High, Critical
- Status tracking: NotStarted, InProgress, Completed
- Completion notes for documenting resolution
- Tags for categorization

### Notes
- Can exist **standalone** or within a project/milestone
- Title and content (markdown supported)
- Tags for categorization
- Ordered via sortOrder

## Architecture

```
src/projects/
├── DonkeyWork.Agents.Projects.Api/
│   ├── Controllers/
│   │   ├── ProjectsController.cs      # /api/v1/projects
│   │   ├── MilestonesController.cs    # /api/v1/projects/{projectId}/milestones
│   │   ├── TasksController.cs         # /api/v1/tasks (with /standalone endpoint)
│   │   └── NotesController.cs         # /api/v1/notes (with /standalone endpoint)
│   └── DependencyInjection.cs
├── DonkeyWork.Agents.Projects.Contracts/
│   ├── Models/
│   │   ├── Enums.cs                   # ProjectStatus, MilestoneStatus, TaskItemStatus, TaskItemPriority
│   │   ├── ProjectV1.cs               # Project DTOs
│   │   ├── MilestoneV1.cs             # Milestone DTOs
│   │   ├── TaskItemV1.cs              # Task DTOs
│   │   ├── NoteV1.cs                  # Note DTOs
│   │   ├── TagV1.cs                   # Tag DTOs
│   │   └── FileReferenceV1.cs         # File reference DTOs
│   └── Services/
│       ├── IProjectService.cs
│       ├── IMilestoneService.cs
│       ├── ITaskItemService.cs
│       └── INoteService.cs
└── DonkeyWork.Agents.Projects.Core/
    └── Services/
        ├── ProjectService.cs
        ├── MilestoneService.cs
        ├── TaskItemService.cs
        └── NoteService.cs
```

## Database Schema

Located in `src/common/DonkeyWork.Agents.Persistence/`:

### Entities (`Entities/Projects/`)
- `ProjectEntity` - Main project entity
- `MilestoneEntity` - Project milestones
- `TaskItemEntity` - Tasks (standalone or linked)
- `NoteEntity` - Notes (standalone or linked)
- `ProjectTagEntity`, `MilestoneTagEntity`, `TaskItemTagEntity`, `NoteTagEntity` - Tags
- `ProjectFileReferenceEntity`, `MilestoneFileReferenceEntity` - File references

### EF Configurations (`Configurations/Projects/`)
All entities use Fluent API configuration with:
- Table names in `projects` schema (e.g., `projects.projects`, `projects.milestones`)
- Proper indexes on foreign keys and common query patterns
- String conversion for enum columns

## API Endpoints

### Projects
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/projects` | List all projects |
| GET | `/api/v1/projects/{id}` | Get project details |
| POST | `/api/v1/projects` | Create project |
| PUT | `/api/v1/projects/{id}` | Update project |
| DELETE | `/api/v1/projects/{id}` | Delete project |

### Milestones
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/projects/{projectId}/milestones` | List project milestones |
| GET | `/api/v1/projects/{projectId}/milestones/{id}` | Get milestone details |
| POST | `/api/v1/projects/{projectId}/milestones` | Create milestone |
| PUT | `/api/v1/projects/{projectId}/milestones/{id}` | Update milestone |
| DELETE | `/api/v1/projects/{projectId}/milestones/{id}` | Delete milestone |

### Tasks
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/tasks` | List all tasks |
| GET | `/api/v1/tasks/standalone` | List standalone tasks only |
| GET | `/api/v1/tasks/{id}` | Get task details |
| POST | `/api/v1/tasks` | Create task |
| PUT | `/api/v1/tasks/{id}` | Update task |
| DELETE | `/api/v1/tasks/{id}` | Delete task |

### Notes
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/notes` | List all notes |
| GET | `/api/v1/notes/standalone` | List standalone notes only |
| GET | `/api/v1/notes/{id}` | Get note details |
| POST | `/api/v1/notes` | Create note |
| PUT | `/api/v1/notes/{id}` | Update note |
| DELETE | `/api/v1/notes/{id}` | Delete note |

## Frontend

Located in `src/frontend/src/`:

### Pages
- `ProjectsPage.tsx` - List and manage projects
- `ProjectDetailPage.tsx` - View project with milestones, tasks, notes
- `TasksPage.tsx` - Manage standalone tasks
- `NotesPage.tsx` - Manage standalone notes

### Navigation
Projects are accessible via the sidebar under the "Projects" group with links to:
- All Projects
- Tasks (standalone)
- Notes (standalone)

### API Client
Extended in `lib/api.ts` with:
- Type definitions for all models
- CRUD functions for projects, milestones, tasks, notes

## Testing

Test project: `test/projects/DonkeyWork.Agents.Projects.Core.Tests/`

### Test Coverage
- `ProjectServiceTests` - 18 tests
- `MilestoneServiceTests` - 13 tests
- `TaskItemServiceTests` - 16 tests
- `NoteServiceTests` - 16 tests

### Running Tests
```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test test/projects/DonkeyWork.Agents.Projects.Core.Tests/
```

## Usage

### Register Services
In `Program.cs`:
```csharp
builder.Services.AddProjectsApi();
```

### Run Database Migration
The migration `20260207100000_RenameTodosToTasks` renames the `projects.todos` and `projects.todo_tags` tables to `projects.tasks` and `projects.task_tags`.
