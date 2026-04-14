# Frontend Overview

This document explains the purpose of each visible section of the Richmond PD Records Portal frontend.

The frontend is a small single-page application served directly by the ASP.NET Core app. Its job is to authenticate the user, display operational data from the API, and let the user maintain incident and officer records.

## Login Screen

Purpose:
- Acts as the secure entry point into the application.
- Collects username and password and exchanges them for a JWT token through `/api/auth/login`.
- Stores the token in browser `localStorage` so the app can make authenticated API requests after login.

What the user sees:
- Department branding
- Username/password inputs
- Authentication button
- Login error area

Why it matters:
- Demonstrates the authentication flow in a simple, visible way.
- Establishes that all later frontend actions are backed by authenticated API access.

## Top Bar

Purpose:
- Gives the user persistent app identity and session context.
- Shows the currently signed-in user.
- Provides a sign-out action that clears the stored token.

What the user sees:
- App title: `RVA - RECORDS PORTAL`
- Current user label
- `SIGN OUT` button

Why it matters:
- Keeps navigation simple in a one-page app.
- Makes session state obvious during demos.

## Dashboard Overview

Purpose:
- Provides a quick operational snapshot before the user drills into records.
- Pulls summary data from `/api/incidents/dashboard`.

What the user sees:
- Open incidents
- Under investigation incidents
- Closed this month
- Total records

Why it matters:
- Makes the app feel more like a real management tool than a plain CRUD table.
- Gives the user immediate insight into system activity.

## Records Workspace

Purpose:
- Acts as the main working area for day-to-day record management.
- Separates incident management from officer management without leaving the page.

What the user sees:
- A tab bar with:
  - `INCIDENTS`
  - `OFFICERS`

Why it matters:
- Keeps the SPA organized.
- Supports multiple workflows inside one interface.

## Incidents Tab

Purpose:
- Displays persisted incident records from the database.
- Supports filtering, editing, archiving, restoring, pagination, and export.

What the user sees:
- Filters for incident status and type
- `INCLUDE ARCHIVED` toggle
- `EXPORT XML` button
- `REFRESH` button
- Incidents table
- Pagination controls

Why it matters:
- This is the primary records-management surface in the app.
- It demonstrates the app's read, update, archive, and restore workflow.

### Incident Filters

Purpose:
- Narrow the results shown in the incidents table.
- Help the user focus on a smaller operational slice of the data.

Current filters:
- Status
- Type
- Include archived

Why it matters:
- Shows that the UI is not just loading everything blindly.
- Demonstrates server-side filtering and record lifecycle awareness.

### Incident Table

Purpose:
- Presents incident records returned by the API in a scannable format.
- Lets the user take actions on individual records.

Columns:
- Case number
- Type
- Status
- Description
- Officer
- Location
- Reported date
- Actions

Why it matters:
- This is the clearest visual representation of persisted data from the database.
- It is the main place where the user interacts with existing incident records.

### Incident Actions

Purpose:
- Let the user manage a record directly from the table.

Current actions:
- `EDIT`
- `ARCHIVE` for active incidents
- `RESTORE` for archived incidents

Why it matters:
- Demonstrates update and soft-delete behavior in a realistic workflow.
- Makes CRUD behavior visible without using Swagger or direct API calls.

### Incident Edit Modal

Purpose:
- Allows a user to update an existing incident without leaving the page.
- Sends a `PUT` request to `/api/incidents/{id}`.

Editable fields:
- Description
- Occurred date/time
- Status
- Type
- Officer
- Location

Why it matters:
- Turns the app from read-only display into a true maintenance interface.
- Demonstrates update support in a user-friendly way.

### Archive / Restore Confirmation Dialog

Purpose:
- Prevents accidental record lifecycle changes.
- Confirms archive and restore actions before they are sent to the API.

Why it matters:
- Adds realism and safety to the UI.
- Makes soft-delete behavior feel intentional instead of abrupt.

### Pagination

Purpose:
- Limits how many incident records are displayed at once.
- Reflects the API's paginated incident endpoint.

Why it matters:
- Keeps the table manageable as the dataset grows.
- Demonstrates that the UI is consuming paged backend data instead of loading everything in one request.

### XML Export

Purpose:
- Lets the user export incident data through the `/api/incidents/export/xml` endpoint.
- Downloads the server-generated XML file in the browser.

Why it matters:
- Highlights a non-CRUD feature that makes the app stand out.
- Shows integration between backend reporting/output logic and the frontend UI.

## Officers Tab

Purpose:
- Displays officer records and supports active/inactive workforce management.
- Uses the officer endpoints to view, deactivate, and reactivate personnel.

What the user sees:
- Officer status filter
- Refresh button
- Officers table

Why it matters:
- Expands the app beyond incidents into related entity management.
- Reinforces that the app supports operational record maintenance across multiple data types.

### Officer Filter

Purpose:
- Lets the user switch between active officers, inactive officers, or all officers.

Why it matters:
- Makes the officer lifecycle visible.
- Supports the soft-delete style behavior already used for officers.

### Officers Table

Purpose:
- Presents officer records along with their current activity state and incident count.

Columns:
- Badge
- Officer name
- Rank
- Precinct
- Status
- Incident count
- Actions

Why it matters:
- Gives the app a second administrative workflow beyond incidents.
- Helps show relationships between officers and incidents.

### Officer Actions

Purpose:
- Let the user deactivate or reactivate officers from the UI.

Current actions:
- `DEACTIVATE`
- `REACTIVATE`

Why it matters:
- Demonstrates practical lifecycle management for related records.
- Makes the officer side of CRUD/admin behavior visible during a demo.

## Toast Notifications

Purpose:
- Give immediate feedback after user actions such as save, archive, restore, export, deactivate, or reactivate.

Why it matters:
- Improves usability.
- Confirms that user actions succeeded without forcing a page reload.

## Data Flow Behind The UI

At a high level, the frontend works like this:

1. The user logs in and receives a JWT token.
2. The frontend stores that token and sends it with API requests.
3. The frontend fetches records and dashboard data from the ASP.NET Core API.
4. The API reads from and writes to SQL Server through EF Core and stored procedures.
5. The UI updates to reflect the latest persisted data.

## Why This Frontend Works Well For A Demo

The frontend is intentionally simple, but each section shows a different part of the product story:

- Login shows authentication.
- Dashboard shows operational visibility.
- Incidents tab shows filtering, editing, archiving, restoring, pagination, and export.
- Officers tab shows related-entity management and active/inactive lifecycle behavior.

Together, these sections make the app feel like a small but realistic records-management product rather than a basic table of sample data.
