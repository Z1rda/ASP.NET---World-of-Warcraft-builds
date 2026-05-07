---
name: list-page
description: "Use when: create a list page or directory page in this app (center list + right details), add a new list view for an entity, or extend navigation to a new list page."
---

# List Page Skill (WoWprojekt)

## Purpose
Create a new read-only list page that fits the app's directory pattern (center list + right detail panel) and wire it into routing and navigation.

## When to use
- Adding a new list/directory page in Encyclopedia.
- Showing a list of entities with a selected item detail on the right.
- Extending navigation for a new list view.

## Steps
1. Identify the entity or aggregate to list (DB entity or computed summary).
2. Add a view model in Models/ViewModels/DirectoryViewModels.cs.
3. Add a controller action in Controllers/EncyclopediaController.cs using EF queries:
   - Use AsNoTracking().
   - Include related data needed for the detail panel.
   - Order results for stable lists.
4. Add a Razor view in Views/Encyclopedia/<Name>.cshtml:
   - Use existing CSS classes: content-header, content-subtitle, entity-list, entity-link, panel-title, detail-block, empty-state.
   - Render a selected item in @section RightPanel.
5. Add a nav link to Views/Shared/_Layout.cshtml.
6. Optional: add a card to Views/Home/Index.cshtml.

## Template (Razor)
- Center list: iterate Model.<Items> and link with asp-route-id.
- Right panel: show selected item or empty state message.

## Checklist
- Action route uses [HttpGet("directory/<name>")].
- View model includes both list and selected item.
- UI uses existing classes and matches dark-fantasy layout.
- No create/edit actions; read-only only.
