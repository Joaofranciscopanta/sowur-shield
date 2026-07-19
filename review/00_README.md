# Execution (resumable across usage-limit windows)

1. Read PROGRESS.md.
2. Pick the FIRST unmarked task in 03_WORKLIST.md whose dependencies are completed.
3. Implement it. Use subagents freely to speed up.
4. When done: mark [x] in the worklist, append 1 line to PROGRESS.md (date, TASK-id, result), commit "TASK-### done".
5. Next. If you hit the limit, stop; on resume, repeat from step 1 — state lives in PROGRESS.md + git, nothing is lost.
