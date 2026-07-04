# Changelog

All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0). Note that this project does not follow
semantic versioning but uses version numbers based on JetBrains [Rider](https://www.jetbrains.com/rider/) releases.
The EnhancedUnrealEngineDocumentation plugin adds a convenient way to get improved documentation for Unreal Engine reflection specifiers provided by [BenUI](https://twitter.com/_benui) right in the [Rider IDE](https://www.jetbrains.com/rider/)

## [Unreleased]

## [1.0.25]

### Changed

- Rework Quick Doc rendering to match Rider's native documentation look: divider under the title, signature badge (UPROPERTY/UCLASS/UFUNCTION/...) moved right under it, native section layout, syntax highlighting and link groups (Related, Incompatible, etc.) rendered as part of the Remarks block
- Verify compatibility with 2026.1, 2026.2 and 2026.3 (261, 262, 263)

### Fixed

- Fix a bug introduced in the previous release, when Quick Doc was presenting only for a symbol under caret, ignoring mouse position
- Fix broken image links in documentation after retargeting the documentation submodule to the canonical JetBrains/UE-Specifier-Docs repo
- Fix signature badge always showing "UPROPERTY" instead of the actual macro (UCLASS, UFUNCTION, ...)

## [1.0.23]

### Fixed

- Fix formatting in doc files that prevented from presenting some docs (e.g. `GetKeyOptions` for `UPROPERTY`)

## [1.0.22]

### Fixed

- Bump 2026.1 IDE support

## [1.0.14]

### Fixed

- Bump 2024.1 IDE support

## [1.0.13]

- Fix EnhancedUnrealEngineDocumentation breaking default C++ quick docs on some systems

## [1.0.12]

### Added

- [ISSUE-9](https://github.com/JetBrains/EnhancedUnrealEngineDocumentation/issues/9) Support for meta specifiers in UE reflections

### Changed

### Deprecated

### Removed

### Fixed

### Security

## 1.0.11 - 2023-09-14

### Fixed

- Crash on reading bundled documentation

### Added

- Logging for crash on reading bundled documentation

## 1.0.10

### Fixed

- Support 2023.3 IDE's

## 1.0.8

### Added

- Pulling new docs from Ben

### Fixed

- Support 2023.2 IDE's

## 1.0.7

### Added

- Pulling new docs from Ben

## 1.0.6 - 2023-02-22

### Fixed

- Port plugin to 2022.3 SDK

## 1.0.5

### Fixed

- Fix missing docs in the plugin

## 1.0.4

### Fixed

- Crash on startup due to reading args.yaml file that doesn't have reflection specifiers

## 1.0.3

### Added

- Initial release
