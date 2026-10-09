#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [[ "$(uname -s)" != Darwin ]]; then echo 'Run this script on a Mac with Xcode and .NET 10 MAUI installed.' >&2; exit 1; fi
platform="${1:-mac}"
brand="${2:-BigDaySorted}"
case "$brand" in BigDaySorted|LiveDaysSorted) ;; *) echo 'Choose BigDaySorted or LiveDaysSorted.' >&2; exit 1 ;; esac
stage="$(node scripts/prepare-native-brand.mjs "$brand")"
project="$stage/src/BigDaySorted.App/BigDaySorted.App.csproj"
case "$platform" in
 mac) target=net10.0-maccatalyst; rid="${BDS_APPLE_RID:-maccatalyst-arm64}" ;;
 ios) target=net10.0-ios; rid=ios-arm64 ;;
 simulator) target=net10.0-ios; rid=iossimulator-arm64 ;;
 *) echo 'Choose mac, ios or simulator.' >&2; exit 1 ;;
esac
args=(publish "$project" "-p:AppBrand=$brand" -f "$target" -c Release "-p:BigDaySortedTargetFramework=$target" "-p:RuntimeIdentifier=$rid" "-p:ApplicationVersion=${BDS_BUILD_NUMBER:-5}" -o "artifacts/$brand/$platform/build-${BDS_BUILD_NUMBER:-5}")
if [[ "$platform" != simulator ]]; then
 : "${BDS_CODESIGN_KEY:?Set BDS_CODESIGN_KEY to your Apple signing identity.}"
 args+=("-p:CodesignKey=$BDS_CODESIGN_KEY")
 if [[ -n "${BDS_PROVISION_PROFILE:-}" ]]; then args+=("-p:CodesignProvision=$BDS_PROVISION_PROFILE"); fi
 if [[ "$platform" == ios ]]; then args+=(-p:ArchiveOnBuild=true -p:BuildIpa=true); else args+=(-p:CreatePackage=true); fi
fi
dotnet "${args[@]}"
