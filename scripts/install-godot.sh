#!/usr/bin/env bash
# Official, pinned binaries only. Check their published SHA-512 before extraction.
set -euo pipefail
version=4.7.2-stable
base="https://github.com/godotengine/godot-builds/releases/download/$version"
root="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$root/.tools"
cd "$root/.tools"
curl -fL --retry 3 "$base/SHA512-SUMS.txt" -o SHA512-SUMS.txt
fetch_verified() {
  local name="$1"
  curl -fL --retry 3 "$base/$name" -o "$name"
  grep " $name$" SHA512-SUMS.txt | sha512sum --check --strict
}
editor="Godot_v${version}_mono_linux_x86_64.zip"
fetch_verified "$editor"
unzip -qo "$editor" -d "$root/.tools/editor"
if [[ "${1:-}" == "--templates" ]]; then
  templates="Godot_v${version}_mono_export_templates.tpz"
  fetch_verified "$templates"
  mkdir -p extracted-templates
  unzip -qo "$templates" -d extracted-templates
  template_version="$(tr -d '\r\n' < extracted-templates/templates/version.txt)"
  [[ "$template_version" == 4.7.2.stable* ]] || { echo 'Unexpected template version'; exit 1; }
  destination="${XDG_DATA_HOME:-$HOME/.local/share}/godot/export_templates/$template_version"
  mkdir -p "$destination"
  cp -a extracted-templates/templates/. "$destination/"
  echo "Installed export templates at $destination"
fi
