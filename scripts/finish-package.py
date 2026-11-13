#!/usr/bin/env python3
"""Finish a self-contained build with matching notices, provenance and per-file checksums."""
from __future__ import annotations
import hashlib, json, os, shutil, subprocess, sys, urllib.request
from pathlib import Path

root=Path(__file__).resolve().parent.parent
for argument in sys.argv[1:]:
    package=Path(argument).resolve()
    assert package.is_dir(), f'Missing package: {package}'
    deps_files=list(package.rglob('Yokai.Client.deps.json'))
    assert len(deps_files)==1, 'Expected exactly one published .NET dependency manifest'
    deps=json.loads(deps_files[0].read_text())
    runtime=[key.rsplit('/',1)[1] for key in deps['libraries'] if key.startswith('runtimepack.Microsoft.NETCore.App.Runtime.')]
    assert len(runtime)==1, 'Export is not a self-contained .NET runtime package'
    version=runtime[0]
    assert all(part.isdigit() for part in version.split('.')), 'Unexpected runtime version'
    windows=any(package.glob('*.exe'))
    assert any(package.rglob('coreclr.dll' if windows else 'libcoreclr.so')), 'Bundled runtime missing'
    assert any(package.rglob('System.Private.CoreLib.dll')), 'Bundled core library missing'
    licenses=package/'licenses'; licenses.mkdir(exist_ok=True)
    sources={
        'Godot-LICENSE.txt':'https://raw.githubusercontent.com/godotengine/godot/4.7.2-stable/LICENSE.txt',
        'Godot-THIRD-PARTY-NOTICES.txt':'https://raw.githubusercontent.com/godotengine/godot/4.7.2-stable/COPYRIGHT.txt',
        'dotnet-LICENSE.txt':f'https://raw.githubusercontent.com/dotnet/runtime/v{version}/LICENSE.TXT',
        'dotnet-THIRD-PARTY-NOTICES.txt':f'https://raw.githubusercontent.com/dotnet/runtime/v{version}/THIRD-PARTY-NOTICES.TXT',
    }
    notice_manifest={}
    for name,url in sources.items():
        request=urllib.request.Request(url,headers={'User-Agent':'YokaiDominion-build-notices'})
        with urllib.request.urlopen(request,timeout=40) as response: content=response.read()
        assert len(content)>100 and b'<html' not in content.lower(), f'Invalid license response: {url}'
        (licenses/name).write_bytes(content)
        notice_manifest[name]={'source':url,'sha256':hashlib.sha256(content).hexdigest()}
    shutil.copy2(root/'assets/CHARACTER01_PROVENANCE.md',licenses/'Character01-PROVENANCE.md')
    if windows:
        (package/'Launch-Compatibility.bat').write_text('@echo off\ncd /d "%~dp0"\nstart "" "%~dp0YokaiDominion.exe" --rendering-method gl_compatibility\n',newline='\r\n')
    metadata={
        'name':'Yokai Dominion','milestone':'0.1.0-dev local vertical slice',
        'source_commit':os.environ.get('GITHUB_SHA','local'),
        'godot':'4.7.2-stable .NET','dotnet_runtime':version,
        'dotnet_sdk':subprocess.check_output(['dotnet','--version'],text=True).strip(),
        'platform':'windows-x64' if windows else 'linux-x64','self_contained':True,
        'notices':notice_manifest,
        'limits':'Development build, unsigned, local bot match only; not final art, animation, cloth, tissue physics or online multiplayer.'
    }
    (package/'BUILD-MANIFEST.json').write_text(json.dumps(metadata,indent=2)+'\n')
    checksums=[]
    for path in sorted(package.rglob('*')):
        if not path.is_file() or path.name=='SHA256SUMS.txt': continue
        digest=hashlib.sha256(path.read_bytes()).hexdigest()
        checksums.append(digest+'  '+path.relative_to(package).as_posix())
    (package/'SHA256SUMS.txt').write_text('\n'.join(checksums)+'\n')
    print(f'PACKAGE_AUDIT_OK platform={metadata["platform"]} runtime={version} files={len(checksums)} commit={metadata["source_commit"]}')
