#!/usr/bin/env python3
"""Independent GLB skin validation, using only Python's standard library."""
from __future__ import annotations
import hashlib, json, math, struct, sys
from pathlib import Path

path = Path(sys.argv[1])
raw = path.read_bytes()
magic, version, total = struct.unpack_from('<4sII', raw)
assert magic == b'glTF' and version == 2 and total == len(raw), 'Invalid GLB header'
length, kind = struct.unpack_from('<II', raw, 12)
assert kind == 0x4E4F534A, 'Missing JSON chunk'
doc = json.loads(raw[20:20+length])
start = 20 + length
bin_length, bin_kind = struct.unpack_from('<II', raw, start)
assert bin_kind == 0x004E4942, 'Missing binary chunk'
data = memoryview(raw)[start+8:start+8+bin_length]
components = {5120:('b',1),5121:('B',1),5122:('h',2),5123:('H',2),5125:('I',4),5126:('f',4)}
counts = {'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}
def values(index):
    accessor=doc['accessors'][index]
    view=doc['bufferViews'][accessor['bufferView']]
    code,size=components[accessor['componentType']]; n=counts[accessor['type']]
    offset=view.get('byteOffset',0)+accessor.get('byteOffset',0)
    stride=view.get('byteStride',size*n)
    for i in range(accessor['count']):
        result=struct.unpack_from('<'+code*n,data,offset+i*stride)
        if accessor.get('normalized') and code in ('B','H'):
            divisor=255 if code=='B' else 65535
            result=tuple(v/divisor for v in result)
        yield result
assert len(doc.get('skins',[])) == 1, 'Expected one consolidated skin'
skin=doc['skins'][0]; joints=len(skin['joints'])
assert joints >= 24
assert all('uri' not in image for image in doc.get('images',[])), 'External image'
assert all('uri' not in buffer for buffer in doc.get('buffers',[])), 'External buffer'
vertices=triangles=0; maximum_error=0.0; maximum_influences=0
for mesh in doc['meshes']:
    for primitive in mesh['primitives']:
        attrs=primitive['attributes']; positions=list(values(attrs['POSITION']))
        weights=list(values(attrs['WEIGHTS_0'])); indices=list(values(attrs['JOINTS_0']))
        assert len(positions)==len(weights)==len(indices)
        for p,w,j in zip(positions,weights,indices):
            assert all(math.isfinite(v) for v in p+w), 'Non-finite coordinate/weight'
            assert all(v >= -1e-7 for v in w), 'Negative skin weight'
            assert all(0 <= v < joints for v in j), 'Invalid joint index'
            error=abs(sum(w)-1); maximum_error=max(maximum_error,error)
            assert error < 1e-4, f'Unnormalized skin: {sum(w)}'
            maximum_influences=max(maximum_influences,sum(v>1e-6 for v in w))
        for i, in values(primitive['indices']): assert i < len(positions), 'Index outside vertex buffer'
        vertices += len(positions)
        triangles += doc['accessors'][primitive['indices']]['count']//3
manifest=json.loads(path.with_suffix('.json').read_text())
assert hashlib.sha256(raw).hexdigest() == manifest['game_sha256'], 'Manifest hash mismatch'
assert len(raw) == manifest['game_bytes'] and triangles == manifest['game_triangles']
result={'vertices_with_seams':vertices,'triangles':triangles,'joints':joints,'maximum_weight_error':maximum_error,
        'maximum_influences':maximum_influences,'bytes':len(raw),'sha256':manifest['game_sha256']}
print('GAME_ASSET_AUDIT_OK '+json.dumps(result))
