make an implementation plan and save it to docs/implementation_plan.md, create a list of task and save them to docs/task.md

review the implementation plan and tasks, proceed with the implementation plan and update the task completion while you do it, thanks

You batch properly
Use atlases
Avoid per-widget materials
Prefer shader masking

please review the current implementation of SUIM against this specification document, if you find that things are missing implement them, thanks

Custom Component Expansion

Dual-axis Spacing: The specification mentions spacing supporting 2 values (e.g., spacing="10 20"), but current implementation only supports a single int.
Z-Index Layering: ZIndex is tracked but doesn't seem to be used in the layout engine for global ordering.


