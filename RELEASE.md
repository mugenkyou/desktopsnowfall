# Making a release

Releases are built by GitHub Actions from a version tag. Nothing is published automatically: the workflow creates a **draft** that you check and then publish yourself.

## 1. Get main ready

1. Merge the changes into `main` through a pull request, and wait for **CI** to pass on both Windows and macOS
2. Download the CI artifacts and try them: run the Windows installer and the portable exe, and if you have a Mac (or a friend with one), the `.app`. Go through the hands-on checklist in [CONTRIBUTING.md](CONTRIBUTING.md)
3. Update `README.md` / `FEATURES.md` if anything changed for users

## 2. Tag it

```sh
git checkout main
git pull
git tag v1.2.0           # pick the next version
git push origin v1.2.0
```

The **Release** workflow then:

- runs the Windows self-tests
- builds the Windows x64 and arm64 portable zips and the x64 installer (version taken from the tag)
- builds the universal macOS `.app` zip (ad-hoc signed, not notarized)
- drops the version from the file names (so the README's `releases/latest/download/…` links always get the newest)
- writes `SHA256SUMS.txt`
- creates a **draft** release with all of them and auto-generated notes

## 3. Publish

Open the repository's **Releases** page, check the draft (files, notes, the checksums), edit the notes if you like, and press **Publish release**.

## What users will see (unsigned builds)

- **Windows:** SmartScreen says "Windows protected your PC": **More info → Run anyway**
- **macOS:** "can't be opened": **System Settings → Privacy & Security → Open Anyway** (already explained in the README)

## Signing later (optional, needs paid accounts)

Neither platform is signed today. When you want to remove those warnings:

**macOS (Apple Developer Program, $99/year)**

1. Create a *Developer ID Application* certificate and an app-specific password (or an App Store Connect API key)
2. Add them to the repository as encrypted secrets (for example `MACOS_CERT_P12`, `MACOS_CERT_PASSWORD`, `APPLE_ID`, `APPLE_TEAM_ID`, `APPLE_APP_PASSWORD`)
3. In `release.yml`, import the certificate into a temporary keychain, replace the ad-hoc `codesign --sign -` in `make_app.sh` with `codesign --force --options runtime --timestamp --sign "Developer ID Application: …"`, then run `xcrun notarytool submit dist/*.zip --wait` and `xcrun stapler staple dist/desktopsnowfall.app` before zipping
4. Check the result with `spctl -a -vv dist/desktopsnowfall.app` (should say "accepted, source=Notarized Developer ID")

**Windows (a code-signing certificate)**

Sign the exe and the installer with `signtool sign /fd sha256 /tr <timestamp server> /td sha256 …` in `release.yml`, using a certificate stored as a secret. Inno Setup can also sign the uninstaller through its `SignTool` setting.

Until a signing step has actually run and been checked with `spctl` / `signtool verify`, describe the builds as unsigned.
