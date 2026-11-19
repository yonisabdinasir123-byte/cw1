# Submission Checklist

## 1. Final settings
- [ ] Game scene, GameManager, Round Manager: Round Duration Seconds is **60** (already set in the repository).
- [ ] Tools > Arena > 6. Verify Setup prints "all checks passed".
- [ ] Build Settings list only MainMenu (0) and Game (1).
- [ ] One full two-player match plays with no red Console errors.

## 2. Remove unused assets (confirm before deleting)

| Candidate | Size | Why it is safe to remove |
|---|---|---|
| `Assets/Photon/PhotonUnityNetworking/Demos/` | 30.0 MB | Photon example scenes; none of our scripts use them |
| `Assets/Photon/PhotonChat/Demos/` | 2.0 MB | Chat example; we do not use Photon Chat |
| `Assets/Photon/PhotonRealtime/Demos/` | 0.4 MB | Realtime example scene |
| `Assets/Photon/PhotonNetworking-Documentation.chm` | 1.5 MB | Offline help file |
| `Assets/TutorialInfo/` and `Assets/Readme.asset` | < 0.1 MB | Unity template welcome page |
| `Assets/Scenes/SampleScene.unity` and `Assets/Settings/SampleSceneProfile.asset` | < 0.1 MB | Template scene, not in Build Settings |

Keep: the rest of `Assets/Photon` (Code, Libs, Realtime/Code, Chat/Code, UtilityScripts, Resources with
your App ID), `InputSystem_Actions` (referenced by project settings), `Assets/Settings` (URP assets).

Removing these takes the project from about 38 MB to about 4 MB, before Library is excluded.

## 3. Folders and files that must NOT be in the zip
Unity regenerates all of these. They are already in `.gitignore`.

- [ ] `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Builds/`
- [ ] `.vs/`, `.idea/`, `.vscode/`
- [ ] `*.csproj`, `*.sln`, `*.slnx`
- [ ] `.git/` (version history, not needed by the marker)

## 4. Make the zip
Zip from your **local** folder, never from a fresh git clone. The Photon DLLs are stored with Git LFS, and a
plain clone without LFS contains placeholder files instead of the real DLLs.

1. Quit Unity.
2. Open Terminal and run:

```bash
cd ~/Documents/GitHub/cw1
```
```bash
zip -r ~/Desktop/cw1-submission.zip Assets Packages ProjectSettings docs README.md .gitignore
```

This only includes the folders the marker needs, so Library, Temp, Logs, UserSettings and .git are left out automatically.

## 5. Check the zip works
- [ ] Unzip `cw1-submission.zip` into a new folder on the Desktop.
- [ ] Unity Hub > Add > Add project from disk > that folder. Open it (first open takes a few minutes).
- [ ] Console has no red errors. Tools > Arena > 6. Verify Setup passes. Press Play in MainMenu and connect.
- [ ] Note the zip size (it should be a few MB).

## 6. Other deliverables
- [ ] `docs/IMPLEMENTATION_SUMMARY.md` with the 4 screenshots added, exported to PDF (open it in VS Code with the Markdown preview, or paste it into Word, then print to PDF). Keep it to about one page: use a small font for the code.
- [ ] Demo video, 5 to 7 minutes, following `docs/VIDEO_SCRIPT.md`, with voice-over, exported as MP4.
- [ ] Upload the zip, the PDF and the video as your module requires.
