# Projects Module

A comprehensive project management feature for organizing work with milestones, todos, and notes.

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
- Contains milestones, todos, and notes

### Milestones
- Nested within projects
- Name, description, status, and due date
- Ordered via sortOrder for custom sequencing
- Status tracking: NotStarted, InProgress, Completed
- Contains todos and notes

### Todos
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
│   │   ├── TodosController.cs         # /api/v1/todos (with /standalone endpoint)
│   │   └── NotesController.cs         # /api/v1/notes (with /standalone endpoint)
│   └── DependencyInjection.cs
├── DonkeyWork.Agents.Projects.Contracts/
│   ├── Models/
│   │   ├── Enums.cs                   # ProjectStatus, MilestoneStatus, TodoStatus, TodoPriority
│   │   ├── ProjectV1.cs               # Project DTOs
│   │   ├── MilestoneV1.cs             # Milestone DTOs
│   │   ├── TodoV1.cs                  # Todo DTOs
│   │   ├── NoteV1.cs                  # Note DTOs
│   │   ├── TagV1.cs                   # Tag DTOs
│   │   └── FileReferenceV1.cs         # File reference DTOs
│   └── Services/
│       ├── IProjectService.cs
│       ├── IMilestoneService.cs
│       ├── ITodoService.cs
│       └── INoteService.cs
└── DonkeyWork.Agents.Projects.Core/
    └── Services/
        ├── ProjectService.cs
        ├── MilestoneService.cs
        ├── TodoService.cs
        └── NoteService.cs
```

## Database Schema

Located in `src/common/DonkeyWork.Agents.Persistence/`:

### Entities (`Entities/Projects/`)
- `ProjectEntity` - Main project entity
- `MilestoneEntity` - Project milestones
- `TodoEntity` - Todos (standalone or linked)
- `NoteEntity` - Notes (standalone or linked)
- `ProjectTagEntity`, `MilestoneTagEntity`, `TodoTagEntity`, `NoteTagEntity` - Tags
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

### Todos
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/todos` | List all todos |
| GET | `/api/v1/todos/standalone` | List standalone todos only |
| GET | `/api/v1/todos/{id}` | Get todo details |
| POST | `/api/v1/todos` | Create todo |
| PUT | `/api/v1/todos/{id}` | Update todo |
| DELETE | `/api/v1/todos/{id}` | Delete todo |

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
- `ProjectDetailPage.tsx` - View project with milestones, todos, notes
- `TodosPage.tsx` - Manage standalone todos
- `NotesPage.tsx` - Manage standalone notes

### Navigation
Projects are accessible via the sidebar under the "Projects" group with links to:
- All Projects
- Todos (standalone)
- Notes (standalone)

### API Client
Extended in `lib/api.ts` with:
- Type definitions for all models
- CRUD functions for projects, milestones, todos, notes

## Testing

Test project: `test/projects/DonkeyWork.Agents.Projects.Core.Tests/`

### Test Coverage
- `ProjectServiceTests` - 18 tests
- `MilestoneServiceTests` - 13 tests
- `TodoServiceTests` - 16 tests
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
The migration `20260131100000_AddProjectsModule` creates the `projects` schema with all required tables.
