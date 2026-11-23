"""Render exported production geometry, not concept art or a Railroader screenshot.
Run Test-RunenwachtGeometry.ps1 first, then:
blender -b -t 4 --python tools/Preview-Runenwacht.py -- build/runenwacht-preview.json build/runenwacht-preview.png
"""
import bpy,json,sys,math
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:];parts=json.loads(Path(args[0]).read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def mat(name,col,metal=.1,rough=.4,emission=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*col,1);m.use_nodes=True;n=m.node_tree.nodes.get('Principled BSDF');n.inputs['Base Color'].default_value=(*col,1);n.inputs['Metallic'].default_value=metal;n.inputs['Roughness'].default_value=rough
 if emission:
  n.inputs['Emission Color'].default_value=(*col,1);n.inputs['Emission Strength'].default_value=emission
 else:
  noise=m.node_tree.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=120;noise.inputs['Detail'].default_value=2
  bump=m.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.12;bump.inputs['Distance'].default_value=.008;m.node_tree.links.new(noise.outputs['Fac'],bump.inputs['Height']);m.node_tree.links.new(bump.outputs['Normal'],n.inputs['Normal'])
 return m
steel=mat('brushed aged steel',(.42,.46,.48),.25);copper=mat('warm aged copper',(.8,.47,.23),.5);ceramic=mat('ivory ceramic',(.87,.81,.66),.02,.3);stone=mat('cut foundation stone',(.61,.6,.55),0,.86);reflector=mat('polished inset reflector',(.58,.64,.66),.36,.27)
colours={'Red':(.9,.06,.025),'Amber':(1,.52,.04),'Green':(.04,.86,.65),'Violet':(.64,.13,.9),'White':(.8,.9,1)}
positions={'main':-1.8,'distant':0,'shunt':1.8}
for part in parts:
 kind=part['kind'];vertices=[(v[0]+positions[kind],v[2],v[1]) for v in part['vertices']];faces=[];indices=[]
 for m,ts in enumerate(part['triangles']):
  for i in range(0,len(ts),3):faces.append((ts[i],ts[i+2],ts[i+1]));indices.append(m)
 mesh=bpy.data.meshes.new(kind+' '+part['name']);mesh.from_pydata(vertices,[],faces);mesh.update();obj=bpy.data.objects.new(mesh.name,mesh);bpy.context.collection.objects.link(obj)
 if part['surface']=='metal':mats=[steel,copper,ceramic]
 elif part['surface']=='stone':mats=[stone]*3
 elif part['surface']=='reflector':mats=[reflector]*3
 else:
  hue=colours[part['channel']];base=tuple(.68*x+.32*y for x,y in zip((.42,.47,.5),hue));active=part['channel']=='Red'
  mats=[mat(kind+part['channel']+' faceted body',base,.05,.21),mat(kind+part['channel']+' edges',hue if active else (.35,.4,.43),.05,.3,.38 if active else 0),mat(kind+part['channel']+' vein',hue if active else (.3,.35,.38),.05,.3,1.2 if active else 0)]
 for m in mats:mesh.materials.append(m)
 for polygon,i in zip(mesh.polygons,indices):polygon.material_index=i
# Labels are illustrative plaque text; housing/crystals above come directly from production export.
for kind,label,y in [('main','HS-087',3.83),('distant','VS-088',1.9),('shunt','RS-089',.82)]:
 bpy.ops.object.text_add(location=(positions[kind],-.465,y));t=bpy.context.object;t.rotation_euler=(math.pi/2,0,0);t.data.body=label;t.data.align_x='CENTER';t.data.align_y='CENTER';t.data.size=.085;t.data.extrude=.0002;t.data.materials.append(mat('engraved code '+kind,(.06,.08,.09),0,.6))
bpy.ops.mesh.primitive_plane_add(size=200);bpy.context.object.data.materials.append(mat('studio ground',(.15,.18,.19),0,.8))
world=bpy.context.scene.world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.32,.4,.5,1);world.node_tree.nodes['Background'].inputs[1].default_value=.65
for name,loc,power,size in [('soft daylight',(1,-4,8),650,6),('side fill',(-5,-1,4),350,5),('rim',(3,4,6),600,4)]:
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.size=size;obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj);obj.location=loc;obj.rotation_euler=(Vector((0,0,2.5))-obj.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(5,-12,7));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,2.55))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=8.3
scene=bpy.context.scene;scene.camera=camera;scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=False;scene.render.resolution_x=1500;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.filepath=args[1];scene.view_settings.view_transform='AgX';scene.view_settings.look='None';bpy.ops.wm.save_as_mainfile(filepath=str(Path(args[1]).with_suffix('.blend')));bpy.ops.render.render(write_still=True)
