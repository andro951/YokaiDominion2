"""Create a separate game adaptation of the user's immutable Art008 asset.
Run inside Blender: blender --background --python scripts/art/rig_character.py -- source.glb output.glb
The visual master is never edited. This creates a first-pass rig, NOT final animation/tissue acceptance.
"""
from __future__ import annotations
import bpy, hashlib, json, math, os, struct, sys
from pathlib import Path
from mathutils import Vector

args = sys.argv[sys.argv.index('--') + 1:]
source, output = map(Path, args[:2])
expected = 'bf422fa4e363f9eb4a04fbc2bb9cb306469fdcbba60123bccdd365203e0856f9'
assert hashlib.sha256(source.read_bytes()).hexdigest() == expected, 'Source asset hash mismatch'
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(source))
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
original_triangles = sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
for obj in meshes:
    bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    triangles = sum(len(p.vertices)-2 for p in obj.data.polygons)
    if triangles > 400:
        ratio = min(1.0, 42000/triangles) if obj.name == 'Body' else .24
        modifier = obj.modifiers.new('Game-only topology reduction', 'DECIMATE')
        modifier.ratio = ratio; modifier.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=modifier.name)
for image in bpy.data.images:
    if image.size[0] <= 0 or image.size[1] <= 0: continue
    limit = 2048 if 'body' in image.name.lower() else 1024
    largest = max(image.size)
    if largest > limit:
        factor = limit/largest
        image.scale(max(1,round(image.size[0]*factor)), max(1,round(image.size[1]*factor)))
        image.pack()

armature = bpy.data.armatures.new('Character01_GameRig')
rig = bpy.data.objects.new('Character01_GameRig', armature)
bpy.context.collection.objects.link(rig)
bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode='EDIT')
# Blender Z-up; source GLB's forward +Z imports as Blender -Y.
def bone(name, head, tail, parent=None):
    b = armature.edit_bones.new(name); b.head=head; b.tail=tail
    if parent: b.parent=armature.edit_bones[parent]
    b.use_deform = name != 'Root'
    return b
bone('Root',(0,0,0),(0,0,.15))
bone('Hips',(0,0,1.02),(0,0,1.18),'Root')
bone('Spine',(0,0,1.18),(0,-.01,1.39),'Hips')
bone('Chest',(0,-.01,1.39),(0,0,1.60),'Spine')
bone('Neck',(0,0,1.60),(0,0,1.72),'Chest')
bone('Head',(0,0,1.72),(0,0,1.95),'Neck')
for side, sign in [('L',1),('R',-1)]:
    bone('Shoulder.'+side,(sign*.045,0,1.59),(sign*.225,0,1.58),'Chest')
    bone('UpperArm.'+side,(sign*.225,0,1.58),(sign*.355,0,1.285),'Shoulder.'+side)
    bone('LowerArm.'+side,(sign*.355,0,1.285),(sign*.445,-.005,1.025),'UpperArm.'+side)
    bone('Hand.'+side,(sign*.445,-.005,1.025),(sign*.49,-.025,.915),'LowerArm.'+side)
    bone('UpperLeg.'+side,(sign*.105,0,1.02),(sign*.13,-.025,.555),'Hips')
    bone('LowerLeg.'+side,(sign*.13,-.025,.555),(sign*.13,.012,.105),'UpperLeg.'+side)
    bone('Foot.'+side,(sign*.13,.012,.105),(sign*.13,-.16,.045),'LowerLeg.'+side)
    bone('Toes.'+side,(sign*.13,-.16,.045),(sign*.13,-.245,.035),'Foot.'+side)
bone('Skirt.Front',(0,-.17,1.18),(0,-.18,.42),'Hips')
bone('Skirt.Back',(0,.15,1.18),(0,.18,.42),'Hips')
tail=[(0,.15,1.05),(.02,.25,.88),(.21,.29,.84),(.39,.21,.98),(.45,.08,1.21),(.455,.06,1.50)]
for i in range(len(tail)-1): bone('Tail.'+str(i),tail[i],tail[i+1],'Hips' if i==0 else 'Tail.'+str(i-1))
bpy.ops.object.mode_set(mode='OBJECT')
body = next(o for o in meshes if o.name == 'Body')
bpy.ops.object.select_all(action='DESELECT'); body.select_set(True); rig.select_set(True); bpy.context.view_layer.objects.active=rig
try: bpy.ops.object.parent_set(type='ARMATURE_AUTO')
except RuntimeError as e: print('HEAT_WEIGHT_FALLBACK', str(e))
segments = {b.name:(b.head_local.copy(),b.tail_local.copy()) for b in armature.bones if b.use_deform}

def segment_distance(p, a, b):
    d=b-a; t=max(0.0,min(1.0,(p-a).dot(d)/max(1e-12,d.length_squared)))
    return (p-(a+d*t)).length

def fallback_weights(p, names=None):
    if names is None:
        side='L' if p.x>=0 else 'R'
        if abs(p.x)>.255 and p.z>.78:
            names=['Shoulder.'+side,'UpperArm.'+side,'LowerArm.'+side,'Hand.'+side]
        elif p.z<1.08:
            names=['Hips','UpperLeg.'+side,'LowerLeg.'+side,'Foot.'+side,'Toes.'+side]
        else: names=['Hips','Spine','Chest','Neck','Head']
    distances=sorted((segment_distance(p,*segments[n]),n) for n in names)[:3]
    values=[(n,1/max(.025,d)**4) for d,n in distances]; total=sum(w for _,w in values)
    return [(n,w/total) for n,w in values]

def assign(obj, vertex, values):
    for name, value in values:
        if value<1e-6: continue
        group=obj.vertex_groups.get(name) or obj.vertex_groups.new(name=name)
        group.add([vertex.index],value,'REPLACE')

unweighted=0
for v in body.data.vertices:
    weights=[(body.vertex_groups[g.group].name,g.weight) for g in v.groups if body.vertex_groups[g.group].name in segments and g.weight>1e-6]
    if not weights:
        unweighted+=1; assign(body,v,fallback_weights(v.co))
    else:
        weights=sorted(weights,key=lambda x:x[1],reverse=True)[:4]; total=sum(x[1] for x in weights)
        for g in list(v.groups): body.vertex_groups[g.group].remove([v.index])
        assign(body,v,[(n,w/total) for n,w in weights])
if not any(m.type=='ARMATURE' for m in body.modifiers):
    m=body.modifiers.new('Game Rig','ARMATURE'); m.object=rig
body.parent=rig
for obj in meshes:
    if obj is body: continue
    text=(obj.name+' '+ ' '.join(m.name for m in obj.data.materials if m)).lower()
    obj.vertex_groups.clear()
    for v in obj.data.vertices:
        p=v.co
        if 'tail' in text and 'opening' not in text:
            values=fallback_weights(p,[n for n in segments if n.startswith('Tail.')])
        elif any(word in text for word in ['hair','crown','eye','iris','lash','brow','horn','tooth','teeth','fang','ear']):
            values=[('Head',1)]
        elif 'skirt' in text or 'opening hem' in text:
            name='Skirt.Back' if p.y>0 else 'Skirt.Front'
            amount=max(0,min(1,(1.19-p.z)/.2)); values=[('Hips',1-amount),(name,amount)]
        elif 'cord' in text: values=[('Hips',1)]
        elif 'halter' in text: values=fallback_weights(p,['Hips','Spine','Chest','Neck'])
        elif 'nail' in text: values=[('Hand.L' if p.x>=0 else 'Hand.R',1)]
        else: values=fallback_weights(p)
        assign(obj,v,values)
    obj.parent=rig; m=obj.modifiers.new('Game Rig','ARMATURE'); m.object=rig
# Consolidate 289 objects into one skinned mesh with material surfaces.
bpy.ops.object.select_all(action='DESELECT')
for obj in meshes: obj.select_set(True)
bpy.context.view_layer.objects.active=body
bpy.ops.object.join(); body.name='Character01_GameMesh'
for polygon in body.data.polygons: polygon.use_smooth=True
bpy.ops.object.select_all(action='DESELECT'); body.select_set(True); rig.select_set(True)
output.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.gltf(filepath=str(output),export_format='GLB',use_selection=True,export_skins=True,
    export_animations=False,export_cameras=False,export_lights=False,export_yup=True,export_materials='EXPORT')
raw=output.read_bytes(); length,kind=struct.unpack_from('<II',raw,12); gltf=json.loads(raw[20:20+length])
assert gltf.get('skins'), 'No exported skin'
assert len(gltf['skins'][0]['joints'])>=24, 'Incomplete rig'
assert all('uri' not in image for image in gltf.get('images',[])), 'External image dependency'
assert all('uri' not in buffer for buffer in gltf.get('buffers',[])), 'External buffer dependency'
triangles=sum(gltf['accessors'][p['indices']]['count']//3 for mesh in gltf['meshes'] for p in mesh['primitives'])
assert triangles<180000, f'Unexpected triangle budget: {triangles}'
assert len(raw)<30*1024*1024, 'Asset exceeds game-copy size budget'
manifest={
 'source_repository':'andro951/YokaiDominion','source_commit':'4bec672d1e7836dec2d0db141ff6236e3c722395',
 'source_path':'art/checkpoints/portable008/e8a6f76486e9/Character01_Visual008.glb','source_sha256':expected,
 'source_triangles':original_triangles,'game_triangles':triangles,'game_bytes':len(raw),'game_sha256':hashlib.sha256(raw).hexdigest(),
 'source_revision':os.environ.get('GITHUB_SHA','local'),'bones':list(segments),'heat_unweighted_vertices_fixed':unweighted,
 'mesh_count':len(gltf['meshes']),'materials':len(gltf.get('materials',[])),'images':len(gltf.get('images',[])),
 'status':'First-pass game rig. Procedural runtime poses; not final animation, calibrated tissue, or approved Vaelira likeness.',
 'license':'Base topology: Blender Studio/community Human Base Meshes v1.4.1, CC0. Project additions: user-owned Character01 Art008 work.'}
output.with_suffix('.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('CHARACTER_GAME_RIG_OK',json.dumps(manifest))
