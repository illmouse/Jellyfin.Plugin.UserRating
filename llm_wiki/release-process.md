# Release Process

## General Rules

- Always pull with rebase (`git pull --rebase`) before starting work to integrate remote changes. This must be done before EVERY commit, not just at session start — the CI/CD pipeline builds and pushes changes (checksums, version bumps) to the repo between releases, so local state will diverge. Pulling rebase before each commit avoids having to resolve conflicts every time you push a new version
- Always ask before committing and pushing changes
- Never overwrite or move existing git tags — always create a new tag. Only force-replace a tag if the user explicitly asks
- A git tag must be created for every version (including betas) — Jellyfin repository requires a tag to fetch the plugin version
- In manifest.json, always add new versions as new entries at the top of the versions array — never replace or remove previous stable versions. Only the latest beta entry may be replaced/superseded
- Changelog: only include bug fixes that existed in a previous stable release. Do not include bugs found and fixed during the same development session — keep CHANGELOG.md clean from noise
- After a stable release is ready: remove transitional/beta tags from the repo (both local and remote) and remove beta entries from manifest.json — keep only stable versions
- Stable versions use 0 as the fourth octet (e.g., `1.11.0.0`). Beta versions have a non-zero fourth octet (e.g., `1.11.0.7`). When promoting a beta line to stable, remove all beta entries (non-zero fourth octet) from manifest.json and replace them with the single stable entry. Beta git tags should be deleted locally (`git tag -d vX.Y.Z.W`) and remotely (`git push origin --delete vX.Y.Z.W`)

## 1. Determine bump type

Ask the user which bump type:
- **feature** → bump second octet (e.g., `1.10.0.0` → `1.11.0.0`)
- **fix** → bump third octet (e.g., `1.10.0.0` → `1.10.1.0`)
- **beta** → bump fourth octet (e.g., `1.10.0.0` → `1.10.0.1`, next `1.10.0.2`, etc.)

## 2. Calculate new version

Read the current version from `Jellyfin.Plugin.UserRatings.csproj` (AssemblyVersion). Apply the bump:
- feature: increment second octet, reset third and fourth to 0
- fix: increment third octet, reset fourth to 0
- beta: increment fourth octet

## 3. Update changelog (feature and fix only)

- For **beta**: skip changelog updates
- For **feature** or **fix**: add a new section at the top of `CHANGELOG.md` with the version heading and describe the changes made since the last release

## 4. Update version strings

Update the version in both:
- `Jellyfin.Plugin.UserRatings.csproj` — `AssemblyVersion` and `FileVersion`
- `manifest.json` — add a new version entry at the top of the `versions` array with:
  - `version`: new version string
  - `changelog`: **shortened** plain-text description — include only the main changes (a few bullet points max). The full changelog goes in `CHANGELOG.md`. The manifest changelog is rendered in Jellyfin's plugin catalog UI where long text renders poorly. Do NOT copy the full CHANGELOG.md entry here. Do NOT use Markdown or any other formatting (no `**bold**`, no `###` headers, no backticks) — Jellyfin renders this as plain text.
  - `targetAbi`: same as previous entry
  - `sourceUrl`: update version segment in URL
  - `checksum`: leave empty (will be computed during build)
  - `timestamp`: current date in ISO format

## 5. Confirm plan with user

Before executing any git operations, present the plan:
```
Release plan:
  Version:     vX.Y.Z.W
  Bump type:   fix/feature/beta
  Changelog:   (yes/no — summary of changes)
  Files:       (list of modified files)
  Tag:         vX.Y.Z.W
  Push to:     origin/main
```
Wait for explicit user confirmation before proceeding.

## 6. Commit, tag, and push

Only after user confirmation:
1. `git add -A && git commit -m "vX.Y.Z.W: <summary>"`
2. `git tag vX.Y.Z.W`
3. `git push` (pushes commits)
4. `git push origin vX.Y.Z.W` (pushes the tag explicitly — `--follow-tags` does not reliably push lightweight tags)