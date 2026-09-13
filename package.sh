#!/usr/bin/env bash
set -euo pipefail

project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
project_file="$project_dir/FeatherLight.csproj"
stage_root="$project_dir/artifacts/stage"
install_dir_name="Shardbyte-FeatherLight"
mod_dir="$stage_root/SPT_Runtime/user/mods/$install_dir_name"
config_file="$project_dir/config/config.json"

jq -e '
    def valid_ids: type == "array" and all(.[]; type == "string" and test("^[0-9A-Fa-f]{24}$"));
    (.preset | ascii_downcase) as $preset
    | ($preset == "custom" or $preset == "casual" or $preset == "reducedsupplies" or $preset == "fullyweightless")
    and (.conflictDiagnostics.enabled | type == "boolean")
    and (.conflictDiagnostics.maxLoggedItems | type == "number" and . >= 0 and . <= 200 and floor == .)
    and (.includedTemplateIds | valid_ids)
    and (.includedCategoryIds | valid_ids)
    and (.excludedTemplateIds | valid_ids)
    and (.excludedCategoryIds | valid_ids)
    and (
      $preset != "custom"
      or (
        (
          ((.weight.mode | ascii_downcase) == "percentage"
            and (.weight.percentageReduction | type == "number" and . >= 0 and . <= 100))
          or ((.weight.mode | ascii_downcase) == "definitive"
            and (.weight.definitiveWeightKg | type == "number" and . >= 0))
        )
        and ([
          .categories.ammo,
          .categories.ammoBoxes,
          .categories.magazines,
          .categories.throwables,
          .categories.food,
          .categories.drinks,
          .categories.meds,
          .categories.armor,
          .categories.weapons,
          .categories.rigs,
          .categories.backpacks
        ] | all(.[]; type == "boolean"))
      )
    )
' "$config_file" >/dev/null

dotnet build "$project_file" --configuration Release
dotnet run --project "$project_dir/tests/FeatherLight.Tests.csproj" --configuration Release
mod_version="$(dotnet msbuild "$project_file" -nologo -getProperty:Version)"
spt_version="$(dotnet msbuild "$project_file" -nologo -getProperty:SptVersion)"
archive="$project_dir/artifacts/$install_dir_name-$mod_version.zip"

rm -rf -- "$stage_root"
rm -f -- "$archive"
mkdir -p -- "$mod_dir/config"

install -m 0644 "$project_dir/bin/Release/net10.0/FeatherLight.dll" "$mod_dir/FeatherLight.dll"
install -m 0644 "$project_dir/README.md" "$project_dir/LICENSE" "$project_dir/CHANGELOG.md" "$mod_dir/"
install -m 0644 "$config_file" "$mod_dir/config/config.json"
printf '%s\n' \
    "Name: FeatherLight" \
    "Author: Shardbyte" \
    "Version: $mod_version" \
    "SPT version: $spt_version" \
    "GUID: com.shardbyte.featherlight" \
    > "$mod_dir/VERSION.txt"

(
    cd -- "$stage_root"
    zip -qr "$archive" SPT_Runtime
)

unzip -t "$archive"

archive_entries="$(unzip -Z1 "$archive")"
required_dll="SPT_Runtime/user/mods/$install_dir_name/FeatherLight.dll"
if ! grep -Fxq "$required_dll" <<< "$archive_entries"; then
    printf 'Archive is missing required path: %s\n' "$required_dll" >&2
    exit 1
fi

required_version_file="SPT_Runtime/user/mods/$install_dir_name/VERSION.txt"
if ! grep -Fxq "$required_version_file" <<< "$archive_entries"; then
    printf 'Archive is missing required path: %s\n' "$required_version_file" >&2
    exit 1
fi

if grep -Evq '^SPT_Runtime/' <<< "$archive_entries"; then
    printf 'Archive contains files outside SPT_Runtime/\n' >&2
    exit 1
fi

printf 'Created %s\n' "$archive"
