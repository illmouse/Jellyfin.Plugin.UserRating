# Release Process

## General Rules

- Always pull with rebase (`git pull --rebase`) before starting work to integrate remote changes. This must be done before EVERY commit, not just at session start — the CI/CD pipeline builds and pushes changes (checksums, version bumps) to the repo between releases, so local state will diverge. Pulling rebase before each commit avoids having to resolve conflicts every time you push a new version
- Always ask before committing and pushing changes
- Never overwrite or move existing git tags — always create a new tag. Only force-replace a tag if the user explicitly asks
- A git tag must be created for every version (including betas) — Jellyfin repository requires a tag to fetch the plugin version
- In manifests, always add new versions as new entries at the top of the `versions` array — never replace or remove previous entries
- Changelog: only include bug fixes that existed in a previous stable release. Do not include bugs found and fixed during the same development session — keep CHANGELOG.md clean from noise
- Stable release changelogs: aggregate all user-facing features and changes from the entire beta cycle since the last stable release, not just the delta from the immediate previous version. A stable release is the culmination of its beta cycle — its changelog should reflect everything a user upgrading from the previous stable version would see. Bug fixes introduced and resolved within the same beta cycle (never affecting stable users) should be omitted per the rule above
- **Versioning (from v2.0.0)**: three parts `X.Y.Z`. Stable tag `vX.Y.Z`, beta tag `vX.Y.Z-beta` (e.g. `v2.0.0-beta`). The beta/stable decision is made only by the tag suffix. CI derives the four-part version `X.Y.Z.0` for the assembly and manifests (Jellyfin compares `System.Version`, and `2.0.0` != `2.0.0.0`), so in `csproj` and manifests always write `X.Y.Z.0`. In `CHANGELOG.md` use the heading `## vX.Y.Z` (add ` (beta)` for betas) — CI extracts the release notes by that heading.
- Promoting a beta to stable with identical code: tag `vX.Y.Z` on the same commit; the manifest-beta entry is updated in place and a stable entry is added to `manifest.json` (add the stable entry with changelog locally first — CI-created entries have no changelog). If fixes are needed, release a new number (`vX.Y.(Z+1)-beta`).
- Legacy scheme (up to and including `1.13.5.0`, the last release for Jellyfin 10.11): four parts, stable = 4th octet 0, beta = non-zero 4th octet.
- `targetAbi` is `12.0.0.0` for all 2.x builds. Jellyfin's catalog hides versions whose `targetAbi` is newer than the server, which keeps 2.x away from 10.x servers (the net10.0 build also cannot load on 10.x).
- The CI build checks out the **tag**, so a release is built from the tagged commit; wait for the CI run (and its manifest commit to `main`) to finish and `git pull --rebase` before the next release.
- GitHub Releases for beta versions must be marked as **Pre-release** (checkbox in the UI / `--prerelease` flag in `gh release create`)

## Manifest Strategy

Two manifest files are maintained:

| Manifest | Contains | Updated on |
|---|---|---|
| `manifest.json` | Stable versions only (4th octet = 0) | Stable releases only |
| `manifest-beta.json` | ALL versions (stable + beta) | Both stable and beta releases |

- Users who want only stable releases add `manifest.json` as their repo URL
- Users who want to test betas add `manifest-beta.json` instead (or in addition)
- The CI pipeline automatically updates the correct manifest(s) based on version

## 1. Determine bump type

Ask the user which bump type:
- **feature** → bump second octet (e.g., `2.0.0` → `2.1.0`)
- **fix** → bump third octet (e.g., `2.0.0` → `2.0.1`)
- **beta** → same bump as above, tag with the `-beta` suffix (e.g., `v2.0.1-beta`)
- (Legacy four-part scheme, up to 1.13.5.0: beta bumped the fourth octet.)

## 2. Calculate new version

Read the current version from `Jellyfin.Plugin.UserRatings.csproj` (AssemblyVersion). Apply the bump:
- feature: increment second octet, reset third and fourth to 0
- fix: increment third octet, reset fourth to 0
- beta: increment fourth octet

## 3. Update changelog

Add a new section at the top of `CHANGELOG.md` with the version heading and describe the changes made since the last release. This applies to **both beta and stable** releases.

## 4. Update version strings

Update the version in both:
- `Jellyfin.Plugin.UserRatings.csproj` — `AssemblyVersion` and `FileVersion`
- The appropriate manifest file:
  - **Beta**: add entry to `manifest-beta.json` only
  - **Stable**: add entry to **both** `manifest.json` and `manifest-beta.json`

For the manifest entry:
- `version`: new version string
- `changelog`: **shortened** plain-text description — include only the main changes (a few bullet points max). The full changelog goes in `CHANGELOG.md`. The manifest changelog is rendered in Jellyfin's plugin catalog UI where long text renders poorly. Do NOT copy the full CHANGELOG.md entry here. Do NOT use Markdown or any other formatting (no `**bold**`, no `###` headers, no backticks) — Jellyfin renders this as plain text.
- `targetAbi`: same as previous entry (`12.0.0.0` for 2.x)
- `sourceUrl`: update version segment in URL
- `checksum`: leave empty (will be computed during build)
- `timestamp`: current date in ISO format

## 5. Confirm plan with user

Before executing any git operations, present the plan:
```
Release plan:
  Version:     vX.Y.Z.W
  Bump type:   fix/feature/beta
  Changelog:   yes — <summary>
  Manifests:   manifest.json / manifest-beta.json / both
  Tag:         vX.Y.Z.W
  GitHub:      Release / Pre-release
  Push to:     origin/main
```
Wait for explicit user confirmation before proceeding.

## 6. Commit, tag, and push

Only after user confirmation:
1. `git stash && git pull --rebase && git stash pop && git add -A && git commit -m "vX.Y.Z.W: <summary>"`
2. `git tag vX.Y.Z.W`
3. `git push` (pushes commits)
4. `git push origin vX.Y.Z.W` (pushes the tag explicitly — `--follow-tags` does not reliably push lightweight tags)

## 7. Handle build failures

If the CI/CD build fails after pushing:
- **Do NOT bump the version number.** The version/tag was never successfully deployed.
- Fix the build errors in the same commit (amend or new commit).
- Force-push the fixed commit: `git push --force`
- Delete and recreate the same tag pointing to the fixed commit: `git tag -d vX.Y.Z.W && git push origin :refs/tags/vX.Y.Z.W && git tag vX.Y.Z.W && git push origin vX.Y.Z.W`
- Only bump the version if the build completed successfully and you need a new release.
