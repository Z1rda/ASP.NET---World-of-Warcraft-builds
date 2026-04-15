# Project Guidelines

## Agent Routing Rules
- For any request that includes UI/UX design, visual styling, layout, components, page polish, or frontend visual refactors, the main agent must invoke the `WoW UIUX Dark Fantasy` sub-agent before writing UI code.
- Trigger examples: CSS changes, Razor markup styling, 3-column page layout work, visual component redesign, theme updates, responsive UI polish.
- The main agent should treat the UI sub-agent as the implementation owner for visuals and interaction presentation.

## Delegation Contract
- Main agent responsibility: detect UI-oriented scope, invoke sub-agent, and integrate returned changes.
- `WoW UIUX Dark Fantasy` responsibility: produce and apply all UI/UX code changes in the project visual style.
- If a task mixes backend and UI, split work so UI portions are delegated to the sub-agent first, then continue backend work in main agent.

## Delegation Logging
- Whenever the main agent delegates UI/UX work, the delegation must be visible in `lab-1/agent_log.txt`.
- The log evidence should include a `runSubagent` tool entry showing `WoW UIUX Dark Fantasy` was invoked for the UI portion.

## UI Style Guardrails
- Keep the established style direction: dark fantasy + northern mythology + WotLK motifs.
- Avoid generic Bootstrap look, purple-gradient dashboard style, and corporate SaaS aesthetics.
- Keep Arial typography unless the user explicitly asks otherwise.
