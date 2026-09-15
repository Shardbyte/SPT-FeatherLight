# Testing FeatherLight

Run from the repository root on Linux with .NET SDK 10, `jq`, `zip`, and `unzip` available:

```bash
bash package.sh
```

The package entry point validates configuration shape and bounds, builds Release, executes the FeatherLight regression harness, creates the install ZIP, checks its CRC, and verifies its allowed root and required files. Any non-zero exit blocks release.

For live acceptance, use a disposable SPT 4.1.5 profile. Test each preset and a custom configuration, confirm included and excluded items retain the expected weights across server restarts, and inspect the server log for bounded conflict diagnostics. Restore the original configuration and profile after testing. Automated checks do not establish live SPT behavior.
