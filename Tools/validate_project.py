"""Python 3 source checks only. This never compiles or runs Unity."""
from pathlib import Path
import json
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
issues = []
guid_paths = {}
assets = ROOT / 'Assets'
for path in assets.rglob('*'):
    if path.suffix == '.meta':
        if not path.with_name(path.name[:-5]).exists():
            issues.append('Orphan metadata: ' + str(path.relative_to(ROOT)))
        continue
    meta = path.with_name(path.name + '.meta')
    if not meta.is_file():
        issues.append('Missing metadata: ' + str(path.relative_to(ROOT)))
        continue
    match = re.search(r'^guid: ([a-f0-9]{32})$', meta.read_text(encoding='utf8'), re.M)
    if not match:
        issues.append('Invalid metadata: ' + str(meta.relative_to(ROOT)))
        continue
    if match.group(1) in guid_paths:
        issues.append('Duplicate GUID: ' + match.group(1))
    guid_paths[match.group(1)] = path

package_refs = json.loads((ROOT / 'Documentation/PackageReferences.json').read_text())
reference_count = 0
local_reference_count = 0
for path in assets.rglob('*'):
    if not path.is_file() or path.suffix not in ('.unity', '.prefab', '.asset', '.physicsMaterial2D'):
        continue
    text = path.read_text(encoding='utf8')
    ids = set(re.findall(r'^--- !u!\d+ &(\d+)', text, re.M))
    # Both Unity's inline maps and expanded maps are accepted.
    references = re.findall(r'\{fileID: (\d+)(?:, guid: ([a-f0-9]{32}), type: \d+)?\}', text)
    lines = text.splitlines()
    for i, line in enumerate(lines):
        matched = re.fullmatch(r'\s+fileID: (\d+)', line)
        if not matched:
            continue
        external = re.fullmatch(r'\s+guid: ([a-f0-9]{32})', lines[i + 1]) if i + 1 < len(lines) else None
        references.append((matched.group(1), external.group(1) if external else ''))
    for object_id, identifier in references:
        if object_id == '0':
            continue
        if identifier:
            reference_count += 1
            if identifier.startswith('0000000000000000'):
                continue
            target = guid_paths.get(identifier)
            if target is None and identifier not in package_refs:
                issues.append('Missing GUID in ' + path.name + ': ' + identifier)
            elif target is not None and target.suffix in ('.prefab', '.asset'):
                target_ids = set(re.findall(r'^--- !u!\d+ &(\d+)', target.read_text(encoding='utf8'), re.M))
                if object_id not in target_ids and not (target.suffix == '.prefab' and object_id == '100100000'):
                    issues.append('Missing external object ID: ' + path.name + ' -> ' + target.name + ':' + object_id)
        else:
            local_reference_count += 1
            if object_id not in ids:
                issues.append('Missing local reference in ' + path.name + ': ' + object_id)

for path in [ROOT / 'Packages/manifest.json', *assets.rglob('*.asmdef')]:
    try:
        json.loads(path.read_text(encoding='utf8'))
    except ValueError as error:
        issues.append(str(path.relative_to(ROOT)) + ': ' + str(error))

expected = {'PlayerController', 'PlatformSpawner', 'CameraFollow', 'ScoreManager', 'GameManager', 'UIManager', 'AudioManager'}
scripts = list((assets / 'Ma3alem/Scripts').rglob('*.cs'))
if {path.stem for path in scripts} != expected | {'BuildingBackdrop'} or len(scripts) != 8:
    issues.append('Expected seven gameplay scripts and the building presentation helper')
build = (ROOT / 'ProjectSettings/EditorBuildSettings.asset').read_text()
for name in ('MainMenu', 'Gameplay'):
    relative = f'Assets/Ma3alem/Scenes/{name}.unity'
    if relative not in build or not (ROOT / relative).exists():
        issues.append('Build scene missing: ' + name)

for path in ROOT.glob('*.md'):
    for target in re.findall(r'\]\(([^)]+)\)', path.read_text(encoding='utf8')):
        from urllib.parse import unquote
        if '://' not in target and not (path.parent / unquote(target.split('#')[0])).exists():
            issues.append('Broken document link in ' + path.name + ': ' + target)

voice_path = assets / 'Audio/VoiceLines'
if not voice_path.is_dir():
    issues.append('Required real-recording folder absent')
report = {
    'status': 'FAIL' if issues else 'PASS',
    'scope': 'Metadata, serialized IDs, source count, package declarations and document links only; not Unity compilation or gameplay',
    'gameplay_scripts': len(scripts) - 1,
    'presentation_support_scripts': 1,
    'metadata_entries': len(guid_paths),
    'external_serialized_references_checked': reference_count,
    'local_serialized_references_checked': local_reference_count,
    'known_package_script_references': package_refs,
    'real_voice_recordings': len(list(voice_path.rglob('*.wav'))),
    'issues': issues,
}
destination = ROOT / 'Documentation/StructuralValidation.json'
destination.write_text(json.dumps(report, indent=2) + '\n', encoding='utf8', newline='\n')
print(json.dumps(report, indent=2))
sys.exit(1 if issues else 0)
